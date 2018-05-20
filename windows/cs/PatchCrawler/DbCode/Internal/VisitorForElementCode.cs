using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DbCode;
using DbCode.Query;

using MethodNodeHandler = System.Action<DbCode.Internal.ElementCodeExpressionVisitor, DbCode.ElementCode, System.Linq.Expressions.MethodCallExpression>;

namespace DbCode.Internal {
	/// <summary>
	/// <see cref="Expression"/>のツリーを<see cref="ElementCode"/>へ展開するためのVisitor
	/// </summary>
	public class ElementCodeExpressionVisitor : ExpressionVisitor {
		/// <summary>
		/// メソッドコール置き換え用ハンドラ
		/// </summary>
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

		/// <summary>
		/// <see cref="IQueryNode"/>が検出された際に発生するイベント
		/// </summary>
		public event Action<IQueryNode> QueryNodeDetected;

		ElementCode _Code;
		ColumnMap _ColumnMap;

		public ElementCodeExpressionVisitor(ElementCode code, ColumnMap columnMap = null) {
			_Code = code;
			_ColumnMap = columnMap;
		}

		protected override Expression VisitUnary(UnaryExpression node) {
			_Code.Add(node.NodeType);
			_Code.Push();
			this.Visit(node.Operand);
			_Code.Pop();
			return node;
		}

		protected override Expression VisitBinary(BinaryExpression node) {
			var nodeType = node.NodeType;

			if (nodeType == ExpressionType.ArrayIndex) {
				_Code.Push();
				this.Visit(node.Left);
				_Code.Pop();
				_Code.Concat("[");
				this.Visit(Expression.Add(node.Right, Expression.Constant(1)));
				_Code.Concat("]");
			} else {
				var l = node.Left;
				var r = node.Right;

				var p = GetOperatorPrecedence(nodeType);
				if (p == GetOperatorPrecedence(l.NodeType)) {
					this.Visit(l);
				} else {
					_Code.Push();
					this.Visit(l);
					_Code.Pop();
				}

				if (nodeType == ExpressionType.Equal && r.NodeType == ExpressionType.Constant && Evaluate(r) == null) {
					_Code.Add(SqlKeyword.IsNull);
				} else if (nodeType == ExpressionType.NotEqual && r.NodeType == ExpressionType.Constant && Evaluate(r) == null) {
					_Code.Add(SqlKeyword.IsNotNull);
				} else {
					_Code.Add(nodeType);

					if (p == GetOperatorPrecedence(r.NodeType)) {
						this.Visit(r);
					} else {
						_Code.Push();
						this.Visit(r);
						_Code.Pop();
					}
				}
			}
			return node;
		}

		protected override Expression VisitConstant(ConstantExpression node) {
			_Code.Add(Evaluate(node));
			return node;
		}

		protected override Expression VisitMember(MemberExpression node) {
			var expression = node.Expression;
			var member = node.Member;
			var parentValue = expression != null ? Evaluate(expression) : null;

			if (parentValue != null) {
				// プロパティが列定義と結びつくものなら列定義を登録する
				if (member.MemberType == MemberTypes.Property) {
					var pi = (PropertyInfo)member;
					var colDef = _ColumnMap.TryGet(parentValue, pi);
					if (colDef != null) {
						_Code.Add(colDef);
						return node;
					}
				}
			}

			// Nullable の HasValue プロパティなら IS NOT NULL を登録する
			if (node.Member.Name == "HasValue") {
				_Code.Push();
				this.Visit(node.Expression);
				_Code.Pop();
				_Code.Add(SqlKeyword.IsNotNull);
				return node;
			}

			// ノードを評価し値を取得する
			var value = Evaluate(node);

			// ElementCode に展開できるものなら展開する
			if (ExpandIfPossible(value)) {
				return node;
			}

			// ※評価時のメンバアクセス結果の値が登録される
			_Code.Add(value);
			return node;
		}

		protected override Expression VisitMethodCall(MethodCallExpression node) {
			// 登録されたメソッド毎に処理を行う
			MethodNodeHandler handler;
			if (MethodProc.TryGetValue(node.Method, out handler)) {
				handler(this, _Code, node);
				return node;
			}

			// SqlMethod 属性が付与されたメソッドなら呼び出してもOK
			if (node.Method.GetCustomAttributes(typeof(SqlMethodAttribute)).Any()) {
				if (ExpandIfPossible(Evaluate(node))) {
					return node;
				}
			}

			throw new ApplicationException($"The method call '{node.Method.Name}' can not be included in an expression.");
		}

		protected override Expression VisitNewArray(NewArrayExpression node) {
			var nodeType = node.NodeType;
			var expressions = node.Expressions;
			if (nodeType == ExpressionType.NewArrayInit) {
				_Code.Concat("ARRAY[");
				for (int i = 0, n = expressions.Count; i < n; i++) {
					if (i != 0) {
						_Code.Concat(",");
					}
					_Code.Push();
					this.Visit(expressions[i]);
					_Code.Pop();
				}
				_Code.Concat("]");
			} else if (nodeType == ExpressionType.NewArrayBounds) {
				_Code.Concat("ARRAY[");
				for (int i = 0, n = expressions.Count; i < n; i++) {
					if (i != 0) {
						_Code.Concat(",");
					}
					_Code.Push();
					this.Visit(expressions[i]);
					_Code.Pop();
				}
				_Code.Concat("]");
			}
			return node;
		}

		protected override Expression VisitNew(NewExpression node) {
			_Code.Add(Evaluate(node));
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

		/// <summary>
		/// ElementCode に展開できるものなら展開する
		/// </summary>
		/// <param name="value">元の値</param>
		/// <returns>展開したら true</returns>
		bool ExpandIfPossible(object value) {
			if (value != null) {
				var queryNode = value as IQueryNode;
				if (queryNode != null) {
					var root = QueryNodeHelper.GetRootNode(queryNode);
					var d = this.QueryNodeDetected;
					if (d != null) {
						d(root);
					}

					_Code.Push();
					root.ToElementCode(_Code);
					_Code.Pop();

					return true;
				}
			}
			return false;
		}

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
}
