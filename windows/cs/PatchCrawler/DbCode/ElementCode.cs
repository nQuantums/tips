using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DbCode.Query;
using DbCode.Internal;
using System.Runtime.CompilerServices;

namespace DbCode {
	/// <summary>
	/// <see cref="Concat"/>、<see cref="Add(int)"/>などで要素を追加する形で式を構成する
	/// </summary>
	public class ElementCode {
		#region 内部クラス
		/// <summary>
		/// 核となるバッファ
		/// </summary>
		class Core {
			/// <summary>
			/// <see cref="System.Text.StringBuilder"/>または式を構成するオブジェクトのリスト
			/// </summary>
			public readonly List<object> Items = new List<object>();

			/// <summary>
			/// 数式的要素数
			/// </summary>
			public int ItemCount = 0;

			/// <summary>
			/// 値を追加する
			/// </summary>
			/// <param name="value">値</param>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Add(object value) {
				this.Items.Add(value);
				this.ItemCount++;
			}
		}

		/// <summary>
		/// 型毎のアイテム追加処理を行う
		/// </summary>
		class Typewise : ITypeWise {
			ElementCode _EditableExpr;
			public Typewise(ElementCode editableExpr) => _EditableExpr = editableExpr;
			public void DoNull() => _EditableExpr.Add(SqlKeyword.Null);
			public bool Prepare(object value) => false;
			public void Do(char value) => _EditableExpr.Add(value);
			public void Do(char[] value) => _EditableExpr.Add(value);
			public void Do(bool value) => _EditableExpr.Add(value);
			public void Do(bool[] value) => _EditableExpr.Add(value);
			public void Do(int value) => _EditableExpr.Add(value);
			public void Do(int[] value) => _EditableExpr.Add(value);
			public void Do(long value) => _EditableExpr.Add(value);
			public void Do(long[] value) => _EditableExpr.Add(value);
			public void Do(double value) => _EditableExpr.Add(value);
			public void Do(double[] value) => _EditableExpr.Add(value);
			public void Do(string value) => _EditableExpr.Add(value);
			public void Do(string[] value) => _EditableExpr.Add(value);
			public void Do(Guid value) => _EditableExpr.Add(value);
			public void Do(Guid[] value) => _EditableExpr.Add(value);
			public void Do(DateTime value) => _EditableExpr.Add(value);
			public void Do(DateTime[] value) => _EditableExpr.Add(value);
			public void Do(Column value) => _EditableExpr.Add(value);
			public void Do(Argument value) => _EditableExpr.Add(value);
		}
		#endregion

		#region フィールド
		Core _Core = new Core();
		Stack<Core> _CoreStack = new Stack<Core>();
		Dictionary<object, List<Action<object, WorkingBuffer>>> _HandlersOnBuild = new Dictionary<object, List<Action<object, WorkingBuffer>>>();
		Typewise _Typewise;
		#endregion

		#region プロパティ
		/// <summary>
		/// 式を構成するアイテム数の取得、<see cref="Items"/>のカウントとは異なる
		/// </summary>
		public int ItemCount => _Core.ItemCount;

		/// <summary>
		/// アイテムリスト
		/// </summary>
		public List<object> Items => _Core.Items;
		#endregion

		#region コンストラクタ
		/// <summary>
		/// コンストラクタ
		/// </summary>
		public ElementCode() {
			_Typewise = new Typewise(this);
		}

		/// <summary>
		/// コンストラクタ、SQLのコードを指定して初期化する
		/// </summary>
		/// <param name="code">SQLコード</param>
		public ElementCode(string code) : this() {
			this.Concat(code);
		}

		/// <summary>
		/// コンストラクタ、式木と列プロパティマップを指定して初期化する
		/// </summary>
		/// <param name="expression">式木</param>
		/// <param name="allAvailableColumns">使用可能な全ての列のプロパティマップ</param>
		/// <param name="queryNodeDetected">式中で<see cref="IQueryNode"/>が検出された際に呼び出される</param>
		public ElementCode(Expression expression, ColumnMap allAvailableColumns, Action<IQueryNode> queryNodeDetected = null) {
			_Core = new Core();
			_CoreStack = new Stack<Core>();
			_Typewise = new Typewise(this);

			Add(expression, allAvailableColumns, queryNodeDetected);
		}

