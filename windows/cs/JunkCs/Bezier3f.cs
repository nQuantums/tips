using System;
using System.Collections.Generic;

using element = System.Single;
using vector = Jk.Vector3f;
using cubicbezier = Jk.Bezier3f.Cubic;

namespace Jk {
	/// <summary>
	/// ベジェ曲線生成用クラス
	/// </summary>
	/// <remarks>GraphicsGems "FitCurves.c" を参考に作成。</remarks>
	public class Bezier3f {
		/// <summary>
		/// 3次ベジェ曲線
		/// </summary>
		public class Cubic {
			public vector P0;
			public vector P1;
			public vector P2;
			public vector P3;

			public vector this[int index] {
				get {
					switch (index) {
					case 0:
						return P0;
					case 1:
						return P1;
					case 2:
						return P2;
					case 3:
						return P3;
					default:
						throw new NotImplementedException();
					}
				}
				set {
					switch (index) {
					case 0:
						P0 = value;
						break;
					case 1:
						P1 = value;
						break;
					case 2:
						P2 = value;
						break;
					case 3:
						P3 = value;
						break;
					default:
						throw new NotImplementedException();
					}
				}
			}

			/// <summary>
			/// 補間する
			/// </summary>
			/// <param name="t">パラメータ</param>
			/// <returns>補間された座標</returns>
			public vector Interpolate(element t) {
				var t2 = t * t;
				var t3 = t2 * t;
				var ti = 1 - t;
				var ti2 = ti * ti;
				var ti3 = ti2 * ti;
				return P0 * ti3 + P1 * (3 * ti2 * t) + P2 * (3 * ti * t2) + P3 * t3;
			}

			/// <summary>
			/// 補間を微分する
			/// </summary>
			/// <param name="t">パラメータ</param>
			/// <returns>微分結果のベクトル</returns>
			public vector DiffInterpolate(element t) {
				var t2 = t * t;
				var ti = 1 - t;
				var ti2 = ti * ti;
				return P0 * (-3 * ti2) + P1 * (9 * t2 - 12 * t + 3) + P2 * (-9 * t2 + 6 * t) + P3 * (3 * t2);
			}
		}


		/// <summary>
		/// 直線で補間する
		/// </summary>
		/// <param name="t">パラメータ</param>
		/// <param name="p0">コントロールポイント０</param>
		/// <param name="p1">コントロールポイント１</param>
		/// <returns>補間された座標</returns>
		public static vector Interpolate1(element t, vector p0, vector p1) {
			return p0 * (1 - t) + p1 * t;
		}

		/// <summary>
		/// ２次ベジェ曲線で補間する
		/// </summary>
		/// <param name="t">パラメータ</param>
		/// <param name="p0">コントロールポイント０</param>
		/// <param name="p1">コントロールポイント１</param>
		/// <param name="p2">コントロールポイント２</param>
		/// <returns>補間された座標</returns>
		public static vector Interpolate2(element t, vector p0, vector p1, vector p2) {
			var ti = 1 - t;
			return p0 * (ti * ti) + p1 * (2 * ti * t) + p2 * (t * t);
		}

		/// <summary>
		/// ３次ベジェ曲線で補間する
		/// </summary>
		/// <param name="t">パラメータ</param>
		/// <param name="p0">コントロールポイント０</param>
		/// <param name="p1">コントロールポイント１</param>
		/// <param name="p2">コントロールポイント２</param>
		/// <param name="p3">コントロールポイント３</param>
		/// <returns>補間された座標</returns>
		public static vector Interpolate3(element t, vector p0, vector p1, vector p2, vector p3) {
			var t2 = t * t;
			var t3 = t2 * t;
			var ti = 1 - t;
			var ti2 = ti * ti;
			var ti3 = ti2 * ti;
			return p0 * ti3 + p1 * (3 * ti2 * t) + p2 * (3 * ti * t2) + p3 * t3;
		}

		/// <summary>
		/// ３次ベジェ曲線での補間を微分する
		/// </summary>
		/// <param name="t">パラメータ</param>
		/// <param name="p0">コントロールポイント０</param>
		/// <param name="p1">コントロールポイント１</param>
		/// <param name="p2">コントロールポイント２</param>
		/// <param name="p3">コントロールポイント３</param>
		/// <returns>微分結果のベクトル</returns>
		public static vector DiffInterpolate3(element t, vector p0, vector p1, vector p2, vector p3) {
			var t2 = t * t;
			var ti = 1 - t;
			var ti2 = ti * ti;
			return p0 * (-3 * ti2) + p1 * (9 * t2 - 12 * t + 3) + p2 * (-9 * t2 + 6 * t) + p3 * (3 * t2);
		}

