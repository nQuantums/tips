using System;
using System.Collections.Generic;
using System.Text;

namespace CodeDb {
	/// <summary>
	/// プライマリキー定義
	/// </summary>
	public interface IPrimaryKeyDef {
		/// <summary>
		/// プライマリキー名
		/// </summary>
		string Name { get; }

		/// <summary>
		/// プライマリキーを構成する列定義の配列
		/// </summary>
		IEnumerable<IColumnDef> Columns { get; }
	}
}
