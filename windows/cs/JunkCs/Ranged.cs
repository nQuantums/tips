using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Runtime.InteropServices;

using element = System.Double;

namespace Jk {
	/// <summary>
	/// １次元空間での開始値と終了値を持つ範囲 element 版
	/// </summary>
	[XmlType("Jk.Ranged")]
	public struct Ranged {
		/// <summary>
		/// レンジの開始値
		/// </summary>
		public element V1;

		/// <summary>
		/// レンジの終了値
		/// </summary>
		public element V2;

		/// <summary>
		/// レンジの開始値と終了値を指定するコンストラクタ
		/// </summary>
		/// <param name="v1">開始値</param>
		/// <param name="v2">終了値</param>
		public Ranged(element v1, element v2) {
			V1 = v1;
			V2 = v2;
		}

		/// <summary>
		/// 範囲のサイズを取得
		/// </summary>
		public element Size {
			get {
				return V2 - V1;
			}
		}

		/// <summary>
		/// 中心値の取得
		/// </summary>
		public element Center {
			get {
				return (V1 + V2) / 2;
			}
		}

		/// <summary>
		///	範囲を移動する
		/// </summary>
		/// <param name="v">移動オフセット値</param>
		public void Offset(element v) {
			V1 += v;
			V2 += v;
		}

		/// <summary>
		/// 現在のサイズとの比率を指定して範囲サイズを変更する
		/// </summary>
		/// <param name="rate">比率(1=100%)</param>
		public void InflateByRate(element rate) {
			element c = Center;
			V1 = c + rate * (V1 - c);
			V2 = c + rate * (V2 - c);
		}
		/// <summary>
		/// 現在のサイズとの比率を指定して範囲サイズを変更する
		/// bZero==TRUE の時は、以下のルールでスケールを0にする
		/// 
		/// 変更後(比率計算後)の V1,V2 が + ～ - の情報で
		///     変更前の V1,V2 が共に＋ か 共に－ の時
		///     共に＋の時は、V1=0
		///     共に－の時は、V2=0
		/// </summary>
		/// <param name="rate">比率</param>
		/// <param name="bZero">０を範囲内に入れる処理を行うかどうか</param>
		public void InflateByRate(element rate, bool bZero) {
			element v1 = V1;
			element v2 = V2;

			InflateByRate(rate);

			if (bZero) {
				if ((V2 >= 0 && V1 <= 0) || (V2 <= 0 && V1 >= 0)) {
					if (v1 >= 0 && v2 >= 0) {
						V1 = 0;
					}
					if (v1 <= 0 && v2 <= 0) {
						V2 = 0;
					}
				}
			}
		}

		/// <summary>
		/// V1＜V2になるように修正する
		/// </summary>
		public void Normalize() {
			if (V2 < V1) {
				element t = V1;
				V1 = V2;
				V2 = t;
			}
		}

		/// <summary>
		/// V1＜V2になるように修正された範囲を取得する
		/// </summary>
		/// <returns>V1＜V2となった範囲</returns>
		public Ranged GetNormalized() {
			if (V1 <= V2)
				return new Ranged(V1, V2);
			else
				return new Ranged(V2, V1);
		}

		/// <summary>
		/// V1=+∞ V2=-∞ となるように設定する
		/// これを呼び出した後に複数回 Union を呼び出すと、 Union に渡した全ての RangeD を包含する RangeD に成る
		/// </summary>
		public void Invalidate() {
			V1 = element.MaxValue;
			V2 = element.MinValue;
		}

		/// <summary>
		/// 範囲が V2＜V1 となっているか調べる
		/// </summary>
		public bool IsInvalid() {
			return V2 < V1;
		}

		/// <summary>
		/// 指定された値を含むように自分の開始値と終了値を広げる
		///	V1＜V2でなければならない
		/// </summary>
		public void Union(element val) {
			if (val < V1)
				V1 = val;
			else if (V2 < val)
				V2 = val;
		}

		/// <summary>
		/// 指定された RangeD を含むように自分の開始値と終了値を広げる
		/// 自分と指定された RangeD 共に V1＜V2でなければならない
		/// </summary>
		public void Union(Ranged r) {
			if (r.V1 < V1)
				V1 = r.V1;
			if (V2 < r.V2)
				V2 = r.V2;
		}

		/// <summary>
		/// 指定された RangeD を含むように自分の開始値と終了値を広げる
		/// 自分と指定された RangeD 共に V1＜V2でなければならない
		/// </summary>
		public void Union(ref Ranged r) {
			if (r.V1 < V1)
				V1 = r.V1;
			if (V2 < r.V2)
				V2 = r.V2;
		}

