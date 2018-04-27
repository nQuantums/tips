using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using CodeDb.Query;

namespace CodeDb {
	public static class Sql {
		public static From<TypeOfColumns> From<TypeOfColumns>(TableDef<TypeOfColumns> table) {
			return new From<TypeOfColumns>(table);
		}

		public static From<TypeOfColumns> From<TypeOfColumns>(ISelect<TypeOfColumns> select) {
			return new From<TypeOfColumns>(select);
		}

		public static InsertInto<TypeOfColumns, TypeOfColumnsOrder> InsertInto<TypeOfColumns, TypeOfColumnsOrder>(TableDef<TypeOfColumns> table, Expression<Func<TypeOfColumns, TypeOfColumnsOrder>> columnsExpression) {
			return new InsertInto<TypeOfColumns, TypeOfColumnsOrder>(table, columnsExpression);
		}

		public static bool Like(this string text, string pattern) {
			return default(bool);
		}

		public static bool Exists(ISelect select) {
			return default(bool);
		}

		public static bool NotExists(ISelect select) {
			return default(bool);
		}
	}
}
