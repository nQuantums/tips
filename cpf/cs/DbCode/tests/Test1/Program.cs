using System;
using System.Text;
using System.Collections.Generic;
using System.Data.Common;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using System.Linq.Expressions;
using DbCode;
using DbCode.Defs;
using DbCode.PgBind;

namespace Test1 {
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
			public static int[] UserIDs => E.Int32Array("user_ids");
			public static int UserGroupID => E.Int32("user_group_id");
			public static string UserName => E.String("user_name");
			public static int EntityID => E.Int32("entity_id");
			public static int ParentEntityID => E.Int32("parent_entity_id");
			public static int TagID => E.Int32("tag_id");
			public static int TagGroupID => E.Int32("tag_group_id");
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
			public static int PermissionID => E.Int32("permission_id");
			public static int CreatableUserGroupID => E.Int32("creatable_usergroup_id");
			public static int WritableUserGroupID => E.Int32("writable_usergroup_id");
			public static int ReadableUserGroupID => E.Int32("readable_usergroup_id");
		}

		/// <summary>
		/// ユーザー
		/// <para>ユーザー名はユニーク、一度決めたら変更できない</para>
		/// </summary>
		public class TbUser : TableDef<TbUser.D> {
			public TbUser() : base(E, "tb_user") { }

			/// <summary>
			/// 列定義
			/// </summary>
			public class D : ColumnsBase {
				/// <summary>
				/// ユーザーID
				/// </summary>
				public int UserID => As(() => C.UserID, ColumnFlags.PrimaryKey | ColumnFlags.Serial);

				/// <summary>
				/// ユーザー名
				/// </summary>
				public string UserName => As(() => C.UserName, ColumnFlags.Index_1 | ColumnFlags.Unique_1);

				/// <summary>
				/// ユーザー作成日時
				/// </summary>
				public DateTime CreateDateTime => As(() => C.CreateDateTime, ColumnFlags.Index_2);
			}
			public class R : D {
				new public int UserID { get; set; }
				new public string UserName { get; set; }
				new public DateTime CreateDateTime { get; set; }
			}
		}

		/// <summary>
		/// ユーザーグループ、複数ユーザーを１つのIDで表現するために使用される
		/// <para>ユーザーID一覧は一度決めたら変更できない</para>
		/// </summary>
		public class TbUserGroup : TableDef<TbUserGroup.D> {
			public TbUserGroup() : base(E, "tb_user_group") { }

			/// <summary>
			/// 列定義
			/// </summary>
			public class D : ColumnsBase {
				/// <summary>
				/// ユーザーグループID
				/// </summary>
				public int UserGroupID => As(() => C.UserGroupID, ColumnFlags.PrimaryKey | ColumnFlags.Serial);

				/// <summary>
				/// ユーザーID一覧
				/// </summary>
				public int[] UserIDs => As(() => C.UserIDs, ColumnFlags.Index_1 | ColumnFlags.Unique_1 | ColumnFlags.Gin);
			}
		}

		/// <summary>
		/// アクセス許可設定
		/// <para>ユーザーグループIDは一度決めたら変更できない</para>
		/// </summary>
		public class TbPermission : TableDef<TbPermission.D> {
			public TbPermission() : base(E, "tb_permission") { }

			/// <summary>
			/// 列定義
			/// </summary>
			public class D : ColumnsBase {
				/// <summary>
				/// アクセス許可設定ID
				/// </summary>
				public int PermissionID => As(() => C.PermissionID, ColumnFlags.PrimaryKey | ColumnFlags.Serial);

				/// <summary>
				/// 作成が許可されたユーザーグループID、0なら全ユーザー許可
				/// </summary>
				public int CreatableUserGroupID => As(() => C.CreatableUserGroupID, ColumnFlags.Index_1 | ColumnFlags.Unique_1);

				/// <summary>
				/// 書き込みが許可されたユーザーグループID、0なら全ユーザー許可
				/// </summary>
				public int WritableUserGroupID => As(() => C.WritableUserGroupID, ColumnFlags.Index_2 | ColumnFlags.Unique_1);

				/// <summary>
				/// 読み込みが許可されたユーザーグループID、0なら全ユーザー許可
				/// </summary>
				public int ReadableUserGroupID => As(() => C.ReadableUserGroupID, ColumnFlags.Index_3 | ColumnFlags.Unique_1);
			}
		}

		/// <summary>
		/// エンティティに付与するタグ
		/// <para>タグテキストは一度決めたら変更できない</para>
		/// </summary>
		public class TbTag : TableDef<TbTag.D> {
			public TbTag() : base(E, "tb_tag") { }

			/// <summary>
			/// 列定義
			/// </summary>
			public class D : ColumnsBase {
				/// <summary>
				/// タグID
				/// </summary>
				public int TagID => As(() => C.TagID, ColumnFlags.PrimaryKey | ColumnFlags.Serial);

				/// <summary>
				/// タグテキスト
				/// </summary>
				public string TagText => As(() => C.TagText, ColumnFlags.Index_1 | ColumnFlags.Unique_1);

				/// <summary>
				/// タグ作成日時
				/// </summary>
				public DateTime CreateDateTime => As(() => C.CreateDateTime, ColumnFlags.Index_2);
			}
		}

		/// <summary>
		/// タググループ、複数タグを１つのIDで表現するために使用される
		/// <para>タグID一覧は一度決めたら変更できない</para>
		/// </summary>
		public class TbTagGroup : TableDef<TbTagGroup.D> {
			public TbTagGroup() : base(E, "tb_tag_group") { }

			/// <summary>
			/// 列定義
			/// </summary>
			public class D : ColumnsBase {
				/// <summary>
				/// タググループID
				/// </summary>
				public int TagGroupID => As(() => C.TagGroupID, ColumnFlags.PrimaryKey | ColumnFlags.Serial);

				/// <summary>
				/// タグID一覧
				/// </summary>
				public int[] TagIDs => As(() => C.TagIDs, ColumnFlags.Index_1 | ColumnFlags.Unique_1 | ColumnFlags.Gin);
			}
		}

		/// <summary>
		/// エンティティ固定値、エンティティ作成時に値が確定しその後変更されることは無い
		/// </summary>
		public class TbEntityConst : TableDef<TbEntityConst.D> {
			public TbEntityConst() : base(E, "tb_entity_const") { }

			/// <summary>
			/// 列定義
			/// </summary>
			public class D : ColumnsBase {
				/// <summary>
				/// エンティティID
				/// </summary>
				public int EntityID => As(() => C.EntityID, ColumnFlags.PrimaryKey | ColumnFlags.Serial);

				/// <summary>
				/// エンティティ作成ユーザーID
				/// </summary>
				public int UserID => As(() => C.UserID, ColumnFlags.Index_1);
			}
		}

		/// <summary>
		/// エンティティへのアクセス許可設定
		/// <para>アクセス許可設定に関係無く<see cref="TbEntityConst.D.UserID"/>のユーザーはフルアクセス可能</para>
		/// </summary>
		public class TbEntityPermission : TableDef<TbEntityPermission.D> {
			public TbEntityPermission() : base(E, "tb_entity_permission") { }

			/// <summary>
			/// 列定義
			/// </summary>
			public class D : ColumnsBase {
				/// <summary>
				/// エンティティID
				/// </summary>
				public int EntityID => As(() => C.EntityID, ColumnFlags.PrimaryKey);

				/// <summary>
				/// アクセス許可設定ID
				/// </summary>
				public int PermissionID => As(() => C.PermissionID);
			}
		}

		/// <summary>
		/// エンティティに付与するタグ
		/// </summary>
		public class TbEntityTag : TableDef<TbEntityTag.D> {
			public TbEntityTag() : base(E, "tb_entity_tag") { }

			/// <summary>
			/// 列定義
			/// </summary>
			public class D : ColumnsBase {
				/// <summary>
				/// エンティティID
				/// </summary>
				public int EntityID => As(() => C.EntityID, ColumnFlags.PrimaryKey);

				/// <summary>
				/// タググループID
				/// </summary>
				public int TagGroupID => As(() => C.TagGroupID, ColumnFlags.Index_1);
			}
		}

		/// <summary>
		/// エンティティの階層構造
		/// </summary>
		public class TbEntityTree : TableDef<TbEntityTree.D> {
			public TbEntityTree() : base(E, "tb_entity_tree") { }

			/// <summary>
			/// 列定義
			/// </summary>
			public class D : ColumnsBase {
				/// <summary>
				/// エンティティID
				/// </summary>
				public int EntityID => As(() => C.EntityID, ColumnFlags.PrimaryKey);

				/// <summary>
				/// 親エンティティID
				/// </summary>
				public int ParentEntityID => As(() => C.ParentEntityID, ColumnFlags.Index_1);
			}
		}

		/// <summary>
		/// エンティティアクセス許可設定の階層構造
		/// </summary>
		public class TbEntityPermissionTree : TableDef<TbEntityPermissionTree.D> {
			public TbEntityPermissionTree() : base(E, "tb_entity_permission_tree") { }

			/// <summary>
			/// 列定義
			/// </summary>
			public class D : ColumnsBase {
				/// <summary>
				/// エンティティID
				/// </summary>
				public int EntityID => As(() => C.EntityID, ColumnFlags.PrimaryKey);

				/// <summary>
				/// アクセス許可設定を持つ直近の親エンティティID
				/// </summary>
				public int ParentEntityID => As(() => C.ParentEntityID, ColumnFlags.Index_1);
			}
		}

		/// <summary>
		/// エンティティ内容
		/// </summary>
		public class TbEntityContent : TableDef<TbEntityContent.D> {
			public TbEntityContent() : base(E, "tb_entity_content") { }

			/// <summary>
			/// 列定義
			/// </summary>
			public class D : ColumnsBase {
				/// <summary>
				/// エンティティ内容リビジョンID、内容が書き換わる度にインクリメントされる
				/// </summary>
				public int ContentRevisionID => As(() => C.ContentRevisionID, ColumnFlags.PrimaryKey | ColumnFlags.Serial);

				/// <summary>
				/// エンティティID
				/// </summary>
				public int EntityID => As(() => C.EntityID, ColumnFlags.Index_1);

				/// <summary>
				/// エンティティ内容
				/// </summary>
				public string Content => As(() => C.Content);

				/// <summary>
				/// エンティティ内容作成日時
				/// </summary>
				public DateTime CreateDateTime => As(() => C.CreateDateTime);
			}
		}

		public class TbEntityWatcher : TableDef<TbEntityWatcher.D> {
			public TbEntityWatcher() : base(E, "tb_entity_watcher") { }

			public class D : ColumnsBase {
				public int EntityID => As(() => C.EntityID, ColumnFlags.PrimaryKey);
				public int[] CachedWatcherIDs => As(() => C.CachedWatcherIDs, ColumnFlags.Index_1 | ColumnFlags.Gin);
				public int[] AddWatcherIDs => As(() => C.AddWatcherIDs);
				public int[] DelWatcherIDs => As(() => C.DelWatcherIDs);
			}
		}

		public static TbUser User { get; } = new TbUser();
		public static TbEntityConst EntityConst { get; } = new TbEntityConst();
		public static TbEntityContent EntityContent { get; } = new TbEntityContent();
		public static TbEntityTag EntityTag { get; } = new TbEntityTag();
		public static TbEntityTree EntityParent { get; } = new TbEntityTree();
		public static TbEntityWatcher EntityWatcher { get; } = new TbEntityWatcher();
		public static TbTag Tag { get; } = new TbTag();
	}

	class Program {
		const string RoleName = "dbcode";
		const string DbName = "dbcode_test1";

		static void Main(string[] args) {
			var E = TestDb.E;

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

			using (var con = E.CreateConnection($"User ID={RoleName};Password='Passw0rd!';Host=localhost;Port=5432;Database={DbName};")) {
				con.Open();
				var cmd = con.CreateCommand();
				cmd.CommandTimeout = 0;

				{
					var s = TestDb.E.NewSql();
					s.DropTable(TestDb.User);
					s.Build().Execute(cmd);
				}

				// データベースの状態を取得
				var current = E.ReadDatabaseDef(con);
				// クラスからデータベース定義を生成する
				var target = E.GenerateDatabaseDef(typeof(TestDb), "test_db");
				// 差分を生成
				var delta = E.GetDatabaseDelta(current, target);

				// 差分を適用する
				var context = new ElementCode();
				E.ApplyDatabaseDelta(context, delta);
				context.Build().Execute(cmd);

				{
					var sql = TestDb.E.NewSql();
					sql.InsertIntoWithValue(TestDb.User, t => new[] { t.UserName == "a" });
					sql.InsertIntoWithValue(TestDb.User, t => new[] { t.UserName == "b" });
					sql.InsertIntoWithValue(TestDb.User, t => new[] { t.UserName == "c" });
					sql.BuildAction().Execute(cmd);
				}
				{
					var sql = TestDb.E.NewSql();
					var select = sql.From(TestDb.User).Where(t => t.UserName == "a").Select(t => new { t.UserName, t.UserID, t.CreateDateTime });
					var f = sql.BuildSelectFunc(select);
					using (var reader = f.Execute(cmd)) {
						foreach (var record in reader.Records) {
							Console.WriteLine($"{record.UserID} {record.UserName} {record.CreateDateTime}");
						}
					}
				}

				{
					var code = new ElementCode();
					var arg1 = new Argument(0);
					var arg2 = new Argument(0);
					code.Concat("SELECT ARRAY[1, 2, 3], '{8EA22A18-EB0A-49E5-B61D-F026CA7773FF}'::uuid, now(),");
					code.Add(arg1);
					code.Go();
					code.Concat("SELECT ARRAY[1, 2, 3], '{8EA22A18-EB0A-49E5-B61D-F026CA7773FF}'::uuid, now(),");
					code.Add(arg2);
					code.Go();
					var s = TestDb.E.NewSql();
					s.Code(code);
					var commandable = s.BuildFunc<int, int, Tuple<int[], Guid, DateTime, int>>(arg1, arg2);
					using (var reader = commandable.Execute(cmd, 16, 32)) {
						do {
							foreach (var record in reader.Records) {
								Console.WriteLine(record);
							}
						} while (reader.DataReader.NextResult());
					}
				}
			}
		}
	}
}
