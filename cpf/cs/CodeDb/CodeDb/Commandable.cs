using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeDb {
	/// <summary>
	/// <see cref="ICodeDbCommand"/>に設定するコマンドテキストとパラメータ
	/// </summary>
	public class Commandable {
		/// <summary>
		/// コマンド文字列
		/// </summary>
		public string CommandText { get; private set; }

		/// <summary>
		/// パラメーター
		/// </summary>
		public Parameter[] Parameters { get; private set; }

		/// <summary>
		/// コンストラクタ、コマンド文字列とパラメーター列を指定して初期化する
		/// </summary>
		/// <param name="commandText">コマンド文字列</param>
		/// <param name="parameters">パラメータ列</param>
		public Commandable(string commandText, IEnumerable<Parameter> parameters) {
			this.CommandText = commandText;
			this.Parameters = parameters.ToArray();
		}

		/// <summary>
		/// 指定された引数オブジェクトの<see cref="Parameters"/>内でのインデックスを取得する
		/// </summary>
		/// <param name="arg">引数オブジェクト</param>
		/// <returns>見つかったらインデックスが返る</returns>
		public int IndexOfArgument(Argument arg) {
			var prms = this.Parameters;
			for (int i = 0; i < prms.Length; i++) {
				if (object.ReferenceEquals(prms[i].Value, arg)) {
					return i;
				}
			}
			throw new ApplicationException();
		}

		/// <summary>
		/// 指定の<see cref="ICodeDbCommand"/>を使用してコマンドを実行する
		/// </summary>
		/// <param name="command">コマンド</param>
		/// <returns>影響を受けた行の数</returns>
		public int Execute(ICodeDbCommand command) => command.ExecuteNonQuery(this);

		/// <summary>
		/// 指定の<see cref="ICodeDbCommand"/>を使用してコマンドを実行する
		/// </summary>
		/// <param name="command">コマンド</param>
		/// <returns>読み取り用オブジェクト</returns>
		public ICodeDbDataReader ExecuteReader(ICodeDbCommand command) => command.ExecuteReader(this);
	}
}
