using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using OpenQA.Selenium;
using OpenQA.Selenium.Support;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.Events;
using Newtonsoft.Json;
using DbCode;
using DbCode.PgBind;
using Newtonsoft.Json.Linq;
using NMeCab;
using System.Web;

namespace PatchCrawler {
	class Program {
		const string LocalHttpServerUrl = "http://localhost:9999/";

		static IDbCodeCommand Cmd;
		static IDbCodeCommand CmdForSearch;

		static MeCabTagger MeCab;

		static event Action<JObject> PageAfterInit;
		static event Action<string> Unload;
		static event Action<string> Focus;
		static event Action<string> Blur;
		static event Action<string, string> VisibilityChange;
		static event Action<string, string, string, string> Jump;

		/// <summary>
		/// XPath指定部分取得用の正規表現
		/// </summary>
		static readonly Regex ExtractXPathRegex = new Regex(@"[^\{]*\{\{(?<xp>[^\}]*)\}\}([ ]+(?<atr>[^ ]+))*", RegexOptions.Compiled);

		/// <summary>
		/// 全HTML要素にユニークIDを付与するJava Scriptの取得
		/// </summary>
		static string _SetUniqueIdJs;
		static string SetUniqueIdJs {
			get {
				if (_SetUniqueIdJs == null) {
					var asm = Assembly.GetExecutingAssembly();
					using (var sr = new StreamReader(asm.GetManifestResourceStream("PatchCrawler.setuniqueid.js"), Encoding.UTF8)) {
						_SetUniqueIdJs = sr.ReadToEnd();
					}
				}
				return _SetUniqueIdJs;
			}
		}

		/// <summary>
		/// ページ初期化のイベント仕込む処理などのJava Scriptの取得
		/// </summary>
		static string _SetEventsJs;
		static string SetEventsJs {
			get {
				if (_SetEventsJs == null) {
					var asm = Assembly.GetExecutingAssembly();
					using (var sr = new StreamReader(asm.GetManifestResourceStream("PatchCrawler.setevents.js"), Encoding.UTF8)) {
						_SetEventsJs = sr.ReadToEnd();
					}
				}
				return _SetEventsJs;
			}
		}

		static void Main(string[] args) {
			// 形態素解析のMeCabを初期化
			var mecabPara = new MeCabParam();
			mecabPara.DicDir = @"c:\dic\mecab-ipadic-neologd";
			MeCab = MeCabTagger.Create(mecabPara);

			// データベース関係初期化
			Db.Initialize();

			// データベースへ接続しブラウザの制御を開始する
			using (var con = Db.CreateConnection())
			using (var conForSearch = Db.CreateConnection()) {
				con.Open();
				conForSearch.Open();
				Cmd = con.CreateCommand();
				CmdForSearch = conForSearch.CreateCommand();

				var cts = new CancellationTokenSource();
				var t = StartHttpServer(cts.Token);
				RunControlBrowser();
				StopHttpServer(t, cts);
			}
		}

