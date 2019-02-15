using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using MySql.Data.MySqlClient;

namespace SusDbHelperTest {
	/// <summary>
	/// <see cref="DbType"/>に付与するフラグ
	/// </summary>
	[Flags]
	public enum DbTypeFlags {
		/// <summary>
		/// 型が可変長である事を示す
		/// </summary>
		IsVariableLengthType = 1 << 0,
	}

	/// <summary>
	/// DB上での型
	/// </summary>
	public class DbType {
		public static readonly DbType int1 = new DbType("int1");
		public static readonly DbType int2 = new DbType("int2");
		public static readonly DbType int4 = new DbType("int4");
		public static readonly DbType int8 = new DbType("int8");
		public static readonly DbType text = new DbType("text", DbTypeFlags.IsVariableLengthType);
		public static readonly DbType timestamp = new DbType("timestamp");
		public static readonly DbType binary = new DbType("binary", DbTypeFlags.IsVariableLengthType);

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
		public string Name { get; private set; }

		/// <summary>
		/// 型名
		/// </summary>
		public DbTypeFlags Flags { get; private set; }

		/// <summary>
		/// 桁数又は要素数
		/// </summary>
		public int Length { get; private set; }

		/// <summary>
		/// コンストラクタ、型名と桁数又は要素数を指定して初期化する
		/// </summary>
		/// <param name="name">型名</param>
		/// <param name="flags">型に対する追加情報のフラグ</param>
		/// <param name="length">桁数又は要素数</param>
		public DbType(string name, DbTypeFlags flags = 0, int length = 0) {
			this.Name = name;
			this.Flags = flags;
			this.Length = length;
		}

		public static implicit operator string(DbType value) {
			return value.ToString();
		}

		public override string ToString() {
			return this.Length == 0 ? this.Name : this.Name + "(" + this.Length + ")";
		}

