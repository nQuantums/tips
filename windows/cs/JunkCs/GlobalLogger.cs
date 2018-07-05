using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jk {
	/// <summary>
	/// どこからでも呼び出せるロガー、ログフォルダは実行ファイルと同じ階層に作られる、デバッグ時に使うことを想定しておりリリース時は使わないこと
	/// </summary>
	public static class GlobalLogger {
		static Logger _Logger = new Logger(AppDomain.CurrentDomain.BaseDirectory, "global", ".log");

		/// <summary>
		/// ログ文字列を１行追加する
		/// </summary>
		/// <param name="text">ログ追加文字列</param>
		/// <returns>ログに出力されたCSVデータが返る</returns>
		public static List<string> AddLogLine(params string[] text) {
			lock (_Logger) {
				return _Logger.AddLogLine(DateTime.Now, text);
			}
		}

		/// <summary>
		/// 拡張ログファイルを作成する
		/// </summary>
		/// <param name="contents">ファイル内容</param>
		/// <param name="prefix2">ファイル名に付与される２番目のプレフィックス</param>
		/// <param name="ext">ファイルの拡張子</param>
		public static void CreateFile(string contents, string prefix2, string ext) {
			lock (_Logger) {
				_Logger.CreateFile(contents, prefix2, ext);
			}
		}
	}
}
