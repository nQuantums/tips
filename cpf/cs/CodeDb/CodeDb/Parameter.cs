using System;
using System.Collections.Generic;
using System.Text;

namespace CodeDb {
	/// <summary>
	/// SQLコマンドに渡すパラメータ
	/// </summary>
	public struct Parameter {
		/// <summary>
		/// パラメータ名
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// パラメータ値
		/// </summary>
		public object Value { get; private set; }

		/// <summary>
		/// コンストラクタ、パラメータ名と値を指定して初期化する
		/// </summary>
		/// <param name="name">パラメータ名</param>
		/// <param name="value">値</param>
		public Parameter(string name, object value) {
			this.Name = name;
			this.Value = value;
		}
	}
}
