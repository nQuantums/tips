using System;
using System.Collections.Generic;
using System.Text;

namespace CodeDb.Query {
	/// <summary>
	/// WHERE句のノード
	/// </summary>
	public interface IWhere : IQueryNode {
		/// <summary>
		/// 式
		/// </summary>
		ElementCode Expression { get; }
	}
}
