using System;
using System.Collections.Generic;
using System.Text;

namespace CodeDb {
	/// <summary>
	/// テーブル定義の基本機能を提供する
	/// </summary>
	public interface ITableDef {
		/// <summary>
		/// テーブル名
		/// </summary>
		string Name { get; }

		/// <summary>
		/// テーブルが直接保持する列定義の取得
		/// </summary>
		IEnumerable<IColumnDef> ColumnDefs { get; }

		/// <summary>
		/// プライマリキー定義を取得する、派生先クラスでオーバーライドする必要がある
		/// </summary>
		/// <returns>プライマリキー定義</returns>
		IPrimaryKeyDef GetPrimaryKey();

		/// <summary>
		/// インデックス定義を取得する、派生先クラスでオーバーライドする必要がある
		/// </summary>
		/// <returns>インデックス定義列</returns>
		IEnumerable<IIndexDef> GetIndices();
	}
}
