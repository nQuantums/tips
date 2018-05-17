using System;
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

		/// <summary>
		/// 列定義一覧
		/// </summary>
		public static class C {
			public static int UrlID => E.Int32("url_id");
			public static string Url => E.String("url");
			public static int KeywordID => E.Int32("keyword_id");
			public static string Keyword => E.String("keyword");
		}

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

		public class TbKeyword : TableDef<TbKeyword.D> {
			public TbKeyword() : base(E, "tb_keyword") { }

			public class D : ColumnsBase {
				public int KeywordID => As(() => C.UrlID, ColumnFlags.Serial);
				public string Keyword => As(() => C.Url);
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
			}

			public override IPrimaryKeyDef GetPrimaryKey() => MakePrimaryKey(() => _.UrlID, () => _.KeywordID);
			public override IEnumerable<IIndexDef> GetIndices() => MakeIndices(
				MakeIndex(0, () => _.KeywordID)
			);
		}
		public static TbUrlKeyword UrlKeyword { get; private set; } = new TbUrlKeyword();



		const string RoleName = "patch_crawler";
		const string DbName = "patch_crawler";

		public static FuncCmd<string, int> AddUrl { get; private set; }


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

				// ストアドを登録
				cmd.CommandText = $@"
DROP FUNCTION IF EXISTS add_url(url text);
CREATE OR REPLACE FUNCTION add_url(url text)
RETURNS int4 AS $$
BEGIN
    BEGIN
        INSERT INTO {Url.Name}({Url.ColumnMap.TryGetByPropertyName(nameof(TbUrl.D.Url)).Name}) VALUES (url);
        RETURN lastval();
    EXCEPTION WHEN SQLSTATE '23505' THEN
        RETURN 0;
    END;
END;
$$ LANGUAGE plpgsql;
";
				cmd.ExecuteNonQuery();
			}

			// 関数作成
			{
				var code = new ElementCode();
				var arg = new Argument("");
				code.Concat(@"SELECT add_url(");
				code.Add(arg);
				code.Concat(")");

				var s = E.NewSql();
				s.Code(code);
				AddUrl = s.BuildFunc<string, int>(arg);
			}
		}
	}
}
