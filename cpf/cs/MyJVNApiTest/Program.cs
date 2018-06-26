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

		static SemaphoreSlim Sem = new SemaphoreSlim(10, 10);

		static string[] JvnIDs = {
			"JVNDB-2017-006943",
			"JVNDB-2017-010471",
			"JVNDB-2017-010644",
			"JVNDB-2017-010650",
			"JVNDB-2017-010766",
			"JVNDB-2017-010822",
			"JVNDB-2017-010823",
			"JVNDB-2017-010824",
			"JVNDB-2017-010825",
			"JVNDB-2017-010826",
			"JVNDB-2017-010827",
			"JVNDB-2017-010828",
			"JVNDB-2017-010833",
			"JVNDB-2017-010834",
			"JVNDB-2017-010838",
			"JVNDB-2017-010839",
			"JVNDB-2017-010842",
			"JVNDB-2017-010843",
			"JVNDB-2017-011161",
			"JVNDB-2017-011246",
			"JVNDB-2017-011270",
			"JVNDB-2017-011271",
			"JVNDB-2018-001232",
			"JVNDB-2018-001705",
			"JVNDB-2018-001706",
			"JVNDB-2018-002030",
			"JVNDB-2018-002031",
			"JVNDB-2018-002032",
			"JVNDB-2018-002042",
			"JVNDB-2018-002242",
			"JVNDB-2018-002242",
			"JVNDB-2017-000076",
			"JVNDB-2017-008629",
			"JVNDB-2014-008502",
			"JVNDB-2014-008510",
			"JVNDB-2014-008511",
			"JVNDB-2015-001665",
			"JVNDB-2018-001898",
			"JVNDB-2018-001899",
			"JVNDB-2018-001900",
			"JVNDB-2018-001902",
			"JVNDB-2018-001903",
			"JVNDB-2018-001904",
			"JVNDB-2018-001905",
			"JVNDB-2018-001906",
			"JVNDB-2018-001928",
			"JVNDB-2018-001929",
			"JVNDB-2018-001934",
			"JVNDB-2018-001935",
			"JVNDB-2018-001936",
			"JVNDB-2018-001937",
			"JVNDB-2018-001938",
			"JVNDB-2018-001939",
		};

		static void GetJvnInfo(string jvnid) {
			var doc = XDocument.Parse(MyJvnApi.getVulnDetailInfo(jvnid).Result);
			var title = doc.XPathSelectElement("//vuldef:Title", MyJvnApi.NamespaceManager);

			//Console.WriteLine(title.Value);
			foreach (var e in doc.XPathSelectElements("//vuldef:AffectedItem", MyJvnApi.NamespaceManager)) {
				//var vendorName = e.XPathSelectElement(".//vuldef:Name", MyJvnApi.NamespaceManager);
				var productName = e.XPathSelectElement(".//vuldef:ProductName", MyJvnApi.NamespaceManager);
				var versionNumbers = e.XPathSelectElements(".//vuldef:VersionNumber", MyJvnApi.NamespaceManager);
				Console.WriteLine($"{jvnid}: {productName.Value}");
				break;
				Console.Write("(");
				foreach (var v in versionNumbers) {
					Console.Write($"	{v.Value}");
				}
			}
		}

		static void Main(string[] args) {
			var start = new DateTime(2017, 1, 1);
			var now = DateTime.Now;
			var dateRange = new MyJvnApi.TimeRange(start, now);

			var startItem = 1;

			for(; ; ) {
				var ol = MyJvnApi.getVulnOverviewList(
					startItem: startItem,
					publicDateRange: dateRange,
					publishedDateRange: dateRange,
					firstPublishDateRange: dateRange
				).Result;

				foreach (var item in ol.Items) {
					var title = item.Title;
					var identifier = item.Identifier;
					Console.WriteLine($"{identifier}: {title}");
					//var detail = MyJvnApi.getVulnDetailInfo(identifier.Value).Result;
					//Console.WriteLine(detail);
					//Thread.Sleep(100);
				}
				var status = ol.Status;
				var retCd = status.RetCd;
				if (retCd != "0") {
					break;
				}
				if (int.TryParse(status.TotalRes, out int totalCount)) {
					if (int.TryParse(status.TotalResRet, out int count)) {
						startItem += count;
						if (totalCount < startItem) {
							break;
						}
					} else {
						break;
					}
				} else {
					break;
				}
			}

			//foreach (var id in JvnIDs) {
			//	GetJvnInfo(id);
			//}
		}

		///// <summary>
		///// 指定URLページ内からのリンクを収集する
		///// </summary>
		///// <param name="urlID">指定URLのID</param>
		///// <param name="url">ホームページのURL</param>
		//static async void CollectUrls(int urlID, string url) {
		//	await Sem.WaitAsync();
		//	try {
		//		var uri = new Uri(url);
		//		var scheme = uri.Scheme;
		//		var host = uri.Host;
		//		var root = string.Concat(scheme, "://", host);
		//		var xmlstr = await MyJvnApi.GetStringAsync(uri.ToString());

		//		var doc = new HtmlDocument();
		//		doc.LoadHtml(xmlstr);

		//		var nodes = doc.DocumentNode.SelectNodes("//a[@href]");
		//		if (nodes != null) {
		//			foreach (var node in nodes) {
		//				foreach (var a in node.Attributes) {
		//					var value = a.Value;
		//					string detectedUrl = null;
		//					if (value.StartsWith("http")) {
		//						detectedUrl = value;
		//					} else if (value.StartsWith("/")) {
		//						detectedUrl = root + value;
		//					}
		//					if (detectedUrl != null) {
		//						var newUrlID = RegisterUrl(urlID, xmlstr, detectedUrl);
		//						if(newUrlID != 0) {
		//							CollectUrls(newUrlID, detectedUrl);
		//						}
		//					}
		//				}
		//			}
		//		}
		//	} finally {
		//		Sem.Release();
		//	}
		//}

		//static int RegisterUrl(int sourceUrlID, string sourceUrlContent, string url) {
		//	int id = 0;
		//	lock (Command) {
		//		using (var reader = RegisterUrlCommand.Execute(Command, url)) {
		//			foreach (var urlID in reader.Records) {
		//				id = urlID;
		//			}
		//		}
		//		if (id != 0) {
		//			RegisterContent.Execute(Command, sourceUrlID, sourceUrlContent);
		//			RegisterUrlLinkCommand.Execute(Command, sourceUrlID, id);
		//		}
		//	}
		//	return id;
		//}

		//static async void GetData() {
		//	var doc = new HtmlDocument();
		//	//var uri = new Uri("https://helpx.adobe.com/jp/security/products/acrobat/apsb18-02.html");
		//	//var uri = new Uri("http://www.adobe.com/jp/support/downloads/product.jsp?product=1&amp;platform=Windows");
		//	var uri = new Uri("http://supportdownloads.adobe.com/product.jsp?product=1&amp;amp;platform=Windows");
		//	var scheme = uri.Scheme;
		//	var host = uri.Host;
		//	var root = string.Concat(scheme, "://", host);
		//	var xmlstr = await MyJvnApi.GetStringAsync(uri.ToString());
		//	var urls = new List<string>();
		//	doc.LoadHtml(xmlstr);
		//	foreach (var node in doc.DocumentNode.SelectNodes("//a[@href]")) {
		//		foreach (var a in node.Attributes) {
		//			var value = a.Value;
		//			string url = null;
		//			if (value.StartsWith("http")) {
		//				url = value;
		//			} else if (value.StartsWith("/")) {
		//				url = root + value;
		//			}
		//			if (url != null) {
		//				//lock (AllUrls) {
		//				//	AllUrls.Add(url);
		//				//}
		//			}
		//		}
		//	}
		//	Console.WriteLine(doc);

		//	//var xdoc = XDocument.Parse(await MyJvnApi.getVulnDetailInfo("JVNDB-2018-002115"));
		//	////var xdoc = XDocument.Parse(await MyJvnApi.getVulnDetailInfo("JVNDB-2018-999999"));
		//	////Console.WriteLine(xdoc);
		//	//foreach (var e in xdoc.XPathSelectElements("/vuldef:VULDEF-Document/vuldef:Vulinfo", NamespaceManager)) {
		//	//	Console.WriteLine(e);
		//	//}
		//}
	}
}
