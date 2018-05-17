﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DbCode;
using DbCode.PgBind;

namespace PatchCrawler {
	/// <summary>
	/// データベースの定義
	/// </summary>
	public static class Db {
		/// <summary>
		/// DB環境
		/// </summary>
		public static PgEnvironment E { get; } = new PgEnvironment();

		#region 列定義
		/// <summary>
		/// 列定義一覧
		/// </summary>
		public static class C {
			public static int UrlID => E.Int32("url_id");
			public static int SrcUrlID => E.Int32("src_url_id");
			public static int DstUrlID => E.Int32("dst_url_id");
			public static string Url => E.String("url");
			public static string UrlTitle => E.String("url_title");
			public static int KeywordID => E.Int32("keyword_id");
			public static int KeywordCount => E.Int32("keyword_count");
			public static string Keyword => E.String("keyword");
		}
		#endregion

		#region テーブル定義
		public class TbUrl : TableDef<TbUrl.D> {
			public TbUrl() : base(E, "tb_url") { }

			public class D : ColumnsBase {
				public int UrlID => As(() => C.UrlID, ColumnFlags.Serial);
				public string Url => As(() => C.Url);
			}

			public override IPrimaryKeyDef GetPrimaryKey() => MakePrimaryKey(() => _.UrlID);
			public override IEnumerable<IIndexDef> GetIndices() => MakeIndices(
				MakeIndex(0, () => _.Url)
			);
			public override IEnumerable<IUniqueDef> GetUniques() => MakeUniques(
				MakeUnique(() => _.Url)
			);
		}
		public static TbUrl Url { get; private set; } = new TbUrl();

		public class TbUrlTitle : TableDef<TbUrlTitle.D> {
			public TbUrlTitle() : base(E, "tb_url_title") { }

			public class D : ColumnsBase {
				public int UrlID => As(() => C.UrlID);
				public string UrlTitle => As(() => C.UrlTitle);
			}

			public override IPrimaryKeyDef GetPrimaryKey() => MakePrimaryKey(() => _.UrlID);
			public override IEnumerable<IIndexDef> GetIndices() => MakeIndices(
				MakeIndex(0, () => _.UrlTitle)
			);
		}
		public static TbUrlTitle UrlTitle { get; private set; } = new TbUrlTitle();

		public class TbKeyword : TableDef<TbKeyword.D> {
			public TbKeyword() : base(E, "tb_keyword") { }

			public class D : ColumnsBase {
				public int KeywordID => As(() => C.KeywordID, ColumnFlags.Serial);
				public string Keyword => As(() => C.Keyword);
			}

			public override IPrimaryKeyDef GetPrimaryKey() => MakePrimaryKey(() => _.KeywordID);
			public override IEnumerable<IIndexDef> GetIndices() => MakeIndices(
				MakeIndex(0, () => _.Keyword)
			);
			public override IEnumerable<IUniqueDef> GetUniques() => MakeUniques(
				MakeUnique(() => _.Keyword)
			);
		}
		public static TbKeyword Keyword { get; private set; } = new TbKeyword();

		public class TbUrlKeyword : TableDef<TbUrlKeyword.D> {
			public TbUrlKeyword() : base(E, "tb_url_keyword") { }

			public class D : ColumnsBase {
				public int UrlID => As(() => C.UrlID);
				public int KeywordID => As(() => C.KeywordID);
				public int KeywordCount => As(() => C.KeywordCount);
			}

			public override IPrimaryKeyDef GetPrimaryKey() => MakePrimaryKey(() => _.UrlID, () => _.KeywordID);
			public override IEnumerable<IIndexDef> GetIndices() => MakeIndices(
				MakeIndex(0, () => _.KeywordID)
			);
		}
		public static TbUrlKeyword UrlKeyword { get; private set; } = new TbUrlKeyword();

		public class TbUrlLink : TableDef<TbUrlLink.D> {
			public TbUrlLink() : base(E, "tb_url_link") { }

			public class D : ColumnsBase {
				public int SrcUrlID => As(() => C.SrcUrlID);
				public int DstUrlID => As(() => C.DstUrlID);
			}

