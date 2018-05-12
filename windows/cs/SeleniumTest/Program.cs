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
					using (var setuniqueid = new StreamReader(asm.GetManifestResourceStream("SeleniumTest.setuniqueid.js"), Encoding.UTF8)) {
						_SetUniqueIdJs = setuniqueid.ReadToEnd();
					}
				}
				return _SetUniqueIdJs;
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
				// URLに移動します。
				chrome.Url = @"http://www.google.com";

				ReadOnlyCollection<IWebElement> elements = null;
				IWebElement currentElement = null;
				string line;
				while ((line = Console.ReadLine()) != null) {
					if (line == "exit") {
						break;
					} else {
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

				// ブラウザを閉じます。
				chrome.Quit();
			}
		}

		/// <summary>
		/// ブラウザのタブ間で同じ値を保持するためにHTTPサーバーを開始する
		/// </summary>
		static async Task StartHttpServer(CancellationToken ct) {
			var listener = new HttpListener();
			listener.Prefixes.Add("http://localhost:9999/");
			listener.Start();
			ct.Register(() => listener.Stop());

			while (!ct.IsCancellationRequested) {
				var context = await listener.GetContextAsync();
				var req = context.Request;
				using (var res = context.Response) {
					// JavaScript側からの接続を許可するのに以下のヘッダが必要
					res.Headers.Add("Access-Control-Allow-Origin", "*");
					res.Headers.Add("Access-Control-Allow-Headers", "Origin, X-Requested-With, Content-Type, Accept");
					res.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");

					// 内容設定
					res.ContentEncoding = Encoding.UTF8;
					res.ContentType = "text/plain";

					var sb = new StringBuilder($"{req.HttpMethod} {req.RawUrl} HTTP/{req.ProtocolVersion}\r\n");
					sb.AppendLine(String.Join("\r\n", req.Headers.AllKeys.Select(k => $"{k}: {req.Headers[k]}")));
					sb.AppendLine(req.Url.ToString());
					sb.AppendLine(req.RawUrl);
					foreach (var value in req.QueryString) {
						sb.AppendLine($"{value}: {req.QueryString[value.ToString()]}");
					}

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
