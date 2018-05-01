﻿using System;
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
using CodeDb;
using CodeDb.PgBinder;

namespace MyJVNApiTest {
	public static class Db {
		public static PgEnvironment E { get; } = new PgEnvironment();

		public static class C {
			public static int KeywordID => E.Int32("keyword_id");
			public static int[] KeywordIDs => E.Int32Array("keyword_ids");
			public static string Keyword => E.String("keyword");
			public static int UrlID => E.Int32("url_id");
			public static int LinkUrlID => E.Int32("link_url_id");
			public static string Url => E.String("url");
			public static string Content => E.String("content");
		}

		public class TbKeyword : TableDef<TbKeyword.Cols> {
			public TbKeyword() : base(E, "tb_keyword") { }
			public class Cols : ColumnsBase {
				public int KeywordID => As(() => C.KeywordID, ColumnFlags.Serial);
				public string Keyword => As(() => C.Keyword);
			}
			public override IPrimaryKeyDef GetPrimaryKey() => MakePrimaryKey(() => _.KeywordID, () => _.Keyword);
			public override IEnumerable<IUniqueDef> GetUniques() => MakeUniques(MakeUnique(() => _.Keyword));
		}
		public static TbKeyword Keyword { get; } = new TbKeyword();

		public class TbUrl : TableDef<TbUrl.Cols> {
			public TbUrl() : base(E, "tb_url") { }
			public class Cols : ColumnsBase {
				public int UrlID => As(() => C.UrlID, ColumnFlags.Serial);
				public string Url => As(() => C.Url);
			}
			public override IPrimaryKeyDef GetPrimaryKey() => MakePrimaryKey(() => _.UrlID, () => _.Url);
			public override IEnumerable<IUniqueDef> GetUniques() => MakeUniques(MakeUnique(() => _.Url));
		}
		public static TbUrl Url { get; } = new TbUrl();

		public class TbContent : TableDef<TbContent.Cols> {
			public TbContent() : base(E, "tb_content") { }
			public class Cols : ColumnsBase {
				public int UrlID => As(() => C.UrlID);
				public string Content => As(() => C.Content);
			}
			public override IPrimaryKeyDef GetPrimaryKey() => MakePrimaryKey(() => _.UrlID);
		}
		public static TbContent Content { get; } = new TbContent();

		public class TbLink : TableDef<TbLink.Cols> {
			public TbLink() : base(E, "tb_link") { }
			public class Cols : ColumnsBase {
				public int UrlID => As(() => C.UrlID);
				public int LinkUrlID => As(() => C.LinkUrlID);
			}
			public override IPrimaryKeyDef GetPrimaryKey() => MakePrimaryKey(() => _.UrlID, () => _.LinkUrlID);
			public override IEnumerable<IIndexDef> GetIndices() => MakeIndices(MakeIndex(0, () => _.LinkUrlID));
		}
		public static TbLink Link { get; } = new TbLink();

		public class TbUrlKeyword : TableDef<TbUrlKeyword.Cols> {
			public TbUrlKeyword() : base(E, "tb_url_keyword") { }
			public class Cols : ColumnsBase {
				public int UrlID => As(() => C.UrlID);
				public int[] KeywordIDs => As(() => C.KeywordIDs);
			}
			public override IPrimaryKeyDef GetPrimaryKey() => MakePrimaryKey(() => _.UrlID);
			public override IEnumerable<IIndexDef> GetIndices() => MakeIndices(MakeIndex(IndexFlags.Gin, () => _.KeywordIDs));
		}
		public static TbUrlKeyword UrlKeyword { get; } = new TbUrlKeyword();
	}

	class Program {
		[DllImport("User32.dll")]
		private static extern short GetAsyncKeyState(int vKey);

		static SemaphoreSlim Sem = new SemaphoreSlim(10, 10);
		const string RoleName = "my_jvn_api_test";
		const string DbName = "my_jvn_api_test";
		const string Password = "Passw0rd!";

		static ICodeDbCommand Command;
		static FuncCmd<string, int> RegisterUrlCommand;
		static ActionCmd<int, string> RegisterContent;
		static ActionCmd<int, int> RegisterUrlLinkCommand;

		const string AddUrl = "add_url";