		/// <summary>
		/// 指定されたベジェ配列から補間された頂点列を取得する
		/// </summary>
		/// <param name="beziers">ベジェ列</param>
		/// <param name="division">ベジェ１つあたりの分割数</param>
		/// <returns>頂点列</returns>
		public static IEnumerable<vector> Interpolate(List<cubicbezier> beziers, int division) {
			var n = beziers.Count;
			var step = (element)1 / division;
			for (int i = 0; i < n; i++) {
				var bezier = beziers[i];
				for (int j = 0; j < division; j++) {
					yield return bezier.Interpolate(j * step);
				}
			}
			yield return beziers[n - 1].Interpolate(1);
		}

		/// <summary>
		/// 指定されたベジェ配列から補間を微分したベクトル列を取得する
		/// </summary>
		/// <param name="beziers">ベジェ列</param>
		/// <param name="division">ベジェ１つあたりの分割数</param>
		/// <returns>ベクトル列</returns>
		public static IEnumerable<vector> DiffInterpolate(List<cubicbezier> beziers, int division) {
			var n = beziers.Count;
			var step = (element)1 / division;
			for (int i = 0; i < n; i++) {
				var bezier = beziers[i];
				for (int j = 0; j < division; j++) {
					yield return bezier.DiffInterpolate(j * step);
				}
			}
			yield return beziers[n - 1].DiffInterpolate(1);
		}

		/// <summary>
		/// 指定された頂点列に３次ベジェ曲線をフィットさせ補間された頂点列を取得する
		/// </summary>
		/// <param name="d">フィット元頂点列</param>
		/// <param name="error">フィット時許容誤差の二乗</param>
		/// <param name="close">頂点列が閉じられているかどうか</param>
		/// <returns>頂点列</returns>
		public static IEnumerable<vector> FitCubicInterpolate(vector[] d, element error, bool close, int division) {
			var beziers = new List<cubicbezier>();
			FitCubic(d, error, close, beziers);
			return Interpolate(beziers, division);
		}

		/// <summary>
		/// 指定された頂点列に３次ベジェ曲線をフィットさせ補間を微分したベクトル列を取得する
		/// </summary>
		/// <param name="d">フィット元頂点列</param>
		/// <param name="error">フィット時許容誤差の二乗</param>
		/// <param name="close">頂点列が閉じられているかどうか</param>
		/// <returns>微分結果のベクトル</returns>
		public static IEnumerable<vector> FitCubicDiffInterpolate(vector[] d, element error, bool close, int division) {
			var beziers = new List<cubicbezier>();
			FitCubic(d, error, close, beziers);
			return DiffInterpolate(beziers, division);
		}

		/// <summary>
		/// 指定された頂点列に３次ベジェ曲線をフィットさせる
		/// </summary>
		/// <param name="d">フィット元頂点列、要素数は２以上でなければならない</param>
		/// <param name="error">フィット時許容誤差の二乗</param>
		/// <param name="close">頂点列が閉じられているかどうか</param>
		/// <param name="result">ここにベジェ曲線列が追加される</param>
		public static void FitCubic(vector[] d, element error, bool close, List<cubicbezier> result) {
			var last = d.Length - 1;
			var tHat1 = (d[1] - d[0]).Normalize();
			var tHat2 = (d[last - 1] - d[last]).Normalize();
			if (close) {
				tHat1.SubSelf(tHat2);
				tHat1.NormalizeSelf();
				tHat2 = -tHat1;
			}
			FitCubic(
				d,
				0,
				last,
				tHat1,
				tHat2,
				error,
				result);
		}

