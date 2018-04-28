using System;
using System.Collections.Generic;
using System.Text;

namespace CodeDb.Query {
	/// <summary>
	/// EXISTS (SELECT * FROM table WHERE a=1)の様に置き換わる
	/// </summary>
	interface IExists : IQueryNode {
		/// <summary>
		/// 存在確認対象のSELECTノード
		/// </summary>
		ISelect Select { get; }
	}
}
