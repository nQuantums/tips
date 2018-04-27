using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using CodeDb.Query;

namespace CodeDb {
	public static class Sql {
		public static From<TColumns> From<TColumns>(TableDef<TColumns> table) {
			return new From<TColumns>(table);
		}

		public static From<TColumns> From<TColumns>(ISelect<TColumns> select) {
			return new From<TColumns>(select);
		}

		public static InsertInto<TColumns, TColumnsOrder> InsertInto<TColumns, TColumnsOrder>(TableDef<TColumns> table, Expression<Func<TColumns, TColumnsOrder>> columnsExpression) {
			return new InsertInto<TColumns, TColumnsOrder>(table, columnsExpression);
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
