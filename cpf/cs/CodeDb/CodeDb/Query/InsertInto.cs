using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using CodeDb.Internal;

namespace CodeDb.Query {
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
		public ISelect<TColumnsOrder> Select { get; private set; }

		ITableDef IInsertInto.Table => this.Table;
		ISelect IInsertInto.Select => this.Select;
		#endregion

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

		public void Values(ISelect<TColumnsOrder> select) {
			this.Select = select;
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

			var sel = this.Select;
			if (sel != null) {
				sel.BuildSql(context);
				return;
			}

			throw new ApplicationException();
		}
	}
}
