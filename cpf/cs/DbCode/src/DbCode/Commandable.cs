using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DbCode.Internal;

namespace DbCode {
	/// <summary>
	/// <see cref="IDbCodeCommand"/>に設定するコマンドテキストとパラメータ
	/// </summary>
	public abstract class Commandable {
		/// <summary>
		/// コマンド文字列とパラメータ列の取得
		/// </summary>
		public abstract (string, IEnumerable<Parameter>) CommandTextAndParameters { get; }

		/// <summary>
		/// 個数固定のパラメータ列の取得
		/// </summary>
		public abstract Parameter[] FixedParameters { get; }

		/// <summary>
		/// 指定された引数オブジェクトの<see cref="Parameters"/>内でのインデックスを取得する
		/// </summary>
		/// <param name="arg">引数オブジェクト</param>
		/// <returns>見つかったらインデックスが返る</returns>
		public abstract int IndexOfArgument(Argument arg);

		/// <summary>
		/// 指定の<see cref="IDbCodeCommand"/>を使用してコマンドを実行する
		/// </summary>
		/// <param name="command">コマンド</param>
		/// <returns>影響を受けた行の数</returns>
		public abstract int Execute(IDbCodeCommand command);

		/// <summary>
		/// 指定の<see cref="IDbCodeCommand"/>を使用してコマンドを実行する
		/// </summary>
		/// <param name="command">コマンド</param>
		/// <returns>読み取り用オブジェクト</returns>
		public abstract IDbCodeDataReader ExecuteReader(IDbCodeCommand command);
	}

	/// <summary>
	/// <see cref="IDbCodeCommand"/>に設定するコマンドテキストとパラメータ、オブジェクト生成時に<see cref="ImmediatelyCommandable.CommandTextAndParameters"/>の内容が確定する
	/// </summary>
	public class ImmediatelyCommandable : Commandable {
		readonly string _CommandText;
		readonly Parameter[] _Parameters;

		/// <summary>
		/// コマンド文字列とパラメータ列の取得
		/// </summary>
		public override (string, IEnumerable<Parameter>) CommandTextAndParameters => (_CommandText, _Parameters);

		/// <summary>
		/// 個数固定のパラメータ列の取得
		/// </summary>
		public override Parameter[] FixedParameters => _Parameters;

		/// <summary>
		/// コンストラクタ、コマンド文字列とパラメーター列を指定して初期化する
		/// </summary>
		/// <param name="commandText">コマンド文字列</param>
		/// <param name="parameters">パラメータ列</param>
		public ImmediatelyCommandable(string commandText, IEnumerable<Parameter> parameters) {
			_CommandText = commandText;
			_Parameters = parameters.ToArray();
		}

		/// <summary>
		/// 指定された引数オブジェクトの<see cref="Parameters"/>内でのインデックスを取得する
		/// </summary>
		/// <param name="arg">引数オブジェクト</param>
		/// <returns>見つかったらインデックスが返る</returns>
		public override int IndexOfArgument(Argument arg) {
			var prms = _Parameters;
			for (int i = 0; i < prms.Length; i++) {
				if (object.ReferenceEquals(prms[i].Value, arg)) {
					return i;
				}
			}
			throw new ApplicationException();
		}

		/// <summary>
		/// 指定の<see cref="IDbCodeCommand"/>を使用してコマンドを実行する
		/// </summary>
		/// <param name="command">コマンド</param>
		/// <returns>影響を受けた行の数</returns>
		public override int Execute(IDbCodeCommand command) => command.ExecuteNonQuery(this);

		/// <summary>
		/// 指定の<see cref="IDbCodeCommand"/>を使用してコマンドを実行する
		/// </summary>
		/// <param name="command">コマンド</param>
		/// <returns>読み取り用オブジェクト</returns>
		public override IDbCodeDataReader ExecuteReader(IDbCodeCommand command) => command.ExecuteReader(this);
	}

	/// <summary>
	/// <see cref="IDbCodeCommand"/>に設定するコマンドテキストとパラメータ、オブジェクト生成時に<see cref="DelayedCommandable.CommandTextAndParameters"/>の内容は確定せず実際にアクセスを行った際に確定する
	/// </summary>
	public class DelayedCommandable : Commandable {
		readonly WorkingBuffer _Buffer;
		readonly Parameter[] _FixedParameters;

		/// <summary>
		/// コマンド文字列とパラメータ列の取得
		/// </summary>
		public override (string, IEnumerable<Parameter>) CommandTextAndParameters {
			get {
				var wb = new WorkingBuffer();
				_Buffer.Build(wb);
				return (wb.Build(), wb.Parameters);
			}
		}

		/// <summary>
		/// 個数固定のパラメータ列の取得
		/// </summary>
		public override Parameter[] FixedParameters => _FixedParameters;

		/// <summary>
		/// コンストラクタ、コマンド文字列を生成可能な作業用バッファを指定して初期化する
		/// </summary>
		public DelayedCommandable(WorkingBuffer buffer) {
			_Buffer = buffer;
			_FixedParameters = buffer.Parameters.ToArray();
		}

		/// <summary>
		/// 指定された引数オブジェクトの<see cref="Parameters"/>内でのインデックスを取得する
		/// </summary>
		/// <param name="arg">引数オブジェクト</param>
		/// <returns>見つかったらインデックスが返る</returns>
		public override int IndexOfArgument(Argument arg) {
			var prms = _Buffer.Parameters;
			for (int i = 0, n = prms.Count; i < n; i++) {
				if (object.ReferenceEquals(prms[i].Value, arg)) {
					return i;
				}
			}
			throw new ApplicationException();
		}

		/// <summary>
		/// 指定の<see cref="IDbCodeCommand"/>を使用してコマンドを実行する
		/// </summary>
		/// <param name="command">コマンド</param>
		/// <returns>影響を受けた行の数</returns>
		public override int Execute(IDbCodeCommand command) => command.ExecuteNonQuery(this);

		/// <summary>
		/// 指定の<see cref="IDbCodeCommand"/>を使用してコマンドを実行する
		/// </summary>
		/// <param name="command">コマンド</param>
		/// <returns>読み取り用オブジェクト</returns>
		public override IDbCodeDataReader ExecuteReader(IDbCodeCommand command) => command.ExecuteReader(this);
	}
}