		/// <summary>
		/// ブラウザの制御を行う
		/// </summary>
		static void RunControlBrowser() {
			// ChromeDriverオブジェクトを生成します。
			var chromeOptions = new ChromeOptions();
			chromeOptions.Proxy = null;
			using (var chrome = new ChromeDriver(Environment.CurrentDirectory, chromeOptions)) {
				var windowHandles = new HashSet<string>();

				// URLに移動します。
				chrome.Url = @"http://www.google.com";
				windowHandles.Add(chrome.CurrentWindowHandle);

				// ページを支配下に置くための初期化、既に初期化済みの場合には処理はスキップされる
				var pageInitializer = new Action<string>((handle) => {
					lock (chrome) {
						Console.WriteLine("----init start----");
						chrome.ExecuteScript(SetEventsJs, handle, LocalHttpServerUrl);
						Console.WriteLine("----init end----");
					}
				});

				// ページ初期化後の処理
				var knownUrls = new HashSet<string>();
				PageAfterInit += new Action<JObject>(jobj => {
					Task.Run(() => {
						lock (Cmd) {
							Console.WriteLine("----after init start----");
							var url = (string)jobj["url"];
							if (!knownUrls.Contains(url)) {
								knownUrls.Add(url);

								// Google検索によるURLなら時間などの毎回変わるパラメータを除いたURLに再構築する
								if (url.StartsWith("https://www.google.co.jp/search") || url.StartsWith("https://www.google.com/search")) {
									var uri = new Uri(url);
									var path = uri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped);
									var dic = HttpUtility.ParseQueryString(uri.Query);
									var list = new List<Tuple<string, string>>();
									foreach (string param in dic) {
										switch (param) {
										case "q":
										case "oq":
											list.Add(new Tuple<string, string>(param, dic[param]));
											break;
										}
									}
									url = path + "?" + string.Join("&", from i in list select HttpUtility.UrlEncode(i.Item1) + HttpUtility.UrlEncode(i.Item2));
								}

								var title = (string)jobj["title"];
								var text = (string)jobj["text"];
								var content = (string)jobj["content"];
								var links = jobj["links"];
								var searchWord = (string)jobj["searchWord"];

								var urlID = Db.AddUrl(Cmd, url);
								Db.AddUrlTitle(Cmd, urlID, title);
								Db.AddUrlContent(Cmd, urlID, content);
								if (!string.IsNullOrEmpty(searchWord)) {
									Db.AddUrlSearchWord(Cmd, urlID, searchWord);
								}

								foreach (var chunk in ChunkedEnumerate(DetectKeywords(url), 1000)) {
									Db.AddUrlKeywords(Cmd, from kvp in chunk select new Db.TbUrlKeyword.R(urlID, Db.AddKeyword(Cmd, kvp.Key), kvp.Value));
								}
								foreach (var chunk in ChunkedEnumerate(DetectKeywords(title), 1000)) {
									Db.AddTitleKeywords(Cmd, from kvp in chunk select new Db.TbTitleKeyword.R(urlID, Db.AddKeyword(Cmd, kvp.Key), kvp.Value));
								}
								foreach (var chunk in ChunkedEnumerate(DetectKeywords(text), 1000)) {
									Db.AddContentKeywords(Cmd, from kvp in chunk select new Db.TbContentKeyword.R(urlID, Db.AddKeyword(Cmd, kvp.Key), kvp.Value));
								}

								foreach (var l in links.Children<JObject>()) {
									var dstUrl = (string)l["h"];
									var linkText = (string)l["t"];
									var dstUrlID = Db.AddUrl(Cmd, dstUrl);
									var linkID = Db.AddLink(Cmd, urlID, dstUrlID, linkText);
									foreach (var chunk in ChunkedEnumerate(DetectKeywords(linkText), 1000)) {
										Db.AddLinkKeywords(Cmd, from kvp in chunk select new Db.TbLinkKeyword.R(linkID, Db.AddKeyword(Cmd, kvp.Key)));
									}
								}
							}
							Console.WriteLine("----after init end----");
						}
					});
				});

				// ジャンプ時の処理
				var knownLink = new HashSet<int>();
				Jump += new Action<string, string, string, string>((src, dst, linkText, aroundText) => {
					Task.Run(() => {
						lock (Cmd) {
							Console.WriteLine("----jump start----");
							var srcUrlID = Db.AddUrl(Cmd, src);
							var dstUrlID = Db.AddUrl(Cmd, dst);
							var linkID = Db.AddLink(Cmd, srcUrlID, dstUrlID, linkText);
							if (!knownLink.Contains(linkID)) {
								knownLink.Add(linkID);

								Db.AddLinkAroundText(Cmd, linkID, aroundText);
								foreach (var chunk in ChunkedEnumerate(DetectKeywords(aroundText), 1000)) {
									Db.AddLinkAroundKeywords(Cmd, from kvp in chunk select new Db.TbLinkAroundKeyword.R(linkID, Db.AddKeyword(Cmd, kvp.Key)));
								}
								Db.AddJump(Cmd, linkID);
							}
							Console.WriteLine("----jump end----");
						}
					});
				});

				// 新規ウィンドウ検出し初期化する処理
				var newWindowDetector = new Action(() => {
					lock (chrome) {
						var curHandles = new HashSet<string>();
						foreach (var h in chrome.WindowHandles) {
							curHandles.Add(h);
						}

						windowHandles.RemoveWhere(h => !curHandles.Contains(h));

						foreach (var h in curHandles) {
							if (!windowHandles.Contains(h)) {
								windowHandles.Add(h);
								chrome.SwitchTo().Window(h);
								pageInitializer(chrome.CurrentWindowHandle);
							}
						}
					}
				});


				pageInitializer(chrome.CurrentWindowHandle);


				// ページ終了した後のページで初期化する
				Unload += new Action<string>((handle) => {
					Task.Run(() => {
						Console.WriteLine(chrome.Url);
						pageInitializer(chrome.CurrentWindowHandle);
					});
				});

				Focus += new Action<string>(handle => {
				});

				// ウィンドウのフォーカスが移ったなら新しいウィンドウを探し出す
				Blur += new Action<string>(handle => {
					Task.Run(() => {
						newWindowDetector();
					});
				});

				// ページの可視状態の切り替わりにより新しいウィンドウを探したりカレントのウィンドウを操作対象とする
				VisibilityChange += new Action<string, string>((handle, visibilityState) => {
					if (visibilityState == "hidden") {
						Task.Run(() => {
							newWindowDetector();
						});
					} else if (visibilityState == "visible") {
						Task.Run(() => {
							// 表示状態になったウィンドウを操作対象とする
							lock (chrome) {
								var curHandles = chrome.WindowHandles;
								var index = curHandles.IndexOf(handle);
								Console.WriteLine(index);
								if (0 <= index) {
									chrome.SwitchTo().Window(handle);
								}
							}
						});
					}
				});

				// コマンドラインからの入力をループ
				ReadOnlyCollection<IWebElement> elements = null;
				IWebElement currentElement = null;
				string line;
				while ((line = Console.ReadLine()) != null) {
					if (line == "exit") {
						break;
					} else {
						lock (chrome) {
							if (int.TryParse(line, out int index)) {
								var wnds = chrome.WindowHandles;
								if ((uint)index < wnds.Count) {
									chrome.SwitchTo().Window(wnds[index]);
								}
							} else {
								var names = line.Split(' ');
								if (names[0].StartsWith("<")) {
									foreach (var e in (elements = chrome.FindElementsByXPath($"//{names[0].Substring(1)}"))) {
										WriteLine(e, names, 1);
										//js.ExecuteScript(script, e, "dummy@user.de");
									}
								} else if (names[0] == "popup") {
									try {
										chrome.SwitchTo().Alert();
									} catch (Exception ex) {
										Console.WriteLine(ex.Message);
									}
								} else if (names[0] == "wnds") {
									foreach (var w in chrome.WindowHandles) {
										chrome.SwitchTo().Window(w);
										pageInitializer(w);
										Console.WriteLine(w);
									}
								} else if (names[0] == "id") {
									if (2 <= names.Length) {
										foreach (var e in (elements = chrome.FindElementsById(names[1]))) {
											WriteLine(e, names, 2);
										}
									}
								} else if (line.StartsWith("xp{")) {
									var (xp, atrs) = ExtractXPath(line);
									if (xp != null) {
										foreach (var e in (elements = chrome.FindElementsByXPath(xp))) {
											WriteLine(e, atrs, 0);
										}
									}
								} else if (line.StartsWith("cxp{")) {
									var (xp, atrs) = ExtractXPath(line);
									if (xp != null) {
										foreach (var e in (elements = currentElement.FindElements(By.XPath(xp)))) {
											WriteLine(e, atrs, 0);
										}
									}
								} else if (names[0] == "ce") {
									if (2 <= names.Length) {
										if (int.TryParse(names[1], out int elementIndex)) {
											if (elements != null && (uint)elementIndex < elements.Count) {
												WriteLine(currentElement = elements[elementIndex], names, 2);
											}
										}
									}
								} else if (names[0] == "click") {
									if (currentElement != null) {
										chrome.ExecuteScript("arguments[0].scrollIntoView(true);", currentElement);
										Thread.Sleep(500);
										currentElement.Click();
									}
								} else if (names[0] == "setid") {
									Console.WriteLine(chrome.ExecuteScript(SetUniqueIdJs));
								} else if (names[0] == "ready") {
									Console.WriteLine(chrome.ExecuteScript("return document.readyState"));
								} else if (names[0] == "close") {
									chrome.Close();
								} else if (names[0] == "handle") {
									Console.WriteLine(chrome.CurrentWindowHandle);
								} else if (names[0] == "url") {
									Console.WriteLine(chrome.Url);
								} else if (names[0] == "tab") {
									Console.WriteLine(chrome.ExecuteScript("return document.activeElement.tabIndex"));
								} else if (names[0] == "js") {
									Console.WriteLine(chrome.ExecuteScript(line.Substring(2)));
								} else if (names[0] == "init") {
									pageInitializer(chrome.CurrentWindowHandle);
								} else if (names[0] == "s") {
									// s 以降のスペース区切りの文字列を and 条件で検索する

									// 先ず検索のキーワードを正規化
									var keywords = new List<string>();
									for (int i = 1; i < names.Length; i++) {
										foreach (var kvp in DetectKeywords(names[i])) {
											keywords.Add(kvp.Key);
										}
									}

									// キーワード分解結果表示
									Console.WriteLine(string.Join(" & ", keywords));

									// SQL組み立て
									var sql = Db.E.NewSql();
									var fmain = sql.From(Db.Url);
									foreach (var keyword in keywords) {
										var a = new Argument(keyword + "%"); // キーワード指定
										var fsub = sql.From(Db.Keyword);
										var j = fsub.InnerJoin(Db.ContentKeyword, t => t.KeywordID == fsub._.KeywordID);
										fsub.Where(t => Sql.Like(t.Keyword, a));
										fsub.GroupBy(t => new { j._.UrlID });
										fmain.InnerJoin(fsub.Select(t => new { j._.UrlID }), t => t.UrlID == fmain._.UrlID);
									}
									var jt = fmain.InnerJoin(Db.UrlTitle, t => t.UrlID == fmain._.UrlID);

									// 実行
									var func = sql.BuildFuncFromSelect(fmain.Select(t => new { jt._.UrlTitle }));
									using (var reader = func.Execute(CmdForSearch)) {
										foreach (var r in reader.Records) {
											Console.WriteLine(r);
										}
									}
								} else if (names[0] == "l") {
									// l 以降のスペース区切りの文字列を and 条件で検索する

									// 先ず検索のキーワードを正規化
									var keywords = new List<string>();
									for (int i = 1; i < names.Length; i++) {
										foreach (var kvp in DetectKeywords(names[i])) {
											keywords.Add(kvp.Key);
										}
									}

									// キーワード分解結果表示
									Console.WriteLine(string.Join(" & ", keywords));

									// SQL組み立て
									var sql = Db.E.NewSql();
									var fmain = sql.From(Db.Link);
									foreach (var keyword in keywords) {
										var a = new Argument(keyword + "%"); // キーワード指定
										var fsub = sql.From(Db.Keyword);
										var j = fsub.InnerJoin(Db.LinkKeyword, t => t.KeywordID == fsub._.KeywordID);
										fsub.Where(t => Sql.Like(t.Keyword, a));
										fsub.GroupBy(t => new { j._.LinkID });
										fmain.InnerJoin(fsub.Select(t => new { j._.LinkID }), t => t.LinkID == fmain._.LinkID);
									}
									var jt = fmain.InnerJoin(Db.UrlTitle, t => t.UrlID == fmain._.SrcUrlID);

									// 実行
									var func = sql.BuildFuncFromSelect(fmain.Select(t => new { jt._.UrlTitle, fmain._.LinkText }));
									using (var reader = func.Execute(CmdForSearch)) {
										foreach (var r in reader.Records) {
											Console.WriteLine(r);
										}
									}
								}
							}
						}
					}
				}

				// ブラウザを閉じます。
				chrome.Quit();
			}
		}