		/// <summary>
		/// コンストラクタ、式木と列プロパティマップを指定して初期化する、パラメータ０を置き換えたものを登録する
		/// </summary>
		/// <param name="lambdaExpression">式木</param>
		/// <param name="allAvailableColumns">使用可能な全ての列のプロパティマップ</param>
		/// <param name="param0"><c>lambdaExpression</c>引数のパラメータ0がこれに置き換わる</param>
		/// <param name="queryNodeDetected">式中で<see cref="IQueryNode"/>が検出された際に呼び出される</param>
		public ElementCode(LambdaExpression lambdaExpression, ColumnMap allAvailableColumns, object param0, Action<IQueryNode> queryNodeDetected = null) {
			_Core = new Core();
			_CoreStack = new Stack<Core>();
			_Typewise = new Typewise(this);

			var replacedExpression = ParameterReplacer.Replace(
				lambdaExpression.Body,
				new Dictionary<Expression, object> { { lambdaExpression.Parameters[0], param0 } }
			);

			Add(replacedExpression, allAvailableColumns, queryNodeDetected);
		}
		#endregion

		#region 公開メソッド
		/// <summary>
		/// 式を括弧で括り始める
		/// </summary>
		public void Push() {
			_CoreStack.Push(_Core);
			_Core = new Core();
		}

		/// <summary>
		/// 括弧で括るのを終える
		/// </summary>
		public void Pop() {
			var core = _Core;
			_Core = _CoreStack.Pop();
			if (2 <= core.ItemCount) {
				this.Concat("(");
				_Core.Items.AddRange(core.Items);
				this.Concat(")");
			} else {
				_Core.Items.AddRange(core.Items);
			}
			_Core.ItemCount++;
		}

		/// <summary>
		/// 文字列を連結する、連結すると意味が変わってしまう識別子などの場合はスペースが挟まれる
		/// </summary>
		/// <param name="value">連結する文字列</param>
		public void Concat(string value) {
			// 連結するものが無ければ何もしない
			if (value is null || value.Length == 0) {
				return;
			}

			// 連結先 StringBuffer の取得
			StringBuilder sb;
			var items = _Core.Items;
			if (items.Count == 0) {
				items.Add(sb = new StringBuilder());
			} else {
				var sb2 = items[items.Count - 1] as StringBuilder;
				if (sb2 is null) {
					items.Add(sb = new StringBuilder());
				} else {
					sb = sb2;
				}
			}

			// 記号系以外が連続すると意味が変わってしまうためスペースを挟む
			if (sb.Length != 0 && WorkingBuffer.Symbols.IndexOf(sb[sb.Length - 1]) < 0 && WorkingBuffer.Symbols.IndexOf(value[0]) < 0) {
				sb.Append(' ');
			}

			// 連結
			sb.Append(value);
			_Core.ItemCount++;
		}

		public void Add(char value) => _Core.Add(value);
		public void Add(char[] value) => _Core.Add(value);
		public void Add(bool value) => this.Concat(value.ToString());
		public void Add(bool[] value) => _Core.Add(value);
		public void Add(int value) => this.Concat(value.ToString());
		public void Add(int[] value) => _Core.Add(value);
		public void Add(long value) => this.Concat(value.ToString());
		public void Add(long[] value) => _Core.Add(value);
		public void Add(double value) => this.Concat(value.ToString());
		public void Add(double[] value) => _Core.Add(value);
		public void Add(string value) => _Core.Add(value);
		public void Add(string[] value) => _Core.Add(value);
		public void Add(Guid value) => this.Concat(string.Concat("'", value, "'"));
		public void Add(Guid[] value) => _Core.Add(value);
		public void Add(DateTime value) => _Core.Add(value);
		public void Add(DateTime[] value) => _Core.Add(value);
		public void Add(Column value) => _Core.Add(value);
		public void Add(ElementCode value) => _Core.Add(value);
		public void Add(Type value) => _Core.Add(value);
		public void Add(Argument value) => _Core.Add(value);
		public void Add(SqlKeyword keyword) {
			this.Concat(KeywordToString(keyword));
		}

		public void Add(SqlKeyword keyword1, SqlKeyword keyword2) {
			this.Concat(KeywordToString(keyword1));
			this.Concat(KeywordToString(keyword2));
		}

		public void Add(SqlKeyword keyword1, SqlKeyword keyword2, SqlKeyword keyword3) {
			this.Concat(KeywordToString(keyword1));
			this.Concat(KeywordToString(keyword2));
			this.Concat(KeywordToString(keyword3));
		}

		public void Add(ExpressionType nodeType) {
			this.Concat(NodeTypeToString(nodeType));
		}

		/// <summary>
		/// オブジェクトを追加する、型が対応しているものの場合のみ追加される
		/// </summary>
		/// <param name="value">オブジェクト</param>
		public void AddObject(object value) {
			if (!TypewiseExecutor.Do(_Typewise, value)) {
				throw new ApplicationException($"The type '{value.GetType().FullName}' can not be included in an expression.");
			}
		}

