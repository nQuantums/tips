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
			"JVNDB-2018-004518",
			"JVNDB-2018-004519",
			"JVNDB-2016-009023",
			"JVNDB-2016-009024",
			"JVNDB-2016-009025",
			"JVNDB-2013-006803",
			"JVNDB-2018-004520",
			"JVNDB-2018-004521",
			"JVNDB-2018-004522",
			"JVNDB-2018-004523",
			"JVNDB-2014-008613",
			"JVNDB-2014-008614",
			"JVNDB-2017-013348",
			"JVNDB-2017-013349",
			"JVNDB-2017-013350",
			"JVNDB-2017-013351",
			"JVNDB-2016-009026",
			"JVNDB-2018-004524",
			"JVNDB-2018-004525",
			"JVNDB-2018-004526",
			"JVNDB-2018-004527",
			"JVNDB-2018-004528",
			"JVNDB-2018-004529",
			"JVNDB-2018-004530",
			"JVNDB-2018-004531",
			"JVNDB-2018-004532",
			"JVNDB-2018-004533",
			"JVNDB-2018-004534",
			"JVNDB-2018-004535",
			"JVNDB-2017-013352",
			"JVNDB-2017-013353",
			"JVNDB-2017-013354",
			"JVNDB-2018-004536",
			"JVNDB-2018-004537",
			"JVNDB-2018-004538",
			"JVNDB-2018-004539",
			"JVNDB-2018-004540",
			"JVNDB-2018-004541",
			"JVNDB-2017-013355",
			"JVNDB-2017-013356",
			"JVNDB-2017-013357",
			"JVNDB-2017-013358",
			"JVNDB-2018-004542",
			"JVNDB-2018-004543",
			"JVNDB-2018-004544",
			"JVNDB-2018-004545",
			"JVNDB-2018-004546",
			"JVNDB-2018-004547",
			"JVNDB-2018-004548",
			"JVNDB-2018-004549",
			"JVNDB-2018-004550",
			"JVNDB-2016-009027",
			"JVNDB-2016-009028",
			"JVNDB-2018-004551",
			"JVNDB-2018-004552",
			"JVNDB-2018-004553",
			"JVNDB-2018-004554",
			"JVNDB-2018-004555",
			"JVNDB-2018-004556",
			"JVNDB-2018-004557",
			"JVNDB-2017-013359",
			"JVNDB-2017-013360",
			"JVNDB-2018-004558",
			"JVNDB-2018-004559",
			"JVNDB-2018-004560",
			"JVNDB-2018-004561",
			"JVNDB-2018-004562",
			"JVNDB-2018-004563",
			"JVNDB-2018-004564",
			"JVNDB-2018-004565",
			"JVNDB-2018-004566",
			"JVNDB-2018-004567",
			"JVNDB-2018-004568",
			"JVNDB-2016-009029",
			"JVNDB-2018-004569",
			"JVNDB-2018-004570",
			"JVNDB-2018-004571",
			"JVNDB-2018-004572",
			"JVNDB-2018-004573",
			"JVNDB-2018-004574",
			"JVNDB-2018-004575",
			"JVNDB-2018-004576",
			"JVNDB-2012-006408",
			"JVNDB-2011-005420",
			"JVNDB-2017-013361",
			"JVNDB-2018-004577",
			"JVNDB-2018-004578",
			"JVNDB-2018-004579",
			"JVNDB-2018-004580",
			"JVNDB-2018-004581",
			"JVNDB-2018-004582",
			"JVNDB-2014-008615",
			"JVNDB-2018-004583",
			"JVNDB-2018-004584",
			"JVNDB-2018-004585",
			"JVNDB-2013-006804",
			"JVNDB-2018-004586",
			"JVNDB-2018-004587",
			"JVNDB-2018-004588",
			"JVNDB-2018-004589",
			"JVNDB-2018-004590",
			"JVNDB-2018-004591",
			"JVNDB-2018-004592",
			"JVNDB-2018-004593",
			"JVNDB-2013-006805",
			"JVNDB-2018-004594",
			"JVNDB-2018-004595",
			"JVNDB-2018-004596",
			"JVNDB-2018-004597",
			"JVNDB-2018-004598",
			"JVNDB-2018-004599",
			"JVNDB-2018-004600",
			"JVNDB-2018-004601",
			"JVNDB-2018-004602",
			"JVNDB-2018-004603",
			"JVNDB-2018-004604",
			"JVNDB-2018-004605",
			"JVNDB-2018-004606",
			"JVNDB-2018-004607",
			"JVNDB-2018-004608",
			"JVNDB-2014-008616",
			"JVNDB-2018-004609",
			"JVNDB-2018-004610",
			"JVNDB-2013-006806",
			"JVNDB-2014-008617",
			"JVNDB-2014-008618",
			"JVNDB-2018-004611",
			"JVNDB-2018-004612",
			"JVNDB-2018-004613",
			"JVNDB-2018-004614",
			"JVNDB-2018-004615",
			"JVNDB-2018-004616",
			"JVNDB-2018-004617",
			"JVNDB-2018-004618",
			"JVNDB-2018-004619",
			"JVNDB-2018-004620",
			"JVNDB-2018-004621",
			"JVNDB-2018-004622",
			"JVNDB-2018-004623",
			"JVNDB-2018-004624",
			"JVNDB-2018-004625",
			"JVNDB-2018-004626",
			"JVNDB-2018-004627",
			"JVNDB-2018-004628",
			"JVNDB-2018-004629",
			"JVNDB-2018-004630",
			"JVNDB-2018-004631",
			"JVNDB-2018-004632",
			"JVNDB-2018-004633",
			"JVNDB-2018-004634",
			"JVNDB-2018-004635",
			"JVNDB-2018-004636",
			"JVNDB-2018-004637",
			"JVNDB-2018-004638",
			"JVNDB-2018-004639",
			"JVNDB-2018-004640",
			"JVNDB-2018-004641",
			"JVNDB-2018-004642",
			"JVNDB-2018-004643",
			"JVNDB-2018-004644",
			"JVNDB-2018-004645",
			"JVNDB-2018-004646",
			"JVNDB-2018-004647",
			"JVNDB-2018-004648",
			"JVNDB-2018-004649",
			"JVNDB-2018-004650",
			"JVNDB-2018-004651",
			"JVNDB-2018-004652",
			"JVNDB-2018-004653",
			"JVNDB-2018-004654",
			"JVNDB-2018-004655",
			"JVNDB-2018-004656",
			"JVNDB-2018-004657",
			"JVNDB-2018-004658",
			"JVNDB-2018-004659",
			"JVNDB-2018-004660",
			"JVNDB-2018-004661",
			"JVNDB-2018-004662",
			"JVNDB-2018-004663",
			"JVNDB-2018-004664",
			"JVNDB-2018-004665",
			"JVNDB-2018-004666",
			"JVNDB-2018-004667",
			"JVNDB-2018-004668",
			"JVNDB-2018-004669",
			"JVNDB-2018-004670",
			"JVNDB-2018-004671",
			"JVNDB-2018-004672",
			"JVNDB-2018-004673",
			"JVNDB-2018-004674",
			"JVNDB-2018-004675",
			"JVNDB-2018-004676",
			"JVNDB-2018-004677",
			"JVNDB-2018-004678",
			"JVNDB-2018-004679",
			"JVNDB-2018-004680",
			"JVNDB-2018-004681",
			"JVNDB-2018-004682",
			"JVNDB-2018-004683",
			"JVNDB-2018-004684",
			"JVNDB-2018-004685",
			"JVNDB-2018-004686",
			"JVNDB-2018-004687",
			"JVNDB-2018-004688",
			"JVNDB-2018-004689",
			"JVNDB-2018-004690",
			"JVNDB-2018-004691",
			"JVNDB-2018-004692",
			"JVNDB-2018-004693",
			"JVNDB-2018-004694",
			"JVNDB-2018-004695",
			"JVNDB-2018-004696",
			"JVNDB-2018-004697",
			"JVNDB-2018-004698",
			"JVNDB-2018-004699",
			"JVNDB-2018-004700",
			"JVNDB-2018-004701",
			"JVNDB-2018-004702",
			"JVNDB-2018-004703",
			"JVNDB-2018-004704",
			"JVNDB-2018-004705",
			"JVNDB-2018-004706",
			"JVNDB-2018-004707",
			"JVNDB-2018-004708",
			"JVNDB-2018-004709",
			"JVNDB-2018-004710",
			"JVNDB-2018-004711",
			"JVNDB-2018-004712",
			"JVNDB-2018-004713",
			"JVNDB-2018-004714",
			"JVNDB-2018-004715",
			"JVNDB-2018-004716",
			"JVNDB-2018-004717",
			"JVNDB-2018-004718",
			"JVNDB-2018-004719",
			"JVNDB-2018-004720",
			"JVNDB-2018-004721",
			"JVNDB-2018-004722",
			"JVNDB-2018-004723",
			"JVNDB-2018-004724",
			"JVNDB-2018-004725",
			"JVNDB-2018-004726",
			"JVNDB-2018-004727",
			"JVNDB-2018-004728",
			"JVNDB-2018-004729",
			"JVNDB-2018-004730",
			"JVNDB-2018-004731",
			"JVNDB-2018-004732",
			"JVNDB-2018-004733",
			"JVNDB-2018-004734",
			"JVNDB-2018-004735",
			"JVNDB-2018-004736",
			"JVNDB-2018-004737",
			"JVNDB-2018-004738",
			"JVNDB-2018-004739",
			"JVNDB-2018-004740",
			"JVNDB-2018-004741",
			"JVNDB-2018-004742",
			"JVNDB-2018-004743",
			"JVNDB-2018-004744",
			"JVNDB-2018-004745",
			"JVNDB-2018-004746",
			"JVNDB-2018-004747",
			"JVNDB-2018-004748",
			"JVNDB-2018-004749",
			"JVNDB-2018-004750",
			"JVNDB-2018-004751",
			"JVNDB-2018-004752",
			"JVNDB-2018-004753",
			"JVNDB-2018-004754",
			"JVNDB-2018-004755",
			"JVNDB-2018-004756",
			"JVNDB-2018-004757",
			"JVNDB-2018-004758",
			"JVNDB-2018-004759",
			"JVNDB-2018-004760",
			"JVNDB-2018-004761",
			"JVNDB-2018-004762",
			"JVNDB-2017-013362",
			"JVNDB-2018-004763",
			"JVNDB-2018-004764",
			"JVNDB-2018-004765",
			"JVNDB-2018-004766",
			"JVNDB-2018-004767",
			"JVNDB-2018-004768",
			"JVNDB-2018-004769",
			"JVNDB-2018-000066",
			"JVNDB-2018-004770",
			"JVNDB-2018-004771",
			"JVNDB-2018-004772",
			"JVNDB-2018-004773",
			"JVNDB-2018-004774",
			"JVNDB-2018-004775",
			"JVNDB-2018-004776",
			"JVNDB-2018-004777",
			"JVNDB-2018-004778",
			"JVNDB-2018-004779",
			"JVNDB-2018-004780",
			"JVNDB-2018-004781",
			"JVNDB-2015-008189",
			"JVNDB-2017-013363",
			"JVNDB-2017-013364",
			"JVNDB-2018-004782",
			"JVNDB-2018-004783",
			"JVNDB-2018-004784",
			"JVNDB-2017-013365",
			"JVNDB-2017-013366",
			"JVNDB-2018-004785",
			"JVNDB-2018-004786",
			"JVNDB-2018-004787",
			"JVNDB-2018-004788",
			"JVNDB-2018-004789",
			"JVNDB-2018-004790",
			"JVNDB-2015-008190",
			"JVNDB-2018-004791",
			"JVNDB-2018-004792",
			"JVNDB-2018-004793",
			"JVNDB-2018-004794",
			"JVNDB-2017-013367",
			"JVNDB-2017-013368",
			"JVNDB-2018-004795",
			"JVNDB-2018-004796",
			"JVNDB-2018-004797",
			"JVNDB-2018-004798",
			"JVNDB-2018-004799",
			"JVNDB-2017-013369",
			"JVNDB-2017-013370",
			"JVNDB-2017-013371",
			"JVNDB-2013-006807",
			"JVNDB-2018-004800",
			"JVNDB-2018-004801",
			"JVNDB-2018-004802",
			"JVNDB-2018-004803",
			"JVNDB-2018-004804",
			"JVNDB-2018-004805",
			"JVNDB-2018-000067",
			"JVNDB-2018-004806",
			"JVNDB-2018-004807",
			"JVNDB-2018-004808",
			"JVNDB-2018-004809",
			"JVNDB-2017-013372",
			"JVNDB-2017-013373",
			"JVNDB-2017-013374",
			"JVNDB-2017-013375",
			"JVNDB-2017-013376",
			"JVNDB-2017-013377",
			"JVNDB-2017-013378",
			"JVNDB-2017-013379",
			"JVNDB-2018-004810",
			"JVNDB-2018-004811",
			"JVNDB-2018-004812",
			"JVNDB-2018-004813",
			"JVNDB-2018-004814",
			"JVNDB-2018-004815",
			"JVNDB-2018-004816",
			"JVNDB-2018-004817",
			"JVNDB-2018-004818",
			"JVNDB-2018-004819",
			"JVNDB-2018-004820",
			"JVNDB-2018-004821",
			"JVNDB-2018-004822",
			"JVNDB-2018-004823",
			"JVNDB-2018-004824",
			"JVNDB-2018-004825",
			"JVNDB-2018-004826",
			"JVNDB-2018-004827",
			"JVNDB-2018-004828",
			"JVNDB-2018-004829",
			"JVNDB-2018-004830",
			"JVNDB-2018-004831",
			"JVNDB-2018-004832",
			"JVNDB-2018-004833",
			"JVNDB-2018-004834",
			"JVNDB-2018-004835",
			"JVNDB-2018-004836",
			"JVNDB-2018-004837",
			"JVNDB-2018-004838",
			"JVNDB-2018-004839",
			"JVNDB-2018-004840",
			"JVNDB-2018-004841",
			"JVNDB-2018-004842",
			"JVNDB-2018-004843",
			"JVNDB-2018-004844",
			"JVNDB-2018-004845",
			"JVNDB-2018-004846",
			"JVNDB-2018-004847",
			"JVNDB-2018-004848",
			"JVNDB-2018-004849",
			"JVNDB-2018-004850",
			"JVNDB-2018-004851",
			"JVNDB-2018-004852",
			"JVNDB-2018-004853",
			"JVNDB-2018-004854",
			"JVNDB-2018-004855",
			"JVNDB-2018-004856",
			"JVNDB-2018-004857",
			"JVNDB-2018-004858",
			"JVNDB-2018-004859",
			"JVNDB-2017-013380",
		};

		static void GetJvnInfo(string jvnid) {
			var doc = XDocument.Parse(MyJvnApi.getVulnDetailInfo(jvnid).Result);
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

		static void Main(string[] args) {
			foreach (var jvndbid in JvnIDs) {
				GetJvnInfo(jvndbid);
				break;
			}
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
