﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using CodeDb.Internal;

namespace CodeDb.Query {
	/// <summary>
	/// FROM句
	/// </summary>
	/// <typeparam name="TColumns">プロパティを列として扱うクラス</typeparam>
	public class From<TColumns> : IFrom<TColumns> {
		#region プロパティ
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
		/// SELECT句までの間に使用された全ての列
		/// </summary>
		public ColumnMap SourceColumnMap { get; private set; } = new ColumnMap();

		/// <summary>
		/// SELECT句までの間に使用された全ての<see cref="ITable"/>
		/// </summary>
		public HashSet<ITable> SourceTables { get; private set; } = new HashSet<ITable>();

		/// <summary>
		/// INNER JOIN、LEFT JOIN、RIGHT JOIN句のリスト
		/// </summary>
		public List<IJoin> Joins { get; private set; }

		/// <summary>
		/// WHERE句の式
		/// </summary>
		public ElementCode WhereExpression { get; private set; }

		/// <summary>
		/// GROUP BY句の列一覧
		/// </summary>
		public IEnumerable<Column> GroupByColumns { get; private set; }

		/// <summary>
		/// ORDER BY句の列一覧
		/// </summary>
		public IEnumerable<Column> OrderByColumns { get; private set; }

		/// <summary>
		/// LIMIT句の値
		/// </summary>
		public object LimitValue { get; private set; }
		#endregion

		#region コンストラクタ
		/// <summary>
		/// コンストラクタ、取得元のテーブル定義を指定して初期化する
		/// </summary>
		/// <param name="table">テーブル定義</param>
		public From(TableDef<TColumns> table) {
			var clone = table.AliasedClone();
			this.Table = clone;
			this.Columns = clone.Columns;

			this.SourceColumnMap.Include(clone.ColumnMap);
		}

		/// <summary>
		/// コンストラクタ、取得元のSELECT句を指定して初期化する
		/// </summary>
		/// <param name="select">SELECT句</param>
		public From(ISelect<TColumns> select) {
			var clone = select.AliasedClone();
			this.Table = clone;
			this.Columns = clone.Columns;

			this.SourceColumnMap.Include(clone.ColumnMap);
		}
		#endregion

		#region 公開メソッド
		/// <summary>
		/// 内部結合の<see cref="Join{TColumns}"/>を生成し登録する
		/// </summary>
		/// <typeparam name="TColumns1">結合するテーブルの<see cref="ITable{TColumns}.Columnsの型"/></typeparam>
		/// <param name="table">結合するテーブル</param>
		/// <param name="on">結合式</param>
		/// <returns>内部結合の<see cref="Join{TColumns}</returns>
		public IJoin<TColumns1> InnerJoin<TColumns1>(ITable<TColumns1> table, Expression<Func<TColumns1, bool>> on) => JoinByType(JoinType.Inner, table, on);

		/// <summary>
		/// 左外部結合の<see cref="Join{TColumns}"/>を生成し登録する
		/// </summary>
		/// <typeparam name="TColumns1">結合するテーブルの<see cref="ITable{TColumns}.Columnsの型"/></typeparam>
		/// <param name="table">結合するテーブル</param>
		/// <param name="on">結合式</param>
		/// <returns>左外部結合の<see cref="Join{TColumns}</returns>
		public IJoin<TColumns1> LeftJoin<TColumns1>(ITable<TColumns1> table, Expression<Func<TColumns1, bool>> on) => JoinByType(JoinType.Left, table, on);

		/// <summary>
		/// 右外部結合の<see cref="Join{TColumns}"/>を生成し登録する
		/// </summary>
		/// <typeparam name="TColumns1">結合するテーブルの<see cref="ITable{TColumns}.Columnsの型"/></typeparam>
		/// <param name="table">結合するテーブル</param>
		/// <param name="on">結合式</param>
		/// <returns>右外部結合の<see cref="Join{TColumns}</returns>
		public IJoin<TColumns1> RightJoin<TColumns1>(ITable<TColumns1> table, Expression<Func<TColumns1, bool>> on) => JoinByType(JoinType.Right, table, on);

		/// <summary>
		/// WHERE句の式を登録する
		/// </summary>
		/// <param name="expression">式</param>
		public void Where(Expression<Func<bool>> expression) {
			var context = new ElementCode();
			context.Add(expression, this.SourceColumnMap);
			this.WhereExpression = context;
		}

		/// <summary>
		/// GROUP BYの列を登録する
		/// </summary>
		/// <typeparam name="TColumns1">列を指定する為の匿名クラス、メンバに列プロパティを指定して初期化する</typeparam>
		/// <param name="columnsExpression">プロパティが列指定として扱われる匿名クラスを生成する式</param>
		public void GroupBy<TColumns1>(Expression<Func<TColumns1>> columnsExpression) {
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
				var context = new ElementCode();
				context.Add(args[i], this.SourceColumnMap);
				if (context.Items.Count != 1) {
					throw new ApplicationException();
				}
				var column = context.Items[0] as Column;
				if (column == null) {
					throw new ApplicationException();
				}

				columns[i] = column;
			}

