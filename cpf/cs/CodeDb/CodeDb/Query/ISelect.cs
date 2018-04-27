using System;
using System.Collections.Generic;
using System.Text;

namespace CodeDb.Query {
	/// <summary>
	/// SELECT式
	/// </summary>
	public interface ISelect : ITable, IInDbEnvironment, ISqlBuildable {
		/// <summary>
		/// このテーブルを構成するのに必要な全ての列定義を取得する
		/// </summary>
		ColumnMap SourceColumnMap { get; }
	}

	/// <summary>
	/// 列クラス型指定のSELECT式
	/// </summary>
	/// <typeparam name="TColumns">プロパティを列として扱うクラス</typeparam>
	public interface ISelect<TColumns> : ISelect, ITable<TColumns> {
	}
}
