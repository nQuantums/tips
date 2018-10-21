using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeleniumTest {
	class Program {
		static void Main(string[] args) {
			RunControlBrowser();
		}

		/// <summary>
		/// ブラウザの制御を行う
		/// </summary>
		static void RunControlBrowser() {
			// ChromeDriverオブジェクトを生成します。
			var chromeOptions = new ChromeOptions();
			chromeOptions.Proxy = null;
			chromeOptions.AddUserProfilePreference("download.default_directory", @"c:\work");
			chromeOptions.AddUserProfilePreference("safebrowsing.enabled", "true");
			using (var chrome = new ChromeDriver(Environment.CurrentDirectory, chromeOptions)) {
				var windowHandles = new HashSet<string>();

				// URLに移動します。
				chrome.Url = @"https://www.oracle.com/technetwork/java/javase/downloads/jre8-downloads-2133155.html";
				windowHandles.Add(chrome.CurrentWindowHandle);

				try {
					Console.WriteLine(chrome.ExecuteScript("document.documentElement.querySelectorAll('.resultArea .concrete2').forEach(c => console.log(c.querySelector('p').textContent + ' ' + c.querySelector('a').href))"));
				} catch (Exception ex) {
					Console.WriteLine(ex.Message);
				}

				// コマンドラインからの入力をループ
				ReadOnlyCollection<IWebElement> elements = null;
				IWebElement currentElement = null;
				string line;
				while ((line = Console.ReadLine()) != null) {
					if (line == "exit") {
						break;
					} else {
						lock (chrome) {
							var names = line.Split(' ');
							switch (names[0]) {
							case "js":
								try {
									Console.WriteLine(chrome.ExecuteScript(line.Substring(2)));
								} catch (Exception ex) {
									Console.WriteLine(ex.Message);
								}
								break;
							case "list":
								try {
									Console.WriteLine(chrome.ExecuteScript("document.documentElement.querySelectorAll('.resultArea .concrete2').forEach(c => console.log(c.querySelector('p').textContent + ' ' + c.querySelector('a').href))"));
								} catch (Exception ex) {
									Console.WriteLine(ex.Message);
								}
								break;
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
