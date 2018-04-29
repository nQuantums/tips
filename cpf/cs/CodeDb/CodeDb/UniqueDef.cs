using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using CodeDb.Internal;

namespace CodeDb {
	/// <summary>
	/// ユニーク制約定義
	/// </summary>
	public class UniqueDef : IUniqueDef {
		/// <summary>
		/// 制約名
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// ユニーク制約を構成する列定義の配列
		/// </summary>
		public IEnumerable<IColumnDef> Columns { get; private set; }

		/// <summary>
		/// コンストラクタ、全要素を指定して初期化する
		/// </summary>
		/// <param name="name">制約名</param>
		/// <param name="columns">ユニーク制約を構成する列定義の配列</param>
		public UniqueDef(string name, params IColumnDef[] columns) {
			this.Name = name;
			this.Columns = columns;
		}

		/// <summary>
		/// コンストラクタ、全要素を指定して初期化する、インデックス名は自動生成される
		/// </summary>
		/// <param name="columns">ユニーク制約を構成する列定義の配列</param>
		internal UniqueDef(params IColumnDef[] columns) : this($"uniq_{Mediator.TableName}_{string.Join("_", from c in columns select c.Name)}", columns) {
		}
	}
}
