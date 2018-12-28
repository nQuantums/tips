using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Db {
	/// <summary>
	/// DB上での型
	/// </summary>
	public class DbType {
		public static readonly DbType int1 = new DbType("int1");
		public static readonly DbType int2 = new DbType("int2");
		public static readonly DbType int4 = new DbType("int4");
		public static readonly DbType int8 = new DbType("int8");
		public static readonly DbType text = new DbType("text");
		public static readonly DbType timestamp = new DbType("timestamp");

		public static DbType Map(Type type) {
			if (type == typeof(sbyte)) {
				return int1;
			} else if (type == typeof(short)) {
				return int2;
			} else if (type == typeof(int)) {
				return int4;
			} else if (type == typeof(long)) {
				return int8;
			} else if (type == typeof(string)) {
				return text;
			} else if (type == typeof(DateTime)) {
				return timestamp;
			} else {
				throw new ApplicationException("型 " + type + " に対応するDB上の型がありません。");
			}
		}

		/// <summary>
		/// 型名
		/// </summary>
		public readonly string Name;

		public DbType(string name) {
			this.Name = name;
		}

		public static implicit operator string(DbType value) {
			return value.Name;
		}

		public override string ToString() {
			return this.Name;
		}
	}

	/// <summary>
	/// 制約タイプ
	/// </summary>
	public enum CrType {
		/// <summary>
		/// プライマリキー
		/// </summary>
		PrimaryKey,

		/// <summary>
		/// インデックス
		/// </summary>
		Index,

		/// <summary>
		/// 自動採番
		/// </summary>
		AutoIncrement,
	}

	/// <summary>
	/// 制約
	/// </summary>
	public class Cr {
		/// <summary>
		/// 制約タイプ
		/// </summary>
		public readonly CrType Type;

		/// <summary>
		/// 制約対象列一覧
		/// </summary>
		public readonly Col[] Cols;

		/// <summary>
		/// コンストラクタ、制約タイプと対象列一覧を指定して初期化する
		/// </summary>
		/// <param name="type">制約タイプ</param>
		/// <param name="cols">対象列一覧</param>
		public Cr(CrType type, params Col[] cols) {
			this.Type = type;
			this.Cols = cols;
		}
	}

	/// <summary>
	/// データベースの列を指すクラス
	/// </summary>
	public class Col {
		/// <summary>
		/// 列名
		/// </summary>
		public readonly string Name;

		/// <summary>
		/// ` 括られた列名の取得
		/// </summary>
		public string NameQuoted {
			get {
				return '`' + this.Name + '`';
			}
		}

		/// <summary>
		/// ソースコード上での型
		/// </summary>
		public readonly Type CodeType;

		/// <summary>
		/// DB上での型
		/// </summary>
		public readonly DbType DbType;

		/// <summary>
		/// 制約
		/// </summary>
		public readonly Cr Constraint;

		/// <summary>
		/// コンストラクタ、列名と型を指定して初期化する
		/// </summary>
		/// <param name="name">列名</param>
		/// <param name="codeType">ソースコード上での型</param>
		/// <param name="dbType">DB上での型</param>
		public Col(string name, Type codeType, DbType dbType) {
			this.Name = name;
			this.CodeType = codeType;
			this.DbType = dbType;
		}

		/// <summary>
		/// コンストラクタ、制約として初期化する
		/// </summary>
		private Col(Cr constraint) {
			this.Constraint = constraint;
		}

		public static implicit operator string(Col value) {
			return value.Name;
		}

		public static implicit operator Col(Cr value) {
			return new Col(value);
		}

		public override string ToString() {
			return this.Name;
		}
	}

	/// <summary>
	/// データベースの列を指し且つ値も保持するクラス
	/// </summary>
	/// <typeparam name="T">データベース内で保持する値に対応するC#型</typeparam>
	public class Col<T> : Col {
		/// <summary>
		/// 値
		/// </summary>
		public T Value;

		/// <summary>
		/// コンストラクタ、列名を指定して初期化する
		/// </summary>
		/// <param name="name">列名</param>
		public Col(string name) : base(name, typeof(T), DbType.Map(typeof(T))) {
		}

		/// <summary>
		/// コンストラクタ、ベースとなる<see cref="Col{T}"/>と値を指定して初期化する
		/// </summary>
		/// <param name="baseCol">ベースとなる列</param>
		/// <param name="value">値</param>
		public Col(Col<T> baseCol, T value) : base(baseCol.Name, baseCol.CodeType, baseCol.DbType) {
			this.Value = value;
		}

		public static implicit operator string(Col<T> value) {
			return value.NameQuoted;
		}

		public override string ToString() {
			var obj = (object)this.Value;
			return obj != null ? obj.ToString() : "null";
		}

		/// <summary>
		/// 指定されたエイリアス内の列を指す文字列を取得する
		/// </summary>
		public string At(string table) {
			return table + "." + this.NameQuoted;
		}
	}

	/// <summary>
	/// テーブル又はエイリアスを指すクラス
	/// </summary>
	public class Tbl {
		/// <summary>
		/// テーブル名またはエイリアス
		/// </summary>
		public readonly string Name;

		/// <summary>
		/// 列一覧
		/// </summary>
		public readonly Col[] Cols;

		/// <summary>
		/// 制約一覧
		/// </summary>
		public readonly Cr[] Constraints;

		/// <summary>
		/// コンストラクタ、テーブル名（またはエイリアス）と列一覧を指定して初期化する
		/// </summary>
		/// <param name="name">テーブル名またはエイリアス</param>
		/// <param name="cols">列一覧</param>
		public Tbl(string name, params Col[] cols) {
			this.Name = name;
			this.Cols = cols.Where(c => c.Constraint == null).ToArray();
			this.Constraints = cols.Where(c => c.Constraint != null).Select(c => c.Constraint).ToArray();
		}

		/// <summary>
		/// クエリ式内で使用できる列名の取得
		/// </summary>
		/// <param name="col">列</param>
		/// <returns>列名</returns>
		public string this[Col col] {
			get {
				var index = Array.IndexOf(this.Cols, col);
				if (index < 0) {
					throw new ApplicationException("テーブルまたはエイリアス " + this.Name + " 内に列 " + col.NameQuoted + " は存在しません。");
				}
				return this.Name + "." + col.NameQuoted;
			}
		}

		public override string ToString() {
			return this.Name;
		}

		public static implicit operator string(Tbl value) {
			return value.Name;
		}

		public string CreateTableIfNotExists() {
			return Db.CreateTableIfNotExists(this.Name, this.Cols, this.Constraints);
		}
	}

	/// <summary>
	/// SQL文生成をサポートするクラス
	/// </summary>
	public static class Db {
		#region 内部型
		static class ParamAdderHolder<T> {
			public static readonly Action<MySqlParameterCollection, StringBuilder, T> Add;
			static ParamAdderHolder() {
				Add = Db.GenerateParamAdder<T>();
			}
		}
		#endregion

		#region 公開メソッド
		public static string Cols(params Col[] cols) {
			return string.Join(", ", cols.Select(c => c.Name));
		}

		public static string Cols(string tableName, params Col[] cols) {
			return string.Join(", ", cols.Select(c => tableName + "." + c.Name));
		}

		public static string Cols(params Tbl[] tbls) {
			var sb = new StringBuilder();
			foreach (var t in tbls) {
				if (sb.Length != 0) {
					sb.Append(", ");
				}
				sb.Append(string.Join(", ", t.Cols.Select(c => t.Name + "." + c.Name)));
			}
			return sb.ToString();
		}

		public static string Sql(params string[] modules) {
			return string.Join("\n", modules) + ";";
		}

		public static string CreateTableIfNotExists(string table, Col[] cols, params Cr[] constraints) {
			var sb = new StringBuilder();
			sb.AppendLine("CREATE TABLE IF NOT EXISTS " + table + "(");
			for (int i = 0; i < cols.Length; i++) {
				var col = cols[i];
				if (i != 0) {
					sb.Append(", ");
				}
				sb.Append(col.NameQuoted + " " + col.DbType);
				if (constraints.Where(c => c.Type == CrType.AutoIncrement && c.Cols.Contains(col)).Any()) {
					sb.Append(" AUTO_INCREMENT");
				}
				sb.AppendLine();
			}
			var pk = constraints.Where(c => c.Type == CrType.PrimaryKey).FirstOrDefault();
			if (pk != null) {
				sb.Append(", PRIMARY KEY(");
				for (int i = 0; i < pk.Cols.Length; i++) {
					if (i != 0) {
						sb.Append(", ");
					}
					sb.Append(pk.Cols[i].NameQuoted);
				}
				sb.AppendLine(")");
			}
			sb.AppendLine(");");
			return sb.ToString();
		}


		/// <summary>
		/// 指定値を<see cref="MySqlParameterCollection"/>に入れた状態にしてその値を指す文字列を生成する
		/// </summary>
		/// <typeparam name="T">値型</typeparam>
		/// <param name="parameters">ここにパラメータとして値が入る</param>
		/// <param name="sb">(@v0, @v1) の様に値を指す文字列が構築される</param>
		/// <param name="value">指定値</param>
		public static void BuildValueUsingParameters<T>(MySqlParameterCollection parameters, StringBuilder sb, T value) {
			ParamAdderHolder<T>.Add(parameters, sb, value);
		}

		/// <summary>
		/// 指定テーブルへの値挿入コマンドを指定<see cref="MySqlCommand"/>に構築する
		/// </summary>
		/// <typeparam name="T">挿入値型</typeparam>
		/// <param name="cmd">構築先コマンド</param>
		/// <param name="table">挿入先テーブル</param>
		/// <param name="cols">挿入列一覧</param>
		/// <param name="value">挿入値</param>
		public static void BuildInsertInto<T>(MySqlCommand cmd, string table, Col[] cols, T value) {
			var parameters = cmd.Parameters;
			parameters.Clear();

			var sb = new StringBuilder();
			sb.Append("INSERT INTO " + table + "(");
			for (int i = 0; i < cols.Length; i++) {
				var col = cols[i];
				if (i != 0) {
					sb.Append(",");
				}
				sb.Append(col.NameQuoted);
			}
			sb.AppendLine(")");
			sb.AppendLine("VALUES");

			BuildValueUsingParameters(parameters, sb, value);
			sb.Append(";");

			cmd.CommandText = sb.ToString();
		}

		/// <summary>
		/// 指定テーブルへの値列挿入コマンドを指定<see cref="MySqlCommand"/>に構築する
		/// </summary>
		/// <typeparam name="T">挿入値型</typeparam>
		/// <param name="cmd">構築先コマンド</param>
		/// <param name="table">挿入先テーブル</param>
		/// <param name="cols">挿入列一覧</param>
		/// <param name="values">挿入値一覧</param>
		public static void BuildInsertInto<T>(MySqlCommand cmd, string table, Col[] cols, IEnumerable<T> values) {
			var parameters = cmd.Parameters;
			parameters.Clear();

			var sb = new StringBuilder();
			sb.Append("INSERT INTO " + table + "(");
			for (int i = 0; i < cols.Length; i++) {
				var col = cols[i];
				if (i != 0) {
					sb.Append(",");
				}
				sb.Append(col.NameQuoted);
			}
			sb.AppendLine(")");
			sb.AppendLine("VALUES");

			var first = true;
			foreach (var value in values) {
				if (first) {
					first = false;
				} else {
					sb.Append(",");
				}
				BuildValueUsingParameters(parameters, sb, value);
			}
			sb.Append(";");

			cmd.CommandText = sb.ToString();
		}



		public static string SelectAll() {
			return "SELECT *";
		}

		public static string Select(params Col[] cols) {
			var sb = new StringBuilder();
			sb.Append("SELECT ");
			for (int i = 0; i < cols.Length; i++) {
				var c = cols[i];
				if (i != 0) {
					sb.Append(", ");
				}
				sb.Append(c.Name);
			}
			return sb.ToString();
		}

		public static string Select(params Tbl[] tblcols) {
			var sb = new StringBuilder();
			sb.Append("SELECT ");
			for (int i = 0; i < tblcols.Length; i++) {
				var t = tblcols[i];
				if (i != 0) {
					sb.Append(", ");
				}
				sb.Append(string.Join(", ", t.Cols.Select(c => t.Name + "." + c.Name)));
			}
			return sb.ToString();
		}

		public static string From(string tableName) {
			return "FROM " + tableName;
		}

		public static string From(string tableName, string alias) {
			return "FROM " + tableName + " " + alias;
		}

		public static string From(string tableName, Tbl alias) {
			return "FROM " + tableName + " " + alias.Name;
		}

		public static string JoinUsing(string tableName, string alias, Col col) {
			return "INNER JOIN " + tableName + " " + alias + " USING(" + col.Name + ")";
		}

		public static string Where(string expression) {
			return "WHERE " + expression;
		}

		public static string Parenthesize(string expression) {
			return "(" + expression + ")";
		}

		/// <summary>
		/// 指定された複数の式を AND で連結する
		/// </summary>
		public static string And(params string[] expressions) {
			return string.Join(" AND ", expressions.Select(e => Parenthesize(e)));
		}

		/// <summary>
		/// 指定された複数の式を OR で連結する
		/// </summary>
		public static string Or(params string[] expressions) {
			return string.Join(" OR ", expressions.Select(e => Parenthesize(e)));
		}

		/// <summary>
		/// 指定された式が NULL または長さ0なら真となる
		/// </summary>
		public static string NullOrEmpty(string expression) {
			return "(" + expression + " IS NULL OR length(" + expression + ")=0)";
		}

		/// <summary>
		/// <see cref="MySqlDataReader"/>から値を読み込み<see cref="Col{T}"/>を取得するデリゲートを作成する
		/// </summary>
		/// <typeparam name="T">クラス型</typeparam>
		/// <param name="columnsExpression">() => new { title, date } の様な匿名クラスを生成する式</param>
		/// <returns>デリゲート</returns>
		public static Func<MySqlDataReader, T> GenerateGetter<T>(Expression<Func<T>> columnsExpression) {
			var type = typeof(T);

			// new 演算子でクラスを生成するもの以外はエラーとする
			var body = columnsExpression.Body;
			if (body.NodeType != ExpressionType.New) {
				throw new ApplicationException();
			}

			// クラスのプロパティ数とコンストラクタ引数の数が異なるならエラーとする
			var newexpr = body as NewExpression;
			var args = newexpr.Arguments;
			var properties = type.GetProperties();
			if (args.Count != properties.Length) {
				throw new ApplicationException(type + " のプロパティ数とコンストラクタ引数の数が一致しません。");
			}

			var propertyTypes = properties.Select(p => p.PropertyType).ToArray();
			var ctor = type.GetConstructor(propertyTypes);
			if (ctor == null) {
				throw new ApplicationException(type + " にプロパティ値を引数として受け取るコンストラクタがありません。");
			}

			// コンストラクタに渡す引数を作成
			var argExprs = new Expression[properties.Length];
			var paramDr = Expression.Parameter(typeof(MySqlDataReader));
			for (int i = 0; i < properties.Length; i++) {
				var p = properties[i];
				var pt = p.PropertyType;
				if (!pt.IsGenericType || pt.GetGenericTypeDefinition() != typeof(Col<>)) {
					throw new ApplicationException(type + " の " + p.Name + " が未対応の型 " + pt + " となっています。Col<T> でなければなりません。");
				}
				var ct = pt.GetGenericArguments()[0];
				var nct = Nullable.GetUnderlyingType(ct);
				var t = nct ?? ct;
				var name = "";

				if (t == typeof(string)) {
					name = "String";
				} else if (t == typeof(bool)) {
					name = "Bool";
				} else if (t == typeof(short)) {
					name = "Int16";
				} else if (t == typeof(int)) {
					name = "Int32";
				} else if (t == typeof(DateTime)) {
					name = "DateTime";
				} else if (t == typeof(Guid)) {
					name = "Guid";
				} else {
					throw new ApplicationException(type + " の " + p.Name + " の値が未対応の型 " + t + " となっています。");
				}
				if (nct != null) {
					name = "Nullable" + name;
				}
				name = "Get" + name;

				var getter = typeof(Db).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);
				if (getter == null) {
					throw new ApplicationException("内部エラー");
				}

				var col = Expression.Lambda(args[i]).Compile().DynamicInvoke() as Col;
				if (col == null) {
					throw new ApplicationException("内部エラー");
				}

				var pctor = pt.GetConstructor(new Type[] { pt, ct });
				if (pctor == null) {
					throw new ApplicationException("内部エラー");
				}

				argExprs[i] = Expression.New(pctor, Expression.Constant(col), Expression.Call(null, getter, paramDr, Expression.Constant(col.Name)));
			}

			// コンストラクタ呼び出しをコンパイル
			return Expression.Lambda<Func<MySqlDataReader, T>>(Expression.New(ctor, argExprs), paramDr).Compile();
		}

		/// <summary>
		/// <typeparamref name="T"/>のプロパティを<see cref="MySqlParameterCollection"/>へ設定するデリゲートを作成する
		/// </summary>
		/// <typeparam name="T">クラス型</typeparam>
		/// <returns>デリゲート</returns>
		public static Action<MySqlParameterCollection, StringBuilder, T> GenerateParamAdder<T>() {
			var type = typeof(T);
			var paramsType = typeof(MySqlParameterCollection);
			var sbType = typeof(StringBuilder);
			var paramParams = Expression.Parameter(paramsType);
			var paramSb = Expression.Parameter(sbType);
			var paramValue = Expression.Parameter(type);
			var name = Expression.Parameter(typeof(string));
			var properties = type.GetProperties();
			var intToString = typeof(int).GetMethod("ToString", new Type[0]);
			var addWithValue = typeof(MySqlParameterCollection).GetMethod("AddWithValue", new Type[] { typeof(string), typeof(object) });
			var countProperty = paramsType.GetProperty("Count");
			var stringConcat = typeof(string).GetMethod("Concat", new Type[] { typeof(string), typeof(string) });
			var appendToSb = sbType.GetMethod("Append", new Type[] { typeof(string) });
			var expressions = new List<Expression>();

			expressions.Add(Expression.Call(paramSb, appendToSb, Expression.Constant("(")));
			for (int i = 0; i < properties.Length; i++) {
				var p = properties[i];
				var pt = p.PropertyType;

				expressions.Add(Expression.Assign(name, Expression.Call(null, stringConcat, Expression.Constant("@v"), Expression.Call(Expression.Property(paramParams, countProperty), intToString))));

				if (i != 0) {
					expressions.Add(Expression.Call(paramSb, appendToSb, Expression.Constant(", ")));
				}
				expressions.Add(Expression.Call(paramSb, appendToSb, name));

				var value = Expression.Property(paramValue, p);
				if (pt.IsGenericType && pt.GetGenericTypeDefinition() == typeof(Col<>)) {
					value = Expression.Field(value, pt.GetField("Value"));
				}
				expressions.Add(Expression.Call(paramParams, addWithValue, name, Expression.Convert(value, typeof(object))));
			}
			expressions.Add(Expression.Call(paramSb, appendToSb, Expression.Constant(")")));

			var block = Expression.Block(new ParameterExpression[] { name }, expressions);

			return Expression.Lambda<Action<MySqlParameterCollection, StringBuilder, T>>(block, new[] { paramParams, paramSb, paramValue }).Compile();
		}
		#endregion

		#region 非公開メソッド
		static string GetString(MySqlDataReader dr, string colName) {
			try {
				var v = dr[colName];
				return v == DBNull.Value ? null : v.ToString();
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}
		static bool GetBool(MySqlDataReader dr, string colName) {
			try {
				return Convert.ToBoolean(dr[colName]);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}
		static short GetInt16(MySqlDataReader dr, string colName) {
			try {
				return Convert.ToInt16(dr[colName]);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}
		static int GetInt32(MySqlDataReader dr, string colName) {
			try {
				return Convert.ToInt32(dr[colName]);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}
		static DateTime GetDateTime(MySqlDataReader dr, string colName) {
			try {
				return Convert.ToDateTime(dr[colName]);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}
		static Guid GetGuid(MySqlDataReader dr, string colName) {
			try {
				return (Guid)dr[colName];
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}

		static string GetNullableString(MySqlDataReader dr, string colName) {
			try {
				var v = dr[colName];
				return v == DBNull.Value ? null : v.ToString();
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}
		static bool? GetNullableBool(MySqlDataReader dr, string colName) {
			try {
				var v = dr[colName];
				return v == DBNull.Value ? default(bool?) : Convert.ToBoolean(v);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}
		static short? GetNullableInt16(MySqlDataReader dr, string colName) {
			try {
				var v = dr[colName];
				return v == DBNull.Value ? default(short?) : Convert.ToInt16(v);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}
		static int? GetNullableInt32(MySqlDataReader dr, string colName) {
			try {
				var v = dr[colName];
				return v == DBNull.Value ? default(int?) : Convert.ToInt32(v);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}
		static DateTime? GetNullableDateTime(MySqlDataReader dr, string colName) {
			try {
				var v = dr[colName];
				return v == DBNull.Value ? default(DateTime?) : Convert.ToDateTime(v);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}
		static Guid? GetNullableGuid(MySqlDataReader dr, string colName) {
			try {
				var v = dr[colName];
				return v == DBNull.Value ? default(Guid?) : (Guid)v;
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}
		#endregion
	}
}
