using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Support;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.Events;
using System.Globalization;

namespace SeleniumTest {
	class Program {
		static string _JustsystemsGetAllLinksJs;
		static string _JustsystemsGetDownloadLinksJs;
		static string _JustsystemsGetResultLinksJs;
		static string _JustsystemsGetFileUrlJs;
		static string _JustsystemsGetTargetProductsJs;

		/// <summary>
		/// Chromeに仕込んだJavaScriptから受け取るリンク情報
		/// </summary>
		[DataContract]
		public class Link {
			/// <summary>
			/// タイトル
			/// </summary>
			[DataMember]
			public string title;

			/// <summary>
			/// リンク先URL
			/// </summary>
			[DataMember]
			public string url;

			public override string ToString() {
				return string.Concat(this.title, " ", this.url);
			}
		}

		/// <summary>
		/// Chromeに仕込んだJavaScriptから受け取るリンクと更新日時情報
		/// </summary>
		[DataContract]
		public class LinkAndUpdateDate {
			DateTime _UpdateDateTime;

			/// <summary>
			/// タイトル
			/// </summary>
			[DataMember]
			public string title;

			/// <summary>
			/// リンク先URL
			/// </summary>
			[DataMember]
			public string url;

			/// <summary>
			/// 更新日時
			/// </summary>
			[DataMember]
			public string updateDate;

			public DateTime UpdateDateTime {
				get {
					if (_UpdateDateTime == DateTime.MinValue) {
						DateTime dt;
						var ci = CultureInfo.CurrentCulture;
						var dts = DateTimeStyles.None;
						if (DateTime.TryParseExact(this.updateDate, "yyyy.MM.dd", ci, dts, out dt)) {
							_UpdateDateTime = dt;
						} else if (DateTime.TryParseExact(this.updateDate, "yyyy/MM/dd", ci, dts, out dt)) {
							_UpdateDateTime = dt;
						} else {
							throw new ApplicationException("更新日時のフォーマットが未知のものです");
						}
					}
					return _UpdateDateTime;
				}
			}

			public override string ToString() {
				return string.Concat(this.title, " ", this.url, " ", this.updateDate);
			}
		}

		static string GetSource(string name) {
			var asm = Assembly.GetExecutingAssembly();
			using (var sr = new StreamReader(asm.GetManifestResourceStream("SeleniumTest." + name), Encoding.UTF8)) {
				return sr.ReadToEnd();
			}
		}

