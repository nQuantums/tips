using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jk {
	/// <summary>
	/// バイナリサーチ関係の関数群
	/// </summary>
	public static class BinarySearch {
		/// <summary>
		/// ソート済みのリスト内から指定値以上の要素が現れる最初のインデックスを検索する
		/// </summary>
		/// <typeparam name="T">要素型</typeparam>
		/// <param name="list">検索対象リスト、ソートされていなければならない</param>
		/// <param name="value">検索対象値</param>
		/// <param name="comp">比較関数</param>
		/// <returns>見つかったらインデックス番号が返る、見つからなかったらリストの要素数と同じ値が返る</returns>
		public static int LowerBoundIndex<T>(IList<T> list, T value, Comparison<T> comp) {
			if (list.Count == 0) return 0;
			int lo = 0, hi = list.Count - 1;
			while (lo < hi) {
				int m = (int)(((long)hi + (long)lo) / 2);
				if (comp(list[m], value) < 0) lo = m + 1;
				else hi = m - 1;
			}
			if (comp(list[lo], value) < 0) lo++;
			return lo;
		}


		/// <summary>
		/// ソート済みのリスト内から指定値以下の要素が現れる最後のインデックスを検索する
		/// </summary>
		/// <typeparam name="T">要素型</typeparam>
		/// <param name="list">検索対象リスト、ソートされていなければならない</param>
		/// <param name="value">検索対象値</param>
		/// <param name="comp">比較関数</param>
		/// <returns>見つかったらインデックス番号が返る、見つからなかったら負数が返る</returns>
		public static int UpperBoundIndex<T>(IList<T> list, T value, Comparison<T> comp) {
			int lo = 0, hi = list.Count - 1;
			while (lo <= hi) {
				int m = (int)(((long)hi + (long)lo) / 2);
				if (comp(list[m], value) <= 0) lo = m + 1;
				else hi = m - 1;
			}
			return hi;
		}
	}
}
