﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using MySql.Data.MySqlClient;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Data.Common;

namespace Db {
	/// <summary>
	/// <see cref="DbType"/>に付与するフラグ
	/// </summary>
	[Flags]
	public enum DbTypeFlags {
		/// <summary>
		/// 型がベースの型からクローン
		/// </summary>
		IsCloned = 1 << 0,

		/// <summary>
		/// 型が可変長である事を示す
		/// </summary>
		IsVariableLengthType = 1 << 1,
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
		public static readonly DbType varbinary = new DbType("varbinary", DbTypeFlags.IsVariableLengthType);

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
			} else if (type == typeof(byte[])) {
				return varbinary;
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
		public DbTypeFlags TypeFlags { get; private set; }

		/// <summary>
		/// 桁数又は要素数
		/// </summary>
		public int TypeLength { get; private set; }

		/// <summary>
		/// コンストラクタ、型名と桁数又は要素数を指定して初期化する
		/// </summary>
		/// <param name="name">型名</param>
		/// <param name="flags">型に対する追加情報のフラグ</param>
		/// <param name="length">桁数又は要素数</param>
		public DbType(string name, DbTypeFlags flags = 0, int length = 0) {
			this.Name = name;
			this.TypeFlags = flags;
			this.TypeLength = length;
		}

		public static implicit operator string(DbType value) {
			return value.ToString();
		}

		public override string ToString() {
			return this.TypeLength == 0 ? this.Name : this.Name + "(" + this.TypeLength + ")";
		}

		/// <summary>
		/// 桁数又は要素数をオーバーライドしたクローンを作成する
		/// </summary>
		/// <param name="length">桁数又は要素数</param>
		/// <returns>クローン</returns>
		public DbType Len(int length) {
			var c = (this.TypeFlags & DbTypeFlags.IsCloned) != 0 ? this : this.MemberwiseClone() as DbType;
			c.TypeFlags |= DbTypeFlags.IsCloned;
			c.TypeLength = length;
			return c;
		}

		/// <summary>
		/// 型に対する追加情報をオーバーライドしたクローンを作成する
		/// </summary>
		/// <param name="flagsAdd">型に対する追加情報のフラグ</param>
		/// <returns>クローン</returns>
		public DbType Flags(DbTypeFlags flagsAdd) {
			var c = (this.TypeFlags & DbTypeFlags.IsCloned) != 0 ? this : this.MemberwiseClone() as DbType;
			c.TypeFlags |= DbTypeFlags.IsCloned | flagsAdd;
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
	public class Constraint {
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
		public Constraint(CrType type, params Col[] cols) {
			this.Type = type;
			this.Cols = cols;
		}
	}

	/// <summary>
	/// プライマリキー制約
	/// </summary>
	public class PrimaryKey : Constraint {
		public PrimaryKey(params Col[] cols) : base(CrType.PrimaryKey, cols) {
		}
	}

	/// <summary>
	/// インデックス制約
	/// </summary>
	public class Index : Constraint {
		public Index(params Col[] cols) : base(CrType.Index, cols) {
		}
	}

	/// <summary>
	/// ユニークキー制約
	/// </summary>
	public class Unique : Constraint {
		public Unique(params Col[] cols) : base(CrType.Unique, cols) {
		}
	}

	/// <summary>
	/// 自動採番制約
	/// </summary>
	public class AutoIncrement : Constraint {
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
		/// DB上での型
		/// </summary>
		public readonly DbType DbType;

		/// <summary>
		/// コンストラクタ、列名と型を指定して初期化する
		/// </summary>
		/// <param name="name">列名</param>
		/// <param name="dbType">DB上での型</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Col(string name, DbType dbType) {
			this.Name = name;
			this.DbType = dbType;
		}

		public SqlAs As(string alias) {
			return new SqlAs(null, alias, this);
		}

		public static implicit operator string(Col value) {
			return value.NameQuotedWithAlias;
		}

		public override string ToString() {
			return this.NameQuotedWithAlias;
		}

		public static Tuple<List<FieldInfo>, List<FieldInfo>> Distribute(FieldInfo[] source) {
			var colmembers = new List<FieldInfo>();
			var noncolmembers = new List<FieldInfo>();
			for (int i = 0; i < source.Length; i++) {
				var m = source[i];
				var t = m.FieldType;
				if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Col<>)) {
					colmembers.Add(m);
				} else {
					noncolmembers.Add(m);
				}
			}
			return new Tuple<List<FieldInfo>, List<FieldInfo>>(colmembers, noncolmembers);
		}

