﻿using System;
using System.Collections.Generic;
using System.Text;

namespace CodeDb.Query {
	/// <summary>
	/// INNER JOIN、LEFT JOIN、RIGHT JOIN句の基本機能を提供し、<see cref="Columns"/>のプロパティにより列へのアクセスも提供する
	/// </summary>
	/// <typeparam name="TColumns">プロパティを列として扱うクラス</typeparam>
	public class Join<TColumns> : IJoin<TColumns> {
		#region プロパティ
		/// <summary>
		/// 結合種類
		/// </summary>
		public JoinType JoinType { get; private set; }

		/// <summary>
		/// 結合するテーブル
		/// </summary>
		public ITable<TColumns> Table { get; private set; }
		ITable IJoin.Table => this.Table;

		/// <summary>
		/// 結合式
		/// </summary>
		public ElementCode On { get; private set; }

		/// <summary>
		/// 列をプロパティとして持つオブジェクト
		/// </summary>
		public TColumns Columns { get; private set; }

		/// <summary>
		/// 列をプロパティとして持つオブジェクト
		/// </summary>
		public TColumns _ => this.Columns;
		#endregion

		#region コンストラクタ
		/// <summary>
		/// コンストラクタ、結合種類、テーブル、結合式を指定して初期化する
		/// </summary>
		/// <param name="joinType">結合種類</param>
		/// <param name="table">結合するテーブル</param>
		/// <param name="on">結合式</param>
		public Join(JoinType joinType, ITable<TColumns> table, ElementCode on) {
			this.JoinType = joinType;
			this.Table = table;
			this.On = on;
			this.Columns = table.Columns;
		}
		#endregion
	}
}