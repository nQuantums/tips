using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;

namespace DbCode.Internal {
	class ParameterReplacer : ExpressionVisitor {
		Dictionary<Expression, object> _ParamMap;

		public ParameterReplacer(Dictionary<Expression, object> paramMap) {
			_ParamMap = paramMap;
		}

		protected override Expression VisitParameter(ParameterExpression node) {
			return Expression.Constant(_ParamMap[node]);
		}

		public static Expression Replace(Expression node, Dictionary<Expression, object> map) {
			var visitor = new ParameterReplacer(map);
			return visitor.Visit(node);
		}
	}
}
