using System;
using System.Collections.Generic;
using System.Text;
using DbCode.Query;

namespace DbCode.Internal {
	/// <summary>
	/// 対応している型ごとに処理を振り分ける
	/// </summary>
	class TypewiseExecutor {
		/// <summary>
		/// 型別の処理を行う
		/// </summary>
		/// <param name="typeWise">型別処理を実際に行うインターフェース</param>
		/// <param name="instance">値</param>
		/// <returns>処理できたら true が返る</returns>
		public static bool Do(ITypeWise typeWise, object instance) {
			if (instance is null) {
				typeWise.DoNull();
				return true;
			}
			if (typeWise.Prepare(instance)) {
				return true;
			}
			switch (instance) {
			case char value:
				typeWise.Do(value);
				break;
			case char[] value:
				typeWise.Do(value);
				break;
			case bool value:
				typeWise.Do(value);
				break;
			case bool[] value:
				typeWise.Do(value);
				break;
			case int value:
				typeWise.Do(value);
				break;
			case int[] value:
				typeWise.Do(value);
				break;
			case long value:
				typeWise.Do(value);
				break;
			case long[] value:
				typeWise.Do(value);
				break;
			case double value:
				typeWise.Do(value);
				break;
			case double[] value:
				typeWise.Do(value);
				break;
			case string value:
				typeWise.Do(value);
				break;
			case string[] value:
				typeWise.Do(value);
				break;
			case Guid value:
				typeWise.Do(value);
				break;
			case Guid[] value:
				typeWise.Do(value);
				break;
			case DateTime value:
				typeWise.Do(value);
				break;
			case DateTime[] value:
				typeWise.Do(value);
				break;
			default: {
					Column column;
					Argument variable;
					if (!((column = instance as Column) is null)) {
						typeWise.Do(column);
					} else if (!((variable = instance as Argument) is null)) {
						typeWise.Do(variable);
					} else {
						return false;
					}
				}
				break;
			}
			return true;
		}
	}
}
