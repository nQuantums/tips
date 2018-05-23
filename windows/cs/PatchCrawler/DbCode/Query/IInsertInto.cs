using System;
using System.Collections.Generic;
using System.Text;
using DbCode.Defs;

namespace DbCode.Query {
	/// <summary>
	/// INSERT INTO句の機能を提供する
	/// </summary>
	public interface IInsertInto : IQueryNode {
		/// <summary>
		/// 挿入先のテーブル
		/// </summary>
		ITableDef Table { get; }

		/// <summary>
		/// 挿入列の指定
		/// </summary>
		ColumnMap ColumnMap { get; }

		/// <summary>
		/// 挿入する値
		/// </summary>
		ISelect ValueNode { get; }
	}

	/// <summary>
	/// INSERT INTO句の機能を提供する
	/// </summary>
	/// <typeparam name="TColumns">プロパティを列として扱う<see cref="TableDef{TColumns}"/>のTColumnsに該当するクラス</typeparam>
	public interface IInsertInto<TColumns> : IInsertInto {
		/// <summary>
		/// 挿入先のテーブル
		/// </summary>
		new TableDef<TColumns> Table { get; }
	}
}
