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
		#endregion

		#region コンストラクタ
		/// <summary>
		/// コンストラクタ、親ノードと取得元のテーブル定義を指定して初期化する
		/// </summary>
		/// <param name="parent">親ノード</param>
		/// <param name="table">テーブル</param>
		[SqlMethod]
		public From(IQueryNode parent, ITable<TColumns> table) {
			var select = table as ISelect<TColumns>;
			if (select is null) {
				table = table.AliasedClone();
			} else {
				QueryNodeHelper.SwitchParent(select, this);
			}

			this.Parent = parent;
			this.Table = table;
			this.Columns = table.Columns;

			this.Owner.RegisterTable(table);
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
		[SqlMethod]
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
		[SqlMethod]
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
		[SqlMethod]
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
		[SqlMethod]
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
		[SqlMethod]
		public From<TColumns> Limit(ILimit limit) {
			if (this.LimitNode != null) {
				throw new ApplicationException();
			}
			QueryNodeHelper.SwitchParent(limit, this);
			this.LimitNode = limit;
			return this;
		}

		/// <summary>
		/// 指定された結合種類の<see cref="Query.Join{TColumns}"/>を生成し登録する
		/// </summary>
		/// <typeparam name="TColumnsOfJoinTable">結合するテーブルの<see cref="ITable{TColumns}.Columns"/></typeparam>
		/// <param name="joinType">結合種類</param>
		/// <param name="table">結合するテーブル</param>
		/// <param name="on">結合式</param>
		/// <returns><see cref="Query.Join{TColumns}"/></returns>
		[SqlMethod]
		public IJoin<TColumnsOfJoinTable> Join<TColumnsOfJoinTable>(JoinType joinType, ITable<TColumnsOfJoinTable> table, Expression<Func<TColumnsOfJoinTable, bool>> on) {
			var join = new Join<TColumnsOfJoinTable>(this, joinType, table, on);
			this.Join(join);
			return join;
		}

		/// <summary>
		/// 内部結合の<see cref="Query.Join{TColumns}"/>を生成し登録する
		/// </summary>
		/// <typeparam name="TColumnsOfJoinTable">結合するテーブルの<see cref="DbCode.ITable{TColumns}.Columns"/></typeparam>
		/// <param name="table">結合するテーブル</param>
		/// <param name="on">結合式</param>
		/// <returns>内部結合の<see cref="Query.Join{TColumns}"></see></returns>
		[SqlMethod]
		public IJoin<TColumnsOfJoinTable> InnerJoin<TColumnsOfJoinTable>(ITable<TColumnsOfJoinTable> table, Expression<Func<TColumnsOfJoinTable, bool>> on) => Join(JoinType.Inner, table, on);

		/// <summary>
		/// 左外部結合の<see cref="Query.Join{TColumns}"/>を生成し登録する
		/// </summary>
		/// <typeparam name="TColumnsOfJoinTable">結合するテーブルの<see cref="DbCode.ITable{TColumns}.Columns"/></typeparam>
		/// <param name="table">結合するテーブル</param>
		/// <param name="on">結合式</param>
		/// <returns>左外部結合の<see cref="Query.Join{TColumns}"/></returns>
		[SqlMethod]
		public IJoin<TColumnsOfJoinTable> LeftJoin<TColumnsOfJoinTable>(ITable<TColumnsOfJoinTable> table, Expression<Func<TColumnsOfJoinTable, bool>> on) => Join(JoinType.Left, table, on);

		/// <summary>
		/// 右外部結合の<see cref="Query.Join{TColumns}"/>を生成し登録する
		/// </summary>
		/// <typeparam name="TColumnsOfJoinTable">結合するテーブルの<see cref="DbCode.ITable{TColumns}.Columns"/></typeparam>
		/// <param name="table">結合するテーブル</param>
		/// <param name="on">結合式</param>
		/// <returns>右外部結合の<see cref="Query.Join{TColumns}"/></returns>
		[SqlMethod]
		public IJoin<TColumnsOfJoinTable> RightJoin<TColumnsOfJoinTable>(ITable<TColumnsOfJoinTable> table, Expression<Func<TColumnsOfJoinTable, bool>> on) => Join(JoinType.Right, table, on);

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
		/// <typeparam name="TGroupByColumns">列を指定する為の匿名クラス、メンバに列プロパティを指定して初期化する</typeparam>
		/// <param name="columnsExpression">t => new { t.A, t.B } の様な式を指定する、プロパティが列指定として扱われる</param>
		/// <returns>自分</returns>
		[SqlMethod]
		public From<TColumns> GroupBy<TGroupByColumns>(Expression<Func<TColumns, TGroupByColumns>> columnsExpression) {
			// new 演算子で匿名クラスを生成するもの以外はエラーとする
			var body = columnsExpression.Body;
			if (body.NodeType != ExpressionType.New) {
				throw new ApplicationException();
			}
			if (!TypeSystem.IsAnonymousType(body.Type)) {
				throw new ApplicationException();
			}

			body = ParameterReplacer.Replace(body, new Dictionary<Expression, object> { { columnsExpression.Parameters[0], this.Columns } });

			// 匿名クラスのプロパティをグルーピング用の列として取得する
			var newexpr = body as NewExpression;
			var args = newexpr.Arguments;
			var columns = new Column[args.Count];
			for (int i = 0; i < columns.Length; i++) {
				var context = new ElementCode(args[i], this.Owner.AllColumns);
				if (context.Items.Count != 1) {
					throw new ApplicationException();
				}
				var column = context.Items[0] as Column;
				if (column == null) {
					throw new ApplicationException();
				}

				columns[i] = column;
			}

			return this.GroupBy(new GroupBy<TGroupByColumns>(this, columns));
		}

		/// <summary>
		/// ORDER BYの列を登録する
		/// </summary>
		/// <typeparam name="TOrderByColumns">列を指定する為の匿名クラス、メンバに列プロパティを指定して初期化する</typeparam>
		/// <param name="columnsExpression">t => new { t.A, t.B } の様な式を指定する、プロパティが列指定として扱われる</param>
		/// <returns>自分</returns>
		[SqlMethod]
		public From<TColumns> OrderBy<TOrderByColumns>(Expression<Func<TColumns, TOrderByColumns>> columnsExpression) {
			// new 演算子で匿名クラスを生成するもの以外はエラーとする
			var body = columnsExpression.Body;
			if (body.NodeType != ExpressionType.New) {
				throw new ApplicationException();
			}
			if (!TypeSystem.IsAnonymousType(body.Type)) {
				throw new ApplicationException();
			}

			body = ParameterReplacer.Replace(body, new Dictionary<Expression, object> { { columnsExpression.Parameters[0], this.Columns } });

			// 匿名クラスのプロパティをグルーピング用の列として取得する
			var newexpr = body as NewExpression;
			var args = newexpr.Arguments;
			var columns = new Column[args.Count];
			for (int i = 0; i < columns.Length; i++) {
				var context = new ElementCode(args[i], this.Owner.AllColumns);
				if (context.Items.Count != 1) {
					throw new ApplicationException();
				}
				var column = context.Items[0] as Column;
				if (column == null) {
					throw new ApplicationException();
				}

				columns[i] = column;
			}

			return this.OrderBy(new OrderBy<TOrderByColumns>(this, columns));
		}

		/// <summary>
		/// LIMITの値を登録する
		/// </summary>
		/// <param name="count">制限値</param>
		/// <returns>自分</returns>
		[SqlMethod]
		public From<TColumns> Limit(object count) {
			return this.Limit(new Limit(this, count));
		}

		/// <summary>
		/// 全ての列を選択する列選択部を生成する
		/// </summary>
		/// <returns>SELECT句ノード</returns>
		[SqlMethod]
		public SelectFrom<TColumns> Select() {
			return new SelectFrom<TColumns>(this, null);
		}

		/// <summary>
		/// 列選択部を生成する
		/// </summary>
		/// <typeparam name="TSelectedColumns">列をプロパティとして持つクラス</typeparam>
		/// <param name="columnsExpression">プロパティが列指定として扱われるクラスを生成する t => new { t.A, t1.B, t3.C } の様な式、<c>t</c>はFROM元のテーブルの列</param>
		/// <returns>SELECT句ノード</returns>
		[SqlMethod]
		public SelectFrom<TSelectedColumns> Select<TSelectedColumns>(Expression<Func<TColumns, TSelectedColumns>> columnsExpression) {
			var expr = ParameterReplacer.Replace(columnsExpression.Body, new Dictionary<Expression, object> { { columnsExpression.Parameters[0], this.Columns } });
			return new SelectFrom<TSelectedColumns>(this, expr);
		}

		/// <summary>
		/// SQL文を生成する
		/// </summary>
		/// <param name="context">生成先のコンテキスト</param>
		public void ToElementCode(ElementCode context) {
			if (this.Table != null) {
				context.Add(SqlKeyword.From);
				context.Push();
				this.Table.ToElementCode(context);
				context.Pop();
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
