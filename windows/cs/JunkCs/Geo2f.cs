using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

using element = System.Single;
using vectori = Jk.Vector2i;
using vector = Jk.Vector2f;
using range = Jk.Range2f;
using volume = Jk.Obb2f;
using polbool = Jk.PolBool2f;

namespace Jk {
	public static class Geo2f {
		/// <summary>
		/// 指定値を範囲内にクリップする
		/// </summary>
		/// <param name="value">[in] クリップ前の値</param>
		/// <param name="min">[in] 最小値</param>
		/// <param name="max">[in] 最大値</param>
		/// <returns>クリップ後の値</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static element Clip(element value, element min, element max) {
			if (value < min)
				return min;
			else if (max < value)
				return max;
			return value;
		}

		/// <summary>
		/// 指定値を範囲内にクリップする
		/// </summary>
		/// <param name="value">[in, out] クリップ前の値</param>
		/// <param name="min">[in] 最小値</param>
		/// <param name="max">[in] 最大値</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Clip(ref element value, element min, element max) {
			if (value < min) {
				value = min;
			} else if (max < value) {
				value = max;
			}
		}

		/// <summary>
		/// pを開始点としvを方向ベクトルとする線分と点cとの最近点の線分パラメータを計算する
		/// </summary>
		/// <param name="p">[in] 線分の開始点</param>
		/// <param name="v">[in] 線分の方向ベクトル</param>
		/// <param name="c">[in] 最近点を調べる点c</param>
		/// <returns>線分のパラメータ</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static element LinePointNearestParam(vector p, vector v, vector c) {
			return (c - p).Dot(v) / v.LengthSquare;
		}

		/// <summary>
		/// ２次元線分同士が交差しているか調べる、交点のパラメータは計算しない（整数ベクトル使用可）
		/// </summary>
		/// <param name="s1">[in] 線分１の開始点</param>
		/// <param name="e1">[in] 線分１の終了点</param>
		/// <param name="s2">[in] 線分２の開始点</param>
		/// <param name="e2">[in] 線分２の終了点</param>
		/// <returns>交差しているなら true が返る</returns>
		public static bool LineIntersect(vector s1, vector e1, vector s2, vector e2) {
			var v = s1 - e1;
			var ox = s2.Y - s1.Y;
			var oy = s1.X - s2.X;
			if (0 <= (v.X * ox + v.Y * oy) * (v.X * (e2.Y - s1.Y) + v.Y * (s1.X - e2.X)))
				return false;
			v = s2 - e2;
			if (0 <= -(v.X * ox + v.Y * oy) * (v.X * (e1.Y - s2.Y) + v.Y * (s2.X - e1.X)))
				return false;
			return true;
		}

		/// <summary>
		/// ２直線の交点計算に使用する除数を計算する、結果が正数なら<see cref="v1"/>に対して<see cref="v2"/>が+Y軸側に折れている、負数なら-Y軸側
		/// </summary>
		/// <param name="v1">[in] 直線１の方向ベクトル</param>
		/// <param name="v2">[in] 直線２の方向ベクトル</param>
		/// <returns>後の交点パラメータ計算用の除数、0なら交点は存在しない</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static element LineIntersectDivisor(vector v1, vector v2) {
			return v1.X * v2.Y - v1.Y * v2.X;
		}

		/// <summary>
		/// 直線１の交点パラメータを計算する、<see cref="v1"/>、<see cref="v2"/>を入れ替えると直線２のパラメータが計算可能
		/// </summary>
		/// <param name="pv">[in] 直線の開始点の差分、P2-P1</param>
		/// <param name="v1">[in] 直線１の方向ベクトル</param>
		/// <param name="v2">[in] 直線２の方向ベクトル</param>
		/// <param name="divisor">[in] <see cref="LineIntersectDivisor"/>で計算された除数</param>
		/// <returns>直線１のパラメータ</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static element LineIntersectParam(vector pv, vector v1, vector v2, element divisor) {
			return (pv.X * v2.Y - pv.Y * v2.X) / divisor;
		}
	}
}
