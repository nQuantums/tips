using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeDb;
using CodeDb.Internal;
using Npgsql;
using NpgsqlTypes;

namespace CodeDb.PgBinder {
	/// <summary>
	/// Npgsql接続環境基本クラス
	/// </summary>
	public abstract class PgEnvironmentBase : DbEnvironment {
		static bool EqualColumnDefs(IEnumerable<IColumnDef> l, IEnumerable<IColumnDef> r) {
			var la = l.ToArray();
			var ra = r.ToArray();
			if (la.Length != ra.Length) {
				return false;
			}
			for (int i = 0, n = la.Length; i < n; i++) {
				var lc = la[i];
				var rc = ra[i];
				if (lc.Name != rc.Name) {
					return false;
				}
				if (!lc.DbType.TypeEqualsTo(rc.DbType)) {
					return false;
				}
			}
			return true;
		}

		public override Sql NewSql() {
			return new Sql(this);
		}

		public override IDatabaseDef GenerateDatabaseDef(Type databaseDefType, string databaseName) {
			var tableDefs = new List<ITableDef>();
			foreach (var pi in databaseDefType.GetProperties()) {
				if (typeof(ITableDef).IsAssignableFrom(pi.PropertyType)) {
					var tableDef = pi.GetValue(null) as ITableDef;
					if (tableDef != null) {
						tableDefs.Add(tableDef);
					}
				}
			}
			return new PgDatabaseDef(databaseName, tableDefs);
		}

		public override ICodeDbConnection CreateConnection(string connectionString) {
			try {
				return new PgConnection(new NpgsqlConnection(connectionString));
			} catch (PostgresException ex) {
				throw new PgEnvironmentException(ex);
			}
		}

		public override void CreateRole(ICodeDbConnection connection, string roleName, string password) {
			using (var cmd = connection.CreateCommand()) {
				var context = new ElementCode();
				context.Add(SqlKeyword.CreateRole);
				context.Concat(Quote(roleName));
				context.Add(SqlKeyword.Password);
				context.Concat(string.Concat("'", password, "'"));
				context.Concat("LOGIN");
				context.Go();
				context.Build().Execute(cmd);
			}
		}

		public override void CreateDatabase(ICodeDbConnection connection, string databaseName, string owner) {
			using (var cmd = connection.CreateCommand()) {
				var context = new ElementCode();
				context.Add(SqlKeyword.CreateDatabase);
				context.Concat(Quote(databaseName));
				context.Add(SqlKeyword.Owner);
				context.Concat(Quote(owner));
				context.Go();
				context.Build().Execute(cmd);
			}
		}

		public override IDatabaseDef ReadDatabaseDef(ICodeDbConnection connection) {
			return new PgDatabaseDef(connection.Core as NpgsqlConnection);
		}

