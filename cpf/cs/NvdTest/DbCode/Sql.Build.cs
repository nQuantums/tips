 
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using DbCode.Query;
using DbCode.Internal;

namespace DbCode {
	public partial class Sql {
		/// <summary>
		/// <see cref="IDbCodeCommand"/>に対して実行するアクションをビルドする
		/// </summary>
		/// <returns>アクションコマンド</returns>
		public ActionCmd BuildAction() {
			return new ActionCmd(this.Build());
		}

		/// <summary>
		/// <see cref="IDbCodeCommand"/>に対して実行可能な関数をビルドする
		/// </summary>
		/// <typeparam name="TResult">列挙するレコードの型</typeparam>
		/// <returns>関数コマンド</returns>
		public FuncCmd<TResult> BuildFunc<TResult>() {
			return new FuncCmd<TResult>(this.Build(), this.Environment.CreateRecordReader<TResult>(null));
		}

		/// <summary>
		/// <see cref="IDbCodeCommand"/>に対して実行可能な関数をビルドする
		/// </summary>
		/// <param name="select">SELECT句ノード、これにより列挙するレコード型が推論される</param>
		/// <typeparam name="TResult">列挙するレコードの型</typeparam>
		/// <returns>実行可能SQL</returns>
		public FuncCmd<TResult> BuildFuncFromSelect<TResult>(ISelect<TResult> @select) {
			return new FuncCmd<TResult>(this.Build(), this.Environment.CreateRecordReader<TResult>((from c in @select.ColumnMap select c.Property).ToArray()));
		}

