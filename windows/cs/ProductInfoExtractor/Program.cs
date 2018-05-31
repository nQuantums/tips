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
using SevenZipExtractor;

namespace ProductInfoExtractor {
	class Program {
		#region PInvoke
		[DllImport("msi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		static extern uint MsiOpenDatabaseW(string szDatabasePath, IntPtr phPersist, out IntPtr phDatabase);
		[DllImport("msi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		static extern uint MsiDatabaseOpenViewW(IntPtr hDatabase, string szQuery, out IntPtr phView);
		[DllImport("msi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		static extern uint MsiViewExecute(IntPtr hView, IntPtr hRecord);
		[DllImport("msi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		static extern uint MsiViewFetch(IntPtr hView, out IntPtr phRecord);
		[DllImport("msi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		static extern uint MsiRecordGetStringW(IntPtr hRecord, uint iField, StringBuilder szValueBuf, ref uint pcchValueBuf);

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
		static Semaphore Sem = new Semaphore(32, 32);

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

			WorkDir = Path.Combine(Directory.GetCurrentDirectory(), "ProductInfoExtractorTmp");
			DeleteTempDir();
			CreateTempDir();

			var productInfos = new List<ProductInfo>();
			ExtractAndGetProductInfos(args[0], productInfos);
			//ExtractAndGetProductInfosAsync(args[0], productInfos);
			//GetProductInfos(args[0], productInfos);

			foreach (var pi in productInfos) {
				Console.WriteLine($"----------------");
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
			var pi = GetProductInfoFromMsiDb(archiveFile);
			if (pi != null) {
				lock (productInfos) {
					productInfos.Add(pi);
				}
			} else {
				var tasks = new List<Task>();
				Parallel.ForEach(GetFilesInArchive(archiveFile), f => {
					Sem.WaitOne();
					try {
						var tmpFile = ExtractFile(archiveFile, f.FileName);
						ExtractAndGetProductInfosAsync(tmpFile, productInfos);
						File.Delete(tmpFile);
					} finally {
						Sem.Release();
					}
				});
			}
		}

		/// <summary>
		/// 指定アーカイブファイルを展開しつつMSIのプロダクト情報を抽出する
		/// </summary>
		/// <param name="archiveFile">アーカイブファイルパス名</param>
		/// <param name="productInfos">プロダクト情報がここに追加されていく</param>
		static void ExtractAndGetProductInfos(string archiveFile, List<ProductInfo> productInfos) {
			var pi = GetProductInfoFromMsiDb(archiveFile);
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
				var buffer = new byte[1024 * 1024 * 2];
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
							if (int.TryParse(sizeStr, out int size)) {
								if (1024 * 1024 * 1 <= size) {
									result.Add(new FileInArchiveInfo {
										FileName = fileName,
										Size = size
									});
								}
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
			var i = Interlocked.Increment(ref _TmpFileCount);
			return Path.Combine(WorkDir, "tmp" + i);
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

		static object MsiDbSync = new object();

		/// <summary>
		/// MSIデータベース形式のファイルからプロダクト情報を取得する
		/// </summary>
		/// <param name="fileName">MSIデータベースファイル名</param>
		/// <returns>プロダクト情報または null</returns>
		static ProductInfo GetProductInfoFromMsiDb(string fileName) {
			IntPtr hMsiDb = IntPtr.Zero, hMsiView = IntPtr.Zero, hMsiRecord = IntPtr.Zero;
			lock (MsiDbSync) {
				try {
					var r = MsiOpenDatabaseW(fileName, new IntPtr(MSIDBOPEN_READONLY), out hMsiDb);
					if (r != 0) {
						return null;
					}
					r = MsiDatabaseOpenViewW(hMsiDb, "SELECT Property, Value FROM Property WHERE Property='ProductName' OR Property='ProductCode' OR Property='ProductVersion'", out hMsiView);
					if (r != 0) {
						return null;
					}
					r = MsiViewExecute(hMsiView, IntPtr.Zero);
					if (r != 0) {
						return null;
					}

					var sb = new StringBuilder(512);
					uint len;
					var pi = new ProductInfo();
					while (MsiViewFetch(hMsiView, out hMsiRecord) == 0) {
						sb.Length = 0;
						len = (uint)sb.Capacity;
						r = MsiRecordGetStringW(hMsiRecord, 1, sb, ref len);
						if (r != 0) {
							return null;
						}
						var property = sb.ToString();

						sb.Length = 0;
						len = (uint)sb.Capacity;
						r = MsiRecordGetStringW(hMsiRecord, 2, sb, ref len);
						if (r != 0) {
							return null;
						}
						var value = sb.ToString();

						switch (property) {
						case "ProductName":
							pi.ProductName = value;
							break;
						case "ProductCode":
							pi.ProductCode = value;
							break;
						case "ProductVersion":
							pi.ProductVersion = value;
							break;
						}

						MsiCloseHandle(hMsiRecord);
						hMsiRecord = IntPtr.Zero;
					}

					return pi;
				} finally {
					if (hMsiRecord != IntPtr.Zero) {
						MsiCloseHandle(hMsiRecord);
					}
					if (hMsiView != IntPtr.Zero) {
						MsiCloseHandle(hMsiView);
					}
					if (hMsiDb != IntPtr.Zero) {
						MsiCloseHandle(hMsiDb);
					}
				}
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

		#region 特定のファイルでメモリアクセス違反発生
		static void GetProductInfos(string file, List<ProductInfo> productInfos) {
			ArchiveFile extractor = null;
			try {
				extractor = new ArchiveFile(file);
				GetProductInfos(extractor, productInfos);
			} catch(SevenZipException) {
			} finally {
				extractor?.Dispose();
			}
		}

		static void GetProductInfos(Stream inputStream, List<ProductInfo> productInfos) {
			ArchiveFile extractor = null;
			try {
				extractor = new ArchiveFile(inputStream);
				GetProductInfos(extractor, productInfos);
			} catch (SevenZipException) {
			} finally {
				extractor?.Dispose();
			}
		}

		static void GetProductInfos(ArchiveFile extractor, List<ProductInfo> productInfos) {
			foreach (var entry in extractor.Entries) {
				if (1024 * 1024 * 1 <= entry.Size) {
					var tmpFile = Extract(entry);
					var pi = GetProductInfo(tmpFile);
					if (pi != null) {
						lock (productInfos) {
							productInfos.Add(pi);
						}
					} else {
						GetProductInfos(tmpFile, productInfos);
					}
					File.Delete(tmpFile);
				}
			}
		}

		static string Extract(Entry entry) {
			var tmpFileName = NewTmpFileName();
			using (var fs = new FileStream(tmpFileName, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite)) {
				entry.Extract(fs);
			}
			return tmpFileName;
		}
		#endregion
	}
}
