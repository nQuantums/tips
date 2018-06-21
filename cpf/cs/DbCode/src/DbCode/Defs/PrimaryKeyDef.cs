using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using DbCode.Query;
using DbCode.Internal;

namespace DbCode.Defs {
	/// <summary>
	/// プライマリキー定義
	/// </summary>
	public class PrimaryKeyDef : IPrimaryKeyDef {
		/// <summary>
		/// プライマリキー名
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// プライマリキーとなる列の配列
		/// </summary>
		public IEnumerable<IColumnDef> Columns { get; private set; }

		/// <summary>
		/// コンストラクタ、全要素を指定して初期化する
		/// </summary>
		/// <param name="name">プライマリキー名</param>
		/// <param name="columns">プライマリキーとなる列の配列</param>
		public PrimaryKeyDef(string name, params IColumnDef[] columns) {
			this.Name = name;
			this.Columns = columns;
		}

		/// <summary>
		/// コンストラクタ、全要素を指定して初期化する、プライマリキー名は自動生成される
		/// </summary>
		/// <param name="columns">プライマリキーとなる列の配列</param>
		internal PrimaryKeyDef(params IColumnDef[] columns) : this($"pkey_{Mediator.TableName}_{string.Join("_", from c in columns select c.Name)}", columns) {
		}
	}
}
