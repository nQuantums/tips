using System;
using System.Collections.Generic;
using System.Text;

namespace CodeDb.Query {
	/// <summary>
	/// ORDER BY句の機能を提供する
	/// </summary>
	public interface IOrderBy : IQueryNode {
		/// <summary>
		/// ORDER BY句の列一覧
		/// </summary>
		IEnumerable<Column> Columns { get; }
	}
}
