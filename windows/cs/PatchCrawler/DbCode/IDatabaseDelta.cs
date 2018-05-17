using System;
using System.Collections.Generic;
using System.Text;

namespace DbCode {
	/// <summary>
	/// データベースの変化分
	/// </summary>
	public interface IDatabaseDelta {
		/// <summary>
		/// データベース名
		/// </summary>
		string Name { get; }

		/// <summary>
		/// ドロップすべきテーブル定義の配列
		/// </summary>
		IEnumerable<ITableDef> TablesToDrop { get; }

		/// <summary>
		/// 新たに追加すべきテーブル定義の配列
		/// </summary>
		IEnumerable<ITableDef> TablesToAdd { get; }

		/// <summary>
		/// 変化があったテーブル定義の配列
		/// </summary>
		IEnumerable<ITableDelta> TablesToModify { get; }
	}
}
