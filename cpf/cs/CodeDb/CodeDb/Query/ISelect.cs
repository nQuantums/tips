using System;
using System.Collections.Generic;
using System.Text;

namespace CodeDb.Query {
	/// <summary>
	/// SELECT句のノード機能を提供する
	/// </summary>
	public interface ISelect : ITable, IQueryNode {
	}

	/// <summary>
	/// 列クラス型指定のSELECT句のノード機能を提供する
	/// </summary>
	/// <typeparam name="TColumns">プロパティを列として扱う<see cref="TableDef{TColumns}"/>のTColumnsに該当するクラス</typeparam>
	public interface ISelect<TColumns> : ISelect, ITable<TColumns> {
		/// <summary>
		/// <see cref="ICodeDbDataReader"/>から<typeparamref name="TColumns"/>を列挙するファンクション
		/// </summary>
		Func<ICodeDbDataReader, IEnumerable<TColumns>> Reader { get; }
	}
}
