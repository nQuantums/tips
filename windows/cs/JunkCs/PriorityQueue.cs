using System;
using System.Collections.Generic;

namespace Jk {
	/// <summary>
	/// 優先順位キュー、内部構造はバイナリヒープとなっている
	/// </summary>
	/// <typeparam name="T">要素型</typeparam>
	public class PriorityQueue<T> {
		FList<T> _List;
		IComparer<T> _Comparer;

		/// <summary>
		/// キュー内の要素数の取得
		/// </summary>
		public int Count {
			get {
				return _List.Count;
			}
		}

		/// <summary>
		/// 内部リストコレクションの取得
		/// </summary>
		public FList<T> List {
			get {
				return _List;
			}
		}

		/// <summary>
		/// 比較インターフェースの取得
		/// </summary>
		public IComparer<T> Comparer {
			get {
				return _Comparer;
			}
		}

		/// <summary>
		/// コンストラクタ、比較インターフェースを指定して初期化する
		/// </summary>
		/// <param name="comparer">比較インターフェース</param>
		public PriorityQueue(IComparer<T> comparer) : this(comparer, 0, null) {
		}

		/// <summary>
		/// コンストラクタ、比較インターフェースを指定して初期化する
		/// </summary>
		/// <param name="comparer">比較インターフェース</param>
		/// <param name="capacity">初期キャパシティ</param>
		public PriorityQueue(IComparer<T> comparer, int capacity) : this(comparer, capacity, null) {
		}

		/// <summary>
		/// コンストラクタ、比較インターフェースと初期項目を指定して初期化する
		/// </summary>
		/// <param name="comparer">比較インターフェース</param>
		/// <param name="initialValues">初期項目</param>
		public PriorityQueue(IComparer<T> comparer, IEnumerable<T> initialValues) : this(comparer, 0, initialValues) {
		}

		/// <summary>
		/// コンストラクタ、比較インターフェースと初期項目を指定して初期化する
		/// </summary>
		/// <param name="comparer">比較インターフェース</param>
		/// <param name="capacity">初期キャパシティ</param>
		/// <param name="initialValues">初期項目</param>
		public PriorityQueue(IComparer<T> comparer, int capacity, IEnumerable<T> initialValues) {
			_List = capacity != 0 ? new FList<T>(capacity) : new FList<T>();
			_Comparer = comparer;
			if(initialValues != null)
				_List.AddRange(initialValues);
			UpdateHeap();
		}

		/// <summary>
		/// キューに値を追加する
		/// </summary>
		/// <param name="value">値</param>
		public void Push(T value) {
			PushHeap(_List, value, _Comparer);
		}

		/// <summary>
		/// キューから優先順位が最大の値を取り出す
		/// </summary>
		/// <returns>値</returns>
		public T Pop() {
			return PopHeap(_List, _Comparer);
		}

		/// <summary>
		/// キュー内の優先順位が最大の値を取得する
		/// </summary>
		/// <returns>値</returns>
		public T Peek() {
			return _List[0];
		}

		/// <summary>
		/// 全要素を取り除く
		/// </summary>
		public void Clear() {
			_List.Clear();
		}

		/// <summary>
		/// 指定値がコレクション内に存在するか調べる
		/// </summary>
		/// <param name="value">値</param>
		/// <returns>存在するなら true 、それ以外は false</returns>
		public bool Contains(T value) {
			return _List.Contains(value);
		}

		/// <summary>
		/// 内部リストをバイナリヒープとして構成し直す、要素の優先順位変更後などに呼び出す必要がある
		/// </summary>
		public void UpdateHeap() {
			MakeHeap(_List, _Comparer);
		}

		/// <summary>
		/// コレクションを配列に変換する
		/// </summary>
		/// <returns>配列</returns>
		public T[] ToArray() {
			return _List.ToArray();
		}

