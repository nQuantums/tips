using System;
using System.Collections.Generic;
using System.Text;

namespace CodeDb.Query {
	/// <summary>
	/// DROP TABLE句の機能を提供するノード
	/// </summary>
	public interface IDropTable : IQueryNode {
		/// <summary>
		/// 破棄するテーブル定義
		/// </summary>
		ITableDef Table { get; }
	}
}