		static void Main(string[] args) {
			_JustsystemsGetAllLinksJs = GetSource("JustsystemsGetAllLinks.js");
			_JustsystemsGetDownloadLinksJs = GetSource("JustsystemsGetDownloadLinks.js");
			_JustsystemsGetResultLinksJs = GetSource("JustsystemsGetResultLinks.js");
			_JustsystemsGetFileUrlJs = GetSource("JustsystemsGetFileUrl.js");
			_JustsystemsGetTargetProductsJs = GetSource("JustsystemsGetTargetProducts.js");

			using (var server = new Server()) {
				server.Start();

				string line;
				while ((line = Console.ReadLine()) != null) {
					if (line == "exit") {
						break;
					} else {
						var fields = line.Split(' ');
						switch (fields[0]) {
						case "justt": {
								// Justsystems の製品検索ページから対象製品を集めつつ対象内の新規製品情報も集める
								foreach (var productLink in JustsystemsGetAllProductLinks(server)) {
									var productFullName = productLink.title;
									//Console.WriteLine(productFullName);

									bool isNewProduct;
									var pi = GetTargetProductInfo(productLink.url, productFullName, _JustsystemProducts, out isNewProduct);
									if (pi != null) {
										Console.WriteLine(productFullName);
									} else if (isNewProduct) {
										Console.WriteLine("新規: " + productFullName);
									}
								}
							}
							break;
						case "just": {
								var products = new List<ProductInfo>();
								var newProducts = new List<string>();

								// Justsystemsのサポート・ダウンロードページ
								//     http://support.justsystems.com/jp/index.html#main1
								// 内の「検索」ボタンで検索して飛んだ先のURL取得
								foreach (var productLink in JustsystemsGetAllProductLinks(server)) {
									var productFullName = productLink.title;

									bool isNewProduct;
									var pi = GetTargetProductInfo(productLink.url, productFullName, _JustsystemProducts, out isNewProduct);
									if (pi != null) {
										products.Add(pi);
									} else if (isNewProduct) {
										newProducts.Add(productFullName);
									}
								}

								// 未知の新規製品があったらエラーとする
								if (newProducts.Count != 0) {
									var sb = new StringBuilder();
									sb.AppendLine("以下の新製品があります。ソースコード内の " + nameof(_JustsystemProducts) + " を新製品にも対応できるよう修正してください。 ");
									foreach (var np in newProducts) {
										sb.AppendLine(np);
									}
									throw new ApplicationException(sb.ToString());
								}

								// 対象製品のパッチファイルダウンロードURLを辿る
								foreach (var tp in products) {
									var pm = tp.ProductMatching;

									Console.WriteLine(tp.ProductFullName);

									// 製品のサポートページ
									//     http://support.justsystems.com/jp/severally/detail1561.html
									// の様なページから「ダウンロード」の項目を探しその中のリンクを取得
									var downloadLinks = JustsystemsGetDownloadLinks(server, tp.Url);
									foreach (var downloadLink in downloadLinks.Where(l => pm.Exists(l.title))) {
										// 製品のリリース情報検索ページ
										//     http://support.justsystems.com/faq/1032/app/servlet/qasearchtop?MAIN=002001003001021
										// の様なページから検索結果の一覧取得
										var resultLinks = JustsystemsGetResultLinks(server, downloadLink.url);
										foreach (var resultLink in resultLinks) {
											// 検索結果の内「ATOK 2017 for Windows アップデートモジュール」の様なものを検索
											if ((resultLink.title.Contains("アップデートモジュール") || resultLink.title.Contains("セキュリティ更新")) && pm.Exists(resultLink.title)) {
												// モジュールのダウンロードページ
												//     http://support.justsystems.com/faq/1032/app/servlet/qadoc?QID=056492
												// の様なページから対象製品一覧取得
												var targetProducts = JustsystemsGetTargetProducts(server, resultLink.url).Split('\n').Select(p => p.Replace("\r", ""));
												if (targetProducts.Where(p => pm.Exists(p)).Any()) {
													// 対象製品一覧内に現在注視している製品があればファイルのURLをJavaScript内から取得
													var fileUrl = JustsystemsGetFileUrl(server, resultLink.url);
													var ext = Path.GetExtension(fileUrl);

													Console.Write(tp.ProductFullName);
													Console.Write("\t");
													Console.WriteLine(fileUrl);
													break; // 上の方に新しいものがあるので最初に見つかったものを優先
												}
											}
										}
									}
								}
							}
							break;
						}
					}
				}
			}
		}

		/// <summary>
		/// 製品マッチング条件、既知の製品で除外したいものにマッチする
		/// </summary>
		class ProductMatchingExclude {
			/// <summary>
			/// 製品の基本名
			/// </summary>
			public string BaseName { get; private set; }

			/// <summary>
			/// 製品名にマッチする正規表現
			/// </summary>
			public Regex Regex { get; private set; }

			/// <summary>
			/// 製品名全体にマッチする正規表現、文字列の末端までを含みマッチする
			/// </summary>
			public Regex RegexFull { get; private set; }

			public ProductMatchingExclude(string baseName, string editionRegex) {
				var pattern = baseName + editionRegex;
				this.BaseName = baseName;
				this.Regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
				this.RegexFull = new Regex(pattern + "$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
			}

			public bool FullMatch(string text) {
				return this.RegexFull.Match(text).Success;
			}

			public bool Exists(string text) {
				return this.Regex.Match(text).Success;
			}
		}

