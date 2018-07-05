using System;
using System.Collections.Generic;

using element = System.Single;
using vectori = Jk.Vector2i;
using vector = Jk.Vector2f;
using range = Jk.Range2f;
using rangei = Jk.Range2i;
using aabb = Jk.Aabb2f;
using obb = Jk.Obb2f;

namespace Jk {
	/// <summary>
	/// <see cref="GridSpace2f{T}"/>のアイテムが持たなければならないインターフェース
	/// </summary>
	public interface IGridSpaceItem2f {
		/// <summary>
		/// アイテムの境界ボリュームを取得する
		/// </summary>
		/// <returns>方向付き境界ボックス</returns>
		obb GetVolume();
	}

	/// <summary>
	/// 空間をグリッド状に区切りボリュームを持つアイテムを管理する
	/// </summary>
	/// <typeparam name="T">アイテム型、アイテムは複数のセルに渡り保持され得るので class が無難</typeparam>
	public class GridSpace2f<T> where T : IGridSpaceItem2f {
		#region フィールド
		range _Range;
		vectori _Division;
		vectori _CellMax;
		public List<T>[] _Cells;
		int _Count;
		vector _CellExtent;
		vector _TransformScale;
		vector _TransformTranslate;
		vector _InvTransformScale;
		element _VolumeExpansion;
		#endregion

		#region プロパティ
		/// <summary>
		/// 管理する範囲
		/// </summary>
		public range Range {
			get {
				return _Range;
			}
		}

		/// <summary>
		/// 管理する範囲の分割数
		/// </summary>
		public vectori Division {
			get {
				return _Division;
			}
		}

		/// <summary>
		/// アイテム数
		/// </summary>
		public int Count {
			get {
				return _Count;
			}
		}

		/// <summary>
		/// アイテムの境界ボリュームを拡張するサイズ、計算誤差に対処するためにも使用する
		/// <para>値が変更されると一旦全アイテムを取り除き<see cref="Add(T)"/>で登録しなおす処理が行われる。</para>
		/// </summary>
		public element VolumeExpansion {
			get {
				return _VolumeExpansion;
			}
			set {
				if (_VolumeExpansion == value)
					return;
				_VolumeExpansion = value;
				Rebuild();
			}
		}
		#endregion

		#region 公開メソッド
		/// <summary>
		/// コンストラクタ、管理する範囲と分割数を指定して初期化する
		/// </summary>
		/// <param name="range">管理する範囲</param>
		/// <param name="division">分割数</param>
		public GridSpace2f(range range, vectori division) {
			var size = range.Size;
			if (size.HasZero)
				throw new InvalidOperationException("\"range\" must be non zero size.");
			if (division.HasZero)
				throw new InvalidOperationException("\"division\" must be non zero.");

			var divisionf = new vector(division);

			_Range = range;
			_Division = division;
			_CellMax = division - 1;
			_Cells = new List<T>[division.Y * division.X];
			_CellExtent = size / (divisionf * 2);
			_TransformScale = divisionf / size;
			_TransformTranslate = -range.Min;
			_InvTransformScale = size / divisionf;
		}

		/// <summary>
		/// アイテムを追加する
		/// <para>アイテムの重複チェックは行わない。</para>
		/// </summary>
		/// <param name="item">アイテム</param>
		public void Add(T item) {
			var obb = item.GetVolume();
			obb.Extents += _VolumeExpansion;

			var cells = _Cells;
			foreach (var i in EnumCellIndices(obb, false)) {
				var c = cells[i];
				if (c == null)
					cells[i] = c = new List<T>();
				c.Add(item);
			}
			_Count++;
		}

		/// <summary>
		/// 指定されたアイテムを保有しているか調べる
		/// </summary>
		/// <param name="item">アイテム</param>
		/// <returns>保有しているならtrue</returns>
		public bool Contains(T item) {
			var obb = item.GetVolume();
			obb.Extents += _VolumeExpansion;

			var cells = _Cells;
			foreach (var i in EnumCellIndices(obb, true)) {
				if (cells[i].Contains(item))
					return true;
			}

			return false;
		}

		/// <summary>
		/// 指定されたアイテムを取り除く
		/// </summary>
		/// <param name="item">アイテム</param>
		/// <returns>取り除かれたらtrue</returns>
		public bool Remove(T item) {
			var obb = item.GetVolume();
			obb.Extents += _VolumeExpansion;

			var cells = _Cells;
			var removed = false;
			foreach (var i in EnumCellIndices(obb, true)) {
				removed |= cells[i].Remove(item);
			}

			return removed;
		}

		/// <summary>
		/// 指定範囲に接触するアイテムを列挙する
		/// </summary>
		/// <param name="volume">境界ボリューム</param>
		/// <returns>列挙子</returns>
		public IEnumerable<T> Query(obb volume) {
			return Query(volume, new HashSet<T>());
		}

