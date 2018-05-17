using System;
using System.Collections.Generic;
using System.Text;

namespace DbCode {
	/// <summary>
	/// インデックス定義
	/// </summary>
	public interface IIndexDef {
		/// <summary>
		/// インデックス名
		/// </summary>
		string Name { get; }

		/// <summary>
		/// インデックスのオプションフラグ
		/// </summary>
		IndexFlags Flags { get; }

		/// <summary>
		/// インデックスを構成する列定義の配列
		/// </summary>
		IEnumerable<IColumnDef> Columns { get; }
	}
}
