using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;

namespace FileChangeWatcher {
	class Program {
		[DllImport("kernel32.dll",
			EntryPoint = "GetStdHandle",
			SetLastError = true,
			CharSet = CharSet.Auto,
			CallingConvention = CallingConvention.StdCall)]
		private static extern IntPtr GetStdHandle(int nStdHandle);
		[DllImport("kernel32.dll",
			EntryPoint = "AllocConsole",
			SetLastError = true,
			CharSet = CharSet.Auto,
			CallingConvention = CallingConvention.StdCall)]
		private static extern int AllocConsole();
		private const int STD_INPUT_HANDLE = -10;
		private const int STD_OUTPUT_HANDLE = -11;

		static int Main(string[] args) {
			AllocConsole();
			var encoding = Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.ANSICodePage);
			var standardInput = new StreamReader(new FileStream(new SafeFileHandle(GetStdHandle(STD_INPUT_HANDLE), true), FileAccess.Read), encoding);
			var standardOutput = new StreamWriter(new FileStream(new SafeFileHandle(GetStdHandle(STD_OUTPUT_HANDLE), true), FileAccess.Write), encoding);
			standardOutput.AutoFlush = true;
			Console.SetIn(standardInput);
			Console.SetOut(standardOutput);

			if (args.Length < 1) {
				Console.WriteLine("指定ディレクトリ内の指定ファイルをログ表示します。");
				Console.WriteLine();
				Console.WriteLine("    usage: FileChangeWatcher.exe <mode>");
				Console.WriteLine();
				Console.WriteLine("FileChangeWatcher.exe watch <監視対象ディレクトリパス名> <フィルタ> [<ファイル名>]");
				Console.WriteLine("FileChangeWatcher.exe view [<キャプション>]");
				Console.ReadKey();
				return 0;
			}

			switch (args[0]) {
			case "watch":
				return Watch(args);

			case "view":
				return View(args);

			default:
				throw new ApplicationException("不明なモード " + args[0]);
			}
		}

		static int Watch(string[] args) {
			var encoding = Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.ANSICodePage);
			var standardInput = new StreamReader(new FileStream(new SafeFileHandle(GetStdHandle(STD_INPUT_HANDLE), true), FileAccess.Read), encoding);
			Console.SetIn(standardInput);

			if (args.Length < 3) {
				Console.WriteLine("監視モード。");
				Console.WriteLine();
				Console.WriteLine("    usage: FileChangeWatcher.exe watch <監視対象ディレクトリパス名> <フィルタ> [<ファイル名>]");
				Console.ReadKey();
				return 0;
			}

			Console.WriteLine("exit と入力すると終了します。");

			var handlers = new Dictionary<string, Action<FileDeltaReader>>();
			for (int i = 3; i < args.Length; i++) {
				var fileName = args[i];
				Process p = null;
				Action startProcess = () => {
					p = new Process();
					p.StartInfo.CreateNoWindow = false;
					p.StartInfo.UseShellExecute = false;
					p.StartInfo.RedirectStandardInput = true;
					p.StartInfo.FileName = "FileChangeWatcher.exe";
					p.StartInfo.Arguments = "view " + fileName;
					p.Start();
				};
				Action<FileDeltaReader> handler = r => {
					var str = r.ReadDeltaString();
					if (p != null && p.HasExited) {
						p.Dispose();
						p = null;
					}
					if (p == null) {
						startProcess();
					}
					foreach (var line in str.Split('\n')) {
						var text = line.Replace("\r", "");
						p.StandardInput.WriteLine(text);
					}
				};
				handlers.Add(fileName, handler);
			}

			var fcw = new TailReader(args[1], args[2], handlers);

			for (; ; ) {
				var line = Console.ReadLine();
				if(line == "exit") {
					break;
				}
			}

			return 0;
		}

		static int View(string[] args) {
			if (2 <= args.Length) {
				Console.Title = args[1];
			}

			ConsoleStyle cs;
			try {
				cs = ConsoleStyle.FromJsonFile("consoleStyle.json");
				cs.Apply();
			} catch (FileNotFoundException) {
				cs = new ConsoleStyle();
			}
			if (cs.highlights == null) {
				cs.highlights = new ConsoleStyle.Highlight[0];
			}

			var highlights = cs.highlights;
			Regex rx;

			if (highlights.Length != 0) {
				var sb = new StringBuilder();
				foreach (var h in cs.highlights) {
					if (sb.Length != 0) {
						sb.Append("|");
					}
					sb.AppendFormat("(?<{0}>{1})", h.name, h.pattern);
				}
				rx = new Regex(sb.ToString(), RegexOptions.IgnoreCase | RegexOptions.Compiled);
			} else {
				rx = null;
			}

			var colors = new ConsoleColor[0];
			for (; ; ) {
				var line = Console.ReadLine();
				if (line == null) {
					break;
				}
				if (line.Length == 0) {
					continue;
				}
				line = line.Replace("\t", "    ");

				if (colors.Length < line.Length) {
					colors = new ConsoleColor[line.Length];
				}
				for (int i = 0; i < line.Length; i++) {
					colors[i] = ConsoleColor.White;
				}

				if (rx != null) {
					rx.Replace(line, match => {
						var groups = match.Groups;
						for (int i = 0; i < highlights.Length; i++) {
							var h = highlights[i];
							var g = groups[h.name];
							if (g.Success) {
								var s = g.Index;
								var c = h.Color;
								for (int j = 0, m = g.Length; j < m; j++) {
									colors[s + j] = c;
								}
								break;
							}
						}
						return "";
					});
				}

				int start = 0;
				int lastColor = (int)colors[0];
				Console.ForegroundColor = (ConsoleColor)lastColor;
				for (int i = 0; i < line.Length; i++) {
					var c = (int)colors[i];
					if (c != lastColor) {
						Console.ForegroundColor = (ConsoleColor)lastColor;
						Console.Write(line.Substring(start, i - start));
						start = i;
					}
					lastColor = c;
				}
				Console.ForegroundColor = (ConsoleColor)lastColor;
				Console.WriteLine(line.Substring(start, line.Length - start));
			}

			return 0;
		}
	}
}
