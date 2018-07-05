using System;
using System.Xml.Serialization;
using System.Runtime.InteropServices;

using element = System.Double;
using vector = Jk.Vector2d;
using volume = Jk.Aabb2d;
using range = Jk.Range2d;

namespace Jk {
	/// <summary>
	/// 軸並行境界ボックス
	/// </summary>
	[XmlType("Jk.Aabb2d")]
	[StructLayout(LayoutKind.Explicit, Pack = 8, Size = 32)]
	[Serializable]
	public struct Aabb2d : IJsonable {
		[FieldOffset(0)]
		public vector Center;
		[FieldOffset(16)]
		public vector Extents;


		public Aabb2d(vector center, vector extents) {
			Center = center;
			Extents = extents;
		}

		public Aabb2d(Aabb2i v) {
			Center = new vector(v.Center);
			Extents = new vector(v.Extents);
		}
		public Aabb2d(Aabb2f v) {
			Center = new vector(v.Center);
			Extents = new vector(v.Extents);
		}

		public override bool Equals(object obj) {
			if (obj is volume)
				return (volume)obj == this;
			else
				return false;
		}

		public override int GetHashCode() {
			return (Center.GetHashCode()) ^ (Extents.GetHashCode() << 2);
		}

		public override string ToString() {
			return string.Concat("{ ", "\"Center\": " + Center, ", ", "\"Extents\": " + Extents, " }");
		}

		public string ToJsonString() {
			return this.ToString();
		}

		public bool IsValid {
			get {
				return vector.Zero <= Extents;
			}
		}

		public range Range {
			get {
				var ext = Extents;
				var c = Center;
				return new range(c - ext, c + ext);
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

		public bool Contains(vector v) {
			v.SubSelf(Center);
			return Math.Abs(v.X) <= Extents.X && Math.Abs(v.Y) <= Extents.Y;
		}

		public bool Intersects(volume aabb) {
			var v = Center - aabb.Center;
			var extents = Extents;
			return Math.Abs(v.X) <= extents.X + aabb.Extents.X && Math.Abs(v.Y) <= extents.Y + aabb.Extents.Y;
		}

		static public bool operator ==(volume b1, volume b2) {
			return b1.Center == b2.Center && b1.Extents == b2.Extents;
		}

		static public bool operator !=(volume b1, volume b2) {
			return b1.Center != b2.Center || b1.Extents != b2.Extents;
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
