using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using CodeDb.Query;
using CodeDb.Internal;

namespace CodeDb {
	/// <summary>
	/// インデックス定義
	/// </summary>
	public class IndexDef : IIndexDef {
		/// <summary>
		/// インデックス名
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// インデックスのオプションフラグ
		/// </summary>
		public IndexFlags Flags { get; private set; }

		/// <summary>
		/// インデックスを構成する列定義の配列
		/// </summary>
		public IEnumerable<IColumnDef> Columns { get; private set; }

		/// <summary>
		/// コンストラクタ、全要素を指定して初期化する
		/// </summary>
		/// <param name="name">インデックス名</param>
		/// <param name="flags">インデックスのオプションフラグ</param>
		/// <param name="columns">インデックスを構成する列定義の配列</param>
		public IndexDef(string name, IndexFlags flags, params IColumnDef[] columns) {
			this.Name = name;
			this.Flags = flags;
			this.Columns = columns;
		}

		/// <summary>
		/// コンストラクタ、全要素を指定して初期化する、インデックス名は自動生成される
		/// </summary>
		/// <param name="flags">インデックスのオプションフラグ</param>
		/// <param name="columns">インデックスを構成する列定義の配列</param>
		internal IndexDef(IndexFlags flags, params IColumnDef[] columns) : this($"idx_{Mediator.TableName}_{string.Join("_", from c in columns select c.Name)}", flags, columns) {
		}
	}

	/// <summary>
	/// インデックスのオプションフラグ
	/// </summary>
	[Flags]
	public enum IndexFlags {
		Gin = 1 << 0,
	}
}
