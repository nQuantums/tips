using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using HtmlAgilityPack;

namespace MyJVNApiTest {
	class Program {
		[DllImport("User32.dll")]
		private static extern short GetAsyncKeyState(int vKey);

		static HashSet<string> AllUrls = new HashSet<string>();
		static SemaphoreSlim Sem = new SemaphoreSlim(1, 1);

		static void Main(string[] args) {
			CollectUrls("https://helpx.adobe.com/jp/security/products/acrobat/apsb18-02.html");
			while (GetAsyncKeyState(0x0D) == 0) {
				lock (AllUrls) {
					Console.WriteLine(AllUrls.Count);
				}
				Thread.Sleep(1000);
			}
			lock (AllUrls) {
				foreach (var url in AllUrls) {
					Console.WriteLine(url);
				}
			}
		}

		static async void CollectUrls(string url) {
			await Sem.WaitAsync();
			try {
				var uri = new Uri(url);
				var scheme = uri.Scheme;
				var host = uri.Host;
				var root = string.Concat(scheme, "://", host);
				var xmlstr = await MyJvnApi.GetStringAsync(uri.ToString());

				var doc = new HtmlDocument();
				doc.LoadHtml(xmlstr);

				var nodes = doc.DocumentNode.SelectNodes("//a[@href]");
				if (nodes != null) {
					foreach (var node in nodes) {
						foreach (var a in node.Attributes) {
							var value = a.Value;
							string detectedUrl = null;
							if (value.StartsWith("http")) {
								detectedUrl = value;
							} else if (value.StartsWith("/")) {
								detectedUrl = root + value;
							}
							if (detectedUrl != null) {
								lock (AllUrls) {
									if (AllUrls.Contains(detectedUrl)) {
										detectedUrl = null;
									} else {
										AllUrls.Add(detectedUrl);
									}
								}
								if (detectedUrl != null) {
									CollectUrls(detectedUrl);
								}
							}
						}
					}
				}
			} finally {
				Sem.Release();
			}
		}

		static async void GetData() {
			var doc = new HtmlDocument();
			//var uri = new Uri("https://helpx.adobe.com/jp/security/products/acrobat/apsb18-02.html");
			//var uri = new Uri("http://www.adobe.com/jp/support/downloads/product.jsp?product=1&amp;platform=Windows");
			var uri = new Uri("http://supportdownloads.adobe.com/product.jsp?product=1&amp;amp;platform=Windows");
			var scheme = uri.Scheme;
			var host = uri.Host;
			var root = string.Concat(scheme, "://", host);
			var xmlstr = await MyJvnApi.GetStringAsync(uri.ToString());
			var urls = new List<string>();
			doc.LoadHtml(xmlstr);
			foreach (var node in doc.DocumentNode.SelectNodes("//a[@href]")) {
				foreach (var a in node.Attributes) {
					var value = a.Value;
					string url = null;
					if (value.StartsWith("http")) {
						url = value;
					} else if (value.StartsWith("/")) {
						url = root + value;
					}
					if (url != null) {
						lock (AllUrls) {
							AllUrls.Add(url);
						}
					}
				}
			}
			Console.WriteLine(doc);

			//var xdoc = XDocument.Parse(await MyJvnApi.getVulnDetailInfo("JVNDB-2018-002115"));
			////var xdoc = XDocument.Parse(await MyJvnApi.getVulnDetailInfo("JVNDB-2018-999999"));
			////Console.WriteLine(xdoc);
			//foreach (var e in xdoc.XPathSelectElements("/vuldef:VULDEF-Document/vuldef:Vulinfo", NamespaceManager)) {
			//	Console.WriteLine(e);
			//}
		}
	}
}