		/// <summary>
		/// <see cref="Commandable.CommandTextAndParameters"/>取得時に評価されるコードを追加する
		/// </summary>
		/// <param name="code">コード</param>
		public void Add(IDelayedCode code) {
			_Core.Add(code);
		}

		/// <summary>
		/// テーブルを追加する、これはビルド時にエイリアス名に変換される
		/// </summary>
		/// <param name="table">テーブル</param>
		public void Add(ITable table) {
			_Core.Add(table);
		}

		/// <summary>
		/// SELECTノードを追加する、これはビルド時にエイリアス名に変換される
		/// </summary>
		/// <param name="select">SELECT句ノード</param>
		public void Add(ISelect select) {
			_Core.Add(select);
		}

		/// <summary>
		/// 式木を追加する
		/// </summary>
		/// <param name="expression">式木</param>
		/// <param name="allAvailableColumns">使用可能な全ての列のプロパティマップ</param>
		/// <param name="queryNodeDetected">式中で<see cref="IQueryNode"/>が検出された際に呼び出される</param>
		public void Add(Expression expression, ColumnMap allAvailableColumns, Action<IQueryNode> queryNodeDetected = null) {
			var visitor = new ElementCodeExpressionVisitor(this, allAvailableColumns);
			if (queryNodeDetected != null) {
				visitor.QueryNodeDetected += queryNodeDetected;
			}
			visitor.Visit(expression);
		}

		/// <summary>
		/// 指定オブジェクトのプロパティをカンマ区切りで追加する
		/// </summary>
		/// <typeparam name="TColumns">オブジェクトの型</typeparam>
		/// <param name="value">オブジェクト</param>
		public void AddValues<TColumns>(TColumns value) {
			TypewiseCache<TColumns>.AddValues(this, value);
		}

		public void AddColumnDefs(IEnumerable<IColumnDef> columns) {
			BeginParenthesize();
			var first = true;
			foreach (var column in columns) {
				if (first) {
					first = false;
				} else {
					AddComma();
				}
				Concat(column.Name);
			}
			EndParenthesize();
		}

		public void AddColumnDefs(IEnumerable<IColumnDef> columns, Action<IColumnDef> before, Action<IColumnDef> after) {
			BeginParenthesize();
			var first = true;
			foreach (var column in columns) {
				if (first) {
					first = false;
				} else {
					AddComma();
				}
				if (before != null) {
					before(column);
				}
				Concat(column.Name);
				if (after != null) {
					after(column);
				}
			}
			EndParenthesize();
		}

		public void AddColumns(IEnumerable<Column> columns, Action<Column> columnwiseProc = null) {
			var first = true;
			foreach (var column in columns) {
				if (first) {
					first = false;
				} else {
					AddComma();
				}
				if (columnwiseProc != null) {
					columnwiseProc(column);
				} else {
					Add(column);
				}
			}
		}

		public void AddComma() {
			this.Concat(",");
		}

		public void BeginParenthesize() {
			this.Concat("(");
		}

		public void EndParenthesize() {
			this.Concat(")");
		}

		public void Go() {
			this.Concat(";");
		}

		/// <summary>
		/// 指定アイテムをビルド時に処理する際のハンドラを登録する
		/// </summary>
		/// <param name="item"><see cref="AddObject(object)"/>などで追加するアイテム</param>
		/// <param name="handler">アイテムを処理するハンドラ、<see cref="Build(WorkingBuffer)"/>内から呼び出される</param>
		public void RegisterBuildHandler(object item, Action<object, WorkingBuffer> handler) {
			List<Action<object, WorkingBuffer>> handlers;
			if (!_HandlersOnBuild.TryGetValue(item, out handlers)) {
				_HandlersOnBuild[item] = handlers = new List<Action<object, WorkingBuffer>>();
			}
			handlers.Add(handler);
		}

		public override string ToString() {
			try {
				var cmd = Build();
				return cmd.CommandTextAndParameters.Item1;
			} catch {
				return "";
			}
		}

		/// <summary>
		/// <see cref="IDbCodeCommand"/>に渡して実行可能な形式にビルドする
		/// </summary>
		/// <returns>実行可能SQL</returns>
		public Commandable Build() {
			var work = new WorkingBuffer();
			this.Build(work);
			if (work.HasDelayedCode) {
				return new DelayedCommandable(work);
			} else {
				return new ImmediatelyCommandable(work.Build(), work.Parameters);
			}
		}

