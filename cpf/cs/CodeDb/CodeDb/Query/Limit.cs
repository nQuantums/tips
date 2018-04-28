using System;
using System.Collections.Generic;
using System.Text;
using CodeDb.Internal;

namespace CodeDb.Query {
	/// <summary>
	/// LIMIT句のノード
	/// </summary>
	public class Limit : ILimit {
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
		/// 制限値
		/// </summary>
		public object Value { get; private set; }
		#endregion

		#region コンストラクタ
		/// <summary>
		/// コンストラクタ、親ノードと列順序指定式を指定して初期化する
		/// </summary>
		/// <param name="parent">親ノード</param>
		/// <param name="value">制限値</param>
		public Limit(IQueryNode parent, object value) {
			this.Parent = parent;
			this.Value = value;
		}
		#endregion

		#region 公開メソッド
		/// <summary>
		/// SQL文を生成する
		/// </summary>
		/// <param name="context">生成先のコンテキスト</param>
		public void BuildSql(ElementCode context) {
			context.Add(SqlKeyword.Limit);
			context.Add(this.Value);
		}
		#endregion
	}
}