		/// <summary>
		/// ブラウザのページ内イベント取得とタブ間で同じ値を保持するためにHTTPサーバーを開始する
		/// </summary>
		static async Task StartHttpServer(CancellationToken ct) {
			var listener = new HttpListener();
			listener.Prefixes.Add(LocalHttpServerUrl);
			listener.Start();
			ct.Register(() => listener.Stop());

			while (!ct.IsCancellationRequested) {
				var context = await listener.GetContextAsync();
				var req = context.Request;
				using (var res = context.Response) {
					// ブラウザ内JavaScriptからの接続を許可するのに以下のヘッダが必要
					res.Headers.Add("Access-Control-Allow-Origin", "*");
					res.Headers.Add("Access-Control-Allow-Headers", "Origin, X-Requested-With, Content-Type, Accept");
					res.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");

					var responseBuffer = new StringBuilder();

					var ev = req.QueryString["event"];
					Console.WriteLine(ev);
					switch (ev) {
					case "page_after_init": {
							var d = PageAfterInit;
							if (d != null) {
								JObject jobj;
								using (var sr = new StreamReader(req.InputStream, req.ContentEncoding)) {
									jobj = JObject.Parse(sr.ReadToEnd());
								}
								d(jobj);
							}
						}
						break;
					case "unload": {
							var d = Unload;
							if (d != null) {
								d(req.QueryString["handle"]);
							}
						}
						break;
					case "focus": {
							var d = Focus;
							if (d != null) {
								d(req.QueryString["handle"]);
							}
						}
						break;
					case "blur": {
							var d = Blur;
							if (d != null) {
								d(req.QueryString["handle"]);
							}
						}
						break;
					case "visibilitychange": {
							var d = VisibilityChange;
							if (d != null) {
								d(req.QueryString["handle"], req.QueryString["visibilityState"]);
							}
						}
						break;
					case "click": {
							//using (var sr = new StreamReader(req.InputStream, req.ContentEncoding)) {
							//	Console.WriteLine(sr.ReadToEnd());
							//}
							//responceBuffer.Append(JsonConvert.SerializeObject(new { Result = 0 }));
						}
						break;
					case "jump": {
							string src, dst, linkText, aroundText;
							using (var sr = new StreamReader(req.InputStream, req.ContentEncoding)) {
								var jobj = JObject.Parse(sr.ReadToEnd());
								src = (string)jobj["src"];
								dst = (string)jobj["dst"];
								linkText = (string)jobj["linkText"];
								aroundText = (string)jobj["aroundText"];
							}
							var d = Jump;
							if (d != null) {
								d(src, dst, linkText, aroundText);
							}
						}
						break;
					}

					// レスポンス内容設定
					res.ContentEncoding = Encoding.UTF8;
					res.ContentType = "text/plain";

					var sw = new StreamWriter(res.OutputStream, Encoding.UTF8);
					sw.WriteLine(responseBuffer.ToString());
					sw.Flush();
				}
			}
		}