		/// <summary>
		/// 再帰的に指定のバッファへ展開する
		/// </summary>
		/// <param name="work">展開先バッファ</param>
		public void Build(WorkingBuffer work) {
			foreach (var item in this.Items) {
				StringBuilder buffer;
				if ((buffer = item as StringBuilder) != null) {
					work.Concat(buffer.ToString());
				} else {
					if (_HandlersOnBuild.TryGetValue(item, out List<Action<object, WorkingBuffer>> handlers)) {
						foreach (var handler in handlers) {
							handler(item, work);
						}
					} else {
						ElementCode ec;
						ITableDef tableDef;
						ITable table;
						Column column;
						Type type;
						IDelayedCode dc;
						if ((ec = item as ElementCode) != null) {
							ec.Build(work);
						} else if ((tableDef = item as ITableDef) != null) {
							work.Concat(work.GetTableAlias(tableDef));
						} else if ((table = item as ITable) != null) {
							work.Concat(work.GetTableAlias(table));
						} else if ((column = item as Column) != null) {
							work.Concat(work.GetTableAlias(column.Table));
							work.Concat(".");
							work.Concat(column.Name);
						} else if ((dc = item as IDelayedCode) != null) {
							work.Add(dc);
						} else if ((type = item as Type) != null) {
							// 型をマーキングしてあるだけなので何もする事はない
						} else {
							// 上記に引っかからないものはSQL実行時にパラメータとして渡される
							work.Concat(work.GetParameterName(item));
						}
					}
				}
			}
		}

		/// <summary>
		/// 全<see cref="Type"/>を列挙する
		/// </summary>
		public IEnumerable<Type> FindTypes() {
			foreach (var item in _Core.Items) {
				var type = item as Type;
				if (type is null) {
					var ec = item as ElementCode;
					if (ec != null) {
						foreach (var t in ec.FindTypes()) {
							yield return t;
						}
					}
				} else {
					yield return type;
				}
			}
		}

		/// <summary>
		/// 全<see cref="Argument"/>を列挙する
		/// </summary>
		public IEnumerable<Argument> FindArguments() {
			foreach (var item in _Core.Items) {
				var variable = item as Argument;
				if (variable is null) {
					var ec = item as ElementCode;
					if (ec != null) {
						foreach (var v in ec.FindArguments()) {
							yield return v;
						}
					}
				} else {
					yield return variable;
				}
			}
		}

		/// <summary>
		/// 全<see cref="Column"/>を列挙する
		/// </summary>
		public IEnumerable<Column> FindColumns() {
			foreach (var item in _Core.Items) {
				var column = item as Column;
				if (column != null) {
					yield return column;
				} else {
					var ec = item as ElementCode;
					if (ec != null) {
						foreach (var c in ec.FindColumns()) {
							yield return c;
						}
					}
				}
			}
		}

