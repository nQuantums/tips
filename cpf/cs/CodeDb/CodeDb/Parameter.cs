using System;
using System.Collections.Generic;
using System.Text;

namespace CodeDb {
	/// <summary>
	/// SQLコマンドに渡すパラメータ
	/// </summary>
	public class Parameter {
		/// <summary>
		/// パラメータ名
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// パラメータ値
		/// </summary>
		public object Value { get; private set; }

		/// <summary>
		/// コマンドに対する引数として毎回変わる可能性があるものなら true となり<see cref="Variable.Value"/>の様な中身の値がパラメータとして使用される
		/// </summary>
		public bool IsArgument { get; private set; }

		/// <summary>
		/// コンストラクタ、パラメータ名と値を指定して初期化する
		/// </summary>
		/// <param name="name">パラメータ名</param>
		/// <param name="value">値</param>
		/// <param name="isArgument">引数として使用するものなら true</param>
		public Parameter(string name, object value, bool isArgument) {
			this.Name = name;
			this.Value = value;
			this.IsArgument = isArgument;
		}
	}
}