		/// <summary>
		/// <see cref="IDbCodeCommand"/>に対して実行するアクションをビルドする
		/// </summary>
		/// <param name="arg1">引数1</param>
		/// <typeparam name="T1">引数1の型</typeparam>
		/// <returns>実行可能SQL</returns>
		public ActionCmd<T1> BuildAction<T1>(Argument arg1) {
			var commandable = this.Build();
			var piPrmValue = typeof(Parameter).GetProperty("Value");
			var piArgValue = typeof(Argument).GetProperty("Value");
			var paramsExpr = Expression.Parameter(typeof(Parameter[]));
			var arg1Expr = Expression.Parameter(typeof(T1));
			var blockExpr = Expression.Block(
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg1))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg1Expr, typeof(object)))
			);
			return new ActionCmd<T1>(commandable, Expression.Lambda<Action<Parameter[], T1>>(blockExpr, paramsExpr, arg1Expr).Compile());
		}

		/// <summary>
		/// <see cref="IDbCodeCommand"/>に対して実行可能な関数をビルドする
		/// </summary>
		/// <param name="arg1">引数1</param>
		/// <typeparam name="T1">引数1の型</typeparam>
		/// <typeparam name="TResult">列挙するレコードの型</typeparam>
		/// <returns>実行可能SQL</returns>
		public FuncCmd<T1, TResult> BuildFunc<T1, TResult>(Argument arg1) {
			var commandable = this.Build();
			var piPrmValue = typeof(Parameter).GetProperty("Value");
			var piArgValue = typeof(Argument).GetProperty("Value");
			var paramsExpr = Expression.Parameter(typeof(Parameter[]));
			var arg1Expr = Expression.Parameter(typeof(T1));
			var blockExpr = Expression.Block(
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg1))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg1Expr, typeof(object)))
			);
			return new FuncCmd<T1, TResult>(commandable, this.Environment.CreateRecordReader<TResult>(null), Expression.Lambda<Action<Parameter[], T1>>(blockExpr, paramsExpr, arg1Expr).Compile());
		}

		/// <summary>
		/// <see cref="IDbCodeCommand"/>に対して実行可能な関数をビルドする
		/// </summary>
		/// <param name="select">SELECT句ノード、これにより列挙するレコード型が推論される</param>
		/// <param name="arg1">引数1</param>
		/// <typeparam name="T1">引数1の型</typeparam>
		/// <typeparam name="TResult">列挙するレコードの型</typeparam>
		/// <returns>実行可能SQL</returns>
		public FuncCmd<T1, TResult> BuildFuncFromSelect<T1, TResult>(ISelect<TResult> select, Argument arg1) {
			var commandable = this.Build();
			var piPrmValue = typeof(Parameter).GetProperty("Value");
			var piArgValue = typeof(Argument).GetProperty("Value");
			var paramsExpr = Expression.Parameter(typeof(Parameter[]));
			var arg1Expr = Expression.Parameter(typeof(T1));
			var blockExpr = Expression.Block(
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg1))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg1Expr, typeof(object)))
			);
			return new FuncCmd<T1, TResult>(commandable, this.Environment.CreateRecordReader<TResult>((from c in @select.ColumnMap select c.Property).ToArray()), Expression.Lambda<Action<Parameter[], T1>>(blockExpr, paramsExpr, arg1Expr).Compile());
		}

		/// <summary>
		/// <see cref="IDbCodeCommand"/>に対して実行するアクションをビルドする
		/// </summary>
		/// <param name="arg1">引数1</param>
		/// <param name="arg2">引数2</param>
		/// <typeparam name="T1">引数1の型</typeparam>
		/// <typeparam name="T2">引数2の型</typeparam>
		/// <returns>実行可能SQL</returns>
		public ActionCmd<T1, T2> BuildAction<T1, T2>(Argument arg1, Argument arg2) {
			var commandable = this.Build();
			var piPrmValue = typeof(Parameter).GetProperty("Value");
			var piArgValue = typeof(Argument).GetProperty("Value");
			var paramsExpr = Expression.Parameter(typeof(Parameter[]));
			var arg1Expr = Expression.Parameter(typeof(T1));
			var arg2Expr = Expression.Parameter(typeof(T2));
			var blockExpr = Expression.Block(
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg1))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg1Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg2))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg2Expr, typeof(object)))
			);
			return new ActionCmd<T1, T2>(commandable, Expression.Lambda<Action<Parameter[], T1, T2>>(blockExpr, paramsExpr, arg1Expr, arg2Expr).Compile());
		}

		/// <summary>
		/// <see cref="IDbCodeCommand"/>に対して実行可能な関数をビルドする
		/// </summary>
		/// <param name="arg1">引数1</param>
		/// <param name="arg2">引数2</param>
		/// <typeparam name="T1">引数1の型</typeparam>
		/// <typeparam name="T2">引数2の型</typeparam>
		/// <typeparam name="TResult">列挙するレコードの型</typeparam>
		/// <returns>実行可能SQL</returns>
		public FuncCmd<T1, T2, TResult> BuildFunc<T1, T2, TResult>(Argument arg1, Argument arg2) {
			var commandable = this.Build();
			var piPrmValue = typeof(Parameter).GetProperty("Value");
			var piArgValue = typeof(Argument).GetProperty("Value");
			var paramsExpr = Expression.Parameter(typeof(Parameter[]));
			var arg1Expr = Expression.Parameter(typeof(T1));
			var arg2Expr = Expression.Parameter(typeof(T2));
			var blockExpr = Expression.Block(
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg1))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg1Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg2))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg2Expr, typeof(object)))
			);
			return new FuncCmd<T1, T2, TResult>(commandable, this.Environment.CreateRecordReader<TResult>(null), Expression.Lambda<Action<Parameter[], T1, T2>>(blockExpr, paramsExpr, arg1Expr, arg2Expr).Compile());
		}

		/// <summary>
		/// <see cref="IDbCodeCommand"/>に対して実行可能な関数をビルドする
		/// </summary>
		/// <param name="select">SELECT句ノード、これにより列挙するレコード型が推論される</param>
		/// <param name="arg1">引数1</param>
		/// <param name="arg2">引数2</param>
		/// <typeparam name="T1">引数1の型</typeparam>
		/// <typeparam name="T2">引数2の型</typeparam>
		/// <typeparam name="TResult">列挙するレコードの型</typeparam>
		/// <returns>実行可能SQL</returns>
		public FuncCmd<T1, T2, TResult> BuildFuncFromSelect<T1, T2, TResult>(ISelect<TResult> select, Argument arg1, Argument arg2) {
			var commandable = this.Build();
			var piPrmValue = typeof(Parameter).GetProperty("Value");
			var piArgValue = typeof(Argument).GetProperty("Value");
			var paramsExpr = Expression.Parameter(typeof(Parameter[]));
			var arg1Expr = Expression.Parameter(typeof(T1));
			var arg2Expr = Expression.Parameter(typeof(T2));
			var blockExpr = Expression.Block(
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg1))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg1Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg2))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg2Expr, typeof(object)))
			);
			return new FuncCmd<T1, T2, TResult>(commandable, this.Environment.CreateRecordReader<TResult>((from c in @select.ColumnMap select c.Property).ToArray()), Expression.Lambda<Action<Parameter[], T1, T2>>(blockExpr, paramsExpr, arg1Expr, arg2Expr).Compile());
		}

		/// <summary>
		/// <see cref="IDbCodeCommand"/>に対して実行するアクションをビルドする
		/// </summary>
		/// <param name="arg1">引数1</param>
		/// <param name="arg2">引数2</param>
		/// <param name="arg3">引数3</param>
		/// <typeparam name="T1">引数1の型</typeparam>
		/// <typeparam name="T2">引数2の型</typeparam>
		/// <typeparam name="T3">引数3の型</typeparam>
		/// <returns>実行可能SQL</returns>
		public ActionCmd<T1, T2, T3> BuildAction<T1, T2, T3>(Argument arg1, Argument arg2, Argument arg3) {
			var commandable = this.Build();
			var piPrmValue = typeof(Parameter).GetProperty("Value");
			var piArgValue = typeof(Argument).GetProperty("Value");
			var paramsExpr = Expression.Parameter(typeof(Parameter[]));
			var arg1Expr = Expression.Parameter(typeof(T1));
			var arg2Expr = Expression.Parameter(typeof(T2));
			var arg3Expr = Expression.Parameter(typeof(T3));
			var blockExpr = Expression.Block(
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg1))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg1Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg2))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg2Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg3))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg3Expr, typeof(object)))
			);
			return new ActionCmd<T1, T2, T3>(commandable, Expression.Lambda<Action<Parameter[], T1, T2, T3>>(blockExpr, paramsExpr, arg1Expr, arg2Expr, arg3Expr).Compile());
		}

		/// <summary>
		/// <see cref="IDbCodeCommand"/>に対して実行可能な関数をビルドする
		/// </summary>
		/// <param name="arg1">引数1</param>
		/// <param name="arg2">引数2</param>
		/// <param name="arg3">引数3</param>
		/// <typeparam name="T1">引数1の型</typeparam>
		/// <typeparam name="T2">引数2の型</typeparam>
		/// <typeparam name="T3">引数3の型</typeparam>
		/// <typeparam name="TResult">列挙するレコードの型</typeparam>
		/// <returns>実行可能SQL</returns>
		public FuncCmd<T1, T2, T3, TResult> BuildFunc<T1, T2, T3, TResult>(Argument arg1, Argument arg2, Argument arg3) {
			var commandable = this.Build();
			var piPrmValue = typeof(Parameter).GetProperty("Value");
			var piArgValue = typeof(Argument).GetProperty("Value");
			var paramsExpr = Expression.Parameter(typeof(Parameter[]));
			var arg1Expr = Expression.Parameter(typeof(T1));
			var arg2Expr = Expression.Parameter(typeof(T2));
			var arg3Expr = Expression.Parameter(typeof(T3));
			var blockExpr = Expression.Block(
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg1))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg1Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg2))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg2Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg3))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg3Expr, typeof(object)))
			);
			return new FuncCmd<T1, T2, T3, TResult>(commandable, this.Environment.CreateRecordReader<TResult>(null), Expression.Lambda<Action<Parameter[], T1, T2, T3>>(blockExpr, paramsExpr, arg1Expr, arg2Expr, arg3Expr).Compile());
		}

		/// <summary>
		/// <see cref="IDbCodeCommand"/>に対して実行可能な関数をビルドする
		/// </summary>
		/// <param name="select">SELECT句ノード、これにより列挙するレコード型が推論される</param>
		/// <param name="arg1">引数1</param>
		/// <param name="arg2">引数2</param>
		/// <param name="arg3">引数3</param>
		/// <typeparam name="T1">引数1の型</typeparam>
		/// <typeparam name="T2">引数2の型</typeparam>
		/// <typeparam name="T3">引数3の型</typeparam>
		/// <typeparam name="TResult">列挙するレコードの型</typeparam>
		/// <returns>実行可能SQL</returns>
		public FuncCmd<T1, T2, T3, TResult> BuildFuncFromSelect<T1, T2, T3, TResult>(ISelect<TResult> select, Argument arg1, Argument arg2, Argument arg3) {
			var commandable = this.Build();
			var piPrmValue = typeof(Parameter).GetProperty("Value");
			var piArgValue = typeof(Argument).GetProperty("Value");
			var paramsExpr = Expression.Parameter(typeof(Parameter[]));
			var arg1Expr = Expression.Parameter(typeof(T1));
			var arg2Expr = Expression.Parameter(typeof(T2));
			var arg3Expr = Expression.Parameter(typeof(T3));
			var blockExpr = Expression.Block(
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg1))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg1Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg2))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg2Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg3))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg3Expr, typeof(object)))
			);
			return new FuncCmd<T1, T2, T3, TResult>(commandable, this.Environment.CreateRecordReader<TResult>((from c in @select.ColumnMap select c.Property).ToArray()), Expression.Lambda<Action<Parameter[], T1, T2, T3>>(blockExpr, paramsExpr, arg1Expr, arg2Expr, arg3Expr).Compile());
		}

		/// <summary>
		/// <see cref="IDbCodeCommand"/>に対して実行するアクションをビルドする
		/// </summary>
		/// <param name="arg1">引数1</param>
		/// <param name="arg2">引数2</param>
		/// <param name="arg3">引数3</param>
		/// <param name="arg4">引数4</param>
		/// <typeparam name="T1">引数1の型</typeparam>
		/// <typeparam name="T2">引数2の型</typeparam>
		/// <typeparam name="T3">引数3の型</typeparam>
		/// <typeparam name="T4">引数4の型</typeparam>
		/// <returns>実行可能SQL</returns>
		public ActionCmd<T1, T2, T3, T4> BuildAction<T1, T2, T3, T4>(Argument arg1, Argument arg2, Argument arg3, Argument arg4) {
			var commandable = this.Build();
			var piPrmValue = typeof(Parameter).GetProperty("Value");
			var piArgValue = typeof(Argument).GetProperty("Value");
			var paramsExpr = Expression.Parameter(typeof(Parameter[]));
			var arg1Expr = Expression.Parameter(typeof(T1));
			var arg2Expr = Expression.Parameter(typeof(T2));
			var arg3Expr = Expression.Parameter(typeof(T3));
			var arg4Expr = Expression.Parameter(typeof(T4));
			var blockExpr = Expression.Block(
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg1))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg1Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg2))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg2Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg3))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg3Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg4))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg4Expr, typeof(object)))
			);
			return new ActionCmd<T1, T2, T3, T4>(commandable, Expression.Lambda<Action<Parameter[], T1, T2, T3, T4>>(blockExpr, paramsExpr, arg1Expr, arg2Expr, arg3Expr, arg4Expr).Compile());
		}

		/// <summary>
		/// <see cref="IDbCodeCommand"/>に対して実行可能な関数をビルドする
		/// </summary>
		/// <param name="arg1">引数1</param>
		/// <param name="arg2">引数2</param>
		/// <param name="arg3">引数3</param>
		/// <param name="arg4">引数4</param>
		/// <typeparam name="T1">引数1の型</typeparam>
		/// <typeparam name="T2">引数2の型</typeparam>
		/// <typeparam name="T3">引数3の型</typeparam>
		/// <typeparam name="T4">引数4の型</typeparam>
		/// <typeparam name="TResult">列挙するレコードの型</typeparam>
		/// <returns>実行可能SQL</returns>
		public FuncCmd<T1, T2, T3, T4, TResult> BuildFunc<T1, T2, T3, T4, TResult>(Argument arg1, Argument arg2, Argument arg3, Argument arg4) {
			var commandable = this.Build();
			var piPrmValue = typeof(Parameter).GetProperty("Value");
			var piArgValue = typeof(Argument).GetProperty("Value");
			var paramsExpr = Expression.Parameter(typeof(Parameter[]));
			var arg1Expr = Expression.Parameter(typeof(T1));
			var arg2Expr = Expression.Parameter(typeof(T2));
			var arg3Expr = Expression.Parameter(typeof(T3));
			var arg4Expr = Expression.Parameter(typeof(T4));
			var blockExpr = Expression.Block(
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg1))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg1Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg2))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg2Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg3))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg3Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg4))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg4Expr, typeof(object)))
			);
			return new FuncCmd<T1, T2, T3, T4, TResult>(commandable, this.Environment.CreateRecordReader<TResult>(null), Expression.Lambda<Action<Parameter[], T1, T2, T3, T4>>(blockExpr, paramsExpr, arg1Expr, arg2Expr, arg3Expr, arg4Expr).Compile());
		}

		/// <summary>
		/// <see cref="IDbCodeCommand"/>に対して実行可能な関数をビルドする
		/// </summary>
		/// <param name="select">SELECT句ノード、これにより列挙するレコード型が推論される</param>
		/// <param name="arg1">引数1</param>
		/// <param name="arg2">引数2</param>
		/// <param name="arg3">引数3</param>
		/// <param name="arg4">引数4</param>
		/// <typeparam name="T1">引数1の型</typeparam>
		/// <typeparam name="T2">引数2の型</typeparam>
		/// <typeparam name="T3">引数3の型</typeparam>
		/// <typeparam name="T4">引数4の型</typeparam>
		/// <typeparam name="TResult">列挙するレコードの型</typeparam>
		/// <returns>実行可能SQL</returns>
		public FuncCmd<T1, T2, T3, T4, TResult> BuildFuncFromSelect<T1, T2, T3, T4, TResult>(ISelect<TResult> select, Argument arg1, Argument arg2, Argument arg3, Argument arg4) {
			var commandable = this.Build();
			var piPrmValue = typeof(Parameter).GetProperty("Value");
			var piArgValue = typeof(Argument).GetProperty("Value");
			var paramsExpr = Expression.Parameter(typeof(Parameter[]));
			var arg1Expr = Expression.Parameter(typeof(T1));
			var arg2Expr = Expression.Parameter(typeof(T2));
			var arg3Expr = Expression.Parameter(typeof(T3));
			var arg4Expr = Expression.Parameter(typeof(T4));
			var blockExpr = Expression.Block(
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg1))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg1Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg2))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg2Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg3))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg3Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg4))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg4Expr, typeof(object)))
			);
			return new FuncCmd<T1, T2, T3, T4, TResult>(commandable, this.Environment.CreateRecordReader<TResult>((from c in @select.ColumnMap select c.Property).ToArray()), Expression.Lambda<Action<Parameter[], T1, T2, T3, T4>>(blockExpr, paramsExpr, arg1Expr, arg2Expr, arg3Expr, arg4Expr).Compile());
		}

		/// <summary>
		/// <see cref="IDbCodeCommand"/>に対して実行するアクションをビルドする
		/// </summary>
		/// <param name="arg1">引数1</param>
		/// <param name="arg2">引数2</param>
		/// <param name="arg3">引数3</param>
		/// <param name="arg4">引数4</param>
		/// <param name="arg5">引数5</param>
		/// <typeparam name="T1">引数1の型</typeparam>
		/// <typeparam name="T2">引数2の型</typeparam>
		/// <typeparam name="T3">引数3の型</typeparam>
		/// <typeparam name="T4">引数4の型</typeparam>
		/// <typeparam name="T5">引数5の型</typeparam>
		/// <returns>実行可能SQL</returns>
		public ActionCmd<T1, T2, T3, T4, T5> BuildAction<T1, T2, T3, T4, T5>(Argument arg1, Argument arg2, Argument arg3, Argument arg4, Argument arg5) {
			var commandable = this.Build();
			var piPrmValue = typeof(Parameter).GetProperty("Value");
			var piArgValue = typeof(Argument).GetProperty("Value");
			var paramsExpr = Expression.Parameter(typeof(Parameter[]));
			var arg1Expr = Expression.Parameter(typeof(T1));
			var arg2Expr = Expression.Parameter(typeof(T2));
			var arg3Expr = Expression.Parameter(typeof(T3));
			var arg4Expr = Expression.Parameter(typeof(T4));
			var arg5Expr = Expression.Parameter(typeof(T5));
			var blockExpr = Expression.Block(
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg1))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg1Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg2))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg2Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg3))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg3Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg4))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg4Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg5))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg5Expr, typeof(object)))
			);
			return new ActionCmd<T1, T2, T3, T4, T5>(commandable, Expression.Lambda<Action<Parameter[], T1, T2, T3, T4, T5>>(blockExpr, paramsExpr, arg1Expr, arg2Expr, arg3Expr, arg4Expr, arg5Expr).Compile());
		}

		/// <summary>
		/// <see cref="IDbCodeCommand"/>に対して実行可能な関数をビルドする
		/// </summary>
		/// <param name="arg1">引数1</param>
		/// <param name="arg2">引数2</param>
		/// <param name="arg3">引数3</param>
		/// <param name="arg4">引数4</param>
		/// <param name="arg5">引数5</param>
		/// <typeparam name="T1">引数1の型</typeparam>
		/// <typeparam name="T2">引数2の型</typeparam>
		/// <typeparam name="T3">引数3の型</typeparam>
		/// <typeparam name="T4">引数4の型</typeparam>
		/// <typeparam name="T5">引数5の型</typeparam>
		/// <typeparam name="TResult">列挙するレコードの型</typeparam>
		/// <returns>実行可能SQL</returns>
		public FuncCmd<T1, T2, T3, T4, T5, TResult> BuildFunc<T1, T2, T3, T4, T5, TResult>(Argument arg1, Argument arg2, Argument arg3, Argument arg4, Argument arg5) {
			var commandable = this.Build();
			var piPrmValue = typeof(Parameter).GetProperty("Value");
			var piArgValue = typeof(Argument).GetProperty("Value");
			var paramsExpr = Expression.Parameter(typeof(Parameter[]));
			var arg1Expr = Expression.Parameter(typeof(T1));
			var arg2Expr = Expression.Parameter(typeof(T2));
			var arg3Expr = Expression.Parameter(typeof(T3));
			var arg4Expr = Expression.Parameter(typeof(T4));
			var arg5Expr = Expression.Parameter(typeof(T5));
			var blockExpr = Expression.Block(
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg1))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg1Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg2))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg2Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg3))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg3Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg4))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg4Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg5))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg5Expr, typeof(object)))
			);
			return new FuncCmd<T1, T2, T3, T4, T5, TResult>(commandable, this.Environment.CreateRecordReader<TResult>(null), Expression.Lambda<Action<Parameter[], T1, T2, T3, T4, T5>>(blockExpr, paramsExpr, arg1Expr, arg2Expr, arg3Expr, arg4Expr, arg5Expr).Compile());
		}

		/// <summary>
		/// <see cref="IDbCodeCommand"/>に対して実行可能な関数をビルドする
		/// </summary>
		/// <param name="select">SELECT句ノード、これにより列挙するレコード型が推論される</param>
		/// <param name="arg1">引数1</param>
		/// <param name="arg2">引数2</param>
		/// <param name="arg3">引数3</param>
		/// <param name="arg4">引数4</param>
		/// <param name="arg5">引数5</param>
		/// <typeparam name="T1">引数1の型</typeparam>
		/// <typeparam name="T2">引数2の型</typeparam>
		/// <typeparam name="T3">引数3の型</typeparam>
		/// <typeparam name="T4">引数4の型</typeparam>
		/// <typeparam name="T5">引数5の型</typeparam>
		/// <typeparam name="TResult">列挙するレコードの型</typeparam>
		/// <returns>実行可能SQL</returns>
		public FuncCmd<T1, T2, T3, T4, T5, TResult> BuildFuncFromSelect<T1, T2, T3, T4, T5, TResult>(ISelect<TResult> select, Argument arg1, Argument arg2, Argument arg3, Argument arg4, Argument arg5) {
			var commandable = this.Build();
			var piPrmValue = typeof(Parameter).GetProperty("Value");
			var piArgValue = typeof(Argument).GetProperty("Value");
			var paramsExpr = Expression.Parameter(typeof(Parameter[]));
			var arg1Expr = Expression.Parameter(typeof(T1));
			var arg2Expr = Expression.Parameter(typeof(T2));
			var arg3Expr = Expression.Parameter(typeof(T3));
			var arg4Expr = Expression.Parameter(typeof(T4));
			var arg5Expr = Expression.Parameter(typeof(T5));
			var blockExpr = Expression.Block(
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg1))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg1Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg2))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg2Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg3))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg3Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg4))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg4Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg5))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg5Expr, typeof(object)))
			);
			return new FuncCmd<T1, T2, T3, T4, T5, TResult>(commandable, this.Environment.CreateRecordReader<TResult>((from c in @select.ColumnMap select c.Property).ToArray()), Expression.Lambda<Action<Parameter[], T1, T2, T3, T4, T5>>(blockExpr, paramsExpr, arg1Expr, arg2Expr, arg3Expr, arg4Expr, arg5Expr).Compile());
		}

		/// <summary>
		/// <see cref="IDbCodeCommand"/>に対して実行するアクションをビルドする
		/// </summary>
		/// <param name="arg1">引数1</param>
		/// <param name="arg2">引数2</param>
		/// <param name="arg3">引数3</param>
		/// <param name="arg4">引数4</param>
		/// <param name="arg5">引数5</param>
		/// <param name="arg6">引数6</param>
		/// <typeparam name="T1">引数1の型</typeparam>
		/// <typeparam name="T2">引数2の型</typeparam>
		/// <typeparam name="T3">引数3の型</typeparam>
		/// <typeparam name="T4">引数4の型</typeparam>
		/// <typeparam name="T5">引数5の型</typeparam>
		/// <typeparam name="T6">引数6の型</typeparam>
		/// <returns>実行可能SQL</returns>
		public ActionCmd<T1, T2, T3, T4, T5, T6> BuildAction<T1, T2, T3, T4, T5, T6>(Argument arg1, Argument arg2, Argument arg3, Argument arg4, Argument arg5, Argument arg6) {
			var commandable = this.Build();
			var piPrmValue = typeof(Parameter).GetProperty("Value");
			var piArgValue = typeof(Argument).GetProperty("Value");
			var paramsExpr = Expression.Parameter(typeof(Parameter[]));
			var arg1Expr = Expression.Parameter(typeof(T1));
			var arg2Expr = Expression.Parameter(typeof(T2));
			var arg3Expr = Expression.Parameter(typeof(T3));
			var arg4Expr = Expression.Parameter(typeof(T4));
			var arg5Expr = Expression.Parameter(typeof(T5));
			var arg6Expr = Expression.Parameter(typeof(T6));
			var blockExpr = Expression.Block(
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg1))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg1Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg2))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg2Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg3))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg3Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg4))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg4Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg5))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg5Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg6))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg6Expr, typeof(object)))
			);
			return new ActionCmd<T1, T2, T3, T4, T5, T6>(commandable, Expression.Lambda<Action<Parameter[], T1, T2, T3, T4, T5, T6>>(blockExpr, paramsExpr, arg1Expr, arg2Expr, arg3Expr, arg4Expr, arg5Expr, arg6Expr).Compile());
		}

		/// <summary>
		/// <see cref="IDbCodeCommand"/>に対して実行可能な関数をビルドする
		/// </summary>
		/// <param name="arg1">引数1</param>
		/// <param name="arg2">引数2</param>
		/// <param name="arg3">引数3</param>
		/// <param name="arg4">引数4</param>
		/// <param name="arg5">引数5</param>
		/// <param name="arg6">引数6</param>
		/// <typeparam name="T1">引数1の型</typeparam>
		/// <typeparam name="T2">引数2の型</typeparam>
		/// <typeparam name="T3">引数3の型</typeparam>
		/// <typeparam name="T4">引数4の型</typeparam>
		/// <typeparam name="T5">引数5の型</typeparam>
		/// <typeparam name="T6">引数6の型</typeparam>
		/// <typeparam name="TResult">列挙するレコードの型</typeparam>
		/// <returns>実行可能SQL</returns>
		public FuncCmd<T1, T2, T3, T4, T5, T6, TResult> BuildFunc<T1, T2, T3, T4, T5, T6, TResult>(Argument arg1, Argument arg2, Argument arg3, Argument arg4, Argument arg5, Argument arg6) {
			var commandable = this.Build();
			var piPrmValue = typeof(Parameter).GetProperty("Value");
			var piArgValue = typeof(Argument).GetProperty("Value");
			var paramsExpr = Expression.Parameter(typeof(Parameter[]));
			var arg1Expr = Expression.Parameter(typeof(T1));
			var arg2Expr = Expression.Parameter(typeof(T2));
			var arg3Expr = Expression.Parameter(typeof(T3));
			var arg4Expr = Expression.Parameter(typeof(T4));
			var arg5Expr = Expression.Parameter(typeof(T5));
			var arg6Expr = Expression.Parameter(typeof(T6));
			var blockExpr = Expression.Block(
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg1))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg1Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg2))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg2Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg3))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg3Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg4))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg4Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg5))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg5Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg6))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg6Expr, typeof(object)))
			);
			return new FuncCmd<T1, T2, T3, T4, T5, T6, TResult>(commandable, this.Environment.CreateRecordReader<TResult>(null), Expression.Lambda<Action<Parameter[], T1, T2, T3, T4, T5, T6>>(blockExpr, paramsExpr, arg1Expr, arg2Expr, arg3Expr, arg4Expr, arg5Expr, arg6Expr).Compile());
		}

		/// <summary>
		/// <see cref="IDbCodeCommand"/>に対して実行可能な関数をビルドする
		/// </summary>
		/// <param name="select">SELECT句ノード、これにより列挙するレコード型が推論される</param>
		/// <param name="arg1">引数1</param>
		/// <param name="arg2">引数2</param>
		/// <param name="arg3">引数3</param>
		/// <param name="arg4">引数4</param>
		/// <param name="arg5">引数5</param>
		/// <param name="arg6">引数6</param>
		/// <typeparam name="T1">引数1の型</typeparam>
		/// <typeparam name="T2">引数2の型</typeparam>
		/// <typeparam name="T3">引数3の型</typeparam>
		/// <typeparam name="T4">引数4の型</typeparam>
		/// <typeparam name="T5">引数5の型</typeparam>
		/// <typeparam name="T6">引数6の型</typeparam>
		/// <typeparam name="TResult">列挙するレコードの型</typeparam>
		/// <returns>実行可能SQL</returns>
		public FuncCmd<T1, T2, T3, T4, T5, T6, TResult> BuildFuncFromSelect<T1, T2, T3, T4, T5, T6, TResult>(ISelect<TResult> select, Argument arg1, Argument arg2, Argument arg3, Argument arg4, Argument arg5, Argument arg6) {
			var commandable = this.Build();
			var piPrmValue = typeof(Parameter).GetProperty("Value");
			var piArgValue = typeof(Argument).GetProperty("Value");
			var paramsExpr = Expression.Parameter(typeof(Parameter[]));
			var arg1Expr = Expression.Parameter(typeof(T1));
			var arg2Expr = Expression.Parameter(typeof(T2));
			var arg3Expr = Expression.Parameter(typeof(T3));
			var arg4Expr = Expression.Parameter(typeof(T4));
			var arg5Expr = Expression.Parameter(typeof(T5));
			var arg6Expr = Expression.Parameter(typeof(T6));
			var blockExpr = Expression.Block(
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg1))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg1Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg2))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg2Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg3))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg3Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg4))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg4Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg5))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg5Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg6))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg6Expr, typeof(object)))
			);
			return new FuncCmd<T1, T2, T3, T4, T5, T6, TResult>(commandable, this.Environment.CreateRecordReader<TResult>((from c in @select.ColumnMap select c.Property).ToArray()), Expression.Lambda<Action<Parameter[], T1, T2, T3, T4, T5, T6>>(blockExpr, paramsExpr, arg1Expr, arg2Expr, arg3Expr, arg4Expr, arg5Expr, arg6Expr).Compile());
		}

		/// <summary>
		/// <see cref="IDbCodeCommand"/>に対して実行するアクションをビルドする
		/// </summary>
		/// <param name="arg1">引数1</param>
		/// <param name="arg2">引数2</param>
		/// <param name="arg3">引数3</param>
		/// <param name="arg4">引数4</param>
		/// <param name="arg5">引数5</param>
		/// <param name="arg6">引数6</param>
		/// <param name="arg7">引数7</param>
		/// <typeparam name="T1">引数1の型</typeparam>
		/// <typeparam name="T2">引数2の型</typeparam>
		/// <typeparam name="T3">引数3の型</typeparam>
		/// <typeparam name="T4">引数4の型</typeparam>
		/// <typeparam name="T5">引数5の型</typeparam>
		/// <typeparam name="T6">引数6の型</typeparam>
		/// <typeparam name="T7">引数7の型</typeparam>
		/// <returns>実行可能SQL</returns>
		public ActionCmd<T1, T2, T3, T4, T5, T6, T7> BuildAction<T1, T2, T3, T4, T5, T6, T7>(Argument arg1, Argument arg2, Argument arg3, Argument arg4, Argument arg5, Argument arg6, Argument arg7) {
			var commandable = this.Build();
			var piPrmValue = typeof(Parameter).GetProperty("Value");
			var piArgValue = typeof(Argument).GetProperty("Value");
			var paramsExpr = Expression.Parameter(typeof(Parameter[]));
			var arg1Expr = Expression.Parameter(typeof(T1));
			var arg2Expr = Expression.Parameter(typeof(T2));
			var arg3Expr = Expression.Parameter(typeof(T3));
			var arg4Expr = Expression.Parameter(typeof(T4));
			var arg5Expr = Expression.Parameter(typeof(T5));
			var arg6Expr = Expression.Parameter(typeof(T6));
			var arg7Expr = Expression.Parameter(typeof(T7));
			var blockExpr = Expression.Block(
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg1))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg1Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg2))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg2Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg3))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg3Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg4))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg4Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg5))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg5Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg6))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg6Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg7))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg7Expr, typeof(object)))
			);
			return new ActionCmd<T1, T2, T3, T4, T5, T6, T7>(commandable, Expression.Lambda<Action<Parameter[], T1, T2, T3, T4, T5, T6, T7>>(blockExpr, paramsExpr, arg1Expr, arg2Expr, arg3Expr, arg4Expr, arg5Expr, arg6Expr, arg7Expr).Compile());
		}

		/// <summary>
		/// <see cref="IDbCodeCommand"/>に対して実行可能な関数をビルドする
		/// </summary>
		/// <param name="arg1">引数1</param>
		/// <param name="arg2">引数2</param>
		/// <param name="arg3">引数3</param>
		/// <param name="arg4">引数4</param>
		/// <param name="arg5">引数5</param>
		/// <param name="arg6">引数6</param>
		/// <param name="arg7">引数7</param>
		/// <typeparam name="T1">引数1の型</typeparam>
		/// <typeparam name="T2">引数2の型</typeparam>
		/// <typeparam name="T3">引数3の型</typeparam>
		/// <typeparam name="T4">引数4の型</typeparam>
		/// <typeparam name="T5">引数5の型</typeparam>
		/// <typeparam name="T6">引数6の型</typeparam>
		/// <typeparam name="T7">引数7の型</typeparam>
		/// <typeparam name="TResult">列挙するレコードの型</typeparam>
		/// <returns>実行可能SQL</returns>
		public FuncCmd<T1, T2, T3, T4, T5, T6, T7, TResult> BuildFunc<T1, T2, T3, T4, T5, T6, T7, TResult>(Argument arg1, Argument arg2, Argument arg3, Argument arg4, Argument arg5, Argument arg6, Argument arg7) {
			var commandable = this.Build();
			var piPrmValue = typeof(Parameter).GetProperty("Value");
			var piArgValue = typeof(Argument).GetProperty("Value");
			var paramsExpr = Expression.Parameter(typeof(Parameter[]));
			var arg1Expr = Expression.Parameter(typeof(T1));
			var arg2Expr = Expression.Parameter(typeof(T2));
			var arg3Expr = Expression.Parameter(typeof(T3));
			var arg4Expr = Expression.Parameter(typeof(T4));
			var arg5Expr = Expression.Parameter(typeof(T5));
			var arg6Expr = Expression.Parameter(typeof(T6));
			var arg7Expr = Expression.Parameter(typeof(T7));
			var blockExpr = Expression.Block(
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg1))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg1Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg2))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg2Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg3))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg3Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg4))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg4Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg5))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg5Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg6))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg6Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg7))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg7Expr, typeof(object)))
			);
			return new FuncCmd<T1, T2, T3, T4, T5, T6, T7, TResult>(commandable, this.Environment.CreateRecordReader<TResult>(null), Expression.Lambda<Action<Parameter[], T1, T2, T3, T4, T5, T6, T7>>(blockExpr, paramsExpr, arg1Expr, arg2Expr, arg3Expr, arg4Expr, arg5Expr, arg6Expr, arg7Expr).Compile());
		}

		/// <summary>
		/// <see cref="IDbCodeCommand"/>に対して実行可能な関数をビルドする
		/// </summary>
		/// <param name="select">SELECT句ノード、これにより列挙するレコード型が推論される</param>
		/// <param name="arg1">引数1</param>
		/// <param name="arg2">引数2</param>
		/// <param name="arg3">引数3</param>
		/// <param name="arg4">引数4</param>
		/// <param name="arg5">引数5</param>
		/// <param name="arg6">引数6</param>
		/// <param name="arg7">引数7</param>
		/// <typeparam name="T1">引数1の型</typeparam>
		/// <typeparam name="T2">引数2の型</typeparam>
		/// <typeparam name="T3">引数3の型</typeparam>
		/// <typeparam name="T4">引数4の型</typeparam>
		/// <typeparam name="T5">引数5の型</typeparam>
		/// <typeparam name="T6">引数6の型</typeparam>
		/// <typeparam name="T7">引数7の型</typeparam>
		/// <typeparam name="TResult">列挙するレコードの型</typeparam>
		/// <returns>実行可能SQL</returns>
		public FuncCmd<T1, T2, T3, T4, T5, T6, T7, TResult> BuildFuncFromSelect<T1, T2, T3, T4, T5, T6, T7, TResult>(ISelect<TResult> select, Argument arg1, Argument arg2, Argument arg3, Argument arg4, Argument arg5, Argument arg6, Argument arg7) {
			var commandable = this.Build();
			var piPrmValue = typeof(Parameter).GetProperty("Value");
			var piArgValue = typeof(Argument).GetProperty("Value");
			var paramsExpr = Expression.Parameter(typeof(Parameter[]));
			var arg1Expr = Expression.Parameter(typeof(T1));
			var arg2Expr = Expression.Parameter(typeof(T2));
			var arg3Expr = Expression.Parameter(typeof(T3));
			var arg4Expr = Expression.Parameter(typeof(T4));
			var arg5Expr = Expression.Parameter(typeof(T5));
			var arg6Expr = Expression.Parameter(typeof(T6));
			var arg7Expr = Expression.Parameter(typeof(T7));
			var blockExpr = Expression.Block(
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg1))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg1Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg2))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg2Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg3))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg3Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg4))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg4Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg5))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg5Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg6))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg6Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg7))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg7Expr, typeof(object)))
			);
			return new FuncCmd<T1, T2, T3, T4, T5, T6, T7, TResult>(commandable, this.Environment.CreateRecordReader<TResult>((from c in @select.ColumnMap select c.Property).ToArray()), Expression.Lambda<Action<Parameter[], T1, T2, T3, T4, T5, T6, T7>>(blockExpr, paramsExpr, arg1Expr, arg2Expr, arg3Expr, arg4Expr, arg5Expr, arg6Expr, arg7Expr).Compile());
		}

		/// <summary>
		/// <see cref="IDbCodeCommand"/>に対して実行するアクションをビルドする
		/// </summary>
		/// <param name="arg1">引数1</param>
		/// <param name="arg2">引数2</param>
		/// <param name="arg3">引数3</param>
		/// <param name="arg4">引数4</param>
		/// <param name="arg5">引数5</param>
		/// <param name="arg6">引数6</param>
		/// <param name="arg7">引数7</param>
		/// <param name="arg8">引数8</param>
		/// <typeparam name="T1">引数1の型</typeparam>
		/// <typeparam name="T2">引数2の型</typeparam>
		/// <typeparam name="T3">引数3の型</typeparam>
		/// <typeparam name="T4">引数4の型</typeparam>
		/// <typeparam name="T5">引数5の型</typeparam>
		/// <typeparam name="T6">引数6の型</typeparam>
		/// <typeparam name="T7">引数7の型</typeparam>
		/// <typeparam name="T8">引数8の型</typeparam>
		/// <returns>実行可能SQL</returns>
		public ActionCmd<T1, T2, T3, T4, T5, T6, T7, T8> BuildAction<T1, T2, T3, T4, T5, T6, T7, T8>(Argument arg1, Argument arg2, Argument arg3, Argument arg4, Argument arg5, Argument arg6, Argument arg7, Argument arg8) {
			var commandable = this.Build();
			var piPrmValue = typeof(Parameter).GetProperty("Value");
			var piArgValue = typeof(Argument).GetProperty("Value");
			var paramsExpr = Expression.Parameter(typeof(Parameter[]));
			var arg1Expr = Expression.Parameter(typeof(T1));
			var arg2Expr = Expression.Parameter(typeof(T2));
			var arg3Expr = Expression.Parameter(typeof(T3));
			var arg4Expr = Expression.Parameter(typeof(T4));
			var arg5Expr = Expression.Parameter(typeof(T5));
			var arg6Expr = Expression.Parameter(typeof(T6));
			var arg7Expr = Expression.Parameter(typeof(T7));
			var arg8Expr = Expression.Parameter(typeof(T8));
			var blockExpr = Expression.Block(
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg1))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg1Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg2))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg2Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg3))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg3Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg4))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg4Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg5))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg5Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg6))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg6Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg7))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg7Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg8))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg8Expr, typeof(object)))
			);
			return new ActionCmd<T1, T2, T3, T4, T5, T6, T7, T8>(commandable, Expression.Lambda<Action<Parameter[], T1, T2, T3, T4, T5, T6, T7, T8>>(blockExpr, paramsExpr, arg1Expr, arg2Expr, arg3Expr, arg4Expr, arg5Expr, arg6Expr, arg7Expr, arg8Expr).Compile());
		}

		/// <summary>
		/// <see cref="IDbCodeCommand"/>に対して実行可能な関数をビルドする
		/// </summary>
		/// <param name="arg1">引数1</param>
		/// <param name="arg2">引数2</param>
		/// <param name="arg3">引数3</param>
		/// <param name="arg4">引数4</param>
		/// <param name="arg5">引数5</param>
		/// <param name="arg6">引数6</param>
		/// <param name="arg7">引数7</param>
		/// <param name="arg8">引数8</param>
		/// <typeparam name="T1">引数1の型</typeparam>
		/// <typeparam name="T2">引数2の型</typeparam>
		/// <typeparam name="T3">引数3の型</typeparam>
		/// <typeparam name="T4">引数4の型</typeparam>
		/// <typeparam name="T5">引数5の型</typeparam>
		/// <typeparam name="T6">引数6の型</typeparam>
		/// <typeparam name="T7">引数7の型</typeparam>
		/// <typeparam name="T8">引数8の型</typeparam>
		/// <typeparam name="TResult">列挙するレコードの型</typeparam>
		/// <returns>実行可能SQL</returns>
		public FuncCmd<T1, T2, T3, T4, T5, T6, T7, T8, TResult> BuildFunc<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Argument arg1, Argument arg2, Argument arg3, Argument arg4, Argument arg5, Argument arg6, Argument arg7, Argument arg8) {
			var commandable = this.Build();
			var piPrmValue = typeof(Parameter).GetProperty("Value");
			var piArgValue = typeof(Argument).GetProperty("Value");
			var paramsExpr = Expression.Parameter(typeof(Parameter[]));
			var arg1Expr = Expression.Parameter(typeof(T1));
			var arg2Expr = Expression.Parameter(typeof(T2));
			var arg3Expr = Expression.Parameter(typeof(T3));
			var arg4Expr = Expression.Parameter(typeof(T4));
			var arg5Expr = Expression.Parameter(typeof(T5));
			var arg6Expr = Expression.Parameter(typeof(T6));
			var arg7Expr = Expression.Parameter(typeof(T7));
			var arg8Expr = Expression.Parameter(typeof(T8));
			var blockExpr = Expression.Block(
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg1))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg1Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg2))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg2Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg3))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg3Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg4))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg4Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg5))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg5Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg6))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg6Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg7))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg7Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg8))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg8Expr, typeof(object)))
			);
			return new FuncCmd<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(commandable, this.Environment.CreateRecordReader<TResult>(null), Expression.Lambda<Action<Parameter[], T1, T2, T3, T4, T5, T6, T7, T8>>(blockExpr, paramsExpr, arg1Expr, arg2Expr, arg3Expr, arg4Expr, arg5Expr, arg6Expr, arg7Expr, arg8Expr).Compile());
		}

		/// <summary>
		/// <see cref="IDbCodeCommand"/>に対して実行可能な関数をビルドする
		/// </summary>
		/// <param name="select">SELECT句ノード、これにより列挙するレコード型が推論される</param>
		/// <param name="arg1">引数1</param>
		/// <param name="arg2">引数2</param>
		/// <param name="arg3">引数3</param>
		/// <param name="arg4">引数4</param>
		/// <param name="arg5">引数5</param>
		/// <param name="arg6">引数6</param>
		/// <param name="arg7">引数7</param>
		/// <param name="arg8">引数8</param>
		/// <typeparam name="T1">引数1の型</typeparam>
		/// <typeparam name="T2">引数2の型</typeparam>
		/// <typeparam name="T3">引数3の型</typeparam>
		/// <typeparam name="T4">引数4の型</typeparam>
		/// <typeparam name="T5">引数5の型</typeparam>
		/// <typeparam name="T6">引数6の型</typeparam>
		/// <typeparam name="T7">引数7の型</typeparam>
		/// <typeparam name="T8">引数8の型</typeparam>
		/// <typeparam name="TResult">列挙するレコードの型</typeparam>
		/// <returns>実行可能SQL</returns>
		public FuncCmd<T1, T2, T3, T4, T5, T6, T7, T8, TResult> BuildFuncFromSelect<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(ISelect<TResult> select, Argument arg1, Argument arg2, Argument arg3, Argument arg4, Argument arg5, Argument arg6, Argument arg7, Argument arg8) {
			var commandable = this.Build();
			var piPrmValue = typeof(Parameter).GetProperty("Value");
			var piArgValue = typeof(Argument).GetProperty("Value");
			var paramsExpr = Expression.Parameter(typeof(Parameter[]));
			var arg1Expr = Expression.Parameter(typeof(T1));
			var arg2Expr = Expression.Parameter(typeof(T2));
			var arg3Expr = Expression.Parameter(typeof(T3));
			var arg4Expr = Expression.Parameter(typeof(T4));
			var arg5Expr = Expression.Parameter(typeof(T5));
			var arg6Expr = Expression.Parameter(typeof(T6));
			var arg7Expr = Expression.Parameter(typeof(T7));
			var arg8Expr = Expression.Parameter(typeof(T8));
			var blockExpr = Expression.Block(
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg1))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg1Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg2))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg2Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg3))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg3Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg4))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg4Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg5))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg5Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg6))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg6Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg7))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg7Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg8))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg8Expr, typeof(object)))
			);
			return new FuncCmd<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(commandable, this.Environment.CreateRecordReader<TResult>((from c in @select.ColumnMap select c.Property).ToArray()), Expression.Lambda<Action<Parameter[], T1, T2, T3, T4, T5, T6, T7, T8>>(blockExpr, paramsExpr, arg1Expr, arg2Expr, arg3Expr, arg4Expr, arg5Expr, arg6Expr, arg7Expr, arg8Expr).Compile());
		}

		/// <summary>
		/// <see cref="IDbCodeCommand"/>に対して実行するアクションをビルドする
		/// </summary>
		/// <param name="arg1">引数1</param>
		/// <param name="arg2">引数2</param>
		/// <param name="arg3">引数3</param>
		/// <param name="arg4">引数4</param>
		/// <param name="arg5">引数5</param>
		/// <param name="arg6">引数6</param>
		/// <param name="arg7">引数7</param>
		/// <param name="arg8">引数8</param>
		/// <param name="arg9">引数9</param>
		/// <typeparam name="T1">引数1の型</typeparam>
		/// <typeparam name="T2">引数2の型</typeparam>
		/// <typeparam name="T3">引数3の型</typeparam>
		/// <typeparam name="T4">引数4の型</typeparam>
		/// <typeparam name="T5">引数5の型</typeparam>
		/// <typeparam name="T6">引数6の型</typeparam>
		/// <typeparam name="T7">引数7の型</typeparam>
		/// <typeparam name="T8">引数8の型</typeparam>
		/// <typeparam name="T9">引数9の型</typeparam>
		/// <returns>実行可能SQL</returns>
		public ActionCmd<T1, T2, T3, T4, T5, T6, T7, T8, T9> BuildAction<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Argument arg1, Argument arg2, Argument arg3, Argument arg4, Argument arg5, Argument arg6, Argument arg7, Argument arg8, Argument arg9) {
			var commandable = this.Build();
			var piPrmValue = typeof(Parameter).GetProperty("Value");
			var piArgValue = typeof(Argument).GetProperty("Value");
			var paramsExpr = Expression.Parameter(typeof(Parameter[]));
			var arg1Expr = Expression.Parameter(typeof(T1));
			var arg2Expr = Expression.Parameter(typeof(T2));
			var arg3Expr = Expression.Parameter(typeof(T3));
			var arg4Expr = Expression.Parameter(typeof(T4));
			var arg5Expr = Expression.Parameter(typeof(T5));
			var arg6Expr = Expression.Parameter(typeof(T6));
			var arg7Expr = Expression.Parameter(typeof(T7));
			var arg8Expr = Expression.Parameter(typeof(T8));
			var arg9Expr = Expression.Parameter(typeof(T9));
			var blockExpr = Expression.Block(
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg1))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg1Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg2))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg2Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg3))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg3Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg4))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg4Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg5))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg5Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg6))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg6Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg7))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg7Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg8))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg8Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg9))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg9Expr, typeof(object)))
			);
			return new ActionCmd<T1, T2, T3, T4, T5, T6, T7, T8, T9>(commandable, Expression.Lambda<Action<Parameter[], T1, T2, T3, T4, T5, T6, T7, T8, T9>>(blockExpr, paramsExpr, arg1Expr, arg2Expr, arg3Expr, arg4Expr, arg5Expr, arg6Expr, arg7Expr, arg8Expr, arg9Expr).Compile());
		}

		/// <summary>
		/// <see cref="IDbCodeCommand"/>に対して実行可能な関数をビルドする
		/// </summary>
		/// <param name="arg1">引数1</param>
		/// <param name="arg2">引数2</param>
		/// <param name="arg3">引数3</param>
		/// <param name="arg4">引数4</param>
		/// <param name="arg5">引数5</param>
		/// <param name="arg6">引数6</param>
		/// <param name="arg7">引数7</param>
		/// <param name="arg8">引数8</param>
		/// <param name="arg9">引数9</param>
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
		/// <returns>実行可能SQL</returns>
		public FuncCmd<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> BuildFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(Argument arg1, Argument arg2, Argument arg3, Argument arg4, Argument arg5, Argument arg6, Argument arg7, Argument arg8, Argument arg9) {
			var commandable = this.Build();
			var piPrmValue = typeof(Parameter).GetProperty("Value");
			var piArgValue = typeof(Argument).GetProperty("Value");
			var paramsExpr = Expression.Parameter(typeof(Parameter[]));
			var arg1Expr = Expression.Parameter(typeof(T1));
			var arg2Expr = Expression.Parameter(typeof(T2));
			var arg3Expr = Expression.Parameter(typeof(T3));
			var arg4Expr = Expression.Parameter(typeof(T4));
			var arg5Expr = Expression.Parameter(typeof(T5));
			var arg6Expr = Expression.Parameter(typeof(T6));
			var arg7Expr = Expression.Parameter(typeof(T7));
			var arg8Expr = Expression.Parameter(typeof(T8));
			var arg9Expr = Expression.Parameter(typeof(T9));
			var blockExpr = Expression.Block(
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg1))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg1Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg2))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg2Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg3))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg3Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg4))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg4Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg5))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg5Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg6))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg6Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg7))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg7Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg8))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg8Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg9))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg9Expr, typeof(object)))
			);
			return new FuncCmd<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(commandable, this.Environment.CreateRecordReader<TResult>(null), Expression.Lambda<Action<Parameter[], T1, T2, T3, T4, T5, T6, T7, T8, T9>>(blockExpr, paramsExpr, arg1Expr, arg2Expr, arg3Expr, arg4Expr, arg5Expr, arg6Expr, arg7Expr, arg8Expr, arg9Expr).Compile());
		}

		/// <summary>
		/// <see cref="IDbCodeCommand"/>に対して実行可能な関数をビルドする
		/// </summary>
		/// <param name="select">SELECT句ノード、これにより列挙するレコード型が推論される</param>
		/// <param name="arg1">引数1</param>
		/// <param name="arg2">引数2</param>
		/// <param name="arg3">引数3</param>
		/// <param name="arg4">引数4</param>
		/// <param name="arg5">引数5</param>
		/// <param name="arg6">引数6</param>
		/// <param name="arg7">引数7</param>
		/// <param name="arg8">引数8</param>
		/// <param name="arg9">引数9</param>
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
		/// <returns>実行可能SQL</returns>
		public FuncCmd<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> BuildFuncFromSelect<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(ISelect<TResult> select, Argument arg1, Argument arg2, Argument arg3, Argument arg4, Argument arg5, Argument arg6, Argument arg7, Argument arg8, Argument arg9) {
			var commandable = this.Build();
			var piPrmValue = typeof(Parameter).GetProperty("Value");
			var piArgValue = typeof(Argument).GetProperty("Value");
			var paramsExpr = Expression.Parameter(typeof(Parameter[]));
			var arg1Expr = Expression.Parameter(typeof(T1));
			var arg2Expr = Expression.Parameter(typeof(T2));
			var arg3Expr = Expression.Parameter(typeof(T3));
			var arg4Expr = Expression.Parameter(typeof(T4));
			var arg5Expr = Expression.Parameter(typeof(T5));
			var arg6Expr = Expression.Parameter(typeof(T6));
			var arg7Expr = Expression.Parameter(typeof(T7));
			var arg8Expr = Expression.Parameter(typeof(T8));
			var arg9Expr = Expression.Parameter(typeof(T9));
			var blockExpr = Expression.Block(
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg1))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg1Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg2))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg2Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg3))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg3Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg4))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg4Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg5))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg5Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg6))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg6Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg7))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg7Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg8))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg8Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg9))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg9Expr, typeof(object)))
			);
			return new FuncCmd<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(commandable, this.Environment.CreateRecordReader<TResult>((from c in @select.ColumnMap select c.Property).ToArray()), Expression.Lambda<Action<Parameter[], T1, T2, T3, T4, T5, T6, T7, T8, T9>>(blockExpr, paramsExpr, arg1Expr, arg2Expr, arg3Expr, arg4Expr, arg5Expr, arg6Expr, arg7Expr, arg8Expr, arg9Expr).Compile());
		}

		/// <summary>
		/// <see cref="IDbCodeCommand"/>に対して実行するアクションをビルドする
		/// </summary>
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
		/// <returns>実行可能SQL</returns>
		public ActionCmd<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> BuildAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Argument arg1, Argument arg2, Argument arg3, Argument arg4, Argument arg5, Argument arg6, Argument arg7, Argument arg8, Argument arg9, Argument arg10) {
			var commandable = this.Build();
			var piPrmValue = typeof(Parameter).GetProperty("Value");
			var piArgValue = typeof(Argument).GetProperty("Value");
			var paramsExpr = Expression.Parameter(typeof(Parameter[]));
			var arg1Expr = Expression.Parameter(typeof(T1));
			var arg2Expr = Expression.Parameter(typeof(T2));
			var arg3Expr = Expression.Parameter(typeof(T3));
			var arg4Expr = Expression.Parameter(typeof(T4));
			var arg5Expr = Expression.Parameter(typeof(T5));
			var arg6Expr = Expression.Parameter(typeof(T6));
			var arg7Expr = Expression.Parameter(typeof(T7));
			var arg8Expr = Expression.Parameter(typeof(T8));
			var arg9Expr = Expression.Parameter(typeof(T9));
			var arg10Expr = Expression.Parameter(typeof(T10));
			var blockExpr = Expression.Block(
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg1))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg1Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg2))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg2Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg3))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg3Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg4))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg4Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg5))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg5Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg6))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg6Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg7))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg7Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg8))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg8Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg9))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg9Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg10))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg10Expr, typeof(object)))
			);
			return new ActionCmd<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(commandable, Expression.Lambda<Action<Parameter[], T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>>(blockExpr, paramsExpr, arg1Expr, arg2Expr, arg3Expr, arg4Expr, arg5Expr, arg6Expr, arg7Expr, arg8Expr, arg9Expr, arg10Expr).Compile());
		}

		/// <summary>
		/// <see cref="IDbCodeCommand"/>に対して実行可能な関数をビルドする
		/// </summary>
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
		/// <returns>実行可能SQL</returns>
		public FuncCmd<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> BuildFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(Argument arg1, Argument arg2, Argument arg3, Argument arg4, Argument arg5, Argument arg6, Argument arg7, Argument arg8, Argument arg9, Argument arg10) {
			var commandable = this.Build();
			var piPrmValue = typeof(Parameter).GetProperty("Value");
			var piArgValue = typeof(Argument).GetProperty("Value");
			var paramsExpr = Expression.Parameter(typeof(Parameter[]));
			var arg1Expr = Expression.Parameter(typeof(T1));
			var arg2Expr = Expression.Parameter(typeof(T2));
			var arg3Expr = Expression.Parameter(typeof(T3));
			var arg4Expr = Expression.Parameter(typeof(T4));
			var arg5Expr = Expression.Parameter(typeof(T5));
			var arg6Expr = Expression.Parameter(typeof(T6));
			var arg7Expr = Expression.Parameter(typeof(T7));
			var arg8Expr = Expression.Parameter(typeof(T8));
			var arg9Expr = Expression.Parameter(typeof(T9));
			var arg10Expr = Expression.Parameter(typeof(T10));
			var blockExpr = Expression.Block(
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg1))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg1Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg2))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg2Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg3))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg3Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg4))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg4Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg5))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg5Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg6))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg6Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg7))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg7Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg8))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg8Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg9))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg9Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg10))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg10Expr, typeof(object)))
			);
			return new FuncCmd<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(commandable, this.Environment.CreateRecordReader<TResult>(null), Expression.Lambda<Action<Parameter[], T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>>(blockExpr, paramsExpr, arg1Expr, arg2Expr, arg3Expr, arg4Expr, arg5Expr, arg6Expr, arg7Expr, arg8Expr, arg9Expr, arg10Expr).Compile());
		}

		/// <summary>
		/// <see cref="IDbCodeCommand"/>に対して実行可能な関数をビルドする
		/// </summary>
		/// <param name="select">SELECT句ノード、これにより列挙するレコード型が推論される</param>
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
		/// <returns>実行可能SQL</returns>
		public FuncCmd<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> BuildFuncFromSelect<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(ISelect<TResult> select, Argument arg1, Argument arg2, Argument arg3, Argument arg4, Argument arg5, Argument arg6, Argument arg7, Argument arg8, Argument arg9, Argument arg10) {
			var commandable = this.Build();
			var piPrmValue = typeof(Parameter).GetProperty("Value");
			var piArgValue = typeof(Argument).GetProperty("Value");
			var paramsExpr = Expression.Parameter(typeof(Parameter[]));
			var arg1Expr = Expression.Parameter(typeof(T1));
			var arg2Expr = Expression.Parameter(typeof(T2));
			var arg3Expr = Expression.Parameter(typeof(T3));
			var arg4Expr = Expression.Parameter(typeof(T4));
			var arg5Expr = Expression.Parameter(typeof(T5));
			var arg6Expr = Expression.Parameter(typeof(T6));
			var arg7Expr = Expression.Parameter(typeof(T7));
			var arg8Expr = Expression.Parameter(typeof(T8));
			var arg9Expr = Expression.Parameter(typeof(T9));
			var arg10Expr = Expression.Parameter(typeof(T10));
			var blockExpr = Expression.Block(
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg1))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg1Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg2))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg2Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg3))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg3Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg4))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg4Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg5))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg5Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg6))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg6Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg7))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg7Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg8))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg8Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg9))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg9Expr, typeof(object))),
				Expression.Assign(Expression.Property(Expression.TypeAs(Expression.Property(Expression.ArrayIndex(paramsExpr, Expression.Constant(commandable.IndexOfArgument(arg10))), piPrmValue), typeof(Argument)), piArgValue), Expression.Convert(arg10Expr, typeof(object)))
			);
			return new FuncCmd<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(commandable, this.Environment.CreateRecordReader<TResult>((from c in @select.ColumnMap select c.Property).ToArray()), Expression.Lambda<Action<Parameter[], T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>>(blockExpr, paramsExpr, arg1Expr, arg2Expr, arg3Expr, arg4Expr, arg5Expr, arg6Expr, arg7Expr, arg8Expr, arg9Expr, arg10Expr).Compile());
		}
	}
}
