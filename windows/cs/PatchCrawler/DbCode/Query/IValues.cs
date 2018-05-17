using System;
using System.Collections.Generic;
using System.Text;

namespace DbCode.Query {
	/// <summary>
	/// VALUES句のノード機能を提供する
	/// </summary>
	public interface IValues : ITable, IQueryNode {
		/// <summary>
		/// 行を列挙
		/// </summary>
		IEnumerable<object> Rows { get; }
	}

	/// <summary>
	/// 列クラス型指定のVALUES句のノード機能を提供する
	/// </summary>
	/// <typeparam name="TColumns">プロパティを列として扱う<see cref="TableDef{TColumns}"/>のTColumnsに該当するクラス</typeparam>
	public interface IValues<TColumns> : IValues, ITable<TColumns> {
		/// <summary>
		/// <see cref="IDbCodeDataReader"/>から<typeparamref name="TColumns"/>を列挙するファンクション
		/// </summary>
		Func<IDbCodeDataReader, IEnumerable<TColumns>> Reader { get; }

		/// <summary>
		/// 行を列挙
		/// </summary>
		new IEnumerable<TColumns> Rows { get; }
	}
}
