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
		static Encoding _SjisEnc = Encoding.GetEncoding("Shift_JIS");
		static string _JustsystemsGetAllLinksJs;
		static string _JustsystemsGetDownloadLinksJs;
		static string _JustsystemsGetResultLinksJs;
		static string _JustsystemsGetFileUrlJs;
		static string _JustsystemsGetTargetProductsJs;

		[DataContract]
		public class Link {
			[DataMember]
			public string title;
			[DataMember]
			public string url;

			public override string ToString() {
				return string.Concat(this.title, " ", this.url);
			}
		}

		[DataContract]
		public class LinkAndUpdateDate {
			DateTime _UpdateDateTime;

			[DataMember]
			public string title;
			[DataMember]
			public string url;
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
			_SjisEnc = Encoding.GetEncoding("Shift_JIS");
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

								// Justsystems の製品検索ページから対象製品を集めつつ対象内の新規製品情報も集める
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
									var productName = tp.ProductName;
									var productFullName = tp.ProductFullName;
									Console.WriteLine(productFullName);

									// 「検索」ボタンで検索して飛んだ先のURL取得
									var downloadLinks = JustsystemsGetDownloadLinks(server, tp.Url);
									foreach (var downloadLink in downloadLinks.Where(l => l.title.Contains(productName))) {
										// 対象モジュールアナウンス検索ページから結果の一覧取得
										var resultLinks = JustsystemsGetResultLinks(server, downloadLink.url);
										foreach (var resultLink in resultLinks) {
											// 検索結果の内「ATOK 2017 for Windows アップデートモジュール」の様なものを検索
											if ((resultLink.title.Contains("アップデートモジュール") || resultLink.title.Contains("セキュリティ更新")) && resultLink.title.StartsWith(productName)) {
												var targetProducts = JustsystemsGetTargetProducts(server, resultLink.url).Split('\n').Select(p => p.Replace("\r", ""));
												if (targetProducts.Where(p => p.Contains(productFullName)).Any()) {
													var fileUrl = JustsystemsGetFileUrl(server, resultLink.url);
													Console.Write(productFullName);
													Console.Write("\t");
													Console.WriteLine(fileUrl);
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

		enum TokenType {
			Unknown,
			Space,
			Digit,
			Alphabet,
			Hiragana,
			Katakana,
			Kanji,
		}


		/// <summary>
		/// 製品マッチング条件
		/// </summary>
		class ProductMatchingExclude {
			/// <summary>
			/// 製品の基本名
			/// </summary>
			public string BaseName { get; private set; }

			/// <summary>
			/// バージョン、エディション以降にマッチする正規表現
			/// </summary>
			public Regex EditionRegex { get; private set; }

			public ProductMatchingExclude(string baseName, string editionRegex) {
				this.BaseName = baseName;
				this.EditionRegex = new Regex(editionRegex, RegexOptions.IgnoreCase);
			}
		}

		/// <summary>
		/// 製品マッチング条件
		/// </summary>
		class ProductMatching : ProductMatchingExclude {
			/// <summary>
			/// <see cref="EditionRegex"/>を使って製品名へ置き換えるための置き換えパターン
			/// </summary>
			public string ProductName { get; private set; }

			/// <summary>
			/// <see cref="EditionRegex"/>を使って正規化された製品名へ置き換えるための置き換えパターン
			/// </summary>
			public string Symbol { get; private set; }

			public ProductMatching(string baseName, string editionRegex, string productName, string symbol) : base(baseName, editionRegex) {
				this.ProductName = productName;
				this.Symbol = symbol;
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
			/// 製品の基本名
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

			public ProductInfo(string url, string baseName, string productFullName, string productName, string symbol) {
				this.Url = url;
				this.BaseName = baseName;
				this.ProductFullName = productFullName;
				this.ProductName = productName;
				this.Symbol = symbol;
			}
		}



		static ProductMatchingExclude[] _JustsystemProducts = {
			new ProductMatching("ATOK", "^ (?<year>[0-9]+) for Windows$", " ${year}", "${year}"),
			new ProductMatchingExclude("ATOK", "^.*"),
			new ProductMatching("一太郎", "^(?<year>[0-9]+)$", "${year}", "${year}"),
			new ProductMatching("一太郎", "^Lite(?<number>[0-9]*)$", "Lite${number}", "Lite${number}"),
			new ProductMatching("一太郎", "^Government$", "Government", "Government"),
			new ProductMatching("一太郎", "^Government (?<number>[0-9]*)$", "Government ${number}", "Government${number}"),
			new ProductMatching("一太郎", "^ガバメント$", "ガバメント", "ガバメント"),
			new ProductMatching("一太郎", "^ガバメント(?<number>[0-9]*)$", "ガバメント ${number}", "ガバメント${number}"),
			new ProductMatching("一太郎", "^Pro$", "Pro", "Pro"),
			new ProductMatching("一太郎", "^Pro (?<number>[0-9]*)$", "Pro ${number}", "Pro${number}"),
			new ProductMatchingExclude("一太郎", "^.*"),
			new ProductMatching("花子", "^(?<year>[0-9]+)$", "${year}", "${year}"),
			new ProductMatching("花子", "^Police$", "Police", "Police"),
			new ProductMatching("花子", "^Police (?<number>[0-9]*)$", "Police ${number}", "Police${number}"),
			new ProductMatching("花子", "^Pro$", "Pro", "Pro"),
			new ProductMatching("花子", "^Pro (?<number>[0-9]*)$", "Pro ${number}", "Pro${number}"),
			new ProductMatchingExclude("花子", "^.*"),
			new ProductMatching("三四郎", "^(?<year>[0-9]+)$", "${year}", "${year}"),
			new ProductMatchingExclude("三四郎", "^.*"),
		};

		static ProductInfo GetTargetProductInfo(string url, string productNameFull, ProductMatchingExclude[] productMatchings, out bool isNewProduct) {
			var matched = false;
			foreach (var pme in productMatchings) {
				var baseName = pme.BaseName;

				if (productNameFull.StartsWith(baseName)) {
					matched = true; // 基本名が一致するものがあった

					var editionName = productNameFull.Substring(baseName.Length);

					var m = pme.EditionRegex.Match(editionName);
					if (m.Success) {
						isNewProduct = false;
						var pm = pme as ProductMatching;
						if (pm == null) {
							// 無視する様に設定されている
							return null;
						} else {
							return new ProductInfo(
								url,
								baseName,
								productNameFull,
								baseName + pme.EditionRegex.Replace(editionName, pm.ProductName),
								baseName + pme.EditionRegex.Replace(editionName, pm.Symbol));
						}
					}
				}
			}
			isNewProduct = matched; // 基本名が一致するものがあったが以降が一致するものが無かったら新規製品となる
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
