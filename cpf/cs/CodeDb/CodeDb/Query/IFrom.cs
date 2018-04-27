using System;
using System.Collections.Generic;
using System.Text;

namespace CodeDb.Query {
	/// <summary>
	/// FROM句の機能を提供する
	/// </summary>
	public interface IFrom : ISqlBuildable {
		/// <summary>
		/// FROMに直接指定された取得元の<see cref="ITable"/>
		/// </summary>
		ITable Table { get; }

		/// <summary>
		/// SELECT句までの間に使用された全ての列
		/// </summary>
		ColumnMap SourceColumnMap { get; }

		/// <summary>
		/// SELECT句までの間に使用された全ての<see cref="ITable"/>
		/// </summary>
		HashSet<ITable> SourceTables { get; }

		/// <summary>
		/// INNER JOIN、LEFT JOIN、RIGHT JOIN句のリスト
		/// </summary>
		List<IJoin> Joins { get; }

		/// <summary>
		/// WHERE句の式
		/// </summary>
		ExpressionInProgress WhereExpression { get; }

		/// <summary>
		/// GROUP BY句の列一覧
		/// </summary>
		Column[] GroupByColumns { get; }

		/// <summary>
		/// ORDER BY句の列一覧
		/// </summary>
		Column[] OrderByColumns { get; }

		/// <summary>
		/// LIMIT句の値
		/// </summary>
		object LimitValue { get; }
	}

	/// <summary>
	/// FROM句の機能を提供する
	/// </summary>
	/// <typeparam name="TypeOfColumns">プロパティを列として扱うクラス</typeparam>
	public interface IFrom<TypeOfColumns> : IFrom {
		/// <summary>
		/// 列プロパティを持つオブジェクト
		/// </summary>
		TypeOfColumns Columns { get; }

		/// <summary>
		/// 列プロパティを持つオブジェクト
		/// </summary>
		TypeOfColumns _ { get; }
	}
}
