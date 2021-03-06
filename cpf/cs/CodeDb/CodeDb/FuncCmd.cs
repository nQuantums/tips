﻿ 
using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeDb {
	/// <summary>
	/// 指定された引数を<see cref="Commandable"/>に設定しコマンドを実行する
	/// </summary>
	/// <typeparam name="TResult">列挙するレコードの型</typeparam>
	public class FuncCmd<TResult> {
		/// <summary>
		/// コマンドテキストとパラメータ
		/// </summary>
		public Commandable Commandable { get; private set; }

		/// <summary>
		/// <see cref="ICodeDbDataReader"/>から<typeparamref name="TResult"/>型のレコードを列挙するオブジェクト
		/// </summary>
		public IRecordReader<TResult> RecordReader { get; private set; }

		/// <summary>
		/// コンストラクタ、全要素を指定して初期化する
		/// </summary>
		/// <param name="commandable">実体</param>
		/// <param name="recordReader"><see cref="ICodeDbDataReader"/>から<typeparamref name="TResult"/>型のレコードを列挙するオブジェクト</param>
		public FuncCmd(Commandable commandable, IRecordReader<TResult> recordReader) {
			this.Commandable = commandable;
			this.RecordReader = recordReader;
		}

		/// <summary>
		/// 指定の<see cref="ICodeDbCommand"/>を使用してコマンドを実行しレコード読み取りオブジェクトを取得する
		/// </summary>
		/// <param name="command">コマンド</param>
		/// <returns>レコード列挙オブジェクト</returns>
		public RecordEnumerator<TResult> Execute(ICodeDbCommand command) {
			return new RecordEnumerator<TResult>(this.Commandable.ExecuteReader(command), this.RecordReader);
		}
	}

	/// <summary>
	/// 指定された引数を<see cref="Commandable"/>に設定しコマンドを実行する
	/// </summary>
	/// <typeparam name="T1">引数1の型</typeparam>
	/// <typeparam name="TResult">列挙するレコードの型</typeparam>
	public class FuncCmd<T1, TResult> {
		/// <summary>
		/// コマンドテキストとパラメータ
		/// </summary>
		public Commandable Commandable { get; private set; }

		/// <summary>
		/// <see cref="ICodeDbDataReader"/>から<typeparamref name="TResult"/>型のレコードを列挙するオブジェクト
		/// </summary>
		public IRecordReader<TResult> RecordReader { get; private set; }

		/// <summary>
		/// 引数を<see cref="Parameter"/>の配列に設定する
		/// </summary>
		public Action<Parameter[], T1> ArgSetter { get; private set; }

		/// <summary>
		/// コンストラクタ、全要素を指定して初期化する
		/// </summary>
		/// <param name="commandable">実体</param>
		/// <param name="recordReader"><see cref="ICodeDbDataReader"/>から<typeparamref name="TResult"/>型のレコードを列挙するオブジェクト</param>
		/// <param name="argSetter">引数を<see cref="Commandable"/>に設定する</param>
		public FuncCmd(Commandable commandable, IRecordReader<TResult> recordReader, Action<Parameter[], T1> argSetter) {
			this.ArgSetter = argSetter;
			this.Commandable = commandable;
			this.RecordReader = recordReader;
		}

		/// <summary>
		/// 指定の<see cref="ICodeDbCommand"/>を使用してコマンドを実行しレコード読み取りオブジェクトを取得する
		/// </summary>
		/// <param name="command">コマンド</param>
		/// <param name="arg1">引数1</param>
		/// <returns>レコード列挙オブジェクト</returns>
		public RecordEnumerator<TResult> Execute(ICodeDbCommand command, T1 arg1) {
			this.ArgSetter(this.Commandable.Parameters, arg1);
			return new RecordEnumerator<TResult>(this.Commandable.ExecuteReader(command), this.RecordReader);
		}
	}

	/// <summary>
	/// 指定された引数を<see cref="Commandable"/>に設定しコマンドを実行する
	/// </summary>
	/// <typeparam name="T1">引数1の型</typeparam>
	/// <typeparam name="T2">引数2の型</typeparam>
	/// <typeparam name="TResult">列挙するレコードの型</typeparam>
	public class FuncCmd<T1, T2, TResult> {
		/// <summary>
		/// コマンドテキストとパラメータ
		/// </summary>
		public Commandable Commandable { get; private set; }

		/// <summary>
		/// <see cref="ICodeDbDataReader"/>から<typeparamref name="TResult"/>型のレコードを列挙するオブジェクト
		/// </summary>
		public IRecordReader<TResult> RecordReader { get; private set; }

		/// <summary>
		/// 引数を<see cref="Parameter"/>の配列に設定する
		/// </summary>
		public Action<Parameter[], T1, T2> ArgSetter { get; private set; }

		/// <summary>
		/// コンストラクタ、全要素を指定して初期化する
		/// </summary>
		/// <param name="commandable">実体</param>
		/// <param name="recordReader"><see cref="ICodeDbDataReader"/>から<typeparamref name="TResult"/>型のレコードを列挙するオブジェクト</param>
		/// <param name="argSetter">引数を<see cref="Commandable"/>に設定する</param>
		public FuncCmd(Commandable commandable, IRecordReader<TResult> recordReader, Action<Parameter[], T1, T2> argSetter) {
			this.ArgSetter = argSetter;
			this.Commandable = commandable;
			this.RecordReader = recordReader;
		}

		/// <summary>
		/// 指定の<see cref="ICodeDbCommand"/>を使用してコマンドを実行しレコード読み取りオブジェクトを取得する
		/// </summary>
		/// <param name="command">コマンド</param>
		/// <param name="arg1">引数1</param>
		/// <param name="arg2">引数2</param>
		/// <returns>レコード列挙オブジェクト</returns>
		public RecordEnumerator<TResult> Execute(ICodeDbCommand command, T1 arg1, T2 arg2) {
			this.ArgSetter(this.Commandable.Parameters, arg1, arg2);
			return new RecordEnumerator<TResult>(this.Commandable.ExecuteReader(command), this.RecordReader);
		}
	}

	/// <summary>
	/// 指定された引数を<see cref="Commandable"/>に設定しコマンドを実行する
	/// </summary>
	/// <typeparam name="T1">引数1の型</typeparam>
	/// <typeparam name="T2">引数2の型</typeparam>
	/// <typeparam name="T3">引数3の型</typeparam>
	/// <typeparam name="TResult">列挙するレコードの型</typeparam>
	public class FuncCmd<T1, T2, T3, TResult> {
		/// <summary>
		/// コマンドテキストとパラメータ
		/// </summary>
		public Commandable Commandable { get; private set; }

		/// <summary>
		/// <see cref="ICodeDbDataReader"/>から<typeparamref name="TResult"/>型のレコードを列挙するオブジェクト
		/// </summary>
		public IRecordReader<TResult> RecordReader { get; private set; }

		/// <summary>
		/// 引数を<see cref="Parameter"/>の配列に設定する
		/// </summary>
		public Action<Parameter[], T1, T2, T3> ArgSetter { get; private set; }

		/// <summary>
		/// コンストラクタ、全要素を指定して初期化する
		/// </summary>
		/// <param name="commandable">実体</param>
		/// <param name="recordReader"><see cref="ICodeDbDataReader"/>から<typeparamref name="TResult"/>型のレコードを列挙するオブジェクト</param>
		/// <param name="argSetter">引数を<see cref="Commandable"/>に設定する</param>
		public FuncCmd(Commandable commandable, IRecordReader<TResult> recordReader, Action<Parameter[], T1, T2, T3> argSetter) {
			this.ArgSetter = argSetter;
			this.Commandable = commandable;
			this.RecordReader = recordReader;
		}

		/// <summary>
		/// 指定の<see cref="ICodeDbCommand"/>を使用してコマンドを実行しレコード読み取りオブジェクトを取得する
		/// </summary>
		/// <param name="command">コマンド</param>
		/// <param name="arg1">引数1</param>
		/// <param name="arg2">引数2</param>
		/// <param name="arg3">引数3</param>
		/// <returns>レコード列挙オブジェクト</returns>
		public RecordEnumerator<TResult> Execute(ICodeDbCommand command, T1 arg1, T2 arg2, T3 arg3) {
			this.ArgSetter(this.Commandable.Parameters, arg1, arg2, arg3);
			return new RecordEnumerator<TResult>(this.Commandable.ExecuteReader(command), this.RecordReader);
		}
	}

	/// <summary>
	/// 指定された引数を<see cref="Commandable"/>に設定しコマンドを実行する
	/// </summary>
	/// <typeparam name="T1">引数1の型</typeparam>
	/// <typeparam name="T2">引数2の型</typeparam>
	/// <typeparam name="T3">引数3の型</typeparam>
	/// <typeparam name="T4">引数4の型</typeparam>
	/// <typeparam name="TResult">列挙するレコードの型</typeparam>
	public class FuncCmd<T1, T2, T3, T4, TResult> {
		/// <summary>
		/// コマンドテキストとパラメータ
		/// </summary>
		public Commandable Commandable { get; private set; }

		/// <summary>
		/// <see cref="ICodeDbDataReader"/>から<typeparamref name="TResult"/>型のレコードを列挙するオブジェクト
		/// </summary>
		public IRecordReader<TResult> RecordReader { get; private set; }

		/// <summary>
		/// 引数を<see cref="Parameter"/>の配列に設定する
		/// </summary>
		public Action<Parameter[], T1, T2, T3, T4> ArgSetter { get; private set; }

		/// <summary>
		/// コンストラクタ、全要素を指定して初期化する
		/// </summary>
		/// <param name="commandable">実体</param>
		/// <param name="recordReader"><see cref="ICodeDbDataReader"/>から<typeparamref name="TResult"/>型のレコードを列挙するオブジェクト</param>
		/// <param name="argSetter">引数を<see cref="Commandable"/>に設定する</param>
		public FuncCmd(Commandable commandable, IRecordReader<TResult> recordReader, Action<Parameter[], T1, T2, T3, T4> argSetter) {
			this.ArgSetter = argSetter;
			this.Commandable = commandable;
			this.RecordReader = recordReader;
		}

		/// <summary>
		/// 指定の<see cref="ICodeDbCommand"/>を使用してコマンドを実行しレコード読み取りオブジェクトを取得する
		/// </summary>
		/// <param name="command">コマンド</param>
		/// <param name="arg1">引数1</param>
		/// <param name="arg2">引数2</param>
		/// <param name="arg3">引数3</param>
		/// <param name="arg4">引数4</param>
		/// <returns>レコード列挙オブジェクト</returns>
		public RecordEnumerator<TResult> Execute(ICodeDbCommand command, T1 arg1, T2 arg2, T3 arg3, T4 arg4) {
			this.ArgSetter(this.Commandable.Parameters, arg1, arg2, arg3, arg4);
			return new RecordEnumerator<TResult>(this.Commandable.ExecuteReader(command), this.RecordReader);
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
	/// <typeparam name="TResult">列挙するレコードの型</typeparam>
	public class FuncCmd<T1, T2, T3, T4, T5, TResult> {
		/// <summary>
		/// コマンドテキストとパラメータ
		/// </summary>
		public Commandable Commandable { get; private set; }

		/// <summary>
		/// <see cref="ICodeDbDataReader"/>から<typeparamref name="TResult"/>型のレコードを列挙するオブジェクト
		/// </summary>
		public IRecordReader<TResult> RecordReader { get; private set; }

		/// <summary>
		/// 引数を<see cref="Parameter"/>の配列に設定する
		/// </summary>
		public Action<Parameter[], T1, T2, T3, T4, T5> ArgSetter { get; private set; }

		/// <summary>
		/// コンストラクタ、全要素を指定して初期化する
		/// </summary>
		/// <param name="commandable">実体</param>
		/// <param name="recordReader"><see cref="ICodeDbDataReader"/>から<typeparamref name="TResult"/>型のレコードを列挙するオブジェクト</param>
		/// <param name="argSetter">引数を<see cref="Commandable"/>に設定する</param>
		public FuncCmd(Commandable commandable, IRecordReader<TResult> recordReader, Action<Parameter[], T1, T2, T3, T4, T5> argSetter) {
			this.ArgSetter = argSetter;
			this.Commandable = commandable;
			this.RecordReader = recordReader;
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
		/// <returns>レコード列挙オブジェクト</returns>
		public RecordEnumerator<TResult> Execute(ICodeDbCommand command, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) {
			this.ArgSetter(this.Commandable.Parameters, arg1, arg2, arg3, arg4, arg5);
			return new RecordEnumerator<TResult>(this.Commandable.ExecuteReader(command), this.RecordReader);
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
	/// <typeparam name="TResult">列挙するレコードの型</typeparam>
	public class FuncCmd<T1, T2, T3, T4, T5, T6, TResult> {
		/// <summary>
		/// コマンドテキストとパラメータ
		/// </summary>
		public Commandable Commandable { get; private set; }

		/// <summary>
		/// <see cref="ICodeDbDataReader"/>から<typeparamref name="TResult"/>型のレコードを列挙するオブジェクト
		/// </summary>
		public IRecordReader<TResult> RecordReader { get; private set; }

		/// <summary>
		/// 引数を<see cref="Parameter"/>の配列に設定する
		/// </summary>
		public Action<Parameter[], T1, T2, T3, T4, T5, T6> ArgSetter { get; private set; }

		/// <summary>
		/// コンストラクタ、全要素を指定して初期化する
		/// </summary>
		/// <param name="commandable">実体</param>
		/// <param name="recordReader"><see cref="ICodeDbDataReader"/>から<typeparamref name="TResult"/>型のレコードを列挙するオブジェクト</param>
		/// <param name="argSetter">引数を<see cref="Commandable"/>に設定する</param>
		public FuncCmd(Commandable commandable, IRecordReader<TResult> recordReader, Action<Parameter[], T1, T2, T3, T4, T5, T6> argSetter) {
			this.ArgSetter = argSetter;
			this.Commandable = commandable;
			this.RecordReader = recordReader;
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
		/// <returns>レコード列挙オブジェクト</returns>
		public RecordEnumerator<TResult> Execute(ICodeDbCommand command, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) {
			this.ArgSetter(this.Commandable.Parameters, arg1, arg2, arg3, arg4, arg5, arg6);
			return new RecordEnumerator<TResult>(this.Commandable.ExecuteReader(command), this.RecordReader);
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
	/// <typeparam name="TResult">列挙するレコードの型</typeparam>
	public class FuncCmd<T1, T2, T3, T4, T5, T6, T7, TResult> {
		/// <summary>
		/// コマンドテキストとパラメータ
		/// </summary>
		public Commandable Commandable { get; private set; }

		/// <summary>
		/// <see cref="ICodeDbDataReader"/>から<typeparamref name="TResult"/>型のレコードを列挙するオブジェクト
		/// </summary>
		public IRecordReader<TResult> RecordReader { get; private set; }

		/// <summary>
		/// 引数を<see cref="Parameter"/>の配列に設定する
		/// </summary>
		public Action<Parameter[], T1, T2, T3, T4, T5, T6, T7> ArgSetter { get; private set; }

		/// <summary>
		/// コンストラクタ、全要素を指定して初期化する
		/// </summary>
		/// <param name="commandable">実体</param>
		/// <param name="recordReader"><see cref="ICodeDbDataReader"/>から<typeparamref name="TResult"/>型のレコードを列挙するオブジェクト</param>
		/// <param name="argSetter">引数を<see cref="Commandable"/>に設定する</param>
		public FuncCmd(Commandable commandable, IRecordReader<TResult> recordReader, Action<Parameter[], T1, T2, T3, T4, T5, T6, T7> argSetter) {
			this.ArgSetter = argSetter;
			this.Commandable = commandable;
			this.RecordReader = recordReader;
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
		/// <returns>レコード列挙オブジェクト</returns>
		public RecordEnumerator<TResult> Execute(ICodeDbCommand command, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7) {
			this.ArgSetter(this.Commandable.Parameters, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
			return new RecordEnumerator<TResult>(this.Commandable.ExecuteReader(command), this.RecordReader);
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
	/// <typeparam name="TResult">列挙するレコードの型</typeparam>
	public class FuncCmd<T1, T2, T3, T4, T5, T6, T7, T8, TResult> {
		/// <summary>
		/// コマンドテキストとパラメータ
		/// </summary>
		public Commandable Commandable { get; private set; }

		/// <summary>
		/// <see cref="ICodeDbDataReader"/>から<typeparamref name="TResult"/>型のレコードを列挙するオブジェクト
		/// </summary>
		public IRecordReader<TResult> RecordReader { get; private set; }

		/// <summary>
		/// 引数を<see cref="Parameter"/>の配列に設定する
		/// </summary>
		public Action<Parameter[], T1, T2, T3, T4, T5, T6, T7, T8> ArgSetter { get; private set; }

		/// <summary>
		/// コンストラクタ、全要素を指定して初期化する
		/// </summary>
		/// <param name="commandable">実体</param>
		/// <param name="recordReader"><see cref="ICodeDbDataReader"/>から<typeparamref name="TResult"/>型のレコードを列挙するオブジェクト</param>
		/// <param name="argSetter">引数を<see cref="Commandable"/>に設定する</param>
		public FuncCmd(Commandable commandable, IRecordReader<TResult> recordReader, Action<Parameter[], T1, T2, T3, T4, T5, T6, T7, T8> argSetter) {
			this.ArgSetter = argSetter;
			this.Commandable = commandable;
			this.RecordReader = recordReader;
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
		/// <returns>レコード列挙オブジェクト</returns>
		public RecordEnumerator<TResult> Execute(ICodeDbCommand command, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8) {
			this.ArgSetter(this.Commandable.Parameters, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
			return new RecordEnumerator<TResult>(this.Commandable.ExecuteReader(command), this.RecordReader);
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
	/// <typeparam name="TResult">列挙するレコードの型</typeparam>
	public class FuncCmd<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> {
		/// <summary>
		/// コマンドテキストとパラメータ
		/// </summary>
		public Commandable Commandable { get; private set; }

		/// <summary>
		/// <see cref="ICodeDbDataReader"/>から<typeparamref name="TResult"/>型のレコードを列挙するオブジェクト
		/// </summary>
		public IRecordReader<TResult> RecordReader { get; private set; }

		/// <summary>
		/// 引数を<see cref="Parameter"/>の配列に設定する
		/// </summary>
		public Action<Parameter[], T1, T2, T3, T4, T5, T6, T7, T8, T9> ArgSetter { get; private set; }

		/// <summary>
		/// コンストラクタ、全要素を指定して初期化する
		/// </summary>
		/// <param name="commandable">実体</param>
		/// <param name="recordReader"><see cref="ICodeDbDataReader"/>から<typeparamref name="TResult"/>型のレコードを列挙するオブジェクト</param>
		/// <param name="argSetter">引数を<see cref="Commandable"/>に設定する</param>
		public FuncCmd(Commandable commandable, IRecordReader<TResult> recordReader, Action<Parameter[], T1, T2, T3, T4, T5, T6, T7, T8, T9> argSetter) {
			this.ArgSetter = argSetter;
			this.Commandable = commandable;
			this.RecordReader = recordReader;
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
		/// <returns>レコード列挙オブジェクト</returns>
		public RecordEnumerator<TResult> Execute(ICodeDbCommand command, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9) {
			this.ArgSetter(this.Commandable.Parameters, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
			return new RecordEnumerator<TResult>(this.Commandable.ExecuteReader(command), this.RecordReader);
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
	/// <typeparam name="TResult">列挙するレコードの型</typeparam>
	public class FuncCmd<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> {
		/// <summary>
		/// コマンドテキストとパラメータ
		/// </summary>
		public Commandable Commandable { get; private set; }

		/// <summary>
		/// <see cref="ICodeDbDataReader"/>から<typeparamref name="TResult"/>型のレコードを列挙するオブジェクト
		/// </summary>
		public IRecordReader<TResult> RecordReader { get; private set; }

		/// <summary>
		/// 引数を<see cref="Parameter"/>の配列に設定する
		/// </summary>
		public Action<Parameter[], T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> ArgSetter { get; private set; }

		/// <summary>
		/// コンストラクタ、全要素を指定して初期化する
		/// </summary>
		/// <param name="commandable">実体</param>
		/// <param name="recordReader"><see cref="ICodeDbDataReader"/>から<typeparamref name="TResult"/>型のレコードを列挙するオブジェクト</param>
		/// <param name="argSetter">引数を<see cref="Commandable"/>に設定する</param>
		public FuncCmd(Commandable commandable, IRecordReader<TResult> recordReader, Action<Parameter[], T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> argSetter) {
			this.ArgSetter = argSetter;
			this.Commandable = commandable;
			this.RecordReader = recordReader;
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
		/// <returns>レコード列挙オブジェクト</returns>
		public RecordEnumerator<TResult> Execute(ICodeDbCommand command, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10) {
			this.ArgSetter(this.Commandable.Parameters, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
			return new RecordEnumerator<TResult>(this.Commandable.ExecuteReader(command), this.RecordReader);
		}
	}

}
