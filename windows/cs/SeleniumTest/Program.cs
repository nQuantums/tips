using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace SeleniumTest {
	class Program {
		static void Main(string[] args) {
			// ChromeDriverオブジェクトを生成します。
			var chromeOptions = new ChromeOptions();
			chromeOptions.Proxy = null;
			using (var chrome = new ChromeDriver(Environment.CurrentDirectory, chromeOptions)) {

				// URLに移動します。
				chrome.Url = @"http://www.google.com";

				//IJavaScriptExecutor js = chrome as IJavaScriptExecutor;

				string line;
				while ((line = Console.ReadLine()) != null) {
					if (line == "exit") {
						break;
					} else {
						if (int.TryParse(line, out int index)) {
							var tabs = chrome.WindowHandles;
							if ((uint)index < tabs.Count) {
								chrome.SwitchTo().Window(tabs[index]);
							}
						} else {
							foreach (var e in chrome.FindElementsByXPath($"//{line}")) {
								Console.WriteLine(e.Text);
								var href = e.GetAttribute("href");
								Console.WriteLine(href);
								//js.ExecuteScript(script, e, "dummy@user.de");
							}
						}
					}
				}

				// ブラウザを閉じます。
				chrome.Quit();
			}
		}
	}
}
