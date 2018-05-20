using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using DbCode.Internal;

namespace DbCode.Query {
	/// <summary>
	/// INNER JOIN、LEFT JOIN、RIGHT JOIN句の基本機能を提供し、<see cref="Columns"/>のプロパティにより列へのアクセスも提供する
	/// </summary>
	/// <typeparam name="TColumns">プロパティを列として扱う<see cref="TableDef{TColumns}"/>のTColumnsに該当するクラス</typeparam>
	public class Join<TColumns> : IJoin<TColumns> {
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
		/// 結合種類
		/// </summary>
		public JoinType JoinType { get; private set; }

		/// <summary>
		/// 結合するテーブル
		/// </summary>
		public ITable<TColumns> Table { get; private set; }
		ITable IJoin.Table => this.Table;

		/// <summary>
		/// 結合式
		/// </summary>
		public ElementCode On { get; private set; }

		/// <summary>
		/// 列をプロパティとして持つオブジェクト
		/// </summary>
		public TColumns Columns { get; private set; }

		/// <summary>
		/// 列をプロパティとして持つオブジェクト
		/// </summary>
		public TColumns _ => this.Columns;
		#endregion

		#region コンストラクタ
		/// <summary>
		/// コンストラクタ、親ノードと結合種類、テーブル、結合式を指定して初期化する
		/// </summary>
		/// <param name="parent">親ノード</param>
		/// <param name="joinType">結合種類</param>
		/// <param name="table">結合するテーブル</param>
		/// <param name="on">結合式</param>
		public Join(IQueryNode parent, JoinType joinType, ITable<TColumns> table, Expression<Func<TColumns, bool>> on) {
			var clone = table.AliasedClone();

			this.Parent = parent;
			this.JoinType = joinType;
			this.Table = clone;
			this.Columns = clone.Columns;

			this.Owner.RegisterTable(clone);

			this.On = new ElementCode(
				ParameterReplacer.Replace(
					on.Body,
					new Dictionary<Expression, object> { { on.Parameters[0], clone.Columns } }
				),
				this.Owner.AllColumns
			);
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
			switch (this.JoinType) {
			case JoinType.Inner:
				context.Add(SqlKeyword.InnerJoin);
				break;
			case JoinType.Left:
				context.Add(SqlKeyword.LeftJoin);
				break;
			case JoinType.Right:
				context.Add(SqlKeyword.RightJoin);
				break;
			}
			this.Table.ToElementCode(context);
			context.Add(this.Table);
			context.Add(SqlKeyword.On);
			context.Add(this.On);
		}
		#endregion
	}
}
