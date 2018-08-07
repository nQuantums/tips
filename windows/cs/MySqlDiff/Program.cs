using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
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
		static bool Sql = false;
		static bool SaveNodiff = false;

		static int Main(string[] args) {
			// 必須引数取得
			var requiredArgs = args.Where(a => !a.StartsWith("/")).ToArray();

			// 必須引数が足りなかったら使い方表示
			if (requiredArgs.Length < 3) {
				Console.WriteLine("--------------------------------");
				Console.WriteLine("指定された２スキーマ間のデータの差分を表示する。");
				Console.WriteLine("スキーマ内のテーブル構造は同じでなければならない。");
				Console.WriteLine();
				Console.WriteLine("usage: MySqlDiff [オプション] <接続文字列> <スキーマ1> <スキーマ2>");
				Console.WriteLine("  オプション:");
				Console.WriteLine("    /sql         : SQLも出力する。");
				Console.WriteLine("    /save_nodiff : 差が無いテーブルの出力を残す。");
				Console.WriteLine();
				Console.WriteLine("カレントディレクトリに MySqlDiff{yyyyMMddHHmmss} フォルダを作成しその中にテーブル毎に差分ファイルを作成する。");
				Console.WriteLine("--------------------------------");
				return 0;
			}

			// オプション取得
			foreach (var option in args.Where(a => a.StartsWith("/"))) {
				switch (option.Substring(1)) {
				case "sql":
					Sql = true;
					break;
				case "save_nodiff":
					SaveNodiff = true;
					break;
				}
			}

			// 使用文字コード
			var enc = new UTF8Encoding(true); // BOM付きUTF8

			using (var con = new MySqlConnection(requiredArgs[0])) {
				con.Open();

				using (var cmd = con.CreateCommand()) {
					cmd.CommandTimeout = 0;

					// 指定された２つのスキーマ情報を取得
					for (int i = 1; i <= 2; i++) {
						AddSchema(cmd, requiredArgs[i]);
						if (Schemas[i - 1].Tables.Count == 0) {
							throw new ApplicationException($"指定されたスキーマ {requiredArgs[i]} は存在しません。");
						}
					}

					// 出力先ディレクトリ作成
					var dir = "MySqlDiff" + DateTime.Now.ToString("yyyyMMddHHmmss");
					Directory.CreateDirectory(dir);

					// 両方のスキーマに存在するテーブルの比較を行う
					var proceededTableCount = 0;
					var allTableCount = Schemas[0].Tables.Count;
					foreach (var table1 in Schemas[0].Tables) {
						Console.WriteLine($"{table1.Name} テーブル処理開始 {proceededTableCount}/{allTableCount}");
						var table2 = Schemas[1][table1.Name];
						if (table2 != null) {
							var fileName = Path.Combine(dir, table1.Name) + ".md";
							int count = 0;

							using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
							using (var sw = new StreamWriter(fs, enc)) {
								count = DiffTable(cmd, table1, table2, sw);
							}

							if (count == 0 && !SaveNodiff) {
								File.Delete(fileName);
							}
						} else {
							Console.WriteLine($"両方に存在しないテーブルのためスキップ。");
						}
						proceededTableCount++;
						Console.WriteLine($"{table1.Name} テーブル処理完了 {proceededTableCount}/{allTableCount}");
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
			cmd.Parameters.AddWithValue("@schemaName", schemaName);
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
	c.table_schema=@schemaName AND (u.constraint_name='PRIMARY' OR u.constraint_name IS NULL);
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
		/// 指定された２テーブル間のデータ差分を出力する
		/// </summary>
		static int DiffTable(MySqlCommand cmd, Table table1, Table table2, StreamWriter sw) {
			if (table1.PrimaryKey == null || table2.PrimaryKey == null) {
				Console.WriteLine("プライマリキーが存在しないためスキップ。");
				return 0;
			}
			if (!table1.PrimaryKey.IsSame(table2.PrimaryKey)) {
				Console.WriteLine("プライマリキー構成が異なるためスキップ。");
				return 0;
			}

			int result = 0;
			sw.WriteLine($"# {table1.Name} テーブル差分");
			sw.WriteLine();

			var columnNames = (from c1 in table1.Columns let c2 = table2[c1.Name] where c2 != null select c1.Name).ToList();
			var pkNames = table1.PrimaryKey.Columns.Select(c => c.Name).ToList();
			var columnNamesWithoutPk = columnNames.Where(c => !pkNames.Contains(c)).ToList();

			Func<Table, Table, string> generateCommandText = (t1, t2) => {
				return $@"
SELECT
	*
FROM
	{t1.FullName} t1
WHERE
	NOT EXISTS (SELECT * FROM {t2.FullName} t2 WHERE {string.Join(" AND ", pkNames.Select(name => "t2." + name + "=t1." + name))});
";
			};

			cmd.Parameters.Clear();
			cmd.CommandText = generateCommandText(table1, table2);
			result += ShowResult(cmd, $"{table1.Schema} > {table2.Schema}", sw);
			cmd.CommandText = generateCommandText(table2, table1);
			result += ShowResult(cmd, $"{table1.Schema} < {table2.Schema}", sw);

			if (columnNamesWithoutPk.Count != 0) {
				cmd.CommandText = $@"
SELECT
	{string.Join(",", columnNames.Select(name => "t1." + name))}
	,'<=>',
	{string.Join(",", columnNames.Select(name => "t2." + name))}
FROM
	{table1.FullName} t1
	INNER JOIN {table2.FullName} t2 ON {string.Join(" AND ", pkNames.Select(name => "t2." + name + "=t1." + name))}
WHERE
	{string.Join(" OR ", columnNamesWithoutPk.Select(name => "t1." + name + "<>t2." + name))};
";
				result += ShowResult(cmd, $"{table1.Schema} != {table2.Schema}", sw);
			}

			return result;
		}

		static int ShowResult(MySqlCommand cmd, string title, StreamWriter sw) {
			int result = 0;

			sw.WriteLine("## " + title);
			sw.WriteLine();

			if (Sql) {
				sw.WriteLine("```sql");
				sw.WriteLine(cmd.CommandText);
				sw.WriteLine("```");
				sw.WriteLine();
			}

			using (var dr = cmd.ExecuteReader()) {
				do {
					sw.Write("|");
					for (int i = 0; i < dr.FieldCount; i++) {
						if (i != 0) {
							sw.Write("|");
						}
						sw.Write(dr.GetName(i));
					}
					sw.WriteLine("|");

					sw.Write("|");
					for (int i = 0; i < dr.FieldCount; i++) {
						if (i != 0) {
							sw.Write("|");
						}
						sw.Write("-");
					}
					sw.WriteLine("|");

					int count = 0;
					while (dr.Read()) {
						count++;
						result++;
						sw.Write("|");
						for (int i = 0; i < dr.FieldCount; i++) {
							if (i != 0) {
								sw.Write("|");
							}
							var value = ToString(dr[i]);
							if (value == null) {
								value = "";
							} else {
								value = value.Replace("\r\n", "").Replace("\n", "").Replace("|", "");
							}
							sw.Write(value);
						}
						sw.WriteLine("|");
					}
					sw.WriteLine();
					sw.WriteLine($"### {count} 件");
					sw.WriteLine();
				} while (dr.NextResult());
			}

			return result;
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