		/// <summary>
		/// 指定された頂点列の指定範囲にベジェ曲線をフィットさせる
		/// </summary>
		/// <param name="d">フィット元頂点列</param>
		/// <param name="first"><see cref="d"/>内の範囲開始インデックス</param>
		/// <param name="last"><see cref="d"/>内の範囲終了インデックス</param>
		/// <param name="tHat1">指定範囲開始点の長さ１の順方向ベクトル</param>
		/// <param name="tHat2">指定範囲終了点の長さ１の逆方向ベクトル</param>
		/// <param name="error">フィット時許容誤差の二乗</param>
		/// <param name="result">ここにベジェ曲線列が追加される</param>
		static void FitCubic(vector[] d, int first, int last, vector tHat1, vector tHat2, element error, List<cubicbezier> result) {
			cubicbezier bezCurve; /*Control points of fitted Bezier curve*/
			var nPts = last - first + 1; /*  Number of points in subset  */

			/*  Use heuristic if region only has two points in it */
			if (nPts == 2) {
				bezCurve = new cubicbezier();
				bezCurve.P0 = d[first];
				bezCurve.P3 = d[last];

				var dist = (d[last] - d[first]).Length / 3;
				bezCurve.P1 = bezCurve.P0 + tHat1.Relength(dist);
				bezCurve.P2 = bezCurve.P3 + tHat2.Relength(dist);
				result.Add(bezCurve);
				return;
			}

			/*  Parameterize points, and attempt to fit curve */
			var u = ChordLengthParameterize(d, first, last);
			bezCurve = GenerateBezier(d, first, last, u, tHat1, tHat2);

			/*  Find max deviation of points to fitted curve */
			int splitPoint; /*  Point to split point set at	 */
			var maxError = ComputeMaxError(d, first, last, bezCurve, u, out splitPoint);
			if (maxError < error) {
				result.Add(bezCurve);
				return;
			}


			/*  If error not too large, try some reparameterization  */
			/*  and iteration */
			var iterationError = error * error; /*Error below which you try iterating  */
			if (maxError < iterationError) {
				var maxIterations = 4; /*  Max times to try iterating  */
				for (var i = 0; i < maxIterations; i++) {
					var uPrime = Reparameterize(d, first, last, u, bezCurve);
					bezCurve = GenerateBezier(d, first, last, uPrime, tHat1, tHat2);
					maxError = ComputeMaxError(d, first, last, bezCurve, uPrime, out splitPoint);
					if (maxError < error) {
						result.Add(bezCurve);
						return;
					}
					u = uPrime;
				}
			}

			/* Fitting failed -- split at max error point and fit recursively */
			var tHatCenter = ((d[splitPoint - 1] - d[splitPoint + 1]) * (element)0.5).Normalize();

			FitCubic(d, first, splitPoint, tHat1, tHatCenter, error, result);

			tHatCenter = -tHatCenter;

			FitCubic(d, splitPoint, last, tHatCenter, tHat2, error, result);
		}

		/// <summary>
		/// 最小二乗法を用いて指定範囲のベジェコントロールポイントを探す
		/// </summary>
		/// <param name="d">頂点列</param>
		/// <param name="first">範囲開始インデックス</param>
		/// <param name="last">範囲終了インデックス</param>
		/// <param name="uPrime">指定範囲内のパラメータ</param>
		/// <param name="tHat1">範囲開始部分のベクトル</param>
		/// <param name="tHat2">範囲終了部分のベクトル</param>
		/// <returns>３次ベジェ曲線</returns>
		static cubicbezier GenerateBezier(vector[] d, int first, int last, element[] uPrime, vector tHat1, vector tHat2) {
			var nPts = last - first + 1;
			var tHat1LenDiv = tHat1.Length;
			var tHat2LenDiv = tHat2.Length;

			if (tHat1LenDiv != 0)
				tHat1LenDiv = 1 / tHat1LenDiv;
			if (tHat2LenDiv != 0)
				tHat2LenDiv = 1 / tHat2LenDiv;

			/* Compute the A's	*/
			var A = new vector[nPts, 2];  /* Precomputed rhs for eqn	*/
			for (int i = 0; i < nPts; i++) {
				var u = uPrime[i];
				var ui = 1 - u;
				var b1 = 3 * u * ui * ui;
				var b2 = 3 * u * u * ui;
				A[i, 0] = tHat1 * (b1 * tHat1LenDiv);
				A[i, 1] = tHat2 * (b2 * tHat2LenDiv);
			}

			/* Create the C and X matrices	*/
			var C = new element[2, 2]; /* Matrix C		*/
			var X = new element[2]; /* Matrix X			*/
			vector tmp; /* Utility variable		*/
			for (int i = 0; i < nPts; i++) {
				C[0, 0] += A[i, 0].Dot(A[i, 0]);
				C[0, 1] += A[i, 0].Dot(A[i, 1]);
				C[1, 0] = C[0, 1];
				C[1, 1] += A[i, 1].Dot(A[i, 1]);

				var df = d[first];
				var dl = d[last];
				tmp = d[first + i] - Interpolate3(uPrime[i], df, df, dl, dl);

				X[0] += A[i, 0].Dot(tmp);
				X[1] += A[i, 1].Dot(tmp);
			}

			/* Compute the determinants of C and X	*/
			var det_C0_C1 = C[0, 0] * C[1, 1] - C[1, 0] * C[0, 1];
			var det_C0_X = C[0, 0] * X[1] - C[1, 0] * X[0];
			var det_X_C1 = X[0] * C[1, 1] - X[1] * C[0, 1];

			/* Finally, derive alpha values	*/
			var alpha_l = det_C0_C1 == 0 ? 0 : det_X_C1 / det_C0_C1;
			var alpha_r = det_C0_C1 == 0 ? 0 : det_C0_X / det_C0_C1;

			/* If alpha negative, use the Wu/Barsky heuristic (see text) */
			/* (if alpha is 0, you get coincident control points that lead to
			 * divide by zero in any subsequent NewtonRaphsonRootFind() call. */
			var bezCurve = new cubicbezier();
			var segLength = (d[last] - d[first]).Length;
			var epsilon = (element)1.0e-6 * segLength;
			if (alpha_l < epsilon || alpha_r < epsilon) {
				/* fall back on standard (probably inaccurate) formula, and subdivide further if needed. */
				element dist = segLength / 3;
				bezCurve.P0 = d[first];
				bezCurve.P3 = d[last];
				bezCurve.P1 = bezCurve.P0 + tHat1 * (dist * tHat1LenDiv);
				bezCurve.P2 = bezCurve.P3 + tHat2 * (dist * tHat2LenDiv);
				return bezCurve;
			}

			/*  First and last control points of the Bezier curve are */
			/*  positioned exactly at the first and last data points */
			/*  Control points 1 and 2 are positioned an alpha distance out */
			/*  on the tangent vectors, left and right, respectively */
			bezCurve.P0 = d[first];
			bezCurve.P3 = d[last];
			bezCurve.P1 = bezCurve.P0 + tHat1 * (alpha_l * tHat1LenDiv);
			bezCurve.P2 = bezCurve.P3 + tHat2 * (alpha_r * tHat2LenDiv);
			return bezCurve;
		}

