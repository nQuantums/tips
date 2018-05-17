using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;
using System.Reflection;

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
	public enum ColumnFlags {
		Nullable = 1 << 0,
		Serial = 1 << 1,
		DefaultCurrentTimestamp = 1 << 2,
	}
}
