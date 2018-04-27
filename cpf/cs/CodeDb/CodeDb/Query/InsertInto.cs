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
		/// 挿入先のテーブル
		/// </summary>
		public TableDef<TColumns> Table { get; private set; }

		/// <summary>
		/// 挿入列の指定
		/// </summary>
		public ColumnMap ColumnMap { get; private set; }

		/// <summary>
		/// 挿入する値
		/// </summary>
		public ISelect<TColumnsOrder> Values { get; private set; }

		ITableDef IInsertInto.Table => this.Table;
		ISelect IInsertInto.Values => this.Values;
		#endregion

		/// <summary>
		/// コンストラクタ、挿入先テーブルと列の並びを指定して初期化する
		/// </summary>
		/// <param name="table">挿入先テーブル</param>
		/// <param name="columnsExpression">列の並びを指定する t => new { t.A, t.B } の様な式</param>
		public InsertInto(TableDef<TColumns> table, Expression<Func<TColumns, TColumnsOrder>> columnsExpression) {
			// new 演算子で匿名クラスを生成するもの以外はエラーとする
			var body = columnsExpression.Body;
			if (body.NodeType != ExpressionType.New) {
				throw new ApplicationException();
			}
			if (!TypeSystem.IsAnonymousType(body.Type)) {
				throw new ApplicationException();
			}

			// 匿名クラスのプロパティを取得、さらに各プロパティ初期化用の式を取得する
			var newexpr = body as NewExpression;
			var args = newexpr.Arguments;
			var map = new Dictionary<Expression, object> { { columnsExpression.Parameters[0], table.Columns } };
			var availableColumns = table.ColumnMap;
			var columnsOrder = new ColumnMap();

			for (int i = 0; i < args.Count; i++) {
				var context = new ElementCode();
				context.Add(ParameterReplacer.Replace(args[i], map), availableColumns);
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
			// new 演算子でクラスを生成するもの以外はエラーとする
			var body = columnsExpression.Body;
			if (body.NodeType != ExpressionType.New) {
				throw new ApplicationException();
			}

			// クラスのプロパティ数とコンストラクタ引数の数が異なるならエラーとする
			var newexpr = body as NewExpression;
			var args = newexpr.Arguments;
			var properties = typeof(TColumnsOrder).GetProperties();
			if (args.Count != properties.Length) {
				throw new ApplicationException();
			}

			// プロパティと列定義を結びつけその生成元としてコンストラクタ引数を指定する
			var environment = this.Table.Environment;
			var select = new Select<TColumnsOrder>(environment);
			for (int i = 0; i < properties.Length; i++) {
				var pi = properties[i];
				if (pi.PropertyType != args[i].Type) {
					throw new ApplicationException();
				}
				var context = new ElementCode();
				context.Add(args[i], null);
				select.BindColumn(pi.Name, "c" + i, environment.CreateDbTypeFromType(pi.PropertyType), 0, context);
			}

			this.Values = select;

			return select;
		}

		public void BuildSql(ElementCode context) {
			var environment = this.Table.Environment;
			context.Add(SqlKeyword.InsertInto);
			context.Concat(this.Table.Name);
			context.BeginParenthesize();
			var columns = this.ColumnMap;
			for (int i = 0; i < columns.Count; i++) {
				if (i != 0) {
					context.AddComma();
				}
				context.Concat(environment.Quote(columns[i].Name));
			}
			context.EndParenthesize();

			var sel = this.Values;
			if (sel != null) {
				sel.BuildSql(context);
				return;
			}

			throw new ApplicationException();
		}
	}
}
