using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Junk {
	/// <summary>
	/// ファイルパスに関するヘルパ処理クラス
	/// </summary>
	public class PathUtil {
		/// <summary>
		/// 指定された基底パスからの相対パスを取得する
		/// </summary>
		/// <param name="rootPath">基底パス</param>
		/// <param name="fullPath">絶対パス</param>
		/// <returns>相対パス　</returns>
		public static string GetRelativePath(string rootPath, string fullPath) {
			if (String.IsNullOrEmpty(rootPath) || String.IsNullOrEmpty(fullPath)) {
				return "";
			}

			rootPath = rootPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
			fullPath = fullPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
			
			if (fullPath.Equals(rootPath)) {
				return string.Empty;
			} else if (fullPath.StartsWith(rootPath + "\\", StringComparison.OrdinalIgnoreCase)) {
				return fullPath.Substring(rootPath.Length + 1);
			} else {
				return fullPath;
			}
		}

		/// <summary>
		/// 比較用にパスを正規化する
		/// </summary>
		/// <param name="path">パス</param>
		/// <returns>正規化されたパス</returns>
		public static string Normalize(string path) {
			return Path.GetFullPath(new Uri(path).LocalPath)
					   .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
					   .ToUpperInvariant();
		}

		/// <summary>
		/// パスを比較する
		/// </summary>
		/// <param name="path1">パス1</param>
		/// <param name="path2">パス2</param>
		/// <returns>比較結果</returns>
		public static int Compare(string path1, string path2) {
			return string.Compare(Normalize(path1), Normalize(path2));
		}
	}
}
