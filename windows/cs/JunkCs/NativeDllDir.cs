using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Junk {
	/// <summary>
	/// DllImport用に、x86用のDLLのあるディレクトリとx64用のDLLのあるディレクトリを設定するためのクラスです。
	/// </summary>
	public static class NativeDllDir {
		/// <summary>
		/// DllImport用に、x86用のDLLのあるディレクトリとx64用のDLLのあるディレクトリを設定します。
		/// </summary>
		/// <param name="x86DllDir">x86環境用のDLLを配置したディレクトリを指定します。指定しなければカレントディレクトリとなります。</param>
		/// <param name="x64DllDir">x64環境用のDLLを配置したディレクトリを指定します。指定しなければカレントディレクトリとなります。</param>
		/// <returns>設定に成功したらtrue。</returns>
		/// <exception cref="PlatformNotSupportedException">x86でもx64でもない場合の例外です。</exception>
		public static void Set(string x86DllDir = null, string x64DllDir = null) {
			if (IntPtr.Size == 8) {
				// 64bitっぽい
				Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + x64DllDir);
				return;
			}
			if (IntPtr.Size == 4) {
				// 32bitっぽい
				Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + x86DllDir);
				return;
			}

			// いずれでもない
			throw new PlatformNotSupportedException();
		}

		//[System.Runtime.InteropServices.DllImport("kernel32", SetLastError = true)]
		//private static extern bool SetDllDirectory(string lpPathName);
	}
}