		/// <summary>
		/// HTTPサーバーの停止
		/// </summary>
		static void StopHttpServer(Task t, CancellationTokenSource cts) {
			cts.Cancel();
			try {
				t.Wait();
			} catch { }
		}

		/// <summary>
		/// 指定要素をコンソールに出力する
		/// </summary>
		/// <param name="e">要素</param>
		/// <param name="names">属性名の配列</param>
		/// <param name="attributeIndex">出力する属性名の開始インデックス</param>
		static void WriteLine(IWebElement e, string[] names, int attributeIndex) {
			Console.WriteLine($"{e.TagName} {e}");
			Console.WriteLine(e.Text);
			for (int i = attributeIndex; i < names.Length; i++) {
				Console.WriteLine($"\t{names[i]}: {e.GetAttribute(names[i])}");
			}
		}

		/// <summary>
		/// xp{{XPath}} 表示属性名... の様な形式の文字列からXPathと属性名一覧を取得する
		/// </summary>
		/// <param name="line">文字列</param>
		/// <returns>XPathと要素名一覧のタプル</returns>
		static (string xp, string[] names) ExtractXPath(string line) {
			var m = ExtractXPathRegex.Match(line);
			var xp = m.Groups["xp"];
			if (!xp.Success) {
				return (null, null);
			}
			var atr = m.Groups["atr"];
			var names = new List<string>();
			if (atr.Success) {
				foreach (Capture c in atr.Captures) {
					names.Add(c.Value);
				}
			}
			return (xp.Value, names.ToArray());
		}