		public override IDatabaseDelta GetDatabaseDelta(IDatabaseDef currentDatabaseDef, IDatabaseDef targetDatabaseDef) {
			// テーブル毎の比較
			var tablesa = currentDatabaseDef.Tables;
			var tablest = targetDatabaseDef.Tables;
			var tablesToDrop = new List<ITableDef>(from ta in tablesa where !(from tt in tablest where tt.Name == ta.Name select tt).Any() select ta);
			var tablesToAdd = new List<ITableDef>();
			var tablesToModify = new List<ITableDelta>();

			foreach (var tt in tablest) {
				var tableName = tt.Name;
				var ta = (from t in tablesa where t.Name == tableName select t).FirstOrDefault();
				if (ta == null) {
					tablesToAdd.Add(tt);
				} else {
					var tableDelta = new PgTableDelta();
					tableDelta.Name = tableName;

					// 列毎の比較
					var columnsa = ta.ColumnDefs;
					var columnst = tt.ColumnDefs;
					var columnsToDrop = new List<IColumnDef>(from ca in columnsa where !(from ct in columnst where ct.Name == ca.Name select ct).Any() select ca);
					var columnsToAdd = new List<IColumnDef>();
					foreach (var ct in columnst) {
						var columnName = ct.Name;
						var ca = (from c in columnsa where c.Name == columnName select c).FirstOrDefault();
						if (ca == null) {
							columnsToAdd.Add(ct);
						}
					}
					tableDelta.ColumnsToDrop = columnsToDrop;
					tableDelta.ColumnsToAdd = columnsToAdd;

					// プライマリキーの比較
					var pka = ta.GetPrimaryKey();
					var pkt = tt.GetPrimaryKey();
					if (pka != null && pkt == null) {
						tableDelta.PrimaryKeyToDrop = pka;
					} else if (pka == null && pkt != null) {
						tableDelta.PrimaryKeyToAdd = pkt;
					} else {
						if (pka.Name != pkt.Name || !EqualColumnDefs(pka.Columns, pkt.Columns)) {
							tableDelta.PrimaryKeyToDrop = pka;
							tableDelta.PrimaryKeyToAdd = pkt;
						}
					}

					// インデックス毎の比較
					var indicesa = ta.GetIndices();
					var indicest = tt.GetIndices();
					var indicesToDrop = new List<IIndexDef>(from a in indicesa where !(from t in indicest where a.Name == t.Name select t).Any() select a);
					var indicesToAdd = new List<IIndexDef>();
					foreach (var it in indicest) {
						var indexName = it.Name;
						var ia = (from i in indicesa where i.Name == indexName select i).FirstOrDefault();
						if (ia == null) {
							indicesToAdd.Add(it);
						} else {
							if (ia.Name != it.Name || ia.Flags != it.Flags || !EqualColumnDefs(ia.Columns, it.Columns)) {
								indicesToDrop.Add(ia);
								indicesToAdd.Add(it);
							}
						}
					}
					tableDelta.IndicesToDrop = indicesToDrop;
					tableDelta.IndicesToAdd = indicesToAdd;

					// ユニーク制約毎の比較
					var uniquesa = ta.GetUniques();
					var uniquest = tt.GetUniques();
					var uniquesToDrop = new List<IUniqueDef>(from a in uniquesa where !(from t in uniquest where a.Name == t.Name select t).Any() select a);
					var uniquesToAdd = new List<IUniqueDef>();
					foreach (var ut in uniquest) {
						var uniqueName = ut.Name;
						var ua = (from u in uniquesa where u.Name == uniqueName select u).FirstOrDefault();
						if (ua == null) {
							uniquesToAdd.Add(ut);
						} else {
							if (ua.Name != ut.Name || !EqualColumnDefs(ua.Columns, ut.Columns)) {
								uniquesToDrop.Add(ua);
								uniquesToAdd.Add(ut);
							}
						}
					}
					tableDelta.UniquesToDrop = uniquesToDrop;
					tableDelta.UniquesToAdd = uniquesToAdd;


					if (tableDelta.ColumnsToDrop.Any() || tableDelta.ColumnsToAdd.Any()
						|| tableDelta.PrimaryKeyToDrop != null || tableDelta.PrimaryKeyToAdd != null
						|| tableDelta.IndicesToDrop.Any() || tableDelta.IndicesToAdd.Any()
						|| tableDelta.UniquesToDrop.Any() || tableDelta.UniquesToAdd.Any()) {
						tablesToModify.Add(tableDelta);
					}
				}
			}

			return new PgDatabaseDelta { Name = targetDatabaseDef.Name, TablesToDrop = tablesToDrop, TablesToAdd = tablesToAdd, TablesToModify = tablesToModify };
		}

		public override IDbType CreateDbTypeFromType(Type type) {
			return new PgDbType(type);
		}

		public override string Quote(string name) {
			return string.Concat("\"", name, "\"");
		}

		static void Add(ElementCode context, string tableName, IColumnDef column, Action<IColumnDef> columnOption) {
			if (column == null) {
				return;
			}
			context.Add(SqlKeyword.AlterTable, SqlKeyword.IfExists);
			context.Concat(tableName);
			context.Add(SqlKeyword.AddColumn);
			context.Concat(column.Name);
			if (columnOption != null) {
				columnOption(column);
			}
			context.Go();
		}

		static void Add(ElementCode context, string tableName, IPrimaryKeyDef primaryKey) {
			if (primaryKey == null) {
				return;
			}
			context.Add(SqlKeyword.AlterTable, SqlKeyword.IfExists);
			context.Concat(tableName);
			context.Add(SqlKeyword.AddConstraint);
			context.Concat(primaryKey.Name);
			context.Add(SqlKeyword.PrimaryKey);
			context.AddColumnDefs(primaryKey.Columns);
			context.Go();
		}

		static void Add(ElementCode context, string tableName, IIndexDef index) {
			if (index == null) {
				return;
			}
			context.Add(SqlKeyword.CreateIndex, SqlKeyword.IfNotExists);
			context.Concat(index.Name);
			context.Add(SqlKeyword.On);
			context.Concat(tableName);
			if ((index.Flags & IndexFlags.Gin) != 0) {
				context.Add(SqlKeyword.Using);
				context.Concat("gin");
			}
			context.AddColumnDefs(index.Columns);
			context.Go();
		}

