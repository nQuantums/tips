using System;
using System.Collections.Generic;
using System.Text;

namespace CodeDb.Query {
	/// <summary>
	/// SELECT式
	/// </summary>
	public interface ISelect : ITable, IQueryNode {
	}

	/// <summary>
	/// 列クラス型指定のSELECT式
	/// </summary>
	/// <typeparam name="TColumns">プロパティを列として扱うクラス</typeparam>
	public interface ISelect<TColumns> : ISelect, ITable<TColumns> {
		/// <summary>
		/// <see cref="ICodeDbDataReader"/>から<see cref="TColumns"/>を列挙するファンクション
		/// </summary>
		Func<ICodeDbDataReader, IEnumerable<TColumns>> Reader { get; }
	}
}
