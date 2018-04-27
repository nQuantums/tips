using System;
using System.Collections.Generic;
using System.Text;

namespace CodeDb {
	/// <summary>
	/// DB接続環境下にあるオブジェクトに付与されるインターフェース
	/// </summary>
	public interface IInDbEnvironment {
		/// <summary>
		/// DB接続環境
		/// </summary>
		DbEnvironment Environment { get; }
	}
}
