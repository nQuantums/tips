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
using System.IO;
using HtmlAgilityPack;

namespace MyJVNApiTest {

	class Program {
		[DllImport("User32.dll")]
		private static extern short GetAsyncKeyState(int vKey);

		static SemaphoreSlim Sem = new SemaphoreSlim(10, 10);

		static string[] JvnIDs = {
			"JVNDB-2017-011116",
		};

		static void GetJvnInfo(string jvnid) {
			var doc = XDocument.Parse(MyJvnApi.getVulnDetailInfoAsString(jvnid).Result);
			Console.WriteLine(doc);
			return;
			var title = doc.XPathSelectElement("//vuldef:Title", MyJvnApi.NamespaceManager);
			Console.WriteLine($"# {jvnid} : {title.Value}");

			//Console.WriteLine(title.Value);
			foreach (var e in doc.XPathSelectElements("//vuldef:AffectedItem", MyJvnApi.NamespaceManager)) {
				//var vendorName = e.XPathSelectElement(".//vuldef:Name", MyJvnApi.NamespaceManager);
				var productName = e.XPathSelectElement(".//vuldef:ProductName", MyJvnApi.NamespaceManager);
				var versionNumbers = e.XPathSelectElements(".//vuldef:VersionNumber", MyJvnApi.NamespaceManager);
				Console.WriteLine($"## {productName.Value}");
				foreach (var version in versionNumbers) {
					Console.WriteLine($"- {version.Value}");
				}
				break;
				Console.Write("(");
				foreach (var v in versionNumbers) {
					Console.Write($"	{v.Value}");
				}
			}
		}

		static void WriteJvnDetail(string jvnid) {
			var doc = XDocument.Parse(MyJvnApi.getVulnDetailInfoAsString(jvnid).Result);
			File.WriteAllText(@"d:\work\myjvnapi_test\" + jvnid + ".xml", doc.ToString());
		}

		static void Main(string[] args) {
			WriteJvnDetail("JVNDB-2018-003396");

			//foreach (var jvndbid in JvnIDs) {
			//	GetJvnInfo(jvndbid);
			//	break;
			//}



			//var startItem = 1;
			//for (; ; ) {
			//	var vl = MyJvnApi.getVendorList(startItem).Result;

			//	foreach (var item in vl.Items) {
			//		Console.WriteLine(item.Name);
			//		Console.WriteLine(item.Cpe);
			//		Console.WriteLine(item.Id);
			//	}

			//	var status = vl.Status;
			//	var retCd = status.RetCd;
			//	if (retCd != "0") {
			//		break;
			//	}
			//	if (int.TryParse(status.TotalRes, out int totalCount)) {
			//		if (int.TryParse(status.TotalResRet, out int count)) {
			//			startItem += count;
			//			if (totalCount < startItem) {
			//				break;
			//			}
			//		} else {
			//			break;
			//		}
			//	} else {
			//		break;
			//	}
			//}

			//var start = new DateTime(2017, 1, 1);
			//var now = DateTime.Now;
			//var dateRange = new MyJvnApi.TimeRange(start, now);

			//var startItem = 1;

			//for(; ; ) {
			//	var ol = MyJvnApi.getVulnOverviewList(
			//		startItem: startItem,
			//		publicDateRange: dateRange,
			//		publishedDateRange: dateRange,
			//		firstPublishDateRange: dateRange
			//	).Result;

			//	foreach (var item in ol.Items) {
			//		var title = item.Title;
			//		var identifier = item.Identifier;
			//		Console.WriteLine($"{identifier}: {title}");
			//		//var detail = MyJvnApi.getVulnDetailInfo(identifier.Value).Result;
			//		//Console.WriteLine(detail);
			//		//Thread.Sleep(100);
			//	}
			//	var status = ol.Status;
			//	var retCd = status.RetCd;
			//	if (retCd != "0") {
			//		break;
			//	}
			//	if (int.TryParse(status.TotalRes, out int totalCount)) {
			//		if (int.TryParse(status.TotalResRet, out int count)) {
			//			startItem += count;
			//			if (totalCount < startItem) {
			//				break;
			//			}
			//		} else {
			//			break;
			//		}
			//	} else {
			//		break;
			//	}
			//}

			////foreach (var id in JvnIDs) {
			////	GetJvnInfo(id);
			////}
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
