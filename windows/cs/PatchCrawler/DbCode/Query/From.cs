using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using DbCode.Internal;

namespace DbCode.Query {
	/// <summary>
	/// FROM句
	/// </summary>
	/// <typeparam name="TColumns">プロパティを列として扱う<see cref="TableDef{TColumns}"/>のTColumnsに該当するクラス</typeparam>
	public class From<TColumns> : IFrom<TColumns> {
		#region プロパティ
		/// <summary>
		/// 所有者
		/// </summary>
		public Sql Owner => this.Parent.Owner;

		/// <summary>
		/// 親ノード
		/// </summary>
		public IQueryNode Parent { get; private set; }

		/// <summary>
		/// 子ノード一覧
		/// </summary>
		public IEnumerable<IQueryNode> Children {
			get {
				if (this.JoinNodes != null) {
					foreach (var join in this.JoinNodes) {
						yield return join;
					}
				}

				if (this.WhereNode != null) {
					yield return this.WhereNode;
				}

				if (this.GroupByNode != null) {
					yield return this.GroupByNode;
				}

				if (this.OrderByNode != null) {
					yield return this.OrderByNode;
				}

				if (this.LimitNode != null) {
					yield return this.LimitNode;
				}
			}
		}

		/// <summary>
		/// FROMに直接指定された取得元の<see cref="ITable"/>
		/// </summary>
		public ITable Table { get; private set; }

		/// <summary>
		/// 列プロパティを持つオブジェクト
		/// </summary>
		public TColumns Columns { get; private set; }

		/// <summary>
		/// 列プロパティを持つオブジェクト
		/// </summary>
		public TColumns _ => this.Columns;

		/// <summary>
		/// INNER JOIN、LEFT JOIN、RIGHT JOIN句のリスト
		/// </summary>
		public List<IJoin> JoinNodes { get; private set; }
		IEnumerable<IJoin> IFrom.JoinNodes => this.JoinNodes;

		/// <summary>
		/// WHERE句の式
		/// </summary>
		public IWhere WhereNode  { get; private set; }

		/// <summary>
		/// GROUP BY句の列一覧
		/// </summary>
		public IGroupBy GroupByNode { get; private set; }

		/// <summary>
		/// ORDER BY句の列一覧
		/// </summary>
		public IOrderBy OrderByNode { get; private set; }

		/// <summary>
		/// LIMIT句のノード
		/// </summary>
		public ILimit LimitNode { get; private set; }

		/// <summary>
		/// SELECT句のノード
		/// </summary>
		public ISelect SelectNode { get; private set; }
		#endregion

		#region コンストラクタ
		/// <summary>
		/// コンストラクタ、親ノードと取得元のテーブル定義を指定して初期化する
		/// </summary>
		/// <param name="parent">親ノード</param>
		/// <param name="tableDef">テーブル定義</param>
		public From(IQueryNode parent, ITable<TColumns> tableDef) {
			this.Parent = parent;

			var clone = tableDef.AliasedClone();
			this.Table = clone;
			this.Columns = clone.Columns;

			parent.Owner.RegisterTable(clone);
		}

		/// <summary>
		/// コンストラクタ、親ノードと取得元のテーブル定義を指定して初期化する
		/// </summary>
		/// <param name="parent">親ノード</param>
		/// <param name="select">SELECTノード</param>
		public From(IQueryNode parent, ISelect<TColumns> select) {
			this.Parent = parent;

			QueryNodeHelper.SwitchParent(select, this);

			this.Table = select;
			this.Columns = select.Columns;

			parent.Owner.RegisterTable(select);
		}
		#endregion

		#region 公開メソッド
		/// <summary>
		/// 指定ノードを子とする、既存の親は<see cref="RemoveChild(IQueryNode)"/>で切り離す必要がある
		/// </summary>
		/// <param name="child">子ノード</param>
		public void AddChild(IQueryNode child) {
			IJoin join;
			IWhere where;
			IGroupBy groupby;
			IOrderBy orderby;
			ILimit limit;
			ISelect select;

			if ((join = child as IJoin) != null) {
				this.Join(join);
			} else if ((where = child as IWhere) != null) {
				this.Where(where);
			} else if ((groupby = child as IGroupBy) != null) {
				this.GroupBy(groupby);
			} else if ((orderby = child as IOrderBy) != null) {
				this.OrderBy(orderby);
			} else if ((limit = child as ILimit) != null) {
				this.Limit(limit);
			} else if ((select = child as ISelect) != null) {
				this.Select(select);
			} else {
				throw new ApplicationException();
			}
		}

		/// <summary>
		/// 指定の子ノードを取り除く
		/// </summary>
		/// <param name="child">子ノード</param>
		public void RemoveChild(IQueryNode child) {
			if (this.JoinNodes != null) {
				var join = child as IJoin;
				if (join != null) {
					this.JoinNodes.Remove(join);
				}
			}
			if (this.WhereNode == child) {
				this.WhereNode = null;
			}
			if (this.GroupByNode == child) {
				this.GroupByNode = null;
			}
			if (this.OrderByNode == child) {
				this.OrderByNode = null;
			}
			if (this.LimitNode == child) {
				this.LimitNode = null;
			}
			if (this.SelectNode == child) {
				this.SelectNode = null;
			}
		}