		/// <summary>
		/// 指定の属性をオーバーライドしたクローンを作成する
		/// </summary>
		/// <param name="flagsAdd">型に対する追加情報のフラグ</param>
		/// <param name="length">桁数又は要素数</param>
		/// <returns>クローン</returns>
		public DbType As(DbTypeFlags flagsAdd, int length) {
			var c = this.MemberwiseClone() as DbType;
			c.Flags |= flagsAdd;
			c.Length = length;
			return c;
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
		/// ユニークキー制約
		/// </summary>
		Unique,

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
	/// ユニークキー制約
	/// </summary>
	public class Unique : Cr {
		public Unique(params Col[] cols) : base(CrType.Unique, cols) {
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
		/// 基となった<see cref="Col"/>
		/// </summary>
		public Col BaseCol { get; protected set; }

		/// <summary>
		/// 基礎となる<see cref="Col"/>の取得
		/// </summary>
		public Col Underlying {
			get {
				if (this.BaseCol != null) {
					return this.BaseCol;
				}
				return this;
			}
		}

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
				return '`' + this.Name + '`';
			}
		}

		/// <summary>
		/// ` で括られたエイリアス名も含む列名の取得
		/// </summary>
		public string NameQuotedWithAlias {
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

		public SqlAs As(string alias) {
			return new SqlAs(alias, this);
		}

		public static implicit operator string(Col value) {
			return value.NameQuotedWithAlias;
		}

		public override string ToString() {
			return this.NameQuotedWithAlias;
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
		/// 基礎となる<see cref="Col{T}"/>の取得
		/// </summary>
		public new Col<T> Underlying {
			get {
				var u = this.BaseCol as Col<T>;
				if (u != null) {
					return u;
				}
				return this;
			}
		}

		/// <summary>
		/// コンストラクタ、列名を指定して初期化する、型は<see cref="DbType.Map(Type)"/>により自動的に判断される
		/// </summary>
		/// <param name="name">列名</param>
		public Col(string name) : base(name, typeof(T), DbType.Map(typeof(T))) {
		}

		/// <summary>
		/// コンストラクタ、列名と型を指定して初期化する
		/// </summary>
		/// <param name="name">列名</param>
		/// <param name="dbType">DB内での型</param>
		public Col(string name, DbType dbType) : base(name, typeof(T), dbType) {
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
			return value.NameQuotedWithAlias;
		}

		public override string ToString() {
			return this.NameQuotedWithAlias;
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
		public Tbl Underlying {
			get {
				if (this.BaseTbl != null) {
					return this.BaseTbl;
				}
				return this;
			}
		}

		/// <summary>
		/// スキーマ名
		/// </summary>
		public string Schema { get; protected set; }

		/// <summary>
		/// テーブル名またはエイリアス
		/// </summary>
		public string Name { get; protected set; }

		/// <summary>
		/// ` で括られたスキーマ名も含むテーブル名の取得
		/// </summary>
		public string FullNameQuoted {
			get {
				var sb = new StringBuilder();
				if (this.Schema != null) {
					sb.Append('`');
					sb.Append(this.Schema);
					sb.Append("`.`");
					sb.Append(this.Name);
					sb.Append('`');
				} else {
					sb.Append('`');
					sb.Append(this.Name);
					sb.Append('`');
				}
				return sb.ToString();
			}
		}

		/// <summary>
		/// ` で括られたテーブル名の取得
		/// </summary>
		public string NameQuoted {
			get {
				var sb = new StringBuilder();
				sb.Append('`');
				sb.Append(this.Name);
				sb.Append('`');
				return sb.ToString();
			}
		}

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
		/// コンストラクタ、スキーマ名、テーブル名（またはエイリアス）と列一覧を指定して初期化する
		/// </summary>
		/// <param name="schema">スキーマ名</param>
		/// <param name="name">テーブル名またはエイリアス</param>
		/// <param name="cols">列一覧</param>
		public Tbl(string schema, string name, params Col[] cols) {
			this.Schema = schema;
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
		/// コンストラクタ、スキーマ名、テーブル名（またはエイリアス）と列一覧を指定して初期化する
		/// </summary>
		/// <param name="schema">スキーマ名</param>
		/// <param name="name">テーブル名またはエイリアス</param>
		/// <param name="cols">列一覧値</param>
		public Tbl(string schema, string name, object cols) {
			this.Schema = schema;
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
		public new Tbl<T> Underlying {
			get {
				return base.Underlying as Tbl<T>;
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
		/// コンストラクタ、スキーマ名、テーブル名（またはエイリアス）と列一覧を指定して初期化する
		/// </summary>
		/// <param name="schema">スキーマ名</param>
		/// <param name="name">テーブル名またはエイリアス</param>
		/// <param name="constraintCreators">制約作成デリゲート一覧</param>
		public Tbl(string schema, string name, params Func<T, Cr>[] constraintCreators) : base(schema, name, new T()) {
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
			c.Schema = null;
			c.Name = aliasName;
			c.Cols = AliasingCols<T>.Invoke(c.Cols, aliasName);
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

	public class Param {
		public object Value;

		public Param(object value) {
			this.Value = value;
		}
	}

	[Flags]
	public enum ToStringFlags {
		AsStringLiteral = 1 << 0,
		WithAlias = 1 << 1,
		Underlying = 1 << 2,
	}

	public class SqlBuffer {
		public readonly Dictionary<Param, string> Params = new Dictionary<Param, string>();

		public string GetPrmName(Param prm) {
			string paramName;
			if (!this.Params.TryGetValue(prm, out paramName)) {
				this.Params[prm] = paramName = "@v" + this.Params.Count;
			}
			return paramName;
		}

		public string ToString(object element, ToStringFlags flags = 0) {
			ISqlElement sm;
			Param p;
			Col c;
			Tbl t;
			if (element == null) {
				return QuoteIfNeeded("NULL", (flags & ToStringFlags.AsStringLiteral) != 0);
			} else if ((sm = element as ISqlElement) != null) {
				return QuoteIfNeeded(sm.Build(this, flags), (flags & ToStringFlags.AsStringLiteral) != 0);
			} else if ((c = element as Col) != null) {
				if ((flags & ToStringFlags.Underlying) != 0) {
					c = c.Underlying;
				}
				if ((flags & ToStringFlags.WithAlias) != 0) {
					return c.NameQuotedWithAlias;
				} else {
					return c.NameQuoted;
				}
			} else if ((t = element as Tbl) != null) {
				if ((flags & ToStringFlags.WithAlias) != 0 && t.FullNameQuoted != t.Underlying.FullNameQuoted) {
					return t.Underlying.FullNameQuoted + " " + t.FullNameQuoted;
				}
				if ((flags & ToStringFlags.Underlying) != 0) {
					t = t.Underlying;
				}
				return t.FullNameQuoted;
			} else if ((p = element as Param) != null) {
				return GetPrmName(p);
			} else {
				return QuoteIfNeeded(element.ToString(), (flags & ToStringFlags.AsStringLiteral) != 0);
			}
		}

		static string QuoteIfNeeded(string value, bool needQuote = false) {
			return needQuote ? "'" + value + "'" : value;
		}
	}

	public interface ISqlElement {
		string Build(SqlBuffer sqlBuffer, ToStringFlags flags);
	}

	public class SqlSelect : ISqlElement {
		public object[] Cols;

		public SqlSelect(params object[] cols) {
			this.Cols = cols;
		}

		public string Build(SqlBuffer sqlBuffer, ToStringFlags flags) {
			var sb = new StringBuilder();

			sb.Append("SELECT ");

			if (this.Cols.Length == 0) {
				sb.Append("*");
			} else {
				for (int i = 0; i < this.Cols.Length; i++) {
					if (i != 0) {
						sb.Append(", ");
					}
					sb.Append(sqlBuffer.ToString(this.Cols[i], flags | ToStringFlags.WithAlias));
				}
			}

			return sb.ToString();
		}
	}

	public class SqlFrom : ISqlElement {
		public object Tbl;

		public SqlFrom(object tbl) {
			this.Tbl = tbl;
		}

		public string Build(SqlBuffer sqlBuffer, ToStringFlags flags) {
			Tbl tbl;
			var sb = new StringBuilder();

			sb.Append("FROM ");

			if ((tbl = this.Tbl as Tbl) != null) {
				var utbl = tbl.Underlying;
				if (tbl.Name != utbl.Name) {
					sb.Append(utbl.FullNameQuoted);
					sb.Append(" ");
					sb.Append(tbl.Name);
				} else {
					sb.Append(tbl.FullNameQuoted);
				}
			} else {
				sb.Append(sqlBuffer.ToString(this.Tbl, flags));
			}

			return sb.ToString();
		}
	}

	public class SqlWhere : ISqlElement {
		public object[] Expressions;

		public SqlWhere(object expression, params object[] expressions) {
			var exprs = new object[expressions.Length + 1];
			exprs[0] = expression;
			if (expressions.Length != 0) {
				Array.Copy(expressions, 0, exprs, 1, expressions.Length);
			}
			this.Expressions = exprs;
		}

		public string Build(SqlBuffer sqlBuffer, ToStringFlags flags) {
			var sb = new StringBuilder();
			sb.Append("WHERE");
			foreach (var e in this.Expressions) {
				sb.Append(" ");
				sb.Append(sqlBuffer.ToString(e, flags | ToStringFlags.WithAlias));
			}
			return sb.ToString();
		}
	}

	public class SqlExpr : ISqlElement {
		public object[] Expressions;

		public SqlExpr(object expression, params object[] expressions) {
			var exprs = new object[expressions.Length + 1];
			exprs[0] = expression;
			if (expressions.Length != 0) {
				Array.Copy(expressions, 0, exprs, 1, expressions.Length);
			}
			this.Expressions = exprs;
		}

		public string Build(SqlBuffer sqlBuffer, ToStringFlags flags) {
			var sb = new StringBuilder();
			for (int i = 0; i < this.Expressions.Length; i++) {
				if (i != 0) {
					sb.Append(" ");
				}
				sb.Append(sqlBuffer.ToString(this.Expressions[i], flags | ToStringFlags.WithAlias));
			}
			return sb.ToString();
		}
	}

	public class SqlAs : ISqlElement {
		public string Alias;
		public object[] Expressions;

		public SqlAs(string alias, object expression, params object[] expressions) {
			this.Alias = alias;

			var exprs = new object[expressions.Length + 1];
			exprs[0] = expression;
			if (expressions.Length != 0) {
				Array.Copy(expressions, 0, exprs, 1, expressions.Length);
			}
			this.Expressions = exprs;
		}

		public string Build(SqlBuffer sqlBuffer, ToStringFlags flags) {
			var sb = new StringBuilder();
			for (int i = 0; i < this.Expressions.Length; i++) {
				if (i != 0) {
					sb.Append(" ");
				}
				sb.Append(sqlBuffer.ToString(this.Expressions[i], flags));
			}
			sb.Append(" AS ");
			sb.Append(this.Alias);
			return sb.ToString();
		}
	}

	public class SqlInsertInto : ISqlElement {
		public object Tbl;
		public object[] Cols;

		public SqlInsertInto(object tbl, params object[] cols) {
			this.Tbl = tbl;
			this.Cols = cols;
		}

		public string Build(SqlBuffer sqlBuffer, ToStringFlags flags) {
			var sb = new StringBuilder();
			sb.Append("INSERT INTO ");
			sb.Append(sqlBuffer.ToString(this.Tbl, ToStringFlags.Underlying));
			sb.Append("(");
			for (int i = 0; i < this.Cols.Length; i++) {
				if (i != 0) {
					sb.Append(", ");
				}
				var col = this.Cols[i];
				Col c;
				if ((c = col as Col) != null) {
					sb.Append(c.NameQuoted);
				} else {
					sb.Append(col.ToString());
				}
			}
			sb.Append(")");
			return sb.ToString();
		}
	}

	public class SqlValues : ISqlElement {
		public object[][] Values;

		public SqlValues(params object[] value) {
			this.Values = new object[][] { value };
		}

		public SqlValues(object[][] values) {
			this.Values = values;
		}

		public string Build(SqlBuffer sqlBuffer, ToStringFlags flags) {
			var sb = new StringBuilder();
			sb.Append("VALUES");
			sb.AppendLine();
			for (int i = 0; i < this.Values.Length; i++) {
				if (i != 0) {
					sb.Append(",");
				} else {
					sb.Append(" ");
				}
				var value = this.Values[i];
				sb.Append("(");
				for (int j = 0; j < value.Length; j++) {
					if (j != 0) {
						sb.Append(", ");
					}
					sb.Append(sqlBuffer.ToString(value[j], flags));
				}
				sb.AppendLine(")");
			}
			return sb.ToString();
		}
	}

	public class SqlLike : ISqlElement {
		public object Expression;
		public object Pattern;

		public SqlLike(object expression, object pattern) {
			this.Expression = expression;
			this.Pattern = pattern;
		}

		public string Build(SqlBuffer sqlBuffer, ToStringFlags flags) {
			var sb = new StringBuilder();
			sb.Append(sqlBuffer.ToString(this.Expression, flags | ToStringFlags.AsStringLiteral | ToStringFlags.WithAlias));
			sb.Append(" LIKE ");
			sb.Append(sqlBuffer.ToString(this.Pattern, flags | ToStringFlags.AsStringLiteral | ToStringFlags.WithAlias));
			return sb.ToString();
		}
	}

	public class SqlDeleteFrom : ISqlElement {
		public object Tbl;

		public SqlDeleteFrom(object tbl) {
			this.Tbl = tbl;
		}

		public string Build(SqlBuffer sqlBuffer, ToStringFlags flags) {
			var sb = new StringBuilder();
			sb.Append("DELETE FROM ");
			sb.Append(sqlBuffer.ToString(this.Tbl, flags | ToStringFlags.Underlying));
			return sb.ToString();
		}
	}

	public enum JoinType {
		Inner,
		Left,
	}

	public class SqlJoin : ISqlElement {
		public JoinType JoinType;
		public bool IsUsing;
		public object Tbl;
		public object Col;
		public object[] Expressions;

		public SqlJoin(JoinType joinType, object tbl) {
			this.JoinType = joinType;
			this.Tbl = tbl;
		}

		public SqlJoin Using(object col) {
			this.IsUsing = true;
			this.Col = col;
			return this;
		}

		public SqlJoin On(params object[] expressions) {
			this.IsUsing = false;
			this.Expressions = expressions;
			return this;
		}

		public string Build(SqlBuffer sqlBuffer, ToStringFlags flags) {
			var sb = new StringBuilder();
			sb.Append(this.JoinType == JoinType.Inner ? "INNER JOIN" : "LEFT JOIN");
			sb.Append(" ");
			sb.Append(sqlBuffer.ToString(this.Tbl, flags | ToStringFlags.WithAlias));
			if (this.IsUsing) {
				if (this.Col == null) {
					throw new ApplicationException("JOIN USING に対して列が指定されていません。");
				}
				sb.Append(" USING(");
				sb.Append(sqlBuffer.ToString(this.Col, flags | ToStringFlags.Underlying));
				sb.Append(")");
			} else {
				if (this.Expressions == null) {
					throw new ApplicationException("JOIN ON に対して結合式が指定されていません。");
				}
				sb.Append(" ON ");
				for (int i = 0; i < this.Expressions.Length; i++) {
					if (i != 0) {
						sb.Append(" ");
					}
					sb.Append(sqlBuffer.ToString(this.Expressions[i], flags | ToStringFlags.WithAlias));
				}
			}
			return sb.ToString();
		}
	}

	public class SqlAnd : ISqlElement {
		public object[] Expressions;

		public SqlAnd(params object[] expressions) {
			this.Expressions = expressions;
		}

		public string Build(SqlBuffer sqlBuffer, ToStringFlags flags) {
			var sb = new StringBuilder();
			for (int i = 0; i < this.Expressions.Length; i++) {
				if (i != 0) {
					sb.Append(" AND ");
				}
				sb.Append("(" + sqlBuffer.ToString(this.Expressions[i], flags) + ")");
			}
			return sb.ToString();
		}
	}

	public class SqlOr : ISqlElement {
		public object[] Expressions;

		public SqlOr(params object[] expressions) {
			this.Expressions = expressions;
		}

		public string Build(SqlBuffer sqlBuffer, ToStringFlags flags) {
			var sb = new StringBuilder();
			for (int i = 0; i < this.Expressions.Length; i++) {
				if (i != 0) {
					sb.Append(" OR ");
				}
				sb.Append("(" + sqlBuffer.ToString(this.Expressions[i], flags) + ")");
			}
			return sb.ToString();
		}
	}

	public class SqlOnDuplicateKeyUpdate : ISqlElement {
		public List<Tuple<object, object>> ColAndValues = new List<Tuple<object, object>>();

		public SqlOnDuplicateKeyUpdate() {
		}

		public SqlOnDuplicateKeyUpdate Set(object col, object value) {
			this.ColAndValues.Add(new Tuple<object, object>(col, value));
			return this;
		}

		public string Build(SqlBuffer sqlBuffer, ToStringFlags flags) {
			var sb = new StringBuilder();
			sb.Append("ON DUPLICATE KEY UPDATE ");
			for (int i = 0; i < this.ColAndValues.Count; i++) {
				var cav = this.ColAndValues[i];
				if (i != 0) {
					sb.Append(", ");
				}
				sb.Append(sqlBuffer.ToString(cav.Item1));
				sb.Append("=");
				sb.Append(sqlBuffer.ToString(cav.Item2));
			}
			return sb.ToString();
		}
	}

	public class SqlLastInsertId : ISqlElement {
		public object Expr;

		public SqlLastInsertId(object expr = null) {
			this.Expr = expr;
		}

		public string Build(SqlBuffer sqlBuffer, ToStringFlags flags) {
			var sb = new StringBuilder();
			sb.Append("LAST_INSERT_ID(");
			if (this.Expr != null) {
				sb.Append(sqlBuffer.ToString(this.Expr));
			}
			sb.Append(")");
			return sb.ToString();
		}
	}

	/// <summary>
	/// SQL文生成をサポートするクラス
	/// </summary>
	public static class Db {
		#region 公開メソッド
		/// <summary>
		/// 指定のSQL文モジュールからSQL文を生成し<see cref="MySqlCommand"/>に適用するデリゲートを取得する
		/// </summary>
		/// <param name="elements">SQL文モジュール</param>
		/// <returns><see cref="MySqlCommand"/>にSQL文を適用するデリゲート</returns>
		public static Action<MySqlCommand> Sql(params object[] elements) {
			var buffer = new SqlBuffer();

			// 各モジュールを文字列に変換してSQL分を生成する
			var sb = new StringBuilder();
			for (int i = 0; i < elements.Length; i++) {
				if (i < elements.Length - 1) {
					sb.AppendLine(buffer.ToString(elements[i]));
				} else {
					sb.Append(buffer.ToString(elements[i]));
				}
			}
			sb.Append(";");
			var commandText = sb.ToString();

			// SQLコマンドに内容を適用するデリゲート作成
			Action<MySqlCommand> applyToCmd = cmd => {
				// 生成されたSQL分を適用
				cmd.CommandText = commandText;

				// 使用されたパラメータを設定する
				var parameters = cmd.Parameters;
				parameters.Clear();
				foreach (var p in buffer.Params) {
					parameters.AddWithValue(p.Value, p.Key.Value);
				}
			};

			return applyToCmd;
		}

		//public static Func<Param, Action<MySqlCommand, T1>> Sql<T1>(params object[] elements) {
		//	var applier = Sql(elements);
		//	return (param1) => {
		//		return (cmd, arg1) => {
		//			param1.Value = arg1;
		//			applier(cmd);
		//		};
		//	};
		//}

		//public static Func<Param, Param, Action<MySqlCommand, T1, T2>> Sql<T1, T2>(params object[] elements) {
		//	var applier = Sql(elements);
		//	return (param1, param2) => {
		//		return (cmd, arg1, arg2) => {
		//			param1.Value = arg1;
		//			param2.Value = arg2;
		//			applier(cmd);
		//		};
		//	};
		//}

		//public static Func<Param, Param, Param, Action<MySqlCommand, T1, T2, T3>> Sql<T1, T2, T3>(params object[] elements) {
		//	var applier = Sql(elements);
		//	return (param1, param2, param3) => {
		//		return (cmd, arg1, arg2, arg3) => {
		//			param1.Value = arg1;
		//			param2.Value = arg2;
		//			param3.Value = arg3;
		//			applier(cmd);
		//		};
		//	};
		//}

		//public static Func<Param, Param, Param, Param, Action<MySqlCommand, T1, T2, T3, T4>> Sql<T1, T2, T3, T4>(params object[] elements) {
		//	var applier = Sql(elements);
		//	return (param1, param2, param3, param4) => {
		//		return (cmd, arg1, arg2, arg3, arg4) => {
		//			param1.Value = arg1;
		//			param2.Value = arg2;
		//			param3.Value = arg3;
		//			param4.Value = arg4;
		//			applier(cmd);
		//		};
		//	};
		//}

		//public static Func<Param, Param, Param, Param, Param, Action<MySqlCommand, T1, T2, T3, T4, T5>> Sql<T1, T2, T3, T4, T5>(params object[] elements) {
		//	var applier = Sql(elements);
		//	return (param1, param2, param3, param4, param5) => {
		//		return (cmd, arg1, arg2, arg3, arg4, arg5) => {
		//			param1.Value = arg1;
		//			param2.Value = arg2;
		//			param3.Value = arg3;
		//			param4.Value = arg4;
		//			param5.Value = arg5;
		//			applier(cmd);
		//		};
		//	};
		//}

		//public static Func<Param, Param, Param, Param, Param, Param, Action<MySqlCommand, T1, T2, T3, T4, T5, T6>> Sql<T1, T2, T3, T4, T5, T6>(params object[] elements) {
		//	var applier = Sql(elements);
		//	return (param1, param2, param3, param4, param5, param6) => {
		//		return (cmd, arg1, arg2, arg3, arg4, arg5, arg6) => {
		//			param1.Value = arg1;
		//			param2.Value = arg2;
		//			param3.Value = arg3;
		//			param4.Value = arg4;
		//			param5.Value = arg5;
		//			param6.Value = arg6;
		//			applier(cmd);
		//		};
		//	};
		//}

		//public static Func<Param, Param, Param, Param, Param, Param, Param, Action<MySqlCommand, T1, T2, T3, T4, T5, T6, T7>> Sql<T1, T2, T3, T4, T5, T6, T7>(params object[] elements) {
		//	var applier = Sql(elements);
		//	return (param1, param2, param3, param4, param5, param6, param7) => {
		//		return (cmd, arg1, arg2, arg3, arg4, arg5, arg6, arg7) => {
		//			param1.Value = arg1;
		//			param2.Value = arg2;
		//			param3.Value = arg3;
		//			param4.Value = arg4;
		//			param5.Value = arg5;
		//			param6.Value = arg6;
		//			param7.Value = arg7;
		//			applier(cmd);
		//		};
		//	};
		//}

		//public static Func<Param, Param, Param, Param, Param, Param, Param, Param, Action<MySqlCommand, T1, T2, T3, T4, T5, T6, T7, T8>> Sql<T1, T2, T3, T4, T5, T6, T7, T8>(params object[] elements) {
		//	var applier = Sql(elements);
		//	return (param1, param2, param3, param4, param5, param6, param7, param8) => {
		//		return (cmd, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8) => {
		//			param1.Value = arg1;
		//			param2.Value = arg2;
		//			param3.Value = arg3;
		//			param4.Value = arg4;
		//			param5.Value = arg5;
		//			param6.Value = arg6;
		//			param7.Value = arg7;
		//			param8.Value = arg8;
		//			applier(cmd);
		//		};
		//	};
		//}

		/// <summary>
		/// 指定列の SELECT を表すSQL要素を生成する
		/// </summary>
		/// <param name="cols">列一覧、数が0なら * 扱いになる</param>
		/// <returns><see cref="SqlSelect"/></returns>
		public static SqlSelect Select(params object[] cols) {
			return new SqlSelect(cols);
		}

		/// <summary>
		/// 指定テーブルからの FROM を表すSQL要素を生成する
		/// </summary>
		/// <param name="tbl">取得元テーブル</param>
		/// <returns><see cref="SqlFrom"/></returns>
		public static SqlFrom From(object tbl) {
			return new SqlFrom(tbl);
		}

		/// <summary>
		/// WHERE を表すSQL要素を生成する
		/// </summary>
		/// <param name="expression">式</param>
		/// <param name="expressions">さらに結合する式一覧</param>
		/// <returns><see cref="SqlWhere "/></returns>
		public static SqlWhere Where(object expression, params object[] expressions) {
			return new SqlWhere(expression, expressions);
		}

		/// <summary>
		/// 式を表すSQL要素を生成する
		/// </summary>
		/// <param name="expression">式</param>
		/// <param name="expressions">さらに結合する式一覧</param>
		/// <returns><see cref="SqlExpr"/></returns>
		public static SqlExpr Expr(object expression, params object[] expressions) {
			return new SqlExpr(expression, expressions);
		}

		/// <summary>
		/// 指定テーブルへの INSERT INTO を表すSQL要素を生成する
		/// </summary>
		/// <param name="tbl">挿入先テーブル</param>
		/// <param name="cols">挿入値に対応する挿入先テーブルの列一覧</param>
		/// <returns><see cref="SqlInsertInto"/></returns>
		public static SqlInsertInto InsertInto(object tbl, params object[] cols) {
			return new SqlInsertInto(tbl, cols);
		}

		/// <summary>
		/// VALUES を表すSQL要素を生成する
		/// </summary>
		/// <param name="value">１レコード分の各列の値一覧</param>
		/// <returns><see cref="SqlValues"/></returns>
		public static SqlValues Values(params object[] value) {
			return new SqlValues(value);
		}

		/// <summary>
		/// VALUES を表すSQL要素を生成する
		/// </summary>
		/// <param name="values">複数レコード分の各列の値一覧</param>
		/// <returns><see cref="SqlValues"/></returns>
		public static SqlValues Values(object[][] values) {
			return new SqlValues(values);
		}

		/// <summary>
		/// LIKE を表すSQL要素を生成する
		/// </summary>
		/// <param name="expression">LIKE の左側の式</param>
		/// <param name="pattern">LIKE のパターン文字列</param>
		/// <returns><see cref="SqlLike"/></returns>
		public static SqlLike Like(object expression, object pattern) {
			return new SqlLike(expression, pattern);
		}

		/// <summary>
		/// DELETE FROM を表すSQL要素を生成する
		/// </summary>
		/// <param name="tbl">削除元テーブル</param>
		/// <returns><see cref="SqlDeleteFrom"/></returns>
		public static SqlDeleteFrom DeleteFrom(object tbl) {
			return new SqlDeleteFrom(tbl);
		}

		/// <summary>
		/// INNER JOIN を表すSQL要素を生成する
		/// </summary>
		/// <param name="tbl">結合するテーブル</param>
		/// <returns><see cref="SqlJoin"/></returns>
		public static SqlJoin InnerJoin(object tbl) {
			return new SqlJoin(JoinType.Inner, tbl);
		}

		/// <summary>
		/// LEFT JOIN を表すSQL要素を生成する
		/// </summary>
		/// <param name="tbl">結合するテーブル</param>
		/// <returns><see cref="SqlJoin"/></returns>
		public static SqlJoin LeftJoin(object tbl) {
			return new SqlJoin(JoinType.Left, tbl);
		}

		/// <summary>
		/// AND を表すSQL要素を生成する
		/// </summary>
		/// <param name="expressions">AND で結合する複数の式</param>
		/// <returns><see cref="SqlAnd"/></returns>
		public static SqlAnd And(params object[] expressions) {
			return new SqlAnd(expressions);
		}

		/// <summary>
		/// OR を表すSQL要素を生成する
		/// </summary>
		/// <param name="expressions">OR で結合する複数の式</param>
		/// <returns><see cref="SqlOr"/></returns>
		public static SqlOr Or(params object[] expressions) {
			return new SqlOr(expressions);
		}

		/// <summary>
		/// ON DUPLICATE KEY UPDATE を表すSQL要素を生成する
		/// </summary>
		/// <returns><see cref="SqlOnDuplicateKeyUpdate"/></returns>
		public static SqlOnDuplicateKeyUpdate OnDuplicateKeyUpdate() {
			return new SqlOnDuplicateKeyUpdate();
		}

		/// <summary>
		/// LAST_INSERT_ID を表すSQL要素を生成する
		/// </summary>
		/// <returns><see cref="SqlLastInsertId"/></returns>
		public static SqlLastInsertId LastInsertId(object expr = null) {
			return new SqlLastInsertId(expr);
		}

		/// <summary>
		/// １つのコマンドの終了を示す記号を取得する
		/// </summary>
		public static string End() {
			return ";";
		}

		/// <summary>
		/// 指定式を括弧で括る
		/// </summary>
		/// <param name="expression">式</param>
		/// <returns>括弧で括られた式</returns>
		public static string Parenthesize(object expression) {
			return "(" + expression + ")";
		}

		/// <summary>
		/// 指定された式が NULL または長さ0なら真となる
		/// </summary>
		public static string NullOrEmpty(object expression) {
			return "(" + expression + " IS NULL OR length(" + expression + ")=0)";
		}

		static string ColNameForIndex(Col col) {
			var dbt = col.DbType;
			var sb = new StringBuilder();
			sb.Append(col.NameQuoted);
			if ((dbt.Flags & DbTypeFlags.IsVariableLengthType) != 0 && dbt.Length != 0) {
				sb.Append("(");
				sb.Append(dbt.Length);
				sb.Append(")");
			}
			return sb.ToString();
		}

		/// <summary>
		/// 指定のテーブルが存在しなかったら作成する
		/// </summary>
		/// <param name="cmd">使用するコマンドオブジェクト</param>
		/// <param name="tbl">テーブルの定義</param>
		public static void CreateTableIfNotExists(MySqlCommand cmd, Tbl tbl) {
			var schemaName = tbl.Schema;
			var tableName = tbl.Name;
			var cols = tbl.ColsArray;
			var constraints = tbl.Constraints;
			var sb = new StringBuilder();

			// テーブル作成
			sb.AppendLine("CREATE TABLE IF NOT EXISTS " + tbl.FullNameQuoted + "(");
			for (int i = 0; i < cols.Length; i++) {
				var col = cols[i];
				if (i == 0) {
					sb.Append(" ");
				} else {
					sb.Append(",");
				}
				sb.Append(col.NameQuoted + " " + col.DbType);
				if (constraints.Where(c => c.Type == CrType.AutoIncrement && c.Cols.Contains(col)).Any()) {
					sb.Append(" AUTO_INCREMENT");
				}
				sb.AppendLine();
			}
			var pk = constraints.Where(c => c.Type == CrType.PrimaryKey).FirstOrDefault();
			if (pk != null) {
				sb.Append(",PRIMARY KEY(");
				for (int i = 0; i < pk.Cols.Length; i++) {
					if (i != 0) {
						sb.Append(", ");
					}
					sb.Append(ColNameForIndex(pk.Cols[i]));
				}
				sb.AppendLine(")");
			}
			sb.AppendLine(");");
			cmd.CommandText = sb.ToString();
			cmd.ExecuteNonQuery();

			// 制約追加
			foreach (var cr in constraints) {
				if (cr.Type == CrType.PrimaryKey || cr.Type == CrType.AutoIncrement) {
					continue;
				}

				uint hashCode = 0;
				for (int i = 0; i < cr.Cols.Length; i++) {
					var c = cr.Cols[i];
					if (i == 0) {
						hashCode = (uint)c.Name.GetHashCode();
					} else {
						hashCode *= (uint)c.Name.GetHashCode();
					}
				}

				switch (cr.Type) {
				case CrType.Index: {
						var name = "idx_" + tableName + "_" + hashCode;
						if (schemaName != null) {
							cmd.CommandText = "SELECT count(*) FROM information_schema.statistics WHERE table_schema='" + schemaName + "' AND table_name='" + tableName + "' AND index_name='" + name + "';";
						} else {
							cmd.CommandText = "SELECT count(*) FROM information_schema.statistics WHERE table_schema=database() AND table_name='" + tableName + "' AND index_name='" + name + "';";
						}
						var exists = false;
						using (var dr = cmd.ExecuteReader()) {
							while (dr.Read()) {
								exists = 0 < Convert.ToInt32(dr[0]);
							}
						}
						if (!exists) {
							cmd.CommandText = "ALTER TABLE " + tbl.FullNameQuoted + " ADD INDEX " + name + "(" + string.Join(", ", cr.Cols.Select(c => ColNameForIndex(c))) + ");";
							cmd.ExecuteNonQuery();
						}
					}
					break;

				case CrType.Unique: {
						var name = "idx_" + tableName + "_" + hashCode;
						if (schemaName != null) {
							cmd.CommandText = "SELECT count(*) FROM information_schema.statistics WHERE table_schema='" + schemaName + "' AND table_name='" + tableName + "' AND index_name='" + name + "';";
						} else {
							cmd.CommandText = "SELECT count(*) FROM information_schema.statistics WHERE table_schema=database() AND table_name='" + tableName + "' AND index_name='" + name + "';";
						}
						var exists = false;
						using (var dr = cmd.ExecuteReader()) {
							while (dr.Read()) {
								exists = 0 < Convert.ToInt32(dr[0]);
							}
						}
						if (!exists) {
							cmd.CommandText = "ALTER TABLE " + tbl.FullNameQuoted + " ADD UNIQUE " + name + "(" + string.Join(", ", cr.Cols.Select(c => ColNameForIndex(c))) + ");";
							cmd.ExecuteNonQuery();
						}
					}
					break;
				}
			}
		}

		/// <summary>
		/// 指定の型にマッピングしつつデータ読み込む
		/// <para>レコードは<see cref="List{T}"/>に読み込まれる</para>
		/// </summary>
		/// <param name="cmd">実行するコマンド</param>
		/// <param name="byName">列名とプロパティ名をマッチングさせて取得するなら true 、列順とプロパティ順を使うなら false</param>
		/// <param name="columns">マッピングする<see cref="Col{T}"/>を指定する new { Cols.title, Cols.date } の様な匿名クラス</param>
		/// <returns>レコードコレクション</returns>
		public static List<T> Read<T>(MySqlCommand cmd, bool byName, T columns) {
			var result = new List<T>();
			var dr2cols = byName ? DataReaderToCols<T>.InvokeByName : DataReaderToCols<T>.InvokeByIndex;
			using (var dr = cmd.ExecuteReader()) {
				while (dr.Read()) {
					result.Add(dr2cols(dr, columns));
				}
				return result;
			}
		}

		/// <summary>
		/// 指定のコマンドをログ残しつつ実行する
		/// <para>レコードは<see cref="List{T}"/>に読み込まれる、その際列順とプロパティ順をマッチングさせて取得する</para>
		/// </summary>
		/// <param name="cmd">実行するコマンド</param>
		/// <param name="columns">マッピングする<see cref="Col{T}"/>を指定する new { Cols.title, Cols.date } の様な匿名クラス</param>
		/// <returns>レコードコレクション</returns>
		public static List<T> ReadByIndex<T>(MySqlCommand cmd, T columns) {
			return Read<T>(cmd, false, columns);
		}

		/// <summary>
		/// 指定のコマンドをログ残しつつ実行する
		/// <para>レコードは<see cref="List{T}"/>に読み込まれる、その際列名とプロパティ名をマッチングさせて取得する</para>
		/// </summary>
		/// <param name="cmd">実行するコマンド</param>
		/// <param name="columns">マッピングする<see cref="Col{T}"/>を指定する new { Cols.title, Cols.date } の様な匿名クラス</param>
		/// <returns>レコードコレクション</returns>
		public static List<T> ReadByName<T>(MySqlCommand cmd, T columns) {
			return Read<T>(cmd, true, columns);
		}

		/// <summary>
		/// 0カラム目の値を int 値として取得する
		/// </summary>
		/// <param name="cmd">実行するコマンド</param>
		/// <param name="defaultValue">読み込まれたレコード数が０の場合用の既定値</param>
		/// <returns>読み込まれた値</returns>
		public static int ReadInt(MySqlCommand cmd, int defaultValue = 0) {
			var value = defaultValue;
			using (var dr = cmd.ExecuteReader()) {
				while (dr.Read()) {
					value = Convert.ToInt32(dr[0]);
				}
			}
			return value;
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
	/// 指定の列一覧クラスを基にエイリアスをセットした列一覧クラスを生成する
	/// </summary>
	/// <typeparam name="T">フィールドまたはプロパティとして<see cref="Col{S}"/>を持つ列一覧クラス型</typeparam>
	public static class AliasingCols<T> where T : class, new() {
		public static readonly Func<T, string, T> Invoke;

		static AliasingCols() {
			var colsType = typeof(T);
			var inputCols = Expression.Parameter(colsType);
			var inputAliasName = Expression.Parameter(typeof(string));
			var fieldsOfCols = colsType.GetFields().Where(f => f.IsPublic && f.FieldType.IsGenericType && f.FieldType.GetGenericTypeDefinition() == typeof(Col<>)).ToArray();
			var propertiesOfCols = colsType.GetProperties().Where(p => p.GetSetMethod(false).IsPublic && p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Col<>)).ToArray();
			var cols = Expression.Parameter(colsType);
			var expressions = new List<Expression>();
			var colsCtor = colsType.GetConstructor(new Type[0]);

			if (colsCtor == null) {
				throw new ApplicationException("列一覧クラス " + colsType + " にはデフォルトコンストラクタが必要です。");
			}

			expressions.Add(Expression.Assign(cols, Expression.New(colsCtor)));

			foreach (var f in fieldsOfCols) {
				var t = f.FieldType;
				var ctorForAlias = t.GetConstructor(new Type[] { typeof(string), t });
				expressions.Add(Expression.Assign(Expression.Field(cols, f), Expression.New(ctorForAlias, inputAliasName, Expression.Field(cols, f))));
			}
			foreach (var p in propertiesOfCols) {
				var t = p.PropertyType;
				var ctorForAlias = t.GetConstructor(new Type[] { typeof(string), t });
				expressions.Add(Expression.Assign(Expression.Property(cols, p), Expression.New(ctorForAlias, inputAliasName, Expression.Property(cols, p))));
			}
			expressions.Add(cols);

			Invoke = Expression.Lambda<Func<T, string, T>>(
				Expression.Block(
					new ParameterExpression[] { cols },
					expressions
				),
				inputCols,
				inputAliasName
			).Compile();
		}
	}

	/// <summary>
	/// <see cref="MySqlDataReader"/>からの値取得用
	/// <para>失敗時にはカラム名が分かる様に例外を投げる</para>
	/// </summary>
	public static class DataReaderAccessor {
		public static byte[] GetBytesByIndex(MySqlDataReader dr, int index) {
			try {
				var v = dr[index];
				return v == DBNull.Value ? null : (byte[])v;
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + dr.GetName(index) + " " + ex.Message, ex);
			}
		}
		public static string GetStringByIndex(MySqlDataReader dr, int index) {
			try {
				var v = dr[index];
				return v == DBNull.Value ? null : v.ToString();
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + dr.GetName(index) + " " + ex.Message, ex);
			}
		}
		public static bool GetBoolByIndex(MySqlDataReader dr, int index) {
			try {
				return Convert.ToBoolean(dr[index]);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + dr.GetName(index) + " " + ex.Message, ex);
			}
		}
		public static short GetInt16ByIndex(MySqlDataReader dr, int index) {
			try {
				return Convert.ToInt16(dr[index]);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + dr.GetName(index) + " " + ex.Message, ex);
			}
		}
		public static int GetInt32ByIndex(MySqlDataReader dr, int index) {
			try {
				return Convert.ToInt32(dr[index]);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + dr.GetName(index) + " " + ex.Message, ex);
			}
		}
		public static DateTime GetDateTimeByIndex(MySqlDataReader dr, int index) {
			try {
				return Convert.ToDateTime(dr[index]);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + dr.GetName(index) + " " + ex.Message, ex);
			}
		}
		public static Guid GetGuidByIndex(MySqlDataReader dr, int index) {
			try {
				return (Guid)dr[index];
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + dr.GetName(index) + " " + ex.Message, ex);
			}
		}

		public static byte[] GetNullableBytesByIndex(MySqlDataReader dr, int index) {
			try {
				var v = dr[index];
				return v == DBNull.Value ? null : (byte[])v;
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + dr.GetName(index) + " " + ex.Message, ex);
			}
		}
		public static string GetNullableStringByIndex(MySqlDataReader dr, int index) {
			try {
				var v = dr[index];
				return v == DBNull.Value ? null : v.ToString();
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + dr.GetName(index) + " " + ex.Message, ex);
			}
		}
		public static bool? GetNullableBoolByIndex(MySqlDataReader dr, int index) {
			try {
				var v = dr[index];
				return v == DBNull.Value ? default(bool?) : Convert.ToBoolean(v);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + dr.GetName(index) + " " + ex.Message, ex);
			}
		}
		public static short? GetNullableInt16ByIndex(MySqlDataReader dr, int index) {
			try {
				var v = dr[index];
				return v == DBNull.Value ? default(short?) : Convert.ToInt16(v);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + dr.GetName(index) + " " + ex.Message, ex);
			}
		}
		public static int? GetNullableInt32ByIndex(MySqlDataReader dr, int index) {
			try {
				var v = dr[index];
				return v == DBNull.Value ? default(int?) : Convert.ToInt32(v);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + dr.GetName(index) + " " + ex.Message, ex);
			}
		}
		public static DateTime? GetNullableDateTimeByIndex(MySqlDataReader dr, int index) {
			try {
				var v = dr[index];
				return v == DBNull.Value ? default(DateTime?) : Convert.ToDateTime(v);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + dr.GetName(index) + " " + ex.Message, ex);
			}
		}
		public static Guid? GetNullableGuidByIndex(MySqlDataReader dr, int index) {
			try {
				var v = dr[index];
				return v == DBNull.Value ? default(Guid?) : (Guid)v;
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + dr.GetName(index) + " " + ex.Message, ex);
			}
		}


		public static byte[] GetBytesByName(MySqlDataReader dr, string colName) {
			try {
				var v = dr[colName];
				return v == DBNull.Value ? null : (byte[])v;
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}
		public static string GetStringByName(MySqlDataReader dr, string colName) {
			try {
				var v = dr[colName];
				return v == DBNull.Value ? null : v.ToString();
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}
		public static bool GetBoolByName(MySqlDataReader dr, string colName) {
			try {
				return Convert.ToBoolean(dr[colName]);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}
		public static short GetInt16ByName(MySqlDataReader dr, string colName) {
			try {
				return Convert.ToInt16(dr[colName]);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}
		public static int GetInt32ByName(MySqlDataReader dr, string colName) {
			try {
				return Convert.ToInt32(dr[colName]);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}
		public static DateTime GetDateTimeByName(MySqlDataReader dr, string colName) {
			try {
				return Convert.ToDateTime(dr[colName]);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}
		public static Guid GetGuidByName(MySqlDataReader dr, string colName) {
			try {
				return (Guid)dr[colName];
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}

		public static byte[] GetNullableBytesByName(MySqlDataReader dr, string colName) {
			try {
				var v = dr[colName];
				return v == DBNull.Value ? null : (byte[])v;
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}
		public static string GetNullableStringByName(MySqlDataReader dr, string colName) {
			try {
				var v = dr[colName];
				return v == DBNull.Value ? null : v.ToString();
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}
		public static bool? GetNullableBoolByName(MySqlDataReader dr, string colName) {
			try {
				var v = dr[colName];
				return v == DBNull.Value ? default(bool?) : Convert.ToBoolean(v);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}
		public static short? GetNullableInt16ByName(MySqlDataReader dr, string colName) {
			try {
				var v = dr[colName];
				return v == DBNull.Value ? default(short?) : Convert.ToInt16(v);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}
		public static int? GetNullableInt32ByName(MySqlDataReader dr, string colName) {
			try {
				var v = dr[colName];
				return v == DBNull.Value ? default(int?) : Convert.ToInt32(v);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}
		public static DateTime? GetNullableDateTimeByName(MySqlDataReader dr, string colName) {
			try {
				var v = dr[colName];
				return v == DBNull.Value ? default(DateTime?) : Convert.ToDateTime(v);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}
		public static Guid? GetNullableGuidByName(MySqlDataReader dr, string colName) {
			try {
				var v = dr[colName];
				return v == DBNull.Value ? default(Guid?) : (Guid)v;
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}
	}

	/// <summary>
	/// <see cref="MySqlDataReader"/>から値を読み込み<see cref="Col{S}"/>をプロパティとして持つクラスを取得する処理を提供、その際列名とプロパティ名でマッチングさせ取得する
	/// </summary>
	/// <typeparam name="T"><see cref="Col{S}"/>をプロパティとして持つクラス型</typeparam>
	public static class DataReaderToCols<T> {
		public static readonly Func<MySqlDataReader, T, T> InvokeByIndex;
		public static readonly Func<MySqlDataReader, T, T> InvokeByName;

		static DataReaderToCols() {
			InvokeByIndex = Generate(false);
			InvokeByName = Generate(false);
		}

		/// <summary>
		/// DataReader から<typeparamref name="T"/>へ値を読み込むデリゲートを生成する
		/// </summary>
		/// <param name="byName">列名とプロパティ名をマッチングさせて取得するなら true 、列順とプロパティ順を使うなら false</param>
		/// <returns>DataReader から<typeparamref name="T"/>へ値を読み込むデリゲート</returns>
		static Func<MySqlDataReader, T, T> Generate(bool byName) {
			var type = typeof(T);

			// クラスのプロパティ数と型に一致するコンストラクタが存在しないならエラーとする
			var properties = type.GetProperties();
			var ctor = type.GetConstructor(properties.Select(p => p.PropertyType).ToArray());
			if (ctor == null) {
				throw new ApplicationException(type + " にプロパティ値を引数として受け取るコンストラクタがありません。");
			}

			// コンストラクタに渡す引数を作成
			var argExprs = new Expression[properties.Length];
			var paramDr = Expression.Parameter(typeof(MySqlDataReader));
			var paramBaseCols = Expression.Parameter(type);
			for (int i = 0; i < properties.Length; i++) {
				var p = properties[i];
				var pt = p.PropertyType;
				if (!pt.IsGenericType || pt.GetGenericTypeDefinition() != typeof(Col<>)) {
					throw new ApplicationException(type + " の " + p.Name + " が未対応の型 " + pt + " となっています。Col<T> でなければなりません。");
				}
				var ct = pt.GetGenericArguments()[0];
				var nct = Nullable.GetUnderlyingType(ct);
				var t = nct ?? ct;

				// 列の型に対応した DataReader から取得メソッド名を選ぶ
				var getMethodName = "";
				if (t == typeof(byte[])) {
					getMethodName = "Bytes";
				} else if (t == typeof(string)) {
					getMethodName = "String";
				} else if (t == typeof(bool)) {
					getMethodName = "Bool";
				} else if (t == typeof(short)) {
					getMethodName = "Int16";
				} else if (t == typeof(int)) {
					getMethodName = "Int32";
				} else if (t == typeof(DateTime)) {
					getMethodName = "DateTime";
				} else if (t == typeof(Guid)) {
					getMethodName = "Guid";
				} else {
					throw new ApplicationException(type + " の " + p.Name + " の値が未対応の型 " + t + " となっています。");
				}
				if (nct != null) {
					getMethodName = "Nullable" + getMethodName;
				}
				getMethodName = "Get" + getMethodName + (byName ? "ByName" : "ByIndex");

				var getter = typeof(DataReaderAccessor).GetMethod(getMethodName, BindingFlags.Public | BindingFlags.Static);
				if (getter == null) {
					throw new ApplicationException("内部エラー");
				}

				var col = Expression.Property(paramBaseCols, p);
				var colId = byName ? Expression.Constant(p.Name) : Expression.Constant(i);

				var pctor = pt.GetConstructor(new Type[] { pt, ct });
				if (pctor == null) {
					throw new ApplicationException("内部エラー");
				}

				argExprs[i] = Expression.New(pctor, col, Expression.Call(null, getter, paramDr, colId));
			}

			// コンストラクタ呼び出しをコンパイル
			return Expression.Lambda<Func<MySqlDataReader, T, T>>(Expression.New(ctor, argExprs), paramDr, paramBaseCols).Compile();
		}
	}
}
