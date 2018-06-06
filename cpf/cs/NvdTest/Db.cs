using DbCode;
using DbCode.Defs;
using DbCode.PgBind;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NvdTest {
	public static class Db {
		public static PgEnvironment E { get; } = new PgEnvironment();

		#region 列定義
		/// <summary>
		/// 列定義一覧
		/// </summary>
		public static class C {
			public static int CveID => E.Int32("cve_id");
			public static string Cve => E.String("cve");
			public static int Cpe22ID => E.Int32("cpe22_id");
			public static int Cpe23ID => E.Int32("cpe23_id");
			public static string Uri => E.String("uri");
			public static string Url => E.String("url");
			public static int VendorID => E.Int32("vendor_id");
			public static string Vendor => E.String("vendor");
		}
		#endregion

		#region テーブル定義
		public class TbCve : TableDef<TbCve.D> {
			public TbCve() : base(E, "tb_cve") { }

			public class D : ColumnsBase {
				public int CveID => As(() => C.CveID, ColumnFlags.Serial);
				public string Cve => As(() => C.Cve);
			}

			public override IPrimaryKeyDef GetPrimaryKey() => MakePrimaryKey(t => t.CveID);
			public override IEnumerable<IIndexDef> GetIndices() => MakeIndices(
				MakeIndex(0, t => t.Cve)
			);
			public override IEnumerable<IUniqueDef> GetUniques() => MakeUniques(
				MakeUnique(t => t.Cve)
			);
		}
		public static TbCve Cve { get; private set; } = new TbCve();

		public class TbVendor : TableDef<TbVendor.D> {
			public TbVendor() : base(E, "tb_vendor") { }

			public class D : ColumnsBase {
				public int VendorID => As(() => C.VendorID, ColumnFlags.Serial);
				public string Vendor => As(() => C.Vendor);
			}

			public override IPrimaryKeyDef GetPrimaryKey() => MakePrimaryKey(t => t.VendorID);
			public override IEnumerable<IIndexDef> GetIndices() => MakeIndices(
				MakeIndex(0, t => t.Vendor)
			);
			public override IEnumerable<IUniqueDef> GetUniques() => MakeUniques(
				MakeUnique(t => t.Vendor)
			);
		}
		public static TbVendor Vendor { get; private set; } = new TbVendor();

		public class TbCpe22 : TableDef<TbCpe22.D> {
			public TbCpe22() : base(E, "tb_cpe22") { }

			public class D : ColumnsBase {
				public int Cpe22ID => As(() => C.Cpe22ID, ColumnFlags.Serial);
				public string Uri => As(() => C.Uri);
			}

			public override IPrimaryKeyDef GetPrimaryKey() => MakePrimaryKey(t => t.Cpe22ID);
			public override IEnumerable<IIndexDef> GetIndices() => MakeIndices(
				MakeIndex(0, t => t.Uri)
			);
			public override IEnumerable<IUniqueDef> GetUniques() => MakeUniques(
				MakeUnique(t => t.Uri)
			);
		}
		public static TbCpe22 Cpe22 { get; private set; } = new TbCpe22();

		public class TbCpe23 : TableDef<TbCpe23.D> {
			public TbCpe23() : base(E, "tb_cpe23") { }

			public class D : ColumnsBase {
				public int Cpe23ID => As(() => C.Cpe23ID, ColumnFlags.Serial);
				public string Uri => As(() => C.Uri);
			}

			public override IPrimaryKeyDef GetPrimaryKey() => MakePrimaryKey(t => t.Cpe23ID);
			public override IEnumerable<IIndexDef> GetIndices() => MakeIndices(
				MakeIndex(0, t => t.Uri)
			);
			public override IEnumerable<IUniqueDef> GetUniques() => MakeUniques(
				MakeUnique(t => t.Uri)
			);
		}
		public static TbCpe23 Cpe23 { get; private set; } = new TbCpe23();

		public class TbCveVendor : TableDef<TbCveVendor.D> {
			public TbCveVendor() : base(E, "tb_cve_vendor") { }

			public class D : ColumnsBase {
				public int CveID => As(() => C.CveID, ColumnFlags.Serial);
				public int VendorID => As(() => C.VendorID);
			}

			public override IEnumerable<IIndexDef> GetIndices() => MakeIndices(
				MakeIndex(0, t => t.VendorID)
			);
		}
		public static TbCveVendor CveVendor { get; private set; } = new TbCveVendor();

		public class TbCveUrl : TableDef<TbCveUrl.D> {
			public TbCveUrl() : base(E, "tb_cve_url") { }

			public class D : ColumnsBase {
				public int CveID => As(() => C.CveID);
				public string Url => As(() => C.Url);
			}

			public override IEnumerable<IIndexDef> GetIndices() => MakeIndices(
				MakeIndex(0, t => t.Url)
			);
		}
		public static TbCveUrl CveUrl { get; private set; } = new TbCveUrl();

		public class TbCveCpe22 : TableDef<TbCveCpe22.D> {
			public TbCveCpe22() : base(E, "tb_cve_cpe22") { }

			public class D : ColumnsBase {
				public int CveID => As(() => C.CveID, ColumnFlags.Serial);
				public int Cpe22ID => As(() => C.Cpe22ID);
			}

			public override IEnumerable<IIndexDef> GetIndices() => MakeIndices(
				MakeIndex(0, t => t.Cpe22ID)
			);
		}
		public static TbCveCpe22 CveCpe22 { get; private set; } = new TbCveCpe22();

		public class TbCveCpe23 : TableDef<TbCveCpe23.D> {
			public TbCveCpe23() : base(E, "tb_cve_cpe23") { }

			public class D : ColumnsBase {
				public int CveID => As(() => C.CveID, ColumnFlags.Serial);
				public int Cpe23ID => As(() => C.Cpe23ID);
			}

			public override IEnumerable<IIndexDef> GetIndices() => MakeIndices(
				MakeIndex(0, t => t.Cpe23ID)
			);
		}
		public static TbCveCpe23 CveCpe23 { get; private set; } = new TbCveCpe23();
		#endregion

		#region フィールド
		const string RoleName = "cve";
		const string DbName = "cve";
		const string SpAddCve = "add_cve";
		const string SpAddVendor = "add_vendor";
		const string SpAddCpe22 = "add_cpe22";
		const string SpAddCpe23 = "add_cpe23";
		const string SpAddCveVendor = "add_cve_vendor";
		const string SpAddCveUrl = "add_cve_url";
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

				// ストアド登録コマンドを生成するデリゲート
				Func<string, Column, Column, string> createAddTextStoredProcedure = (spname, idcolumn, column) => {
					var tableName = (column.Table as ITableDef).Name;
					return $@"
DROP FUNCTION IF EXISTS {spname}(TEXT);
CREATE OR REPLACE FUNCTION {spname}(value_to_add TEXT)
RETURNS INT AS $$
	DECLARE
		id INT := 0;
	BEGIN
		SELECT {idcolumn.Name} INTO id FROM {tableName} WHERE {column.Name}=value_to_add;
		IF id <> 0 THEN
			RETURN id;
		ELSE
			INSERT INTO {tableName}({column.Name}) VALUES (value_to_add);
			RETURN lastval();
		END IF;
	END;
$$ LANGUAGE plpgsql;
";
				};

				// CVE登録ストアドを登録
				cmd.CommandText = createAddTextStoredProcedure(SpAddCve, Db.Cve.GetColumn(() => Db.Cve._.CveID), Db.Cve.GetColumn(() => Db.Cve._.Cve));
				cmd.ExecuteNonQuery();

				// ベンダー登録ストアドを登録
				cmd.CommandText = createAddTextStoredProcedure(SpAddVendor, Db.Vendor.GetColumn(() => Db.Vendor._.VendorID), Db.Vendor.GetColumn(() => Db.Vendor._.Vendor));
				cmd.ExecuteNonQuery();

				// CPE2.2登録ストアドを登録
				cmd.CommandText = createAddTextStoredProcedure(SpAddCpe22, Db.Cpe22.GetColumn(() => Db.Cpe22._.Cpe22ID), Db.Cpe22.GetColumn(() => Db.Cpe22._.Uri));
				cmd.ExecuteNonQuery();

				// CPE2.3登録ストアドを登録
				cmd.CommandText = createAddTextStoredProcedure(SpAddCpe23, Db.Cpe23.GetColumn(() => Db.Cpe23._.Cpe23ID), Db.Cpe23.GetColumn(() => Db.Cpe23._.Uri));
				cmd.ExecuteNonQuery();
			}
		}
		#endregion

		#region DBアクセス関数
		/// <summary>
		/// 可能ならCVE追加しIDを取得する
		/// </summary>
		public static Func<IDbCodeCommand, string, int> AddCve {
			get {
				if (_AddCve == null) {
					var argCve = new Argument("");
					var sql = E.NewSql();
					sql.CallFunc(SpAddCve, argCve);

					var func = sql.BuildFunc<string, int>(argCve);
					_AddCve = new Func<IDbCodeCommand, string, int>((cmd, url) => {
						if (_CveMap.TryGetValue(url, out int id)) {
							return id;
						} else {
							int result = 0;
							using (var reader = func.Execute(cmd, url)) {
								foreach (var r in reader.Records) {
									result = r;
								}
							}
							_CveMap[url] = result;
							return result;
						}
					});
				}
				return _AddCve;
			}
		}
		static Func<IDbCodeCommand, string, int> _AddCve;
		static readonly Dictionary<string, int> _CveMap = new Dictionary<string, int>();

		/// <summary>
		/// 可能ならVendor追加しIDを取得する
		/// </summary>
		public static Func<IDbCodeCommand, string, int> AddVendor {
			get {
				if (_AddVendor == null) {
					var argVendor = new Argument("");
					var sql = E.NewSql();
					sql.CallFunc(SpAddVendor, argVendor);

					var func = sql.BuildFunc<string, int>(argVendor);
					_AddVendor = new Func<IDbCodeCommand, string, int>((cmd, url) => {
						if (_VendorMap.TryGetValue(url, out int id)) {
							return id;
						} else {
							int result = 0;
							using (var reader = func.Execute(cmd, url)) {
								foreach (var r in reader.Records) {
									result = r;
								}
							}
							_VendorMap[url] = result;
							return result;
						}
					});
				}
				return _AddVendor;
			}
		}
		static Func<IDbCodeCommand, string, int> _AddVendor;
		static readonly Dictionary<string, int> _VendorMap = new Dictionary<string, int>();

		/// <summary>
		/// 可能ならCPE2.2追加しIDを取得する
		/// </summary>
		public static Func<IDbCodeCommand, string, int> AddCpe22 {
			get {
				if (_AddCpe22 == null) {
					var argUri = new Argument("");
					var sql = E.NewSql();
					sql.CallFunc(SpAddCpe22, argUri);

					var func = sql.BuildFunc<string, int>(argUri);
					_AddCpe22 = new Func<IDbCodeCommand, string, int>((cmd, url) => {
						if (_Cpe22Map.TryGetValue(url, out int id)) {
							return id;
						} else {
							int result = 0;
							using (var reader = func.Execute(cmd, url)) {
								foreach (var r in reader.Records) {
									result = r;
								}
							}
							_Cpe22Map[url] = result;
							return result;
						}
					});
				}
				return _AddCpe22;
			}
		}
		static Func<IDbCodeCommand, string, int> _AddCpe22;
		static readonly Dictionary<string, int> _Cpe22Map = new Dictionary<string, int>();

		/// <summary>
		/// 可能ならCPE2.3追加しIDを取得する
		/// </summary>
		public static Func<IDbCodeCommand, string, int> AddCpe23 {
			get {
				if (_AddCpe23 == null) {
					var argUri = new Argument("");
					var sql = E.NewSql();
					sql.CallFunc(SpAddCpe23, argUri);

					var func = sql.BuildFunc<string, int>(argUri);
					_AddCpe23 = new Func<IDbCodeCommand, string, int>((cmd, url) => {
						if (_Cpe23Map.TryGetValue(url, out int id)) {
							return id;
						} else {
							int result = 0;
							using (var reader = func.Execute(cmd, url)) {
								foreach (var r in reader.Records) {
									result = r;
								}
							}
							_Cpe23Map[url] = result;
							return result;
						}
					});
				}
				return _AddCpe23;
			}
		}
		static Func<IDbCodeCommand, string, int> _AddCpe23;
		static readonly Dictionary<string, int> _Cpe23Map = new Dictionary<string, int>();

		/// <summary>
		/// 可能ならCVEに関するベンダーを追加する
		/// </summary>
		public static Action<IDbCodeCommand, int, int> AddCveVendor {
			get {
				if (_AddCveVendor == null) {
					var argCveID = new Argument(0);
					var argVendorID = new Argument(0);
					var sql = E.NewSql();
					sql.InsertIntoIfNotExists(CveVendor, t => new[] { t.CveID == argCveID, t.VendorID == argVendorID });

					var action = sql.BuildAction<int, int>(argCveID, argVendorID);
					_AddCveVendor = new Action<IDbCodeCommand, int, int>((cmd, cveID, vendorID) => {
						action.Execute(cmd, cveID, vendorID);
					});
				}
				return _AddCveVendor;
			}
		}
		static Action<IDbCodeCommand, int, int> _AddCveVendor;

		/// <summary>
		/// 可能ならCVEに関するURLを追加する
		/// </summary>
		public static Action<IDbCodeCommand, int, string> AddCveUrl {
			get {
				if (_AddCveUrl == null) {
					var argCveID = new Argument(0);
					var argUrl = new Argument("");
					var sql = E.NewSql();
					sql.InsertIntoIfNotExists(CveUrl, t => new[] { t.CveID == argCveID, t.Url == argUrl });

					var action = sql.BuildAction<int, string>(argCveID, argUrl);
					_AddCveUrl = new Action<IDbCodeCommand, int, string>((cmd, cveID, url) => {
						action.Execute(cmd, cveID, url);
					});
				}
				return _AddCveUrl;
			}
		}
		static Action<IDbCodeCommand, int, string> _AddCveUrl;

		/// <summary>
		/// 可能ならCVEに関するCPE2.2情報を追加する
		/// </summary>
		public static Action<IDbCodeCommand, int, int> AddCveCpe22 {
			get {
				if (_AddCveCpe22 == null) {
					var argCveID = new Argument(0);
					var argCpe22ID = new Argument(0);
					var sql = E.NewSql();
					sql.InsertIntoIfNotExists(CveCpe22, t => new[] { t.CveID == argCveID, t.Cpe22ID == argCpe22ID });

					var action = sql.BuildAction<int, int>(argCveID, argCpe22ID);
					_AddCveCpe22 = new Action<IDbCodeCommand, int, int>((cmd, cveID, Cpe22ID) => {
						action.Execute(cmd, cveID, Cpe22ID);
					});
				}
				return _AddCveCpe22;
			}
		}
		static Action<IDbCodeCommand, int, int> _AddCveCpe22;

		/// <summary>
		/// 可能ならCVEに関するCPE2.3情報を追加する
		/// </summary>
		public static Action<IDbCodeCommand, int, int> AddCveCpe23 {
			get {
				if (_AddCveCpe23 == null) {
					var argCveID = new Argument(0);
					var argCpe23ID = new Argument(0);
					var sql = E.NewSql();
					sql.InsertIntoIfNotExists(CveCpe23, t => new[] { t.CveID == argCveID, t.Cpe23ID == argCpe23ID });

					var action = sql.BuildAction<int, int>(argCveID, argCpe23ID);
					_AddCveCpe23 = new Action<IDbCodeCommand, int, int>((cmd, cveID, Cpe23ID) => {
						action.Execute(cmd, cveID, Cpe23ID);
					});
				}
				return _AddCveCpe23;
			}
		}
		static Action<IDbCodeCommand, int, int> _AddCveCpe23;
		#endregion
	}
}
