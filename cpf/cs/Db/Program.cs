using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

		public class ClassA {
			public DateTime D;
			//public Guid G;
			//public string S;
			//public ClassB B;
			//public Col<byte[]> ColBytes;
			//public Col<int> ColInt;
		}

		public class ClassB {
			public int I1;
			//public int I2;
			//public int I3;
			//public int? A;
			//public int B;
			//public string[] Strs;
			//public byte[] Bytes;
			//public int[] Ints;
		}

		static bool r;

		static void Main(string[] args) {
			//var a = new KeyOf<ClassB>(new ClassB { I1 = 1 });
			//var b = new KeyOf<ClassB>(new ClassB { I1 = 1 });
			////var a = new KeyOf<ClassB>(new ClassB { A = 1, B = 2, Strs = new[] { "a", "b" }, Bytes = new byte[] { 1, 2, 3 }, Ints = new[] { 1, 2, 3 } });
			////var b = new KeyOf<ClassB>(new ClassB { A = 1, B = 2, Strs = new[] { "a", "b" }, Bytes = new byte[] { 1, 2, 3 }, Ints = new[] { 1, 2, 3 } });
			//var s = new HashSet<KeyOf<ClassB>>();
			//s.Add(a);
			//s.Add(b);
			//Console.WriteLine(s.Count);
			//var sw1 = new System.Diagnostics.Stopwatch();
			//for (int loop = 0; loop < 10; loop++) {
			//	sw1.Reset();
			//	sw1.Start();
			//	for (int i = 0; i < 100000000; i++) {
			//		r = a == b;
			//	}
			//	Console.WriteLine(sw1.ElapsedMilliseconds);
			//}
			//return;

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

					//Tbls.test1.DropTableIfExists(cmd);
					//Tbls.test2.DropTableIfExists(cmd);
					//Tbls.test3.DropTableIfExists(cmd);
					//Tbls.test4.DropTableIfExists(cmd);
					//Tbls.test5.DropTableIfExists(cmd);
					//Tbls.test6.DropTableIfExists(cmd);
					//Tbls.test7.DropTableIfExists(cmd);
					//Tbls.test8.DropTableIfExists(cmd);

					//Tbls.test1.CreateTableIfNotExists(cmd);
					//Tbls.test2.CreateTableIfNotExists(cmd);
					//Tbls.test3.CreateTableIfNotExists(cmd);
					//Tbls.test4.CreateTableIfNotExists(cmd);
					//Tbls.test5.CreateTableIfNotExists(cmd);
					//Tbls.test6.CreateTableIfNotExists(cmd);
					//Tbls.test7.CreateTableIfNotExists(cmd);
					//Tbls.test8.CreateTableIfNotExists(cmd);

					var paramId1 = new Param(0);
					var paramId2 = new Param(0);
					var paramBulk1 = new Param[500, 2];
					var paramBulk2 = new Param[500, 2];
					var paramBulk3 = new Param[500, 2];
					var paramBulk4 = new Param[500, 2];
					var paramBulk5 = new Param[500, 2];
					var paramBulk6 = new Param[500, 2];
					var paramBulk7 = new Param[500, 2];
					var paramBulk8 = new Param[500, 2];

					for (int i = 0; i < paramBulk1.GetLength(0); i++) {
						paramBulk1[i, 0] = new Param(0);
						paramBulk1[i, 1] = new Param(0);
						paramBulk2[i, 0] = new Param(0);
						paramBulk2[i, 1] = new Param(0);
						paramBulk3[i, 0] = new Param(0);
						paramBulk3[i, 1] = new Param(0);
						paramBulk4[i, 0] = new Param(0);
						paramBulk4[i, 1] = new Param(0);
						paramBulk5[i, 0] = new Param(0);
						paramBulk5[i, 1] = new Param(0);
						paramBulk6[i, 0] = new Param(0);
						paramBulk6[i, 1] = new Param(0);
						paramBulk7[i, 0] = new Param(0);
						paramBulk7[i, 1] = new Param(0);
						paramBulk8[i, 0] = new Param(0);
						paramBulk8[i, 1] = new Param(0);
					}

					var insertBulk1 = Db.InsertInto(t1, t1.Cols.id1, t1.Cols.id2).Values(paramBulk1).OnDuplicateKeyUpdate(Db.Assign(t1.Cols.id2, Db.Values(t1.Cols.id2))).ToFunc();
					var insertBulk2 = Db.InsertInto(t2, t2.Cols.id2, t2.Cols.id3).Values(paramBulk2).ToFunc();
					var insertBulk3 = Db.InsertInto(t3, t3.Cols.id3, t3.Cols.id4).Values(paramBulk3).ToFunc();
					var insertBulk4 = Db.InsertInto(t4, t4.Cols.id4, t4.Cols.id5).Values(paramBulk4).ToFunc();
					var insertBulk5 = Db.InsertInto(t5, t5.Cols.id5, t5.Cols.id6).Values(paramBulk5).ToFunc();
					var insertBulk6 = Db.InsertInto(t6, t6.Cols.id6, t6.Cols.id7).Values(paramBulk6).ToFunc();
					var insertBulk7 = Db.InsertInto(t7, t7.Cols.id7, t7.Cols.id8).Values(paramBulk7).ToFunc();
					var insertBulk8 = Db.InsertInto(t8, t8.Cols.id8, t8.Cols.int_data).Values(paramBulk8).ToFunc();

					var insert1 = Db.InsertInto(t1, t1.Cols.id1, t1.Cols.id2).Values(paramId1, paramId2).OnDuplicateKeyUpdate(Db.Assign(t1.Cols.id2, Db.Values(t1.Cols.id2))).ToFunc();
					var insert2 = Db.InsertInto(t2, t2.Cols.id2, t2.Cols.id3).Values(paramId1, paramId2).ToFunc();
					var insert3 = Db.InsertInto(t3, t3.Cols.id3, t3.Cols.id4).Values(paramId1, paramId2).ToFunc();
					var insert4 = Db.InsertInto(t4, t4.Cols.id4, t4.Cols.id5).Values(paramId1, paramId2).ToFunc();
					var insert5 = Db.InsertInto(t5, t5.Cols.id5, t5.Cols.id6).Values(paramId1, paramId2).ToFunc();
					var insert6 = Db.InsertInto(t6, t6.Cols.id6, t6.Cols.id7).Values(paramId1, paramId2).ToFunc();
					var insert7 = Db.InsertInto(t7, t7.Cols.id7, t7.Cols.id8).Values(paramId1, paramId2).ToFunc();
					var insert8 = Db.InsertInto(t8, t8.Cols.id8, t8.Cols.int_data).Values(paramId1, paramId2).ToFunc();

					var count = 0;
					for (int i = 0; i < 100; i++) {
						paramBulk1[count, 0].Value = i;
						paramBulk1[count, 1].Value = i * 2;
						paramBulk2[count, 0].Value = i * 2;
						paramBulk2[count, 1].Value = i * 3;
						paramBulk3[count, 0].Value = i * 3;
						paramBulk3[count, 1].Value = i * 4;
						paramBulk4[count, 0].Value = i * 4;
						paramBulk4[count, 1].Value = i * 5;
						paramBulk5[count, 0].Value = i * 5;
						paramBulk5[count, 1].Value = i * 6;
						paramBulk6[count, 0].Value = i * 6;
						paramBulk6[count, 1].Value = i * 7;
						paramBulk7[count, 0].Value = i * 7;
						paramBulk7[count, 1].Value = i * 8;
						paramBulk8[count, 0].Value = i * 8;
						paramBulk8[count, 1].Value = i * 9;
						count++;
						if (count == paramBulk1.GetLength(0)) {
							count = 0;
							insertBulk1(cmd).ExecuteNonQuery();
							insertBulk2(cmd).ExecuteNonQuery();
							insertBulk3(cmd).ExecuteNonQuery();
							insertBulk4(cmd).ExecuteNonQuery();
							insertBulk5(cmd).ExecuteNonQuery();
							insertBulk6(cmd).ExecuteNonQuery();
							insertBulk7(cmd).ExecuteNonQuery();
							insertBulk8(cmd).ExecuteNonQuery();
						}
					}
					for (int i = 0; i < count; i++) {
						paramId1.Value = paramBulk1[i, 0].Value;
						paramId2.Value = paramBulk1[i, 1].Value;
						insert1(cmd).ExecuteNonQuery();

						paramId1.Value = paramBulk2[i, 0].Value;
						paramId2.Value = paramBulk2[i, 1].Value;
						insert2(cmd).ExecuteNonQuery();

						paramId1.Value = paramBulk3[i, 0].Value;
						paramId2.Value = paramBulk3[i, 1].Value;
						insert3(cmd).ExecuteNonQuery();

						paramId1.Value = paramBulk4[i, 0].Value;
						paramId2.Value = paramBulk4[i, 1].Value;
						insert4(cmd).ExecuteNonQuery();

						paramId1.Value = paramBulk5[i, 0].Value;
						paramId2.Value = paramBulk5[i, 1].Value;
						insert5(cmd).ExecuteNonQuery();

						paramId1.Value = paramBulk6[i, 0].Value;
						paramId2.Value = paramBulk6[i, 1].Value;
						insert6(cmd).ExecuteNonQuery();

						paramId1.Value = paramBulk7[i, 0].Value;
						paramId2.Value = paramBulk7[i, 1].Value;
						insert7(cmd).ExecuteNonQuery();

						paramId1.Value = paramBulk8[i, 0].Value;
						paramId2.Value = paramBulk8[i, 1].Value;
						insert8(cmd).ExecuteNonQuery();
					}

					//var selectCols = new { t1 = t1.Cols, t2 = t2.Cols, t3 = t3.Cols, t4 = t4.Cols, t5 = t5.Cols, t6 = t6.Cols, t7 = t7.Cols, t8 = t8.Cols };
					var selectCols = new { t8 = t8.Cols };
					var select =
						Db.Select(selectCols)
						.From(t1)
						.InnerJoin(t2).On(t2.Cols.id2, "=", t1.Cols.id2)
						.InnerJoin(t3).On(t3.Cols.id3, "=", t2.Cols.id3)
						.InnerJoin(t4).On(t4.Cols.id4, "=", t3.Cols.id4)
						.InnerJoin(t5).On(t5.Cols.id5, "=", t4.Cols.id5)
						.InnerJoin(t6).On(t6.Cols.id6, "=", t5.Cols.id6)
						.InnerJoin(t7).On(t7.Cols.id7, "=", t6.Cols.id7)
						.InnerJoin(t8).On(t8.Cols.id8, "=", t7.Cols.id8)
						.ToFunc();


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

					using (var records = Db.Enumerate(select(cmd), selectCols)) {
						foreach (var r in records) {
							//Console.WriteLine(r.t8.int_data.Value);
						}
					}

					Console.WriteLine(sw.ElapsedMilliseconds);


					Tbls.test.DropTableIfExists(cmd);
					Tbls.employee.DropTableIfExists(cmd);
					Tbls.test.CreateTableIfNotExists(cmd);
					Tbls.employee.CreateTableIfNotExists(cmd);

					var t = Tbls.test.As("t");
					var paramValuesBulk = new Param[500][];
					var paramBinaryId = new Param(null);
					var paramIntValue = new Param(0);

					for (int i = 0; i < paramValuesBulk.Length; i++) {
						paramValuesBulk[i] = new Param[] { new Param(null), new Param(0) };
					}

					var upsert =
						Db.InsertInto(t, t.Cols.binary_id, t.Cols.int_data)
						.Values(paramValuesBulk)
						.OnDuplicateKeyUpdate(
							Db.Assign(t.Cols.int_data, Db.Values(t.Cols.int_data))
						)
						.ToFunc();

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