		/// <summary>
		/// 指定範囲に接触するアイテムを列挙する
		/// </summary>
		/// <param name="volume">境界ボリューム</param>
		/// <param name="excludes">列挙から除外するアイテム一覧、一度列挙されたものはこれに登録される</param>
		/// <returns>列挙子</returns>
		public IEnumerable<T> Query(obb volume, HashSet<T> excludes) {
			var isAabb1 = volume.IsAabb;
			var cells = _Cells;
			foreach (var i in EnumCellIndices(volume, true)) {
				var items = cells[i];
				for (int j = items.Count - 1; j != -1; j--) {
					var item = items[j];
					var obb2 = item.GetVolume();
					if (obb2.IsAabb && isAabb1) {
						var aabb2 = new aabb(obb2.Center, obb2.Extents);
						if (volume.IntersectsAsAabb(aabb2))
							yield return item;
					} else {
						if (volume.Intersects(obb2))
							yield return item;
					}
				}
			}
		}

		/// <summary>
		/// 全セルの軸並行境界ボリュームを取得する
		/// </summary>
		/// <returns>境界ボリューム列</returns>
		public List<Tuple<aabb, int>> GetCellAabb() {
			var result = new List<Tuple<aabb, int>>();
			var cr = new rangei(vectori.Zero, _CellMax);
			var cells = _Cells;
			var stride = _Division.X;
			var iy = cr.Min.X + cr.Min.Y * stride;
			var invs = _InvTransformScale;
			var invt = _CellExtent - _TransformTranslate;
			var aabb2 = new aabb(vector.Zero, _CellExtent);

			for (int y = cr.Min.Y; y <= cr.Max.Y; iy += stride, y++) {
				aabb2.Center.Y = y * invs.Y;
				aabb2.Center.Y += invt.Y;

				for (int x = cr.Min.X, ix = iy; x <= cr.Max.X; ix++, x++) {
					aabb2.Center.X = x * invs.X;
					aabb2.Center.X += invt.X;

					result.Add(new Tuple<aabb, int>(aabb2, ix));
				}
			}

			return result;
		}

		/// <summary>
		/// アイテムが範囲内に一様に分布していると仮定して分割数を計算する
		/// </summary>
		/// <param name="count">アイテム数</param>
		/// <param name="countPerCell">１セルに格納するアイテム数の目安</param>
		/// <returns></returns>
		public static vectori CalcDivisionUniform(int count, int countPerCell = 25) {
			var n = (int)Math.Ceiling(Math.Sqrt((double)count / countPerCell));
			if (n == 0)
				n++;
			return new vectori(n);
		}
		#endregion

		#region 非公開クラス
		/// <summary>
		/// 指定された範囲に対応するセル範囲を取得する
		/// </summary>
		/// <param name="range">範囲</param>
		/// <returns>セル範囲</returns>
		rangei CellRange(range range) {
			var s = _TransformScale;
			var t = _TransformTranslate;
			range.Min.AddSelf(t);
			range.Min.MulSelf(s);
			range.Max.AddSelf(t);
			range.Max.MulSelf(s);

			var cr = new rangei(range);
			cr.Min.ElementWiseMaxSelf(0);
			cr.Max.ElementWiseMinSelf(_CellMax);
			return cr;
		}

		/// <summary>
		/// 指定された範囲に接触するセルインデックスを列挙する
		/// </summary>
		/// <param name="obb">範囲</param>
		/// <param name="skipNullCell">アイテム数ゼロのセルを無視するかどうか</param>
		/// <returns>セルインデックス一覧</returns>
		IEnumerable<int> EnumCellIndices(obb obb, bool skipNullCell) {
			var cr = CellRange(obb.Range);
			var cells = _Cells;
			var stride = _Division.X;
			var iy = cr.Min.X + cr.Min.Y * stride;
			var invs = _InvTransformScale;
			var invt = _CellExtent - _TransformTranslate;
			var isAabb = obb.IsAabb;
			var aabb2 = new aabb(new vector(), _CellExtent);

			for (int y = cr.Min.Y; y <= cr.Max.Y; iy += stride, y++) {
				aabb2.Center.Y = y * invs.Y;
				aabb2.Center.Y += invt.Y;

				for (int x = cr.Min.X, ix = iy; x <= cr.Max.X; ix++, x++) {
					if (skipNullCell) {
						var c = cells[ix];
						if (c == null || c.Count == 0)
							continue;
					}

					aabb2.Center.X = x * invs.X;
					aabb2.Center.X += invt.X;

					if (isAabb) {
						if (obb.IntersectsAsAabb(aabb2))
							yield return ix;
					} else {
						var obb2 = new obb(aabb2);
						if (obb.Intersects(obb2))
							yield return ix;
					}
				}
			}
		}

		/// <summary>
		/// 全アイテムを一旦取り除き登録し直す
		/// </summary>
		void Rebuild() {
			if (_Count == 0)
				return;
			var cache = new HashSet<T>();
			var cells = _Cells;
			for (var i = cells.Length - 1; i != -1; i--) {
				var c = cells[i];
				if (c == null)
					continue;
				for (var j = c.Count - 1; j != -1; j--)
					cache.Add(c[j]);
				cells[i] = null;
			}
			_Count = 0;
			foreach (var item in cache)
				Add(item);
		}
		#endregion
	}
}
