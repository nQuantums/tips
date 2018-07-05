using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using element = System.Int32;
using vector = Jk.Vector2i;
using volume = Jk.Range2i;

namespace Jk {
	/// <summary>
	/// 軸平行境界ボックス
	/// </summary>
	[XmlType("Jk.Range2i")]
	[StructLayout(LayoutKind.Explicit, Pack = 4, Size = 16)]
	[Serializable]
	public struct Range2i : IJsonable {
		public static readonly volume InvalidValue = new volume(vector.MaxValue, vector.MinValue);

		[FieldOffset(0)]
		public vector Min;
		[FieldOffset(8)]
		public vector Max;


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Range2i(vector position) {
			Min = position;
			Max = position;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Range2i(vector min, vector max) {
			Min = min;
			Max = max;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Range2i(vector min, vector max, bool normalize) {
			if (normalize) {
				vector.ElementWiseMinMax(min, max, out Min, out Max);
			} else {
				Min = min;
				Max = max;
			}
		}

		public Range2i(IEnumerable<vector> positions) {
			vector min = vector.MaxValue, max = vector.MinValue;
			foreach(var p in positions) {
				min.ElementWiseMinSelf(p);
				max.ElementWiseMaxSelf(p);
			}
			Min = min;
			Max = max;
		}

		public Range2i(IEnumerable<volume> volumes) {
			vector min = vector.MaxValue, max = vector.MinValue;
			foreach (var v in volumes) {
				min.ElementWiseMinSelf(v.Min);
				max.ElementWiseMaxSelf(v.Max);
			}
			Min = min;
			Max = max;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Range2i(Range2f v) {
			Min = new vector(v.Min);
			Max = new vector(v.Max);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Range2i(Range2d v) {
			Min = new vector(v.Min);
			Max = new vector(v.Max);
		}

		public override bool Equals(object obj) {
			if (obj is volume)
				return (volume)obj == this;
			else
				return false;
		}

		public override int GetHashCode() {
			return Min.GetHashCode() ^ Max.GetHashCode() << 1;
		}

		public override string ToString() {
			return string.Concat("{ ", "\"Min\": " + Min, ", ", "\"Max\": " + Max, " }");
		}

		public string ToJsonString() {
			return this.ToString();
		}

		public bool IsValid {
			get {
				return Min <= Max;
			}
		}

		public vector Size {
			get {
				return Max - Min;
			}
		}

		public vector Center {
			get {
				return (Min + Max) / 2;
			}
		}

		public vector Extents {
			get {
				return (Max - Min) / 2;
			}
		}

		public element Perimeter {
			get {
				var size = Max - Min;
				return 2 * size.Sum();
			}
		}

		public element VolumeAndEdgesLength {
			get {
				var s = Size;
				return s.Product() + s.Sum();
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Contains(vector v) {
			return Min <= v && v <= Max;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Contains(volume range) {
			return Min <= range.Min && range.Max <= Max;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Intersects(volume range) {
			if (Max.X < range.Min.X || range.Max.X < Min.X) return false;
			if (Max.Y < range.Min.Y || range.Max.Y < Min.Y) return false;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public volume Merge(vector v) {
			return new volume(vector.ElementWiseMin(Min, v), vector.ElementWiseMax(Max, v));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public volume Merge(volume range) {
			return new volume(vector.ElementWiseMin(Min, range.Min), vector.ElementWiseMax(Max, range.Max));
		}

		public volume Merge(IEnumerable<vector> positions) {
			var min = Min;
			var max = Max;
			foreach (var p in positions) {
				min.ElementWiseMinSelf(p);
				max.ElementWiseMaxSelf(p);
			}
			return new volume(min, max);
		}

		public volume Merge(IEnumerable<volume> volumes) {
			var min = Min;
			var max = Max;
			foreach (var v in volumes) {
				min.ElementWiseMinSelf(v.Min);
				max.ElementWiseMaxSelf(v.Max);
			}
			return new volume(min, max);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void MergeSelf(vector v) {
			Min.ElementWiseMinSelf(v);
			Max.ElementWiseMaxSelf(v);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void MergeSelf(volume range) {
			Min.ElementWiseMinSelf(range.Min);
			Max.ElementWiseMaxSelf(range.Max);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public volume Expand(element s) {
			return new volume(Min - s, Max + s);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ExpandSelf(element s) {
			Min.SubSelf(s);
			Max.AddSelf(s);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public volume Expand(vector v) {
			return new volume(Min - v, Max + v);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ExpandSelf(vector v) {
			Min.SubSelf(v);
			Max.AddSelf(v);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public bool operator ==(volume b1, volume b2) {
			return b1.Min == b2.Min && b1.Max == b2.Max;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public bool operator !=(volume b1, volume b2) {
			return b1.Min != b2.Min || b1.Max != b2.Max;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public volume operator +(volume b, vector v) {
			return new volume(b.Min + v, b.Max + v);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public volume operator +(vector v, volume b) {
			return new volume(b.Min + v, b.Max + v);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public volume operator -(volume b, vector v) {
			return new volume(b.Min - v, b.Max - v);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public volume operator *(volume b, element s) {
			return new volume(b.Min * s, b.Max * s);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public volume operator /(volume b, element s) {
			return new volume(b.Min / s, b.Max / s);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public volume operator *(volume b, vector v) {
			return new volume(b.Min * v, b.Max * v);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public volume operator *(vector v, volume b) {
			return new volume(b.Min * v, b.Max * v);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public volume operator /(volume b, vector v) {
			return new volume(b.Min / v, b.Max / v);
		}
	}
}
