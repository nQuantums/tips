using System;
using System.Collections.Generic;
using System.Text;

namespace DbCode {
	/// <summary>
	/// テーブルの変化分
	/// </summary>
	public interface ITableDelta {
		/// <summary>
		/// テーブル名
		/// </summary>
		string Name { get; }

		/// <summary>
		/// ドロップすべき列定義の配列
		/// </summary>
		IEnumerable<IColumnDef> ColumnsToDrop { get; }

		/// <summary>
		/// 新たに追加すべき列定義の配列
		/// </summary>
		IEnumerable<IColumnDef> ColumnsToAdd { get; }

		/// <summary>
		/// ドロップすべきプライマリキー定義
		/// </summary>
		IPrimaryKeyDef PrimaryKeyToDrop { get; }

		/// <summary>
		/// 新たに追加すべきプライマリキー定義
		/// </summary>
		IPrimaryKeyDef PrimaryKeyToAdd { get; }

		/// <summary>
		/// ドロップすべきインデックス定義の配列
		/// </summary>
		IEnumerable<IIndexDef> IndicesToDrop { get; }

		/// <summary>
		/// 新たに追加すべきインデックス定義の配列
		/// </summary>
		IEnumerable<IIndexDef> IndicesToAdd { get; }

		/// <summary>
		/// ドロップすべきユニーク制約定義の配列
		/// </summary>
		IEnumerable<IUniqueDef> UniquesToDrop { get; }

		/// <summary>
		/// 新たに追加すべきユニーク制約定義の配列
		/// </summary>
		IEnumerable<IUniqueDef> UniquesToAdd { get; }
	}
}
