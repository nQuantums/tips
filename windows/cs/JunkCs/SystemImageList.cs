using System;
using System.Runtime.InteropServices;

namespace Jk
{
	/// <summary>
	/// システムイメージリストへのアクセスを行うクラス。
	/// </summary>
    internal static class SystemImageList
    {
        #region フィールド
		private static Boolean m_bInitialized = false;
        private static IntPtr m_himlSmall = IntPtr.Zero;
		private static IntPtr m_himlLarge = IntPtr.Zero;
        #endregion

		#region プロパティ
		/// <summary>
		/// 小システムイメージリストハンドルの取得。
		/// </summary>
		public static IntPtr SmallImageList
		{
			get
			{
				if (!m_bInitialized)
					Initialize();
				return m_himlSmall;
			}
		}

		/// <summary>
		/// 大システムイメージリストハンドルの取得。
		/// </summary>
		public static IntPtr LargeImageList
		{
			get
			{
				if (!m_bInitialized)
					Initialize();
				return m_himlLarge;
			}
		}
		#endregion

		#region 内部メソッド
		/// <summary>
		///	変数などを初期化する。
		/// </summary>
		private static void Initialize()
		{
			m_bInitialized = true;

			ShellAPI.SHFILEINFO shInfo = new ShellAPI.SHFILEINFO();
			ShellAPI.SHGFI dwAttribs =
				ShellAPI.SHGFI.SHGFI_USEFILEATTRIBUTES |
				ShellAPI.SHGFI.SHGFI_SMALLICON |
				ShellAPI.SHGFI.SHGFI_SYSICONINDEX;
			m_himlSmall = ShellAPI.SHGetFileInfoW(".txt", ShellAPI.FILE_ATTRIBUTE_NORMAL, out shInfo, (uint)Marshal.SizeOf(shInfo), dwAttribs);
			if (m_himlSmall.Equals(IntPtr.Zero))
				throw new Exception("小システムイメージリストを取得できませんでした。");

			dwAttribs =
				ShellAPI.SHGFI.SHGFI_USEFILEATTRIBUTES |
				ShellAPI.SHGFI.SHGFI_LARGEICON |
				ShellAPI.SHGFI.SHGFI_SYSICONINDEX;
			m_himlLarge = ShellAPI.SHGetFileInfoW(".txt", ShellAPI.FILE_ATTRIBUTE_NORMAL, out shInfo, (uint)Marshal.SizeOf(shInfo), dwAttribs);
			if (m_himlLarge.Equals(IntPtr.Zero))
				throw new Exception("大システムイメージリストを取得できませんでした。");
		}
		#endregion
    }
}
