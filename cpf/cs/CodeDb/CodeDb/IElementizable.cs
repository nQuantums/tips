using System;
using System.Collections.Generic;
using System.Text;

namespace CodeDb {
	/// <summary>
	/// ElementCode にコード化可能なオブジェクトに付与するインターフェース
	/// </summary>
	public interface IElementizable {
		/// <summary>
		/// SQL文を生成する
		/// </summary>
		/// <param name="context">生成先のコンテキスト</param>
		void ToElementCode(ElementCode context);
	}
}
