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
using System.Globalization;

namespace SeleniumTest {
	class Program {
		static string _JustsystemsGetAllLinksJs;
		static string _JustsystemsGetDownloadLinksJs;
		static string _JustsystemsGetSupportLinksJs;
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
			_JustsystemsGetAllLinksJs = GetSource("JustsystemsGetAllLinks.js");
			_JustsystemsGetDownloadLinksJs = GetSource("JustsystemsGetDownloadLinks.js");
			_JustsystemsGetSupportLinksJs = GetSource("JustsystemsGetSupportLinks.js");
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
						case "just":
							foreach (var productLink in JustsystemsGetAllProductLinks(server)) {
								var productName = productLink.title;

								var downloadLinks = JustsystemsGetDownloadLinks(server, productLink.url);
								foreach(var downloadLink in downloadLinks.Where(l => l.title.Contains(productName))) {
									var supportLinks = JustsystemsGetSupportLinks(server, downloadLink.url);
									foreach (var supportLink in supportLinks) {
										if (supportLink.title.Contains(" アップデート")) {
											var targetProducts = JustsystemsGetTargetProducts(server, supportLink.url).Split('\n').Select(p => p.Replace("\r", ""));
											if (targetProducts.Where(p => p == productName).Any()) {
												var fileUrl = JustsystemsGetFileUrl(server, supportLink.url);
												Console.WriteLine(productName);
												Console.WriteLine("FileUrl: " + fileUrl);
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

		static Link[] JustsystemsGetAllProductLinks(Server server) {
			server.Navigate("http://support.justsystems.com/jp/");
			return Json.ToObject<Link[]>(server.ExecuteScript(_JustsystemsGetAllLinksJs));
		}

		static Link[] JustsystemsGetDownloadLinks(Server server, string url) {
			server.Navigate(url);
			return Json.ToObject<Link[]>(server.ExecuteScript(_JustsystemsGetDownloadLinksJs));
		}

		static LinkAndUpdateDate[] JustsystemsGetSupportLinks(Server server, string url) {
			server.Navigate(url);
			return Json.ToObject<LinkAndUpdateDate[]>(server.ExecuteScript(_JustsystemsGetSupportLinksJs));
		}

		static string JustsystemsGetFileUrl(Server server, string url) {
			server.Navigate(url);
			return server.ExecuteScript(_JustsystemsGetFileUrlJs);
		}

		static string JustsystemsGetTargetProducts(Server server, string url) {
			server.Navigate(url);
			return server.ExecuteScript(_JustsystemsGetTargetProductsJs);
		}
	}
}
