using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using element = System.Double;

namespace Jk {
	/// <summary>
	/// 行列関係メソッド
	/// </summary>
	public class Matrix {
		/// <summary>
		/// 単位行列にする
		/// </summary>
		/// <param name="a">行列</param>
		static public void Unit(element[,] a) {
			for (int row = 0; row < a.GetLength(0); row++)
				for (int col = 0; col < a.GetLength(1); col++)
					a[row, col] = row == col ? 1 : 0;
		}

		/// <summary>
		/// 行列にスカラーを掛ける
		/// </summary>
		/// <param name="a">行列</param>
		/// <param name="s">スカラー</param>
		static public void Mul(element[,] a, element s) {
			for (int row = 0; row < a.GetLength(0); row++)
				for (int col = 0; col < a.GetLength(1); col++)
					a[row, col] *= s;
		}

		/// <summary>
		/// 行列をスカラーで割る
		/// </summary>
		/// <param name="a">行列</param>
		/// <param name="s">スカラー</param>
		static public void Div(element[,] a, element s) {
			for (int row = 0; row < a.GetLength(0); row++)
				for (int col = 0; col < a.GetLength(1); col++)
					a[row, col] /= s;
		}

		/// <summary>
		/// 行列同士を掛ける
		/// </summary>
		/// <param name="ret">結果の行列が返る</param>
		/// <param name="a">左側の行列</param>
		/// <param name="b">右側の行列</param>
		static public void Mul(element[,] ret, element[,] a, element[,] b) {
			int r = a.GetLength(0);
			int l = a.GetLength(1);
			int c = b.GetLength(1);
			for (int i = 0; i < r; i++) {
				for (int j = 0; j < c; j++) {
					element t = a[i, 0] * b[0, j];
					for (int k = 1; k < l; k++)
						t += a[i, k] * b[k, j];
					ret[i, j] = t;
				}
			}
		}

		/// <summary>
		/// 行列式を計算する、２次専用
		/// </summary>
		/// <param name="a">入力行列</param>
		/// <returns>true=成功、false=失敗</returns>
		static public element Determinant2(element[,] a) {
			// たすきがけで解く
			element det = 0;
			det = a[0, 0] * a[1, 1];
			det -= a[0, 1] * a[1, 0];
			return det;
		}

		/// <summary>
		/// 行列式を計算する、３次専用
		/// </summary>
		/// <param name="a">入力行列</param>
		/// <returns>true=成功、false=失敗</returns>
		static public element Determinant3(element[,] a) {
			// サラスの方法で解く
			double det = 0.0;
			det = a[0, 0] * a[1, 1] * a[2, 2];
			det += a[1, 0] * a[2, 1] * a[0, 2];
			det += a[2, 0] * a[0, 1] * a[1, 2];
			det -= a[2, 0] * a[1, 1] * a[0, 2];
			det -= a[1, 0] * a[0, 1] * a[2, 2];
			det -= a[0, 0] * a[2, 1] * a[1, 2];
			return det;
		}

		/// <summary>
		/// 行列式を計算する
		/// </summary>
		/// <param name="n">次数</param>
		/// <param name="a">入力行列</param>
		/// <param name="rdet">行列式が返る</param>
		/// <returns>true=成功、false=失敗</returns>
		static public bool Determinant(int n, element[,] a, out double rdet) {
			element det = rdet = 1.0, buf;
			int i, j, k;

			// 三角行列を作成
			for (i = 0; i < n; i++) {
				for (j = 0; j < n; j++) {
					if (i < j) {
						buf = a[i, i];
						if (buf == 0)
							return false;
						buf = a[j, i] / buf;
						for (k = 0; k < n; k++)
							a[j, k] -= a[i, k] * buf;
					}
				}
			}
			// 対角部分の積
			for (i = 0; i < n; i++)
				det *= a[i, i];

			rdet = det;

			return true;
		}

		/// <summary>
		/// 掃き出し法で逆行列を求める
		/// </summary>
		/// <param name="n">次数</param>
		/// <param name="a">入力行列、計算中に改変されます</param>
		/// <param name="inv_a">逆行列が返る</param>
		/// <param name="eps">計算機イプシロン(絶対値がこの値以下の場合はゼロと判定される)</param>
		/// <returns>true=成功、false=失敗</returns>
		static public bool InverseGauss(int n, element[,] a, element[,] inv_a, double eps) {
			double buf; // 一時的なデータを蓄える
			int i, j, k; // カウンタ

			// 単位行列を作る
			for (i = 0; i < n; i++)
				for (j = 0; j < n; j++)
					inv_a[i, j] = (i == j) ? 1.0 : 0.0;

			// 掃き出し法
			for (i = 0; i < n; i++) {
				buf = a[i, i];
				if (Math.Abs(buf) <= eps)
					return false;
				buf = 1 / buf;
				for (j = 0; j < n; j++) {
					a[i, j] *= buf;
					inv_a[i, j] *= buf;
				}
				for (j = 0; j < n; j++) {
					if (i != j) {
						buf = a[j, i];
						for (k = 0; k < n; k++) {
							a[j, k] -= a[i, k] * buf;
							inv_a[j, k] -= inv_a[i, k] * buf;
						}
					}
				}
			}

			return true;
		}

