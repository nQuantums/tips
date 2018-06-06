using System;
using System.Collections.Generic;
using System.Text;

namespace DbCode.Query {
	/// <summary>
	/// クエリツリーのノード
	/// </summary>
	public interface IQueryNode : IElementizable {
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

		/// <summary>
		/// 指定ノードを子とする、既存の親は<see cref="RemoveChild(IQueryNode)"/>で切り離す必要がある
		/// </summary>
		/// <param name="child">子ノード</param>
		void AddChild(IQueryNode child);

		/// <summary>
		/// 指定の子ノードを取り除く
		/// </summary>
		/// <param name="child">子ノード</param>
		void RemoveChild(IQueryNode child);

		/// <summary>
		/// 親ノードが変更された際に呼び出される
		/// </summary>
		/// <param name="parent">新しい親ノード</param>
		void ChangeParent(IQueryNode parent);
	}
}
