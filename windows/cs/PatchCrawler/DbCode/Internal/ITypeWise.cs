using System;
using System.Collections.Generic;
using System.Text;
using DbCode.Query;

namespace DbCode.Internal {
	/// <summary>
	/// 型ごとの処理を行う
	/// </summary>
	interface ITypeWise {
		/// <summary>
		/// null が指定された際の処理
		/// </summary>
		void DoNull();

		/// <summary>
		/// 型別の処理に入る前の処理
		/// </summary>
		/// <param name="value">値</param>
		/// <returns>true を返すと型別の処理へ進まない</returns>
		bool Prepare(object value);

		void Do(char value);
		void Do(char[] value);
		void Do(bool value);
		void Do(bool[] value);
		void Do(int value);
		void Do(int[] value);
		void Do(long value);
		void Do(long[] value);
		void Do(double value);
		void Do(double[] value);
		void Do(string value);
		void Do(string[] value);
		void Do(Guid value);
		void Do(Guid[] value);
		void Do(DateTime value);
		void Do(DateTime[] value);
		void Do(Column value);
		void Do(Argument value);
	}
}
