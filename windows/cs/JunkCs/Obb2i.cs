using System;
using System.Xml.Serialization;
using System.Runtime.InteropServices;

using element = System.Int32;
using vector = Jk.Vector2i;
using volume = Jk.Obb2i;
using range = Jk.Range2i;
using aabb = Jk.Aabb2i;
using System.Runtime.CompilerServices;

namespace Jk {
	/// <summary>
	/// 方向付き境界ボックス
	/// </summary>
	[XmlType("Jk.Obb2i")]
	[StructLayout(LayoutKind.Explicit, Pack = 4, Size = 32)]
	[Serializable]
	public struct Obb2i : IJsonable {
		[FieldOffset(0)]
		public vector Center;
		[FieldOffset(8)]
		public vector Extents;
		[FieldOffset(16)]
		public vector Ax;
		[FieldOffset(24)]
		public vector Ay;


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Obb2i(vector center, vector extents, vector ax, vector ay) {
			Center = center;
			Extents = extents;
			Ax = ax;
			Ay = ay;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Obb2i(Obb2f v) {
			Center = new vector(v.Center);
			Extents = new vector(v.Extents);
			Ax = new vector(v.Ax);
			Ay = new vector(v.Ay);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Obb2i(Obb2d v) {
			Center = new vector(v.Center);
			Extents = new vector(v.Extents);
			Ax = new vector(v.Ax);
			Ay = new vector(v.Ay);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Obb2i(aabb aabb) {
			Center = aabb.Center;
			Extents = aabb.Extents;
			Ax = vector.AxisX;
			Ay = vector.AxisY;
		}

		public override bool Equals(object obj) {
			if (obj is volume)
				return (volume)obj == this;
			else
				return false;
		}

		public override int GetHashCode() {
			return (Center.GetHashCode()) ^ (Extents.GetHashCode() << 2) ^ (Ax.GetHashCode() >> 2) ^ (Ay.GetHashCode());
		}

		public override string ToString() {
			return string.Concat("{ ", "\"Center\": " + Center, ", ", "\"Extents\": " + Extents, ", ", "\"Ax\": " + Ax, ", ", "\"Ay\": " + Ay, " }");
		}

		public string ToJsonString() {
			return this.ToString();
		}

		public bool IsValid {
			get {
				return vector.Zero <= Extents;
			}
		}

		public bool IsAabb {
			get {
				return Ax == vector.AxisX && Ay == vector.AxisY;
			}
		}

		public range Range {
			get {
				var c = Center;
				var ext = Extents;
				var eax = Ax * ext.X;
				var eay = Ay * ext.Y;
				var r = range.InvalidValue;
				r.MergeSelf(c - eax - eay);
				r.MergeSelf(c + eax - eay);
				r.MergeSelf(c - eax + eay);
				r.MergeSelf(c + eax + eay);
				return r;
			}
		}

		public vector P0 {
			get {
				var ext = Extents;
				return Center - Ax * ext.X - Ay * ext.Y;
			}
		}
		public vector P1 {
			get {
				var ext = Extents;
				return Center + Ax * ext.X - Ay * ext.Y;
			}
		}
		public vector P2 {
			get {
				var ext = Extents;
				return Center - Ax * ext.X + Ay * ext.Y;
			}
		}
		public vector P3 {
			get {
				var ext = Extents;
				return Center + Ax * ext.X + Ay * ext.Y;
			}
		}

		public vector Size {
			get {
				return Extents * 2;
			}
		}

		public element Perimeter {
			get {
				var size = Extents * 2;
				return 2 * size.Sum();
			}
		}

		public element VolumeAndEdgesLength {
			get {
				var s = Extents * 2;
				return s.Product() + s.Sum();
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Contains(vector v) {
			v.SubSelf(Center);
			v = (Ax * v.X) + (Ay * v.Y);
			return Math.Abs(v.X) <= Extents.X && Math.Abs(v.Y) <= Extents.Y;
		}

		public bool Intersects(volume obb2) {
			volume obb1;
			if (this.IsAabb) {
				obb1 = obb2;
				obb2 = this;
			} else {
				obb1 = this;
			}

			var d = new vector();
			var v = obb1.Center - obb2.Center;

			var eax = obb2.Ax * obb2.Extents.X;
			var eay = obb2.Ay * obb2.Extents.Y;
			var e1 = eax + eay;
			var e2 = eay - eax;
			d.X = Math.Abs(obb1.Ax.Dot(e1));
			d.Y = Math.Abs(obb1.Ax.Dot(e2));
			if (obb1.Extents.X + d.Max() < Math.Abs(obb1.Ax.Dot(v)))
				return false;
			d.X = Math.Abs(obb1.Ay.Dot(e1));
			d.Y = Math.Abs(obb1.Ay.Dot(e2));
			if (obb1.Extents.Y + d.Max() < Math.Abs(obb1.Ay.Dot(v)))
				return false;

			eax = obb1.Ax * obb1.Extents.X;
			eay = obb1.Ay * obb1.Extents.Y;
			e1 = eax + eay;
			e2 = eay - eax;
			d.X = Math.Abs(obb2.Ax.Dot(e1));
			d.Y = Math.Abs(obb2.Ax.Dot(e2));
			if (obb2.Extents.X + d.Max() < Math.Abs(obb2.Ax.Dot(v)))
				return false;
			d.X = Math.Abs(obb2.Ay.Dot(e1));
			d.Y = Math.Abs(obb2.Ay.Dot(e2));
			if (obb2.Extents.Y + d.Max() < Math.Abs(obb2.Ay.Dot(v)))
				return false;

			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IntersectsAsAabb(aabb aabb) {
			var v = Center - aabb.Center;
			var extents = Extents;
			return Math.Abs(v.X) <= extents.X + aabb.Extents.X && Math.Abs(v.Y) <= extents.Y + aabb.Extents.Y;
		}

		static public bool operator ==(volume b1, volume b2) {
			return b1.Center == b2.Center && b1.Extents == b2.Extents && b1.Ax == b2.Ax && b1.Ay == b2.Ay;
		}

		static public bool operator !=(volume b1, volume b2) {
			return b1.Center != b2.Center || b1.Extents != b2.Extents || b1.Ax != b2.Ax || b1.Ay != b2.Ay;
		}

		static public volume operator +(volume b, vector v) {
			b.Center.AddSelf(v);
			return b;
		}

		static public volume operator +(vector v, volume b) {
			b.Center.AddSelf(v);
			return b;
		}

		static public volume operator -(volume b, vector v) {
			b.Center.SubSelf(v);
			return b;
		}
	}
}
