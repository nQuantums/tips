using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Jk {
	/// <summary>
	/// 一切引数チェックをしない危険なリスト、但しサイズは小さく速度は速い、しかし foreach は遅い
	/// </summary>
	/// <typeparam name="T">要素型</typeparam>
	public class FList<T> : IList<T>, IJsonable {
		#region 内部クラス
		public struct CoreElement {
			public T[] Items;
			public int Count;

			public CoreElement(T[] items, int count) {
				this.Items = items;
				this.Count = count;
			}
		}

		public class InternalEnumerator : IEnumerator<T>, System.Collections.IEnumerator {
			T[] _Items;
			int _Count;
			int _Index;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public InternalEnumerator(T[] items, int count) {
				_Items = items;
				_Count = count;
				_Index = -1;
			}

			public T Current {
				get {
					return _Items[_Index];
				}
			}

			object IEnumerator.Current {
				get {
					return _Items[_Index];
				}
			}

			public void Dispose() {
			}

			public bool MoveNext() {
				return ++_Index != _Count;
			}

			public void Reset() {
				_Count = -1;
			}
		}
		#endregion

		#region フィールド
		const int InitialCapacity = 4;
		const int GrowRate = 2;
		static readonly T[] EmptyArray = new T[0];

		T[] _Items;
		int _Count;
		#endregion

		#region プロパティ
		public CoreElement Core {
			get {
				return new CoreElement(_Items, _Count);
			}
		}

		public T this[int index] {
			get {
				return _Items[index];
			}
			set {
				_Items[index] = value;
			}
		}

		public int Capacity {
			get {
				return _Items.Length;
			}
			set {
				Array.Resize(ref _Items, value);
			}
		}

		public int Count {
			get {
				return _Count;
			}
		}

		public bool IsReadOnly {
			get {
				return false;
			}
		}
		#endregion

		#region 公開メソッド
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public FList() {
			_Items = EmptyArray;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public FList(FList<T> list) {
			if (list._Items == EmptyArray) {
				_Items = EmptyArray;
			} else {
				var count = list._Count;
				var items = new T[count];
				Array.Copy(list._Items, items, count);
				_Items = items;
				_Count = count;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public FList(int capacity) {
			_Items = new T[capacity];
		}

		public FList(IEnumerable<T> collection) {
			var collection2 = collection as ICollection<T>;
			if (collection2 != null) {
				var count = collection2.Count;
				if (count == 0) {
					_Items = EmptyArray;
					return;
				}
				_Items = new T[count];
				collection2.CopyTo(this._Items, 0);
				_Count = count;
			} else {
				_Count = 0;
				_Items = EmptyArray;
				using (IEnumerator<T> enumerator = collection.GetEnumerator()) {
					while (enumerator.MoveNext()) {
						this.Add(enumerator.Current);
					}
				}
			}
		}

		public void Add(T item) {
			var len = _Items.Length;
			if (len == _Count)
				Array.Resize(ref _Items, len == 0 ? InitialCapacity : len * GrowRate);
			_Items[_Count++] = item;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void AddRange(IEnumerable<T> collection) {
			this.InsertRange(_Count, collection);
		}

		public void AddRange(CoreElement core) {
			if (core.Count == 0)
				return;

			var count = _Count;
			var requiredCount = count + core.Count;
			var len = _Items.Length;
			if (len < requiredCount) {
				var newLen = len == 0 ? InitialCapacity : len * GrowRate;
				if (newLen < requiredCount)
					newLen = requiredCount;
				Array.Resize(ref _Items, newLen);
			}

			Array.Copy(core.Items, 0, _Items, count, core.Count);

			_Count += core.Count;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Clear() {
			_Count = 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Contains(T item) {
			return 0 <= Array.IndexOf(_Items, item, 0, _Count);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void CopyTo(T[] array, int arrayIndex) {
			Array.Copy(_Items, 0, array, arrayIndex, _Count);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public IEnumerator<T> GetEnumerator() {
			return new InternalEnumerator(_Items, _Count);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int IndexOf(T item) {
			return Array.IndexOf(_Items, item, 0, _Count);
		}

		public void Insert(int index, T item) {
			var len = _Items.Length;
			var count = _Count;
			if (len == count)
				Array.Resize(ref _Items, len == 0 ? InitialCapacity : len * GrowRate);
			if (index < count)
				Array.Copy(_Items, index, _Items, index + 1, count - index);
			_Items[index] = item;
		}

		public void InsertRange(int index, IEnumerable<T> collection) {
			var collection2 = collection as ICollection<T>;
			if (collection2 != null) {
				int insertCount = collection2.Count;
				if (insertCount > 0) {
					var count = _Count;
					var requiredCount = count + insertCount;
					var len = _Items.Length;
					if (len < requiredCount) {
						var newLen = len == 0 ? InitialCapacity : len * GrowRate;
						if (newLen < requiredCount)
							newLen = requiredCount;
						Array.Resize(ref _Items, newLen);
					}

					if (index < count) {
						Array.Copy(_Items, index, _Items, index + insertCount, count - index);
					}
					if (this == collection2) {
						Array.Copy(_Items, 0, _Items, index, index);
						Array.Copy(_Items, index + insertCount, _Items, index * 2, count - index);
					} else {
						collection2.CopyTo(_Items, index);
					}
					_Count += insertCount;
				}
			} else {
				using (var enumerator = collection.GetEnumerator()) {
					while (enumerator.MoveNext()) {
						this.Insert(index++, enumerator.Current);
					}
				}
			}
		}

		public bool Remove(T item) {
			var index = Array.IndexOf(_Items, item, 0, _Count);
			if (index < 0)
				return false;
			this.RemoveAt(index);
			return true;
		}

		public void RemoveAt(int index) {
			_Count--;
			if (index < _Count)
				Array.Copy(_Items, index + 1, _Items, index, _Count - index);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Reverse() {
			Array.Reverse(_Items, 0, _Count);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Reverse(int index, int count) {
			Array.Reverse(_Items, index, count);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Sort(int index, int count, IComparer<T> comparer) {
			Array.Sort<T>(_Items, index, count, comparer);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Sort(IComparer<T> comparer) {
			Array.Sort<T>(_Items, 0, _Count, comparer);
		}

		public T[] ToArray() {
			T[] array = new T[_Count];
			Array.Copy(_Items, 0, array, 0, _Count);
			return array;
		}

		public override string ToString() {
			var sb = new StringBuilder();
			var items = _Items;
			var count = _Count;
			if (1000 < count)
				count = 1000;
			sb.Append("[ ");
			for (int i = 0; i < count; i++) {
				if (i != 0)
					sb.AppendLine(",");
				sb.Append(Jsonable.ToString(items[i]));
			}
			if (count < _Count) {
				sb.AppendLine(",");
				sb.AppendLine("{ \"AndMore\": \"...\" }");
			}
			sb.Append(" ]");
			return sb.ToString();
		}

		public string ToJsonString() {
			var sb = new StringBuilder();
			var items = _Items;
			var count = _Count;
			sb.Append("[ ");
			for (int i = 0; i < count; i++) {
				if (i != 0)
					sb.AppendLine(",");
				sb.Append(Jsonable.ToString(items[i]));
			}
			sb.Append(" ]");
			return sb.ToString();
		}
		#endregion

		#region 非公開メソッド
		IEnumerator IEnumerable.GetEnumerator() {
			return this.GetEnumerator();
		}
		#endregion
	}
}