			public override IPrimaryKeyDef GetPrimaryKey() => MakePrimaryKey(() => _.SrcUrlID, () => _.DstUrlID);
			public override IEnumerable<IIndexDef> GetIndices() => MakeIndices(
				MakeIndex(0, () => _.DstUrlID)
			);
		}
		public static TbUrlLink UrlLink { get; private set; } = new TbUrlLink();
		#endregion

		#region フィールド
		const string RoleName = "patch_crawler";
		const string DbName = "patch_crawler";
		const string SpAddUrl = "add_url";
		const string SpAddKeyword = "add_keyword";
		#endregion

		#region 公開メソッド
		public static IDbCodeConnection CreateConnection() {
			return E.CreateConnection($"User ID={RoleName};Password='Passw0rd!';Host=localhost;Port=5432;Database={DbName};");
		}

		public static void Initialize() {
			// ロールとデータベースを作成する
			using (var con = E.CreateConnection("User ID=sa;Password='Passw0rd!';Host=localhost;Port=5432;Database=postgres;")) {
				con.Open();

				try {
					E.CreateRole(con, RoleName, "Passw0rd!");
				} catch (DbCodeEnvironmentException ex) {
					if (ex.ErrorType != DbEnvironmentErrorType.DuplicateObject) {
						throw;
					}
				}
				try {
					E.CreateDatabase(con, DbName, RoleName);
				} catch (DbCodeEnvironmentException ex) {
					if (ex.ErrorType != DbEnvironmentErrorType.DuplicateDatabase) {
						throw;
					}
				}
			}

			// データベース内のテーブル等を最新の仕様に合わせて変更する
			using (var con = CreateConnection()) {
				con.Open();
				var cmd = con.CreateCommand();
				cmd.CommandTimeout = 0;

				// データベースの状態を取得
				var current = E.ReadDatabaseDef(con);
				// クラスからデータベース定義を生成する
				var target = E.GenerateDatabaseDef(typeof(Db), DbName);
				// 差分を生成
				var delta = E.GetDatabaseDelta(current, target);

				// 差分を適用する
				var context = new ElementCode();
				E.ApplyDatabaseDelta(context, delta);
				context.Build().Execute(cmd);

				// URL登録ストアドを登録
				cmd.CommandText = $@"
DROP FUNCTION IF EXISTS {SpAddUrl}(TEXT);
CREATE OR REPLACE FUNCTION {SpAddUrl}(url_to_add TEXT)
RETURNS INT AS $$
	DECLARE
		id INT := 0;
	BEGIN
		SELECT {Db.Url.ColumnName(nameof(Db.Url._.UrlID))} INTO id FROM {Db.Url.Name} WHERE {Db.Url.ColumnName(nameof(Db.Url._.Url))}=url_to_add;
		IF id <> 0 THEN
			RETURN id;
		ELSE
			INSERT INTO {Db.Url.Name}({Db.Url.ColumnName(nameof(Db.Url._.Url))}) VALUES (url_to_add);
			RETURN lastval();
		END IF;
	END;
$$ LANGUAGE plpgsql;
";
				cmd.ExecuteNonQuery();

				// キーワード登録ストアドを登録
				cmd.CommandText = $@"
DROP FUNCTION IF EXISTS {SpAddKeyword}(TEXT);
CREATE OR REPLACE FUNCTION {SpAddKeyword}(keyword_to_add TEXT)
RETURNS INT AS $$
	DECLARE
		id INT := 0;
	BEGIN
		SELECT {Db.Keyword.ColumnName(nameof(Db.Keyword._.KeywordID))} INTO id FROM {Db.Keyword.Name} WHERE {Db.Keyword.ColumnName(nameof(Db.Keyword._.Keyword))}=keyword_to_add;
		IF id <> 0 THEN
			RETURN id;
		ELSE
			INSERT INTO {Db.Keyword.Name}({Db.Keyword.ColumnName(nameof(Db.Keyword._.Keyword))}) VALUES (keyword_to_add);
			RETURN lastval();
		END IF;
	END;
$$ LANGUAGE plpgsql;
";
				cmd.ExecuteNonQuery();
			}
		}
		#endregion

