using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using CodeDb.Internal;

namespace CodeDb.Query {
	/// <summary>
	/// ORDER BY句のノード
	/// </summary>
	/// <typeparam name="TColumns">列を指定する為の匿名クラス、メンバに列プロパティを指定して初期化する</typeparam>
	public class OrderBy<TColumns> : IOrderBy {
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
		/// GROUP BY句の列一覧
		/// </summary>
		public IEnumerable<Column> Columns { get; private set; }
		#endregion

		#region コンストラクタ
		/// <summary>
		/// コンストラクタ、親ノードと列順序指定式を指定して初期化する
		/// </summary>
		/// <param name="parent">親ノード</param>
		/// <param name="columnsExpression">列順序指定式</param>
		public OrderBy(IQueryNode parent, Expression<Func<TColumns>> columnsExpression) {
			this.Parent = parent;

			// new 演算子で匿名クラスを生成するもの以外はエラーとする
			var body = columnsExpression.Body;
			if (body.NodeType != ExpressionType.New) {
				throw new ApplicationException();
			}
			if (!TypeSystem.IsAnonymousType(body.Type)) {
				throw new ApplicationException();
			}

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

			this.Columns = columns;
		}
		#endregion

		#region 公開メソッド
		/// <summary>
		/// SQL文を生成する
		/// </summary>
		/// <param name="context">生成先のコンテキスト</param>
		public void BuildSql(ElementCode context) {
			context.Add(SqlKeyword.OrderBy);
			context.AddColumns(this.Columns);
		}
		#endregion
	}
}
