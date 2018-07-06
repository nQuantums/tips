using System;
using System.Xml.Serialization;
using System.Runtime.InteropServices;

using element = System.Single;
using vector = Jk.Vector4f;
using volume = Jk.Obb4f;
using range = Jk.Range4f;
using aabb = Jk.Aabb4f;
using System.Runtime.CompilerServices;

namespace Jk {
	/// <summary>
	/// 方向付き境界ボックス
	/// </summary>
	[XmlType("Jk.Obb4f")]
	[StructLayout(LayoutKind.Explicit, Pack = 4, Size = 96)]
	[Serializable]
	public struct Obb4f : IJsonable {
		[FieldOffset(0)]
		public vector Center;
		[FieldOffset(16)]
		public vector Extents;
		[FieldOffset(32)]
		public vector Ax;
		[FieldOffset(48)]
		public vector Ay;
		[FieldOffset(64)]
		public vector Az;
		[FieldOffset(80)]
		public vector Aw;


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Obb4f(vector center, vector extents, vector ax, vector ay, vector az, vector aw) {
			Center = center;
			Extents = extents;
			Ax = ax;
			Ay = ay;
			Az = az;
			Aw = aw;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Obb4f(Obb4i v) {
			Center = new vector(v.Center);
			Extents = new vector(v.Extents);
			Ax = new vector(v.Ax);
			Ay = new vector(v.Ay);
			Az = new vector(v.Az);
			Aw = new vector(v.Aw);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Obb4f(Obb4d v) {
			Center = new vector(v.Center);
			Extents = new vector(v.Extents);
			Ax = new vector(v.Ax);
			Ay = new vector(v.Ay);
			Az = new vector(v.Az);
			Aw = new vector(v.Aw);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Obb4f(aabb aabb) {
			Center = aabb.Center;
			Extents = aabb.Extents;
			Ax = vector.AxisX;
			Ay = vector.AxisY;
			Az = vector.AxisZ;
			Aw = vector.AxisW;
		}

		public override bool Equals(object obj) {
			if (obj is volume)
				return (volume)obj == this;
			else
				return false;
		}

		public override int GetHashCode() {
			return (Center.GetHashCode()) ^ (Extents.GetHashCode() << 2) ^ (Ax.GetHashCode() >> 2) ^ (Ay.GetHashCode()) ^ (Az.GetHashCode() << 2) ^ (Aw.GetHashCode() >> 2);
		}

		public override string ToString() {
			return string.Concat("{ ", "\"Center\": " + Center, ", ", "\"Extents\": " + Extents, ", ", "\"Ax\": " + Ax, ", ", "\"Ay\": " + Ay, ", ", "\"Az\": " + Az, ", ", "\"Aw\": " + Aw, " }");
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
				return Ax == vector.AxisX && Ay == vector.AxisY && Az == vector.AxisZ && Aw == vector.AxisW;
			}
		}

		public range Range {
			get {
				var c = Center;
				var ext = Extents;
				var eax = Ax * ext.X;
				var eay = Ay * ext.Y;
				var eaz = Az * ext.Z;
				var eaw = Aw * ext.W;
				var r = range.InvalidValue;
				r.MergeSelf(c - eax - eay - eaz - eaw);
				r.MergeSelf(c + eax - eay - eaz - eaw);
				r.MergeSelf(c - eax + eay - eaz - eaw);
				r.MergeSelf(c + eax + eay - eaz - eaw);
				r.MergeSelf(c - eax - eay + eaz - eaw);
				r.MergeSelf(c + eax - eay + eaz - eaw);
				r.MergeSelf(c - eax + eay + eaz - eaw);
				r.MergeSelf(c + eax + eay + eaz - eaw);
				r.MergeSelf(c - eax - eay - eaz + eaw);
				r.MergeSelf(c + eax - eay - eaz + eaw);
				r.MergeSelf(c - eax + eay - eaz + eaw);
				r.MergeSelf(c + eax + eay - eaz + eaw);
				r.MergeSelf(c - eax - eay + eaz + eaw);
				r.MergeSelf(c + eax - eay + eaz + eaw);
				r.MergeSelf(c - eax + eay + eaz + eaw);
				r.MergeSelf(c + eax + eay + eaz + eaw);
				return r;
			}
		}

		public vector P0 {
			get {
				var ext = Extents;
				return Center - Ax * ext.X - Ay * ext.Y - Az * ext.Z - Aw * ext.W;
			}
		}
		public vector P1 {
			get {
				var ext = Extents;
				return Center + Ax * ext.X - Ay * ext.Y - Az * ext.Z - Aw * ext.W;
			}
		}
		public vector P2 {
			get {
				var ext = Extents;
				return Center - Ax * ext.X + Ay * ext.Y - Az * ext.Z - Aw * ext.W;
			}
		}
		public vector P3 {
			get {
				var ext = Extents;
				return Center + Ax * ext.X + Ay * ext.Y - Az * ext.Z - Aw * ext.W;
			}
		}
		public vector P4 {
			get {
				var ext = Extents;
				return Center - Ax * ext.X - Ay * ext.Y + Az * ext.Z - Aw * ext.W;
			}
		}
		public vector P5 {
			get {
				var ext = Extents;
				return Center + Ax * ext.X - Ay * ext.Y + Az * ext.Z - Aw * ext.W;
			}
		}
		public vector P6 {
			get {
				var ext = Extents;
				return Center - Ax * ext.X + Ay * ext.Y + Az * ext.Z - Aw * ext.W;
			}
		}
		public vector P7 {
			get {
				var ext = Extents;
				return Center + Ax * ext.X + Ay * ext.Y + Az * ext.Z - Aw * ext.W;
			}
		}
		public vector P8 {
			get {
				var ext = Extents;
				return Center - Ax * ext.X - Ay * ext.Y - Az * ext.Z + Aw * ext.W;
			}
		}
		public vector P9 {
			get {
				var ext = Extents;
				return Center + Ax * ext.X - Ay * ext.Y - Az * ext.Z + Aw * ext.W;
			}
		}
		public vector P10 {
			get {
				var ext = Extents;
				return Center - Ax * ext.X + Ay * ext.Y - Az * ext.Z + Aw * ext.W;
			}
		}
		public vector P11 {
			get {
				var ext = Extents;
				return Center + Ax * ext.X + Ay * ext.Y - Az * ext.Z + Aw * ext.W;
			}
		}
		public vector P12 {
			get {
				var ext = Extents;
				return Center - Ax * ext.X - Ay * ext.Y + Az * ext.Z + Aw * ext.W;
			}
		}
		public vector P13 {
			get {
				var ext = Extents;
				return Center + Ax * ext.X - Ay * ext.Y + Az * ext.Z + Aw * ext.W;
			}
		}
		public vector P14 {
			get {
				var ext = Extents;
				return Center - Ax * ext.X + Ay * ext.Y + Az * ext.Z + Aw * ext.W;
			}
		}
		public vector P15 {
			get {
				var ext = Extents;
				return Center + Ax * ext.X + Ay * ext.Y + Az * ext.Z + Aw * ext.W;
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
			v = (Ax * v.X) + (Ay * v.Y) + (Az * v.Z) + (Aw * v.W);
			return Math.Abs(v.X) <= Extents.X && Math.Abs(v.Y) <= Extents.Y && Math.Abs(v.Z) <= Extents.Z && Math.Abs(v.W) <= Extents.W;
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IntersectsAsAabb(aabb aabb) {
			var v = Center - aabb.Center;
			var extents = Extents;
			return Math.Abs(v.X) <= extents.X + aabb.Extents.X && Math.Abs(v.Y) <= extents.Y + aabb.Extents.Y && Math.Abs(v.Z) <= extents.Z + aabb.Extents.Z && Math.Abs(v.W) <= extents.W + aabb.Extents.W;
		}

		static public bool operator ==(volume b1, volume b2) {
			return b1.Center == b2.Center && b1.Extents == b2.Extents && b1.Ax == b2.Ax && b1.Ay == b2.Ay && b1.Az == b2.Az && b1.Aw == b2.Aw;
		}

		static public bool operator !=(volume b1, volume b2) {
			return b1.Center != b2.Center || b1.Extents != b2.Extents || b1.Ax != b2.Ax || b1.Ay != b2.Ay || b1.Az != b2.Az || b1.Aw != b2.Aw;
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
