using System;
using System.Collections.Generic;
using System.Text;

namespace DbCode.PgBind {
	/// <summary>
	/// 列定義
	/// </summary>
	public class PgColumnDef : IColumnDef {
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
		/// コンストラクタ、全要素を指定して初期化する
		/// </summary>
		/// <param name="name">DB上での列名</param>
		/// <param name="dbType">列の型</param>
		/// <param name="flags">列のオプションフラグ</param>
		public PgColumnDef(string name, PgDbType dbType, ColumnFlags flags) {
			this.Name = name;
			this.DbType = dbType;
			this.Flags = flags;
		}
	}
}