		/// <summary>
		/// 製品マッチング条件、既知の製品で対象としたいものにマッチする
		/// </summary>
		class ProductMatching : ProductMatchingExclude {
			/// <summary>
			/// <see cref="EditionRegex"/>を使って製品名へ置き換えるための置き換えパターン
			/// </summary>
			public string ProductNamePattern { get; private set; }

			/// <summary>
			/// <see cref="EditionRegex"/>を使って正規化された製品名へ置き換えるための置き換えパターン
			/// </summary>
			public string SymbolPattern { get; private set; }

			public ProductMatching(string baseName, string editionRegex, string productName, string symbol) : base(baseName, editionRegex) {
				this.ProductNamePattern = productName;
				this.SymbolPattern = symbol;
			}

			public string GetProductName(string fullProductName) {
				return this.BaseName + this.RegexFull.Replace(fullProductName, this.ProductNamePattern);
			}

			public string GetProductSymbol(string fullProductName) {
				return this.BaseName + this.RegexFull.Replace(fullProductName, this.SymbolPattern);
			}
		}

		/// <summary>
		/// 製品情報
		/// </summary>
		class ProductInfo {
			/// <summary>
			/// 製品用のページのURL
			/// </summary>
			public string Url { get; private set; }

			/// <summary>
			/// 製品の基本名、「一太郎」など
			/// </summary>
			public string BaseName { get; private set; }

			/// <summary>
			/// エディションなども含めた製品名
			/// </summary>
			public string ProductFullName { get; private set; }

			/// <summary>
			/// エディションなどを除いた製品名
			/// </summary>
			public string ProductName { get; private set; }

			/// <summary>
			/// 製品を表す記号、AssetViewP 側での表記
			/// </summary>
			public string Symbol { get; private set; }

			/// <summary>
			/// 製品名マッチング情報
			/// </summary>
			public ProductMatching ProductMatching;

			public ProductInfo(string url, string baseName, string productFullName, string productName, string symbol, ProductMatching productMatching) {
				this.Url = url;
				this.BaseName = baseName;
				this.ProductFullName = productFullName;
				this.ProductName = productName;
				this.Symbol = symbol;
				this.ProductMatching = productMatching;
			}
		}


		/// <summary>
		/// Justsystems社製の対象製品
		/// </summary>
		static ProductMatchingExclude[] _JustsystemProducts = {
			new ProductMatching("ATOK", " *(?<year>[0-9]+)(|(?<arc> *for +Windows))", " ${year}", "${year}"),
			new ProductMatchingExclude("ATOK", ".*"),
			new ProductMatching("一太郎", " *(?<n>[0-9]+)", "${n}", "${n}"),
			new ProductMatching("一太郎", " *Lite *(?<n>[0-9]*)", "Lite${n}", "Lite${n}"),
			new ProductMatching("一太郎", " *Government(?<s> *)(|(?<n>[0-9]+))", "Government${s}${n}", "Government${n}"),
			new ProductMatching("一太郎", " *ガバメント(?<s> *)(|(?<n>[0-9]+))", "ガバメント${s}${n}", "ガバメント${n}"),
			new ProductMatching("一太郎", " *Pro(?<s> *)(|(?<n>[0-9]+))", "Pro${s}${n}", "Pro${n}"),
			new ProductMatchingExclude("一太郎", ".*"),
			new ProductMatching("花子", " *(?<n>[0-9]+)", "${n}", "${n}"),
			new ProductMatching("花子", " *Police(?<s> *)(|(?<n>[0-9]+))", "Police${s}${n}", "Police${n}"),
			new ProductMatching("花子", " *Pro(?<s> *)(|(?<n>[0-9]+))", "Pro${s}${n}", "Pro${n}"),
			new ProductMatchingExclude("花子", ".*"),
			new ProductMatching("三四郎", " *(?<n>[0-9]+)", "${n}", "${n}"),
			new ProductMatchingExclude("三四郎", ".*"),
		};

