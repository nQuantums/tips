using System;
using System.Collections.Generic;
using System.Text;

namespace CodeDb.PgBinder {
	/// <summary>
	/// テーブルの変化分
	/// </summary>
	public class PgTableDelta : ITableDelta {
		/// <summary>
		/// テーブル名
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// ドロップすべき列定義の配列
		/// </summary>
		public IEnumerable<IColumnDef> ColumnsToDrop { get; set; }

		/// <summary>
		/// 新たに追加すべき列定義の配列
		/// </summary>
		public IEnumerable<IColumnDef> ColumnsToAdd { get; set; }

		/// <summary>
		/// ドロップすべきプライマリキー定義
		/// </summary>
		public IPrimaryKeyDef PrimaryKeyToDrop { get; set; }

		/// <summary>
		/// 新たに追加すべきプライマリキー定義
		/// </summary>
		public IPrimaryKeyDef PrimaryKeyToAdd { get; set; }

		/// <summary>
		/// ドロップすべきインデックス定義の配列
		/// </summary>
		public IEnumerable<IIndexDef> IndicesToDrop { get; set; }

		/// <summary>
		/// 新たに追加すべきインデックス定義の配列
		/// </summary>
		public IEnumerable<IIndexDef> IndicesToAdd { get; set; }
	}
}
