using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DbCode;
using DbCode.Query;

namespace DbCode.Internal {
	public static class QueryNodeHelper {
		public static void SwitchParent(IQueryNode child, IQueryNode newParent) {
			var parent = child.Parent;
			if (parent != null) {
				parent.RemoveChild(child);
			}
			child.ChangeParent(newParent);
		}

		public static IQueryNode GetRootNode(IQueryNode node) {
			// 所有者(Sql クラス)が親なら自分がルート
			var owner = node.Owner;
			if (owner == node) {
				return node;
			}

			// 所有者の直前まで辿ったものがルートとなる
			IQueryNode parent;
			while ((parent = node.Parent) != null) {
				if (parent == owner) {
					return node;
				}
				node = parent;
			}

			return node;
		}
	}
}
