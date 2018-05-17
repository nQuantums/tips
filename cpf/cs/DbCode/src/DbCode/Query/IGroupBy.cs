using System;
using System.Collections.Generic;
using System.Text;

namespace DbCode.Query {
	/// <summary>
	/// GROUP BY句の機能を提供する
	/// </summary>
	public interface IGroupBy : IQueryNode {
		/// <summary>
		/// GROUP BY句の列一覧
		/// </summary>
		IEnumerable<Column> Columns { get; }
	}
}
