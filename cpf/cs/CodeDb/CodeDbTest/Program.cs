using System;
using System.Text;
using System.Collections.Generic;
using System.Data.Common;
using System.Net.Http;
using System.Threading.Tasks;
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

	class MyJVN {
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

		public static async Task<string> getVulnOverviewList(TimeRange pub = new TimeRange(), TimeRange publish = new TimeRange(), TimeRange firstPublish = new TimeRange()) {
			var sb = new StringBuilder("https://jvndb.jvn.jp/myjvn?method=getVulnOverviewList&feed=hnd");
			if (pub != TimeRange.Empty) {
				sb.Append("&datePublicStartY=" + pub.Start.Year);
				sb.Append("&datePublicStartM=" + pub.Start.Month);
				sb.Append("&datePublicStartD=" + pub.Start.Day);
				sb.Append("&datePublicEndY=" + pub.End.Year);
				sb.Append("&datePublicEndM=" + pub.End.Month);
				sb.Append("&datePublicEndD=" + pub.End.Day);
			}
			if (publish != TimeRange.Empty) {
				sb.Append("&datePublishedStartY=" + publish.Start.Year);
				sb.Append("&datePublishedStartM=" + publish.Start.Month);
				sb.Append("&datePublishedStartD=" + publish.Start.Day);
				sb.Append("&datePublishedEndY=" + publish.End.Year);
				sb.Append("&datePublishedEndM=" + publish.End.Month);
				sb.Append("&datePublishedEndD=" + publish.End.Day);
			}
			if (firstPublish != TimeRange.Empty) {
				sb.Append("&dateFirstPublishedStartY=" + firstPublish.Start.Year);
				sb.Append("&dateFirstPublishedStartM=" + firstPublish.Start.Month);
				sb.Append("&dateFirstPublishedStartD=" + firstPublish.Start.Day);
				sb.Append("&dateFirstPublishedEndY=" + firstPublish.End.Year);
				sb.Append("&dateFirstPublishedEndM=" + firstPublish.End.Month);
				sb.Append("&dateFirstPublishedEndD=" + firstPublish.End.Day);
			}

			using (HttpClient client = new HttpClient())
			using (var res = await client.GetAsync(sb.ToString()))
			using (var content = res.Content) {
				return await content.ReadAsStringAsync();
			}
		}
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
			}

			var sql = new Sql(TestDb.E);
			sql.InsertInto(TestDb.EntityConst, t => new { t.UserID }).SelectIfNotExists(() => new { UserID = 1 }, t => t.UserID == 1);

			var ec = new ElementCode();
			sql.BuildSql(ec);
			var p = ec.Build();
		}

		static async void GetData() {
			Console.WriteLine(await MyJVN.getVulnOverviewList(new MyJVN.TimeRange(new DateTime(2017, 1, 1), new DateTime(2017, 12, 31))));
		}
	}
}