			this.GroupByColumns = columns;
		}

		/// <summary>
		/// ORDER BYの列を登録する
		/// </summary>
		/// <typeparam name="TColumns1">列を指定する為の匿名クラス、メンバに列プロパティを指定して初期化する</typeparam>
		/// <param name="columnsExpression">プロパティが列指定として扱われる匿名クラスを生成する式</param>
		public void OrderBy<TColumns1>(Expression<Func<TColumns1>> columnsExpression) {
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
				var context = new ElementCode();
				context.Add(args[i], this.SourceColumnMap);
				if (context.Items.Count != 1) {
					throw new ApplicationException();
				}
				var column = context.Items[0] as Column;
				if (column == null) {
					throw new ApplicationException();
				}

				columns[i] = column;
			}

			this.OrderByColumns = columns;
		}

		/// <summary>
		/// LIMITの値を登録する
		/// </summary>
		/// <param name="count">制限値</param>
		public void Limit(long count) {
			this.LimitValue = count;
		}

		/// <summary>
		/// 列選択部を生成する
		/// </summary>
		/// <typeparam name="TColumns1">列をプロパティとして持つクラス</typeparam>
		/// <param name="columnsExpression">プロパティが列指定として扱われるクラスを生成する () => new { t1.A, t1.B } の様な式</param>
		/// <returns>SELECT句</returns>
		public SelectFrom<TColumns1> Select<TColumns1>(Expression<Func<TColumns1>> columnsExpression) {
			// new 演算子でクラスを生成するもの以外はエラーとする
			var body = columnsExpression.Body;
			if (body.NodeType != ExpressionType.New) {
				throw new ApplicationException();
			}

			// クラスのプロパティ数とコンストラクタ引数の数が異なるならエラーとする
			var newexpr = body as NewExpression;
			var args = newexpr.Arguments;
			var properties = typeof(TColumns1).GetProperties();
			if (args.Count != properties.Length) {
				throw new ApplicationException();
			}

			// プロパティと列定義を結びつけその生成元としてコンストラクタ引数を指定する
			var environment = this.Table.Environment;
			var sourceColumnMap = this.SourceColumnMap;
			var select = new SelectFrom<TColumns1>(environment, this);
			for (int i = 0; i < properties.Length; i++) {
				var pi = properties[i];
				if (pi.PropertyType != args[i].Type) {
					throw new ApplicationException();
				}
				var context = new ElementCode();
				context.Add(args[i], sourceColumnMap);
				select.BindColumn(pi.Name, "c" + i, environment.CreateDbTypeFromType(pi.PropertyType), 0, context);
			}

			return select;
		}

		/// <summary>
		/// SQL文を生成する
		/// </summary>
		/// <param name="context">生成先のコンテキスト</param>
		public void BuildSql(ElementCode context) {
			if (this.Table != null) {
				context.Add(SqlKeyword.From);
				context.Add(this.Table);
			}

			if (this.Joins != null) {
				foreach (var join in this.Joins) {
					switch (join.JoinType) {
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
					context.Add(join.Table);
					context.Add(SqlKeyword.On);
					context.Add(join.On);
				}
			}

			if (this.WhereExpression != null) {
				context.Add(SqlKeyword.Where);
				context.Add(this.WhereExpression);
			}

			if (this.GroupByColumns != null && this.GroupByColumns.Any()) {
				context.Add(SqlKeyword.GroupBy);
				context.AddColumns(this.GroupByColumns);
			}

			if (this.OrderByColumns != null && this.OrderByColumns.Any()) {
				context.Add(SqlKeyword.OrderBy);
				context.AddColumns(this.GroupByColumns);
			}

			if (this.LimitValue != null) {
				context.Add(SqlKeyword.Limit);
				context.Add(this.LimitValue);
			}
		}
		#endregion

		#region 非公開メソッド
		/// <summary>
		/// 指定された結合種類の<see cref="Join{TColumns}"/>を生成し登録する
		/// </summary>
		/// <typeparam name="TColumns1">結合するテーブルの<see cref="ITable{TColumns}.Columnsの型"/></typeparam>
		/// <param name="joinType">結合種類</param>
		/// <param name="table">結合するテーブル</param>
		/// <param name="on">結合式</param>
		/// <returns><see cref="Join{TColumns}</returns>
		IJoin<TColumns1> JoinByType<TColumns1>(JoinType joinType, ITable<TColumns1> table, Expression<Func<TColumns1, bool>> on) {
			var clone = table.AliasedClone();

			if (this.Joins == null) {
				this.Joins = new List<IJoin>();
			}

			if (!this.SourceTables.Contains(clone)) {
				this.SourceColumnMap.Include(clone.ColumnMap);
			}

			var context = new ElementCode();
			context.Add(ParameterReplacer.Replace(on.Body, new Dictionary<Expression, object> { { on.Parameters[0], clone.Columns } }), this.SourceColumnMap);
			var join = new Join<TColumns1>(joinType, clone, context);
			this.Joins.Add(join);
			return join;
		}
		#endregion
	}
}