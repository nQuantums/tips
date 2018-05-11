using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace SeleniumTest {
	class Program {
		static readonly Regex ExtractXPathRegex = new Regex(@"[^\{]*\{\{(?<xp>[^\}]*)\}\}([ ]+(?<atr>[^ ]+))*", RegexOptions.Compiled);

		static void Main(string[] args) {
			// ChromeDriverオブジェクトを生成します。
			var chromeOptions = new ChromeOptions();
			chromeOptions.Proxy = null;
			using (var chrome = new ChromeDriver(Environment.CurrentDirectory, chromeOptions)) {

				// URLに移動します。
				chrome.Url = @"http://www.google.com";

				//IJavaScriptExecutor js = chrome as IJavaScriptExecutor;

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
								var cur = chrome.CurrentWindowHandle;
								var wnds = chrome.WindowHandles;
								foreach (var w in wnds) {
									chrome.SwitchTo().Window(w);
									Console.WriteLine(chrome.Url);
								}
								chrome.SwitchTo().Window(cur);
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
							}
						}
					}
				}

				// ブラウザを閉じます。
				chrome.Quit();
			}
		}

		static void WriteLine(IWebElement e, string[] names, int attributeIndex) {
			Console.WriteLine($"{e.TagName} {e}");
			Console.WriteLine(e.Text);
			for (int i = attributeIndex; i < names.Length; i++) {
				Console.WriteLine($"\t{names[i]}: {e.GetAttribute(names[i])}");
			}
		}

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
