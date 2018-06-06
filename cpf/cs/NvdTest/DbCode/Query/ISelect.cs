using System;
using System.Collections.Generic;
using System.Text;

namespace DbCode.Query {
	/// <summary>
	/// SELECT句のノード機能を提供する
	/// </summary>
	public interface ISelect : ITable, IQueryNode {
	}

	/// <summary>
	/// 列クラス型指定のSELECT句のノード機能を提供する
	/// </summary>
	/// <typeparam name="TColumns">プロパティを列として扱う<see cref="TableDef{TColumns}"/>クラス、クエリ結果読み込み時の１レコードに対応する</typeparam>
	public interface ISelect<TColumns> : ISelect, ITable<TColumns> {
	}
}
