using System;
using System.Collections.Generic;
using System.Text;

namespace DbCode {
	/// <summary>
	/// データベース定義に関する機能を提供する
	/// </summary>
	public interface IDatabaseDef {
		/// <summary>
		/// データベース名
		/// </summary>
		string Name { get; }

		/// <summary>
		/// データベース内の全テーブル定義の取得
		/// </summary>
		IEnumerable<ITableDef> Tables { get; }
	}
}
