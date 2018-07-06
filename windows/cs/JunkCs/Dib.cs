using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Diagnostics;

namespace Junk {
	public class Dib : IDisposable {
		#region PInvoke
		const int DIB_RGB_COLORS = 0;
		const int BI_RGB = 0;

		[StructLayout(LayoutKind.Sequential)]
		public struct BITMAPINFO {
			public Int32 biSize;
			public Int32 biWidth;
			public Int32 biHeight;
			public Int16 biPlanes;
			public Int16 biBitCount;
			public Int32 biCompression;
			public Int32 biSizeImage;
			public Int32 biXPelsPerMeter;
			public Int32 biYPelsPerMeter;
			public Int32 biClrUsed;
			public Int32 biClrImportant;
		}

		public enum TernaryRasterOperations : uint {
			SRCCOPY = 0x00CC0020,
			SRCPAINT = 0x00EE0086,
			SRCAND = 0x008800C6,
			SRCINVERT = 0x00660046,
			SRCERASE = 0x00440328,
			NOTSRCCOPY = 0x00330008,
			NOTSRCERASE = 0x001100A6,
			MERGECOPY = 0x00C000CA,
			MERGEPAINT = 0x00BB0226,
			PATCOPY = 0x00F00021,
			PATPAINT = 0x00FB0A09,
			PATINVERT = 0x005A0049,
			DSTINVERT = 0x00550009,
			BLACKNESS = 0x00000042,
			WHITENESS = 0x00FF0062,
			CAPTUREBLT = 0x40000000 //only if WinVer >= 5.0.0 (see wingdi.h)
		}

		private enum StretchBltMode : int {
			STRETCH_ANDSCANS = 1,
			STRETCH_ORSCANS = 2,
			STRETCH_DELETESCANS = 3,
			STRETCH_HALFTONE = 4,
		}

