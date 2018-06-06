using System;
using System.Collections.Generic;
using System.Text;

namespace DbCode.Query {
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
