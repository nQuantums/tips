using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using CodeDb;
using CodeDb.Internal;

namespace CodeDb.Query {
	/// <summary>
	/// WHERE句のノード
	/// </summary>
	public class Where : IWhere {
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
		/// 式
		/// </summary>
		public ElementCode Expression { get; private set; }
		#endregion

		#region コンストラクタ
		/// <summary>
		/// コンストラクタ、親ノードを指定して初期化する
		/// </summary>
		/// <param name="parent">親ノード</param>
		public Where(IQueryNode parent) {
			this.Parent = parent;
		}

		/// <summary>
		/// コンストラクタ、親ノードと式を指定して初期化する
		/// </summary>
		/// <param name="parent">親ノード</param>
		/// <param name="expression">式</param>
		public Where(IQueryNode parent, Expression<Func<bool>> expression) {
			this.Parent = parent;
			this.Expression = new ElementCode(expression, this.Owner.AllColumns);
		}

		/// <summary>
		/// コンストラクタ、親ノードと式を指定して初期化する
		/// </summary>
		/// <param name="parent">親ノード</param>
		/// <param name="expression">式</param>
		public Where(IQueryNode parent, ElementCode expression) {
			this.Parent = parent;
			this.Expression = expression;
		}
		#endregion

		#region 公開メソッド
		public void NotExistsSelect<TColumns>(TColumns columns, Expression<Func<TColumns, bool>> selectWhereExpression) {
			var expression = new ElementCode(selectWhereExpression, this.Owner.AllColumns, columns);
			var table = expression.FindTables().FirstOrDefault();
			if (table == null) {
				throw new ApplicationException();
			}

			var whereExpression = new ElementCode();
			whereExpression.Add(SqlKeyword.NotExists);
			whereExpression.BeginParenthesize();
			whereExpression.Add(SqlKeyword.Select, SqlKeyword.Asterisk, SqlKeyword.From);
			whereExpression.Add(table);
			whereExpression.Add(SqlKeyword.Where);
			whereExpression.Add(expression);
			whereExpression.EndParenthesize();
			this.Expression = whereExpression;
		}

		/// <summary>
		/// SQL文を生成する
		/// </summary>
		/// <param name="context">生成先のコンテキスト</param>
		public void ToElementCode(ElementCode context) {
			context.Add(SqlKeyword.Where);
			context.Add(this.Expression);
		}
		#endregion
	}
}
