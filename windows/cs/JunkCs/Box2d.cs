using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Runtime.InteropServices;

using element = System.Double;

namespace Junk {
	[XmlType("Junk.Box2d")]
	[StructLayout(LayoutKind.Explicit, Pack = 8, Size = 32)]
	public struct Box2d {
		[FieldOffset(0)]
		public element X1;
		[FieldOffset(8)]
		public element Y1;
		[FieldOffset(16)]
		public element X2;
		[FieldOffset(24)]
		public element Y2;

		public Box2d(element x1, element y1, element x2, element y2) {
			this.X1 = x1;
			this.Y1 = y1;
			this.X2 = x2;
			this.Y2 = y2;
		}

		public element this[int i] {
			get {
				switch (i) {
				case 0:
					return X1;
				case 1:
					return Y1;
				case 2:
					return X2;
				case 3:
					return Y2;
				default:
					throw new IndexOutOfRangeException();
				}
			}
			set {
				switch (i) {
				case 0:
					X1 = value;
					break;
				case 1:
					Y1 = value;
					break;
				case 2:
					X2 = value;
					break;
				case 3:
					Y2 = value;
					break;
				default:
					throw new IndexOutOfRangeException();
				}
			}
		}

		public element SizeX {
			get {
				return this.X2 - this.X1;
			}
		}

		public element SizeY {
			get {
				return this.Y2 - this.Y1;
			}
		}

		public Box2d Normalize() {
			var b = this;
			if (b.X2 < b.X1) {
				var t = b.X1;
				b.X1 = b.X2;
				b.X2 = t;
			}
			if (b.Y2 < b.Y1) {
				var t = b.Y1;
				b.Y1 = b.Y2;
				b.Y2 = t;
			}
			return b;
		}

		public Box2d And(Box2d v) {
			return new Box2d(
					this.X1 < v.X1 ? v.X1 : this.X1,
					this.Y1 < v.Y1 ? v.Y1 : this.Y1,
					this.X2 < v.X2 ? this.X2 : v.X2,
					this.Y2 < v.Y2 ? this.Y2 : v.Y2);
		}

		public override bool Equals(object obj) {
			if (obj is Box2d)
				return (Box2d)obj == this;
			else
				return false;
		}

		public override int GetHashCode() {
			return (int)(X1 + Y1 + X2 + Y2);
		}

		static public bool operator ==(Box2d v1, Box2d v2) {
			return v1.X1 == v2.X1 && v1.Y1 == v2.Y1 && v1.X2 == v2.X2 && v1.Y2 == v2.Y2;
		}

		static public bool operator !=(Box2d v1, Box2d v2) {
			return v1.X1 != v2.X1 || v1.Y1 != v2.Y1 || v1.X2 != v2.X2 || v1.Y2 != v2.Y2;
		}
	}
}
