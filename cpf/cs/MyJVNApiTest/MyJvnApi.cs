using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;


namespace MyJVNApiTest {
	/// <summary>
	/// MyJVN APIアクセス用クラス
	/// </summary>
	public static class MyJvnApi {
		static XmlNamespaceManager _NamespaceManager;

		/// <summary>
		/// 応答XML用のネームスペースマネージャ
		/// </summary>
		public static XmlNamespaceManager NamespaceManager {
			get {
				if (_NamespaceManager == null) {
					_NamespaceManager = new XmlNamespaceManager(new NameTable());
					_NamespaceManager.AddNamespace("dc", "http://purl.org/dc/elements/1.1/");
					_NamespaceManager.AddNamespace("dcterms", "http://purl.org/dc/terms/");
					_NamespaceManager.AddNamespace("marking", "http://data-marking.mitre.org/Marking-1");
					_NamespaceManager.AddNamespace("rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
					_NamespaceManager.AddNamespace("rss", "http://purl.org/rss/1.0/");
					_NamespaceManager.AddNamespace("sec", "http://jvn.jp/rss/mod_sec/3.0/");
					_NamespaceManager.AddNamespace("status", "http://jvndb.jvn.jp/myjvn/Status");
					_NamespaceManager.AddNamespace("status", "http://jvndb.jvn.jp/myjvn/Status");
					_NamespaceManager.AddNamespace("tlpMarking", "http://data-marking.mitre.org/extensions/MarkingStructure#TLP-1");
					_NamespaceManager.AddNamespace("vuldef", "http://jvn.jp/vuldef/");
					_NamespaceManager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
					_NamespaceManager.AddNamespace("mjres", "http://jvndb.jvn.jp/myjvn/Results");
				}
				return _NamespaceManager;
			}
		}

		/// <summary>
		/// 年月日範囲
		/// </summary>
		public struct TimeRange {
			public static readonly TimeRange Empty = new TimeRange(DateTime.MinValue, DateTime.MinValue);

			public DateTime Start { get; set; }
			public DateTime End { get; set; }

			public TimeRange(DateTime start, DateTime end) {
				this.Start = start;
				this.End = end;
			}

			public override bool Equals(object obj) {
				if (obj is TimeRange) {
					return this == (TimeRange)obj;
				}
				return base.Equals(obj);
			}

			public override int GetHashCode() {
				return this.Start.GetHashCode() ^ this.Start.GetHashCode();
			}

			public static bool operator ==(TimeRange l, TimeRange r) {
				return l.Start == r.Start && l.End == r.End;
			}

			public static bool operator !=(TimeRange l, TimeRange r) {
				return l.Start != r.Start || l.End != r.End;
			}
		}

		/// <summary>
		/// レスポンス内のステータス
		/// </summary>
		public class Status {
			public XElement Element { get; private set; }

			public string Version {
				get {
					var e = this.Element.Attribute("version");
					return e != null ? e.Value : throw new ApplicationException("応答の status に version が存在しません。");
				}
			}

			public string RetCd {
				get {
					var e = this.Element.Attribute("retCd");
					return e != null ? e.Value : throw new ApplicationException("応答の status に retCd が存在しません。");
				}
			}
			public string TotalRes {
				get {
					var e = this.Element.Attribute("totalRes");
					return e != null ? e.Value : throw new ApplicationException("応答の status に totalRes が存在しません。");
				}
			}
			public string TotalResRet {
				get {
					var e = this.Element.Attribute("totalResRet");
					return e != null ? e.Value : throw new ApplicationException("応答の status に totalResRet が存在しません。");
				}
			}

			public Status(XElement element) {
				if (element == null) {
					throw new ApplicationException("応答の Status が null です。");
				}
				this.Element = element;
			}
		}

		/// <summary>
		/// 脆弱性対策概要情報
		/// </summary>
		public class VulnOverviewList {
			public XDocument Doc { get; private set; }

			/// <summary>
			/// ステータス
			/// </summary>
			public Status Status => new Status(this.Doc.XPathSelectElement("//status:Status", NamespaceManager));

			/// <summary>
			/// 脆弱性対策概要情報一覧
			/// </summary>
			public IEnumerable<VulnOverviewItem> Items {
				get {
					foreach (var item in this.Doc.XPathSelectElements("/rdf:RDF/rss:item", NamespaceManager)) {
						yield return new VulnOverviewItem(item);
					}
				}
			}

			public VulnOverviewList(XDocument doc) {
				this.Doc = doc;
			}
		}

		/// <summary>
		/// ベンダ一覧
		/// </summary>
		public class VendorList {
			public XDocument Doc { get; private set; }

			/// <summary>
			/// ステータス
			/// </summary>
			public Status Status => new Status(this.Doc.XPathSelectElement("/mjres:Result/status:Status", NamespaceManager));

			/// <summary>
			/// ベンダ戒情報一覧
			/// </summary>
			public IEnumerable<VendorInfo> Items {
				get {
					foreach (var item in this.Doc.XPathSelectElements("/mjres:Result/mjres:VendorInfo/mjres:Vendor", NamespaceManager)) {
						yield return new VendorInfo(item);
					}
				}
			}

			public VendorList(XDocument doc) {
				this.Doc = doc;
			}
		}

		/// <summary>
		/// ベンダ情報
		/// </summary>
		public class VendorInfo {
			public XElement Element { get; private set; }

			/// <summary>
			/// ベンダ名
			/// </summary>
			public string Name {
				get {
					var e = this.Element.Attribute("vname");
					return e != null ? e.Value : throw new ApplicationException("ベンダー情報に vname が存在しません。");
				}
			}

			/// <summary>
			/// CPE ベンダ名
			/// </summary>
			public string Cpe {
				get {
					var e = this.Element.Attribute("cpe");
					return e != null ? e.Value : throw new ApplicationException("ベンダー情報に cpe が存在しません。");
				}
			}

			/// <summary>
			/// ベンダID
			/// </summary>
			public string Id {
				get {
					var e = this.Element.Attribute("vid");
					return e != null ? e.Value : throw new ApplicationException("ベンダー情報に vid が存在しません。");
				}
			}

			public VendorInfo(XElement element) {
				this.Element = element;
			}
		}

		/// <summary>
		/// 注意警戒情報
		/// </summary>
		public class VulnOverviewItem {
			public XElement Element { get; private set; }

			/// <summary>
			/// 注意警戒のタイトル
			/// </summary>
			public string Title {
				get {
					var e = this.Element.XPathSelectElement("./rss:title", NamespaceManager);
					return e != null ? e.Value : throw new ApplicationException("getVulnOverviewList の応答アイテムに title が存在しないものがありました。");
				}
			}

			/// <summary>
			/// 注意警戒の識別子
			/// </summary>
			public string Identifier {
				get {
					var e = this.Element.XPathSelectElement("./sec:identifier", NamespaceManager);
					return e != null ? e.Value : throw new ApplicationException("getVulnOverviewList の応答アイテムに identifier が存在しないものがありました。");
				}
			}

			public VulnOverviewItem(XElement element) {
				if (element == null) {
					throw new ApplicationException("応答の item が null です。");
				}
				this.Element = element;
			}
		}

		public static async Task<string> GetStringAsync(string url) {
			using (HttpClient client = new HttpClient())
			using (var res = await client.GetAsync(url))
			using (var content = res.Content) {
				return await content.ReadAsStringAsync();
			}
		}


		/// <summary>
		/// 脆弱性対策概要情報一覧の取得
		/// </summary>
		/// <param name="startItem">エントリ開始位置、1～応答エントリ数</param>
		/// <param name="publicDateRange">発見年月日範囲または<see cref="TimeRange.Empty"/></param>
		/// <param name="publishedDateRange">更新年月日範囲または<see cref="TimeRange.Empty"/></param>
		/// <param name="firstPublishDateRange">発行年月日範囲または<see cref="TimeRange.Empty"/></param>
		/// <param name="lang">表示言語 、ja:日本語、en:英語</param>
		/// <returns>XML文字列</returns>
		public static async Task<VulnOverviewList> getVulnOverviewList(int startItem = 0, TimeRange publicDateRange = new TimeRange(), TimeRange publishedDateRange = new TimeRange(), TimeRange firstPublishDateRange = new TimeRange(), string lang = "ja") {
			var result = await getVulnOverviewListAsString(startItem, publicDateRange, publishedDateRange, firstPublishDateRange, lang);
			return new VulnOverviewList(XDocument.Parse(result));
		}

		/// <summary>
		/// ベンダ一覧の取得
		/// </summary>
		/// <param name="startItem">エントリ開始位置、1～応答エントリ数</param>
		/// <param name="maxCountItem">エントリ取得件数、1～10,000</param>
		/// <param name="lang">表示言語 、ja:日本語、en:英語</param>
		/// <returns>XML文字列</returns>
		public static async Task<VendorList> getVendorList(int startItem = 0, int maxCountItem = 0, string lang = "ja") {
			var result = await getVendorListAsString(startItem, maxCountItem, lang);
			return new VendorList(XDocument.Parse(result));
		}

		/// <summary>
		/// 脆弱性対策概要情報一覧の取得
		/// </summary>
		/// <param name="startItem">エントリ開始位置、1～応答エントリ数</param>
		/// <param name="publicDateRange">発見年月日範囲または<see cref="TimeRange.Empty"/></param>
		/// <param name="publishedDateRange">更新年月日範囲または<see cref="TimeRange.Empty"/></param>
		/// <param name="firstPublishDateRange">発行年月日範囲または<see cref="TimeRange.Empty"/></param>
		/// <param name="lang">表示言語 、ja:日本語、en:英語</param>
		/// <returns>XML文字列</returns>
		public static async Task<string> getVulnOverviewListAsString(int startItem = 0, TimeRange publicDateRange = new TimeRange(), TimeRange publishedDateRange = new TimeRange(), TimeRange firstPublishDateRange = new TimeRange(), string lang = "ja") {
			var sb = new StringBuilder("https://jvndb.jvn.jp/myjvn?method=getVulnOverviewList&feed=hnd");
			sb.Append("&rangeDatePublic=n");
			sb.Append("&rangeDatePublished=n");
			sb.Append("&rangeDateFirstPublished=n");
			if (startItem != 0) {
				sb.Append("&startItem=" + startItem);
			}
			if (publicDateRange != TimeRange.Empty) {
				sb.Append("&datePublicStartY=" + publicDateRange.Start.Year);
				sb.Append("&datePublicStartM=" + publicDateRange.Start.Month);
				sb.Append("&datePublicStartD=" + publicDateRange.Start.Day);
				sb.Append("&datePublicEndY=" + publicDateRange.End.Year);
				sb.Append("&datePublicEndM=" + publicDateRange.End.Month);
				sb.Append("&datePublicEndD=" + publicDateRange.End.Day);
			}
			if (publishedDateRange != TimeRange.Empty) {
				sb.Append("&datePublishedStartY=" + publishedDateRange.Start.Year);
				sb.Append("&datePublishedStartM=" + publishedDateRange.Start.Month);
				sb.Append("&datePublishedStartD=" + publishedDateRange.Start.Day);
				sb.Append("&datePublishedEndY=" + publishedDateRange.End.Year);
				sb.Append("&datePublishedEndM=" + publishedDateRange.End.Month);
				sb.Append("&datePublishedEndD=" + publishedDateRange.End.Day);
			}
			if (firstPublishDateRange != TimeRange.Empty) {
				sb.Append("&dateFirstPublishedStartY=" + firstPublishDateRange.Start.Year);
				sb.Append("&dateFirstPublishedStartM=" + firstPublishDateRange.Start.Month);
				sb.Append("&dateFirstPublishedStartD=" + firstPublishDateRange.Start.Day);
				sb.Append("&dateFirstPublishedEndY=" + firstPublishDateRange.End.Year);
				sb.Append("&dateFirstPublishedEndM=" + firstPublishDateRange.End.Month);
				sb.Append("&dateFirstPublishedEndD=" + firstPublishDateRange.End.Day);
			}
			sb.Append("&lang=" + lang);

			using (HttpClient client = new HttpClient())
			using (var res = await client.GetAsync(sb.ToString()))
			using (var content = res.Content) {
				return await content.ReadAsStringAsync();
			}
		}

		/// <summary>
		/// フィルタリング条件に当てはまる脆弱性対策の詳細情報を取得します。
		/// </summary>
		/// <param name="vulnId">脆弱性対策情報ID</param>
		/// <param name="lang">表示言語 、ja:日本語、en:英語</param>
		/// <returns>XML文字列</returns>
		public static async Task<string> getVulnDetailInfoAsString(string vulnId, string lang = "ja") {
			var sb = new StringBuilder("https://jvndb.jvn.jp/myjvn?method=getVulnDetailInfo&feed=hnd");
			sb.Append("&vulnId=" + vulnId);
			sb.Append("&lang=" + lang);

			using (HttpClient client = new HttpClient())
			using (var res = await client.GetAsync(sb.ToString()))
			using (var content = res.Content) {
				return await content.ReadAsStringAsync();
			}
		}

		/// <summary>
		/// ベンダ一覧の取得
		/// </summary>
		/// <param name="startItem">エントリ開始位置、1～応答エントリ数</param>
		/// <param name="maxCountItem">エントリ取得件数、1～10,000</param>
		/// <param name="lang">表示言語 、ja:日本語、en:英語</param>
		/// <returns>XML文字列</returns>
		public static async Task<string> getVendorListAsString(int startItem = 0, int maxCountItem = 0, string lang = "ja") {
			var sb = new StringBuilder("https://jvndb.jvn.jp/myjvn?method=getVendorList&feed=hnd");
			if (startItem != 0) {
				sb.Append("&startItem=" + startItem);
			}
			if (maxCountItem != 0) {
				sb.Append("&maxCountItem=" + maxCountItem);
			}
			sb.Append("&lang=" + lang);

			using (HttpClient client = new HttpClient())
			using (var res = await client.GetAsync(sb.ToString()))
			using (var content = res.Content) {
				return await content.ReadAsStringAsync();
			}
		}
	}
}