		[DllImport("gdi32.dll")]
		static extern IntPtr CreateDIBSection(IntPtr hdc, [In] ref BITMAPINFO pbmi, uint pila, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);
		[DllImport("gdi32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool DeleteObject(IntPtr hObject);
		[DllImport("gdi32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool BitBlt(IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, TernaryRasterOperations dwRop);
		[DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
		static extern IntPtr CreateDCW(string lpszDriver, IntPtr lpszDevice, IntPtr lpszOutput, IntPtr lpInitData);
		[DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
		static extern IntPtr CreateCompatibleDC(IntPtr hdc);
		[DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
		static extern bool DeleteDC(IntPtr hdc);
		[DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
		static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);
		[DllImport("gdi32.dll")]
		static extern bool StretchBlt(IntPtr hdcDest, int nXOriginDest, int nYOriginDest, int nWidthDest, int nHeightDest, IntPtr hdcSrc, int nXOriginSrc, int nYOriginSrc, int nWidthSrc, int nHeightSrc, TernaryRasterOperations dwRop);
		[DllImport("gdi32.dll")]
		static extern int SetStretchBltMode(IntPtr hdc, StretchBltMode iStretchMode);
		[DllImport("Kernel32.dll", EntryPoint="RtlMoveMemory", SetLastError=false)]
		static extern void MoveMemory(IntPtr dest, IntPtr src, int size);
		#endregion

		private bool disposed = false;

		/// <summary>
		/// ビットマップハンドル
		/// </summary>
		public IntPtr hBmp;

		/// <summary>
		/// DIBデータ先頭へのポインタ
		/// </summary>
		public IntPtr pPtr;

		/// <summary>
		/// DIB幅(px)
		/// </summary>
		public int Width;

		/// <summary>
		/// DIB高さ(px)
		/// </summary>
		public int Height;

		/// <summary>
		/// １ラインのバイト数(bytes)
		/// </summary>
		public int Stride;

		/// <summary>
		/// DIBデータのサイズ(bytes)
		/// </summary>
		public int Size;

		/// <summary>
		/// 色深度(bits)
		/// </summary>
		public int ColorBits;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		public Dib() {
		}

		/// <summary>
		/// ファイナライザ
		/// </summary>
		~Dib() {
			Dispose(false);
		}

		/// <summary>
		/// DIBを作成する
		/// </summary>
		/// <param name="width">幅(px)</param>
		/// <param name="height">高さ(px)</param>
		/// <param name="colorBits">色深度(bits)</param>
		/// <returns>true:成功、false:失敗</returns>
		public bool Create(int width, int height, int colorBits) {
			Delete();

			int absHeight = Math.Abs(height);

			BITMAPINFO bmi = new BITMAPINFO();
			int stride = (width * (colorBits / 8) + 3) & ~3;
			bmi.biSize = 40;
			bmi.biWidth = width;
			bmi.biHeight = height;
			bmi.biPlanes = 1;
			bmi.biBitCount = (short)colorBits;
			bmi.biCompression = BI_RGB;
			bmi.biSizeImage = stride * absHeight;
			bmi.biXPelsPerMeter = 0;
			bmi.biYPelsPerMeter = 0;
			bmi.biClrUsed = 0;
			bmi.biClrImportant = 0;
			this.hBmp = CreateDIBSection(IntPtr.Zero, ref bmi, DIB_RGB_COLORS, out this.pPtr, IntPtr.Zero, 0);
			if (this.hBmp == IntPtr.Zero)
				return false;

			this.Width = width;
			this.Height = absHeight;
			this.Stride = stride;
			this.Size = bmi.biSizeImage;
			this.ColorBits = colorBits;

			GC.AddMemoryPressure(this.Size);

			return true;
		}

		public void Delete() {
			if (this.hBmp != IntPtr.Zero) {
				DeleteObject(this.hBmp);
				this.hBmp = IntPtr.Zero;
			}
		}

		public void Dispose() {
			Dispose(true);
            GC.SuppressFinalize(this);
		}

		public Dib Clone() {
			return this.Clone(this.ColorBits);
		}

		public Dib Clone(int colorBits) {
			Dib dib = null;
			IntPtr hDispDC = IntPtr.Zero;
			IntPtr hSrcDC = IntPtr.Zero;
			IntPtr hDstDC = IntPtr.Zero;
			IntPtr hOldSrcBmp = IntPtr.Zero;
			IntPtr hOldDstBmp = IntPtr.Zero;
			try {
				dib = new Dib();
				if (!dib.Create(this.Width, this.Height, colorBits == 0 ? this.ColorBits : colorBits))
					return null;
				hDispDC = CreateDCW("DISPLAY", IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
				if (hDispDC == IntPtr.Zero)
					return null;
				hSrcDC = CreateCompatibleDC(hDispDC);
				if (hSrcDC == IntPtr.Zero)
					return null;
				hDstDC = CreateCompatibleDC(hDispDC);
				if (hDstDC == IntPtr.Zero)
					return null;

				hOldSrcBmp = SelectObject(hSrcDC, this.hBmp);
				hOldDstBmp = SelectObject(hDstDC, dib.hBmp);

				BitBlt(hDstDC, 0, 0, this.Width, this.Height, hSrcDC, 0, 0, TernaryRasterOperations.SRCCOPY);

				Dib tmp = dib;
				dib = null;
				return tmp;
			} finally {
				if (dib != null)
					dib.Dispose();
				if (hDispDC != IntPtr.Zero)
					DeleteDC(hDispDC);
				if (hSrcDC != IntPtr.Zero) {
					if (hOldSrcBmp != IntPtr.Zero)
						SelectObject(hSrcDC, hOldSrcBmp);
					DeleteDC(hSrcDC);
				}
				if (hDstDC != IntPtr.Zero) {
					if (hOldDstBmp != IntPtr.Zero)
						SelectObject(hDstDC, hOldDstBmp);
					DeleteDC(hDstDC);
				}
			}
		}

		public void DrawTo(Graphics g, int x, int y) {
			IntPtr hDC = IntPtr.Zero;
			try {
				hDC = g.GetHdc();
				DrawTo(hDC, x, y);
			} finally {
				if (hDC != IntPtr.Zero)
					g.ReleaseHdc(hDC);
			}
		}
		public void DrawTo(Graphics g, int x, int y, int width, int height) {
			IntPtr hDC = IntPtr.Zero;
			try {
				hDC = g.GetHdc();
				DrawTo(hDC, x, y, width, height);
			} finally {
				if (hDC != IntPtr.Zero)
					g.ReleaseHdc(hDC);
			}
		}

		public void DrawTo(IntPtr hDC, int x, int y) {
			IntPtr hDibDC = IntPtr.Zero;
			IntPtr hOldBmp = IntPtr.Zero;
			try {
				hDibDC = CreateCompatibleDC(hDC);
				if (hDibDC == IntPtr.Zero)
					return;

				hOldBmp = SelectObject(hDibDC, this.hBmp);

				BitBlt(hDC, x, y, this.Width, this.Height, hDibDC, 0, 0, TernaryRasterOperations.SRCCOPY);
			} finally {
				if (hDibDC != IntPtr.Zero) {
					if (hOldBmp != IntPtr.Zero)
						SelectObject(hDibDC, hOldBmp);
					DeleteDC(hDibDC);
				}
			}
		}
		public void DrawTo(IntPtr hDC, int x, int y, int width, int height) {
			IntPtr hDibDC = IntPtr.Zero;
			IntPtr hOldBmp = IntPtr.Zero;
			try {
				hDibDC = CreateCompatibleDC(hDC);
				if (hDibDC == IntPtr.Zero)
					return;

				hOldBmp = SelectObject(hDibDC, this.hBmp);

				SetStretchBltMode(hDC, StretchBltMode.STRETCH_DELETESCANS);
				StretchBlt(hDC, x, y, width, height, hDibDC, 0, 0, this.Width, this.Height, TernaryRasterOperations.SRCCOPY);
			} finally {
				if (hDibDC != IntPtr.Zero) {
					if (hOldBmp != IntPtr.Zero)
						SelectObject(hDibDC, hOldBmp);
					DeleteDC(hDibDC);
				}
			}
		}

		public void CopyFromScreen(int sx, int sy, int dx, int dy, int w, int h) {
			IntPtr hDispDC = IntPtr.Zero;
			try {
				hDispDC = CreateDCW("DISPLAY", IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
				if (hDispDC == IntPtr.Zero)
					return;
				CopyFrom(hDispDC, sx, sy, dx, dy, w, h);
			} finally {
				if (hDispDC != IntPtr.Zero)
					DeleteDC(hDispDC);
			}
		}

		public void CopyFrom(IntPtr hDC, int sx, int sy, int dx, int dy, int w, int h) {
			IntPtr hDibDC = IntPtr.Zero;
			IntPtr hOldBmp = IntPtr.Zero;
			try {
				hDibDC = CreateCompatibleDC(hDC);
				if (hDibDC == IntPtr.Zero)
					return;

				hOldBmp = SelectObject(hDibDC, this.hBmp);

				BitBlt(hDibDC, dx, dy, w, h, hDC, sx, sy, TernaryRasterOperations.SRCCOPY);
			} finally {
				if (hDibDC != IntPtr.Zero) {
					if (hOldBmp != IntPtr.Zero)
						SelectObject(hDibDC, hOldBmp);
					DeleteDC(hDibDC);
				}
			}
		}

		public void DrawTo(Dib dib, int x, int y) {
			IntPtr hDispDC = IntPtr.Zero;
			IntPtr hDibDC = IntPtr.Zero;
			IntPtr hOldBmp = IntPtr.Zero;
			try {
				hDispDC = CreateDCW("DISPLAY", IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
				if (hDispDC == IntPtr.Zero)
					return;
				hDibDC = CreateCompatibleDC(hDispDC);
				if (hDibDC == IntPtr.Zero)
					return;

				hOldBmp = SelectObject(hDibDC, dib.hBmp);

				DrawTo(hDibDC, x, y);
			} finally {
				if (hDispDC != IntPtr.Zero)
					DeleteDC(hDispDC);
				if (hDibDC != IntPtr.Zero) {
					if (hOldBmp != IntPtr.Zero)
						SelectObject(hDibDC, hOldBmp);
					DeleteDC(hDibDC);
				}
			}
		}

		public bool MemoryCopyFrom(Dib dib)
		{
			if(this.Size != dib.Size)
				return false;
			MoveMemory(this.pPtr, dib.pPtr, this.Size);
			return true;
		}

		protected virtual void Dispose(bool disposing) {
			if (!disposed) {
				if (disposing) {
					// Free other state (managed objects).
				}

				Delete();

				// Free your own state (unmanaged objects).
				// Set large fields to null.
				this.disposed = true;
			}
		}
	}
}
