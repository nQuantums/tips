using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CodeDb.Query;
using CodeDb.Internal;

using MethodNodeHandler = System.Action<CodeDb.ElementCode.Visitor, CodeDb.ElementCode, System.Linq.Expressions.MethodCallExpression>;

namespace CodeDb {
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
		}

		/// <summary>
		/// 型毎のアイテム追加処理を行う
		/// </summary>
		class TypeWise : ITypeWise {
			ElementCode _EditableExpr;
			public TypeWise(ElementCode editableExpr) => _EditableExpr = editableExpr;
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
			public void Do(Variable value) => _EditableExpr.Add(value);
		}

		/// <summary>
		/// <see cref="Expression"/>のツリーを<see cref="ElementCode"/>へ展開するためのVisitor
		/// </summary>
		public class Visitor : ExpressionVisitor {
			ElementCode _ExprInProgress;
			ColumnMap _ColDefDic;

			public Visitor(ElementCode exprInProgress, ColumnMap colDefDic = null) {
				_ExprInProgress = exprInProgress;
				_ColDefDic = colDefDic;
			}

			protected override Expression VisitUnary(UnaryExpression node) {
				_ExprInProgress.Add(node.NodeType);
				_ExprInProgress.Push();
				this.Visit(node.Operand);
				_ExprInProgress.Pop();
				return node;
			}

			protected override Expression VisitBinary(BinaryExpression node) {
				var nodeType = node.NodeType;

				if (nodeType == ExpressionType.ArrayIndex) {
					_ExprInProgress.Push();
					this.Visit(node.Left);
					_ExprInProgress.Pop();
					_ExprInProgress.Concat("[");
					this.Visit(Expression.Add(node.Right, Expression.Constant(1)));
					_ExprInProgress.Concat("]");
				} else {
					var l = node.Left;
					var r = node.Right;

					var p = GetOperatorPrecedence(nodeType);
					if (p == GetOperatorPrecedence(l.NodeType)) {
						this.Visit(l);
					} else {
						_ExprInProgress.Push();
						this.Visit(l);
						_ExprInProgress.Pop();
					}

					if (nodeType == ExpressionType.Equal && r.NodeType == ExpressionType.Constant && Evaluate(r) == null) {
						_ExprInProgress.Add(SqlKeyword.IsNull);
					} else if (nodeType == ExpressionType.NotEqual && r.NodeType == ExpressionType.Constant && Evaluate(r) == null) {
						_ExprInProgress.Add(SqlKeyword.IsNotNull);
					} else {
						_ExprInProgress.Add(nodeType);

						if (p == GetOperatorPrecedence(r.NodeType)) {
							this.Visit(r);
						} else {
							_ExprInProgress.Push();
							this.Visit(r);
							_ExprInProgress.Pop();
						}
					}
				}
				return node;
			}

			protected override Expression VisitConstant(ConstantExpression node) {
				_ExprInProgress.Add(Evaluate(node));
				return node;
			}

			protected override Expression VisitMember(MemberExpression node) {
				var expression = node.Expression;
				var member = node.Member;
				var obj = expression != null ? Evaluate(expression) : null;

				if (obj != null) {
					if (member.MemberType == MemberTypes.Property) {
						var pi = (PropertyInfo)member;

						// プロパティが列定義と結びつくものなら列定義を登録する
						var colDef = _ColDefDic.TryGet(obj, pi);
						if (colDef != null) {
							_ExprInProgress.Add(colDef);
							return node;
						}
					}
				}

				// Nullable の HasValue プロパティなら IS NOT NULL を登録する
				if (node.Member.Name == "HasValue") {
					_ExprInProgress.Push();
					this.Visit(node.Expression);
					_ExprInProgress.Pop();
					_ExprInProgress.Add(SqlKeyword.IsNotNull);
					return node;
				}

				// ノードを評価し値を登録する
				// ※評価時のメンバアクセス結果の値が登録される
				_ExprInProgress.Add(Evaluate(node));
				return node;
			}

			protected override Expression VisitMethodCall(MethodCallExpression node) {
				// 登録されたメソッド毎に処理を行う
				MethodNodeHandler handler;
				if (MethodProc.TryGetValue(node.Method, out handler)) {
					handler(this, _ExprInProgress, node);
				}

				throw new ApplicationException($"The method call '{node.Method.Name}' can not be included in an expression.");
			}

			protected override Expression VisitNewArray(NewArrayExpression node) {
				var nodeType = node.NodeType;
				var expressions = node.Expressions;
				if (nodeType == ExpressionType.NewArrayInit) {
					_ExprInProgress.Concat("ARRAY[");
					for (int i = 0, n = expressions.Count; i < n; i++) {
						if (i != 0) {
							_ExprInProgress.Concat(",");
						}
						_ExprInProgress.Push();
						this.Visit(expressions[i]);
						_ExprInProgress.Pop();
					}
					_ExprInProgress.Concat("]");
				} else if (nodeType == ExpressionType.NewArrayBounds) {
					_ExprInProgress.Concat("ARRAY[");
					for (int i = 0, n = expressions.Count; i < n; i++) {
						if (i != 0) {
							_ExprInProgress.Concat(",");
						}
						_ExprInProgress.Push();
						this.Visit(expressions[i]);
						_ExprInProgress.Pop();
					}
					_ExprInProgress.Concat("]");
				}
				return node;
			}

			protected override Expression VisitNew(NewExpression node) {
				_ExprInProgress.Add(Evaluate(node));
				return node;
			}

			protected override Expression VisitParameter(ParameterExpression node) => throw new ApplicationException();
			protected override Expression VisitBlock(BlockExpression node) => throw new ApplicationException();
			protected override Expression VisitDefault(DefaultExpression node) => throw new ApplicationException();
			protected override Expression VisitListInit(ListInitExpression node) => throw new ApplicationException();
			protected override Expression VisitMemberInit(MemberInitExpression node) => throw new ApplicationException();
			protected override Expression VisitTypeBinary(TypeBinaryExpression node) => throw new ApplicationException();
			protected override Expression VisitLabel(LabelExpression node) => throw new ApplicationException();
			protected override Expression VisitGoto(GotoExpression node) => throw new ApplicationException();
			protected override Expression VisitLoop(LoopExpression node) => throw new ApplicationException();
			protected override Expression VisitSwitch(SwitchExpression node) => throw new ApplicationException();
			protected override Expression VisitTry(TryExpression node) => throw new ApplicationException();
			protected override Expression VisitIndex(IndexExpression node) => throw new ApplicationException();
			protected override Expression VisitExtension(Expression node) => throw new ApplicationException();
			protected override Expression VisitDebugInfo(DebugInfoExpression node) => throw new ApplicationException();
			protected override Expression VisitInvocation(InvocationExpression node) => throw new ApplicationException();
			protected override Expression VisitConditional(ConditionalExpression node) => throw new ApplicationException();
			protected override Expression VisitRuntimeVariables(RuntimeVariablesExpression node) => throw new ApplicationException();
			protected override CatchBlock VisitCatchBlock(CatchBlock node) => throw new ApplicationException();
			protected override SwitchCase VisitSwitchCase(SwitchCase node) => throw new ApplicationException();
			protected override ElementInit VisitElementInit(ElementInit node) => throw new ApplicationException();
			protected override MemberAssignment VisitMemberAssignment(MemberAssignment assignment) => throw new ApplicationException();
			protected override MemberListBinding VisitMemberListBinding(MemberListBinding binding) => throw new ApplicationException();
			protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding binding) => throw new ApplicationException();


			public static object Evaluate(Expression expression) {
				if (expression == null) {
					return null;
				}
				return Expression.Lambda(expression).Compile().DynamicInvoke();
			}

			public static int GetOperatorPrecedence(ExpressionType nodeType) {
				switch (nodeType) {
				case ExpressionType.Coalesce:
				case ExpressionType.Assign:
				case ExpressionType.AddAssign:
				case ExpressionType.AndAssign:
				case ExpressionType.DivideAssign:
				case ExpressionType.ExclusiveOrAssign:
				case ExpressionType.LeftShiftAssign:
				case ExpressionType.ModuloAssign:
				case ExpressionType.MultiplyAssign:
				case ExpressionType.OrAssign:
				case ExpressionType.PowerAssign:
				case ExpressionType.RightShiftAssign:
				case ExpressionType.SubtractAssign:
				case ExpressionType.AddAssignChecked:
				case ExpressionType.MultiplyAssignChecked:
				case ExpressionType.SubtractAssignChecked:
					return 1;
				case ExpressionType.OrElse:
					return 2;
				case ExpressionType.AndAlso:
					return 3;
				case ExpressionType.Or:
					return 4;
				case ExpressionType.ExclusiveOr:
					return 5;
				case ExpressionType.And:
					return 6;
				case ExpressionType.Equal:
				case ExpressionType.NotEqual:
					return 7;
				case ExpressionType.GreaterThan:
				case ExpressionType.GreaterThanOrEqual:
				case ExpressionType.LessThan:
				case ExpressionType.LessThanOrEqual:
				case ExpressionType.TypeAs:
				case ExpressionType.TypeIs:
				case ExpressionType.TypeEqual:
					return 8;
				case ExpressionType.LeftShift:
				case ExpressionType.RightShift:
					return 9;
				case ExpressionType.Add:
				case ExpressionType.AddChecked:
				case ExpressionType.Subtract:
				case ExpressionType.SubtractChecked:
					return 10;
				case ExpressionType.Divide:
				case ExpressionType.Modulo:
				case ExpressionType.Multiply:
				case ExpressionType.MultiplyChecked:
					return 11;
				case ExpressionType.Convert:
				case ExpressionType.ConvertChecked:
				case ExpressionType.Negate:
				case ExpressionType.UnaryPlus:
				case ExpressionType.NegateChecked:
				case ExpressionType.Not:
				case ExpressionType.Decrement:
				case ExpressionType.Increment:
				case ExpressionType.Throw:
				case ExpressionType.Unbox:
				case ExpressionType.PreIncrementAssign:
				case ExpressionType.PreDecrementAssign:
				case ExpressionType.OnesComplement:
				case ExpressionType.IsTrue:
				case ExpressionType.IsFalse:
					return 12;
				case ExpressionType.Power:
					return 13;
				default:
					return 14;
				case ExpressionType.Constant:
				case ExpressionType.Parameter:
					return 15;
				}
			}
		}

		/// <summary>
		/// 作業用のバッファ
		/// </summary>
		public class WorkingBuffer {
			public StringBuilder Buffer { get; private set; } = new StringBuilder();
			public List<object> Parameters { get; private set; } = new List<object>();
			public List<object> Tables { get; private set; } = new List<object>();

			public string GetParameterName(object value) {
				// IndexOf などでは Variable のオーバーロードのせいで正しく判定できないので自前で object.ReferenceEquals 呼び出して判定する
				var parameters = this.Parameters;
				for (int i = 0, n = parameters.Count; i < n; i++) {
					if (object.ReferenceEquals(value, parameters[i])) {
						return "@p" + i;
					}
				}
				var index = parameters.Count;
				parameters.Add(value);
				return "@p" + index;
			}

			public string GetTableAlias(object table) {
				var tables = this.Tables;
				var index = tables.IndexOf(table);
				if (0 <= index) {
					return "t" + index;
				}
				index = tables.Count;
				tables.Add(table);
				return "t" + index;
			}

			public void Concat(string value) {
				// 連結するものが無ければ何もしない
				if (string.IsNullOrEmpty(value)) {
					return;
				}

				// 連結先 StringBuffer の取得
				var sb = this.Buffer;

				// 記号系以外が連続してしまうならスペースを挟む
				if (sb.Length != 0 && Symbols.IndexOf(sb[sb.Length - 1]) < 0 && Symbols.IndexOf(value[0]) < 0) {
					sb.Append(' ');
				}

				// 連結
				sb.Append(value);
			}
		}
		#endregion

		#region フィールド
		public static readonly Dictionary<MethodInfo, MethodNodeHandler> MethodProc = new Dictionary<MethodInfo, MethodNodeHandler> {
			{ typeof(Sql).GetMethod(nameof(Sql.Like)), (visitor, core, expr) => {
				core.Push();
				visitor.Visit(expr.Arguments[0]);
				core.Pop();

				core.Add(SqlKeyword.Like);

				core.Push();
				visitor.Visit(expr.Arguments[1]);
				core.Pop();
			} },
			{ typeof(Sql).GetMethod(nameof(Sql.Exists)), (visitor, core, expr) => {
				core.Add(SqlKeyword.Exists);

				core.Push();
				visitor.Visit(expr.Arguments[0]);
				core.Pop();
			} },
			{ typeof(Sql).GetMethod(nameof(Sql.NotExists)), (visitor, core, expr) => {
				core.Add(SqlKeyword.NotExists);

				core.Push();
				visitor.Visit(expr.Arguments[0]);
				core.Pop();
			} },
		};

		const string Symbols = "(),.+-*/%=<>#;";

		Core _Core;
		Stack<Core> _CoreStack;
		TypeWise _TypeWise;
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
			_Core = new Core();
			_CoreStack = new Stack<Core>();
			_TypeWise = new TypeWise(this);
		}

		/// <summary>
		/// コンストラクタ、式木と列プロパティマップを指定して初期化する
		/// </summary>
		/// <param name="expression">式木</param>
		/// <param name="allAvailableColumns">使用可能な全ての列のプロパティマップ</param>
		public ElementCode(Expression expression, ColumnMap allAvailableColumns) {
			_Core = new Core();
			_CoreStack = new Stack<Core>();
			_TypeWise = new TypeWise(this);

			Add(expression, allAvailableColumns);
		}

		/// <summary>
		/// コンストラクタ、式木と列プロパティマップを指定して初期化する、パラメータ０を置き換えたものを登録する
		/// </summary>
		/// <param name="lambdaExpression">式木</param>
		/// <param name="allAvailableColumns">使用可能な全ての列のプロパティマップ</param>
		/// <param name="param0"><see cref="lambdaExpression"/>のパラメータ０がこれに置き換わる</param>
		public ElementCode(LambdaExpression lambdaExpression, ColumnMap allAvailableColumns, object param0) {
			_Core = new Core();
			_CoreStack = new Stack<Core>();
			_TypeWise = new TypeWise(this);

			var replacedExpression = ParameterReplacer.Replace(
				lambdaExpression.Body,
				new Dictionary<Expression, object> { { lambdaExpression.Parameters[0], param0 } }
			);

			Add(replacedExpression, allAvailableColumns);
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
			if (string.IsNullOrEmpty(value)) {
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
			if (sb.Length != 0 && Symbols.IndexOf(sb[sb.Length - 1]) < 0 && Symbols.IndexOf(value[0]) < 0) {
				sb.Append(' ');
			}

			// 連結
			sb.Append(value);
			_Core.ItemCount++;
		}

		public void Add(char value) => _Core.Items.Add(value);
		public void Add(char[] value) => _Core.Items.Add(value);
		public void Add(bool value) => this.Concat(value.ToString());
		public void Add(bool[] value) => _Core.Items.Add(value);
		public void Add(int value) => this.Concat(value.ToString());
		public void Add(int[] value) => _Core.Items.Add(value);
		public void Add(long value) => this.Concat(value.ToString());
		public void Add(long[] value) => _Core.Items.Add(value);
		public void Add(double value) => this.Concat(value.ToString());
		public void Add(double[] value) => _Core.Items.Add(value);
		public void Add(string value) => _Core.Items.Add(value);
		public void Add(string[] value) => _Core.Items.Add(value);
		public void Add(Guid value) => this.Concat(string.Concat("'", value, "'"));
		public void Add(Guid[] value) => _Core.Items.Add(value);
		public void Add(DateTime value) => _Core.Items.Add(value);
		public void Add(DateTime[] value) => _Core.Items.Add(value);
		public void Add(Column value) => _Core.Items.Add(value);
		public void Add(Variable value) => _Core.Items.Add(value);
		public void Add(IElementizable value) => _Core.Items.Add(value);
		public void Add(ITable value) => _Core.Items.Add(value);
		public void Add(ElementCode value) => _Core.Items.Add(value);
		public void Add(Expression value, ColumnMap map) =>new Visitor(this, map).Visit(value);
		public void Add(object value) {
			if (!TypeWiseExecutor.Do(_TypeWise, value)) {
				throw new ApplicationException($"The type '{value.GetType().FullName}' can not be included in an expression.");
			}
		}
		public void AddValues<TypeOfCols>(TypeOfCols value) {
			TypeWiseCache<TypeOfCols>.AddValues(this, value);
		}

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

		public void AddColumns(IEnumerable<Column> columns, Action<Column> columnWise = null) {
			var first = true;
			foreach (var column in columns) {
				if (first) {
					first = false;
				} else {
					AddComma();
				}
				if (columnWise != null) {
					columnWise(column);
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
		/// <see cref="ICodeDbCommand"/>に渡して実行可能な形式にビルドする
		/// </summary>
		/// <returns>実行可能SQL</returns>
		public Commandable Build() {
			var work = new WorkingBuffer();
			this.Build(work);
			return new Commandable(work.Buffer.ToString(), work.Parameters);
		}

		/// <summary>
		/// 再帰的に指定のバッファへ展開する
		/// </summary>
		/// <param name="work">展開先バッファ</param>
		void Build(WorkingBuffer work) {
			foreach (var item in this.Items) {
				StringBuilder buffer;
				ElementCode ec;
				ITableDef tableDef;
				ITable table;
				Column column;

				if ((buffer = item as StringBuilder) != null) {
					work.Concat(buffer.ToString());
				} else if ((ec = item as ElementCode) != null) {
					ec.Build(work);
				} else if ((tableDef = item as ITableDef) != null) {
					work.Concat(tableDef.Name);
					work.Concat(work.GetTableAlias(tableDef));
				} else if ((table = item as ITable) != null) {
					var context = new ElementCode();
					context.Push();
					table.ToElementCode(context);
					context.Pop();
					context.Concat(work.GetTableAlias(table));
					context.Build(work);
				} else if ((column = item as Column) != null) {
					work.Concat(work.GetTableAlias(column.Table));
					work.Concat(".");
					work.Concat(column.Name);
				} else {
					work.Concat(work.GetParameterName(item));
				}
			}
		}

		/// <summary>
		/// 全<see cref="Variable"/>を列挙する
		/// </summary>
		public IEnumerable<Variable> FindVariables() {
			foreach (var item in _Core.Items) {
				var variable = item as Variable;
				if (variable is null) {
					var ec = item as ElementCode;
					if (ec != null) {
						foreach (var v in ec.FindVariables()) {
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
