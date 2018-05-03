using System;
using System.Collections.Generic;
using System.Text;

namespace CodeDb.Query {
	/// <summary>
	/// FROM句の機能を提供する
	/// </summary>
	public interface IFrom : IQueryNode {
		/// <summary>
		/// FROMに直接指定された取得元の<see cref="ITable"/>
		/// </summary>
		ITable Table { get; }

		/// <summary>
		/// INNER JOIN、LEFT JOIN、RIGHT JOIN句のノード列
		/// </summary>
		IEnumerable<IJoin> JoinNodes { get; }

		/// <summary>
		/// WHERE句のノード
		/// </summary>
		IWhere WhereNode { get; }

		/// <summary>
		/// GROUP BY句のノード
		/// </summary>
		IGroupBy GroupByNode { get; }

		/// <summary>
		/// ORDER BY句のノード
		/// </summary>
		IOrderBy OrderByNode { get; }

		/// <summary>
		/// LIMIT句のノード
		/// </summary>
		ILimit LimitNode { get; }

		/// <summary>
		/// SELECT句のノード
		/// </summary>
		ISelect SelectNode { get; }
	}

	/// <summary>
	/// FROM句の機能を提供する
	/// </summary>
	/// <typeparam name="TColumns">プロパティを列として扱う<see cref="TableDef{TColumns}"/>のTColumnsに該当するクラス</typeparam>
	public interface IFrom<TColumns> : IFrom {
		/// <summary>
		/// 列プロパティを持つオブジェクト
		/// </summary>
		TColumns Columns { get; }

		/// <summary>
		/// 列プロパティを持つオブジェクト
		/// </summary>
		TColumns _ { get; }
	}
}
