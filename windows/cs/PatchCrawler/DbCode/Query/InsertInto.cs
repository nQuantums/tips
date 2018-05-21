using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DbCode.Internal;

namespace DbCode.Query {
	/// <summary>
	/// INSERT INTO句
	/// </summary>
	/// <typeparam name="TColumns">プロパティを列として扱う<see cref="TableDef{TColumns}"/>のTColumnsに該当するクラス</typeparam>
	public class InsertInto<TColumns> : IInsertInto<TColumns> {
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
		public ISelect ValueNode { get; private set; }
		#endregion

		#region コンストラクタ
		/// <summary>
		/// コンストラクタ、親ノード、挿入先テーブルと列の並びを指定して初期化する
		/// </summary>
		/// <param name="parent">親ノード</param>
		/// <param name="table">挿入先テーブル</param>
		/// <param name="columnsExpression">列と設定値を指定する t => new[] { t.A == a, t.B == b } の様な式</param>
		public InsertInto(IQueryNode parent, TableDef<TColumns> table, Expression<Func<TColumns, bool[]>> columnsExpression) {
			this.Parent = parent;

			// new [] { bool, bool... } の様な形式以外はエラーとする
			var body = columnsExpression.Body;
			if (body.NodeType != ExpressionType.NewArrayInit) {
				throw new ApplicationException();
			}

			// 登録
			var owner = this.Owner;
			owner.RegisterTable(table);

			// bool[] の各要素初期化式を取得する
			var newexpr = body as NewArrayExpression;
			var expressions = newexpr.Expressions;
			var map = new Dictionary<Expression, object> { { columnsExpression.Parameters[0], table.Columns } };
			var availableColumns = owner.AllColumns;
			var tableColumnMap = table.ColumnMap;
			var columnsOrder = new ColumnMap();
			var values = new ElementCode[expressions.Count];

			for (int i = 0; i < values.Length; i++) {
				// t.A == a の様に Equal を代入として扱いたいのでそれ以外はエラーとする
				var expression = expressions[i];
				if (expression.NodeType != ExpressionType.Equal) {
					throw new ApplicationException();
				}

				var binary = expression as BinaryExpression;

				// 左辺は代入先の列でなければならない
				var left = new ElementCode(ParameterReplacer.Replace(binary.Left, map), tableColumnMap);
				var column = left.FindColumns().FirstOrDefault();
				if (column == null) {
					throw new ApplicationException();
				}

				// 右辺は式
				values[i] = new ElementCode(ParameterReplacer.Replace(binary.Right, map), availableColumns);

				// 列の生成元を右辺の式にして列を登録
				columnsOrder.Add(column);
			}

			this.Table = table;
			this.ColumnMap = columnsOrder;
			this.ValueNode = new ValueSetter(this, columnsOrder, values);
		}
		#endregion

		#region 公開メソッド
		/// <summary>
		/// 指定ノードを子とする、既存の親は<see cref="RemoveChild(IQueryNode)"/>で切り離す必要がある
		/// </summary>
		/// <param name="child">子ノード</param>
		public void AddChild(IQueryNode child) {
			var select = child as ISelect;
			if (select != null) {
				if (this.ValueNode != null) {
					throw new ApplicationException();
				}
				QueryNodeHelper.SwitchParent(select, this);
				this.ValueNode = select;
			} else {
				throw new ApplicationException();
			}
		}

