using System;
using System.Collections.Generic;
using System.Text;

namespace CodeDb.Query {
	/// <summary>
	/// クエリツリーのノード
	/// </summary>
	public interface IQueryNode : ISqlBuildable {
		/// <summary>
		/// 所有者
		/// </summary>
		Sql Owner { get; }

		/// <summary>
		/// 親ノード
		/// </summary>
		IQueryNode Parent { get; }

		/// <summary>
		/// 子ノード一覧
		/// </summary>
		IEnumerable<IQueryNode> Children { get; }
	}
}
