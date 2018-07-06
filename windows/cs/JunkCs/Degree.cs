using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using element = System.Double;

namespace Jk {
	/// <summary>
	/// デグリー同士の計算などをまとめたクラス
	/// </summary>
	public static class Degree {
		/// <summary>
		/// デグリーでの最大範囲
		/// </summary>
		public const element Full = 360;

		/// <summary>
		/// デグリーでの最大範囲の半分
		/// </summary>
		public const element Half = 180;

		/// <summary>
		/// デグリーからラジアンへの換算係数
		/// </summary>
		public const element ToRadK = Math.PI / 180;

		/// <summary>
		/// ラジアンからデグリーへの換算係数
		/// </summary>
		public const element FromRadK = 180 / Math.PI;

		/// <summary>
		/// 角度 a を 0...360 間の値に正規化する
		/// </summary>
		/// <returns>正規化された角度</returns>
		public static element Normalize(element a) {
			a %= Full;
			if (a < 0)
				a += Full;
			return a;
		}

		/// <summary>
		/// 角度 b から a へ最短変化量
		/// </summary>
		/// <param name="a">角度1</param>
		/// <param name="b">角度2</param>
		/// <returns>角度変化量</returns>
		public static element Sub(element a, element b) {
			a = Normalize(a);
			b = Normalize(b);
			var d = a - b;
			if (d < -Half) d += Full;
			else if (Half < d) d -= Full;
			return d;
		}

		/// <summary>
		/// 正規化済み角度 b から a へ最短変化量
		/// </summary>
		/// <param name="a">正規化済み角度1</param>
		/// <param name="b">正規化済み角度2</param>
		/// <returns>角度変化量</returns>
		public static element SubAfterNoamalize(element a, element b) {
			var d = a - b;
			if (d < -Half) d += Full;
			else if (Half < d) d -= Full;
			return d;
		}

		/// <summary>
		/// 角度 a と b を加算し正規化する
		/// </summary>
		/// <param name="a">角度1</param>
		/// <param name="b">角度2</param>
		/// <returns>角度</returns>
		public static element Add(element a, element b) {
			return Normalize(a + b);
		}
	}
}