		/// <summary>
		/// 指定の子ノードを取り除く
		/// </summary>
		/// <param name="child">子ノード</param>
		public void RemoveChild(IQueryNode child) {
			if(this.ValueNode == child) {
				this.ValueNode = null;
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
		/// 同一値が存在しない場合のみ挿入するように判定を追加する
		/// </summary>
		/// <param name="columnCountToWhere">NOT EXISTS (SELECT * FROM t WHERE t.Name = "test") の部分で判定に使用する列数、0が指定されたら全て使用する</param>
		[SqlMethod]
		public void IfNotExists(int columnCountToWhere = 0) {
			var valueSetter = this.ValueNode as ValueSetter;
			if (valueSetter != null) {
				if (columnCountToWhere <= 0 || this.ColumnMap.Count < columnCountToWhere) {
					columnCountToWhere = this.ColumnMap.Count;
				}

				// WHERE NOT EXISTS部作成
				var notExistsSelectFrom = new ElementCode();
				var columnMap = this.ColumnMap;
				notExistsSelectFrom.Add(SqlKeyword.NotExists);
				notExistsSelectFrom.BeginParenthesize();
				notExistsSelectFrom.Add(SqlKeyword.Select, SqlKeyword.Asterisk, SqlKeyword.From);
				this.Table.ToElementCode(notExistsSelectFrom);
				notExistsSelectFrom.Add(this.Table);
				notExistsSelectFrom.Add(SqlKeyword.Where);
				for (int i = 0; i < columnCountToWhere; i++) {
					if (i != 0) {
						notExistsSelectFrom.Add(SqlKeyword.And);
					}
					notExistsSelectFrom.Add(columnMap[i]);
					notExistsSelectFrom.Concat("=");
					notExistsSelectFrom.Add(valueSetter.ColumnMap[i].Source);
				}
				notExistsSelectFrom.EndParenthesize();

				var where = new Where(this);
				where.Expression = notExistsSelectFrom;
				valueSetter.Where(where);
			}
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

	/// <summary>
	/// INSERT INTO句
	/// </summary>
	/// <typeparam name="TColumns">プロパティを列として扱う<see cref="TableDef{TColumns}"/>のTColumnsに該当するクラス</typeparam>
	/// <typeparam name="TColumnsOrder">列の並びを指定する t => new { t.A, t.B } の様な式で生成されるクラス</typeparam>
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
		/// コンストラクタ、親ノード、挿入先テーブルとSELECTノードを指定して初期化する
		/// </summary>
		/// <param name="parent">親ノード</param>
		/// <param name="table">挿入先テーブル</param>
		/// <param name="select">SELECTノード</param>
		public InsertInto(IQueryNode parent, TableDef<TColumns> table, ISelect<TColumnsOrder> select) {
			this.Parent = parent;

			// 登録
			var owner = this.Owner;
			owner.RegisterTable(table);
			owner.RegisterTable(select);

			// 匿名クラスのプロパティを取得、さらに各プロパティ初期化用の式を取得する
			var availableColumns = owner.AllColumns;
			var columnMap = table.ColumnMap;
			var columnsOrder = new ColumnMap();
			foreach (var sourceColumn in select.ColumnMap) {
				if (!availableColumns.Contains(sourceColumn)) {
					throw new ApplicationException();
				}
				var column = columnMap.TryGetByPropertyName(sourceColumn.Property.Name);
				if (column == null) {
					throw new ApplicationException();
				}
				columnsOrder.Add(column);
			}

			this.Table = table;
			this.ColumnMap = columnsOrder;
			this.Value(select);
		}

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
			owner.RegisterTable(table);

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
		/// 指定ノードを子とする、既存の親は<see cref="RemoveChild(IQueryNode)"/>で切り離す必要がある
		/// </summary>
		/// <param name="child">子ノード</param>
		public void AddChild(IQueryNode child) {
			var select = child as ISelect<TColumnsOrder>;
			if (select != null) {
				if (this.ValueNode != null) {
					throw new ApplicationException();
				}
				QueryNodeHelper.SwitchParent(select, this);
				this.ValueNode = select;
			} else {
				throw new ApplicationException();
			}
		}

		/// <summary>
		/// 指定の子ノードを取り除く
		/// </summary>
		/// <param name="child">子ノード</param>
		public void RemoveChild(IQueryNode child) {
			if (this.ValueNode == child) {
				this.ValueNode = null;
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
		/// 挿入する値を指定する
		/// </summary>
		/// <param name="select">SELECTノード</param>
		/// <returns>自分</returns>
		public InsertInto<TColumns, TColumnsOrder> Value(ISelect<TColumnsOrder> select) {
			if (this.ValueNode != null) {
				throw new ApplicationException();
			}
			QueryNodeHelper.SwitchParent(select, this);
			this.ValueNode = select;
			return this;
		}

		/// <summary>
		/// 列選択部を生成する
		/// </summary>
		/// <param name="columnsExpression">プロパティが列指定として扱われるクラスを生成する () => new { t1.A, t1.B } の様な式</param>
		/// <returns>SELECT句</returns>
		[SqlMethod]
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
		[SqlMethod]
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
		[SqlMethod]
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

			var where = new Where(this);
			where.Expression = notExistsSelectFrom;
			select.Where(where);

			return select;
		}

		/// <summary>
		/// SQL文を生成する
		/// </summary>
		/// <param name="context">生成先のコンテキスト</param>
		public void ToElementCode(ElementCode context) {
			if (this.ValueNode is null) {
				throw new ApplicationException();
			}

			var environment = this.Table.Environment;
			context.Add(SqlKeyword.InsertInto);
			context.Concat(this.Table.Name);
			context.AddColumnDefs(this.ColumnMap);

			this.ValueNode.ToElementCode(context);
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
