using System;
using System.Collections.Generic;
using System.Text;

namespace CodeDb {
	/// <summary>
	/// ユニーク制約定義
	/// </summary>
	public interface IUniqueDef {
		/// <summary>
		/// 制約名
		/// </summary>
		string Name { get; }

		/// <summary>
		/// ユニーク制約を構成する列定義の配列
		/// </summary>
		IEnumerable<IColumnDef> Columns { get; }
	}
}
