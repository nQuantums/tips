using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace MySqlDiff {
	class Program {
		public class Schema {
			public string Name { get; private set; }
			public List<Table> Tables { get; private set; }

			public Table this[string tableName] {
				get {
					return this.Tables.Where(t => t.Name == tableName).FirstOrDefault();
				}
			}

			public Schema(string name) {
				this.Name = name;
				this.Tables = new List<Table>();
			}

			public Table AddTable(string name) {
				var table = this.Tables.Where(c => c.Name == name).FirstOrDefault();
				if (table != null) {
					return table;
				}
				this.Tables.Add(table = new Table(this, name));
				return table;
			}

			public override string ToString() {
				return this.Name;
			}
		}

		public class Table {
			public Schema Schema { get; private set; }
			public string Name { get; private set; }
			public List<Column> Columns { get; private set; }
			public PrimaryKey PrimaryKey { get; private set; }

			public string FullName {
				get {
					return string.Join(".", this.Schema.Name, this.Name);
				}
			}

			public Column this[string columnName] {
				get {
					return this.Columns.Where(t => t.Name == columnName).FirstOrDefault();
				}
			}

			public Table(Schema schema, string name) {
				this.Schema = schema;
				this.Name = name;
				this.Columns = new List<Column>();
			}

			public Column AddColumn(string name, int ordinalPosition, int ordinalPositionInConstraint) {
				var column = this.Columns.Where(c => c.Name == name).FirstOrDefault();
				if (column != null) {
					return column;
				}
				this.Columns.Add(column = new Column(name, ordinalPosition, ordinalPositionInConstraint));
				return column;
			}

			public PrimaryKey SetPrimaryKey(Column column) {
				var primaryKey = this.PrimaryKey;
				if (primaryKey == null) {
					this.PrimaryKey = primaryKey = new PrimaryKey();
				}
				primaryKey.AddColumn(column);
				return primaryKey;
			}

			public void Normalize() {
				this.Columns.Sort((l, r) => l.OrdinalPosition - r.OrdinalPosition);
			}

			public override string ToString() {
				return this.Name;
			}
		}

		public class Column {
			public string Name { get; private set; }
			public int OrdinalPosition { get; private set; }
			public int OrdinalPositionInConstraint { get; private set; }

			public Column(string name, int ordinalPosition, int ordinalPositionInConstraint) {
				this.Name = name;
				this.OrdinalPosition = ordinalPosition;
				this.OrdinalPositionInConstraint = ordinalPositionInConstraint;
			}

			public override string ToString() {
				return string.Join(", ", this.Name, this.OrdinalPosition, this.OrdinalPositionInConstraint);
			}
		}

		public class PrimaryKey {
			public List<Column> Columns { get; private set; }

			public Column this[string columnName] {
				get {
					return this.Columns.Where(t => t.Name == columnName).FirstOrDefault();
				}
			}

			public PrimaryKey() {
				this.Columns = new List<Column>();
			}

			public void AddColumn(Column column) {
				if (this.Columns.Where(c => c == column).Any()) {
					return;
				}
				this.Columns.Add(column);
			}

			public bool IsSame(PrimaryKey primaryKey) {
				if (primaryKey == null) {
					return false;
				}
				if (this.Columns.Count != primaryKey.Columns.Count) {
					return false;
				}
				for (int i = 0; i < this.Columns.Count; i++) {
					if (this.Columns[i].Name != primaryKey.Columns[i].Name) {
						return false;
					}
				}
				return true;
			}

			public void Normalize() {
				this.Columns.Sort((l, r) => l.OrdinalPositionInConstraint - r.OrdinalPositionInConstraint);
			}

			public override string ToString() {
				return string.Join(", ", this.Columns.Select(c => c.Name));
			}
		}

		static List<Schema> Schemas = new List<Schema>();

		static int Main(string[] args) {
			if (args.Length < 3) {
				Console.WriteLine("--------------------------------");
				Console.WriteLine("指定された２スキーマ間のデータの差分を表示する。");
				Console.WriteLine("スキーマ内のテーブル構造は同じでなければならない。");
				Console.WriteLine();
				Console.WriteLine(" usage : MySqlDiff <接続文字列> <スキーマ1> <スキーマ2>");
				Console.WriteLine("--------------------------------");
				return 0;
			}

			var connectionString = args[0];
			var schema1 = args[1];
			var schema2 = args[2];

			using (var con = new MySqlConnection(connectionString)) {
				con.Open();

				using (var cmd = con.CreateCommand()) {
					cmd.CommandTimeout = 0;

					// 指定された２つのスキーマ情報を取得
					for (int i = 1; i <= 2; i++) {
						AddSchema(cmd, args[i]);
					}

					// 両方のスキーマに存在するテーブルの比較を行う
					foreach (var table1 in Schemas[0].Tables) {
						var table2 = Schemas[1][table1.Name];
						if (table2 != null) {
							DiffTable(cmd, table1, table2);
						}
					}
				}
			}

			return 0;
		}

		/// <summary>
		/// 指定スキーマ内の全テーブル内の全カラム情報を取得する
		/// </summary>
		static void AddSchema(MySqlCommand cmd, string schemaName) {
			cmd.Parameters.Clear();
			cmd.CommandText = $@"
SELECT
	c.table_schema
	,c.table_name
	,c.column_name
	,c.ordinal_position
	,c.column_key
	,u.constraint_name
	,u.ordinal_position
FROM
	information_schema.columns c
	LEFT JOIN information_schema.key_column_usage u ON u.table_schema=c.table_schema AND u.table_name=c.table_name AND u.column_name=c.column_name
WHERE
	c.table_schema='{schemaName}' AND (u.constraint_name='PRIMARY' OR u.constraint_name IS NULL);
";
			var schema = new Schema(schemaName);
			using (var dr = cmd.ExecuteReader()) {
				while (dr.Read()) {
					var table = schema.AddTable(ToString(dr[1]));
					var column = table.AddColumn(ToString(dr[2]), ToInt32(dr[3]), ToInt32(dr[6]));
					var constraintName = ToString(dr[5]);
					if (constraintName == "PRIMARY") {
						table.SetPrimaryKey(column);
					}
				}
			}

			foreach (var table in schema.Tables) {
				table.Normalize();

				var primaryKey = table.PrimaryKey;
				if (primaryKey != null) {
					primaryKey.Normalize();
				}
			}

			Schemas.Add(schema);
		}

		/// <summary>
		/// 指定テーブルのデータの差分を出力する
		/// </summary>
		static void DiffTable(MySqlCommand cmd, string tableName) {
			var tables = new List<Table>();
			foreach (var schema in Schemas) {
				var table = schema[tableName];
				if (table == null) {
					throw new ApplicationException($"テーブル {tableName} は {schema.Name} 内に存在しません。");
				}
				tables.Add(table);
			}

			DiffTable(cmd, tables[0], tables[1]);
		}

		/// <summary>
		/// 指定された２テーブル間のデータ差分を出力する
		/// </summary>
		static void DiffTable(MySqlCommand cmd, Table table1, Table table2) {
			if (table1.PrimaryKey == null || table2.PrimaryKey == null) {
				return;
			}
			if (!table1.PrimaryKey.IsSame(table2.PrimaryKey)) {
				return;
			}

			var columnNames = new List<string>();
			foreach (var column1 in table1.Columns) {
				var columnName = column1.Name;
				var column2 = table2[columnName];
				if (column2 != null) {
					columnNames.Add(columnName);
				}
			}

			var pkNames = new List<string>(table1.PrimaryKey.Columns.Select(c => c.Name));

			var columnNamesWithoutPk = new List<string>(columnNames.Where(c => !pkNames.Contains(c)));

			Func<Table, Table, string> generateCommandText = (t1, t2) => {
				return $@"
SELECT
	{string.Join(",", pkNames.Select(name => "t1." + name))}
FROM
	{t1.FullName} t1
WHERE
	NOT EXISTS (SELECT * FROM {t2.FullName} t2 WHERE {string.Join(" AND ", pkNames.Select(name => "t2." + name + "=t1." + name))});
";
			};

			cmd.Parameters.Clear();
			cmd.CommandText = generateCommandText(table1, table2);
			ShowResult(cmd, $"{table1.Name} [{table1.Schema} > {table2.Schema}]");
			cmd.CommandText = generateCommandText(table2, table1);
			ShowResult(cmd, $"{table1.Name} [{table1.Schema} < {table2.Schema}]");

			if (columnNamesWithoutPk.Count != 0) {
				cmd.CommandText = $@"
SELECT
	{string.Join(",", columnNames.Select(name => "t1." + name))}
	,
	{string.Join(",", columnNames.Select(name => "t2." + name))}
FROM
	{table1.FullName} t1
	INNER JOIN {table2.FullName} t2 ON {string.Join(" AND ", pkNames.Select(name => "t2." + name + "=t1." + name))}
WHERE
	{string.Join(" OR ", columnNamesWithoutPk.Select(name => "t1." + name + "<>t2." + name))};
";
				ShowResult(cmd, $"{table1.Name} [{table1.Schema} != {table2.Schema}]");
			}
		}

		static void ShowResult(MySqlCommand cmd, string title) {
			Console.WriteLine("# " + title);
			Console.WriteLine();

			Console.WriteLine("```sql");
			Console.WriteLine(cmd.CommandText);
			Console.WriteLine("```");
			Console.WriteLine();

			using (var dr = cmd.ExecuteReader()) {
				do {
					Console.Write("|");
					for (int i = 0; i < dr.FieldCount; i++) {
						if (i != 0) {
							Console.Write("|");
						}
						Console.Write(dr.GetName(i));
					}
					Console.WriteLine("|");

					Console.Write("|");
					for (int i = 0; i < dr.FieldCount; i++) {
						if (i != 0) {
							Console.Write("|");
						}
						Console.Write("-");
					}
					Console.WriteLine("|");

					while (dr.Read()) {
						Console.Write("|");
						for (int i = 0; i < dr.FieldCount; i++) {
							if (i != 0) {
								Console.Write("|");
							}
							Console.Write(dr[i]);
						}
						Console.WriteLine("|");
					}
					Console.WriteLine();
				} while (dr.NextResult());
			}
		}

		static string ToString(object value) {
			if (value == null) {
				return null;
			}
			return Convert.ToString(value);
		}

		static int ToInt32(object value) {
			int i32;
			int.TryParse(ToString(value), out i32);
			return i32;
		}
	}
}
