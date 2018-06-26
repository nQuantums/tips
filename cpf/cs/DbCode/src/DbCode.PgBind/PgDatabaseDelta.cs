using System;
using System.Collections.Generic;
using System.Text;
using DbCode.Defs;

namespace DbCode.PgBind {
	/// <summary>
	/// データベースの変化分
	/// </summary>
	public class PgDatabaseDelta : IDatabaseDelta {
		/// <summary>
		/// データベース名
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// ドロップすべきテーブル定義の配列
		/// </summary>
		public IEnumerable<ITableDef> TablesToDrop { get; set; }

		/// <summary>
		/// 新たに追加するテーブル定義の配列
		/// </summary>
		public IEnumerable<ITableDef> TablesToAdd { get; set; }

		/// <summary>
		/// 変化があったテーブル定義の配列
		/// </summary>
		public IEnumerable<ITableDelta> TablesToModify { get; set; }
	}
}
