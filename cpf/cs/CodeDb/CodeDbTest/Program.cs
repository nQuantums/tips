using System;
using System.Text;
using System.Collections.Generic;
using System.Data.Common;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using System.Linq.Expressions;
using CodeDb;
using CodeDb.PgBinder;

namespace CodeDbTest {
	/// <summary>
	/// データベースの定義
	/// </summary>
	public static class TestDb {
		/// <summary>
		/// DB環境
		/// </summary>
		public static PgEnvironment E { get; } = new PgEnvironment();

		/// <summary>
		/// 列定義一覧
		/// </summary>
		public static class C {
			public static int UserID => E.Int32("user_id");
			public static string UserName => E.String("user_name");
			public static int EntityID => E.Int32("entity_id");
			public static int ParentEntityID => E.Int32("parent_entity_id");
			public static int TagID => E.Int32("tag_id");
			public static int[] TagIDs => E.Int32Array("tag_ids");
			public static string TagText => E.String("tag_text");
			public static string Content => E.String("content");
			public static int ContentRevisionID => E.Int32("content_revision_id");
			public static int[] CachedHolderIDs => E.Int32Array("cached_holder_ids");
			public static int[] AddHolderIDs => E.Int32Array("add_holder_ids");
			public static int[] DelHolderIDs => E.Int32Array("del_holder_ids");
			public static int[] CachedWatcherIDs => E.Int32Array("cached_watcher_ids");
			public static int[] AddWatcherIDs => E.Int32Array("add_watcher_ids");
			public static int[] DelWatcherIDs => E.Int32Array("del_watcher_ids");
			public static DateTime CreateDateTime => E.DateTime("create_date_time", ColumnFlags.DefaultCurrentTimestamp);
		}

		public class TblUser : TableDef<TblUser.Cols> {
			public TblUser() : base(E, "tb_user") { }

			public class Cols : ColumnsBase {
				public int UserID => As(() => C.UserID, ColumnFlags.Serial);
				public string UserName => As(() => C.UserName);
				public DateTime CreateDateTime => As(() => C.CreateDateTime);
			}

			public override IPrimaryKeyDef GetPrimaryKey() => MakePrimaryKey(() => Columns.UserID);
			public override IEnumerable<IIndexDef> GetIndices() => MakeIndices(
				MakeIndex(0, () => Columns.UserID),
				MakeIndex(0, () => Columns.CreateDateTime)
			);
			public override IEnumerable<IUniqueDef> GetUniques() => MakeUniques(
				MakeUnique(() => _.UserName)
			);
		}

		public class TblEntityConst : TableDef<TblEntityConst.Cols> {
			public TblEntityConst() : base(E, "tb_entity_const") { }

			public class Cols : ColumnsBase {
				public int EntityID => As(() => C.EntityID, ColumnFlags.Serial);
				public int UserID => As(() => C.UserID);
				public DateTime CreateDateTime => As(() => C.CreateDateTime);
			}

			public override IPrimaryKeyDef GetPrimaryKey() => MakePrimaryKey(() => Columns.EntityID);
			public override IEnumerable<IIndexDef> GetIndices() => MakeIndices(
				MakeIndex(0, () => Columns.UserID),
				MakeIndex(0, () => Columns.CreateDateTime)
			);
		}

		public class TblEntityContent : TableDef<TblEntityContent.Cols> {
			public TblEntityContent() : base(E, "tb_entity_content") { }

			public class Cols : ColumnsBase {
				public int ContentRevisionID => As(() => C.ContentRevisionID, ColumnFlags.Serial);
				public int EntityID => As(() => C.EntityID);
				public string Content => As(() => C.Content);
				public DateTime CreateDateTime => As(() => C.CreateDateTime);
			}

			public override IPrimaryKeyDef GetPrimaryKey() => MakePrimaryKey(() => Columns.ContentRevisionID);
			public override IEnumerable<IIndexDef> GetIndices() => MakeIndices(MakeIndex(0, () => Columns.EntityID));
		}

		public class TblEntityTag : TableDef<TblEntityTag.Cols> {
			public TblEntityTag() : base(E, "tb_entity_tag") { }

			public class Cols : ColumnsBase {
				public int EntityID => As(() => C.EntityID);
				public int[] TagIDs => As(() => C.TagIDs);
			}

			public override IPrimaryKeyDef GetPrimaryKey() => MakePrimaryKey(() => Columns.EntityID);
			public override IEnumerable<IIndexDef> GetIndices() => MakeIndices(MakeIndex(IndexFlags.Gin, () => Columns.TagIDs));
		}

		public class TblEntityParent : TableDef<TblEntityParent.Cols> {
			public TblEntityParent() : base(E, "tb_entity_parent") { }

			public class Cols : ColumnsBase {
				public int EntityID => As(() => C.EntityID);
				public int ParentEntityID => As(() => C.ParentEntityID);
			}

			public override IPrimaryKeyDef GetPrimaryKey() => MakePrimaryKey(() => Columns.EntityID);
			public override IEnumerable<IIndexDef> GetIndices() => MakeIndices(MakeIndex(0, () => Columns.ParentEntityID));
		}