		/// <summary>
		/// LU分解
		/// </summary>
		/// <param name="n">次数</param>
		/// <param name="a">入力行列、計算中に改変されます</param>
		/// <param name="ip">行交換の情報が返る</param>
		/// <param name="rdet">行列式が返る</param>
		/// <param name="eps">計算機イプシロン(絶対値がこの値以下の場合はゼロと判定される)</param>
		/// <returns>true=成功、false=失敗</returns>
		static public bool Lu(int n, element[,] a, int[] ip, out double rdet, double eps) { // LU分解
			int i, j = 0, k, ii, ik;
			element t, u, det;
			element[] weight = new element[n];

			rdet = 0;

			for (k = 0; k < n; k++) {  // 各行について
				ip[k] = k;             // 行交換情報の初期値
				u = 0;              // その行の絶対値最大の要素を求める
				for (j = 0; j < n; j++) {
					t = Math.Abs(a[k, j]);
					if (t > u) u = t;
				}
				if (Math.Abs(u) <= eps) return false; // 0 なら行列はLU分解できない
				weight[k] = 1 / u;   // 最大絶対値の逆数
			}
			det = 1;                   // 行列式の初期値
			for (k = 0; k < n; k++) {  // 各行について
				u = -1;
				for (i = k; i < n; i++) {  // より下の各行について
					ii = ip[i];            // 重み×絶対値 が最大の行を見つける
					t = Math.Abs(a[ii, k]) * weight[ii];
					if (t > u) {
						u = t;
						j = i;
					}
				}
				ik = ip[j];
				if (j != k) {
					ip[j] = ip[k];
					ip[k] = ik;  // 行番号を交換
					det = -det;  // 行を交換すれば行列式の符号が変わる
				}
				u = a[ik, k];
				det *= u;  // 対角成分
				if (Math.Abs(u) <= eps) return false;    // 0 なら行列はLU分解できない
				for (i = k + 1; i < n; i++) {  // Gauss消去法
					ii = ip[i];
					t = (a[ii, k] /= u);
					for (j = k + 1; j < n; j++)
						a[ii, j] -= t * a[ik, j];
				}
			}
			rdet = det;
			return true;
		}

		/// <summary>
		/// ax=b を解く
		/// </summary>
		/// <param name="n">次数</param>
		/// <param name="a">入力行列</param>
		/// <param name="b">ベクトル</param>
		/// <param name="ip">Lu() メソッドで作成した行交換情報</param>
		/// <param name="x">解が返る</param>
		/// <param name="eps">計算機イプシロン(絶対値がこの値以下の場合はゼロと判定される)</param>
		/// <returns>true=成功、false=失敗</returns>
		static public bool Solve(int n, element[,] a, element[] b, int[] ip, element[] x, element eps) {
			int i, j, ii;
			element t;

			for (i = 0; i < n; i++) {       // Gauss消去法の残り
				ii = ip[i];
				t = b[ii];
				for (j = 0; j < i; j++)
					t -= a[ii, j] * x[j];
				x[i] = t;
			}
			for (i = n - 1; i >= 0; i--) {  // 後退代入
				t = x[i];
				ii = ip[i];
				for (j = i + 1; j < n; j++)
					t -= a[ii, j] * x[j];
				element buf = a[ii, i];
				if (Math.Abs(buf) <= eps)
					return false;
				x[i] = t / buf;
			}

			return true;
		}

		/// <summary>
		/// LU分解で逆行列を計算する
		/// </summary>
		/// <param name="n">次数</param>
		/// <param name="a">入力行列、計算中に改変されます</param>
		/// <param name="inv_a">逆行列が返る</param>
		/// <param name="rdet">行列式が返る</param>
		/// <param name="eps">計算機イプシロン(絶対値がこの値以下の場合はゼロと判定される)</param>
		/// <returns>true=成功、false=失敗</returns>
		static public bool InverseLu(int n, element[,] a, element[,] inv_a, out double rdet, double eps) {
			int i, j, k, ii;
			element t;
			int[] ip = new int[n];   // 行交換の情報

			if(!Lu(n, a, ip, out rdet, eps))
				return false;

			for (k = 0; k < n; k++) {
				for (i = 0; i < n; i++) {
					ii = ip[i];
					t = (ii == k) ? 1 : 0;
					for (j = 0; j < i; j++)
						t -= a[ii, j] * inv_a[j, k];
					inv_a[i, k] = t;
				}
				for (i = n - 1; i >= 0; i--) {
					t = inv_a[i, k];
					ii = ip[i];
					for (j = i + 1; j < n; j++)
						t -= a[ii, j] * inv_a[j, k];
					inv_a[i, k] = t / a[ii, i];
				}
			}

			return true;
		}
	}
}
