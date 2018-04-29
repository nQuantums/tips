using System;
using System.Collections.Generic;
using System.Text;

namespace CodeDb.Query {
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
		/// SQL文を生成する
		/// </summary>
		/// <param name="context">生成先のコンテキスト</param>
		public void BuildSql(ElementCode context) {
			context.Add(SqlKeyword.DropTable, SqlKeyword.IfExists);
			context.Concat(this.Table.Name);
		}
		#endregion
	}
}