		/// <summary>
		/// 指定頂点列の開始位置から終了位置までの距離を0から1にパラメータ化する
		/// </summary>
		/// <param name="d">頂点列</param>
		/// <param name="first"><see cref="d"/>内の範囲開始インデックス</param>
		/// <param name="last"><see cref="d"/>内の範囲終了インデックス</param>
		/// <returns>パラメータ列</returns>
		static element[] ChordLengthParameterize(vector[] d, int first, int last) {
			var n = last - first;
			var u = new element[n + 1];
			element distance = 0;
			for (var i = first + 1; i <= last; i++) {
				distance += (d[i] - d[i - 1]).Length;
				u[i - first] = distance;
			}
			for (var i = u.Length - 1; i != -1; i--)
				u[i] /= distance;
			return u;
		}

		/// <summary>
		/// Given set of points and their parameterization, try to find a better parameterization.
		/// </summary>
		/// <param name="d">Array of digitized points</param>
		/// <param name="first">Indices defining region</param>
		/// <param name="last">Indices defining region</param>
		/// <param name="u">Current parameter values</param>
		/// <param name="bezCurve">Current fitted curve</param>
		/// <returns>パラメータ列</returns>
		static element[] Reparameterize(vector[] d, int first, int last, element[] u, cubicbezier bezCurve) {
			var uPrime = new element[last - first + 1];     /*  New parameter values	*/
			for (int i = first; i <= last; i++) {
				var j = i - first;
				uPrime[j] = NewtonRaphsonRootFind(bezCurve, d[i], u[j]);
			}
			return uPrime;
		}

		/// <summary>
		/// Use Newton-Raphson iteration to find better root.
		/// </summary>
		/// <param name="Q">Current fitted curve</param>
		/// <param name="P">Digitized point</param>
		/// <param name="u">Parameter value for <see cref="P"/></param>
		/// <returns>パラメータ</returns>
		static element NewtonRaphsonRootFind(cubicbezier Q, vector P, element u) {
			/* Compute Q(u)	*/
			var Q_u = Q.Interpolate(u);

			/* Generate control vertices for Q'	*/
			var Q1 = new vector[3];    /*  Q' and Q''			*/
			for (int i = 0; i <= 2; i++) {
				Q1[i] = (Q[i + 1] - Q[i]) * 3;
			}

			/* Generate control vertices for Q'' */
			var Q2 = new vector[2];
			for (int i = 0; i <= 1; i++) {
				Q2[i] = (Q1[i + 1] - Q1[i]) * 2;
			}

			/* Compute Q'(u) and Q''(u)	*/
			var Q1_u = Interpolate2(u, Q1[0], Q1[1], Q1[2]);
			var Q2_u = Interpolate1(u, Q2[0], Q2[1]);

			/* Compute f(u)/f'(u) */
			var Q_u_P = Q_u - P;
			var numerator = Q_u_P.Dot(Q1_u);
			var denominator = Q1_u.LengthSquare + Q_u_P.Dot(Q2_u);
			if (denominator == 0)
				return u;

			/* u = u - f(u)/f'(u) */
			return u - numerator / denominator;
		}

		static element ComputeMaxError(vector[] d, int first, int last, cubicbezier bezCurve, element[] u, out int splitPoint) {
			var sp = (last - first + 1) / 2;
			element maxDist2 = 0;
			for (var i = first + 1; i < last; i++) {
				var dist2 = (bezCurve.Interpolate(u[i - first]) - d[i]).LengthSquare;
				if (maxDist2 <= dist2) {
					maxDist2 = dist2;
					sp = i;
				}
			}
			splitPoint = sp;
			return maxDist2;
		}
	}
}