		/// <summary>
		/// 指定されたリスト内要素をバイナリヒープになるよう並び替える
		/// </summary>
		/// <param name="list">並び替えられるリスト</param>
		/// <param name="comparer">比較インターフェース</param>
		public static void MakeHeap(FList<T> list, IComparer<T> comparer) {
			var last = list.Count;
			if (2 <= last) {
				for (var hole = last / 2; 0 < hole;) {
					--hole;
					PopHeapHoleByIndex(list, hole, last, list[hole], comparer);
				}
			}
		}

		/// <summary>
		/// 指定されたバイナリヒープリストへ値を追加する
		/// </summary>
		/// <param name="list">追加先リスト、バイナリヒープになっている必要がある</param>
		/// <param name="value">追加値</param>
		/// <param name="comparer">比較インターフェース</param>
		public static void PushHeap(FList<T> list, T value, IComparer<T> comparer) {
			list.Add(value);
			var last = list.Count;
			if (2 <= last) {
				--last;
				PushHeapByIndex(list, last, 0, list[last], comparer);
			}
		}

		/// <summary>
		/// 指定されたバイナリヒープリストから優先順位が最大の値を取り出す
		/// </summary>
		/// <param name="list">削除元リスト、バイナリヒープになっている必要がある</param>
		/// <param name="comparer">比較インターフェース</param>
		/// <returns>取り出された値</returns>
		public static T PopHeap(FList<T> list, IComparer<T> comparer) { // pop *_First to *(_Last - 1) and reheap, using _Pred
			var top = list[0];
			var last = list.Count - 1;
			if (1 <= last) {
				PopHeapHoleByIndex(list, 0, last, list[last], comparer);
			}
			list.RemoveAt(last);
			return top;
		}


		/// <summary>
		/// 指定されたバイナリヒープリストに値を追加する
		/// </summary>
		/// <param name="list">追加先リスト、バイナリヒープになっている必要がある</param>
		/// <param name="hole">追加先リスト内でのバイナリヒープを満たしていない可能性がある項目インデックス</param>
		/// <param name="top">追加先リスト内での順序並び替え開始インデックス</param>
		/// <param name="val">追加値</param>
		/// <param name="comparer">比較インターフェース</param>
		static void PushHeapByIndex(FList<T> list, int hole, int top, T val, IComparer<T> comparer) {
			// hole を親へ移動していく
			for (var idx = (hole - 1) / 2; top < hole && comparer.Compare(list[idx], val) < 0; idx = (hole - 1) / 2) {
				list[hole] = list[idx];
				hole = idx;
			}
			// 最後の hole に追加値を設定
			list[hole] = val;
		}

		/// <summary>
		/// 指定されたバイナリヒープリストから指定インデックスの値を削除し、空いた穴を最終インデックスへ移動する
		/// </summary>
		/// <param name="list">削除元リスト、バイナリヒープになっている必要がある</param>
		/// <param name="hole">削除元リスト内での削除するインデックス</param>
		/// <param name="bottom">削除元リスト内での最終要素インデックス、0が指定されてはならない</param>
		/// <param name="val">削除元リスト内の最終要素オリジナル値、削除後に適切なインデックスへ移動される</param>
		/// <param name="comparer">比較インターフェース</param>
		static void PopHeapHoleByIndex(FList<T> list, int hole, int bottom, T val, IComparer<T> comparer) {
			var top = hole;
			var idx = hole;

			// Check whether _Idx can have a child before calculating that child's index, since
			// calculating the child's index can trigger integer overflows
			var maxSequenceNonLeaf = (bottom - 1) / 2;
			while (idx < maxSequenceNonLeaf) {
				// hole を大きい方の子へ移動していく
				idx = 2 * idx + 2;
				if (comparer.Compare(list[idx], list[idx - 1]) < 0)
					--idx;
				list[hole] = list[idx];
				hole = idx;
			}

			if (idx == maxSequenceNonLeaf && bottom % 2 == 0) {   // only child at bottom, move _Hole down to it
				list[hole] = list[bottom - 1];
				hole = bottom - 1;
			}

			PushHeapByIndex(list, hole, top, val, comparer);
		}
	}
}
