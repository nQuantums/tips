using System;
using System.Collections.Generic;
using System.Text;

namespace CodeDb.Query {
	/// <summary>
	/// クエリのノードに<see cref="ElementCode"/>をそのまま使用するもの
	/// </summary>
	public class CodeNode : IQueryNode {
		#region プロパティ
		/// <summary>
		/// ノードが属するSQLオブジェクト
		/// </summary>
		public Sql Owner => this.Parent.Owner;

		/// <summary>
		/// 親ノード
		/// </summary>
		public IQueryNode Parent { get; private set; }

		/// <summary>
		/// 子ノード一覧
		/// </summary>
		public IEnumerable<IQueryNode> Children => null;

		/// <summary>
		/// コード
		/// </summary>
		public ElementCode Code { get; private set; }
		#endregion

		#region コンストラクタ
		/// <summary>
		/// コンストラクタ、親ノードとコードを指定して初期化する
		/// </summary>
		/// <param name="parent">親ノード</param>
		/// <param name="code">コード</param>
		public CodeNode(IQueryNode parent, ElementCode code) {
			this.Parent = parent;
			this.Code = code;
		}
		#endregion

		#region 公開メソッド
		/// <summary>
		/// SQL文を生成する
		/// </summary>
		/// <param name="context">生成先のコンテキスト</param>
		public void ToElementCode(ElementCode context) {
			context.Add(this.Code);
		}
		#endregion
	}
}