		#region DBアクセス関数
		static Func<IDbCodeCommand, string, int> _AddUrl;
		public static Func<IDbCodeCommand, string, int> AddUrl {
			get {
				if (_AddUrl == null) {
					var argUrl = new Argument("");
					var sql = E.NewSql();
					sql.CallFunc(SpAddUrl, argUrl);

					var func = sql.BuildFunc<string, int>(argUrl);
					_AddUrl = new Func<IDbCodeCommand, string, int>((cmd, url) => {
						using (var reader = func.Execute(cmd, url)) {
							int result = 0;
							foreach (var r in reader.Records) {
								result = r;
							}
							return result;
						}
					});
				}
				return _AddUrl;
			}
		}

		static Func<IDbCodeCommand, string, int> _AddKeyword;
		public static Func<IDbCodeCommand, string, int> AddKeyword {
			get {
				if (_AddKeyword == null) {
					var argKeyword = new Argument("");
					var sql = E.NewSql();
					sql.CallFunc(SpAddKeyword, argKeyword);

					var func = sql.BuildFunc<string, int>(argKeyword);
					_AddKeyword = new Func<IDbCodeCommand, string, int>((cmd, url) => {
						using (var reader = func.Execute(cmd, url)) {
							int result = 0;
							foreach (var r in reader.Records) {
								result = r;
							}
							return result;
						}
					});
				}
				return _AddKeyword;
			}
		}

		static Action<IDbCodeCommand, int, int, int> _AddUrlKeyword;
		public static Action<IDbCodeCommand, int, int, int> AddUrlKeyword {
			get {
				if (_AddUrlKeyword == null) {
					var argUrlId = new Argument(0);
					var argKeywordId = new Argument(0);
					var argKeywordCount = new Argument(0);
					var sql = E.NewSql();
					sql.InsertIntoIfNotExists(UrlKeyword, t => new[] { t.UrlID == argUrlId, t.KeywordID == argKeywordId, t.KeywordCount == argKeywordCount }, 2);

					var action = sql.BuildAction<int, int, int>(argUrlId, argKeywordId, argKeywordCount);
					_AddUrlKeyword = new Action<IDbCodeCommand, int, int, int>((cmd, urlID, keywordID, keywordCount) => {
						action.Execute(cmd, urlID, keywordID, keywordCount);
					});
				}
				return _AddUrlKeyword;
			}
		}

		static Action<IDbCodeCommand, int, string> _AddUrlTitle;
		public static Action<IDbCodeCommand, int, string> AddUrlTitle {
			get {
				if (_AddUrlTitle == null) {
					var argUrlId = new Argument(0);
					var argTitle = new Argument("");
					var sql = E.NewSql();
					sql.InsertIntoIfNotExists(UrlTitle, t => new[] { t.UrlID == argUrlId, t.UrlTitle == argTitle }, 1);

					var action = sql.BuildAction<int, string>(argUrlId, argTitle);
					_AddUrlTitle = new Action<IDbCodeCommand, int, string>((cmd, urlID, urlTitle) => {
						action.Execute(cmd, urlID, urlTitle);
					});
				}
				return _AddUrlTitle;
			}
		}

		static Action<IDbCodeCommand, int, int> _AddLink;
		public static Action<IDbCodeCommand, int, int> AddLink {
			get {
				if (_AddLink == null) {
					var argSrcUrlID = new Argument(0);
					var argDstUrlID = new Argument(0);
					var sql = E.NewSql();
					sql.InsertIntoIfNotExists(UrlLink, t => new[] { t.SrcUrlID == argSrcUrlID, t.DstUrlID == argDstUrlID });

					var action = sql.BuildAction<int, int>(argSrcUrlID, argDstUrlID);
					_AddLink = new Action<IDbCodeCommand, int, int>((cmd, srcUrlID, dstUrlID) => {
						action.Execute(cmd, srcUrlID, dstUrlID);
					});
				}
				return _AddLink;
			}
		}
		#endregion
	}
}