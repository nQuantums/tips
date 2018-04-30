 
using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeDb {
	/// <summary>
	/// 指定された引数を<see cref="Commandable"/>に設定しコマンドを実行する
	/// </summary>
	public class ActionCmd {
		/// <summary>
		/// コマンドテキストとパラメータ
		/// </summary>
		public Commandable Commandable { get; private set; }

		/// <summary>
		/// コンストラクタ、実体となる<see cref="CodeDb.Commandable"/>を指定して初期化する
		/// </summary>
		/// <param name="commandable">実体</param>
		public ActionCmd(Commandable commandable) {
			this.Commandable = commandable;
		}

		/// <summary>
		/// 指定の<see cref="ICodeDbCommand"/>を使用してコマンドを実行しレコード読み取りオブジェクトを取得する
		/// </summary>
		/// <param name="command">コマンド</param>
		/// <returns>影響を受けた行の数</returns>
		public int Execute(ICodeDbCommand command) {
			return this.Commandable.Execute(command);
		}
	}

	/// <summary>
	/// 指定された引数を<see cref="Commandable"/>に設定しコマンドを実行する
	/// </summary>
	/// <typeparam name="T1">引数1の型</typeparam>
	public class ActionCmd<T1> {
		/// <summary>
		/// コマンドテキストとパラメータ
		/// </summary>
		public Commandable Commandable { get; private set; }

		/// <summary>
		/// 引数を<see cref="Parameter[]"/>に設定する
		/// </summary>
		public Action<Parameter[], T1> ArgSetter { get; private set; }

		/// <summary>
		/// コンストラクタ、実体となる<see cref="CodeDb.Commandable"/>を指定して初期化する
		/// </summary>
		/// <param name="commandable">実体</param>
		/// <param name="argSetter">引数を<see cref="Commandable"/>に設定する</param>
		public ActionCmd(Commandable commandable, Action<Parameter[], T1> argSetter) {
			this.ArgSetter = argSetter;
			this.Commandable = commandable;
		}

		/// <summary>
		/// 指定の<see cref="ICodeDbCommand"/>を使用してコマンドを実行しレコード読み取りオブジェクトを取得する
		/// </summary>
		/// <param name="command">コマンド</param>
		/// <param name="arg1">引数1</param>
		/// <returns>影響を受けた行の数</returns>
		public int Execute(ICodeDbCommand command, T1 arg1) {
			this.ArgSetter(this.Commandable.Parameters, arg1);
			return this.Commandable.Execute(command);
		}
	}

	/// <summary>
	/// 指定された引数を<see cref="Commandable"/>に設定しコマンドを実行する
	/// </summary>
	/// <typeparam name="T1">引数1の型</typeparam>
	/// <typeparam name="T2">引数2の型</typeparam>
	public class ActionCmd<T1, T2> {
		/// <summary>
		/// コマンドテキストとパラメータ
		/// </summary>
		public Commandable Commandable { get; private set; }

		/// <summary>
		/// 引数を<see cref="Parameter[]"/>に設定する
		/// </summary>
		public Action<Parameter[], T1, T2> ArgSetter { get; private set; }

		/// <summary>
		/// コンストラクタ、実体となる<see cref="CodeDb.Commandable"/>を指定して初期化する
		/// </summary>
		/// <param name="commandable">実体</param>
		/// <param name="argSetter">引数を<see cref="Commandable"/>に設定する</param>
		public ActionCmd(Commandable commandable, Action<Parameter[], T1, T2> argSetter) {
			this.ArgSetter = argSetter;
			this.Commandable = commandable;
		}

		/// <summary>
		/// 指定の<see cref="ICodeDbCommand"/>を使用してコマンドを実行しレコード読み取りオブジェクトを取得する
		/// </summary>
		/// <param name="command">コマンド</param>
		/// <param name="arg1">引数1</param>
		/// <param name="arg2">引数2</param>
		/// <returns>影響を受けた行の数</returns>
		public int Execute(ICodeDbCommand command, T1 arg1, T2 arg2) {
			this.ArgSetter(this.Commandable.Parameters, arg1, arg2);
			return this.Commandable.Execute(command);
		}
	}

	/// <summary>
	/// 指定された引数を<see cref="Commandable"/>に設定しコマンドを実行する
	/// </summary>
	/// <typeparam name="T1">引数1の型</typeparam>
	/// <typeparam name="T2">引数2の型</typeparam>
	/// <typeparam name="T3">引数3の型</typeparam>
	public class ActionCmd<T1, T2, T3> {
		/// <summary>
		/// コマンドテキストとパラメータ
		/// </summary>
		public Commandable Commandable { get; private set; }

		/// <summary>
		/// 引数を<see cref="Parameter[]"/>に設定する
		/// </summary>
		public Action<Parameter[], T1, T2, T3> ArgSetter { get; private set; }

		/// <summary>
		/// コンストラクタ、実体となる<see cref="CodeDb.Commandable"/>を指定して初期化する
		/// </summary>
		/// <param name="commandable">実体</param>
		/// <param name="argSetter">引数を<see cref="Commandable"/>に設定する</param>
		public ActionCmd(Commandable commandable, Action<Parameter[], T1, T2, T3> argSetter) {
			this.ArgSetter = argSetter;
			this.Commandable = commandable;
		}

		/// <summary>
		/// 指定の<see cref="ICodeDbCommand"/>を使用してコマンドを実行しレコード読み取りオブジェクトを取得する
		/// </summary>
		/// <param name="command">コマンド</param>
		/// <param name="arg1">引数1</param>
		/// <param name="arg2">引数2</param>
		/// <param name="arg3">引数3</param>
		/// <returns>影響を受けた行の数</returns>
		public int Execute(ICodeDbCommand command, T1 arg1, T2 arg2, T3 arg3) {
			this.ArgSetter(this.Commandable.Parameters, arg1, arg2, arg3);
			return this.Commandable.Execute(command);
		}
	}

	/// <summary>
	/// 指定された引数を<see cref="Commandable"/>に設定しコマンドを実行する
	/// </summary>
	/// <typeparam name="T1">引数1の型</typeparam>
	/// <typeparam name="T2">引数2の型</typeparam>
	/// <typeparam name="T3">引数3の型</typeparam>
	/// <typeparam name="T4">引数4の型</typeparam>
	public class ActionCmd<T1, T2, T3, T4> {
		/// <summary>
		/// コマンドテキストとパラメータ
		/// </summary>
		public Commandable Commandable { get; private set; }

		/// <summary>
		/// 引数を<see cref="Parameter[]"/>に設定する
		/// </summary>
		public Action<Parameter[], T1, T2, T3, T4> ArgSetter { get; private set; }

		/// <summary>
		/// コンストラクタ、実体となる<see cref="CodeDb.Commandable"/>を指定して初期化する
		/// </summary>
		/// <param name="commandable">実体</param>
		/// <param name="argSetter">引数を<see cref="Commandable"/>に設定する</param>
		public ActionCmd(Commandable commandable, Action<Parameter[], T1, T2, T3, T4> argSetter) {
			this.ArgSetter = argSetter;
			this.Commandable = commandable;
		}

		/// <summary>
		/// 指定の<see cref="ICodeDbCommand"/>を使用してコマンドを実行しレコード読み取りオブジェクトを取得する
		/// </summary>
		/// <param name="command">コマンド</param>
		/// <param name="arg1">引数1</param>
		/// <param name="arg2">引数2</param>
		/// <param name="arg3">引数3</param>
		/// <param name="arg4">引数4</param>
		/// <returns>影響を受けた行の数</returns>
		public int Execute(ICodeDbCommand command, T1 arg1, T2 arg2, T3 arg3, T4 arg4) {
			this.ArgSetter(this.Commandable.Parameters, arg1, arg2, arg3, arg4);
			return this.Commandable.Execute(command);
		}
	}

	/// <summary>
	/// 指定された引数を<see cref="Commandable"/>に設定しコマンドを実行する
	/// </summary>
	/// <typeparam name="T1">引数1の型</typeparam>
	/// <typeparam name="T2">引数2の型</typeparam>
	/// <typeparam name="T3">引数3の型</typeparam>
	/// <typeparam name="T4">引数4の型</typeparam>
	/// <typeparam name="T5">引数5の型</typeparam>
	public class ActionCmd<T1, T2, T3, T4, T5> {
		/// <summary>
		/// コマンドテキストとパラメータ
		/// </summary>
		public Commandable Commandable { get; private set; }

		/// <summary>
		/// 引数を<see cref="Parameter[]"/>に設定する
		/// </summary>
		public Action<Parameter[], T1, T2, T3, T4, T5> ArgSetter { get; private set; }

		/// <summary>
		/// コンストラクタ、実体となる<see cref="CodeDb.Commandable"/>を指定して初期化する
		/// </summary>
		/// <param name="commandable">実体</param>
		/// <param name="argSetter">引数を<see cref="Commandable"/>に設定する</param>
		public ActionCmd(Commandable commandable, Action<Parameter[], T1, T2, T3, T4, T5> argSetter) {
			this.ArgSetter = argSetter;
			this.Commandable = commandable;
		}

		/// <summary>
		/// 指定の<see cref="ICodeDbCommand"/>を使用してコマンドを実行しレコード読み取りオブジェクトを取得する
		/// </summary>
		/// <param name="command">コマンド</param>
		/// <param name="arg1">引数1</param>
		/// <param name="arg2">引数2</param>
		/// <param name="arg3">引数3</param>
		/// <param name="arg4">引数4</param>
		/// <param name="arg5">引数5</param>
		/// <returns>影響を受けた行の数</returns>
		public int Execute(ICodeDbCommand command, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) {
			this.ArgSetter(this.Commandable.Parameters, arg1, arg2, arg3, arg4, arg5);
			return this.Commandable.Execute(command);
		}
	}

	/// <summary>
	/// 指定された引数を<see cref="Commandable"/>に設定しコマンドを実行する
	/// </summary>
	/// <typeparam name="T1">引数1の型</typeparam>
	/// <typeparam name="T2">引数2の型</typeparam>
	/// <typeparam name="T3">引数3の型</typeparam>
	/// <typeparam name="T4">引数4の型</typeparam>
	/// <typeparam name="T5">引数5の型</typeparam>
	/// <typeparam name="T6">引数6の型</typeparam>
	public class ActionCmd<T1, T2, T3, T4, T5, T6> {
		/// <summary>
		/// コマンドテキストとパラメータ
		/// </summary>
		public Commandable Commandable { get; private set; }

		/// <summary>
		/// 引数を<see cref="Parameter[]"/>に設定する
		/// </summary>
		public Action<Parameter[], T1, T2, T3, T4, T5, T6> ArgSetter { get; private set; }

		/// <summary>
		/// コンストラクタ、実体となる<see cref="CodeDb.Commandable"/>を指定して初期化する
		/// </summary>
		/// <param name="commandable">実体</param>
		/// <param name="argSetter">引数を<see cref="Commandable"/>に設定する</param>
		public ActionCmd(Commandable commandable, Action<Parameter[], T1, T2, T3, T4, T5, T6> argSetter) {
			this.ArgSetter = argSetter;
			this.Commandable = commandable;
		}

		/// <summary>
		/// 指定の<see cref="ICodeDbCommand"/>を使用してコマンドを実行しレコード読み取りオブジェクトを取得する
		/// </summary>
		/// <param name="command">コマンド</param>
		/// <param name="arg1">引数1</param>
		/// <param name="arg2">引数2</param>
		/// <param name="arg3">引数3</param>
		/// <param name="arg4">引数4</param>
		/// <param name="arg5">引数5</param>
		/// <param name="arg6">引数6</param>
		/// <returns>影響を受けた行の数</returns>
		public int Execute(ICodeDbCommand command, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) {
			this.ArgSetter(this.Commandable.Parameters, arg1, arg2, arg3, arg4, arg5, arg6);
			return this.Commandable.Execute(command);
		}
	}

	/// <summary>
	/// 指定された引数を<see cref="Commandable"/>に設定しコマンドを実行する
	/// </summary>
	/// <typeparam name="T1">引数1の型</typeparam>
	/// <typeparam name="T2">引数2の型</typeparam>
	/// <typeparam name="T3">引数3の型</typeparam>
	/// <typeparam name="T4">引数4の型</typeparam>
	/// <typeparam name="T5">引数5の型</typeparam>
	/// <typeparam name="T6">引数6の型</typeparam>
	/// <typeparam name="T7">引数7の型</typeparam>
	public class ActionCmd<T1, T2, T3, T4, T5, T6, T7> {
		/// <summary>
		/// コマンドテキストとパラメータ
		/// </summary>
		public Commandable Commandable { get; private set; }

		/// <summary>
		/// 引数を<see cref="Parameter[]"/>に設定する
		/// </summary>
		public Action<Parameter[], T1, T2, T3, T4, T5, T6, T7> ArgSetter { get; private set; }

		/// <summary>
		/// コンストラクタ、実体となる<see cref="CodeDb.Commandable"/>を指定して初期化する
		/// </summary>
		/// <param name="commandable">実体</param>
		/// <param name="argSetter">引数を<see cref="Commandable"/>に設定する</param>
		public ActionCmd(Commandable commandable, Action<Parameter[], T1, T2, T3, T4, T5, T6, T7> argSetter) {
			this.ArgSetter = argSetter;
			this.Commandable = commandable;
		}

		/// <summary>
		/// 指定の<see cref="ICodeDbCommand"/>を使用してコマンドを実行しレコード読み取りオブジェクトを取得する
		/// </summary>
		/// <param name="command">コマンド</param>
		/// <param name="arg1">引数1</param>
		/// <param name="arg2">引数2</param>
		/// <param name="arg3">引数3</param>
		/// <param name="arg4">引数4</param>
		/// <param name="arg5">引数5</param>
		/// <param name="arg6">引数6</param>
		/// <param name="arg7">引数7</param>
		/// <returns>影響を受けた行の数</returns>
		public int Execute(ICodeDbCommand command, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7) {
			this.ArgSetter(this.Commandable.Parameters, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
			return this.Commandable.Execute(command);
		}
	}

	/// <summary>
	/// 指定された引数を<see cref="Commandable"/>に設定しコマンドを実行する
	/// </summary>
	/// <typeparam name="T1">引数1の型</typeparam>
	/// <typeparam name="T2">引数2の型</typeparam>
	/// <typeparam name="T3">引数3の型</typeparam>
	/// <typeparam name="T4">引数4の型</typeparam>
	/// <typeparam name="T5">引数5の型</typeparam>
	/// <typeparam name="T6">引数6の型</typeparam>
	/// <typeparam name="T7">引数7の型</typeparam>
	/// <typeparam name="T8">引数8の型</typeparam>
	public class ActionCmd<T1, T2, T3, T4, T5, T6, T7, T8> {
		/// <summary>
		/// コマンドテキストとパラメータ
		/// </summary>
		public Commandable Commandable { get; private set; }

		/// <summary>
		/// 引数を<see cref="Parameter[]"/>に設定する
		/// </summary>
		public Action<Parameter[], T1, T2, T3, T4, T5, T6, T7, T8> ArgSetter { get; private set; }

		/// <summary>
		/// コンストラクタ、実体となる<see cref="CodeDb.Commandable"/>を指定して初期化する
		/// </summary>
		/// <param name="commandable">実体</param>
		/// <param name="argSetter">引数を<see cref="Commandable"/>に設定する</param>
		public ActionCmd(Commandable commandable, Action<Parameter[], T1, T2, T3, T4, T5, T6, T7, T8> argSetter) {
			this.ArgSetter = argSetter;
			this.Commandable = commandable;
		}

		/// <summary>
		/// 指定の<see cref="ICodeDbCommand"/>を使用してコマンドを実行しレコード読み取りオブジェクトを取得する
		/// </summary>
		/// <param name="command">コマンド</param>
		/// <param name="arg1">引数1</param>
		/// <param name="arg2">引数2</param>
		/// <param name="arg3">引数3</param>
		/// <param name="arg4">引数4</param>
		/// <param name="arg5">引数5</param>
		/// <param name="arg6">引数6</param>
		/// <param name="arg7">引数7</param>
		/// <param name="arg8">引数8</param>
		/// <returns>影響を受けた行の数</returns>
		public int Execute(ICodeDbCommand command, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8) {
			this.ArgSetter(this.Commandable.Parameters, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
			return this.Commandable.Execute(command);
		}
	}

	/// <summary>
	/// 指定された引数を<see cref="Commandable"/>に設定しコマンドを実行する
	/// </summary>
	/// <typeparam name="T1">引数1の型</typeparam>
	/// <typeparam name="T2">引数2の型</typeparam>
	/// <typeparam name="T3">引数3の型</typeparam>
	/// <typeparam name="T4">引数4の型</typeparam>
	/// <typeparam name="T5">引数5の型</typeparam>
	/// <typeparam name="T6">引数6の型</typeparam>
	/// <typeparam name="T7">引数7の型</typeparam>
	/// <typeparam name="T8">引数8の型</typeparam>
	/// <typeparam name="T9">引数9の型</typeparam>
	public class ActionCmd<T1, T2, T3, T4, T5, T6, T7, T8, T9> {
		/// <summary>
		/// コマンドテキストとパラメータ
		/// </summary>
		public Commandable Commandable { get; private set; }

		/// <summary>
		/// 引数を<see cref="Parameter[]"/>に設定する
		/// </summary>
		public Action<Parameter[], T1, T2, T3, T4, T5, T6, T7, T8, T9> ArgSetter { get; private set; }

		/// <summary>
		/// コンストラクタ、実体となる<see cref="CodeDb.Commandable"/>を指定して初期化する
		/// </summary>
		/// <param name="commandable">実体</param>
		/// <param name="argSetter">引数を<see cref="Commandable"/>に設定する</param>
		public ActionCmd(Commandable commandable, Action<Parameter[], T1, T2, T3, T4, T5, T6, T7, T8, T9> argSetter) {
			this.ArgSetter = argSetter;
			this.Commandable = commandable;
		}

		/// <summary>
		/// 指定の<see cref="ICodeDbCommand"/>を使用してコマンドを実行しレコード読み取りオブジェクトを取得する
		/// </summary>
		/// <param name="command">コマンド</param>
		/// <param name="arg1">引数1</param>
		/// <param name="arg2">引数2</param>
		/// <param name="arg3">引数3</param>
		/// <param name="arg4">引数4</param>
		/// <param name="arg5">引数5</param>
		/// <param name="arg6">引数6</param>
		/// <param name="arg7">引数7</param>
		/// <param name="arg8">引数8</param>
		/// <param name="arg9">引数9</param>
		/// <returns>影響を受けた行の数</returns>
		public int Execute(ICodeDbCommand command, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9) {
			this.ArgSetter(this.Commandable.Parameters, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
			return this.Commandable.Execute(command);
		}
	}

	/// <summary>
	/// 指定された引数を<see cref="Commandable"/>に設定しコマンドを実行する
	/// </summary>
	/// <typeparam name="T1">引数1の型</typeparam>
	/// <typeparam name="T2">引数2の型</typeparam>
	/// <typeparam name="T3">引数3の型</typeparam>
	/// <typeparam name="T4">引数4の型</typeparam>
	/// <typeparam name="T5">引数5の型</typeparam>
	/// <typeparam name="T6">引数6の型</typeparam>
	/// <typeparam name="T7">引数7の型</typeparam>
	/// <typeparam name="T8">引数8の型</typeparam>
	/// <typeparam name="T9">引数9の型</typeparam>
	/// <typeparam name="T10">引数10の型</typeparam>
	public class ActionCmd<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> {
		/// <summary>
		/// コマンドテキストとパラメータ
		/// </summary>
		public Commandable Commandable { get; private set; }

		/// <summary>
		/// 引数を<see cref="Parameter[]"/>に設定する
		/// </summary>
		public Action<Parameter[], T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> ArgSetter { get; private set; }

		/// <summary>
		/// コンストラクタ、実体となる<see cref="CodeDb.Commandable"/>を指定して初期化する
		/// </summary>
		/// <param name="commandable">実体</param>
		/// <param name="argSetter">引数を<see cref="Commandable"/>に設定する</param>
		public ActionCmd(Commandable commandable, Action<Parameter[], T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> argSetter) {
			this.ArgSetter = argSetter;
			this.Commandable = commandable;
		}

		/// <summary>
		/// 指定の<see cref="ICodeDbCommand"/>を使用してコマンドを実行しレコード読み取りオブジェクトを取得する
		/// </summary>
		/// <param name="command">コマンド</param>
		/// <param name="arg1">引数1</param>
		/// <param name="arg2">引数2</param>
		/// <param name="arg3">引数3</param>
		/// <param name="arg4">引数4</param>
		/// <param name="arg5">引数5</param>
		/// <param name="arg6">引数6</param>
		/// <param name="arg7">引数7</param>
		/// <param name="arg8">引数8</param>
		/// <param name="arg9">引数9</param>
		/// <param name="arg10">引数10</param>
		/// <returns>影響を受けた行の数</returns>
		public int Execute(ICodeDbCommand command, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10) {
			this.ArgSetter(this.Commandable.Parameters, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
			return this.Commandable.Execute(command);
		}
	}

}
