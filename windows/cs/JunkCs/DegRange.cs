using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using element = System.Double;

namespace Jk {
	/// <summary>
	/// デグリーでの範囲
	/// </summary>
	public struct DegRange {
		/// <summary>
		/// 開始角度
		/// </summary>
		public element Start;

		/// <summary>
		/// 角度幅
		/// </summary>
		public element Size;

		/// <summary>
		/// コンストラクタ、開始角度と幅で初期化する
		/// </summary>
		/// <param name="start">開始角度</param>
		/// <param name="size">幅</param>
		public DegRange(element start, element size) {
			this.Start = start;
			this.Size = size;
		}

		/// <summary>
		/// 範囲を正規化する
		/// </summary>
		public void NormalizeSelf() {
			var start = this.Start;
			var size = this.Size;
			if (size < 0) {
				start += size;
				size = -size;
			}
			this.Start = Degree.Normalize(start);
			if (Degree.Full < size)
				size = Degree.Full;
			this.Size = size;
		}

		/// <summary>
		/// 指定角度が範囲内かどうかチェック
		/// </summary>
		/// <param name="a">角度</param>
		/// <returns>範囲内なら true 、それ以外は false</returns>
		public bool InRange(element a) {
			a = Degree.Normalize(a);
			a -= this.Start;
			return 0 <= a && a <= this.Size;
		}

		/// <summary>
		/// 指定範囲が範囲内かどうかチェック
		/// </summary>
		/// <param name="r">範囲</param>
		/// <returns>範囲内なら true 、それ以外は false</returns>
		public bool InRange(DegRange r) {
			if (Degree.Full <= this.Size)
				return true;
			r.Start = Degree.Sub(r.Start, this.Start);
			return 0 <= r.Start && r.Start + r.Size <= this.Size;
		}

		/// <summary>
		/// ２つの範囲を包含する範囲を作成する
		/// </summary>
		/// <param name="r1">範囲1</param>
		/// <param name="r2">範囲2</param>
		/// <returns>r1 と r2 を包含した範囲</returns>
		public static DegRange Union(DegRange r1, DegRange r2) {
			// 正規化しサイズが大きい方をメインr1とする
			r1.NormalizeSelf();
			r2.NormalizeSelf();
			if (r1.Size < r2.Size) {
				var t = r1;
				r1 = r2;
				r2 = t;
			}

			// 既にフルサイズかチェック
			if (Degree.Full <= r1.Size)
				return r1;

			// r1 の開始点を0として r2 をシフトする
			var start2 = Degree.Sub(r2.Start, r1.Start);
			var end2 = start2 + r2.Size;

			// 既に r2 が r1 に包含されているかチェック
			if (0 <= start2 && end2 <= r1.Size)
				return r1;

			// r2 の開始点が r1 の範囲内なら r1 のサイズを拡張する
			if (0 <= start2 && start2 <= r1.Size) {
				if (Degree.Full <= end2)
					return new DegRange(r1.Start, Degree.Full);
				r1.Size = end2;
				return r1;
			}

			// r2 の終点が r1 の範囲内なら r1 の開始位置をシフトしサイズも拡張する
			if (0 <= end2 && end2 <= r1.Size) {
				if (Degree.Full + start2 <= r1.Size)
					return new DegRange(r1.Start, Degree.Full);
				return new DegRange(Degree.Normalize(r1.Start + start2), r1.Size - start2);
			}

			// r2 が r1 と接触していないなら r2 の開始点または終点の近い方と結合する
			var dstart = Math.Abs(Degree.Sub(start2, r1.Size));
			var dend = Math.Abs(Degree.Sub(end2, 0));
			if (dstart <= dend) {
				return new DegRange(r1.Start, r1.Size + r2.Size + dstart);
			} else {
				return new DegRange(r2.Start, r1.Size + Math.Abs(start2));
			}
		}

		/// <summary>
		/// 範囲を反転する
		/// </summary>
		/// <param name="r">範囲</param>
		/// <returns>反転された範囲</returns>
		public static DegRange Not(DegRange r) {
			r.NormalizeSelf();
			r.Start += r.Size;
			r.Size = Degree.Full - r.Size;
			return r;
		}

		/// <summary>
		/// 範囲をラジアン範囲に変換する
		/// </summary>
		/// <returns>ラジアン範囲</returns>
		public RadRange ToRad() {
			return new RadRange(this.Start * Degree.ToRadK, this.Size * Degree.ToRadK);
		}

		static public bool operator ==(DegRange a, DegRange b) {
			return a.Start == b.Start && a.Size == b.Size;
		}

		static public bool operator !=(DegRange a, DegRange b) {
			return a.Start != b.Start || a.Size != b.Size;
		}

		static public DegRange operator +(DegRange rr, element offset) {
			return new DegRange(rr.Start + offset, rr.Size);
		}

		static public DegRange operator -(DegRange rr, element offset) {
			return new DegRange(rr.Start - offset, rr.Size);
		}

		public override string ToString() {
			return string.Format("{{ {0}, {1}({2}) }}", this.Start, this.Size, Degree.Normalize(this.Start + this.Size));
		}

		public override bool Equals(object obj) {
			if (obj is DegRange)
				return (DegRange)obj == this;
			else
				return false;
		}

		public override int GetHashCode() {
			return (int)(this.Start * this.Size);
		}
	}
}