		public class TblEntityHolder : TableDef<TblEntityHolder.Cols> {
			public TblEntityHolder() : base(E, "tb_entity_holder") { }

			public class Cols : ColumnsBase {
				public int EntityID => As(() => C.EntityID);
				public int[] CachedHolderIDs => As(() => C.CachedHolderIDs);
				public int[] AddHolderIDs => As(() => C.AddHolderIDs);
			}

			public override IPrimaryKeyDef GetPrimaryKey() => MakePrimaryKey(() => Columns.EntityID);
			public override IEnumerable<IIndexDef> GetIndices() => MakeIndices(MakeIndex(IndexFlags.Gin, () => Columns.CachedHolderIDs));
		}

		public class TblEntityWatcher : TableDef<TblEntityWatcher.Cols> {
			public TblEntityWatcher() : base(E, "tb_entity_watcher") { }

			public class Cols : ColumnsBase {
				public int EntityID => As(() => C.EntityID);
				public int[] CachedWatcherIDs => As(() => C.CachedWatcherIDs);
				public int[] AddWatcherIDs => As(() => C.AddWatcherIDs);
				public int[] DelWatcherIDs => As(() => C.DelWatcherIDs);
			}

			public override IPrimaryKeyDef GetPrimaryKey() => MakePrimaryKey(() => Columns.EntityID);
			public override IEnumerable<IIndexDef> GetIndices() => MakeIndices(MakeIndex(IndexFlags.Gin, () => Columns.CachedWatcherIDs));
		}

		public class TblTag : TableDef<TblTag.Cols> {
			public TblTag() : base(E, "tb_tag") { }

			public class Cols : ColumnsBase {
				public int TagID => As(() => C.TagID);
				public string TagText => As(() => C.TagText);
				public DateTime CreateDateTime => As(() => C.CreateDateTime);
			}

			public override IPrimaryKeyDef GetPrimaryKey() => MakePrimaryKey(() => Columns.TagID);
			public override IEnumerable<IIndexDef> GetIndices() => MakeIndices(MakeIndex(0, () => Columns.TagText), MakeIndex(0, () => Columns.CreateDateTime));
		}

		public static TblUser User { get; } = new TblUser();
		public static TblEntityConst EntityConst { get; } = new TblEntityConst();
		public static TblEntityContent EntityContent { get; } = new TblEntityContent();
		public static TblEntityTag EntityTag { get; } = new TblEntityTag();
		public static TblEntityParent EntityParent { get; } = new TblEntityParent();
		public static TblEntityHolder EntityHolder { get; } = new TblEntityHolder();
		public static TblEntityWatcher EntityWatcher { get; } = new TblEntityWatcher();
		public static TblTag Tag { get; } = new TblTag();
	}

	class Program {
		const string RoleName = "role1";
		const string DbName = "testdb";

		static void Main(string[] args) {
			var E = TestDb.E;

			using (var con = E.CreateConnection("User ID=postgres;Password=wertyu89?;Host=localhost;Port=5432;Database=postgres;")) {
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

			using (var con = E.CreateConnection($"User ID={RoleName};Password='Passw0rd!';Host=localhost;Port=5432;Database={DbName};")) {
				con.Open();

				// データベースの状態を取得
				var current = E.ReadDatabaseDef(con);
				// クラスからデータベース定義を生成する
				var target = E.GenerateDatabaseDef(typeof(TestDb), "test_db");
				// 差分を生成
				var delta = E.GetDatabaseDelta(current, target);

				// 差分を適用する
				var context = new ElementCode();
				E.ApplyDatabaseDelta(context, delta);
				var cmd = con.CreateCommand();
				cmd.Apply(context.Build());
				cmd.ExecuteNonQuery();


				var sql = new Sql(TestDb.E);
				var id = new Variable(4);
				var now = new Variable(DateTime.Now);
				sql.InsertInto(TestDb.User, t => new { UserName = "afe" });
				var p = sql.Build();
				cmd.Apply(p);
				try {
					cmd.ExecuteNonQuery();
				} catch (CodeDbEnvironmentException ex) {
					if (ex.ErrorType != DbEnvironmentErrorType.DuplicateKey) {
						throw;
					}
				}

				{
					var code = new ElementCode();
					code.Concat("SELECT '{8EA22A18-EB0A-49E5-B61D-F026CA7773FF}'::uuid, now();");
					code.Concat("SELECT '{8EA22A18-EB0A-49E5-B61D-F026CA7773FF}'::uuid, now();");
					var s = TestDb.E.NewSql();
					s.Code(code);
					cmd.Apply(s.Build());
					using (var dr = cmd.ExecuteReader()) {
						foreach (var value in dr.Enumerate<Tuple<Guid, DateTime>>()) {
							Console.WriteLine(value);
						}
						dr.NextResult();
						foreach (var value in dr.Enumerate<Tuple<Guid, DateTime>>()) {
							Console.WriteLine(value);
						}
					}
				}

				//for (int i = 0; i < 1000; i++) {
				//	id.Value = i;
				//	cmd.Apply(p);
				//	cmd.ExecuteNonQuery();
				//}
				Console.WriteLine(p.CommandText);
			}
		}
	}
}