		/// <summary>
		/// V1 と V2 に自分と指定された RangeD が交差する部分の範囲を設定する
		/// 自分と指定された RangeD 共に V1＜V2でなければならない
		/// </summary>
		public void Intersect(Ranged r) {
			if (V1 < r.V1)
				V1 = r.V1;
			if (r.V2 < V2)
				V2 = r.V2;
		}

		/// <summary>
		/// V1 と V2 に自分と指定された RangeD が交差する部分の範囲を設定する
		/// 自分と指定された RangeD 共に V1＜V2でなければならない
		/// </summary>
		public void Intersect(ref Ranged r) {
			if (V1 < r.V1)
				V1 = r.V1;
			if (r.V2 < V2)
				V2 = r.V2;
		}

		/// <summary>
		/// 現在の範囲から指定された範囲への変化率を計算する
		/// </summary>
		/// <param name="r">目標の範囲</param>
		/// <returns>変化率が返る</returns>
		public Ranged CalcChangeRate(Ranged r) {
			element s = this.Size;
			if (s == 0)
				return new Ranged();
			return new Ranged((r.V1 - this.V1) / s, (r.V2 - this.V2) / s);
		}

		/// <summary>
		/// 変化率を適用する
		/// </summary>
		/// <param name="cr">変化率</param>
		public void ApplyChangeRate(Ranged cr) {
			element s = this.Size;
			this.V1 += cr.V1 * s;
			this.V2 += cr.V2 * s;
		}

		/// <summary>
		/// 指定された値を範囲内に含んでいるかどうかどうか調べる
		/// </summary>
		/// <param name="v">範囲内かどうか調べる値</param>
		/// <returns>true=範囲内 false=範囲外</returns>
		/// <remarks>呼び出し時にはV1＜＝V2でなければならない</remarks>
		public bool CheckInclusion(element v) {
			return this.V1 <= v && v <= this.V2;
		}

		/// <summary>
		/// 指定された範囲を自分の範囲内に完全に含めているかどうかどうか調べる
		/// </summary>
		/// <param name="r">範囲内かどうか調べる範囲</param>
		/// <returns>true=範囲内 false=範囲外</returns>
		/// <remarks>呼び出し時にはV1＜＝V2でなければならない</remarks>
		public bool CheckInclusion(Ranged r) {
			return this.V1 <= r.V1 && r.V2 <= this.V2;
		}

		/// <summary>
		/// 指定された範囲と接触しているかどうか調べる
		/// </summary>
		/// <param name="r">接触しているかどうか調べる範囲</param>
		/// <returns>true=接触している、false=接触していない</returns>
		/// <remarks>呼び出し時にはV1＜＝V2でなければならない</remarks>
		public bool CheckIntersect(Ranged r) {
			return this.V1 <= r.V2 && r.V1 <= this.V2;
		}

		/// <summary>
		/// 指定された範囲と接触しているかどうか調べる
		/// </summary>
		/// <param name="r">接触しているかどうか調べる範囲</param>
		/// <returns>true=接触している、false=接触していない</returns>
		/// <remarks>呼び出し時にはV1＜＝V2でなければならない</remarks>
		public bool CheckIntersect(ref Ranged r) {
			return this.V1 <= r.V2 && r.V1 <= this.V2;
		}

		/// <summary>
		/// 二つの RangeD 型の値が同じか調べる
		/// </summary>
		public static bool operator ==(Ranged a, Ranged b) {
			return a.V1 == b.V1 && a.V2 == b.V2;
		}
		/// <summary>
		/// 二つの RangeD 型の値が異なるか調べる
		/// </summary>
		public static bool operator !=(Ranged a, Ranged b) {
			return a.V1 != b.V1 || a.V2 != b.V2;
		}
		/// <summary>
		/// 指定されたオブジェクトと内容が同じか調べる
		/// </summary>
		public override bool Equals(object obj) {
			try {
				return this == (Ranged)obj;
			} catch (Exception) {
				return false;
			}
		}
		/// <summary>
		///	ハッシュコード取得
		/// </summary>
		public override int GetHashCode() {
			return base.GetHashCode();
		}

		/// <summary>
		/// 文字列に変換
		/// </summary>
		public override string ToString() {
			return string.Format("{{ V1 = {0}, V2 = {1} }}", this.V1, this.V2);
		}
	}
}
