using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace Jk {
	/// <summary>
	/// 色計算クラス
	/// </summary>
	public static class ColorMath {
		/// <summary>
		/// Color から Vector3d に変換する
		/// </summary>
		/// <param name="c">色</param>
		/// <returns>Vector3d 値</returns>
		public static Vector3d ToVector3d(Color c) {
			return new Vector3d(c.R / 255.0, c.G / 255.0, c.B / 255.0);
		}

		/// <summary>
		/// Color から Vector3i に変換する
		/// </summary>
		/// <param name="c">色</param>
		/// <returns>Vector3i 値</returns>
		public static Vector3i ToVector3i(Color c) {
			return new Vector3i(c.R, c.G, c.B);
		}

		/// <summary>
		/// Vector3d から Color に変換する
		/// </summary>
		/// <param name="v">ベクトル</param>
		/// <returns>Color 値</returns>
		public static Color ToColor(Vector3d v) {
			v *= 255.0;
			return ToColor(new Vector3i(v));
		}

		/// <summary>
		/// Vector3i から Color に変換する
		/// </summary>
		/// <param name="v">ベクトル</param>
		/// <returns>Color 値</returns>
		public static Color ToColor(Vector3i v) {
			v.Clip(0, 255);
			return Color.FromArgb(v.X, v.Y, v.Z);
		}

		/// <summary>
		/// RGBからYUVへ変換する
		/// </summary>
		/// <param name="rgb">RGB値を示すベクトル</param>
		/// <returns>YUV値を示すベクトル</returns>
		public static Vector3d RgbToYuv(Vector3d rgb) {
			return new Vector3d(
				0.2989 * rgb.X + 0.5866 * rgb.Y + 0.1145 * rgb.Z,
				-0.1684 * rgb.X - 0.3311 * rgb.Y + 0.4997 * rgb.Z,
				0.4998 * rgb.X - 0.4187 * rgb.Y - 0.0813 * rgb.Z
			);
		}

		/// <summary>
		/// YUVからRGBへ変換する
		/// </summary>
		/// <param name="yuv">YUV値を示すベクトル</param>
		/// <returns>RGB値を示すベクトル</returns>
		public static Vector3d YuvToRgb(Vector3d yuv) {
			return new Vector3d(
				yuv.X + 1.4022 * yuv.Z,
				yuv.X - 0.3441 * yuv.Y - 0.7139 * yuv.Z,
				yuv.X + 1.7718 * yuv.Y - 0.0012 * yuv.Z
			);
		}

		/// <summary>
		/// RGBからHSVへ変換する
		/// </summary>
		/// <param name="rgb">RGB値を示すベクトル</param>
		/// <returns>HSV値を示すベクトル</returns>
		public static Vector3d RgbToHsv(Vector3d rgb) {
			Vector3d hsv = new Vector3d();
			double max = rgb.Max();
			double min = rgb.Min();

			hsv.Z = max;

			if (max == 0.0) {
				hsv.X = 0.0;
				hsv.Y = 0.0;
			} else {
				double d = max - min;
				hsv.Y = d / max;

				if (d != 0.0) {
					double cr = (max - rgb.X) / d;
					double cg = (max - rgb.Y) / d;
					double cb = (max - rgb.Z) / d;
					if (rgb.X == max)
						hsv.X = cb - cg;
					else if (rgb.Y == max)
						hsv.X = 2.0 + cr - cb;
					else if (rgb.Z == max)
						hsv.X = 4.0 + cg - cr;
					hsv.X *= 60.0;
					hsv.X %= 360.0;
					if (hsv.X < 0.0)
						hsv.X += 360.0;
				} else {
					hsv.X = 0.0;
				}
			}
			
			return hsv;
		}

		/// <summary>
		/// HSVからRGBへ変換する
		/// </summary>
		/// <param name="hsv">HSV値を示すベクトル</param>
		/// <returns>RGB値を示すベクトル</returns>
		public static Vector3d HsvToRgb(Vector3d hsv) {
			if (hsv.Y == 0.0) {
				return new Vector3d(hsv.Z, hsv.Z, hsv.Z);
			} else {
				double t1 = hsv.X / 60.0;
				double t2 = Math.Floor(t1);
				double f = t1 - t2;
				double s = hsv.Y;
				double v = hsv.Z;
				double m = v * (1.0 - s);
				double n = v * (1.0 - s * f);
				double k = v * (1.0 - s * (1.0 - f));
				switch((int)t2 % 6) {
				case 0:
					return new Vector3d(v, k, m);
				case 1:
					return new Vector3d(n, v, m);
				case 2:
					return new Vector3d(m, v, k);
				case 3:
					return new Vector3d(m, n, v);
				case 4:
					return new Vector3d(k, m, v);
				case 5:
					return new Vector3d(v, m, n);
				default:
					return new Vector3d(0.0, 0.0, 0.0);
				}
			}
		}

		/// <summary>
		/// Color からHSV値を示すベクトルに変換する
		/// </summary>
		/// <param name="rgb">色</param>
		/// <returns>HSV値を示すベクトル</returns>
		public static Vector3d ToHsv(Color rgb) {
			return RgbToHsv(ToVector3d(rgb));
		}

		/// <summary>
		/// HSV値を示すベクトルから Color に変換する
		/// </summary>
		/// <param name="hsv">HSV値を示すベクトル</param>
		/// <returns>色</returns>
		public static Color HsvToColor(Vector3d hsv) {
			return ToColor(HsvToRgb(hsv));
		}

		/// <summary>
		/// 指定された色にHSV値での変化を加える
		/// </summary>
		/// <param name="rgb">色</param>
		/// <param name="h">色相</param>
		/// <param name="s">彩度</param>
		/// <param name="v">明度</param>
		/// <returns>変換後の色</returns>
		public static Color AddHsv(Color rgb, double h, double s, double v) {
			Vector3d hsv = ToHsv(rgb);
			hsv.X += h;
			hsv.Y += s;
			hsv.Z += v;
			return HsvToColor(hsv);
		}
	}
}