		static readonly Regex RegexCve = new Regex("cve-(|([0-9]+(|-(|([0-9]+)))))$", RegexOptions.Compiled);
		static readonly Regex RegexJvndb = new Regex("jvndb-(|([0-9]+(|-(|([0-9]+)))))$", RegexOptions.Compiled);
		static readonly Regex RegexCveComplete = new Regex("cve-[0-9]+-[0-9]+$", RegexOptions.Compiled);
		static readonly Regex RegexJvndbComplete = new Regex("jvndb-[0-9]+-[0-9]+$", RegexOptions.Compiled);

		/// <summary>
		/// 指定キーワードを特別処理する必要があるなら処理用のデリゲートを返す
		/// </summary>
		/// <param name="keyword">キーワード</param>
		/// <param name="addTodic">キーワードを辞書に登録する処理</param>
		/// <returns>デリゲート</returns>
		static Func<string, bool> SpecialKeyword(string keyword, Action<string> addTodic) {
			switch (keyword) {
			case "cve": {
					var list = new List<string>();
					list.Add(keyword);
					return new Func<string, bool>(k => {
						var n = list.Count;
						if (k.Length != 0) {
							list.Add(k);
						}
						if (k.Length == 0 || !RegexCve.IsMatch(string.Concat(list))) {
							var c = string.Concat(list.Take(n));
							if (RegexCveComplete.IsMatch(c)) {
								addTodic(c);
								Console.WriteLine(c);
							} else {
								list.ForEach(i => addTodic(i));
							}
							return false;
						} else {
							return true;
						}
					});
				}
			case "jvndb": {
					var list = new List<string>();
					list.Add(keyword);
					return new Func<string, bool>(k => {
						var n = list.Count;
						if (k.Length != 0) {
							list.Add(k);
						}
						if (k.Length == 0 || !RegexJvndb.IsMatch(string.Concat(list))) {
							var c = string.Concat(list.Take(n));
							if (RegexJvndbComplete.IsMatch(c)) {
								addTodic(c);
								Console.WriteLine(c);
							} else {
								list.ForEach(i => addTodic(i));
							}
							return false;
						} else {
							return true;
						}
					});
				}
			default:
				return null;
			}
		}

