using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using System.ComponentModel;
using System.Diagnostics;
using SevenZip;

namespace ProductInfoExtractor {
	class Program {
		#region PInvoke
		[DllImport("msi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		static extern uint MsiOpenDatabaseW(string szDatabasePath, IntPtr phPersist, out IntPtr phDatabase);
		[DllImport("msi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		static extern uint MsiOpenPackageW(string szPackagePath, out IntPtr hProduct);
		[DllImport("msi.dll", ExactSpelling = true)]
		static extern uint MsiCloseHandle(IntPtr hAny);
		[DllImport("msi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		static extern uint MsiDatabaseExportW(IntPtr hDatabase, string szTableName, string szFolderPath, string szFileName);
		[DllImport("msi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		static extern uint MsiGetPropertyW(IntPtr hInstall, string szName, StringBuilder szValueBuf, ref uint pchValueBuf);
		#endregion

		const string SevenZipExe = @"C:\Program Files\7-Zip\7z.exe";
		public static string WorkDir { get; private set; }
		static int _TmpFileCount;
		static Semaphore Sem = new Semaphore(20, 20);

		const int MSIDBOPEN_READONLY = 0;  // database open read-only, no persistent changes
		const int MSIDBOPEN_TRANSACT = 1;  // database read/write in transaction mode
		const int MSIDBOPEN_DIRECT = 2;  // database direct read/write without transaction
		const int MSIDBOPEN_CREATE = 3;  // create new database, transact mode read/write
		const int MSIDBOPEN_CREATEDIRECT = 4;  // create new database, direct mode read/write
		const int MSIDBOPEN_PATCHFILE = 16; // add flag to indicate patch file

		/// <summary>
		/// アーカイブファイル内のファイル情報
		/// </summary>
		public class FileInArchiveInfo {
			public string FileName;
			public long Size;
		}

		/// <summary>
		/// プロダクト情報
		/// </summary>
		public class ProductInfo {
			public string ProductName;
			public string ProductCode;
			public string ProductVersion;
		}

		static int Main(string[] args) {
			if (args.Length < 1) {
				Console.WriteLine("指定ファイルからプロダクト情報を抽出し表示します。");
				Console.WriteLine("");
				Console.WriteLine("	udage : ProductInfoExtractor.exe <アーカイブファイル>");
				Console.WriteLine("");
				Console.WriteLine("<アーカイブファイル> : *.msi, *.exe, *.cab などなんでも");
				return 0;
			}

			ThreadPool.SetMinThreads(100, 100);

			//SevenZipBase.SetLibraryPath(@"C:\Program Files\7-Zip\7z.dll");
			WorkDir = Path.Combine(Directory.GetCurrentDirectory(), "ProductInfoExtractorTmp");
			DeleteTempDir();
			CreateTempDir();

			var productInfos = new List<ProductInfo>();
			//ExtractAndGetProductInfos(args[0], productInfos);
			ExtractAndGetProductInfosAsync(args[0], productInfos);

			foreach (var pi in productInfos) {
				Console.WriteLine($"ProductName: {pi.ProductName}");
				Console.WriteLine($"ProductCode: {pi.ProductCode}");
				Console.WriteLine($"ProductVersion: {pi.ProductVersion}");
			}

			DeleteTempDir();

			return 0;
		}

		/// <summary>
		/// 指定アーカイブファイルを展開しつつMSIのプロダクト情報を抽出する
		/// </summary>
		/// <param name="archiveFile">アーカイブファイルパス名</param>
		/// <param name="productInfos">プロダクト情報がここに追加されていく</param>
		/// <param name="deleteFile">処理終了後に入力アーカイブファイルを削除するかどうか</param>
		static void ExtractAndGetProductInfosAsync(string archiveFile, List<ProductInfo> productInfos) {
			var pi = GetProductInfo(archiveFile);
			if (pi != null) {
				lock (productInfos) {
					productInfos.Add(pi);
				}
			} else {
				var tasks = new List<Task>();
				foreach (var f in GetFilesInArchive(archiveFile)) {
					Sem.WaitOne();
					tasks.Add(Task.Run(() => {
						try {
							var tmpFile = ExtractFile(archiveFile, f.FileName);
							ExtractAndGetProductInfosAsync(tmpFile, productInfos);
							File.Delete(tmpFile);
						} finally {
							Sem.Release();
						}
					}));
				}
				Task.WaitAll(tasks.ToArray());
			}
		}

		/// <summary>
		/// 指定アーカイブファイルを展開しつつMSIのプロダクト情報を抽出する
		/// </summary>
		/// <param name="archiveFile">アーカイブファイルパス名</param>
		/// <param name="productInfos">プロダクト情報がここに追加されていく</param>
		static void ExtractAndGetProductInfos(string archiveFile, List<ProductInfo> productInfos) {
			var pi = GetProductInfo(archiveFile);
			if (pi != null) {
				lock (productInfos) {
					productInfos.Add(pi);
				}
			} else {
				foreach (var f in GetFilesInArchive(archiveFile)) {
					var tmpFile = ExtractFile(archiveFile, f.FileName);
					ExtractAndGetProductInfos(tmpFile, productInfos);
					File.Delete(tmpFile);
				}
			}
		}

		/// <summary>
		/// アーカイブファイル内の指定ファイルを一時ファイルに解凍する
		/// </summary>
		/// <param name="archiveFile">アーカイブファイルパス名</param>
		/// <param name="extractFile">アーカイブファイル内の解凍するファイル名</param>
		/// <returns>作成された一時ファイルパス名</returns>
		static string ExtractFile(string archiveFile, string extractFile) {
			var tmpFile = NewTmpFileName();
			Action<StreamReader> outputProc = sr => {
				var buffer = new byte[1024 * 1024 * 10];
				var bs = sr.BaseStream;
				int n;
				using (var fs = new FileStream(tmpFile, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite)) {
					while ((n = bs.Read(buffer, 0, buffer.Length)) != 0) {
						fs.Write(buffer, 0, n);
					}
				}
			};
			SevenZip($@"e -sccWIN -so ""{archiveFile}"" ""{extractFile}""", outputProc);
			return tmpFile;
		}

		/// <summary>
		/// 指定されたアーカイブファイル内で1MB以上のファイルを列挙する
		/// </summary>
		/// <param name="file">アーカイブファイルパス名</param>
		/// <returns>アーカイブ内のファイル情報のリスト</returns>
		static List<FileInArchiveInfo> GetFilesInArchive(string file) {
			var result = new List<FileInArchiveInfo>();
			int state = 0;
			Action<string> lineProc = line => {
				switch (state) {
				case 0:
					if (line.StartsWith("-------------------")) {
						state = 1;
					}
					break;
				case 1:
					if (line.StartsWith("-------------------")) {
						state = 2;
					} else {
						var isDir = line.Substring(20, 1);
						if (isDir != "D") {
							var sizeStr = line.Substring(26, 12).Trim();
							var fileName = line.Substring(53);
							var size = long.Parse(sizeStr);
							if (1024 * 1024 * 1 <= size) {
								result.Add(new FileInArchiveInfo {
									FileName = fileName,
									Size = size
								});
							}
						}
					}
					break;
				case 2:
					break;
				}
			};
			SevenZip($@"l -sccWIN -scsUTF-8 ""{file}""", lineProc);
			return result;
		}

		/// <summary>
		/// 7z.exe を指定引数で実行し標準出力を１ラインずつ処理する
		/// </summary>
		/// <param name="args">7z.exe に渡す引数</param>
		/// <param name="lineProc">１ライン処理デリゲート</param>
		static void SevenZip(string args, Action<string> lineProc) {
			using (var process = new Process()) {
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.RedirectStandardInput = true;
				process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
				process.StartInfo.FileName = SevenZipExe;
				process.StartInfo.Arguments = args;
				process.EnableRaisingEvents = true;
				process.Start();

				string line;
				while ((line = process.StandardOutput.ReadLine()) != null) {
					lineProc(line);
				}

				process.WaitForExit();
			}
		}

		/// <summary>
		/// 7z.exe を指定引数で実行し標準出力ストリームを処理する
		/// </summary>
		/// <param name="args">7z.exe に渡す引数</param>
		/// <param name="outputProc">標準出力ストリームを受け取り処理を行うデリゲート</param>
		static void SevenZip(string args, Action<StreamReader> outputProc) {
			using (var process = new Process()) {
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.RedirectStandardInput = true;
				process.StartInfo.StandardOutputEncoding = null;
				process.StartInfo.FileName = SevenZipExe;
				process.StartInfo.Arguments = args;
				process.EnableRaisingEvents = true;
				process.Start();

				outputProc(process.StandardOutput);

				process.WaitForExit();
			}
		}


		static string NewTmpFileName() {
			Interlocked.Increment(ref _TmpFileCount);
			return Path.Combine(WorkDir, "tmp" + _TmpFileCount);
		}

		static void CreateTempDir() {
			if (!Directory.Exists(WorkDir)) {
				Directory.CreateDirectory(WorkDir);
			}
		}

		static void DeleteTempDir() {
			if (Directory.Exists(WorkDir)) {
				foreach (var f in Directory.GetFiles(WorkDir)) {
					try {
						File.Delete(f);
					} catch {
					}
				}
				Directory.Delete(WorkDir);
			}
		}

		/// <summary>
		/// 指定のMSIファイルからプロダクト情報を取得する
		/// </summary>
		/// <param name="file">MSIファイルパス名</param>
		/// <returns>プロダクト情報</returns>
		static ProductInfo GetProductInfo(string file) {
			IntPtr hMsi;
			uint r;

			r = MsiOpenPackageW(file, out hMsi);
			if (r != 0) {
				return null;
			}
			try {
				return new ProductInfo {
					ProductName = MsiGetProperty(hMsi, "ProductName"),
					ProductCode = MsiGetProperty(hMsi, "ProductCode"),
					ProductVersion = MsiGetProperty(hMsi, "ProductVersion"),
				};
			} finally {
				MsiCloseHandle(hMsi);
			}
		}

		static string MsiGetProperty(IntPtr hInstall, string szName) {
			var sb = new StringBuilder(4096);
			var length = (uint)sb.Capacity;
			var r = MsiGetPropertyW(hInstall, szName, sb, ref length);
			if (r != 0) {
				throw new Win32Exception((int)r);
			}
			return sb.ToString();
		}



		//static void GetProductInfos(SevenZipExtractor extractor, List<ProductInfo> productInfos) {
		//	foreach (var fi in extractor.ArchiveFileData) {
		//		if (string.Compare(Path.GetExtension(fi.FileName), ".msi", true) == 0) {
		//			var tmpFile = Extract(extractor, fi.FileName);
		//			var pi = GetProductInfo(tmpFile);
		//			if (pi != null) {
		//				productInfos.Add(pi);
		//			}
		//			File.Delete(tmpFile);
		//		} else {
		//			if (fi.Size <= 1024 * 1024 * 10) {
		//				using (var ms = new MemoryStream()) {
		//					extractor.ExtractFile(fi.FileName, ms);
		//					ms.Position = 0;
		//					GetProductInfos(ms, productInfos);
		//				}
		//			} else {
		//				var tmpFile = Extract(extractor, fi.FileName);
		//				GetProductInfos(tmpFile, productInfos);
		//				File.Delete(tmpFile);
		//			}
		//		}
		//	}
		//}


		//static string Extract(SevenZipExtractor extractor, string extractFileName) {
		//	var tmpFileName = NewTmpFileName();
		//	using (var fs = new FileStream(tmpFileName, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite)) {
		//		extractor.ExtractFile(extractFileName, fs);
		//	}
		//	return tmpFileName;
		//}

		//static void GetProductInfos(Stream inputStream, List<ProductInfo> productInfos) {
		//	SevenZipExtractor extractor = null;
		//	try {
		//		extractor = new SevenZipExtractor(inputStream);
		//		GetProductInfos(extractor, productInfos);
		//	} catch {
		//	} finally {
		//		extractor?.Dispose();
		//	}
		//}

		//static void GetProductInfos(string file, List<ProductInfo> productInfos) {
		//	SevenZipExtractor extractor = null;
		//	extractor = new SevenZipExtractor(file);
		//	GetProductInfos(extractor, productInfos);
		//	特定のファイルで SevenZipExtractor が例外発生させるため諦め
		//	//try {
		//	//	extractor = new SevenZipExtractor(file);
		//	//	GetProductInfos(extractor, productInfos);
		//	//} catch {
		//	//} finally {
		//	//	extractor?.Dispose();
		//	//}
		//}
	}
}
