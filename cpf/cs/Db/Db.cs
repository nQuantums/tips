﻿using MySql.Data.MySqlClient;
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
			} else if (type == typeof(sbyte?)) {
				return int1;
			} else if (type == typeof(short?)) {
				return int2;
			} else if (type == typeof(int?)) {
				return int4;
			} else if (type == typeof(long?)) {
				return int8;
			} else if (type == typeof(DateTime?)) {
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
	/// プライマリキー制約
	/// </summary>
	public class PrimaryKey : Cr {
		public PrimaryKey(params Col[] cols) : base(CrType.PrimaryKey, cols) {
		}
	}

	/// <summary>
	/// インデックス制約
	/// </summary>
	public class Index : Cr {
		public Index(params Col[] cols) : base(CrType.Index, cols) {
		}
	}

	/// <summary>
	/// 自動採番制約
	/// </summary>
	public class AutoIncrement : Cr {
		public AutoIncrement(params Col[] cols) : base(CrType.AutoIncrement, cols) {
		}
	}

	/// <summary>
	/// データベースの列を指すクラス
	/// </summary>
	public class Col {
		/// <summary>
		/// エイリアス名
		/// </summary>
		public string Alias { get; protected set; }

		/// <summary>
		/// 列名
		/// </summary>
		public readonly string Name;

		/// <summary>
		/// ` で括られた列名の取得
		/// </summary>
		public string NameQuoted {
			get {
				if (string.IsNullOrEmpty(this.Alias)) {
					return '`' + this.Name + '`';
				} else {
					return '`' + this.Alias + "`.`" + this.Name + '`';
				}
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

		public static implicit operator string(Col value) {
			return value.NameQuoted;
		}

		public override string ToString() {
			return this.NameQuoted;
		}
	}

	/// <summary>
	/// データベースの列を指し且つ値も保持するクラス
	/// </summary>
	/// <typeparam name="T">データベース内で保持する値に対応するC#型</typeparam>
	public class Col<T> : Col {
		/// <summary>
		/// 基となった<see cref="Col{T}"/>
		/// </summary>
		readonly Col<T> BaseCol;

		/// <summary>
		/// 値
		/// </summary>
		public T Value;

		/// <summary>
		/// 基礎となる<see cref="Col{T}"/>の取得
		/// </summary>
		public Col<T> UnderlyingCol {
			get {
				if (this.BaseCol != null) {
					return this.BaseCol;
				}
				return this;
			}
		}

		/// <summary>
		/// コンストラクタ、列名を指定して初期化する
		/// </summary>
		/// <param name="name">列名</param>
		public Col(string name) : base(name, typeof(T), DbType.Map(typeof(T))) {
		}

		/// <summary>
		/// コンストラクタ、ベースとなる<see cref="Col{T}"/>を指定して初期化する
		/// </summary>
		/// <param name="alias">エイリアス名</param>
		/// <param name="baseCol">ベースとなる列</param>
		public Col(string alias, Col<T> baseCol) : base(baseCol.Name, baseCol.CodeType, baseCol.DbType) {
			this.Alias = alias;
			this.BaseCol = baseCol;
		}

		/// <summary>
		/// コンストラクタ、ベースとなる<see cref="Col{T}"/>と値を指定して初期化する
		/// </summary>
		/// <param name="baseCol">ベースとなる列</param>
		/// <param name="value">値</param>
		public Col(Col<T> baseCol, T value) : base(baseCol.Name, baseCol.CodeType, baseCol.DbType) {
			this.BaseCol = baseCol;
			this.Value = value;
		}


		public static implicit operator string(Col<T> value) {
			return value.NameQuoted;
		}

		public override string ToString() {
			return this.NameQuoted;
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
		/// 基となった<see cref="Tbl"/>
		/// </summary>
		protected Tbl BaseTbl;

		/// <summary>
		/// 基礎となる<see cref="Tbl"/>の取得
		/// </summary>
		public Tbl UnderlyingTbl {
			get {
				if (this.BaseTbl != null) {
					return this.BaseTbl;
				}
				return this;
			}
		}

		/// <summary>
		/// テーブル名またはエイリアス
		/// </summary>
		public string Name { get; protected set; }

		/// <summary>
		/// 列一覧
		/// </summary>
		public Col[] ColsArray { get; protected set; }

		/// <summary>
		/// 制約一覧
		/// </summary>
		public Cr[] Constraints { get; protected set; }

		/// <summary>
		/// コンストラクタ、テーブル名（またはエイリアス）と列一覧を指定して初期化する
		/// </summary>
		/// <param name="name">テーブル名またはエイリアス</param>
		/// <param name="cols">列一覧</param>
		public Tbl(string name, params Col[] cols) {
			this.Name = name;
			this.ColsArray = cols;
			this.Constraints = new Cr[0];
		}

		/// <summary>
		/// コンストラクタ、テーブル名（またはエイリアス）と列一覧を指定して初期化する
		/// </summary>
		/// <param name="name">テーブル名またはエイリアス</param>
		/// <param name="cols">列一覧値</param>
		public Tbl(string name, object cols) {
			this.Name = name;
			SetCols(cols);
		}

		/// <summary>
		/// クエリ式内で使用できる列名の取得
		/// </summary>
		/// <param name="col">列</param>
		/// <returns>列名</returns>
		public string this[Col col] {
			get {
				var index = Array.IndexOf(this.ColsArray, col);
				if (index < 0) {
					throw new ApplicationException("テーブルまたはエイリアス " + this.Name + " 内に列 " + col.NameQuoted + " は存在しません。");
				}
				return this.Name + "." + col.NameQuoted;
			}
		}

		protected virtual void SetCols(object cols) {
		}

		public override string ToString() {
			return this.Name;
		}

		public static implicit operator string(Tbl value) {
			return value.Name;
		}

		public string CreateTableIfNotExists() {
			return Db.CreateTableIfNotExists(this.Name, this.ColsArray, this.Constraints);
		}
	}

	/// <summary>
	/// テーブル又はエイリアスを指すクラス
	/// </summary>
	public class Tbl<T> : Tbl where T : class, new() {
		/// <summary>
		/// 列一覧を含む値
		/// </summary>
		public T Cols { get; protected set; }

		/// <summary>
		/// 基礎となる<see cref="Tbl{T}"/>の取得
		/// </summary>
		public new Tbl<T> UnderlyingTbl {
			get {
				return this.UnderlyingTbl as Tbl<T>;
			}
		}

		/// <summary>
		/// コンストラクタ、テーブル名（またはエイリアス）と列一覧を指定して初期化する
		/// </summary>
		/// <param name="name">テーブル名またはエイリアス</param>
		/// <param name="constraintCreators">制約作成デリゲート一覧</param>
		public Tbl(string name, params Func<T, Cr>[] constraintCreators) : base(name, new T()) {
			this.Constraints = constraintCreators.Select(c => c(this.Cols)).ToArray();
		}

		/// <summary>
		/// 指定名のエイリアスを作成
		/// </summary>
		/// <param name="aliasName">エイリアス名</param>
		/// <returns>エイリアス</returns>
		public Tbl<T> Alias(string aliasName) {
			var c = this.MemberwiseClone() as Tbl<T>;
			c.BaseTbl = this;
			c.Name = aliasName;
			AliasingCols<T>.Invoke(c);
			c.ColsArray = ColsGetter<T>.Invoke(c.Cols);
			return c;
		}

		protected override void SetCols(object cols) {
			this.Cols = (T)cols;
			this.ColsArray = ColsGetter<T>.Invoke(this.Cols);
		}

		public static implicit operator string(Tbl<T> value) {
			return value.Name;
		}
	}

	public class Prm {
		public object Value;

		public Prm(object value) {
			this.Value = value;
		}
	}

	public class Where {
		public object[] Expressions;

		public Where(object expression, params object[] expressions) {
			var exprs = new object[expressions.Length + 1];
			exprs[0] = expression;
			if (expressions.Length != 0) {
				Array.Copy(expressions, 0, exprs, 1, expressions.Length);
			}
			this.Expressions = exprs;
		}
	}

	/// <summary>
	/// SQL文生成をサポートするクラス
	/// </summary>
	public static class Db {
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
				sb.Append(string.Join(", ", t.ColsArray.Select(c => t.Name + "." + c.Name)));
			}
			return sb.ToString();
		}

		public static string Sql(params string[] modules) {
			return string.Join("\n", modules) + ";";
		}

		public static void Sql(MySqlCommand cmd, params object[] modules) {
			var prms = new Dictionary<Prm, string>();

			// パラメータを文字列に変換する処理
			Func<Prm, string> prmToStr = p => {
				string paramName;
				if (!prms.TryGetValue(p, out paramName)) {
					prms[p] = paramName = "@v" + prms.Count;
				}
				return paramName;
			};

			// オブジェクトを文字列に変換する処理
			Func<object, string> objToStr = null;
			objToStr = obj => {
				Where w;
				Prm p;
				if (obj == null) {
					return "NULL";
				} else if ((w = obj as Where) != null) {
					var b = new StringBuilder();
					b.Append("WHERE");
					foreach (var e in w.Expressions) {
						b.Append(" ");
						b.Append(objToStr(e));
					}
					return b.ToString();
				} else if ((p = obj as Prm) != null) {
					return prmToStr(p);
				} else {
					return obj.ToString();
				}
			};

			// 各モジュールを文字列に変換してコマンド文字列を生成する
			var sb = new StringBuilder();
			foreach (var m in modules) {
				sb.AppendLine(objToStr(m));
			}
			sb.Append(";");
			cmd.CommandText = sb.ToString();

			// 使用されたパラメータを設定する
			var parameters = cmd.Parameters;
			parameters.Clear();
			foreach (var p in prms) {
				parameters.AddWithValue(p.Value, p.Key.Value);
			}
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
			PropertiesToParam<T>.Invoke(parameters, sb, value);
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
				sb.Append(c.NameQuoted);
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
				sb.Append(string.Join(", ", t.ColsArray.Select(c => t.Name + "." + c.Name)));
			}
			return sb.ToString();
		}

		public static string From(Tbl tbl) {
			return "FROM " + tbl.UnderlyingTbl.Name + " " + tbl.Name;
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

		public static Where Where(object expression, params object[] expressions) {
			return new Where(expression, expressions);
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
		#endregion
	}

	/// <summary>
	/// 指定値から<see cref="Col{T}"/>型のフィールドとプロパティを抜き出し配列化する
	/// </summary>
	/// <typeparam name="T"><see cref="Col{T}"/>プロパティを持つ型</typeparam>
	public static class ColsGetter<T> {
		public static readonly Func<T, Col[]> Invoke;

		static ColsGetter() {
			var type = typeof(T);
			var input = Expression.Parameter(type);
			var fields = type.GetFields();
			var properties = type.GetProperties();
			var colFields = fields.Where(f => f.IsPublic && f.FieldType.IsGenericType && f.FieldType.GetGenericTypeDefinition() == typeof(Col<>)).ToArray();
			var colProperties = properties.Where(p => p.GetSetMethod(false).IsPublic && p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Col<>)).ToArray();
			var arrayType = typeof(Col[]);
			var variable = Expression.Parameter(arrayType);
			var expressions = new List<Expression>();
			var arrayAccesses = new List<Expression>();

			expressions.Add(Expression.Assign(variable, Expression.New(arrayType.GetConstructor(new Type[] { typeof(int) }), Expression.Constant(colFields.Length + colProperties.Length))));

			foreach (var f in colFields) {
				var a = Expression.ArrayAccess(variable, Expression.Constant(arrayAccesses.Count));
				arrayAccesses.Add(a);
				expressions.Add(Expression.Assign(a, Expression.Field(input, f)));
			}
			foreach (var p in colProperties) {
				var a = Expression.ArrayAccess(variable, Expression.Constant(arrayAccesses.Count));
				arrayAccesses.Add(a);
				expressions.Add(Expression.Assign(a, Expression.Property(input, p)));
			}

			expressions.Add(variable);

			Invoke = Expression.Lambda<Func<T, Col[]>>(Expression.Block(new ParameterExpression[] { variable }, expressions), input).Compile();
		}
	}

	/// <summary>
	/// <typeparamref name="T"/>のプロパティを<see cref="MySqlParameterCollection"/>へ追加するデリゲートを作成する
	/// </summary>
	/// <typeparam name="T">クラス型</typeparam>
	public static class PropertiesToParam<T> {
		public static readonly Action<MySqlParameterCollection, StringBuilder, T> Invoke;

		static PropertiesToParam() {
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

			Invoke = Expression.Lambda<Action<MySqlParameterCollection, StringBuilder, T>>(block, new[] { paramParams, paramSb, paramValue }).Compile();
		}
	}

	/// <summary>
	/// <typeparamref name="T"/>内にフィールドまたはプロパティとして存在する<see cref="Col{S}"/>にエイリアスをセットする
	/// </summary>
	/// <typeparam name="T">フィールドまたはプロパティとして<see cref="Col{S}"/>を持つクラス型</typeparam>
	public static class AliasingCols<T> where T : class, new() {
		public static readonly Action<Tbl<T>> Invoke;

		static AliasingCols() {
			var colsType = typeof(T);
			var tblType = typeof(Tbl<T>);
			var tbl = Expression.Parameter(tblType);
			var fieldsOfCols = colsType.GetFields().Where(f => f.IsPublic && f.FieldType.IsGenericType && f.FieldType.GetGenericTypeDefinition() == typeof(Col<>)).ToArray();
			var propertiesOfCols = colsType.GetProperties().Where(p => p.GetSetMethod(false).IsPublic && p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Col<>)).ToArray();
			var aliasName = Expression.Parameter(typeof(string));
			var cols = Expression.Parameter(colsType);
			var expressions = new List<Expression>();

			expressions.Add(Expression.Assign(aliasName, Expression.Property(tbl, tblType.GetProperty("Name"))));
			expressions.Add(Expression.Assign(cols, Expression.Property(tbl, tblType.GetProperty("Cols"))));

			foreach (var f in fieldsOfCols) {
				var t = f.FieldType;
				var ctorForAlias = t.GetConstructor(new Type[] { typeof(string), t });
				expressions.Add(Expression.Assign(Expression.Field(cols, f), Expression.New(ctorForAlias, aliasName, Expression.Field(cols, f))));
			}
			foreach (var p in propertiesOfCols) {
				var t = p.PropertyType;
				var ctorForAlias = t.GetConstructor(new Type[] { typeof(string), t });
				expressions.Add(Expression.Assign(Expression.Property(cols, p), Expression.New(ctorForAlias, aliasName, Expression.Property(cols, p))));
			}

			Invoke = Expression.Lambda<Action<Tbl<T>>>(Expression.Block(new ParameterExpression[] { aliasName, cols }, expressions), tbl).Compile();
		}
	}

	/// <summary>
	/// <see cref="MySqlDataReader"/>から値を読み込み<see cref="Col{T}"/>を取得するデリゲートを作成する
	/// </summary>
	/// <typeparam name="T">クラス型</typeparam>
	public static class GetFromDataReader {
		/// <summary>
		/// <see cref="MySqlDataReader"/>から値を読み込み<see cref="Col{T}"/>を取得するデリゲートを作成する
		/// </summary>
		/// <typeparam name="T">クラス型</typeparam>
		/// <param name="columnsExpression">() => new { title, date } の様な匿名クラスを生成する式</param>
		/// <returns>デリゲート</returns>
		public static Func<MySqlDataReader, T> Generate<T>(Expression<Func<T>> columnsExpression) {
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

				var getter = typeof(GetFromDataReader).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);
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
	}
}
