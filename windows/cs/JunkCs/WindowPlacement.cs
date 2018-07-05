using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Jk
{
	/// <summary>
	/// ウィンドウ配置クラス
	/// </summary>
	public class WindowPlacement
	{
		/// <summary>
		/// ウィンドウ位置  Ｘ
		/// </summary>
		public int Left;
		/// <summary>
		/// ウィンドウ位置  Ｙ
		/// </summary>
		public int Top;
		/// <summary>
		/// ウィンドウ位置  長さ
		/// </summary>
		public int Width;
		/// <summary>
		/// ウィンドウ位置  高さ
		/// </summary>
		public int Height;
		/// <summary>
		/// 最小化時のX位置。
		/// </summary>
		public int MinX;
		/// <summary>
		/// 最小化時のY位置。
		/// </summary>
		public int MinY;
		/// <summary>
		/// 最大化時のX位置。
		/// </summary>
		public int MaxX;
		/// <summary>
		/// 最大化時のY位置。
		/// </summary>
		public int MaxY;
		/// <summary>
		///	ウィンドウ状態。
		/// </summary>
		public FormWindowState WindowState;

		/// <summary>
		/// 指定されたフォームにアタッチし、配置をフォームに対して適用する
		/// フォームが閉じられる際にフォームから配置を取得する
		/// </summary>
		/// <param name="form">アタッチ対象フォーム</param>
		public void Attach(Form form)
		{
			SetPlacementTo(form, false);
			form.FormClosing += new FormClosingEventHandler(form_FormClosing);
		}

		/// <summary>
		/// 指定されたフォームからウィンドウ位置に関する情報を取得する。
		/// </summary>
		/// <param name="form">位置情報の取得元フォーム。</param>
		public void GetPlacementFrom(Form form)
		{
			WINDOWPLACEMENT wndpl = new WINDOWPLACEMENT();
			wndpl.length = Marshal.SizeOf(wndpl);
			GetWindowPlacement(form.Handle, ref wndpl);

			this.Left = wndpl.rcNormalPosition.left;
			this.Top = wndpl.rcNormalPosition.top;
			this.Width = wndpl.rcNormalPosition.right - wndpl.rcNormalPosition.left;
			this.Height = wndpl.rcNormalPosition.bottom - wndpl.rcNormalPosition.top;
			this.MinX = wndpl.ptMinPosition.x;
			this.MinY = wndpl.ptMinPosition.y;
			this.MaxX = wndpl.ptMaxPosition.x;
			this.MaxY = wndpl.ptMaxPosition.y;
			this.WindowState = form.WindowState;
		}

		/// <summary>
		/// 指定されたフォームへウィンドウ位置に関する情報を設定する。
		/// </summary>
		/// <param name="form">位置情報の設定先フォーム。</param>
		/// <param name="fixedSize">フォームのサイズを変更したくないときは true を指定します。</param>
		public void SetPlacementTo(Form form, bool fixedSize)
		{
			if (this.Width <= 0 || this.Height <= 0)
			{
				//	幅と高さが0未満の場合は設定しない
				return;
			}

			WINDOWPLACEMENT wndpl = new WINDOWPLACEMENT();
			wndpl.length = Marshal.SizeOf(wndpl);
			wndpl.flags = 0;
			wndpl.rcNormalPosition.left = this.Left;
			wndpl.rcNormalPosition.top = this.Top;
			wndpl.rcNormalPosition.right = this.Left + this.Width;
			wndpl.rcNormalPosition.bottom = this.Top + this.Height;
			wndpl.ptMinPosition.x = this.MinX;
			wndpl.ptMinPosition.y = this.MinY;
			wndpl.ptMaxPosition.x = this.MaxX;
			wndpl.ptMaxPosition.y = this.MaxY;
			switch (this.WindowState)
			{
				case FormWindowState.Maximized:
					wndpl.showCmd = SW_SHOWMAXIMIZED;
					break;
				default:
					wndpl.showCmd = SW_SHOWNORMAL;
					break;
			}

			//	位置がディスプレイの外だとまずいので位置を補正する
			IntPtr hMonitor = MonitorFromRect(ref wndpl.rcNormalPosition, MONITOR_DEFAULTTONEAREST);
			MONITORINFO mi = new MONITORINFO();
			mi.cbSize = Marshal.SizeOf(mi);
			GetMonitorInfoW(hMonitor, ref mi);
			if (wndpl.rcNormalPosition.right > mi.rcMonitor.right)
			{
				wndpl.rcNormalPosition.left -= wndpl.rcNormalPosition.right - mi.rcMonitor.right;
				wndpl.rcNormalPosition.right = mi.rcMonitor.right;
			}
			if (wndpl.rcNormalPosition.left < mi.rcMonitor.left)
			{
				wndpl.rcNormalPosition.right += mi.rcMonitor.left - wndpl.rcNormalPosition.left;
				wndpl.rcNormalPosition.left = mi.rcMonitor.left;
			}
			if (wndpl.rcNormalPosition.bottom > mi.rcMonitor.bottom)
			{
				wndpl.rcNormalPosition.top -= wndpl.rcNormalPosition.bottom - mi.rcMonitor.bottom;
				wndpl.rcNormalPosition.bottom = mi.rcMonitor.bottom;
			}
			if (wndpl.rcNormalPosition.top < mi.rcMonitor.top)
			{
				wndpl.rcNormalPosition.bottom += mi.rcMonitor.top - wndpl.rcNormalPosition.top;
				wndpl.rcNormalPosition.top = mi.rcMonitor.top;
			}

			//	サイズを固定にしたい場合はサイズが変わらないようにする
			if (fixedSize)
			{
				WINDOWPLACEMENT t = new WINDOWPLACEMENT();
				t.length = Marshal.SizeOf(t);
				GetWindowPlacement(form.Handle, ref t);
				wndpl.rcNormalPosition.right = wndpl.rcNormalPosition.left + (t.rcNormalPosition.right - t.rcNormalPosition.left);
				wndpl.rcNormalPosition.bottom = wndpl.rcNormalPosition.top + (t.rcNormalPosition.bottom - t.rcNormalPosition.top);
			}

			SetWindowPlacement(form.Handle, ref wndpl);
		}

		/// <summary>
		///	アタッチ先フォームのクローズイベント処理
		/// </summary>
		void form_FormClosing(object sender, FormClosingEventArgs e)
		{
			Form form = (Form)sender;
			form.FormClosing -= new FormClosingEventHandler(form_FormClosing);
			GetPlacementFrom(form);
		}

		#region 内部用
		const int SW_HIDE = 0;
		const int SW_SHOWNORMAL = 1;
		const int SW_NORMAL = 1;
		const int SW_SHOWMINIMIZED = 2;
		const int SW_SHOWMAXIMIZED = 3;
		const int SW_MAXIMIZE = 3;
		const int SW_SHOWNOACTIVATE = 4;
		const int SW_SHOW = 5;
		const int SW_MINIMIZE = 6;
		const int SW_SHOWMINNOACTIVE = 7;
		const int SW_SHOWNA = 8;
		const int SW_RESTORE = 9;
		const int SW_SHOWDEFAULT = 10;
		const int SW_FORCEMINIMIZE = 11;
		const int SW_MAX = 11;

		const int MONITOR_DEFAULTTONULL = 0x00000000;
		const int MONITOR_DEFAULTTOPRIMARY = 0x00000001;
		const int MONITOR_DEFAULTTONEAREST = 0x00000002;

		[StructLayout(LayoutKind.Sequential)]
		struct POINT
		{
			public int x;
			public int y;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct RECT
		{
			public int left;
			public int top;
			public int right;
			public int bottom;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct WINDOWPLACEMENT
		{
			public int length;
			public int flags;
			public int showCmd;
			public POINT ptMinPosition;
			public POINT ptMaxPosition;
			public RECT rcNormalPosition;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct MONITORINFO
		{
			public int cbSize;
			public RECT rcMonitor;
			public RECT rcWork;
			public int dwFlags;
		}

		[DllImport("user32.dll")]
		extern static bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);
		[DllImport("user32.dll")]
		extern static bool SetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);
		[DllImport("user32.dll")]
		extern static IntPtr MonitorFromRect(ref RECT lprc, int dwFlags);
		[DllImport("user32.dll")]
		extern static bool GetMonitorInfoW(IntPtr hMonitor, ref MONITORINFO lpmi);
		#endregion
	}
}
