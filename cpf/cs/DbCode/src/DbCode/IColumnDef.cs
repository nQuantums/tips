using System;
using System.Collections.Generic;
using System.Text;

namespace DbCode {
	/// <summary>
	/// 列定義
	/// </summary>
	public interface IColumnDef {
		/// <summary>
		/// DB上での列名
		/// </summary>
		string Name { get; }

		/// <summary>
		/// 列の型
		/// </summary>
		IDbType DbType { get; }

		/// <summary>
		/// 列のオプションフラグ
		/// </summary>
		ColumnFlags Flags { get; }
	}
}
