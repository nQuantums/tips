using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;
using System.Reflection;
using DbCode.Defs;

namespace DbCode {
	/// <summary>
	/// 列定義、プロパティと列を結びつける情報を持つ
	/// </summary>
	public class Column : IColumnDef {
		#region プロパティ
		/// <summary>
		/// DB接続環境
		/// </summary>
		public DbEnvironment Environment { get; private set; }

		/// <summary>
		/// 列プロパティを持つインスタンス
		/// </summary>
		public object Instance { get; private set; }

		/// <summary>
		/// 列に対応する<see cref="Instance"/>オブジェクトのプロパティ情報
		/// </summary>
		public PropertyInfo Property { get; private set; }

		/// <summary>
		/// この列が所属する<see cref="ITable"/>
		/// </summary>
		public ITable Table { get; private set; }

		/// <summary>
		/// DB上での列名
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// 列の型
		/// </summary>
		public IDbType DbType { get; private set; }

		/// <summary>
		/// 列のオプションフラグ
		/// </summary>
		public ColumnFlags Flags { get; private set; }

		/// <summary>
		/// この列を生成するための式
		/// </summary>
		public ElementCode Source { get; private set; }
		#endregion

		#region 公開メソッド
		/// <summary>
		/// プロパティに結びつく列定義として初期化する
		/// </summary>
		/// <param name="environment">この列定義がベースとするDB接続環境</param>
		/// <param name="instance">プロパティを直接保持するオブジェクト</param>
		/// <param name="property">プロパティ情報</param>
		/// <param name="table">この列が所属する<see cref="ITable"/></param>
		/// <param name="name">DB上の列名</param>
		/// <param name="dbType">DB上の型</param>
		/// <param name="flags">列定義オプションフラグ</param>
		/// <param name="source">列を生成する元となった式</param>
		public Column(DbEnvironment environment, object instance, PropertyInfo property, ITable table, string name, IDbType dbType, ColumnFlags flags = 0, ElementCode source = null) {
			this.Environment = environment;
			this.Instance = instance;
			this.Property = property;
			this.Table = table;
			this.Name = name;
			this.DbType = dbType;
			this.Flags = flags;
			this.Source = source;
		}

		/// <summary>
		/// 指定引数部分を置き換えてクローンを作成する
		/// </summary>
		/// <param name="instance">プロパティを直接保持するオブジェクト</param>
		/// <param name="table">新しく所属する<see cref="ITable"/></param>
		/// <returns>クローン</returns>
		public Column Clone(object instance, ITable table) {
			var c = this.MemberwiseClone() as Column;
			c.Instance = instance;
			c.Table = table;
			return c;
		}

		/// <summary>
		/// 指定引数部分を置き換えてクローンを作成する
		/// </summary>
		/// <param name="source">列を生成する元となった式</param>
		/// <returns>クローン</returns>
		public Column Clone(ElementCode source) {
			var c = this.MemberwiseClone() as Column;
			c.Source = source;
			return c;
		}

		public override string ToString() {
			return this.Name;
		}
		#endregion
	}

	/// <summary>
	/// 列定義のオプションフラグ
	/// </summary>
	[Flags]
	public enum ColumnFlags : ulong {
		/// <summary>
		/// NULL許容
		/// </summary>
		Nullable = 1u << 0,

		/// <summary>
		/// プライマリキーとする、複数列に指定されたら複合プライマリキーとなる
		/// </summary>
		PrimaryKey = 1u << 1,

		/// <summary>
		/// １から始まる自動採番とする
		/// </summary>
		Serial = 1u << 2,

		/// <summary>
		/// インデックスを付与する、複数列に指定されたら複合インデックスとなる
		/// </summary>
		Index_1 = 1u << 4,

		/// <summary>
		/// インデックスを付与する、複数列に指定されたら複合インデックスとなる
		/// </summary>
		Index_2 = 1u << 5,

		/// <summary>
		/// インデックスを付与する、複数列に指定されたら複合インデックスとなる
		/// </summary>
		Index_3 = 1u << 6,

		/// <summary>
		/// インデックスを付与する、複数列に指定されたら複合インデックスとなる
		/// </summary>
		Index_4 = 1u << 7,

		/// <summary>
		/// インデックスマスク
		/// </summary>
		IndexMask = Index_1 | Index_2 | Index_3 | Index_4,

		/// <summary>
		/// UNIQUE制約とする、複数列に指定されたら複合列のUNIQUE制約となる
		/// </summary>
		Unique_1 = 1u << 8,

		/// <summary>
		/// UNIQUE制約とする、複数列に指定されたら複合列のUNIQUE制約となる
		/// </summary>
		Unique_2 = 1u << 9,

		/// <summary>
		/// UNIQUE制約とする、複数列に指定されたら複合列のUNIQUE制約となる
		/// </summary>
		Unique_3 = 1u << 10,

		/// <summary>
		/// UNIQUE制約とする、複数列に指定されたら複合列のUNIQUE制約となる
		/// </summary>
		Unique_4 = 1u << 11,

		/// <summary>
		/// UNIQUEマスク
		/// </summary>
		UniqueMask = Unique_1 | Unique_2 | Unique_3 | Unique_4,

		/// <summary>
		/// インデックスと一緒に指定すると gin インデックスとなる
		/// </summary>
		Gin = 1u << 62,

		/// <summary>
		/// デフォルト値として現在日時を使用する
		/// </summary>
		DefaultCurrentTimestamp = 1u << 63,
	}
}
