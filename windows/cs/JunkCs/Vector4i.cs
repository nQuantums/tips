using System;
using System.Xml.Serialization;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using element = System.Int32;
using vector = Jk.Vector4i;

namespace Jk {
	[XmlType("Jk.Vector4i")]
	[StructLayout(LayoutKind.Explicit, Pack = 4, Size = 16)]
	[Serializable]
	public struct Vector4i : IJsonable {
		public static readonly vector Zero = new vector();
		public static readonly vector AxisX = new vector(1, 0, 0, 0);
		public static readonly vector AxisY = new vector(0, 1, 0, 0);
		public static readonly vector AxisZ = new vector(0, 0, 1, 0);
		public static readonly vector AxisW = new vector(0, 0, 0, 1);
		public static readonly vector MinValue = new vector(element.MinValue, element.MinValue, element.MinValue, element.MinValue);
		public static readonly vector MaxValue = new vector(element.MaxValue, element.MaxValue, element.MaxValue, element.MaxValue);

		[FieldOffset(0)]
		public element X;
		[FieldOffset(4)]
		public element Y;
		[FieldOffset(8)]
		public element Z;
		[FieldOffset(12)]
		public element W;


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector4i(element s) {
			X = s;
			Y = s;
			Z = s;
			W = s;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector4i(element x, element y, element z, element w) {
			X = x;
			Y = y;
			Z = z;
			W = w;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector4i(Vector4f v) {
			X = (element)v.X;
			Y = (element)v.Y;
			Z = (element)v.Z;
			W = (element)v.W;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector4i(Vector4d v) {
			X = (element)v.X;
			Y = (element)v.Y;
			Z = (element)v.Z;
			W = (element)v.W;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector4i(element[] arr) {
			X = arr[0];
			Y = arr[1];
			Z = arr[2];
			W = arr[3];
		}

		public element this[int i] {
			get {
				switch (i) {
				case 0: return X;
				case 1: return Y;
				case 2: return Z;
				case 3: return W;
				default: throw new IndexOutOfRangeException();
				}
			}
			set {
				switch (i) {
				case 0: X = value; break;
				case 1: Y = value; break;
				case 2: Z = value; break;
				case 3: W = value; break;
				default: throw new IndexOutOfRangeException();
				}
			}
		}

		public bool IsZero {
			get {
				return X == 0 && Y == 0 && Z == 0 && W == 0;
			}
		}

		public bool HasZero {
			get {
				return X == 0 || Y == 0 || Z == 0 || W == 0;
			}
		}

		public override bool Equals(object obj) {
			if (obj is vector)
				return (vector)obj == this;
			else
				return false;
		}

		public override int GetHashCode() {
			return (X.GetHashCode()) ^ (Y.GetHashCode() << 2) ^ (Z.GetHashCode() >> 2) ^ (W.GetHashCode());
		}

		public override string ToString() {
			return string.Concat("{ ", "\"X\": " + X, ", ", "\"Y\": " + Y, ", ", "\"Z\": " + Z, ", ", "\"W\": " + W, " }");
		}

		public string ToString(string f) {
			return string.Concat("{ ", "\"X\": " + X.ToString(f), ", ", "\"Y\": " + Y.ToString(f), ", ", "\"Z\": " + Z.ToString(f), ", ", "\"W\": " + W.ToString(f), " }");
		}

		public string ToJsonString() {
			return this.ToString();
		}

		public element LengthSquare {
			get { return X * X + Y * Y + Z * Z + W * W; }
		}

		public element Length {
			get { return (element)Math.Sqrt(LengthSquare); }
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void NormalizeSelf() {
			var l = LengthSquare;
			if (l == 0 || l == 1)
				return;
			l = 1 / (element)Math.Sqrt(l);
			X *= l; Y *= l; Z *= l; W *= l;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public vector Normalize() {
			var l = LengthSquare;
			if (l == 0 || l == 1)
				return this;
			l = 1 / (element)Math.Sqrt(l);
			return new vector(X * l, Y * l, Z * l, W * l);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void RelengthSelf(element length) {
			var l = LengthSquare;
			if (l == 0 || l == length)
				return;
			l = length / (element)Math.Sqrt(l);
			X *= l; Y *= l; Z *= l; W *= l;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public vector Relength(element length) {
			var l = LengthSquare;
			if (l == 0 || l == length)
				return this;
			l = length / (element)Math.Sqrt(l);
			return new vector(X * l, Y * l, Z * l, W * l);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void AddSelf(element s) {
			X += s;
			Y += s;
			Z += s;
			W += s;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void AddSelf(vector v) {
			X += v.X;
			Y += v.Y;
			Z += v.Z;
			W += v.W;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SubSelf(element s) {
			X -= s;
			Y -= s;
			Z -= s;
			W -= s;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SubSelf(vector v) {
			X -= v.X;
			Y -= v.Y;
			Z -= v.Z;
			W -= v.W;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void MulSelf(element s) {
			X *= s;
			Y *= s;
			Z *= s;
			W *= s;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void MulSelf(vector v) {
			X *= v.X;
			Y *= v.Y;
			Z *= v.Z;
			W *= v.W;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void DivSelf(element s) {
			X /= s;
			Y /= s;
			Z /= s;
			W /= s;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void DivSelf(vector v) {
			X /= v.X;
			Y /= v.Y;
			Z /= v.Z;
			W /= v.W;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ClipSelf(element min, element max) {
			if (X < min) X = min; else if (max < X) X = max;
			if (Y < min) Y = min; else if (max < Y) Y = max;
			if (Z < min) Z = min; else if (max < Z) Z = max;
			if (W < min) W = min; else if (max < W) W = max;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public vector Clip(element min, element max) {
			var v = this;
			if (v.X < min) v.X = min; else if (max < v.X) v.X = max;
			if (v.Y < min) v.Y = min; else if (max < v.Y) v.Y = max;
			if (v.Z < min) v.Z = min; else if (max < v.Z) v.Z = max;
			if (v.W < min) v.W = min; else if (max < v.W) v.W = max;
			return v;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ClipSelf(vector min, vector max) {
			if (X < min.X) X = min.X; else if (max.X < X) X = max.X;
			if (Y < min.Y) Y = min.Y; else if (max.Y < Y) Y = max.Y;
			if (Z < min.Z) Z = min.Z; else if (max.Z < Z) Z = max.Z;
			if (W < min.W) W = min.W; else if (max.W < W) W = max.W;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public vector Clip(vector min, vector max) {
			vector v = this;
			if (v.X < min.X) v.X = min.X; else if (max.X < v.X) v.X = max.X;
			if (v.Y < min.Y) v.Y = min.Y; else if (max.Y < v.Y) v.Y = max.Y;
			if (v.Z < min.Z) v.Z = min.Z; else if (max.Z < v.Z) v.Z = max.Z;
			if (v.W < min.W) v.W = min.W; else if (max.W < v.W) v.W = max.W;
			return v;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void AbsSelf() {
			X = Math.Abs(X);
			Y = Math.Abs(Y);
			Z = Math.Abs(Z);
			W = Math.Abs(W);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public vector Abs() {
			return new vector(Math.Abs(X), Math.Abs(Y), Math.Abs(Z), Math.Abs(W));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public element Sum() {
			return X + Y + Z + W;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public element Product() {
			return X * Y * Z * W;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public element Max() {
			var m = X;
			if (Y > m) m = Y;
			if (Z > m) m = Z;
			if (W > m) m = W;
			return m;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public element Min() {
			var m = X;
			if (Y < m) m = Y;
			if (Z < m) m = Z;
			if (W < m) m = W;
			return m;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ElementWiseMinSelf(element s) {
			if (s < X) X = s;
			if (s < Y) Y = s;
			if (s < Z) Z = s;
			if (s < W) W = s;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ElementWiseMaxSelf(element s) {
			if (s > X) X = s;
			if (s > Y) Y = s;
			if (s > Z) Z = s;
			if (s > W) W = s;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ElementWiseMinSelf(vector v) {
			if (v.X < X) X = v.X;
			if (v.Y < Y) Y = v.Y;
			if (v.Z < Z) Z = v.Z;
			if (v.W < W) W = v.W;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ElementWiseMaxSelf(vector v) {
			if (v.X > X) X = v.X;
			if (v.Y > Y) Y = v.Y;
			if (v.Z > Z) Z = v.Z;
			if (v.W > W) W = v.W;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public element Dot(vector v) {
			return X * v.X + Y * v.Y + Z * v.Z + W * v.W;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public vector Cross(vector v) {
			return new vector(Y * v.Z - Z * v.Y, Z * v.W - W * v.Z, W * v.X - X * v.W, X * v.Y - Y * v.X);
		}


		public bool LessIdThan(vector v) {
			if (X < v.X) return true;
			if (X > v.X) return false;
			if (Y < v.Y) return true;
			if (Y > v.Y) return false;
			if (Z < v.Z) return true;
			if (Z > v.Z) return false;
			if (W < v.W) return true;
			if (W > v.W) return false;
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public bool operator ==(vector v1, vector v2) {
			return v1.X == v2.X && v1.Y == v2.Y && v1.Z == v2.Z && v1.W == v2.W;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public bool operator !=(vector v1, vector v2) {
			return v1.X != v2.X || v1.Y != v2.Y || v1.Z != v2.Z || v1.W != v2.W;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public bool operator <(vector v1, vector v2) {
			if (v1.X >= v2.X) return false;
			if (v1.Y >= v2.Y) return false;
			if (v1.Z >= v2.Z) return false;
			if (v1.W >= v2.W) return false;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public bool operator >(vector v1, vector v2) {
			if (v1.X <= v2.X) return false;
			if (v1.Y <= v2.Y) return false;
			if (v1.Z <= v2.Z) return false;
			if (v1.W <= v2.W) return false;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public bool operator <=(vector v1, vector v2) {
			if (v1.X > v2.X) return false;
			if (v1.Y > v2.Y) return false;
			if (v1.Z > v2.Z) return false;
			if (v1.W > v2.W) return false;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public bool operator >=(vector v1, vector v2) {
			if (v1.X < v2.X) return false;
			if (v1.Y < v2.Y) return false;
			if (v1.Z < v2.Z) return false;
			if (v1.W < v2.W) return false;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public vector operator +(vector v) {
			return v;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public vector operator -(vector v) {
			return new vector(-v.X, -v.Y, -v.Z, -v.W);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public vector operator +(vector v1, vector v2) {
			return new vector(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z, v1.W + v2.W);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public vector operator -(vector v1, vector v2) {
			return new vector(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z, v1.W - v2.W);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public vector operator +(vector v, element s) {
			return new vector(v.X + s, v.Y + s, v.Z + s, v.W + s);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public vector operator +(element s, vector v) {
			return new vector(s + v.X, s + v.Y, s + v.Z, s + v.W);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public vector operator -(vector v, element s) {
			return new vector(v.X - s, v.Y - s, v.Z - s, v.W - s);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public vector operator -(element s, vector v) {
			return new vector(s - v.X, s - v.Y, s - v.Z, s - v.W);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public vector operator *(vector v, element s) {
			return new vector(v.X * s, v.Y * s, v.Z * s, v.W * s);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public vector operator *(element s, vector v) {
			return new vector(s * v.X, s * v.Y, s * v.Z, s * v.W);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public vector operator /(vector v, element s) {
			return new vector(v.X / s, v.Y / s, v.Z / s, v.W / s);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public vector operator *(vector v1, vector v2) {
			return new vector(v1.X * v2.X, v1.Y * v2.Y, v1.Z * v2.Z, v1.W * v2.W);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public vector operator /(vector v1, vector v2) {
			return new vector(v1.X / v2.X, v1.Y / v2.Y, v1.Z / v2.Z, v1.W / v2.W);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public vector ElementWiseMin(vector v1, vector v2) {
			if (v2.X < v1.X) v1.X = v2.X;
			if (v2.Y < v1.Y) v1.Y = v2.Y;
			if (v2.Z < v1.Z) v1.Z = v2.Z;
			if (v2.W < v1.W) v1.W = v2.W;
			return v1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static public vector ElementWiseMax(vector v1, vector v2) {
			if (v2.X < v1.X) v2.X = v1.X;
			if (v2.Y < v1.Y) v2.Y = v1.Y;
			if (v2.Z < v1.Z) v2.Z = v1.Z;
			if (v2.W < v1.W) v2.W = v1.W;
			return v2;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ElementWiseMinMax(vector v1, vector v2, out vector min, out vector max) {
			if (v2.X < v1.X) { var t = v1.X; v1.X = v2.X; v2.X = t; };
			if (v2.Y < v1.Y) { var t = v1.Y; v1.Y = v2.Y; v2.Y = t; };
			if (v2.Z < v1.Z) { var t = v1.Z; v1.Z = v2.Z; v2.Z = t; };
			if (v2.W < v1.W) { var t = v1.W; v1.W = v2.W; v2.W = t; };
			min = v1;
			max = v2;
		}

#if UNITY_5_3_OR_NEWER
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector4i(UnityEngine.Vector4 v) {
			X = (element)v.x;
			Y = (element)v.y;
			Z = (element)v.z;
			W = (element)v.w;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator vector(UnityEngine.Vector4 v) {
			return new vector((element)v.x, (element)v.y, (element)v.z, (element)v.w);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator UnityEngine.Vector4(vector v) {
			return new UnityEngine.Vector4((float)v.X, (float)v.Y, (float)v.Z, (float)v.W);
		}
#endif
	}
}