		public static Tuple<List<PropertyInfo>, List<PropertyInfo>> Distribute(PropertyInfo[] source) {
			var colmembers = new List<PropertyInfo>();
			var noncolmembers = new List<PropertyInfo>();
			for (int i = 0; i < source.Length; i++) {
				var m = source[i];
				var t = m.PropertyType;
				if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Col<>)) {
					colmembers.Add(m);
				} else {
					noncolmembers.Add(m);
				}
			}
			return new Tuple<List<PropertyInfo>, List<PropertyInfo>>(colmembers, noncolmembers);
		}

		/// <summary>
		/// 指定型内での列にマッピング可能なフィールド又は列一覧の取得
		/// </summary>
		public static Tuple<FieldInfo[], PropertyInfo[], FieldInfo[], PropertyInfo[]> GetFieldsAndProperties(Type type) {
			var fields = Distribute(type.GetFields(BindingFlags.Public | BindingFlags.Instance));
			var properties = Distribute(type.GetProperties(BindingFlags.Public | BindingFlags.Instance));
			var colFields = fields.Item1;
			var colProperties = properties.Item1;
			if (colFields.Count != 0 && colProperties.Count != 0) {
				throw new ApplicationException("型 " + type + " はフィールドとプロパティ両方に Col<T> メンバを持っています。このパターンは順序が判断できないためメンバを列にマッピングできません。");
			}
			var noncolFields = fields.Item2;
			var noncolProperties = properties.Item2;
			return new Tuple<FieldInfo[], PropertyInfo[], FieldInfo[], PropertyInfo[]>(colFields.ToArray(), colProperties.ToArray(), noncolFields.ToArray(), noncolProperties.ToArray());
		}

		/// <summary>
		/// 指定オブジェクト内の列オブジェクト一覧(<see cref="Col{T}"/>型のメンバの値)を取得する
		/// <para>cols が値型の場合には処理対象外とされ、長さ０の配列が返る</para>
		/// </summary>
		public static Col[] GetMemberValues(object cols) {
			var list = new List<Col>();
			GetMemberValues(cols, list);
			return list.ToArray();
		}

		/// <summary>
		/// 指定されたオブジェクトのメンバから再帰的に<see cref="Col{T}"/>型のメンバの値を取得していく
		/// </summary>
		/// <param name="cols">取得元オブジェクト</param>
		/// <param name="childCols">取得先リスト</param>
		public static void GetMemberValues(object cols, List<Col> childCols) {
			if (cols == null) {
				return;
			}

			// 内部にカラムを含むクラス以外は弾く
			var type = cols.GetType();
			if (type.IsSubclassOf(typeof(Col))) {
				// Col クラスはこれ以上分解できない列なので対象外
				return;
			}
			if (type.IsSubclassOf(typeof(SqlElement))) {
				// SqlElement はそのオブジェクト自身に文字列化を行わせるためカラムに分解してはいけない
				return;
			}
			if (type.IsValueType || type == typeof(string)) {
				// 値型や文字列は処理対象外
				return;
			}

			var members = GetFieldsAndProperties(type);
			var colFields = members.Item1;
			var colProperties = members.Item2;
			var noncolFields = members.Item3;
			var noncolProperties = members.Item4;

			if (colFields.Length != 0) {
				for (int i = 0; i < colFields.Length; i++) {
					childCols.Add(colFields[i].GetValue(cols) as Col);
				}
			} else if (colProperties.Length != 0) {
				for (int i = 0; i < colProperties.Length; i++) {
					childCols.Add(colProperties[i].GetValue(cols) as Col);
				}
			} else if (noncolFields.Length != 0) {
				for (int i = 0; i < noncolFields.Length; i++) {
					GetMemberValues(noncolFields[i].GetValue(cols), childCols);
				}
			} else if (noncolProperties.Length != 0) {
				for (int i = 0; i < noncolProperties.Length; i++) {
					GetMemberValues(noncolProperties[i].GetValue(cols), childCols);
				}
			}
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
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Col(string name) : base(name, DbType.Map(typeof(T))) {
		}

		/// <summary>
		/// コンストラクタ、列名と型を指定して初期化する
		/// </summary>
		/// <param name="name">列名</param>
		/// <param name="dbType">DB内での型</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Col(string name, DbType dbType) : base(name, dbType) {
		}

		/// <summary>
		/// コンストラクタ、ベースとなる<see cref="Col{T}"/>を指定して初期化する
		/// </summary>
		/// <param name="alias">エイリアス名</param>
		/// <param name="baseCol">ベースとなる列</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Col(string alias, Col<T> baseCol) : base(baseCol.Name, baseCol.DbType) {
			this.Alias = alias;
			this.BaseCol = baseCol;
		}

		/// <summary>
		/// コンストラクタ、ベースとなる<see cref="Col{T}"/>と値を指定して初期化する
		/// </summary>
		/// <param name="baseCol">ベースとなる列</param>
		/// <param name="value">値</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Col(Col<T> baseCol, T value) : base(baseCol.Name, baseCol.DbType) {
			this.BaseCol = baseCol;
			this.Value = value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Col<T> ValueAs(T value) {
			return new Col<T>(this, value);
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
		public Constraint[] Constraints { get; protected set; }

		/// <summary>
		/// コンストラクタ、テーブル名（またはエイリアス）と列一覧を指定して初期化する
		/// </summary>
		/// <param name="name">テーブル名またはエイリアス</param>
		/// <param name="cols">列一覧</param>
		public Tbl(string name, params Col[] cols) {
			this.Name = name;
			this.ColsArray = cols;
			this.Constraints = new Constraint[0];
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
			this.Constraints = new Constraint[0];
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
			return this.FullNameQuoted;
		}

		public static implicit operator string(Tbl value) {
			return value.FullNameQuoted;
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
		public Tbl(string name, params Func<T, Constraint>[] constraintCreators) : base(name, new T()) {
			this.Constraints = constraintCreators.Select(c => c(this.Cols)).ToArray();
		}

		/// <summary>
		/// コンストラクタ、スキーマ名、テーブル名（またはエイリアス）と列一覧を指定して初期化する
		/// </summary>
		/// <param name="schema">スキーマ名</param>
		/// <param name="name">テーブル名またはエイリアス</param>
		/// <param name="constraintCreators">制約作成デリゲート一覧</param>
		public Tbl(string schema, string name, params Func<T, Constraint>[] constraintCreators) : base(schema, name, new T()) {
			this.Constraints = constraintCreators.Select(c => c(this.Cols)).ToArray();
		}

		/// <summary>
		/// 指定名のエイリアスを作成
		/// </summary>
		/// <param name="aliasName">エイリアス名</param>
		/// <returns>エイリアス</returns>
		public Tbl<T> As(string aliasName) {
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
	}

	/// <summary>
	/// SQL文生成時にパラメータとしてマーキングするためのクラス
	/// </summary>
	public class Param {
		/// <summary>
		/// 値
		/// </summary>
		public object Value;

		public Param(object value) {
			this.Value = value;
		}
	}

	/// <summary>
	/// <see cref="SqlBuildContext.Append(StringBuilder, object, SqlBuildContextAppendFlags)"/>に渡す文字列化のオプションフラグ
	/// </summary>
	[Flags]
	public enum SqlBuildContextAppendFlags {
		AsStringLiteral = 1 << 0,
		WithAlias = 1 << 1,
		Underlying = 1 << 2,
	}

	/// <summary>
	/// SQL文生成時の状態などを保持する
	/// </summary>
	public class SqlBuildContext {
		/// <summary>
		/// パラメータ一覧
		/// </summary>
		public readonly Dictionary<Param, string> Params = new Dictionary<Param, string>();

		public SqlBuildContextAppendFlags AddFlags = 0;

		public SqlBuildContextAppendFlags RemoveFlags = 0;

		/// <summary>
		/// 指定のパラメータ名を取得する、未登録なら自動でパラメータ名生成＆登録して取得する
		/// </summary>
		/// <param name="prm">パラメータ</param>
		/// <returns>パラメータ名</returns>
		public string GetParamName(Param prm) {
			string paramName;
			if (!this.Params.TryGetValue(prm, out paramName)) {
				this.Params[prm] = paramName = "@v" + this.Params.Count;
			}
			return paramName;
		}

		/// <summary>
		/// 現在の設定で指定の要素を文字列化し<see cref="StringBuilder"/>に追加する
		/// </summary>
		/// <param name="sb">文字列追加先</param>
		/// <param name="element">追加する要素</param>
		/// <param name="flags">文字列化オプションフラグ</param>
		public void Append(StringBuilder sb, object element, SqlBuildContextAppendFlags flags = 0) {
			flags |= this.AddFlags;
			flags &= ~this.RemoveFlags;

			SqlElement se;
			Param p;
			Col col;
			Tbl tbl;
			if (element == null) {
				// null なら NULL に置き換え
				QuoteIfNeeded(sb, sb2 => sb2.Append("NULL"), (flags & SqlBuildContextAppendFlags.AsStringLiteral) != 0);
			} else if ((se = element as SqlElement) != null) {
				// SQL要素を処理
				if (se.Prev == null) {
					// リンク無しの要素なら普通に文字列化
					QuoteIfNeeded(sb, sb2 => se.Build(this, flags, sb2), (flags & SqlBuildContextAppendFlags.AsStringLiteral) != 0);
				} else {
					// リンク有りの要素 Prev の方から文字列化
					var ses = new List<SqlElement>();
					ses.Add(se);
					for (var prev = se.Prev; prev != null; prev = prev.Prev) {
						ses.Add(prev);
					}
					for (int i = ses.Count - 1; i != -1; i--) {
						ses[i].Build(this, flags, sb);
						if (i != 0) {
							sb.AppendLine();
						}
					}
				}
			} else if ((col = element as Col) != null) {
				// カラムを処理
				if ((flags & SqlBuildContextAppendFlags.Underlying) != 0) {
					col = col.Underlying;
				}
				if ((flags & SqlBuildContextAppendFlags.WithAlias) != 0) {
					sb.Append(col.NameQuotedWithAlias);
				} else {
					sb.Append(col.NameQuoted);
				}
			} else if ((tbl = element as Tbl) != null) {
				// テーブルを処理
				if ((flags & SqlBuildContextAppendFlags.WithAlias) != 0 && tbl.FullNameQuoted != tbl.Underlying.FullNameQuoted) {
					sb.Append(tbl.Underlying.FullNameQuoted);
					sb.Append(' ');
					sb.Append(tbl.FullNameQuoted);
					return;
				}
				if ((flags & SqlBuildContextAppendFlags.Underlying) != 0) {
					tbl = tbl.Underlying;
				}
				sb.Append(tbl.FullNameQuoted);
			} else if ((p = element as Param) != null) {
				// パラメータを処理
				sb.Append(GetParamName(p));
			} else {
				// その他は普通に文字列化
				QuoteIfNeeded(sb, sb2 => sb2.Append(element.ToString()), (flags & SqlBuildContextAppendFlags.AsStringLiteral) != 0);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static void QuoteIfNeeded(StringBuilder sb, Action<StringBuilder> generateContent, bool needQuote = false) {
			if (needQuote) {
				sb.Append('\'');
				generateContent(sb);
				sb.Append('\'');
			} else {
				generateContent(sb);
			}
		}
	}

	/// <summary>
	/// SQL文を構成する要素基本クラス
	/// </summary>
	public abstract class SqlElement {
		/// <summary>
		/// 直前の要素があるなら null 以外を指定する、文字列化時にこちらが先に文字列化される
		/// </summary>
		public readonly SqlElement Prev;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SqlElement(SqlElement prev) {
			this.Prev = prev;
		}

		/// <summary>
		/// 要素を文字列化して指定の<see cref="StringBuilder"/>に追加する
		/// </summary>
		/// <param name="context">文字列化時の状態などを保持する</param>
		/// <param name="flags">文字列化オプションフラグ</param>
		/// <param name="sb">文字列追加先</param>
		public abstract void Build(SqlBuildContext context, SqlBuildContextAppendFlags flags, StringBuilder sb);
	}

	public class SqlSelect : SqlElement {
		public object[] Cols;

		public SqlSelect(SqlElement prev, params object[] cols) : base(prev) {
			var list = new List<object>();
			foreach (var c in cols) {
				var colMemberValues = Col.GetMemberValues(c);
				if (colMemberValues.Length == 0) {
					list.Add(c);
				} else {
					list.AddRange(colMemberValues);
				}
			}
			this.Cols = list.ToArray();
		}

		public override void Build(SqlBuildContext context, SqlBuildContextAppendFlags flags, StringBuilder sb) {
			sb.Append("SELECT ");

			if (this.Cols.Length == 0) {
				sb.Append("*");
			} else {
				for (int i = 0; i < this.Cols.Length; i++) {
					if (i != 0) {
						sb.Append(", ");
					}
					context.Append(sb, this.Cols[i], flags | SqlBuildContextAppendFlags.WithAlias);
				}
			}

			context.RemoveFlags |= SqlBuildContextAppendFlags.Underlying;
		}
	}

	public class SqlFrom : SqlElement {
		public object Tbl;

		public SqlFrom(SqlElement prev, object tbl) : base(prev) {
			this.Tbl = tbl;
		}

		public override void Build(SqlBuildContext context, SqlBuildContextAppendFlags flags, StringBuilder sb) {
			Tbl tbl;
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
				context.Append(sb, this.Tbl, flags);
			}
		}
	}

	public class SqlWhere : SqlElement {
		public object[] Expressions;

		public SqlWhere(SqlElement prev, object expression, params object[] expressions) : base(prev) {
			var exprs = new object[expressions.Length + 1];
			exprs[0] = expression;
			if (expressions.Length != 0) {
				Array.Copy(expressions, 0, exprs, 1, expressions.Length);
			}
			this.Expressions = exprs;
		}

		public override void Build(SqlBuildContext context, SqlBuildContextAppendFlags flags, StringBuilder sb) {
			sb.Append("WHERE");
			foreach (var e in this.Expressions) {
				sb.Append(" ");
				context.Append(sb, e, flags | SqlBuildContextAppendFlags.WithAlias);
			}
		}
	}

	public class SqlExpr : SqlElement {
		public object[] Expressions;

		public SqlExpr(SqlElement prev, object expression, params object[] expressions) : base(prev) {
			var exprs = new object[expressions.Length + 1];
			exprs[0] = expression;
			if (expressions.Length != 0) {
				Array.Copy(expressions, 0, exprs, 1, expressions.Length);
			}
			this.Expressions = exprs;
		}

		public override void Build(SqlBuildContext context, SqlBuildContextAppendFlags flags, StringBuilder sb) {
			for (int i = 0; i < this.Expressions.Length; i++) {
				if (i != 0) {
					sb.Append(" ");
				}
				context.Append(sb, this.Expressions[i], flags | SqlBuildContextAppendFlags.WithAlias);
			}
		}
	}

	public class SqlAs : SqlElement {
		public string Alias;
		public object[] Expressions;

		public SqlAs(SqlElement prev, string alias, object expression, params object[] expressions) : base(prev) {
			this.Alias = alias;

			var exprs = new object[expressions.Length + 1];
			exprs[0] = expression;
			if (expressions.Length != 0) {
				Array.Copy(expressions, 0, exprs, 1, expressions.Length);
			}
			this.Expressions = exprs;
		}

		public override void Build(SqlBuildContext context, SqlBuildContextAppendFlags flags, StringBuilder sb) {
			for (int i = 0; i < this.Expressions.Length; i++) {
				if (i != 0) {
					sb.Append(" ");
				}
				context.Append(sb, this.Expressions[i], flags);
			}
			sb.Append(" AS ");
			sb.Append(this.Alias);
		}
	}

	public class SqlUpdate : SqlElement {
		public object Tbl;

		public SqlUpdate(SqlElement prev, object tbl) : base(prev) {
			this.Tbl = tbl;
		}

		public override void Build(SqlBuildContext context, SqlBuildContextAppendFlags flags, StringBuilder sb) {
			sb.Append("UPDATE ");
			context.Append(sb, this.Tbl, SqlBuildContextAppendFlags.WithAlias);
		}
	}

	public class SqlSet : SqlElement {
		public SqlAssign[] Assigns;

		public SqlSet(SqlElement prev, params SqlAssign[] assigns) : base(prev) {
			this.Assigns = assigns;
		}

		public override void Build(SqlBuildContext context, SqlBuildContextAppendFlags flags, StringBuilder sb) {
			sb.Append("SET ");
			var assigns = this.Assigns;
			for (int i = 0; i < assigns.Length; i++) {
				if (i != 0) {
					sb.Append(", ");
				}
				var a = assigns[i];
				context.Append(sb, a.Col, SqlBuildContextAppendFlags.WithAlias);
				sb.Append("=");
				context.Append(sb, a.Value);
			}
		}
	}

	public struct SqlAssign {
		public object Col;
		public object Value;

		public SqlAssign(object col, object value) {
			this.Col = col;
			this.Value = value;
		}
	}

	public class SqlInsertInto : SqlElement {
		public object Tbl;
		public object[] Cols;

		public SqlInsertInto(SqlElement prev, object tbl, params object[] cols) : base(prev) {
			this.Tbl = tbl;
			var list = new List<object>();
			foreach (var c in cols) {
				var colMemberValues = Col.GetMemberValues(c);
				if (colMemberValues.Length == 0) {
					list.Add(c);
				} else {
					list.AddRange(colMemberValues);
				}
			}
			this.Cols = list.ToArray();
		}

		public override void Build(SqlBuildContext context, SqlBuildContextAppendFlags flags, StringBuilder sb) {
			sb.Append("INSERT INTO ");
			context.Append(sb, this.Tbl, SqlBuildContextAppendFlags.Underlying);
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
		}
	}

	public class SqlValues : SqlElement {
		public object[,] Values;

		public SqlValues(SqlElement prev, params object[] value) : base(prev) {
			this.Values = new object[1, value.Length];
			var values = this.Values;
			for (int i = 0; i < value.Length; i++) {
				values[0, i] = value[i];
			}
		}

		public SqlValues(SqlElement prev, object[,] values) : base(prev) {
			this.Values = values;
		}

		public override void Build(SqlBuildContext context, SqlBuildContextAppendFlags flags, StringBuilder sb) {
			var values = this.Values;
			var rowsCount = values.GetLength(0);
			var colsCount = values.GetLength(1);
			sb.Append("VALUES");
			sb.AppendLine();
			for (int i = 0; i < rowsCount; i++) {
				if (i != 0) {
					sb.Append(",");
				} else {
					sb.Append(" ");
				}
				sb.Append("(");
				for (int j = 0; j < colsCount; j++) {
					if (j != 0) {
						sb.Append(", ");
					}
					context.Append(sb, values[i, j], flags);
				}
				sb.AppendLine(")");
			}
		}
	}

	public class SqlLike : SqlElement {
		public object Expression;
		public object Pattern;

		public SqlLike(SqlElement prev, object expression, object pattern) : base(prev) {
			this.Expression = expression;
			this.Pattern = pattern;
		}

		public override void Build(SqlBuildContext context, SqlBuildContextAppendFlags flags, StringBuilder sb) {
			context.Append(sb, this.Expression, flags | SqlBuildContextAppendFlags.AsStringLiteral | SqlBuildContextAppendFlags.WithAlias);
			sb.Append(" LIKE ");
			context.Append(sb, this.Pattern, flags | SqlBuildContextAppendFlags.AsStringLiteral | SqlBuildContextAppendFlags.WithAlias);
		}
	}

	public class NotSqlLike : SqlElement {
		public object Expression;
		public object Pattern;

		public NotSqlLike(SqlElement prev, object expression, object pattern) : base(prev) {
			this.Expression = expression;
			this.Pattern = pattern;
		}

		public override void Build(SqlBuildContext context, SqlBuildContextAppendFlags flags, StringBuilder sb) {
			context.Append(sb, this.Expression, flags | SqlBuildContextAppendFlags.AsStringLiteral | SqlBuildContextAppendFlags.WithAlias);
			sb.Append(" NOT LIKE ");
			context.Append(sb, this.Pattern, flags | SqlBuildContextAppendFlags.AsStringLiteral | SqlBuildContextAppendFlags.WithAlias);
		}
	}

	public class SqlDeleteFrom : SqlElement {
		public object Tbl;

		public SqlDeleteFrom(SqlElement prev, object tbl) : base(prev) {
			this.Tbl = tbl;
		}

		public override void Build(SqlBuildContext context, SqlBuildContextAppendFlags flags, StringBuilder sb) {
			sb.Append("DELETE FROM ");
			context.Append(sb, this.Tbl, flags | SqlBuildContextAppendFlags.Underlying);
			context.AddFlags |= SqlBuildContextAppendFlags.Underlying;
		}
	}

	public enum JoinType {
		Inner,
		Left,
	}

	public class SqlJoin : SqlElement {
		public JoinType JoinType;
		public bool IsUsing;
		public object Tbl;
		public object Col;
		public object[] Expressions;

		public SqlJoin(SqlElement prev, JoinType joinType, object tbl) : base(prev) {
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

		public override void Build(SqlBuildContext context, SqlBuildContextAppendFlags flags, StringBuilder sb) {
			sb.Append(this.JoinType == JoinType.Inner ? "INNER JOIN" : "LEFT JOIN");
			sb.Append(" ");
			context.Append(sb, this.Tbl, flags | SqlBuildContextAppendFlags.WithAlias);
			if (this.IsUsing) {
				if (this.Col == null) {
					throw new ApplicationException("JOIN USING に対して列が指定されていません。");
				}
				sb.Append(" USING(");
				context.Append(sb, this.Col, flags | SqlBuildContextAppendFlags.Underlying);
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
					context.Append(sb, this.Expressions[i], flags | SqlBuildContextAppendFlags.WithAlias);
				}
			}
		}
	}

	public class SqlGroupBy : SqlElement {
		public object[] Cols;

		public SqlGroupBy(SqlElement prev, params object[] cols) : base(prev) {
			this.Cols = cols;
		}

		public override void Build(SqlBuildContext context, SqlBuildContextAppendFlags flags, StringBuilder sb) {
			sb.Append("GROUP BY ");
			for (int i = 0; i < this.Cols.Length; i++) {
				if (i != 0) {
					sb.Append(", ");
				}
				context.Append(sb, this.Cols[i], SqlBuildContextAppendFlags.WithAlias);
			}
		}
	}

	public class SqlOrderBy : SqlElement {
		public object[] Cols;

		public SqlOrderBy(SqlElement prev, params object[] cols) : base(prev) {
			this.Cols = cols;
		}

		public override void Build(SqlBuildContext context, SqlBuildContextAppendFlags flags, StringBuilder sb) {
			sb.Append("ORDER BY ");
			for (int i = 0; i < this.Cols.Length; i++) {
				if (i != 0) {
					sb.Append(", ");
				}
				context.Append(sb, this.Cols[i], SqlBuildContextAppendFlags.WithAlias);
			}
		}
	}

	public class SqlDesc : SqlElement {
		public object Col;

		public SqlDesc(SqlElement prev, object col) : base(prev) {
			this.Col = col;
		}

		public override void Build(SqlBuildContext context, SqlBuildContextAppendFlags flags, StringBuilder sb) {
			context.Append(sb, this.Col, SqlBuildContextAppendFlags.WithAlias);
			sb.Append(" DESC");
		}
	}

	public class SqlAnd : SqlElement {
		public object[] Expressions;

		public SqlAnd(SqlElement prev, params object[] expressions) : base(prev) {
			this.Expressions = expressions;
		}

		public override void Build(SqlBuildContext context, SqlBuildContextAppendFlags flags, StringBuilder sb) {
			for (int i = 0; i < this.Expressions.Length; i++) {
				if (i != 0) {
					sb.Append(" AND ");
				}
				sb.Append('(');
				context.Append(sb, this.Expressions[i], flags);
				sb.Append(')');
			}
		}
	}

	public class SqlOr : SqlElement {
		public object[] Expressions;

		public SqlOr(SqlElement prev, params object[] expressions) : base(prev) {
			this.Expressions = expressions;
		}

		public override void Build(SqlBuildContext context, SqlBuildContextAppendFlags flags, StringBuilder sb) {
			for (int i = 0; i < this.Expressions.Length; i++) {
				if (i != 0) {
					sb.Append(" OR ");
				}
				sb.Append('(');
				context.Append(sb, this.Expressions[i], flags);
				sb.Append(')');
			}
		}
	}

	public class SqlEq : SqlElement {
		public object Left;
		public object Right;

		public SqlEq(SqlElement prev, object left, object right) : base(prev) {
			this.Left = left;
			this.Right = right;
		}

		public override void Build(SqlBuildContext context, SqlBuildContextAppendFlags flags, StringBuilder sb) {
			context.Append(sb, this.Left, SqlBuildContextAppendFlags.WithAlias);
			sb.Append("=");
			context.Append(sb, this.Right, SqlBuildContextAppendFlags.WithAlias);
		}
	}

	public class SqlNeq : SqlElement {
		public object Left;
		public object Right;

		public SqlNeq(SqlElement prev, object left, object right) : base(prev) {
			this.Left = left;
			this.Right = right;
		}

		public override void Build(SqlBuildContext context, SqlBuildContextAppendFlags flags, StringBuilder sb) {
			context.Append(sb, this.Left, SqlBuildContextAppendFlags.WithAlias);
			sb.Append("<>");
			context.Append(sb, this.Right, SqlBuildContextAppendFlags.WithAlias);
		}
	}

	public class SqlCase : SqlElement {
		public object CaseValue;

		public SqlCase(SqlElement prev, object caseValue = null) : base(prev) {
			this.CaseValue = caseValue;
		}

		public override void Build(SqlBuildContext context, SqlBuildContextAppendFlags flags, StringBuilder sb) {
			sb.Append("CASE");
			if (this.CaseValue != null) {
				context.Append(sb, this.CaseValue, flags | SqlBuildContextAppendFlags.WithAlias);
			}
		}
	}

	public class SqlWhen : SqlElement {
		public object[] Expressions;

		public SqlWhen(SqlElement prev, object expression, params object[] expressions) : base(prev) {
			var exprs = new object[expressions.Length + 1];
			exprs[0] = expression;
			if (expressions.Length != 0) {
				Array.Copy(expressions, 0, exprs, 1, expressions.Length);
			}
			this.Expressions = exprs;
		}

		public override void Build(SqlBuildContext context, SqlBuildContextAppendFlags flags, StringBuilder sb) {
			sb.Append("WHEN");
			foreach (var e in this.Expressions) {
				sb.Append(" ");
				context.Append(sb, e, flags | SqlBuildContextAppendFlags.WithAlias);
			}
		}
	}

	public class SqlThen : SqlElement {
		public object[] Expressions;

		public SqlThen(SqlElement prev, object expression, params object[] expressions) : base(prev) {
			var exprs = new object[expressions.Length + 1];
			exprs[0] = expression;
			if (expressions.Length != 0) {
				Array.Copy(expressions, 0, exprs, 1, expressions.Length);
			}
			this.Expressions = exprs;
		}

		public override void Build(SqlBuildContext context, SqlBuildContextAppendFlags flags, StringBuilder sb) {
			sb.Append("THEN");
			foreach (var e in this.Expressions) {
				sb.Append(" ");
				context.Append(sb, e, flags | SqlBuildContextAppendFlags.WithAlias);
			}
		}
	}

	public class SqlElse : SqlElement {
		public object[] Expressions;

		public SqlElse(SqlElement prev, object expression, params object[] expressions) : base(prev) {
			var exprs = new object[expressions.Length + 1];
			exprs[0] = expression;
			if (expressions.Length != 0) {
				Array.Copy(expressions, 0, exprs, 1, expressions.Length);
			}
			this.Expressions = exprs;
		}

		public override void Build(SqlBuildContext context, SqlBuildContextAppendFlags flags, StringBuilder sb) {
			sb.Append("ELSE");
			foreach (var e in this.Expressions) {
				sb.Append(" ");
				context.Append(sb, e, flags | SqlBuildContextAppendFlags.WithAlias);
			}
		}
	}

	public class SqlEndCase : SqlElement {
		public SqlEndCase(SqlElement prev) : base(prev) {
		}

		public override void Build(SqlBuildContext context, SqlBuildContextAppendFlags flags, StringBuilder sb) {
			sb.Append("END");
		}
	}

	public class SqlOnDuplicateKeyUpdate : SqlElement {
		public SqlAssign[] Assigns;

		public SqlOnDuplicateKeyUpdate(SqlElement prev, params SqlAssign[] assigns) : base(prev) {
			this.Assigns = assigns;
		}

		public override void Build(SqlBuildContext context, SqlBuildContextAppendFlags flags, StringBuilder sb) {
			sb.Append("ON DUPLICATE KEY UPDATE ");
			var assigns = this.Assigns;
			for (int i = 0; i < assigns.Length; i++) {
				if (i != 0) {
					sb.Append(", ");
				}
				var a = assigns[i];
				context.Append(sb, a.Col, SqlBuildContextAppendFlags.Underlying);
				sb.Append("=");
				context.Append(sb, a.Value);
			}
		}
	}

	public class SqlLastInsertId : SqlElement {
		public object Expr;

		public SqlLastInsertId(SqlElement prev, object expr = null) : base(prev) {
			this.Expr = expr;
		}

		public override void Build(SqlBuildContext context, SqlBuildContextAppendFlags flags, StringBuilder sb) {
			sb.Append("LAST_INSERT_ID(");
			if (this.Expr != null) {
				context.Append(sb, this.Expr);
			}
			sb.Append(")");
		}
	}

	public class SqlEnd : SqlElement {
		public SqlEnd(SqlElement prev) : base(prev) {
		}

		public override void Build(SqlBuildContext context, SqlBuildContextAppendFlags flags, StringBuilder sb) {
			sb.Append(";");
		}
	}

	public static class SqlExtensionForMySql {
		public static void CreateTableIfNotExists(this Tbl tbl, MySqlCommand cmd) { Db.CreateTableIfNotExists(cmd, tbl); }
		public static void DropTableIfExists(this Tbl tbl, MySqlCommand cmd) { Db.DropTableIfExists(cmd, tbl); }
		public static Func<MySqlCommand, MySqlCommand> ToFunc(this SqlElement element) { return Db.Sql(element); }
		public static SqlSelect Select(this SqlInsertInto element, params object[] cols) { return new SqlSelect(element, cols); }
		public static SqlSelect Select(this SqlEnd element, params object[] cols) { return new SqlSelect(element, cols); }
		public static SqlUpdate Update(this SqlEnd element, object tbl) { return new SqlUpdate(element, tbl); }
		public static SqlFrom From(this SqlSelect element, object tbl) { return new SqlFrom(element, tbl); }
		public static SqlJoin InnerJoin(this SqlFrom element, object tbl) { return new SqlJoin(element, JoinType.Inner, tbl); }
		public static SqlJoin LeftJoin(this SqlFrom element, object tbl) { return new SqlJoin(element, JoinType.Left, tbl); }
		public static SqlJoin InnerJoin(this SqlJoin element, object tbl) { return new SqlJoin(element, JoinType.Inner, tbl); }
		public static SqlJoin LeftJoin(this SqlJoin element, object tbl) { return new SqlJoin(element, JoinType.Left, tbl); }
		public static SqlJoin InnerJoin(this SqlUpdate element, object tbl) { return new SqlJoin(element, JoinType.Inner, tbl); }
		public static SqlJoin LeftJoin(this SqlUpdate element, object tbl) { return new SqlJoin(element, JoinType.Left, tbl); }
		public static SqlWhere Where(this SqlFrom element, object expression, params object[] expressions) { return new SqlWhere(element, expression, expressions); }
		public static SqlWhere Where(this SqlJoin element, object expression, params object[] expressions) { return new SqlWhere(element, expression, expressions); }
		public static SqlWhere Where(this SqlSet element, object expression, params object[] expressions) { return new SqlWhere(element, expression, expressions); }
		public static SqlGroupBy GroupBy(this SqlFrom element, params object[] cols) { return new SqlGroupBy(element, cols); }
		public static SqlGroupBy GroupBy(this SqlJoin element, params object[] cols) { return new SqlGroupBy(element, cols); }
		public static SqlGroupBy GroupBy(this SqlWhere element, params object[] cols) { return new SqlGroupBy(element, cols); }
		public static SqlOrderBy OrderBy(this SqlFrom element, params object[] cols) { return new SqlOrderBy(element, cols); }
		public static SqlOrderBy OrderBy(this SqlJoin element, params object[] cols) { return new SqlOrderBy(element, cols); }
		public static SqlOrderBy OrderBy(this SqlWhere element, params object[] cols) { return new SqlOrderBy(element, cols); }
		public static SqlOrderBy OrderBy(this SqlGroupBy element, params object[] cols) { return new SqlOrderBy(element, cols); }
		public static SqlOnDuplicateKeyUpdate OnDuplicateKeyUpdate(this SqlValues element, params SqlAssign[] assigns) { return new SqlOnDuplicateKeyUpdate(element, assigns); }
		public static SqlSet Set(this SqlUpdate element, params SqlAssign[] assigns) { return new SqlSet(element, assigns); }
		public static SqlSet Set(this SqlJoin element, params SqlAssign[] assigns) { return new SqlSet(element, assigns); }
		public static SqlValues Values(this SqlInsertInto element, params object[] value) { return new SqlValues(element, value); }
		public static SqlValues Values(this SqlInsertInto element, object[,] values) { return new SqlValues(element, values); }
		public static SqlEnd End(this SqlFrom element) { return new SqlEnd(element); }
		public static SqlEnd End(this SqlJoin element) { return new SqlEnd(element); }
		public static SqlEnd End(this SqlWhere element) { return new SqlEnd(element); }
		public static SqlEnd End(this SqlGroupBy element) { return new SqlEnd(element); }
		public static SqlEnd End(this SqlOrderBy element) { return new SqlEnd(element); }
		public static SqlEnd End(this SqlValues element) { return new SqlEnd(element); }
		public static SqlEnd End(this SqlSet element) { return new SqlEnd(element); }
		public static SqlEnd End(this SqlOnDuplicateKeyUpdate element) { return new SqlEnd(element); }
		public static SqlWhen When(this SqlCase element, object expression, params object[] expressions) { return new SqlWhen(element, expression, expressions); }
		public static SqlWhen When(this SqlThen element, object expression, params object[] expressions) { return new SqlWhen(element, expression, expressions); }
		public static SqlThen Then(this SqlWhen element, object expression, params object[] expressions) { return new SqlThen(element, expression, expressions); }
		public static SqlElse Else(this SqlThen element, object expression, params object[] expressions) { return new SqlElse(element, expression, expressions); }
		public static SqlEndCase End(this SqlThen element) { return new SqlEndCase(element); }
		public static SqlEndCase End(this SqlElse element) { return new SqlEndCase(element); }
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
		public static Func<MySqlCommand, MySqlCommand> Sql(params object[] elements) {
			var buffer = new SqlBuildContext();

			// 各モジュールを文字列に変換してSQL文を生成する
			var sb = new StringBuilder();
			for (int i = 0; i < elements.Length; i++) {
				if (i < elements.Length - 1) {
					buffer.Append(sb, elements[i]);
					sb.AppendLine();
				} else {
					buffer.Append(sb, elements[i]);
				}
			}
			sb.Append(";");
			var commandText = sb.ToString();

			// SQLコマンドに内容を適用するデリゲート作成
			Func<MySqlCommand, MySqlCommand> applyToCmd = cmd => {
				// 生成されたSQL分を適用
				cmd.CommandText = commandText;

				// 使用されたパラメータを設定する
				var parameters = cmd.Parameters;
				parameters.Clear();
				foreach (var p in buffer.Params) {
					parameters.AddWithValue(p.Value, p.Key.Value);
				}

				return cmd;
			};

			return applyToCmd;
		}

		public static Func<Param, Func<MySqlCommand, T1, MySqlCommand>> Sql<T1>(params object[] elements) {
			var applier = Sql(elements);
			return (param1) => {
				return (cmd, arg1) => {
					param1.Value = arg1;
					return applier(cmd);
				};
			};
		}

		public static Func<Param, Param, Func<MySqlCommand, T1, T2, MySqlCommand>> Sql<T1, T2>(params object[] elements) {
			var applier = Sql(elements);
			return (param1, param2) => {
				return (cmd, arg1, arg2) => {
					param1.Value = arg1;
					param2.Value = arg2;
					return applier(cmd);
				};
			};
		}

		public static Func<Param, Param, Param, Func<MySqlCommand, T1, T2, T3, MySqlCommand>> Sql<T1, T2, T3>(params object[] elements) {
			var applier = Sql(elements);
			return (param1, param2, param3) => {
				return (cmd, arg1, arg2, arg3) => {
					param1.Value = arg1;
					param2.Value = arg2;
					param3.Value = arg3;
					return applier(cmd);
				};
			};
		}

		public static Func<Param, Param, Param, Param, Func<MySqlCommand, T1, T2, T3, T4, MySqlCommand>> Sql<T1, T2, T3, T4>(params object[] elements) {
			var applier = Sql(elements);
			return (param1, param2, param3, param4) => {
				return (cmd, arg1, arg2, arg3, arg4) => {
					param1.Value = arg1;
					param2.Value = arg2;
					param3.Value = arg3;
					param4.Value = arg4;
					return applier(cmd);
				};
			};
		}

		public static Func<Param, Param, Param, Param, Param, Func<MySqlCommand, T1, T2, T3, T4, T5, MySqlCommand>> Sql<T1, T2, T3, T4, T5>(params object[] elements) {
			var applier = Sql(elements);
			return (param1, param2, param3, param4, param5) => {
				return (cmd, arg1, arg2, arg3, arg4, arg5) => {
					param1.Value = arg1;
					param2.Value = arg2;
					param3.Value = arg3;
					param4.Value = arg4;
					param5.Value = arg5;
					return applier(cmd);
				};
			};
		}

		public static Func<Param, Param, Param, Param, Param, Param, Func<MySqlCommand, T1, T2, T3, T4, T5, T6, MySqlCommand>> Sql<T1, T2, T3, T4, T5, T6>(params object[] elements) {
			var applier = Sql(elements);
			return (param1, param2, param3, param4, param5, param6) => {
				return (cmd, arg1, arg2, arg3, arg4, arg5, arg6) => {
					param1.Value = arg1;
					param2.Value = arg2;
					param3.Value = arg3;
					param4.Value = arg4;
					param5.Value = arg5;
					param6.Value = arg6;
					return applier(cmd);
				};
			};
		}

		public static Func<Param, Param, Param, Param, Param, Param, Param, Func<MySqlCommand, T1, T2, T3, T4, T5, T6, T7, MySqlCommand>> Sql<T1, T2, T3, T4, T5, T6, T7>(params object[] elements) {
			var applier = Sql(elements);
			return (param1, param2, param3, param4, param5, param6, param7) => {
				return (cmd, arg1, arg2, arg3, arg4, arg5, arg6, arg7) => {
					param1.Value = arg1;
					param2.Value = arg2;
					param3.Value = arg3;
					param4.Value = arg4;
					param5.Value = arg5;
					param6.Value = arg6;
					param7.Value = arg7;
					return applier(cmd);
				};
			};
		}

		public static Func<Param, Param, Param, Param, Param, Param, Param, Param, Func<MySqlCommand, T1, T2, T3, T4, T5, T6, T7, T8, MySqlCommand>> Sql<T1, T2, T3, T4, T5, T6, T7, T8>(params object[] elements) {
			var applier = Sql(elements);
			return (param1, param2, param3, param4, param5, param6, param7, param8) => {
				return (cmd, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8) => {
					param1.Value = arg1;
					param2.Value = arg2;
					param3.Value = arg3;
					param4.Value = arg4;
					param5.Value = arg5;
					param6.Value = arg6;
					param7.Value = arg7;
					param8.Value = arg8;
					return applier(cmd);
				};
			};
		}

		/// <summary>
		/// 指定列の SELECT を表すSQL要素を生成する
		/// </summary>
		/// <param name="cols">列一覧</param>
		/// <returns><see cref="SqlSelect"/></returns>
		public static SqlSelect Select(params object[] cols) {
			return new SqlSelect(null, cols);
		}

		/// <summary>
		/// 指定テーブルからの FROM を表すSQL要素を生成する
		/// </summary>
		/// <param name="tbl">取得元テーブル</param>
		/// <returns><see cref="SqlFrom"/></returns>
		public static SqlFrom From(object tbl) {
			return new SqlFrom(null, tbl);
		}

		/// <summary>
		/// WHERE を表すSQL要素を生成する
		/// </summary>
		/// <param name="expression">式</param>
		/// <param name="expressions">さらに結合する式一覧</param>
		/// <returns><see cref="SqlWhere "/></returns>
		public static SqlWhere Where(object expression, params object[] expressions) {
			return new SqlWhere(null, expression, expressions);
		}

		/// <summary>
		/// 式を表すSQL要素を生成する
		/// </summary>
		/// <param name="expression">式</param>
		/// <param name="expressions">さらに結合する式一覧</param>
		/// <returns><see cref="SqlExpr"/></returns>
		public static SqlExpr Expr(object expression, params object[] expressions) {
			return new SqlExpr(null, expression, expressions);
		}

		/// <summary>
		/// 指定テーブルへの UPDATE を表すSQL要素を生成する
		/// </summary>
		/// <param name="tbl">対象テーブル</param>
		/// <returns><see cref="SqlUpdate"/></returns>
		public static SqlUpdate Update(object tbl) {
			return new SqlUpdate(null, tbl);
		}

		/// <summary>
		/// SET を表すSQL要素を生成する
		/// </summary>
		/// <param name="assigns">代入式一覧</param>
		/// <returns><see cref="SqlSet"/></returns>
		public static SqlSet Set(params SqlAssign[] assigns) {
			return new SqlSet(null, assigns);
		}

		/// <summary>
		/// 代入式を表すSQL要素を生成する
		/// </summary>
		/// <param name="col">列</param>
		/// <param name="value">値</param>
		/// <returns><see cref="SqlAssign"/></returns>
		public static SqlAssign Assign(object col, object value) {
			return new SqlAssign(col, value);
		}

		/// <summary>
		/// 指定テーブルへの INSERT INTO を表すSQL要素を生成する
		/// </summary>
		/// <param name="tbl">挿入先テーブル</param>
		/// <param name="cols">挿入値に対応する挿入先テーブルの列一覧</param>
		/// <returns><see cref="SqlInsertInto"/></returns>
		public static SqlInsertInto InsertInto(object tbl, params object[] cols) {
			return new SqlInsertInto(null, tbl, cols);
		}

		/// <summary>
		/// VALUES を表すSQL要素を生成する
		/// </summary>
		/// <param name="value">１レコード分の各列の値一覧</param>
		/// <returns><see cref="SqlValues"/></returns>
		public static SqlValues Values(params object[] value) {
			return new SqlValues(null, value);
		}

		/// <summary>
		/// VALUES を表すSQL要素を生成する
		/// </summary>
		/// <param name="values">複数レコード分の各列の値一覧</param>
		/// <returns><see cref="SqlValues"/></returns>
		public static SqlValues Values(object[,] values) {
			return new SqlValues(null, values);
		}

		/// <summary>
		/// LIKE を表すSQL要素を生成する
		/// </summary>
		/// <param name="expression">LIKE の左側の式</param>
		/// <param name="pattern">LIKE のパターン文字列</param>
		/// <returns><see cref="SqlLike"/></returns>
		public static SqlLike Like(object expression, object pattern) {
			return new SqlLike(null, expression, pattern);
		}

		/// <summary>
		/// NOT LIKE を表すSQL要素を生成する
		/// </summary>
		/// <param name="expression">NOT LIKE の左側の式</param>
		/// <param name="pattern">NOT LIKE のパターン文字列</param>
		/// <returns><see cref="NotSqlLike"/></returns>
		public static NotSqlLike NotLike(object expression, object pattern) {
			return new NotSqlLike(null, expression, pattern);
		}

		/// <summary>
		/// DELETE FROM を表すSQL要素を生成する
		/// </summary>
		/// <param name="tbl">削除元テーブル</param>
		/// <returns><see cref="SqlDeleteFrom"/></returns>
		public static SqlDeleteFrom DeleteFrom(object tbl) {
			return new SqlDeleteFrom(null, tbl);
		}

		/// <summary>
		/// INNER JOIN を表すSQL要素を生成する
		/// </summary>
		/// <param name="tbl">結合するテーブル</param>
		/// <returns><see cref="SqlJoin"/></returns>
		public static SqlJoin InnerJoin(object tbl) {
			return new SqlJoin(null, JoinType.Inner, tbl);
		}

		/// <summary>
		/// LEFT JOIN を表すSQL要素を生成する
		/// </summary>
		/// <param name="tbl">結合するテーブル</param>
		/// <returns><see cref="SqlJoin"/></returns>
		public static SqlJoin LeftJoin(object tbl) {
			return new SqlJoin(null, JoinType.Left, tbl);
		}

		/// <summary>
		/// GROUP BY を表すSQL要素を生成する
		/// </summary>
		/// <param name="cols">列一覧</param>
		/// <returns><see cref="SqlGroupBy"/></returns>
		public static SqlGroupBy GroupBy(params object[] cols) {
			return new SqlGroupBy(null, cols);
		}

		/// <summary>
		/// ORDER BY を表すSQL要素を生成する
		/// </summary>
		/// <param name="cols">列一覧</param>
		/// <returns><see cref="SqlOrderBy"/></returns>
		public static SqlOrderBy OrderBy(params object[] cols) {
			return new SqlOrderBy(null, cols);
		}

		/// <summary>
		/// DESC を表すSQL要素を生成する
		/// </summary>
		/// <param name="col">列</param>
		/// <returns><see cref="SqlDesc"/></returns>
		public static SqlDesc Desc(object col) {
			return new SqlDesc(null, col);
		}

		/// <summary>
		/// AND を表すSQL要素を生成する
		/// </summary>
		/// <param name="expressions">AND で結合する複数の式</param>
		/// <returns><see cref="SqlAnd"/></returns>
		public static SqlAnd And(params object[] expressions) {
			return new SqlAnd(null, expressions);
		}

		/// <summary>
		/// OR を表すSQL要素を生成する
		/// </summary>
		/// <param name="expressions">OR で結合する複数の式</param>
		/// <returns><see cref="SqlOr"/></returns>
		public static SqlOr Or(params object[] expressions) {
			return new SqlOr(null, expressions);
		}

		/// <summary>
		/// = を表すSQL要素を生成する
		/// </summary>
		/// <param name="left">左辺値</param>
		/// <param name="right">右辺値</param>
		/// <returns><see cref="SqlEq"/></returns>
		public static SqlEq Eq(object left, object right) {
			return new SqlEq(null, left, right);
		}

		/// <summary>
		/// <> を表すSQL要素を生成する
		/// </summary>
		/// <param name="left">左辺値</param>
		/// <param name="right">右辺値</param>
		/// <returns><see cref="SqlNeq"/></returns>
		public static SqlNeq Neq(object left, object right) {
			return new SqlNeq(null, left, right);
		}

		/// <summary>
		/// ON DUPLICATE KEY UPDATE を表すSQL要素を生成する
		/// </summary>
		/// <param name="assigns">代入式一覧</param>
		/// <returns><see cref="SqlOnDuplicateKeyUpdate"/></returns>
		public static SqlOnDuplicateKeyUpdate OnDuplicateKeyUpdate(params SqlAssign[] assigns) {
			return new SqlOnDuplicateKeyUpdate(null, assigns);
		}

		public static SqlCase Case(object caseValue = null) {
			return new SqlCase(null, caseValue);
		}

		/// <summary>
		/// LAST_INSERT_ID を表すSQL要素を生成する
		/// </summary>
		/// <returns><see cref="SqlLastInsertId"/></returns>
		public static SqlLastInsertId LastInsertId(object expr = null) {
			return new SqlLastInsertId(null, expr);
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
						hashCode = (uint)EqualTester.CombineHashCode((int)hashCode, c.Name.GetHashCode());
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
		/// 指定のテーブルが存在したら捨てる
		/// </summary>
		/// <param name="cmd">使用するコマンドオブジェクト</param>
		/// <param name="tbl">テーブルの定義</param>
		public static void DropTableIfExists(MySqlCommand cmd, Tbl tbl) {
			cmd.CommandText = "DROP TABLE IF EXISTS " + tbl.FullNameQuoted + ";";
			cmd.ExecuteNonQuery();
		}

		/// <summary>
		/// 指定カラムの値を<typeparamref name="T"/>型の値として取得する
		/// </summary>
		/// <typeparam name="T">取得値型</typeparam>
		/// <param name="cmd">実行するコマンド</param>
		/// <param name="defaultValue">読み込まれたレコードが存在しない場合の既定値</param>
		/// <param name="index">カラムインデックス</param>
		/// <returns>読み込まれた値</returns>
		public static T Read<T>(MySqlCommand cmd, T defaultValue = default(T), int index = 0) {
			var value = defaultValue;
			using (var dr = cmd.ExecuteReader()) {
				while (dr.Read()) {
					value = DataReaderToValue<T>.InvokeByIndex(dr, index);
				}
			}
			return value;
		}

		/// <summary>
		/// 指定の型にマッピングしつつデータ読み込む
		/// <para>レコードは<see cref="List{T}"/>に読み込まれる</para>
		/// </summary>
		/// <param name="cmd">実行するコマンド</param>
		/// <param name="byName">列名とプロパティ名をマッチングさせて取得するなら true 、列順とプロパティ順を使うなら false</param>
		/// <param name="columns">マッピングする<see cref="Col{T}"/>を指定する new { Cols.title, Cols.date } の様な匿名クラス</param>
		/// <returns>レコードコレクション</returns>
		public static List<T> ReadList<T>(MySqlCommand cmd, T columns, bool byName = false) {
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
		/// コマンドを実行し、指定カラムから<typeparamref name="T"/>型の値を取得し列挙するオブジェクトを取得する
		/// </summary>
		/// <typeparam name="T">取得値型</typeparam>
		/// <param name="cmd">実行するコマンド</param>
		/// <param name="index">カラムインデックス</param>
		/// <returns>値列挙オブジェクト</returns>
		public static DataReaderEnumerableByIndex<T> Enumerate<T>(MySqlCommand cmd, int index = 0) {
			return new DataReaderEnumerableByIndex<T>(cmd.ExecuteReader(), DataReaderToValue<T>.InvokeByIndex, index);
		}

		/// <summary>
		/// コマンドを実行し、<typeparamref name="T"/>型の値を取得し列挙するオブジェクトを取得する
		/// </summary>
		/// <typeparam name="T">取得値型</typeparam>
		/// <param name="cmd">実行するコマンド</param>
		/// <param name="baseValue">列挙される値の基となる値、<see cref="Col{S}"/>型のプロパティを複製する際に使用される</param>
		/// <param name="byName">プロパティ名とカラム名をマッチングさせて値取得するなら true を指定する、false ならプロパティインデックスとカラムインデックスのマッチングとなる</param>
		/// <returns>値列挙オブジェクト</returns>
		public static DataReaderEnumerable<T> Enumerate<T>(MySqlCommand cmd, T baseValue, bool byName = false) {
			return new DataReaderEnumerable<T>(cmd.ExecuteReader(), byName ? DataReaderToCols<T>.InvokeByName : DataReaderToCols<T>.InvokeByIndex, baseValue);
		}
		#endregion

		#region 非公開メソッド
		static string ColNameForIndex(Col col) {
			var dbt = col.DbType;
			var sb = new StringBuilder();
			sb.Append(col.NameQuoted);
			if ((dbt.TypeFlags & DbTypeFlags.IsVariableLengthType) != 0 && dbt.TypeLength != 0) {
				sb.Append("(");
				sb.Append(dbt.TypeLength);
				sb.Append(")");
			}
			return sb.ToString();
		}
		#endregion
	}

	/// <summary>
	/// 指定値から<see cref="Col{T}"/>型のフィールドとプロパティを抜き出し配列化する処理を提供する
	/// </summary>
	/// <typeparam name="T"><see cref="Col{T}"/>プロパティを持つ型</typeparam>
	public static class ColsGetter<T> {
		public static readonly Func<T, Col[]> Invoke;

		static ColsGetter() {
			var type = typeof(T);
			var colMembers = Col.GetFieldsAndProperties(type);
			var colFields = colMembers.Item1;
			var colProperties = colMembers.Item2;
			var input = Expression.Parameter(type);
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
	/// 指定の列一覧クラスを基にエイリアスをセットした列一覧クラスを生成する
	/// </summary>
	/// <typeparam name="T">フィールドまたはプロパティとして<see cref="Col{S}"/>を持つ列一覧クラス型</typeparam>
	public static class AliasingCols<T> where T : class, new() {
		public static readonly Func<T, string, T> Invoke;

		static AliasingCols() {
			var colsType = typeof(T);
			var inputCols = Expression.Parameter(colsType);
			var inputAliasName = Expression.Parameter(typeof(string));
			var members = Col.GetFieldsAndProperties(colsType);
			var fieldsOfCols = members.Item1;
			var propertiesOfCols = members.Item2;
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
	/// <see cref="DbDataReader"/>からの基本的な値取得用、１列毎のアクセスを提供する
	/// <para>失敗時にはカラム名が分かる様に例外を投げる</para>
	/// </summary>
	public static class SingleColumnAccessor {
		public static byte[] GetBytesByIndex(DbDataReader dr, int index) {
			try {
				var v = dr[index];
				return v == DBNull.Value ? null : (byte[])v;
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + dr.GetName(index) + " " + ex.Message, ex);
			}
		}
		public static byte[] GetBytesByName(DbDataReader dr, string colName) {
			try {
				var v = dr[colName];
				return v == DBNull.Value ? null : (byte[])v;
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}

		public static string GetStringByIndex(DbDataReader dr, int index) {
			try {
				var v = dr[index];
				return v == DBNull.Value ? null : (string)v;
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + dr.GetName(index) + " " + ex.Message, ex);
			}
		}
		public static string GetStringByName(DbDataReader dr, string colName) {
			try {
				var v = dr[colName];
				return v == DBNull.Value ? null : (string)v;
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}

		public static bool GetBoolByIndex(DbDataReader dr, int index) {
			try {
				return Convert.ToBoolean(dr[index]);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + dr.GetName(index) + " " + ex.Message, ex);
			}
		}
		public static bool? GetNullableBoolByIndex(DbDataReader dr, int index) {
			try {
				var v = dr[index];
				return v == DBNull.Value ? default(bool?) : Convert.ToBoolean(v);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + dr.GetName(index) + " " + ex.Message, ex);
			}
		}
		public static bool GetBoolByName(DbDataReader dr, string colName) {
			try {
				return Convert.ToBoolean(dr[colName]);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}
		public static bool? GetNullableBoolByName(DbDataReader dr, string colName) {
			try {
				var v = dr[colName];
				return v == DBNull.Value ? default(bool?) : Convert.ToBoolean(v);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}

		public static sbyte GetInt8ByIndex(DbDataReader dr, int index) {
			try {
				return Convert.ToSByte(dr[index]);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + dr.GetName(index) + " " + ex.Message, ex);
			}
		}
		public static sbyte? GetNullableInt8ByIndex(DbDataReader dr, int index) {
			try {
				var v = dr[index];
				return v == DBNull.Value ? default(sbyte?) : Convert.ToSByte(v);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + dr.GetName(index) + " " + ex.Message, ex);
			}
		}
		public static sbyte GetInt8ByName(DbDataReader dr, string colName) {
			try {
				return Convert.ToSByte(dr[colName]);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}
		public static sbyte? GetNullableInt8ByName(DbDataReader dr, string colName) {
			try {
				var v = dr[colName];
				return v == DBNull.Value ? default(sbyte?) : Convert.ToSByte(v);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}

		public static short GetInt16ByIndex(DbDataReader dr, int index) {
			try {
				return Convert.ToInt16(dr[index]);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + dr.GetName(index) + " " + ex.Message, ex);
			}
		}
		public static short? GetNullableInt16ByIndex(DbDataReader dr, int index) {
			try {
				var v = dr[index];
				return v == DBNull.Value ? default(short?) : Convert.ToInt16(v);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + dr.GetName(index) + " " + ex.Message, ex);
			}
		}
		public static short GetInt16ByName(DbDataReader dr, string colName) {
			try {
				return Convert.ToInt16(dr[colName]);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}
		public static short? GetNullableInt16ByName(DbDataReader dr, string colName) {
			try {
				var v = dr[colName];
				return v == DBNull.Value ? default(short?) : Convert.ToInt16(v);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}

		public static int GetInt32ByIndex(DbDataReader dr, int index) {
			try {
				return Convert.ToInt32(dr[index]);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + dr.GetName(index) + " " + ex.Message, ex);
			}
		}
		public static int? GetNullableInt32ByIndex(DbDataReader dr, int index) {
			try {
				var v = dr[index];
				return v == DBNull.Value ? default(int?) : Convert.ToInt32(v);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + dr.GetName(index) + " " + ex.Message, ex);
			}
		}
		public static int GetInt32ByName(DbDataReader dr, string colName) {
			try {
				return Convert.ToInt32(dr[colName]);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}
		public static int? GetNullableInt32ByName(DbDataReader dr, string colName) {
			try {
				var v = dr[colName];
				return v == DBNull.Value ? default(int?) : Convert.ToInt32(v);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}

		public static long GetInt64ByIndex(DbDataReader dr, int index) {
			try {
				return Convert.ToInt64(dr[index]);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + dr.GetName(index) + " " + ex.Message, ex);
			}
		}
		public static long? GetNullableInt64ByIndex(DbDataReader dr, int index) {
			try {
				var v = dr[index];
				return v == DBNull.Value ? default(long?) : Convert.ToInt64(v);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + dr.GetName(index) + " " + ex.Message, ex);
			}
		}
		public static long GetInt64ByName(DbDataReader dr, string colName) {
			try {
				return Convert.ToInt64(dr[colName]);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}
		public static long? GetNullableInt64ByName(DbDataReader dr, string colName) {
			try {
				var v = dr[colName];
				return v == DBNull.Value ? default(long?) : Convert.ToInt64(v);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}

		public static DateTime GetDateTimeByIndex(DbDataReader dr, int index) {
			try {
				return Convert.ToDateTime(dr[index]);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + dr.GetName(index) + " " + ex.Message, ex);
			}
		}
		public static DateTime? GetNullableDateTimeByIndex(DbDataReader dr, int index) {
			try {
				var v = dr[index];
				return v == DBNull.Value ? default(DateTime?) : Convert.ToDateTime(v);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + dr.GetName(index) + " " + ex.Message, ex);
			}
		}
		public static DateTime GetDateTimeByName(DbDataReader dr, string colName) {
			try {
				return Convert.ToDateTime(dr[colName]);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}
		public static DateTime? GetNullableDateTimeByName(DbDataReader dr, string colName) {
			try {
				var v = dr[colName];
				return v == DBNull.Value ? default(DateTime?) : Convert.ToDateTime(v);
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}

		public static Guid GetGuidByIndex(DbDataReader dr, int index) {
			try {
				return (Guid)dr[index];
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + dr.GetName(index) + " " + ex.Message, ex);
			}
		}
		public static Guid? GetNullableGuidByIndex(DbDataReader dr, int index) {
			try {
				var v = dr[index];
				return v == DBNull.Value ? default(Guid?) : (Guid)v;
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + dr.GetName(index) + " " + ex.Message, ex);
			}
		}
		public static Guid GetGuidByName(DbDataReader dr, string colName) {
			try {
				return (Guid)dr[colName];
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}
		public static Guid? GetNullableGuidByName(DbDataReader dr, string colName) {
			try {
				var v = dr[colName];
				return v == DBNull.Value ? default(Guid?) : (Guid)v;
			} catch (Exception ex) {
				throw new ApplicationException("カラム名 " + colName + " " + ex.Message, ex);
			}
		}

		public static MethodInfo GetMethodByType(Type type, bool byName, bool throwOnTypeUnmatch = true) {
			var nt = Nullable.GetUnderlyingType(type);
			var t = nt ?? type;

			// 列の型に対応した DataReader から取得メソッド名を選ぶ
			var getMethodName = "";
			if (t == typeof(byte[])) {
				getMethodName = "Bytes";
			} else if (t == typeof(string)) {
				getMethodName = "String";
			} else if (t == typeof(bool)) {
				getMethodName = "Bool";
			} else if (t == typeof(sbyte)) {
				getMethodName = "Int8";
			} else if (t == typeof(short)) {
				getMethodName = "Int16";
			} else if (t == typeof(int)) {
				getMethodName = "Int32";
			} else if (t == typeof(long)) {
				getMethodName = "Int64";
			} else if (t == typeof(DateTime)) {
				getMethodName = "DateTime";
			} else if (t == typeof(Guid)) {
				getMethodName = "Guid";
			} else {
				if (throwOnTypeUnmatch) {
					throw new ApplicationException("DataReader からの " + t + " 値取得メソッドは実装されていません。");
				} else {
					return null;
				}
			}
			if (nt != null) {
				getMethodName = "Nullable" + getMethodName;
			}
			getMethodName = "Get" + getMethodName + (byName ? "ByName" : "ByIndex");

			var getter = typeof(SingleColumnAccessor).GetMethod(getMethodName, BindingFlags.Public | BindingFlags.Static);
			if (getter == null) {
				throw new ApplicationException("内部エラー、 DataReaderAccessor." + getMethodName + " メソッドは存在しません。");
			}

			return getter;
		}
	}

	/// <summary>
	/// DataReader の指定カラムから<typeparamref name="T"/>型の値を取得、列挙する処理を提供する
	/// </summary>
	/// <typeparam name="T">取得値型</typeparam>
	public class DataReaderEnumerableByIndex<T> : IDisposable, IEnumerable<T> {
		DbDataReader DataReader;
		Func<DbDataReader, int, T> Getter;
		int Index;

		public DataReaderEnumerableByIndex(DbDataReader dataReader, Func<DbDataReader, int, T> getter, int index) {
			this.DataReader = dataReader;
			this.Getter = getter;
			this.Index = index;
		}

		public IEnumerator<T> GetEnumerator() {
			var dr = this.DataReader;
			var getter = this.Getter;
			var index = this.Index;
			while (this.DataReader.Read()) {
				yield return getter(dr, index);
			}
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return this.GetEnumerator();
		}

		#region IDisposable Support
		protected virtual void Dispose(bool disposing) {
			if (this.DataReader != null) {
				this.DataReader.Dispose();
				this.DataReader = null;
			}
		}

		~DataReaderEnumerableByIndex() {
			// このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
			Dispose(false);
		}

		// このコードは、破棄可能なパターンを正しく実装できるように追加されました。
		public void Dispose() {
			// このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}

	/// <summary>
	/// DataReader から<typeparamref name="T"/>型の値を取得、列挙する処理を提供する
	/// </summary>
	/// <typeparam name="T">取得値型、基本的に匿名クラス</typeparam>
	public class DataReaderEnumerable<T> : IDisposable, IEnumerable<T> {
		DbDataReader DataReader;
		Func<DbDataReader, T, T> Getter;
		T BaseValue;

		public DataReaderEnumerable(DbDataReader dataReader, Func<DbDataReader, T, T> getter, T baseValue) {
			this.DataReader = dataReader;
			this.Getter = getter;
			this.BaseValue = baseValue;
		}

		public IEnumerator<T> GetEnumerator() {
			var dr = this.DataReader;
			var getter = this.Getter;
			var baseValue = this.BaseValue;
			while (this.DataReader.Read()) {
				yield return getter(dr, baseValue);
			}
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return this.GetEnumerator();
		}

		#region IDisposable Support
		protected virtual void Dispose(bool disposing) {
			if (this.DataReader != null) {
				this.DataReader.Dispose();
				this.DataReader = null;
			}
		}

		~DataReaderEnumerable() {
			// このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
			Dispose(false);
		}

		// このコードは、破棄可能なパターンを正しく実装できるように追加されました。
		public void Dispose() {
			// このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}

	/// <summary>
	/// <see cref="DbDataReader"/>から<typeparamref name="T"/>型の値を取得する処理を提供
	/// </summary>
	/// <typeparam name="T">取得先の値の型</typeparam>
	public static class DataReaderToValue<T> {
		public static readonly Func<DbDataReader, int, T> InvokeByIndex;

		static DataReaderToValue() {
			var type = typeof(T);
			var getter = SingleColumnAccessor.GetMethodByType(type, false);
			InvokeByIndex = (Func<DbDataReader, int, T>)getter.CreateDelegate(typeof(Func<DbDataReader, int, T>));
		}
	}

	public static class EqualTester {
		public static int GetHashCode(byte[] value) {
			if (value == null || value.Length == 0) {
				return 0;
			}
			var hashCode = value[0].GetHashCode();
			for (int i = 1; i < value.Length; i++) {
				hashCode = CombineHashCode(hashCode, value[i].GetHashCode());
			}
			return hashCode;
		}

		public static int GetHashCode(sbyte[] value) {
			if (value == null || value.Length == 0) {
				return 0;
			}
			var hashCode = value[0].GetHashCode();
			for (int i = 1; i < value.Length; i++) {
				hashCode = CombineHashCode(hashCode, value[i].GetHashCode());
			}
			return hashCode;
		}

		public static int GetHashCode(short[] value) {
			if (value == null || value.Length == 0) {
				return 0;
			}
			var hashCode = value[0].GetHashCode();
			for (int i = 1; i < value.Length; i++) {
				hashCode = CombineHashCode(hashCode, value[i].GetHashCode());
			}
			return hashCode;
		}

		public static int GetHashCode(ushort[] value) {
			if (value == null || value.Length == 0) {
				return 0;
			}
			var hashCode = value[0].GetHashCode();
			for (int i = 1; i < value.Length; i++) {
				hashCode = CombineHashCode(hashCode, value[i].GetHashCode());
			}
			return hashCode;
		}

		public static int GetHashCode(int[] value) {
			if (value == null || value.Length == 0) {
				return 0;
			}
			var hashCode = value[0].GetHashCode();
			for (int i = 1; i < value.Length; i++) {
				hashCode = CombineHashCode(hashCode, value[i].GetHashCode());
			}
			return hashCode;
		}

		public static int GetHashCode(uint[] value) {
			if (value == null || value.Length == 0) {
				return 0;
			}
			var hashCode = value[0].GetHashCode();
			for (int i = 1; i < value.Length; i++) {
				hashCode = CombineHashCode(hashCode, value[i].GetHashCode());
			}
			return hashCode;
		}

		public static int GetHashCode(long[] value) {
			if (value == null || value.Length == 0) {
				return 0;
			}
			var hashCode = value[0].GetHashCode();
			for (int i = 1; i < value.Length; i++) {
				hashCode = CombineHashCode(hashCode, value[i].GetHashCode());
			}
			return hashCode;
		}

		public static int GetHashCode(ulong[] value) {
			if (value == null || value.Length == 0) {
				return 0;
			}
			var hashCode = value[0].GetHashCode();
			for (int i = 1; i < value.Length; i++) {
				hashCode = CombineHashCode(hashCode, value[i].GetHashCode());
			}
			return hashCode;
		}

		public static int GetHashCode(decimal[] value) {
			if (value == null || value.Length == 0) {
				return 0;
			}
			var hashCode = value[0].GetHashCode();
			for (int i = 1; i < value.Length; i++) {
				hashCode = CombineHashCode(hashCode, value[i].GetHashCode());
			}
			return hashCode;
		}

		public static int GetHashCode(Guid[] value) {
			if (value == null || value.Length == 0) {
				return 0;
			}
			var hashCode = value[0].GetHashCode();
			for (int i = 1; i < value.Length; i++) {
				hashCode = CombineHashCode(hashCode, value[i].GetHashCode());
			}
			return hashCode;
		}

		public static int GetHashCode(DateTime[] value) {
			if (value == null || value.Length == 0) {
				return 0;
			}
			var hashCode = value[0].GetHashCode();
			for (int i = 1; i < value.Length; i++) {
				hashCode = CombineHashCode(hashCode, value[i].GetHashCode());
			}
			return hashCode;
		}

		public static int GetHashCode(string[] value) {
			if (value == null || value.Length == 0) {
				return 0;
			}
			var s = value[0];
			var hashCode = s != null ? s.GetHashCode() : 0;
			for (int i = 1; i < value.Length; i++) {
				s = value[i];
				hashCode = CombineHashCode(hashCode, s != null ? s.GetHashCode() : 0);
			}
			return hashCode;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int CombineHashCode(int h1, int h2) {
			// RyuJIT optimizes this to use the ROL instruction
			// Related GitHub pull request: dotnet/coreclr#1830
			uint rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
			return ((int)rol5 + h1) ^ h2;
		}

		/// <summary>
		/// 指定のメモリ内容の一致判定を行う
		/// </summary>
		/// <param name="l">左辺値</param>
		/// <param name="r">右辺値</param>
		/// <param name="n"></param>
		/// <returns>一致するなら true</returns>
		public static unsafe bool Equals(byte* l, byte* r, int n) {
			int i = 0;
			for (; ; ) {
				var ni = i + sizeof(IntPtr);
				if (n < ni) {
					break;
				}
				if (*(IntPtr*)(l + i) != *(IntPtr*)(r + i)) {
					return false;
				}
				i = ni;
			}
			for (; i < n; i++) {
				if (l[i] != r[i]) {
					return false;
				}
			}
			return true;
		}

		public static bool Equals(sbyte[] l, sbyte[] r) {
			if ((l == null) != (r == null)) {
				return false;
			}
			if (l == null) {
				return true;
			}
			if (l.Length != r.Length) {
				return false;
			}
			var n = l.Length;
			if (n <= 128) {
				for (int i = 0; i < n; i++) {
					if (l[i] != r[i]) {
						return false;
					}
				}
			} else {
				unsafe {
					fixed (sbyte* pl = l)
					fixed (sbyte* pr = r) {
						return Equals((byte*)pl, (byte*)pr, n);
					}
				}
			}
			return true;
		}

		public static bool Equals(byte[] l, byte[] r) {
			if ((l == null) != (r == null)) {
				return false;
			}
			if (l == null) {
				return true;
			}
			if (l.Length != r.Length) {
				return false;
			}
			var n = l.Length;
			if (n <= 128) {
				for (int i = 0; i < n; i++) {
					if (l[i] != r[i]) {
						return false;
					}
				}
			} else {
				unsafe {
					fixed (byte* pl = l)
					fixed (byte* pr = r) {
						return Equals((byte*)pl, (byte*)pr, n);
					}
				}
			}
			return true;
		}

		public static bool Equals(short[] l, short[] r) {
			if ((l == null) != (r == null)) {
				return false;
			}
			if (l == null) {
				return true;
			}
			if (l.Length != r.Length) {
				return false;
			}
			var n = l.Length;
			if (n <= 128 / sizeof(short)) {
				for (int i = 0; i < n; i++) {
					if (l[i] != r[i]) {
						return false;
					}
				}
			} else {
				unsafe {
					n *= sizeof(short);
					fixed (short* pl = l)
					fixed (short* pr = r) {
						return Equals((byte*)pl, (byte*)pr, n);
					}
				}
			}
			return true;
		}

		public static bool Equals(ushort[] l, ushort[] r) {
			if ((l == null) != (r == null)) {
				return false;
			}
			if (l == null) {
				return true;
			}
			if (l.Length != r.Length) {
				return false;
			}
			var n = l.Length;
			if (n <= 128 / sizeof(ushort)) {
				for (int i = 0; i < n; i++) {
					if (l[i] != r[i]) {
						return false;
					}
				}
			} else {
				unsafe {
					n *= sizeof(ushort);
					fixed (ushort* pl = l)
					fixed (ushort* pr = r) {
						return Equals((byte*)pl, (byte*)pr, n);
					}
				}
			}
			return true;
		}

		public static bool Equals(int[] l, int[] r) {
			if ((l == null) != (r == null)) {
				return false;
			}
			if (l == null) {
				return true;
			}
			if (l.Length != r.Length) {
				return false;
			}
			var n = l.Length;
			if (n <= 128 / sizeof(int)) {
				for (int i = 0; i < n; i++) {
					if (l[i] != r[i]) {
						return false;
					}
				}
			} else {
				unsafe {
					n *= sizeof(int);
					fixed (int* pl = l)
					fixed (int* pr = r) {
						return Equals((byte*)pl, (byte*)pr, n);
					}
				}
			}
			return true;
		}

		public static bool Equals(uint[] l, uint[] r) {
			if ((l == null) != (r == null)) {
				return false;
			}
			if (l == null) {
				return true;
			}
			if (l.Length != r.Length) {
				return false;
			}
			var n = l.Length;
			if (n <= 128 / sizeof(uint)) {
				for (int i = 0; i < n; i++) {
					if (l[i] != r[i]) {
						return false;
					}
				}
			} else {
				unsafe {
					n *= sizeof(uint);
					fixed (uint* pl = l)
					fixed (uint* pr = r) {
						return Equals((byte*)pl, (byte*)pr, n);
					}
				}
			}
			return true;
		}

		public static bool Equals(long[] l, long[] r) {
			if ((l == null) != (r == null)) {
				return false;
			}
			if (l == null) {
				return true;
			}
			if (l.Length != r.Length) {
				return false;
			}
			var n = l.Length;
			if (n <= 128 / sizeof(long)) {
				for (int i = 0; i < n; i++) {
					if (l[i] != r[i]) {
						return false;
					}
				}
			} else {
				unsafe {
					n *= sizeof(long);
					fixed (long* pl = l)
					fixed (long* pr = r) {
						return Equals((byte*)pl, (byte*)pr, n);
					}
				}
			}
			return true;
		}

		public static bool Equals(ulong[] l, ulong[] r) {
			if ((l == null) != (r == null)) {
				return false;
			}
			if (l == null) {
				return true;
			}
			if (l.Length != r.Length) {
				return false;
			}
			var n = l.Length;
			if (n <= 128 / sizeof(ulong)) {
				for (int i = 0; i < n; i++) {
					if (l[i] != r[i]) {
						return false;
					}
				}
			} else {
				unsafe {
					n *= sizeof(ulong);
					fixed (ulong* pl = l)
					fixed (ulong* pr = r) {
						return Equals((byte*)pl, (byte*)pr, n);
					}
				}
			}
			return true;
		}

		public static bool Equals(decimal[] l, decimal[] r) {
			if ((l == null) != (r == null)) {
				return false;
			}
			if (l == null) {
				return true;
			}
			if (l.Length != r.Length) {
				return false;
			}
			var n = l.Length;
			if (n <= 128 / 16) {
				for (int i = 0; i < n; i++) {
					if (l[i] != r[i]) {
						return false;
					}
				}
			} else {
				unsafe {
					n *= sizeof(decimal);
					fixed (decimal* pl = l)
					fixed (decimal* pr = r) {
						return Equals((byte*)pl, (byte*)pr, n);
					}
				}
			}
			return true;
		}

		public static bool Equals(Guid[] l, Guid[] r) {
			if ((l == null) != (r == null)) {
				return false;
			}
			if (l == null) {
				return true;
			}
			if (l.Length != r.Length) {
				return false;
			}
			var n = l.Length;
			if (n <= 128 / 16) {
				for (int i = 0; i < n; i++) {
					if (l[i] != r[i]) {
						return false;
					}
				}
			} else {
				unsafe {
					n *= sizeof(Guid);
					fixed (Guid* pl = l)
					fixed (Guid* pr = r) {
						return Equals((byte*)pl, (byte*)pr, n);
					}
				}
			}
			return true;
		}

		public static bool Equals(DateTime[] l, DateTime[] r) {
			if ((l == null) != (r == null)) {
				return false;
			}
			if (l == null) {
				return true;
			}
			if (l.Length != r.Length) {
				return false;
			}
			var n = l.Length;
			if (n <= 128 / 8) {
				for (int i = 0; i < n; i++) {
					if (l[i] != r[i]) {
						return false;
					}
				}
			} else {
				unsafe {
					n *= sizeof(DateTime);
					fixed (DateTime* pl = l)
					fixed (DateTime* pr = r) {
						return Equals((byte*)pl, (byte*)pr, n);
					}
				}
			}
			return true;
		}

		public static bool Equals(string[] l, string[] r) {
			if ((l == null) != (r == null)) {
				return false;
			}
			if (l == null) {
				return true;
			}
			if (l.Length != r.Length) {
				return false;
			}
			for (int i = 0; i < l.Length; i++) {
				if (l[i] != r[i]) {
					return false;
				}
			}
			return true;
		}
	}

	public static class ExpressionHelper {
		/// <summary>
		/// 準Primitive型一覧（標準で用意されており良く使う値型で）
		/// </summary>
		static readonly Type[] SecondPrimitiveTypes = new Type[] {
			typeof(decimal),
			typeof(DateTime),
			typeof(Guid),
		};

		/// <summary>
		/// 指定値の<see cref="object.GetHashCode"/>を呼び出す式を生成する
		/// </summary>
		/// <param name="valueExpr">値の式</param>
		/// <param name="valueType">値の型</param>
		/// <returns>式</returns>
		public static Expression GetHashCodeFromValue(Expression valueExpr, Type valueType) {
			var getHashCode = valueType.GetMethod("GetHashCode");
			if (getHashCode == null) {
				throw new ApplicationException("内部エラー、型 " + valueType + " に GetHashCode メソッドが存在しません。");
			}

			if (valueType.IsClass) {
				var customGetHashCode = typeof(EqualTester).GetMethod("GetHashCode", new Type[] { valueType });
				if (customGetHashCode != null) {
					// 配列など特殊処理が必要なものならそれ用の処理を呼び出す
					return Expression.Call(null, customGetHashCode, valueExpr);
				} else {
					// メンバがクラスなら null チェック後に GetHashCode() 呼び出し
					return Expression.Condition(Expression.Equal(valueExpr, Expression.Constant(null)), Expression.Constant((int)0), Expression.Call(valueExpr, getHashCode));
				}
			} else {
				// メンバが値型なら普通に GetHashCode() 呼び出し
				return Expression.Call(valueExpr, getHashCode);
			}
		}

		/// <summary>
		/// 指定インスタンスの<see cref="object.GetHashCode"/>を呼び出す式を生成する
		/// </summary>
		/// <param name="instanceExpr">インスタンスの式</param>
		/// <param name="memberInfo">メンバー情報</param>
		/// <param name="memberType">メンバーの型</param>
		/// <returns>式</returns>
		public static Expression GetHashCodeFromMember(Expression instanceExpr, MemberInfo memberInfo, Type memberType) {
			var getHashCode = memberType.GetMethod("GetHashCode");
			if (getHashCode == null) {
				throw new ApplicationException("内部エラー、型 " + memberType + " に GetHashCode メソッドが存在しません。");
			}
			return GetHashCodeFromValue(Expression.MakeMemberAccess(instanceExpr, memberInfo), memberType);
		}

		public static Expression GetNullTest(Expression value, Expression valueIfNotNull, Expression valueIfNull) {
			var nullExpr = Expression.Constant(null);
			var isNull = Expression.Equal(value, nullExpr);
			return Expression.Condition(isNull, valueIfNull, valueIfNotNull);
		}

		public static Expression GetHasValueTest(Type type, Expression value, Expression valueIfNotNull, Expression valueIfNull) {
			var hasValue = type.GetProperty("HasValue");
			return Expression.Condition(Expression.MakeMemberAccess(value, hasValue), valueIfNotNull, valueIfNull);
		}

		public static Expression GetExpressionWithNullTest(Expression left, Expression right, Expression valueIfNotNull, Expression valueIfNull, Expression valueIfNullDifferent) {
			var nullExpr = Expression.Constant(null);
			var leftIsNull = Expression.Equal(left, nullExpr);
			var rightIsNull = Expression.Equal(right, nullExpr);
			var valueEqualsIfLeftNotNull = Expression.Condition(leftIsNull, valueIfNull, valueIfNotNull);
			return Expression.Condition(Expression.Equal(leftIsNull, rightIsNull), valueEqualsIfLeftNotNull, valueIfNullDifferent);
		}

		public static Expression GetExpressionWithHasValue(Type type, Expression left, Expression right, Expression valueIfNotNull, Expression valueIfNull, Expression valueIfNullDifferent) {
			var hasValue = type.GetProperty("HasValue");
			var leftHasValue = Expression.MakeMemberAccess(left, hasValue);
			var rightHasValue = Expression.MakeMemberAccess(right, hasValue);
			var valueEqualsIfLeftHasValue = Expression.Condition(leftHasValue, valueIfNotNull, valueIfNull);
			return Expression.Condition(Expression.Equal(leftHasValue, rightHasValue), valueEqualsIfLeftHasValue, valueIfNullDifferent);
		}

		/// <summary>
		/// 指定の値をメンバーレベルまで再帰的に辿りハッシュ値を計算する
		/// </summary>
		/// <param name="recursiveTest">同じ型に対して再帰的に呼び出されたか判定用</param>
		/// <param name="rootType">判定処理の根っこの型</param>
		/// <param name="type">値の型</param>
		/// <param name="value">値の式</param>
		/// <param name="forImplement"><see cref="object.GetHashCode"/>実装用なら true</param>
		/// <returns>比較結果の式</returns>
		public static Expression GetDeepHashCode(HashSet<Type> recursiveTest, Type rootType, Type type, Expression value, bool forImplement) {
			if (recursiveTest.Contains(type)) {
				// 再帰的に同じ型に戻ってきてしまうならエラー
				throw new ApplicationException("型 " + rootType + " から辿れるメンバに型 " + type + " が再帰的に出現しました。無限ループとなるため GetDeepHashCode のサポート対象外です。");
			}

			var getHashCode = type.GetMethod("GetHashCode", new Type[0]);
			if (getHashCode == null) {
				throw new ApplicationException("内部エラー、型 " + type + " に GetHashCode メソッドが存在しません。");
			}

			if (type.IsPrimitive || SecondPrimitiveTypes.Contains(type)) {
				// 基本型なら普通に GetHashCode 呼び出し
				return Expression.Call(value, getHashCode);
			}

			var zero = Expression.Constant((int)0);

			if (type == typeof(string)) {
				// 文字列なら null チェック後に GetHashCode 呼び出し
				return GetNullTest(value, Expression.Call(value, getHashCode), zero);
			}

			if (!forImplement && getHashCode.DeclaringType == type) {
				// type 型にて GetHashCode をオーバーライドしているなら呼び出す
				if (type.IsValueType) {
					return Expression.Call(value, getHashCode);
				} else {
					return GetNullTest(value, Expression.Call(value, getHashCode), zero);
				}
			}

			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)) {
				// Nullable<> 型なら Nullable<>.Value に対して GetHashCode 呼び出す
				var ct = type.GetGenericArguments()[0];
				var valueProperty = type.GetProperty("Value");
				recursiveTest.Add(type);
				var hashCodeExpr = GetDeepHashCode(recursiveTest, rootType, ct, Expression.MakeMemberAccess(value, valueProperty), forImplement);
				recursiveTest.Remove(type);
				return GetHasValueTest(type, value, hashCodeExpr, zero);
			} else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Col<>)) {
				// Col<> 型なら Col<>.Value に対して GetHashCode 呼び出す
				var ct = type.GetGenericArguments()[0];
				var valueField = type.GetField("Value");
				recursiveTest.Add(type);
				var hashCodeExpr = GetDeepHashCode(recursiveTest, rootType, ct, Expression.MakeMemberAccess(value, valueField), forImplement);
				recursiveTest.Remove(type);
				return GetNullTest(value, hashCodeExpr, zero);
			} else {
				var customGetHashCode = typeof(EqualTester).GetMethod("GetHashCode", new Type[] { type });
				if (customGetHashCode != null) {
					// 配列など特別なハッシュコード計算メソッドがあるならそれを使う
					return Expression.Call(null, customGetHashCode, value);
				} else {
					// 上記以外は各メンバに対してハッシュコードを計算して連結していく
					Expression hashCodeExpr = null;
					var mat = GetMembersAndTypes(type);
					var members = mat.Item1;
					var memberTypes = mat.Item2;
					var hashCombine = typeof(EqualTester).GetMethod("CombineHashCode", new Type[] { typeof(int), typeof(int) });

					for (int i = 0; i < members.Length; i++) {
						var m = members[i];
						var mt = memberTypes[i];
						recursiveTest.Add(type);
						var expr = GetDeepHashCode(recursiveTest, rootType, mt, Expression.MakeMemberAccess(value, m), forImplement);
						recursiveTest.Remove(type);

						if (hashCodeExpr == null) {
							hashCodeExpr = expr;
						} else {
							hashCodeExpr = Expression.Call(null, hashCombine, hashCodeExpr, expr);
						}
					}

					return hashCodeExpr;
				}
			}
		}

		/// <summary>
		/// 指定の２値をメンバーレベルまで再帰的に一致比較する
		/// <para>== 演算子がオーバーロードされていたらそれを使用する</para>
		/// </summary>
		/// <param name="recursiveTest">同じ型に対して再帰的に呼び出されたか判定用</param>
		/// <param name="rootType">判定処理の根っこの型</param>
		/// <param name="type">値の型</param>
		/// <param name="left">右辺値の式</param>
		/// <param name="right">左辺値の式</param>
		/// <param name="forImplement"><see cref="object.Equals(object)"/>、 operator == 実装用なら true</param>
		/// <returns>比較結果の式</returns>
		public static Expression GetDeepEqual(HashSet<Type> recursiveTest, Type rootType, Type type, Expression left, Expression right, bool forImplement) {
			if (recursiveTest.Contains(type)) {
				// 再帰的に同じ型に戻ってきてしまうならエラー
				throw new ApplicationException("型 " + rootType + " から辿れるメンバに型 " + type + " が再帰的に出現しました。無限ループとなるため GetDeepEqual のサポート対象外です。");
			}

			if (type.IsPrimitive || type == typeof(string) || SecondPrimitiveTypes.Contains(type)) {
				// 基本型なら普通に == 呼び出し
				return Expression.Equal(left, right);
			}

			var op_Equality = forImplement ? null : type.GetMethod("op_Equality", BindingFlags.Static | BindingFlags.Public, null, new Type[] { type, type }, new ParameterModifier[0]);
			if (op_Equality != null) {
				// == 演算子オーバーロードされているならそれを使う
				return Expression.Call(null, op_Equality, left, right);
			} else {
				if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)) {
					// Nullable<> 型なら Nullable<>.Value に対して一致判定を行う
					var vt = type.GetGenericArguments()[0];
					var valueProperty = type.GetProperty("Value");
					recursiveTest.Add(type);
					var valueEquals = GetDeepEqual(recursiveTest, rootType, vt, Expression.MakeMemberAccess(left, valueProperty), Expression.MakeMemberAccess(right, valueProperty), forImplement);
					recursiveTest.Remove(type);
					return GetExpressionWithHasValue(type, left, right, valueEquals, Expression.Constant(true), Expression.Constant(false));
				} else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Col<>)) {
					// Col<> 型なら Col<>.Value に対して一致判定を行う
					var ct = type.GetGenericArguments()[0];
					var valueField = type.GetField("Value");
					recursiveTest.Add(type);
					var valueEquals = GetDeepEqual(recursiveTest, rootType, ct, Expression.MakeMemberAccess(left, valueField), Expression.MakeMemberAccess(right, valueField), forImplement);
					recursiveTest.Remove(type);
					return GetExpressionWithNullTest(left, right, valueEquals, Expression.Constant(true), Expression.Constant(false));
				} else {
					var customEquals = typeof(EqualTester).GetMethod("Equals", new Type[] { type, type });
					if (customEquals != null) {
						// 配列など特別一致判定があるならそれを使う
						return Expression.Call(null, customEquals, left, right);
					} else {
						// 上記以外はメンバの一致判定を三項演算子で結合していく
						// if、return は遅くなるので使わない
						Expression valueEquals = null;
						var mat = GetMembersAndTypes(type);
						var members = mat.Item1;
						var memberTypes = mat.Item2;
						var falseExpr = Expression.Constant(false);

						for (int i = members.Length - 1; i != -1; i--) {
							var m = members[i];
							var mt = memberTypes[i];
							recursiveTest.Add(type);
							var eq = GetDeepEqual(recursiveTest, rootType, mt, Expression.MakeMemberAccess(left, m), Expression.MakeMemberAccess(right, m), forImplement);
							recursiveTest.Remove(type);
							if (valueEquals == null) {
								valueEquals = eq;
							} else {
								valueEquals = Expression.Condition(eq, valueEquals, falseExpr);
							}
						}

						if (type.IsValueType) {
							// 構造体なら null はあり得ないのでそのまま比較する
							return valueEquals;
						}

						// クラスなら null チェックを追加する
						return GetExpressionWithNullTest(left, right, valueEquals, Expression.Constant(true), Expression.Constant(false));
					}
				}
			}
		}

		public static Tuple<MemberInfo[], Type[]> GetMembersAndTypes(Type type) {
			// プロパティとフィールド一覧取得、両方存在する場合は順序が定かではなくなるため対応できない
			var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
			var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
			if (fields.Length != 0 && properties.Length != 0) {
				throw new ApplicationException(type + " 型はフィールドとプロパティ両方を使用しており、マッピングする列順を判定できないため使用できません。");
			} else if (fields.Length == 0 && properties.Length == 0) {
				throw new ApplicationException(type + " 型はフィールドとプロパティ両方が存在せず列のマッピングが行えません。");
			}

			// メンバー情報一覧取得
			MemberInfo[] members;
			Type[] memberTypes;
			if (fields.Length != 0) {
				members = fields;
				memberTypes = fields.Select(p => p.FieldType).ToArray();
			} else {
				members = properties;
				memberTypes = properties.Select(p => p.PropertyType).ToArray();
			}

			return new Tuple<MemberInfo[], Type[]>(members, memberTypes);
		}

		/// <summary>
		/// 指定メンバの型の取得
		/// </summary>
		/// <param name="mi">メンバ情報</param>
		/// <returns>型</returns>
		public static Type MemberValueType(MemberInfo mi) {
			switch (mi.MemberType) {
			case MemberTypes.Field:
				return (mi as FieldInfo).FieldType;
			case MemberTypes.Property:
				return (mi as PropertyInfo).PropertyType;
			default:
				throw new ApplicationException("内部エラー、メンバータイプ " + mi.MemberType + " は対象外です。");
			}
		}
	}

	/// <summary>
	/// <typeparamref name="T"/>型に対してのメンバレベルで一致判定、ハッシュコード生成コードを動的に生成する
	/// </summary>
	/// <typeparam name="T">処理生成対象の型</typeparam>
	public static class EqualTester<T> {
		/// <summary>
		/// <see cref="T"/>型の値からハッシュコードを生成する処理
		/// </summary>
		public new static readonly Func<T, int> GetHashCode;

		/// <summary>
		/// <see cref="T"/>型の値の一致判定を行う処理
		/// </summary>
		public new static readonly Func<T, T, bool> Equals;

		static EqualTester() {
			var mat = ExpressionHelper.GetMembersAndTypes(typeof(T));
			var members = mat.Item1;
			var memberTypes = mat.Item2;

			GetHashCode = GenerateGetHashCode(members, memberTypes, false);
			Equals = GenerateEquals(members, memberTypes, false);
		}

		static Func<T, int> GenerateGetHashCode(MemberInfo[] members, Type[] memberTypes, bool forImplement) {
			var type = typeof(T);
			var paramInstance = Expression.Parameter(type);
			var recursiveTest = new HashSet<Type>();
			var tree = ExpressionHelper.GetDeepHashCode(recursiveTest, type, type, paramInstance, forImplement);
			return Expression.Lambda<Func<T, int>>(tree, paramInstance).Compile();
		}

		static Func<T, T, bool> GenerateEquals(MemberInfo[] members, Type[] memberTypes, bool forImplement) {
			var type = typeof(T);
			var paramLeft = Expression.Parameter(type);
			var paramRight = Expression.Parameter(type);
			var recursiveTest = new HashSet<Type>();
			var tree = ExpressionHelper.GetDeepEqual(recursiveTest, type, type, paramLeft, paramRight, forImplement);
			return Expression.Lambda<Func<T, T, bool>>(tree, paramLeft, paramRight).Compile();
		}
	}

	/// <summary>
	/// <typeparamref name="T"/>型の値を保持し、<see cref="Dictionary{TKey, TValue}"/>等のキーとして扱える様に一致判定、ハッシュコード生成機能を提供する
	/// <para>一致判定処理、ハッシュコード生成処理は動的コード生成により作成される</para>
	/// </summary>
	/// <typeparam name="T">保持する値の型</typeparam>
	public class KeyOf<T> : IEquatable<KeyOf<T>> {
		static readonly Func<T, int> _GetHashCode = EqualTester<T>.GetHashCode;
		static readonly Func<T, T, bool> _Equals = EqualTester<T>.Equals;

		int _HashCode;

		/// <summary>
		/// 値
		/// </summary>
		public readonly T Value;

		public KeyOf(T value) {
			this.Value = value;
			_HashCode = _GetHashCode(value);
		}

		public override int GetHashCode() {
			return _HashCode;
		}

		public override bool Equals(object obj) {
			var c = obj as KeyOf<T>;
			return object.ReferenceEquals(c, null) ? false : _Equals(this.Value, c.Value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(KeyOf<T> other) {
			return _Equals(this.Value, other.Value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(KeyOf<T> l, KeyOf<T> r) {
			return _Equals(l.Value, r.Value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(KeyOf<T> l, KeyOf<T> r) {
			return !_Equals(l.Value, r.Value);
		}
	}

	public static class ColExpression {
		public static bool GetValue(Type type, Expression col, out Type valueType, out Expression value) {
			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Col<>)) {
				// Col<> に指定されたジェネリック型の取得
				valueType = type.GetGenericArguments()[0];
				// Value 値の式取得
				value = Expression.Field(col, type.GetField("Value"));
				return true;
			} else {
				valueType = null;
				value = null;
				return false;
			}
		}
	}

	public static class DataReaderExpression {
		public static Expression ReadClass(Type type, Expression dataReader, ref int index, Expression baseValue) {
			// メンバー情報一覧取得
			var mat = ExpressionHelper.GetMembersAndTypes(type);
			var members = mat.Item1;
			var memberTypes = mat.Item2;

			// 各メンバーに設定する値を生成する式一覧生成
			var memberSourceExprs = new Expression[members.Length];
			for (int i = 0; i < memberSourceExprs.Length; i++) {
				memberSourceExprs[i] = ReadMemberValue(members[i], dataReader, ref index, baseValue);
			}

			var ctor = type.GetConstructor(memberTypes);
			if (ctor != null) {
				// コンストラクタが使えるなら new でコンストラクタ呼び出し
				return Expression.New(ctor, memberSourceExprs);
			} else {
				// コンストラクタ使えないならデフォルトコンストラクタで生成してメンバー毎に設定していく処理を生成

				// デフォルトコンストラクタ取得
				ctor = type.GetConstructor(new Type[0]);
				if (ctor == null) {
					throw new ApplicationException(type + " にデフォルトコンストラクタが存在しないため DataReader からのマッピングが行えません。");
				}

				var result = Expression.Parameter(type);
				var expressions = new List<Expression>();

				// type 型の一時変数作成
				expressions.Add(Expression.Assign(result, Expression.New(ctor)));

				// 一時変数のメンバに値を取得していく
				for (int i = 0; i < memberSourceExprs.Length; i++) {
					expressions.Add(Expression.Assign(Expression.MakeMemberAccess(result, members[i]), memberSourceExprs[i]));
				}

				// 一時変数を戻り値とする
				expressions.Add(result);

				return Expression.Block(new ParameterExpression[] { result }, expressions);
			}
		}

		public static Expression ReadMemberValue(MemberInfo mi, Expression dataReader, ref int index, Expression baseValue) {
			// メンバー名
			var name = mi.Name;

			// カラム指定
			Expression colId;
			string colSymbol;
			if (index < 0) {
				colId = Expression.Constant(name);
				colSymbol = "カラム名 " + name;
			} else {
				colId = Expression.Constant(index);
				colSymbol = "カラムインデックス " + index;
			}

			var mt = ExpressionHelper.MemberValueType(mi);
			if (mt.IsGenericType && mt.GetGenericTypeDefinition() == typeof(Col<>)) {
				// メンバが Col<> 型の場合

				// Col<> に指定されたジェネリック型の取得
				var ct = mt.GetGenericArguments()[0];

				// DataReader から指定型の値を取得するデリゲートの取得
				MethodInfo getter;
				try {
					getter = SingleColumnAccessor.GetMethodByType(ct, index < 0);
				} catch (Exception ex) {
					throw new ApplicationException("DataReader の" + colSymbol + " から型 " + mt + " の値への変換メソッドが存在しません。", ex);
				}

				// Col<> のコンストラクタ取得、ベースとなる Col<> と値を指定して初期化するコンストラクタを取得する
				var pctor = mt.GetConstructor(new Type[] { mt, ct });
				if (pctor == null) {
					throw new ApplicationException("内部エラー、Col<T>(" + mt.Name + ", " + ct.Name + ") コンストラクタが存在しません。");
				}

				// ベースとなる Col<>
				var col = Expression.MakeMemberAccess(baseValue, mi);

				// DataReader から取得した値を基に Col<> を new する処理
				if (0 <= index) {
					index++;
				}
				return Expression.New(pctor, col, Expression.Call(null, getter, dataReader, colId));
			} else {
				// DataReader から指定型の値を取得するデリゲートの取得
				MethodInfo getter;
				try {
					getter = SingleColumnAccessor.GetMethodByType(mt, index < 0, false);
					if (getter == null) {
						// 列に直接マッピングできないならクラスを想定
						return ReadClass(mt, dataReader, ref index, Expression.MakeMemberAccess(baseValue, mi));
					}
				} catch (Exception ex) {
					throw new ApplicationException("DataReader の" + colSymbol + " から型 " + mt + " の値への変換メソッドが存在しません。", ex);
				}

				// DataReader から値取得する処理
				if (0 <= index) {
					index++;
				}
				return Expression.Call(null, getter, dataReader, colId);
			}
		}
	}

	/// <summary>
	/// <see cref="DbDataReader"/>から値を読み込みプロパティまたはフィールドへ値を設定する処理を提供
	/// </summary>
	/// <typeparam name="T">プロパティまたはフィールドを持つクラス型、基本的に匿名クラス</typeparam>
	public static class DataReaderToCols<T> {
		public static readonly Func<DbDataReader, T, T> InvokeByIndex;
		public static readonly Func<DbDataReader, T, T> InvokeByName;

		static DataReaderToCols() {
			InvokeByIndex = Generate(false);
			InvokeByName = Generate(false);
		}

		/// <summary>
		/// DataReader から<typeparamref name="T"/>へ値を読み込むデリゲートを生成する
		/// </summary>
		/// <param name="byName">列名とプロパティ名をマッチングさせて取得するなら true 、列順とプロパティ順を使うなら false</param>
		/// <returns>DataReader から<typeparamref name="T"/>へ値を読み込むデリゲート</returns>
		static Func<DbDataReader, T, T> Generate(bool byName) {
			var type = typeof(T);

			// 作成するデリゲートの入力引数となる式
			var dataReader = Expression.Parameter(typeof(DbDataReader));
			var baseValue = Expression.Parameter(type);

			// アクセス先の列インデックス
			int index = byName ? -1 : 0;

			// DataReader から値を読み込み T 型のオブジェクトを生成する式を生成
			var expression = DataReaderExpression.ReadClass(type, dataReader, ref index, baseValue);

			return Expression.Lambda<Func<DbDataReader, T, T>>(
				expression,
				dataReader,
				baseValue
			).Compile();
		}
	}
}
