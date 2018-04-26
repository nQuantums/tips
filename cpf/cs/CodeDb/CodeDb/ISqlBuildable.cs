using System;
using System.Collections.Generic;
using System.Text;

namespace CodeDb {
	/// <summary>
	/// SQL文にコンパイル可能なオブジェクトに付与するインターフェース
	/// </summary>
	public interface ISqlBuildable {
		/// <summary>
		/// SQL文を生成する
		/// </summary>
		/// <param name="context">生成先のコンテキスト</param>
		void BuildSql(ExpressionInProgress context);
	}
}
