using System;
using System.Collections.Generic;
using System.Text;

namespace CodeDb.Query {
	/// <summary>
	/// INNER JOIN、LEFT JOIN、RIGHT JOIN句の基本機能を提供する
	/// </summary>
	public interface IJoin {
		/// <summary>
		/// 結合種類
		/// </summary>
		JoinType JoinType { get; }

		/// <summary>
		/// 結合するテーブル
		/// </summary>
		ITable Table { get; }

		/// <summary>
		/// 結合式
		/// </summary>
		ElementCode On { get; }
	}

	/// <summary>
	/// INNER JOIN、LEFT JOIN、RIGHT JOIN句の基本機能を提供し、<see cref="Columns"/>のプロパティにより列へのアクセスも提供する
	/// </summary>
	/// <typeparam name="TColumns">プロパティを列として扱うクラス</typeparam>
	public interface IJoin<TColumns> : IJoin {
		/// <summary>
		/// 結合するテーブル
		/// </summary>
		new ITable<TColumns> Table { get; }

		/// <summary>
		/// 列をプロパティとして持つオブジェクト
		/// </summary>
		TColumns Columns { get; }

		/// <summary>
		/// 列をプロパティとして持つオブジェクト
		/// </summary>
		TColumns _ { get; }
	}

	/// <summary>
	/// 結合種類
	/// </summary>
	public enum JoinType {
		Inner,
		Left,
		Right,
	}
}