		static void Add(ElementCode context, string tableName, IUniqueDef unique) {
			if (unique == null) {
				return;
			}
			context.Add(SqlKeyword.AlterTable, SqlKeyword.IfExists);
			context.Concat(tableName);
			context.Add(SqlKeyword.AddConstraint);
			context.Concat(unique.Name);
			context.Add(SqlKeyword.Unique);
			context.AddColumnDefs(unique.Columns);
			context.Go();
		}

		static void Drop(ElementCode context, string tableName, IPrimaryKeyDef primaryKey) {
			if (primaryKey == null) {
				return;
			}
			context.Add(SqlKeyword.AlterTable, SqlKeyword.IfExists);
			context.Concat(tableName);
			context.Add(SqlKeyword.DropConstraint, SqlKeyword.IfExists);
			context.Concat(primaryKey.Name);
			context.Go();
		}

		static void Drop(ElementCode context, string tableName, IIndexDef index) {
			if (index == null) {
				return;
			}
			context.Add(SqlKeyword.DropIndex, SqlKeyword.IfExists);
			context.Concat(index.Name);
			context.Go();
		}

		static void Drop(ElementCode context, string tableName, IUniqueDef unique) {
			if (unique == null) {
				return;
			}
			context.Add(SqlKeyword.AlterTable, SqlKeyword.IfExists);
			context.Concat(tableName);
			context.Add(SqlKeyword.DropConstraint, SqlKeyword.IfExists);
			context.Concat(unique.Name);
			context.Go();
		}

		static void Drop(ElementCode context, string tableName, IColumnDef column) {
			if (column == null) {
				return;
			}
			context.Add(SqlKeyword.AlterTable, SqlKeyword.IfExists);
			context.Concat(tableName);
			context.Add(SqlKeyword.DropColumn, SqlKeyword.IfExists);
			context.Concat(column.Name);
			context.Go();
		}

		public override void ApplyDatabaseDelta(ElementCode context, IDatabaseDelta databaseDelta) {
			// 列の型など付与するデリゲート
			Action<IColumnDef> columnOption = (column) => {
				context.Concat(column.DbType.ToDbTypeString((column.Flags & ColumnFlags.Serial) != 0 ? DbTypeStringFlags.Serial : 0));
				if ((column.Flags & ColumnFlags.Nullable) != 0) {
					context.Add(SqlKeyword.Null);
				} else {
					context.Add(SqlKeyword.NotNull);
				}
				if ((column.Flags & ColumnFlags.DefaultCurrentTimestamp) != 0) {
					context.Add(SqlKeyword.Default, SqlKeyword.CurrentTimestamp);
				}
			};
			
			// 捨てるべきテーブルを捨てる
			foreach (var table in databaseDelta.TablesToDrop) {
				context.Add(SqlKeyword.DropTable, SqlKeyword.IfExists);
				context.Concat(table.Name);
				context.Go();
			}

			// 新たに増えるテーブルを作成する
			foreach (var table in databaseDelta.TablesToAdd) {
				var tableName = table.Name;

				// テーブル本体
				context.Add(SqlKeyword.CreateTable, SqlKeyword.IfNotExists);
				context.Concat(tableName);
				context.AddColumnDefs(table.ColumnDefs, null, columnOption);
				context.Go();

				// プライマリキー
				Add(context, tableName, table.GetPrimaryKey());

				// インデックス
				foreach (var index in table.GetIndices()) {
					Add(context, tableName, index);
				}

				// ユニークキー
				foreach (var unique in table.GetUniques()) {
					Add(context, tableName, unique);
				}
			}

			// 変化するテーブルに対応する
			foreach (var table in databaseDelta.TablesToModify) {
				var tableName = table.Name;

				// 先にプライマリキー等の制約を捨てる
				Drop(context, tableName, table.PrimaryKeyToDrop);
				foreach (var index in table.IndicesToDrop) {
					Drop(context, tableName, index);
				}
				foreach (var unique in table.UniquesToDrop) {
					Drop(context, tableName, unique);
				}

				// 列を捨てる
				foreach (var column in table.ColumnsToDrop) {
					Drop(context, tableName, column);
				}

				// 列を追加する
				foreach (var column in table.ColumnsToAdd) {
					Add(context, tableName, column, columnOption);
				}

				// プライマリキー追加
				Add(context, tableName, table.PrimaryKeyToAdd);

				// インデックス追加
				foreach (var index in table.IndicesToAdd) {
					Add(context, tableName, index);
				}

				// ユニークキー追加
				foreach (var unique in table.UniquesToAdd) {
					Add(context, tableName, unique);
				}
			}
		}
	}
}
