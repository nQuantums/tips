using System;
using System.Collections.Generic;
using System.Text;

namespace CodeDb {
	/// <summary>
	/// SQLのコマンドに設定するテキスト
	/// </summary>
	public class SqlProgram {
		/// <summary>
		/// コマンド文字列
		/// </summary>
		public string CommandText { get; private set; }

		/// <summary>
		/// パラメーター
		/// </summary>
		public List<object> Parameters { get; private set; }

		/// <summary>
		/// パラメーター名と値の列
		/// </summary>
		public IEnumerable<Parameter> ParameterNameAndValues {
			get {
				var parameters = this.Parameters;
				for (int i = 0, n = parameters.Count; i < n; i++) {
					yield return new Parameter("@p" + i, parameters[i]);
				}
			}
		}

		/// <summary>
		/// コンストラクタ、コマンド文字列とパラメーター列を指定して初期化する
		/// </summary>
		/// <param name="commandText">コマンド文字列</param>
		/// <param name="parameters">パラメータ列</param>
		public SqlProgram(string commandText, List<object> parameters) {
			this.CommandText = commandText;
			this.Parameters = parameters;
		}
	}
}
