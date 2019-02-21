using System;
using System.Linq;
using MySql.Data.MySqlClient;

namespace Db {
	class Program {
		static class Cols {
			public static readonly Col<int> id1 = new Col<int>("id1");
			public static readonly Col<int> id2 = new Col<int>("id2");
			public static readonly Col<int> id3 = new Col<int>("id3");
			public static readonly Col<int> id4 = new Col<int>("id4");
			public static readonly Col<int> id5 = new Col<int>("id5");
			public static readonly Col<int> id6 = new Col<int>("id6");
			public static readonly Col<int> id7 = new Col<int>("id7");
			public static readonly Col<int> id8 = new Col<int>("id8");
			public static readonly Col<string> name = new Col<string>("name", DbType.text.Len(32));
			public static readonly Col<DateTime> birth_day = new Col<DateTime>("birth_day");
			public static readonly Col<byte[]> binary_id = new Col<byte[]>("binary_id", DbType.varbinary.Len(1024));
			public static readonly Col<int> int_data = new Col<int>("int_data");
		}

		static class Tbls {
			public class Employee {
				public Col<int> id = Cols.id1;
				public Col<string> name = Cols.name;
				public Col<DateTime> birth_day = Cols.birth_day;
			}
			public static readonly Tbl<Employee> employee = new Tbl<Employee>(
				"employee",
				c => new PrimaryKey(c.id),
				c => new Index(c.name)
			);

			public class Test1 {
				public Col<int> id1 = Cols.id1;
				public Col<int> id2 = Cols.id2;
			}
			public static readonly Tbl<Test1> test1 = new Tbl<Test1>(
				"test1",
				c => new PrimaryKey(c.id1),
				c => new Index(c.id2)
			);

			public class Test2 {
				public Col<int> id2 = Cols.id2;
				public Col<int> id3 = Cols.id3;
			}
			public static readonly Tbl<Test2> test2 = new Tbl<Test2>(
				"test2",
				c => new PrimaryKey(c.id2),
				c => new Index(c.id3)
			);

			public class Test3 {
				public Col<int> id3 = Cols.id3;
				public Col<int> id4 = Cols.id4;
			}
			public static readonly Tbl<Test3> test3 = new Tbl<Test3>(
				"test3",
				c => new PrimaryKey(c.id3),
				c => new Index(c.id4)
			);

			public class Test4 {
				public Col<int> id4 = Cols.id4;
				public Col<int> id5 = Cols.id5;
			}
			public static readonly Tbl<Test4> test4 = new Tbl<Test4>(
				"test4",
				c => new PrimaryKey(c.id4),
				c => new Index(c.id5)
			);

			public class Test5 {
				public Col<int> id5 = Cols.id5;
				public Col<int> id6 = Cols.id6;
			}
			public static readonly Tbl<Test5> test5 = new Tbl<Test5>(
				"test5",
				c => new PrimaryKey(c.id5),
				c => new Index(c.id6)
			);

			public class Test6 {
				public Col<int> id6 = Cols.id6;
				public Col<int> id7 = Cols.id7;
			}
			public static readonly Tbl<Test6> test6 = new Tbl<Test6>(
				"test6",
				c => new PrimaryKey(c.id6),
				c => new Index(c.id7)
			);

			public class Test7 {
				public Col<int> id7 = Cols.id7;
				public Col<int> id8 = Cols.id8;
			}
			public static readonly Tbl<Test7> test7 = new Tbl<Test7>(
				"test7",
				c => new PrimaryKey(c.id7),
				c => new Index(c.id8)
			);

			public class Test8 {
				public Col<int> id8 = Cols.id8;
				public Col<int> int_data = Cols.int_data;
			}
			public static readonly Tbl<Test8> test8 = new Tbl<Test8>(
				"test8",
				c => new PrimaryKey(c.id8),
				c => new Index(c.int_data)
			);

			public class Test {
				public Col<byte[]> binary_id = Cols.binary_id;
				public Col<int> int_data = Cols.int_data;
			}
			public static readonly Tbl<Test> test = new Tbl<Test>(
				"Test",
				c => new PrimaryKey(c.binary_id),
				c => new Index(c.int_data)
			);
		}

