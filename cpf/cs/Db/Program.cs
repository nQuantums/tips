using System;

namespace Db {
	class Program {
		static class Cols {
			public static readonly Col<int> id = new Col<int>("id");
			public static readonly Col<string> name = new Col<string>("name");
			public static readonly Col<DateTime> birth_day = new Col<DateTime>("birth_day");
		}

		static class Tbls {
			public class Employee {
				public Col<int> id = Cols.id;
				public Col<string> name = Cols.name;
				public Col<DateTime> birth_day = Cols.birth_day;
			}

			public static readonly Tbl<Employee> employee = new Tbl<Employee>(
				"employee",
				c => new PrimaryKey(c.id),
				c => new Index(c.name)
			);
		}

		static void Main(string[] args) {
			Console.WriteLine(DateTime.MinValue);
			Console.WriteLine(DateTime.MaxValue);
			Console.WriteLine($"id={Tbls.employee.Cols.id}");

			var a = Tbls.employee.Alias("e");

			Console.WriteLine(a.CreateTableIfNotExists());


			//Console.WriteLine(tbl.GetType());
			//var tbl = Tbl.Def(
			//	"table1",
			//	new {
			//		id,
			//		name
			//	},
			//	new Cr(CrType.PrimaryKey, id),
			//	new Cr(CrType.Index, name)
			//);

			//var cols = tbl.ColsArray;
			//Console.WriteLine(cols[0]);
			//Console.WriteLine(cols[1]);
		}
	}
}
