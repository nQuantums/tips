using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using CodeDb.Internal;

namespace CodeDb.Query {
	/// <summary>
	/// INSERT INTO句
	/// </summary>
	/// <typeparam name="TColumns">プロパティを列として扱うクラス</typeparam>
	/// <typeparam name="TColumnsOrder">列の並びを指定する t => new { t.A, t.B } の様な式で生成される匿名クラス</typeparam>
	public class InsertInto<TColumns, TColumnsOrder> : IInsertInto<TColumns> {
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
		public IEnumerable<IQueryNode> Children {
			get {
				if (this.ValueNode != null) {
					yield return this.ValueNode;
				}
			}
		}

		/// <summary>
		/// 挿入先のテーブル
		/// </summary>
		public TableDef<TColumns> Table { get; private set; }
		ITableDef IInsertInto.Table => this.Table;

		/// <summary>
		/// 挿入列の指定
		/// </summary>
		public ColumnMap ColumnMap { get; private set; }

		/// <summary>
		/// 挿入する値
		/// </summary>
		public ISelect<TColumnsOrder> ValueNode { get; private set; }
		ISelect IInsertInto.ValueNode => this.ValueNode;
		#endregion

		#region コンストラクタ
		/// <summary>
		/// コンストラクタ、親ノード、挿入先テーブルと列の並びを指定して初期化する
		/// </summary>
		/// <param name="parent">親ノード</param>
		/// <param name="table">挿入先テーブル</param>
		/// <param name="columnsExpression">列の並びを指定する t => new { t.A, t.B } の様な式</param>
		public InsertInto(IQueryNode parent, TableDef<TColumns> table, Expression<Func<TColumns, TColumnsOrder>> columnsExpression) {
			this.Parent = parent;

			// new 演算子で匿名クラスを生成するもの以外はエラーとする
			var body = columnsExpression.Body;
			if (body.NodeType != ExpressionType.New) {
				throw new ApplicationException();
			}
			if (!TypeSystem.IsAnonymousType(body.Type)) {
				throw new ApplicationException();
			}

			// 登録
			var owner = this.Owner;
			owner.Register(table);

			// 匿名クラスのプロパティを取得、さらに各プロパティ初期化用の式を取得する
			var newexpr = body as NewExpression;
			var args = newexpr.Arguments;
			var map = new Dictionary<Expression, object> { { columnsExpression.Parameters[0], table.Columns } };
			var availableColumns = owner.AllColumns;
			var columnsOrder = new ColumnMap();

			for (int i = 0; i < args.Count; i++) {
				var context = new ElementCode(ParameterReplacer.Replace(args[i], map), availableColumns);
				if (context.Items.Count != 1) {
					throw new ApplicationException();
				}
				var column = context.Items[0] as Column;
				if (!availableColumns.Contains(column)) {
					throw new ApplicationException();
				}

				columnsOrder.Add(column);
			}

			this.Table = table;
			this.ColumnMap = columnsOrder;
		}
		#endregion

		#region 公開メソッド

		//public void Values(ISelect<TColumnsOrder> select) {
		//	this.Values = select;
		//}

		/// <summary>
		/// 列選択部を生成する
		/// </summary>
		/// <typeparam name="TColumns1">列をプロパティとして持つクラス</typeparam>
		/// <param name="columnsExpression">プロパティが列指定として扱われるクラスを生成する () => new { t1.A, t1.B } の様な式</param>
		/// <returns>SELECT句</returns>
		public Select<TColumnsOrder> Select(Expression<Func<TColumnsOrder>> columnsExpression) {
			var select = new Select<TColumnsOrder>(this, columnsExpression);
			this.ValueNode = select;
			return select;
		}

		/// <summary>
		/// 列選択部を生成する
		/// </summary>
		/// <typeparam name="TColumns1">列をプロパティとして持つクラス</typeparam>
		/// <param name="columnsExpression">プロパティが列指定として扱われるクラスを生成する () => new { t1.A, t1.B } の様な式</param>
		/// <param name="selectWhereExpression">既存レコードに一致判定の t => t.Column1 == 3 の様な式</param>
		/// <returns>SELECT句</returns>
		public Select<TColumnsOrder> SelectIfNotExists(Expression<Func<TColumnsOrder>> columnsExpression, Expression<Func<TColumns, bool>> selectWhereExpression) {
			var select = new Select<TColumnsOrder>(this, columnsExpression);
			this.ValueNode = select;
			var where = select.Where();
			where.NotExistsSelect(this.Table.Columns, selectWhereExpression);
			return select;
		}

		/// <summary>
		/// SQL文を生成する
		/// </summary>
		/// <param name="context">生成先のコンテキスト</param>
		public void BuildSql(ElementCode context) {
			var environment = this.Table.Environment;
			context.Add(SqlKeyword.InsertInto);
			context.Concat(this.Table.Name);
			context.AddColumnDefs(this.ColumnMap);

			var valueNode = this.ValueNode;
			if (valueNode != null) {
				valueNode.BuildSql(context);
				return;
			}

			throw new ApplicationException();
		}
		#endregion
	}
}
