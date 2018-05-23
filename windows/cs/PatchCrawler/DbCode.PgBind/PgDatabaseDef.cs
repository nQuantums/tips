using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using DbCode;
using DbCode.Defs;
using Npgsql;
using Npgsql.PostgresTypes;

namespace DbCode.PgBind {
	public class PgDatabaseDef : IDatabaseDef {
		class ConstraintInfo {
			public string Name { get; set; }
			public bool IsPrimaryKey { get; set; }
			public bool IsUniqueKey { get; set; }
			public short[] Combination { get; set; }
			public string IndexType { get; set; }
			public Dictionary<int, PgColumnDef> Columns { get; set; }

			public ConstraintInfo(string name, bool isPrimaryKey, bool isUniqueKey, short[] combination, string indexType) {
				this.Name = name;
				this.IsPrimaryKey = isPrimaryKey;
				this.IsUniqueKey = isUniqueKey;
				this.Combination = combination;
				this.IndexType = indexType;
				this.Columns = new Dictionary<int, PgColumnDef>();
			}
		}

		public string Name { get; private set; }
		public IEnumerable<ITableDef> Tables { get; private set; }

		public PgDatabaseDef(string name, IEnumerable<ITableDef> tables) {
			this.Name = name;
			this.Tables = tables;
		}

		public PgDatabaseDef(NpgsqlConnection connection) {
			try {
				using (var cmd = connection.CreateCommand()) {
					// データベース名の取得
					cmd.CommandText = "SELECT * FROM current_catalog;";
					using (var dr = cmd.ExecuteReader()) {
						while (dr.Read()) {
							this.Name = dr[0] as string;
						}
					}

					// テーブル一覧と列の取得
					var tables = new Dictionary<string, PgTableDef>();
					cmd.CommandText = @"
SELECT
	table_name
	,column_name
	,ordinal_position
	,column_default
	,udt_name
FROM
	information_schema.columns
WHERE
	table_catalog=@0 AND table_schema='public'
ORDER BY
	table_name, ordinal_position
;
";
					cmd.Parameters.Clear();
					cmd.Parameters.AddWithValue("@0", this.Name);
					using (var dr = cmd.ExecuteReader()) {
						while (dr.Read()) {
							var table_name = dr[0] as string;
							var column_name = dr[1] as string;
							var ordinal_position = dr[2] as string;
							var column_default = dr[3] as string;
							var udt_name = dr[4] as string;
							ColumnFlags flags = 0;
							if (!string.IsNullOrEmpty(column_default)) {
								if (column_default.StartsWith("nextval('")) {
									flags |= ColumnFlags.Serial;
								}
								if (column_default == "CURRENT_TIMESTAMP") {
									flags |= ColumnFlags.DefaultCurrentTimestamp;
								}
							}

							PgTableDef tableDef;
							if (!tables.TryGetValue(table_name, out tableDef)) {
								tables[table_name] = tableDef = new PgTableDef(table_name);
							}
							tableDef.ColumnDefs.Add(new PgColumnDef(column_name, new PgDbType(udt_name), flags));
						}
					}

					// プライマリキーとインデックス、ユニークキーの取得
					cmd.CommandText = @"
SELECT
--	t.relname,
	i.relname AS index_name,
	ix.indisprimary AS is_primary_key,
	ix.indisunique AS is_unique_key,
	ix.indkey AS combination,
	att.attname AS column_name,
	att.attnum AS attnum,
	a.amname AS index_type 
FROM
	pg_class t 
	INNER JOIN pg_index ix ON ix.indrelid=t.oid 
	INNER JOIN pg_class i ON i.oid=ix.indexrelid 
	INNER JOIN pg_am a ON a.oid=i.relam 
	INNER JOIN pg_attribute att ON att.attrelid=t.oid AND att.attnum=ANY(ix.indkey) 
WHERE
	t.relname=@0 AND t.relkind='r';
";
					foreach (var table in tables.Values) {
						var indicesDic = new Dictionary<string, ConstraintInfo>();
						cmd.Parameters.Clear();
						cmd.Parameters.AddWithValue("@0", table.Name);
						using (var dr = cmd.ExecuteReader()) {
							while (dr.Read()) {
								var index_name = dr[0] as string;
								var is_primary_key = (bool)dr[1];
								var is_unique_key = (bool)dr[2];
								var combination = dr[3] as short[];
								var column_name = dr[4] as string;
								var attnum = Convert.ToInt32(dr[5]);
								var index_type = dr[6] as string;

								ConstraintInfo info;
								if (!indicesDic.TryGetValue(index_name, out info)) {
									indicesDic[index_name] = info = new ConstraintInfo(index_name, is_primary_key, is_unique_key, combination, index_type);
								}
								var column = (from c in table.ColumnDefs where c.Name == column_name select c).FirstOrDefault();
								info.Columns[attnum] = column ?? throw new ApplicationException();
							}
						}
						var indices = new List<Tuple<string, string, PgColumnDef[]>>();
						var uniques = new List<Tuple<string, PgColumnDef[]>>();
						foreach (var kvp in indicesDic) {
							var info = kvp.Value;
							var columns = (from i in info.Combination select info.Columns[i]).ToArray();
							if (info.IsPrimaryKey) {
								table.PrimaryKey = new PrimaryKeyDef(kvp.Key, columns);
							} else if (info.IsUniqueKey) {
								uniques.Add(new Tuple<string, PgColumnDef[]>(kvp.Key, columns));
							} else {
								indices.Add(new Tuple<string, string, PgColumnDef[]>(kvp.Key, info.IndexType, columns));
							}
						}
						table.Indices = (from i in indices select new IndexDef(i.Item1, i.Item2 == "gin" ? IndexFlags.Gin : 0, i.Item3)).ToArray();
						table.Uniques = (from u in uniques select new UniqueDef(u.Item1, u.Item2)).ToArray();
					}

					this.Tables = (from t in tables.Values select t).ToArray();
				}
			} catch (PostgresException ex) {
				throw new PgEnvironmentException(ex);
			}
		}
	}
}