		/// <summary>
		/// 親ノードが変更された際に呼び出される
		/// </summary>
		/// <param name="parent">新しい親ノード</param>
		public void ChangeParent(IQueryNode parent) {
			this.Parent = parent;
		}

		/// <summary>
		/// JOIN句のノードを登録する
		/// </summary>
		/// <param name="join">JOIN句ノード</param>
		/// <returns>自分</returns>
		public From<TColumns> Join(IJoin join) {
			if (this.JoinNodes == null) {
				this.JoinNodes = new List<IJoin>();
			}
			QueryNodeHelper.SwitchParent(join, this);
			this.JoinNodes.Add(join);
			return this;
		}

		/// <summary>
		/// WHERE句のノードを登録する
		/// </summary>
		/// <param name="where">WHERE句ノード</param>
		/// <returns>自分</returns>
		public From<TColumns> Where(IWhere where) {
			if (this.WhereNode != null) {
				throw new ApplicationException();
			}
			QueryNodeHelper.SwitchParent(where, this);
			this.WhereNode = where;
			return this;
		}

		/// <summary>
		/// GROUP BY句のノードを登録する
		/// </summary>
		/// <param name="groupBy">GROUP BY句ノード</param>
		/// <returns>自分</returns>
		public From<TColumns> GroupBy(IGroupBy groupBy) {
			if (this.GroupByNode != null) {
				throw new ApplicationException();
			}
			QueryNodeHelper.SwitchParent(groupBy, this);
			this.GroupByNode = groupBy;
			return this;
		}

		/// <summary>
		/// ORDER BY句のノードを登録する
		/// </summary>
		/// <param name="orderBy">ORDER BY句ノード</param>
		/// <returns>自分</returns>
		public From<TColumns> OrderBy(IOrderBy orderBy) {
			if (this.OrderByNode != null) {
				throw new ApplicationException();
			}
			QueryNodeHelper.SwitchParent(orderBy, this);
			this.OrderByNode = orderBy;
			return this;
		}

		/// <summary>
		/// LIMIT句のノードを登録する
		/// </summary>
		/// <param name="limit">LIMIT句ノード</param>
		/// <returns>自分</returns>
		public From<TColumns> Limit(ILimit limit) {
			if (this.LimitNode != null) {
				throw new ApplicationException();
			}
			QueryNodeHelper.SwitchParent(limit, this);
			this.LimitNode = limit;
			return this;
		}

		/// <summary>
		/// SELECT句のノードを登録する
		/// </summary>
		/// <param name="select">SELECT句ノード</param>
		/// <returns>自分</returns>
		public From<TColumns> Select(ISelect select) {
			if (this.SelectNode != null) {
				throw new ApplicationException();
			}
			QueryNodeHelper.SwitchParent(select, this);
			this.SelectNode = select;
			return this;
		}

		/// <summary>
		/// 内部結合の<see cref="Join{TColumns}"/>を生成し登録する
		/// </summary>
		/// <typeparam name="TColumns1">結合するテーブルの<see cref="DbCode.ITable{TColumns}.Columns"/></typeparam>
		/// <param name="table">結合するテーブル</param>
		/// <param name="on">結合式</param>
		/// <returns>内部結合の<see cref="Join{TColumns}"></see></returns>
		public IJoin<TColumns1> InnerJoin<TColumns1>(ITable<TColumns1> table, Expression<Func<TColumns1, bool>> on) => JoinByType(JoinType.Inner, table, on);

		/// <summary>
		/// 左外部結合の<see cref="Join{TColumns}"/>を生成し登録する
		/// </summary>
		/// <typeparam name="TColumns1">結合するテーブルの<see cref="DbCode.ITable{TColumns}.Columns"/></typeparam>
		/// <param name="table">結合するテーブル</param>
		/// <param name="on">結合式</param>
		/// <returns>左外部結合の<see cref="Join{TColumns}"/></returns>
		public IJoin<TColumns1> LeftJoin<TColumns1>(ITable<TColumns1> table, Expression<Func<TColumns1, bool>> on) => JoinByType(JoinType.Left, table, on);

		/// <summary>
		/// 右外部結合の<see cref="Join{TColumns}"/>を生成し登録する
		/// </summary>
		/// <typeparam name="TColumns1">結合するテーブルの<see cref="DbCode.ITable{TColumns}.Columns"/></typeparam>
		/// <param name="table">結合するテーブル</param>
		/// <param name="on">結合式</param>
		/// <returns>右外部結合の<see cref="Join{TColumns}"/></returns>
		public IJoin<TColumns1> RightJoin<TColumns1>(ITable<TColumns1> table, Expression<Func<TColumns1, bool>> on) => JoinByType(JoinType.Right, table, on);

