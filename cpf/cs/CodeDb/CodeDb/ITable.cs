using System;
using System.Collections.Generic;
using System.Text;

namespace CodeDb {
	/// <summary>
	/// 列をプロパティにバインドする機能を提供する
	/// </summary>
	/// <remarks>プロパティは自オブジェクトのもので無くて良い</remarks>
	public interface ITable : ISqlBuildable {
		/// <summary>
		/// プロパティに列定義をバインドして取得する、バインド済みなら取得のみ行われる
		/// </summary>
		/// <param name="propertyName">プロパティ名</param>
		/// <param name="name">DB上での列名</param>
		/// <param name="dbType">DB上での型</param>
		/// <param name="flags">列に対するオプションフラグ</param>
		/// <param name="source">列を生成する基となった式</param>
		/// <returns>列定義</returns>
		Column BindColumn(string propertyName, string name, IDbType dbType, ColumnFlags flags = 0, ElementCode source = null);

		/// <summary>
		/// テーブルが直接保持する列定義の取得
		/// </summary>
		ColumnMap ColumnMap { get; }

		/// <summary>
		/// エイリアス用にクローンを作成する
		/// </summary>
		/// <returns>クローン</returns>
		ITable AliasedClone();
	}

	/// <summary>
	/// 列をプロパティにバインドする機能を提供し、<see cref="Columns"/>のプロパティにより列へのアクセスも提供する
	/// </summary>
	/// <typeparam name="TColumns">プロパティを列として扱う<see cref="TableDef{TColumns}"/>のTColumnsに該当するクラス</typeparam>
	public interface ITable<TColumns> : ITable {
		/// <summary>
		/// 列をプロパティとして持つオブジェクト
		/// </summary>
		TColumns Columns { get; }

		/// <summary>
		/// 列をプロパティとして持つオブジェクト
		/// </summary>
		TColumns _ { get; }

		/// <summary>
		/// エイリアス用にクローンを作成する
		/// </summary>
		/// <returns>クローン</returns>
		new ITable<TColumns> AliasedClone();
	}
}
