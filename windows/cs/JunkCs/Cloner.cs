using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jk
{
	/// <summary>
	///	クラスインスタンスのディープコピー処理をまとめたクラス。
	/// </summary>
	public static class Cloner
	{
		/// <summary>
		///	指定されたオブジェクトのクローンを作成する。
		/// </summary>
		/// <param name="obj">クローン作成元のオブジェクト、又は null。</param>
		public static T Clone<T>(T obj) where T : ICloneable
		{
			if (obj == null)
				return default(T);
			return (T)obj.Clone();
		}

		/// <summary>
		///	指定された配列のクローンを作成する。各要素もクローンを作成します。
		/// </summary>
		/// <param name="obj">クローン作成元の配列オブジェクト、又は null。</param>
		public static T[] Clone<T>(T[] obj)
		{
			if (obj == null)
				return null;

			Type type = typeof(T);
			T[] retval = (T[])obj.Clone();

			//	T が ICloneable を実装していた場合は各要素もクローンを作成する
			if (type.GetInterface(typeof(ICloneable).Name) != null)
			{
				for (int i = 0, n = retval.Length; i < n; i++)
				{
					if (retval[i] != null)
					{
						retval[i] = (T)((ICloneable)retval[i]).Clone();
					}
				}
			}

			return retval;
		}

		/// <summary>
		///	指定された変数をクローンで置き換える。
		/// </summary>
		/// <param name="obj">クローンで置き換えたいオブジェクト変数、又は null。</param>
		public static void SetClone<T>(ref T obj) where T : ICloneable
		{
			if (obj == null)
				return;
			obj = Clone(obj);
		}
	}
}
