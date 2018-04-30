using System;
using System.Collections.Generic;
using System.Text;

namespace CodeDb {
	/// <summary>
	/// SQLのコマンドに設定するテキストとパラメータ
	/// </summary>
	public class Commandable {
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
		public Commandable(string commandText, List<object> parameters) {
			this.CommandText = commandText;
			this.Parameters = parameters;
		}

		/// <summary>
		/// 指定の<see cref="ICodeDbCommand"/>を使用してコマンドを実行する
		/// </summary>
		/// <param name="command">コマンド</param>
		public void Execute(ICodeDbCommand command) {
			command.Apply(this);
			command.ExecuteNonQuery();
		}
	}

	/// <summary>
	/// SQLのコマンドに設定するテキストとパラメータ、指定型の値を列挙する機能も提供する
	/// 指定型の値を列挙するコマンド
	/// </summary>
	/// <typeparam name="T">列挙する型</typeparam>
	public class Commandable<T> {
		/// <summary>
		/// 実体
		/// </summary>
		public Commandable Core { get; private set; }

		/// <summary>
		/// コマンド文字列
		/// </summary>
		public string CommandText => this.Core.CommandText;

		/// <summary>
		/// パラメーター
		/// </summary>
		public List<object> Parameters => this.Core.Parameters;

		/// <summary>
		/// コンストラクタ、実体となる<see cref="Commandable"/>を指定して初期化する
		/// </summary>
		/// <param name="core"></param>
		public Commandable(Commandable core) {
			this.Core = core;
		}

		/// <summary>
		/// 指定の<see cref="ICodeDbCommand"/>を使用してコマンドを実行しレコード読み取りオブジェクトを取得する
		/// </summary>
		/// <param name="command">コマンド</param>
		/// <returns>レコード読み取りオブジェクト</returns>
		public RecordReader<T> Execute(ICodeDbCommand command) {
			command.Apply(this.Core);
			return new RecordReader<T>(command.ExecuteReader());
		}
	}
}