		static void Main(string[] args) {
			var E = Db.E;

			// とりあえずロールとデータベース作成
			using (var con = E.CreateConnection("User ID=sa;Password=Password;Host=localhost;Port=5432;Database=postgres;")) {
				con.Open();
				try {
					E.CreateRole(con, RoleName, "Passw0rd!");
				} catch (CodeDbEnvironmentException ex) {
					if (ex.ErrorType != DbEnvironmentErrorType.DuplicateObject) {
						throw;
					}
				}
				try {
					E.CreateDatabase(con, DbName, RoleName);
				} catch (CodeDbEnvironmentException ex) {
					if (ex.ErrorType != DbEnvironmentErrorType.DuplicateDatabase) {
						throw;
					}
				}
			}

			// データベースの構造をソース上での定義に合わせる
			using (var con = E.CreateConnection($"User ID={RoleName};Password='Passw0rd!';Host=localhost;Port=5432;Database={DbName};")) {
				con.Open();
				var cmd = con.CreateCommand();
				cmd.CommandTimeout = 0;

				{
					var dropSql = Db.E.NewSql();
					dropSql.DropTable(Db.Url);
					dropSql.Build().Execute(cmd);
				}

				// データベースの状態を取得
				var current = E.ReadDatabaseDef(con);
				// クラスからデータベース定義を生成する
				var target = E.GenerateDatabaseDef(typeof(Db), "test_db");
				// 差分を生成
				var delta = E.GetDatabaseDelta(current, target);

				// 差分を適用する
				var context = new ElementCode();
				E.ApplyDatabaseDelta(context, delta);
				context.Build().Execute(cmd);

				// URL追加ストアド作成
				{
					var code = new ElementCode();
					code.Concat($@"
DROP FUNCTION IF EXISTS {AddUrl}(TEXT);
CREATE OR REPLACE FUNCTION {AddUrl}(url_to_add TEXT)
RETURNS INT AS $$
	BEGIN
		INSERT INTO {Db.Url.Name}({Db.Url.ColumnName(nameof(Db.Url._.Url))}) VALUES (url_to_add);
		RETURN lastval();
	EXCEPTION WHEN SQLSTATE '23505' THEN
		RETURN 0;
	END;
$$ LANGUAGE plpgsql;
");
					var s = Db.E.NewSql();
					s.Code(code);
					s.Build().Execute(cmd);
				}

				// URL追加プログラム作成
				{
					var url = new Argument("");
					var code = new ElementCode();
					code.Add(SqlKeyword.Select);
					code.Concat(AddUrl);
					code.BeginParenthesize();
					code.Add(url);
					code.EndParenthesize();
					var s = Db.E.NewSql();
					s.Code(code);
					RegisterUrlCommand = s.BuildFunc<string, int>(url);
				}

				// 内容追加プログラム作成
				{
					var srcUrlID = new Argument(0);
					var contentText = new Argument("");
					var sql = Db.E.NewSql();
					sql.InsertIntoWithValueIfNotExists(Db.Content, t => new[] { t.UrlID == srcUrlID, t.Content == contentText }, 1);
					RegisterContent = sql.BuildAction<int, string>(srcUrlID, contentText);
				}

				// URLリンク追加プログラム作成
				{
					var srcUrlID = new Argument(0);
					var dstUrlID = new Argument(0);
					var sql = Db.E.NewSql();
					sql.InsertIntoWithValueIfNotExists(Db.Link, t => new [] { t.UrlID == srcUrlID, t.LinkUrlID == dstUrlID });
					RegisterUrlLinkCommand = sql.BuildAction<int, int>(srcUrlID, dstUrlID);
				}

				// 挿入のテスト
				Command = cmd;

				var urlID = RegisterUrl(0, "", "https://helpx.adobe.com/jp/security/products/acrobat/apsb18-02.html");

				CollectUrls(urlID, "https://helpx.adobe.com/jp/security/products/acrobat/apsb18-02.html");
				while (GetAsyncKeyState(0x0D) == 0) {
					Thread.Sleep(1000);
				}

				Console.ReadKey();
				return;
			}
		}

		/// <summary>
		/// 指定URLページ内からのリンクを収集する
		/// </summary>
		/// <param name="urlID">指定URLのID</param>
		/// <param name="url">ホームページのURL</param>
		static async void CollectUrls(int urlID, string url) {
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
								var newUrlID = RegisterUrl(urlID, xmlstr, detectedUrl);
								if(newUrlID != 0) {
									CollectUrls(newUrlID, detectedUrl);
								}
							}
						}
					}
				}
			} finally {
				Sem.Release();
			}
		}

		static int RegisterUrl(int sourceUrlID, string sourceUrlContent, string url) {
			int id = 0;
			lock (Command) {
				using (var reader = RegisterUrlCommand.Execute(Command, url)) {
					foreach (var urlID in reader.Records) {
						id = urlID;
					}
				}
				if (id != 0) {
					RegisterContent.Execute(Command, sourceUrlID, sourceUrlContent);
					RegisterUrlLinkCommand.Execute(Command, sourceUrlID, id);
				}
			}
			return id;
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
						//lock (AllUrls) {
						//	AllUrls.Add(url);
						//}
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
