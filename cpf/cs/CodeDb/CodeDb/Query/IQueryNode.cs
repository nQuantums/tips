using System;
using System.Collections.Generic;
using System.Text;

namespace CodeDb.Query {
	/// <summary>
	/// クエリツリーのノード
	/// </summary>
	public interface IQueryNode : ISqlBuildable {
		/// <summary>
		/// 根ノード
		/// </summary>
		IQueryNode RootNode { get; }

		/// <summary>
		/// 親ノード
		/// </summary>
		IQueryNode ParentNode { get; }

		/// <summary>
		/// 子ノード一覧
		/// </summary>
		IEnumerable<IQueryNode> ChildrenNodes { get; }
	}
}
