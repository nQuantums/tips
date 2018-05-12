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

namespace SeleniumTest {
	class Program {
		const string LocalHttpServerUrl = "http://localhost:9999/";

		static event Action<string> Unload;
		static event Action<string> Focus;
		static event Action<string> Blur;
		static event Action<string, string> VisibilityChange;

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
					using (var sr = new StreamReader(asm.GetManifestResourceStream("SeleniumTest.setuniqueid.js"), Encoding.UTF8)) {
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
					using (var sr = new StreamReader(asm.GetManifestResourceStream("SeleniumTest.setevents.js"), Encoding.UTF8)) {
						_SetEventsJs = sr.ReadToEnd();
					}
				}
				return _SetEventsJs;
			}
		}

		static void Main(string[] args) {
			var cts = new CancellationTokenSource();
			var t = StartHttpServer(cts.Token);

			RunControlBrowser();

			StopHttpServer(t, cts);
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

				var pageInitHandler = new Action<string>((handle) => {
					lock (chrome) {
						Console.WriteLine("--------");
						chrome.ExecuteScript(SetEventsJs, handle, LocalHttpServerUrl);
						Console.WriteLine(chrome.Url);
						Console.WriteLine("--------");
					}
				});

				pageInitHandler(chrome.CurrentWindowHandle);
				pageInitHandler(chrome.CurrentWindowHandle);
				pageInitHandler(chrome.CurrentWindowHandle);
				pageInitHandler(chrome.CurrentWindowHandle);


				var unloadHandler = new Action<string>((handle) => {
					Task.Run(() => {
						//Thread.Sleep(500);
						lock (chrome) {
							Console.WriteLine(chrome.Url);
							pageInitHandler(chrome.CurrentWindowHandle);
						}
					});
				});
				Unload += unloadHandler;

				var focusHandler = new Action<string>(handle => {
				});
				Focus += focusHandler;

				var blurHandler = new Action<string>(handle => {
					Task.Run(() => {
						//Thread.Sleep(1000);
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
									pageInitHandler(chrome.CurrentWindowHandle);
								}
							}
						}
					});
				});
				Blur += blurHandler;

				var visibilityChangeHandler = new Action<string, string>((handle, visibilityState) => {
					if (visibilityState == "hidden") {
						Task.Run(() => {
							//Thread.Sleep(1000);
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
										pageInitHandler(chrome.CurrentWindowHandle);
									}
								}
							}
						});
					} else if (visibilityState == "visible") {
						Task.Run(() => {
							//Thread.Sleep(100);
							lock (chrome) {
								var curHandles = chrome.WindowHandles;
								var index = curHandles.IndexOf(handle);
								Console.WriteLine(index);
								if (0 <= index) {
									chrome.SwitchTo().Window(curHandles[index]);
								}
							}
						});
					}
				});
				VisibilityChange += visibilityChangeHandler;


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
								} else if (names[0] == "cxp") {
									if (2 <= names.Length && currentElement != null) {
										foreach (var e in (elements = currentElement.FindElements(By.XPath(names[1])))) {
											WriteLine(e, names, 2);
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
								} else if (names[0] == "httptest") {
									var js = @"
(async() => {
	try {
		const response = await fetch('http://localhost:9999/index.html?id=32');
		console.log(await response.text());
		console.log(response.status);
	} catch (e) {
		console.log('error!')
	}
})();
";
									Console.WriteLine(chrome.ExecuteScript(js));
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

					var ev = req.QueryString["event"];
					Console.WriteLine(ev);
					switch (ev) {
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
					}
					//Console.WriteLine(req.HttpMethod);
					//foreach (var value in req.QueryString) {
					//	Console.WriteLine(req.QueryString[value.ToString()]);
					//}

					// レスポンス内容設定
					res.ContentEncoding = Encoding.UTF8;
					res.ContentType = "text/plain";

					var sb = new StringBuilder();
					//sb.AppendLine($"{req.HttpMethod} {req.RawUrl} HTTP/{req.ProtocolVersion}");
					//sb.AppendLine(String.Join("\r\n", req.Headers.AllKeys.Select(k => $"{k}: {req.Headers[k]}")));
					//sb.AppendLine(req.Url.ToString());
					//sb.AppendLine(req.RawUrl);
					//foreach (var value in req.QueryString) {
					//	sb.AppendLine($"{value}: {req.QueryString[value.ToString()]}");
					//}

					var sw = new StreamWriter(res.OutputStream, Encoding.UTF8);
					sw.WriteLine(sb.ToString());
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
		/// xp{{XPath}} 取得要素の表示属性名... の様な形式の文字列からXPathと要素名一覧を取得する
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
	}
}
