using System;
using System.Collections.Generic;
using System.Text;
using DbCode.Query;

namespace DbCode.Internal {
	public class Mediator {
		[ThreadStatic]
		public static ITable Table;
		[ThreadStatic]
		public static string TableName;
		[ThreadStatic]
		public static Column Column;
		[ThreadStatic]
		public static ColumnFlags ColumnFlags;
		[ThreadStatic]
		public static string PropertyName;

		/// <summary>
		/// 列定義を取得する、指定された関数内で<see cref="Column"/>が設定される必要がある
		/// </summary>
		/// <param name="getter">プロパティを取得する処理</param>
		/// <returns>列定義</returns>
		public static Column GetFrom<TColumns>(Func<TColumns, object> getter, TColumns columns) {
			getter(columns);
			var col = Mediator.Column;
			if (col is null) {
				throw new ApplicationException($"The function '{getter}' can not acquire column definitions because {nameof(Column)}'s column definition method is not called.");
			}
			Mediator.Column = null;
			return col;
		}
	}
}
