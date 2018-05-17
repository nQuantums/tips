using System;
using System.Collections.Generic;
using System.Text;

namespace DbCode.Query {
	/// <summary>
	/// LIMIT句のノードの機能を提供する
	/// </summary>
	public interface ILimit : IQueryNode {
		/// <summary>
		/// 制限値
		/// </summary>
		object Value { get; }
	}
}
