using System;
using System.Collections.Generic;
using System.Text;

namespace CodeDb.Query {
	/// <summary>
	/// INSERT INTO句の機能を提供する
	/// </summary>
	public interface IInsertInto : ISqlBuildable {
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
		ISelect Select { get; }
	}

	/// <summary>
	/// INSERT INTO句の機能を提供する
	/// </summary>
	/// <typeparam name="TypeOfColumns">プロパティを列として扱うクラス</typeparam>
	public interface IInsertInto<TypeOfColumns> : IInsertInto {
		/// <summary>
		/// 挿入先のテーブル
		/// </summary>
		new TableDef<TypeOfColumns> Table { get; }
	}
}
