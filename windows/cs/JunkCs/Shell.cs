using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Jk
{
	/// <summary>
	/// シェル機能のヘルパクラス
	/// </summary>
	public static class Shell
	{
		/// <summary>
		/// ショートカットファイル作成
		/// </summary>
		/// <param name="filename">ターゲットファイルパス名</param>
		/// <param name="shortcutPath">作成するショートカットファイルパス名</param>
		/// <param name="description">説明</param>
		public static void CreateShortcut(string filename, string shortcutPath, string description)
		{
			ShellLink link = new ShellLink();
			link.ShortCutFile = shortcutPath;
			link.Target = filename;
			link.Description = description;
			link.Save();
			link.Dispose();
		}

		/// <summary>
		/// 指定されたショートカットのターゲットパスの取得
		/// </summary>
		/// <param name="shortcutPath">開くショートカットファイルパス名</param>
		/// <returns>ショートカットのターゲットファイルパス名</returns>
		public static string GetShortcutTargetPath(string shortcutPath)
		{
			ShellLink link = new ShellLink(shortcutPath);
			string target = link.Target;
			link.Dispose();
			return target;
		}

		/// <summary>
		/// 指定されたファイルを削除する
		/// </summary>
		/// <param name="files">ファイル配列</param>
		/// <param name="hwnd">通知先ウィンドウハンドル</param>
		public static void Delete(string[] files, IntPtr hwnd)
		{
			if (files.Length == 0)
				return;

			StringBuilder sb = new StringBuilder();
			foreach (string file in files)
			{
				sb.Append(file);
				sb.Append('\0');
			}
			sb.Append('\0');

			ShellAPI.SHFILEOPSTRUCT sh = new ShellAPI.SHFILEOPSTRUCT();

			sh.hwnd = hwnd;
			sh.wFunc = ShellAPI.FOFunc.FO_DELETE;
			sh.pFrom = sb.ToString();
			sh.pTo = null;
			sh.fFlags = ShellAPI.FOFlags.FOF_ALLOWUNDO;
			sh.fAnyOperationsAborted = 1;
			sh.hNameMappings = IntPtr.Zero;
			sh.lpszProgressTitle = "削除しています";

			ShellAPI.SHFileOperation(ref sh);
		}

		/// <summary>
		/// 指定されたシェルアイテムを削除する
		/// </summary>
		/// <param name="files">シェルアイテム配列</param>
		/// <param name="hwnd">通知先ウィンドウハンドル</param>
		public static void Delete(ShellItem[] files, IntPtr hwnd)
		{
			if (files.Length == 0)
				return;

			StringBuilder sb = new StringBuilder();
			foreach (ShellItem si in files)
			{
				sb.Append(si.Path);
				sb.Append('\0');
			}
			sb.Append('\0');

			ShellAPI.SHFILEOPSTRUCT sh = new ShellAPI.SHFILEOPSTRUCT();

			sh.hwnd = hwnd;
			sh.wFunc = ShellAPI.FOFunc.FO_DELETE;
			sh.pFrom = sb.ToString();
			sh.pTo = null;
			sh.fFlags = ShellAPI.FOFlags.FOF_ALLOWUNDO;
			sh.fAnyOperationsAborted = 1;
			sh.hNameMappings = IntPtr.Zero;
			sh.lpszProgressTitle = "削除しています";

			ShellAPI.SHFileOperation(ref sh);
		}

		/// <summary>
		/// 指定されたファイルコピーする
		/// </summary>
		/// <param name="files">ファイル配列</param>
		/// <param name="pathTo">コピー先ディレクトリパス</param>
		/// <param name="hwnd">通知先ウィンドウハンドル</param>
		public static void Copy(string[] files, string pathTo, IntPtr hwnd)
		{
			if (files.Length == 0)
				return;

			StringBuilder sb = new StringBuilder();
			foreach (string file in files)
			{
				sb.Append(file);
				sb.Append('\0');
			}
			sb.Append('\0');

			ShellAPI.SHFILEOPSTRUCT sh = new ShellAPI.SHFILEOPSTRUCT();

			sh.hwnd = hwnd;
			sh.wFunc = ShellAPI.FOFunc.FO_COPY;
			sh.pFrom = sb.ToString();
			sh.pTo =pathTo;
			sh.fFlags = ShellAPI.FOFlags.FOF_ALLOWUNDO;
			sh.fAnyOperationsAborted = 1;
			sh.hNameMappings = IntPtr.Zero;
			sh.lpszProgressTitle = "コピーしています";

			ShellAPI.SHFileOperation(ref sh);
		}

		/// <summary>
		/// 指定されたファイルコピーする
		/// </summary>
		/// <param name="files">ファイル配列</param>
		/// <param name="pathTo">コピー先ディレクトリパス</param>
		/// <param name="hwnd">通知先ウィンドウハンドル</param>
		public static void Copy(ShellItem[] files, string pathTo, IntPtr hwnd)
		{
			if (files.Length == 0)
				return;

			StringBuilder sb = new StringBuilder();
			foreach (ShellItem file in files)
			{
				sb.Append(file.Path);
				sb.Append('\0');
			}
			sb.Append('\0');

			ShellAPI.SHFILEOPSTRUCT sh = new ShellAPI.SHFILEOPSTRUCT();

			sh.hwnd = hwnd;
			sh.wFunc = ShellAPI.FOFunc.FO_COPY;
			sh.pFrom = sb.ToString();
			sh.pTo = pathTo;
			sh.fFlags = ShellAPI.FOFlags.FOF_ALLOWUNDO;
			sh.fAnyOperationsAborted = 1;
			sh.hNameMappings = IntPtr.Zero;
			sh.lpszProgressTitle = "コピーしています";

			ShellAPI.SHFileOperation(ref sh);
		}
	}
}