		/// <summary>
		/// 指定の製品名が対象製品なら製品情報を取得する
		/// </summary>
		/// <param name="url">製品情報ページのURL</param>
		/// <param name="fullProductName">製品の名称</param>
		/// <param name="productMatchings">対象製品を判別するためのマッチング情報一覧</param>
		/// <param name="isNewProduct">製品が未知の新製品なら true がセットされる</param>
		/// <returns>製品情報</returns>
		static ProductInfo GetTargetProductInfo(string url, string fullProductName, ProductMatchingExclude[] productMatchings, out bool isNewProduct) {
			var baseNameMatched = false;
			foreach (var pme in productMatchings) {
				var baseName = pme.BaseName;

				if (fullProductName.StartsWith(baseName)) {
					baseNameMatched = true; // 基本名が一致するものがあった

					var m = pme.RegexFull.Match(fullProductName);
					if (m.Success) {
						isNewProduct = false;
						var pm = pme as ProductMatching;
						if (pm == null) {
							// 対象外製品とする
							return null;
						} else {
							// 対象製品とする
							return new ProductInfo(
								url,
								baseName,
								fullProductName,
								pm.GetProductName(fullProductName),
								pm.GetProductSymbol(fullProductName),
								pm);
						}
					}
				}
			}
			isNewProduct = baseNameMatched; // 基本名が一致するものがあったが以降が一致するものが無かったら新規製品となる
			return null;
		}

		/// <summary>
		/// Justsystemsの製品検索トップページから製品名と製品用ページの一覧を取得する
		/// <para>http://support.justsystems.com/jp/</para>
		/// </summary>
		static Link[] JustsystemsGetAllProductLinks(Server server) {
			server.Navigate("http://support.justsystems.com/jp/");
			var links = Json.ToObject<Link[]>(server.ExecuteScript(_JustsystemsGetAllLinksJs));
			return (from g in links.GroupBy(l => l.title) select g.First()).ToArray();
		}

		/// <summary>
		/// Justsystemsの製品用ページからダウンロード用のリンク一覧を取得する
		/// <para>次の様なページから取得する http://support.justsystems.com/jp/severally/detail1559.html </para>
		/// </summary>
		static Link[] JustsystemsGetDownloadLinks(Server server, string url) {
			server.Navigate(url);
			return Json.ToObject<Link[]>(server.ExecuteScript(_JustsystemsGetDownloadLinksJs));
		}

		/// <summary>
		/// Justsystemsのアップデートモジュール検索用ページから該当ページのリンク一覧を取得する
		/// <para>次の様なページから取得する http://support.justsystems.com/faq/1032/app/servlet/qasearchtop?MAIN=002001002001025 </para>
		/// </summary>
		static LinkAndUpdateDate[] JustsystemsGetResultLinks(Server server, string url) {
			server.Navigate(url);
			return Json.ToObject<LinkAndUpdateDate[]>(server.ExecuteScript(_JustsystemsGetResultLinksJs));
		}

		/// <summary>
		/// JustsystemsのアップデートモジュールダウンロードページからパッチファイルのURLを取得する
		/// <para>次の様なページから取得する http://support.justsystems.com/faq/1032/app/servlet/qadoc?QID=056482 </para>
		/// </summary>
		static string JustsystemsGetFileUrl(Server server, string url) {
			server.Navigate(url);
			return server.ExecuteScript(_JustsystemsGetFileUrlJs);
		}

		/// <summary>
		/// Justsystemsのアップデートモジュールダウンロードページから対象製品一覧を取得する
		/// <para>次の様なページから取得する http://support.justsystems.com/faq/1032/app/servlet/qadoc?QID=056482 </para>
		/// </summary>
		static string JustsystemsGetTargetProducts(Server server, string url) {
			server.Navigate(url);
			return server.ExecuteScript(_JustsystemsGetTargetProductsJs);
		}
	}
}