		/// <summary>
		/// WHERE句の式を登録する
		/// </summary>
		/// <param name="expression">式</param>
		/// <returns>自分</returns>
		[SqlMethod]
		public From<TColumns> Where(Expression<Func<TColumns, bool>> expression) {
			var body = ParameterReplacer.Replace(expression.Body, new Dictionary<Expression, object> { { expression.Parameters[0], this.Columns } });
			return this.Where(new Where(this, body));
		}

		/// <summary>
		/// GROUP BYの列を登録する
		/// </summary>
		/// <typeparam name="TColumns1">列を指定する為の匿名クラス、メンバに列プロパティを指定して初期化する</typeparam>
		/// <param name="columnsExpression">プロパティが列指定として扱われる匿名クラスを生成する式</param>
		/// <returns>自分</returns>
		public From<TColumns> GroupBy<TColumns1>(Expression<Func<TColumns1>> columnsExpression) {
			return this.GroupBy(new GroupBy<TColumns1>(this, columnsExpression));
		}

		/// <summary>
		/// ORDER BYの列を登録する
		/// </summary>
		/// <typeparam name="TColumns1">列を指定する為の匿名クラス、メンバに列プロパティを指定して初期化する</typeparam>
		/// <param name="columnsExpression">プロパティが列指定として扱われる匿名クラスを生成する式</param>
		/// <returns>自分</returns>
		public From<TColumns> OrderBy<TColumns1>(Expression<Func<TColumns1>> columnsExpression) {
			return this.OrderBy(new OrderBy<TColumns1>(this, columnsExpression));
		}

		/// <summary>
		/// LIMITの値を登録する
		/// </summary>
		/// <param name="count">制限値</param>
		/// <returns>自分</returns>
		public From<TColumns> Limit(object count) {
			return this.Limit(new Limit(this, count));
		}

		/// <summary>
		/// 全ての列を選択する列選択部を生成する
		/// </summary>
		/// <returns>SELECT句ノード</returns>
		[SqlMethod]
		public SelectFrom<TColumns> Select() {
			var node = new SelectFrom<TColumns>(this, null);
			this.Select(node);
			return node;
		}

		/// <summary>
		/// 列選択部を生成する
		/// </summary>
		/// <typeparam name="TSelectedColumns">列をプロパティとして持つクラス</typeparam>
		/// <param name="columnsExpression">プロパティが列指定として扱われるクラスを生成する t => new { t.A, t1.B, t3.C } の様な式、<c>t</c>はFROM元のテーブルの列</param>
		/// <returns>SELECT句ノード</returns>
		public SelectFrom<TSelectedColumns> Select<TSelectedColumns>(Expression<Func<TColumns, TSelectedColumns>> columnsExpression) {
			var expr = ParameterReplacer.Replace(columnsExpression.Body, new Dictionary<Expression, object> { { columnsExpression.Parameters[0], this.Columns } });
			var node = new SelectFrom<TSelectedColumns>(this, expr);
			this.Select(node);
			return node;
		}

		/// <summary>
		/// SQL文を生成する
		/// </summary>
		/// <param name="context">生成先のコンテキスト</param>
		public void ToElementCode(ElementCode context) {
			if (this.SelectNode == null) {
				throw new ApplicationException();
			}

			this.SelectNode.ToElementCode(context);

			if (this.Table != null) {
				context.Add(SqlKeyword.From);
				this.Table.ToElementCode(context);
				context.Add(this.Table);
			}

			if (this.JoinNodes != null) {
				foreach (var join in this.JoinNodes) {
					join.ToElementCode(context);
				}
			}

			if (this.WhereNode != null) {
				this.WhereNode.ToElementCode(context);
			}

			if (this.GroupByNode != null) {
				this.GroupByNode.ToElementCode(context);
			}

			if (this.OrderByNode != null) {
				this.OrderByNode.ToElementCode(context);
			}

			if (this.LimitNode != null) {
				this.LimitNode.ToElementCode(context);
			}
		}
		#endregion

		#region 非公開メソッド
		/// <summary>
		/// 指定された結合種類の<see cref="Join{TColumns}"/>を生成し登録する
		/// </summary>
		/// <typeparam name="TColumns1">結合するテーブルの<see cref="ITable{TColumns}.Columns"/></typeparam>
		/// <param name="joinType">結合種類</param>
		/// <param name="table">結合するテーブル</param>
		/// <param name="on">結合式</param>
		/// <returns><see cref="Join{TColumns}"/></returns>
		IJoin<TColumns1> JoinByType<TColumns1>(JoinType joinType, ITable<TColumns1> table, Expression<Func<TColumns1, bool>> on) {
			var join = new Join<TColumns1>(this, joinType, table, on);
			this.Join(join);
			return join;
		}
		#endregion
	}
}