		/// <summary>
		/// <see cref="SpecialKeyword"/>で取得されているデリゲートを通してキーワードを処理する
		/// </summary>
		/// <param name="receivers">デリゲートリスト、処理が完了したものは取り除かれる</param>
		/// <param name="keyword">キーワード</param>
		static void SpecialKeywordProc(List<Func<string, bool>> receivers, string keyword) {
			for (int i = receivers.Count - 1; i != -1; --i) {
				if (!receivers[i](keyword)) {
					receivers.RemoveAt(i);
				}
			}
		}

		/// <summary>
		/// 指定テキスト内に含まれるキーワードとカウントを取得する
		/// </summary>
		/// <param name="text">検出元テキスト</param>
		/// <returns>キーワードをキー、カウントを値とするディクショナリ</returns>
		static Dictionary<string, int> DetectKeywords(string text) {
			var result = new Dictionary<string, int>();
			var receivers = new List<Func<string, bool>>();
			var node = MeCab.ParseToNode(text);

			var addToDic = new Action<string>(k => {
				if (result.ContainsKey(k)) {
					result[k]++;
				} else {
					result[k] = 1;
				}
			});

			while (node != null) {
				if (node.CharType != 0) {
					var keyword = node.Surface.ToLower();
					if (!(node.Feature.StartsWith("記号,") || node.Feature.StartsWith("名詞,数,"))) {
						SpecialKeywordProc(receivers, keyword);

						var r = SpecialKeyword(keyword, addToDic);
						if (r is null) {
							addToDic(keyword);
						} else {
							receivers.Add(r);
						}
					} else {
						SpecialKeywordProc(receivers, keyword);
					}
				}
				node = node.Next;
			}

			SpecialKeyword("", addToDic);

			return result;
		}

		static IEnumerable<IEnumerable<T>> ChunkedEnumerate<T>(IEnumerable<T> items, int chunkSize) {
			while (items.Any()) {
				yield return items.Take(chunkSize);
				items = items.Skip(chunkSize);
			}
		}
	}
}
