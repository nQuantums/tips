using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CodeDb.Internal;

namespace CodeDb.Query {
	/// <summary>
	/// INSERT INTO句
	/// </summary>
	/// <typeparam name="TColumns">プロパティを列として扱う<see cref="TableDef{TColumns}"/>のTColumnsに該当するクラス</typeparam>
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
			var properties = typeof(TColumnsOrder).GetProperties();
			var map = new Dictionary<Expression, object> { { columnsExpression.Parameters[0], table.Columns } };
			var availableColumns = owner.AllColumns;
			var tableColumnMap = table.ColumnMap;
			var columnsOrder = new ColumnMap();

			for (int i = 0; i < args.Count; i++) {
				var context = new ElementCode(ParameterReplacer.Replace(args[i], map), availableColumns);
				if (context.Items.Count != 1) {
					throw new ApplicationException();
				}
				var column = context.Items[0] as Column;
				if (column == null) {
					column = tableColumnMap.TryGetByPropertyName(properties[i].Name);
				}
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
		/// <summary>
		/// 列選択部を生成する
		/// </summary>
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
		/// <param name="columnsAssignExpression">列への値代入を示す t => new { Name = "test", ID = 1 } の様な式、入力の t は列名参考用に使うのみ</param>
		/// <returns>SELECT句</returns>
		public Select<TColumnsOrder> Select(Expression<Func<TColumns, TColumnsOrder>> columnsAssignExpression) {
			// new 演算子でクラスを生成するもの以外はエラーとする
			var body = columnsAssignExpression.Body;
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

			// SELECT value1, value2... 部作成
			var select = new Select<TColumnsOrder>(this, properties, args.ToArray());
			this.ValueNode = select;

			return select;
		}

		/// <summary>
		/// 列選択部を生成する
		/// </summary>
		/// <param name="columnsAssignExpression">列への値代入を示す t => new { Name = "test", ID = 1 } の様な式、入力の t は列名参考用に使うのみ</param>
		/// <param name="columnCountToWhere">NOT EXISTS (SELECT * FROM t WHERE t.Name = "test") の部分で判定に使用する列数、0が指定されたら全て使用する</param>
		/// <returns>SELECT句</returns>
		public Select<TColumnsOrder> SelectIfNotExists(Expression<Func<TColumns, TColumnsOrder>> columnsAssignExpression, int columnCountToWhere = 0) {
			// new 演算子でクラスを生成するもの以外はエラーとする
			var body = columnsAssignExpression.Body;
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
			if (columnCountToWhere <= 0) {
				columnCountToWhere = properties.Length;
			}
			if (properties.Length < columnCountToWhere) {
				columnCountToWhere = properties.Length;
			}

			// SELECT value1, value2... 部作成
			var select = new Select<TColumnsOrder>(this, properties, args.ToArray());
			this.ValueNode = select;

			// WHERE NOT EXISTS部作成
			var notExistsSelectFrom = new ElementCode();
			var columnMap = this.Table.ColumnMap;
			var allColumns = this.Owner.AllColumns;
			notExistsSelectFrom.Add(SqlKeyword.NotExists);
			notExistsSelectFrom.BeginParenthesize();
			notExistsSelectFrom.Add(SqlKeyword.Select, SqlKeyword.Asterisk, SqlKeyword.From);
			notExistsSelectFrom.Add(this.Table);
			notExistsSelectFrom.Add(SqlKeyword.Where);
			for (int i = 0; i < columnCountToWhere; i++) {
				if (i != 0) {
					notExistsSelectFrom.Add(SqlKeyword.And);
				}
				var column = columnMap.TryGetByPropertyName(properties[i].Name);
				if (column == null) {
					throw new ApplicationException();
				}
				notExistsSelectFrom.Add(column);
				notExistsSelectFrom.Concat("=");
				notExistsSelectFrom.Add(args[i], allColumns);
			}
			notExistsSelectFrom.EndParenthesize();
			select.Where(notExistsSelectFrom);

			return select;
		}

		/// <summary>
		/// SQL文を生成する
		/// </summary>
		/// <param name="context">生成先のコンテキスト</param>
		public void ToElementCode(ElementCode context) {
			var environment = this.Table.Environment;
			context.Add(SqlKeyword.InsertInto);
			context.Concat(this.Table.Name);
			context.AddColumnDefs(this.ColumnMap);

			var valueNode = this.ValueNode;
			if (valueNode != null) {
				valueNode.ToElementCode(context);
				return;
			}

			throw new ApplicationException();
		}
		#endregion
	}
}
