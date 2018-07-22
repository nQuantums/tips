using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace MySQLTest1 {
	class Program {
		const string DbName = "employees";
		const string TableName = "employees";

		static void Main(string[] args) {
			using (var con = new MySqlConnection("Server=localhost;Database=mysql;Uid=root;Pwd=Passw0rd!;")) {
				con.Open();

				using (var cmd = con.CreateCommand()) {
					cmd.CommandTimeout = 0;

					//					cmd.CommandText = $@"
					//SELECT `COLUMN_NAME` 
					//FROM `INFORMATION_SCHEMA`.`COLUMNS` 
					//WHERE `TABLE_SCHEMA`='{DbName}' 
					//    AND `TABLE_NAME`='{TableName}';
					//";


					Console.WriteLine("# プライマリキー一覧");
					Console.WriteLine();

					cmd.CommandText = $@"
SELECT 
	c.CONSTRAINT_SCHEMA,
	c.TABLE_NAME,
	c.COLUMN_NAME,
	c.ORDINAL_POSITION,
	cls.DATA_TYPE
FROM 
	INFORMATION_SCHEMA.TABLE_CONSTRAINTS p 
	INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE c ON c.CONSTRAINT_SCHEMA=p.CONSTRAINT_SCHEMA AND c.TABLE_NAME=p.TABLE_NAME AND c.CONSTRAINT_NAME=p.CONSTRAINT_NAME
	INNER JOIN INFORMATION_SCHEMA.COLUMNS cls ON cls.TABLE_SCHEMA=p.TABLE_SCHEMA AND cls.TABLE_NAME=c.TABLE_NAME AND cls.COLUMN_NAME=c.COLUMN_NAME
WHERE
	CONSTRAINT_TYPE='PRIMARY KEY'
ORDER by
	c.TABLE_SCHEMA, c.TABLE_NAME, c.COLUMN_NAME;
";
					ShowResult(cmd);
				}
			}
		}

		static void ShowResult(MySqlCommand cmd) {
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
				} while (dr.NextResult());
			}
		}
	}
}