		static void Main(string[] args) {
			var rnd = new Random();

			using (var con = new MySqlConnection("server=127.0.0.1;uid=root;pwd=Passw0rd!;database=test1")) {
				con.Open();

				using (var cmd = con.CreateCommand()) {
					cmd.CommandTimeout = 0;

					var t1 = Tbls.test1.As("t1");
					var t2 = Tbls.test2.As("t2");
					var t3 = Tbls.test3.As("t3");
					var t4 = Tbls.test4.As("t4");
					var t5 = Tbls.test5.As("t5");
					var t6 = Tbls.test6.As("t6");
					var t7 = Tbls.test7.As("t7");
					var t8 = Tbls.test8.As("t8");

					//Db.DropTableIfExists(cmd, Tbls.test1);
					//Db.DropTableIfExists(cmd, Tbls.test2);
					//Db.DropTableIfExists(cmd, Tbls.test3);
					//Db.DropTableIfExists(cmd, Tbls.test4);
					//Db.DropTableIfExists(cmd, Tbls.test5);
					//Db.DropTableIfExists(cmd, Tbls.test6);
					//Db.DropTableIfExists(cmd, Tbls.test7);
					//Db.DropTableIfExists(cmd, Tbls.test8);

					//Db.CreateTableIfNotExists(cmd, Tbls.test1);
					//Db.CreateTableIfNotExists(cmd, Tbls.test2);
					//Db.CreateTableIfNotExists(cmd, Tbls.test3);
					//Db.CreateTableIfNotExists(cmd, Tbls.test4);
					//Db.CreateTableIfNotExists(cmd, Tbls.test5);
					//Db.CreateTableIfNotExists(cmd, Tbls.test6);
					//Db.CreateTableIfNotExists(cmd, Tbls.test7);
					//Db.CreateTableIfNotExists(cmd, Tbls.test8);

					//var paramId1 = new Param(0);
					//var paramId2 = new Param(0);
					//var paramBulk = new Param[500, 2];

					//for (int i = 0; i < paramBulk.GetLength(0); i++) {
					//	paramBulk[i, 0] = new Param(0);
					//	paramBulk[i, 1] = new Param(0);
					//}

					//var insertBulk1 = Db.Sql(Db.InsertInto(t1, t1.Cols.id1, t1.Cols.id2), Db.Values(paramBulk));
					//var insertBulk2 = Db.Sql(Db.InsertInto(t2, t2.Cols.id2, t2.Cols.id3), Db.Values(paramBulk));
					//var insertBulk3 = Db.Sql(Db.InsertInto(t3, t3.Cols.id3, t3.Cols.id4), Db.Values(paramBulk));
					//var insertBulk4 = Db.Sql(Db.InsertInto(t4, t4.Cols.id4, t4.Cols.id5), Db.Values(paramBulk));
					//var insertBulk5 = Db.Sql(Db.InsertInto(t5, t5.Cols.id5, t5.Cols.id6), Db.Values(paramBulk));
					//var insertBulk6 = Db.Sql(Db.InsertInto(t6, t6.Cols.id6, t6.Cols.id7), Db.Values(paramBulk));
					//var insertBulk7 = Db.Sql(Db.InsertInto(t7, t7.Cols.id7, t7.Cols.id8), Db.Values(paramBulk));
					//var insertBulk8 = Db.Sql(Db.InsertInto(t8, t8.Cols.id8, t8.Cols.int_data), Db.Values(paramBulk));

					//var insert1 = Db.Sql(Db.InsertInto(t1, t1.Cols.id1, t1.Cols.id2), Db.Values(paramId1, paramId2));
					//var insert2 = Db.Sql(Db.InsertInto(t2, t2.Cols.id2, t2.Cols.id3), Db.Values(paramId1, paramId2));
					//var insert3 = Db.Sql(Db.InsertInto(t3, t3.Cols.id3, t3.Cols.id4), Db.Values(paramId1, paramId2));
					//var insert4 = Db.Sql(Db.InsertInto(t4, t4.Cols.id4, t4.Cols.id5), Db.Values(paramId1, paramId2));
					//var insert5 = Db.Sql(Db.InsertInto(t5, t5.Cols.id5, t5.Cols.id6), Db.Values(paramId1, paramId2));
					//var insert6 = Db.Sql(Db.InsertInto(t6, t6.Cols.id6, t6.Cols.id7), Db.Values(paramId1, paramId2));
					//var insert7 = Db.Sql(Db.InsertInto(t7, t7.Cols.id7, t7.Cols.id8), Db.Values(paramId1, paramId2));
					//var insert8 = Db.Sql(Db.InsertInto(t8, t8.Cols.id8, t8.Cols.int_data), Db.Values(paramId1, paramId2));

					//var count = 0;
					//for (int i = 0; i < 65536; i++) {
					//	paramBulk[count, 0].Value = i;
					//	paramBulk[count, 1].Value = i;
					//	count++;
					//	if (count == paramBulk.GetLength(0)) {
					//		count = 0;
					//		insertBulk1(cmd).ExecuteNonQuery();
					//		insertBulk2(cmd).ExecuteNonQuery();
					//		insertBulk3(cmd).ExecuteNonQuery();
					//		insertBulk4(cmd).ExecuteNonQuery();
					//		insertBulk5(cmd).ExecuteNonQuery();
					//		insertBulk6(cmd).ExecuteNonQuery();
					//		insertBulk7(cmd).ExecuteNonQuery();
					//		insertBulk8(cmd).ExecuteNonQuery();
					//	}
					//}
					//for (int i = 0; i < count; i++) {
					//	paramId1.Value = paramBulk[i, 0].Value;
					//	paramId2.Value = paramBulk[1, 1].Value;
					//	insert1(cmd).ExecuteNonQuery();
					//	insert2(cmd).ExecuteNonQuery();
					//	insert3(cmd).ExecuteNonQuery();
					//	insert4(cmd).ExecuteNonQuery();
					//	insert5(cmd).ExecuteNonQuery();
					//	insert6(cmd).ExecuteNonQuery();
					//	insert7(cmd).ExecuteNonQuery();
					//	insert8(cmd).ExecuteNonQuery();
					//}

					var selectTest1 = Db.Sql(
						Db.Select(t1.Cols),
						Db.From(t1)
					);

					var select = Db.Sql(
						Db.Select(),
						Db.From(t1),
						Db.InnerJoin(t2).On(t2.Cols.id2, "=", t1.Cols.id2),
						Db.InnerJoin(t3).On(t3.Cols.id3, "=", t2.Cols.id3),
						Db.InnerJoin(t4).On(t4.Cols.id4, "=", t3.Cols.id4),
						Db.InnerJoin(t5).On(t5.Cols.id5, "=", t4.Cols.id5),
						Db.InnerJoin(t6).On(t6.Cols.id6, "=", t5.Cols.id6),
						Db.InnerJoin(t7).On(t7.Cols.id7, "=", t6.Cols.id7),
						Db.InnerJoin(t8).On(t8.Cols.id8, "=", t7.Cols.id8)
					);


					var sw = new System.Diagnostics.Stopwatch();
					sw.Start();

					//using (var records = Db.Enumerate(selectTest1(cmd), t1.Cols)) {
					//	var count = 0;
					//	foreach (var r in records) {
					//		count++;
					//		//Console.WriteLine(r.id1.Value + ", " + r.id2.Value);
					//	}
					//	//Console.WriteLine(count);
					//}

					//using (var dr = select(cmd).ExecuteReader()) {
					//	while (dr.Read()) {
					//	}
					//}
					//using (var records = Db.Enumerate<int>(select(cmd), 0)) {
					//	foreach (var r in records) {
					//		//Console.WriteLine(r.i1 + " " + r.i2);
					//	}
					//}
					using (var records = Db.Enumerate(select(cmd), new { t1 = t1.Cols, t2 = t2.Cols, t3 = t3.Cols, t4 = t4.Cols, t5 = t5.Cols, t6 = t6.Cols, t7 = t7.Cols, t8 = t8.Cols })) {
						foreach (var r in records) {
							//Console.WriteLine(r.t8.int_data.Value);
						}
					}

					Console.WriteLine(sw.ElapsedMilliseconds);


					//Db.DropTableIfExists(cmd, Tbls.test);
					//Db.DropTableIfExists(cmd, Tbls.employee);
					//Db.CreateTableIfNotExists(cmd, Tbls.test);
					//Db.CreateTableIfNotExists(cmd, Tbls.employee);

					//var t = Tbls.test.Alias("t");
					//var paramValuesBulk = new Param[500][];
					//var paramBinaryId = new Param(null);
					//var paramIntValue = new Param(0);

					//for (int i = 0; i < paramValuesBulk.Length; i++) {
					//	paramValuesBulk[i] = new Param[] { new Param(null), new Param(0) };
					//}

					//var insertBulk = Db.Sql(
					//	Db.InsertInto(t, t.Cols.binary_id, t.Cols.int_data),
					//	Db.Values(paramValuesBulk),
					//	Db.OnDuplicateKeyUpdate()
					//		.Set(t.Cols.int_data, Db.Values(t.Cols.int_data))
					//);

					//var insert = Db.Sql(
					//	Db.InsertInto(t, t.Cols.binary_id, t.Cols.int_data),
					//	Db.Values(paramBinaryId, paramIntValue),
					//	Db.OnDuplicateKeyUpdate()
					//		.Set(t.Cols.int_data, Db.Values(t.Cols.int_data))
					//);

					//var select = Db.Sql(
					//	Db.Select(t.Cols.binary_id, t.Cols.int_data),
					//	Db.From(t)
					//);

					//var groupedSelect = Db.Sql(
					//	Db.Select(t.Cols.int_data),
					//	Db.From(t),
					//	Db.GroupBy(t.Cols.int_data)
					//);

					//var orderedSelect = Db.Sql(
					//	Db.Select(t.Cols.int_data),
					//	Db.From(t),
					//	Db.OrderBy(Db.Desc(t.Cols.int_data))
					//);

					//var count = 0;
					//for (int i = 0; i < 256; i++) {
					//	for (int j = 0; j < 256; j++) {
					//		var vb = paramValuesBulk[count++];
					//		vb[0].Value = new byte[] { (byte)i, (byte)j };
					//		vb[1].Value = rnd.Next() % 100;
					//		if (count == paramValuesBulk.Length) {
					//			count = 0;
					//			insertBulk(cmd).ExecuteNonQuery();
					//		}
					//	}
					//}
					//for (int i = 0; i < count; i++) {
					//	var vb = paramValuesBulk[i];
					//	paramBinaryId.Value = vb[0].Value;
					//	paramIntValue.Value = vb[1].Value;
					//	insert(cmd).ExecuteNonQuery();
					//}

					//using (var records = Db.Enumerate<int>(groupedSelect(cmd))) {
					//	count = 0;
					//	foreach (var r in records) {
					//		count++;
					//		//Console.WriteLine(r);
					//		//foreach (var b in r.value) {
					//		//	//Console.Write(" ");
					//		//	//Console.Write(b);
					//		//}
					//		//Console.WriteLine();
					//	}
					//	Console.WriteLine(count);
					//}
					////using (var records = Db.Enumerate(select(cmd), new { id = t.Cols.binary_id.Value, data = t.Cols.int_data.Value })) {
					////	foreach (var r in records) {
					////		Console.WriteLine(r.data);
					////		//foreach (var b in r.value) {
					////		//	//Console.Write(" ");
					////		//	//Console.Write(b);
					////		//}
					////		//Console.WriteLine();
					////	}
					////}
					////using (var records = Db.Enumerate<byte[]>(select(cmd), 0)) {
					////	foreach (var r in records) {
					////		foreach (var b in r) {
					////			//Console.Write(" ");
					////			//Console.Write(b);
					////		}
					////		//Console.WriteLine();
					////	}
					////}
				}
			}
		}
	}
}
