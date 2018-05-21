using System;
using System.Collections.Generic;
using System.Text;

namespace DbCode.Query {
	/// <summary>
	/// DROP TABLE句の機能を提供するノード
	/// </summary>
	public class DropTable : IDropTable {
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
		/// 破棄するテーブル定義
		/// </summary>
		public ITableDef Table { get; private set; }
		#endregion

		#region コンストラクタ
		/// <summary>
		/// コンストラクタ、親ノードと破棄するテーブル定義を指定して初期化する
		/// </summary>
		/// <param name="parent">親ノード</param>
		/// <param name="table">破棄するテーブル定義</param>
		public DropTable(IQueryNode parent, ITableDef table) {
			this.Parent = parent;
			this.Table = table;
		}
		#endregion

		#region 公開メソッド
		/// <summary>
		/// 指定ノードを子とする、既存の親は<see cref="RemoveChild(IQueryNode)"/>で切り離す必要がある
		/// </summary>
		/// <param name="child">子ノード</param>
		public void AddChild(IQueryNode child) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// 指定の子ノードを取り除く
		/// </summary>
		/// <param name="child">子ノード</param>
		public void RemoveChild(IQueryNode child) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// 親ノードが変更された際に呼び出される
		/// </summary>
		/// <param name="parent">新しい親ノード</param>
		public void ChangeParent(IQueryNode parent) {
			this.Parent = parent;
		}

		/// <summary>
		/// SQL文を生成する
		/// </summary>
		/// <param name="context">生成先のコンテキスト</param>
		public void ToElementCode(ElementCode context) {
			context.Add(SqlKeyword.DropTable, SqlKeyword.IfExists);
			context.Concat(this.Table.Name);
		}

		public override string ToString() {
			try {
				var ec = new ElementCode();
				this.ToElementCode(ec);
				return ec.ToString();
			} catch {
				return "";
			}
		}
		#endregion
	}
}