		/// <summary>
		/// 全<see cref="ITable"/>を列挙する
		/// </summary>
		public IEnumerable<ITable> FindTables() {
			foreach (var item in _Core.Items) {
				var table = item as ITable;
				if (table != null) {
					yield return table;
				} else {
					var column = item as Column;
					if (column != null) {
						yield return column.Table;
					} else {
						var ec = item as ElementCode;
						if (ec != null) {
							foreach (var t in ec.FindTables()) {
								yield return t;
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// 式ノードの種類<see cref="ExpressionType"/>から文字列への変換
		/// </summary>
		/// <param name="nodeType">式ノードの種類</param>
		/// <returns>文字列</returns>
		public static string NodeTypeToString(ExpressionType nodeType) {
			switch (nodeType) {
			case ExpressionType.Convert:
				return "";
			case ExpressionType.Not:
				return KeywordToString(SqlKeyword.Not);
			case ExpressionType.OnesComplement:
				return "~";
			case ExpressionType.Negate:
				return "-";
			case ExpressionType.UnaryPlus:
				return "+";
			case ExpressionType.Equal:
				return "=";
			case ExpressionType.NotEqual:
				return "<>";
			case ExpressionType.AndAlso:
				return KeywordToString(SqlKeyword.And);
			case ExpressionType.OrElse:
				return KeywordToString(SqlKeyword.Or);
			case ExpressionType.GreaterThan:
				return ">";
			case ExpressionType.LessThan:
				return "<";
			case ExpressionType.GreaterThanOrEqual:
				return ">=";
			case ExpressionType.LessThanOrEqual:
				return "<=";
			case ExpressionType.Add:
				return "+";
			case ExpressionType.AddChecked:
				return "#+";
			case ExpressionType.Subtract:
				return "-";
			case ExpressionType.SubtractChecked:
				return "#-";
			case ExpressionType.Divide:
				return "/";
			case ExpressionType.Modulo:
				return "%";
			case ExpressionType.Multiply:
				return "*";
			case ExpressionType.MultiplyChecked:
				return "#*";
			case ExpressionType.LeftShift:
				return "<<";
			case ExpressionType.RightShift:
				return ">>";
			case ExpressionType.And:
				return "&";
			case ExpressionType.Or:
				return "|";
			case ExpressionType.ExclusiveOr:
				return "^";
			case ExpressionType.Power:
				return "**";
			case ExpressionType.Coalesce:
				return "??";
			default:
				//case ExpressionType.IsFalse:
				//case ExpressionType.IsTrue:
				//case ExpressionType.Quote:
				//case ExpressionType.ConvertChecked:
				//case ExpressionType.NegateChecked:
				//case ExpressionType.Throw:
				//case ExpressionType.Decrement:
				//case ExpressionType.Increment:
				//case ExpressionType.PreDecrementAssign:
				//case ExpressionType.PreIncrementAssign:
				//case ExpressionType.Unbox:
				throw new ApplicationException();
			}
		}

		/// <summary>
		/// SQLキーワード<see cref="SqlKeyword"/>から文字列への変換
		/// </summary>
		/// <param name="keyword">SQLキーワード</param>
		/// <returns>文字列</returns>
		public static string KeywordToString(SqlKeyword keyword) {
			switch (keyword) {
			case SqlKeyword.Null:
				return "NULL";
			case SqlKeyword.Asterisk:
				return "*";
			case SqlKeyword.Not:
				return "NOT";
			case SqlKeyword.NotNull:
				return "NOT NULL";
			case SqlKeyword.CreateTable:
				return "CREATE TABLE";
			case SqlKeyword.DropTable:
				return "DROP TABLE";
			case SqlKeyword.CreateIndex:
				return "CREATE INDEX";
			case SqlKeyword.DropIndex:
				return "DROP INDEX";
			case SqlKeyword.Exists:
				return "EXISTS";
			case SqlKeyword.NotExists:
				return "NOT EXISTS";
			case SqlKeyword.IfExists:
				return "IF EXISTS";
			case SqlKeyword.IfNotExists:
				return "IF NOT EXISTS";
			case SqlKeyword.PrimaryKey:
				return "PRIMARY KEY";
			case SqlKeyword.Select:
				return "SELECT";
			case SqlKeyword.From:
				return "FROM";
			case SqlKeyword.Where:
				return "WHERE";
			case SqlKeyword.InnerJoin:
				return "INNER JOIN";
			case SqlKeyword.LeftJoin:
				return "LEFT JOIN";
			case SqlKeyword.RightJoin:
				return "RIGHT JOIN";
			case SqlKeyword.On:
				return "ON";
			case SqlKeyword.And:
				return "AND";
			case SqlKeyword.Or:
				return "OR";
			case SqlKeyword.GroupBy:
				return "GROUP BY";
			case SqlKeyword.OrderBy:
				return "ORDER BY";
			case SqlKeyword.InsertInto:
				return "INSERT INTO";
			case SqlKeyword.Limit:
				return "LIMIT";
			case SqlKeyword.As:
				return "AS";
			case SqlKeyword.IsNull:
				return "IS NULL";
			case SqlKeyword.IsNotNull:
				return "IS NOT NULL";
			case SqlKeyword.Using:
				return "USING";
			case SqlKeyword.Like:
				return "LIKE";
			case SqlKeyword.In:
				return "IN";
			case SqlKeyword.Values:
				return "VALUES";
			case SqlKeyword.Default:
				return "DEFAULT";
			case SqlKeyword.CurrentTimestamp:
				return "CURRENT_TIMESTAMP";
			case SqlKeyword.AlterTable:
				return "ALTER TABLE";
			case SqlKeyword.DropConstraint:
				return "DROP CONSTRAINT";
			case SqlKeyword.AddConstraint:
				return "ADD CONSTRAINT";
			case SqlKeyword.DropColumn:
				return "DROP COLUMN";
			case SqlKeyword.AddColumn:
				return "ADD COLUMN";
			case SqlKeyword.CreateRole:
				return "CREATE ROLE";
			case SqlKeyword.Password:
				return "PASSWORD";
			case SqlKeyword.CreateDatabase:
				return "CREATE DATABASE";
			case SqlKeyword.Owner:
				return "OWNER";
			case SqlKeyword.Unique:
				return "UNIQUE";
			default:
				return "";
			}
		}
		#endregion
	}
}
