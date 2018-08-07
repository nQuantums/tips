using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace SeleniumTest {
	class Program {
		static string _JustsystemsGetAllLinksJs;
		static string _JustsystemsGetDownloadLinksJs;

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

		static string GetSource(string name) {
			var asm = Assembly.GetExecutingAssembly();
			using (var sr = new StreamReader(asm.GetManifestResourceStream("SeleniumTest." + name), Encoding.UTF8)) {
				return sr.ReadToEnd();
			}
		}

		static void Main(string[] args) {
			_JustsystemsGetAllLinksJs = GetSource("JustsystemsGetAllLinks.js");
			_JustsystemsGetDownloadLinksJs = GetSource("JustsystemsGetDownloadLinks.js");

			using (var server = new Server()) {
				server.Start();

				string line;
				while ((line = Console.ReadLine()) != null) {
					if (line == "exit") {
						break;
					} else {
						var fields = line.Split(' ');
						switch (fields[0]) {
						case "just":
							foreach (var l1 in JustSystemsGetAllProductLinks(server)) {
								Console.WriteLine(l1);
								foreach (var l2 in JustSystemsGetDownloadLinks(server, l1.url)) {
									Console.WriteLine(l2);
								}
							}
							break;
						}
					}
				}
			}
		}

		static Link[] JustSystemsGetAllProductLinks(Server server) {
			using (var ev = new ManualResetEvent(false)) {
				server.Navigate("http://support.justsystems.com/jp/", () => ev.Set());
				ev.WaitOne();

				var jsresult = server.Chrome.ExecuteScript(_JustsystemsGetAllLinksJs);
				return Json.ToObject<Link[]>(jsresult.ToString());
			}
		}

		static Link[] JustSystemsGetDownloadLinks(Server server, string url) {
			using (var ev = new ManualResetEvent(false)) {
				server.Navigate(url, () => ev.Set());
				ev.WaitOne();

				var jsresult = server.Chrome.ExecuteScript(_JustsystemsGetDownloadLinksJs);
				return Json.ToObject<Link[]>(jsresult.ToString());
			}
		}
	}
}
