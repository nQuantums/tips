using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace HvnDbBeforeCpeCheck {
	public class Logger : IDisposable {
		public const int KindCount = 5;

		public enum Kind {
			Error,
			Warning,
			Modify,
			Sql,
		}

		public enum FileType {
			Markdown,
			Csv,
		}




		static Encoding _Encoding = new UTF8Encoding(false);


		static Dictionary<string, Logger> _Tasks;
		public static string[] _KindwiseDir;
		public static int[] _KindwiseCount;
		public static OutputFile _SummaryFile;

		public static string RootLogDir { get; private set; }

		public static void Begin(string rootLogDir) {
			RootLogDir = rootLogDir;
			if (Directory.Exists(rootLogDir)) {
				throw new ApplicationException("既に " + rootLogDir + " ディレクトリが存在しています。");
			}
			Directory.CreateDirectory(rootLogDir);

			_Tasks = new Dictionary<string, Logger>();
			_KindwiseDir = new string[KindCount];
			for (int i = 0; i < KindCount; i++) {
				var kind = (Kind)i;
				_KindwiseDir[i] = Path.Combine(rootLogDir, kind.ToString());
			}
			_KindwiseCount = new int[KindCount];
			_SummaryFile = new OutputFile(Path.Combine(rootLogDir, "Summary"), FileType.Markdown);
		}

		public static void Begin() {
			var now = DateTime.Now;
			var dir = Directory.GetCurrentDirectory();
			Begin(Path.Combine(dir, now.ToString("yyyyMMddHHmmss")));
		}

		public static void End() {
			if (_Tasks != null) {
				foreach (var task in _Tasks.Values) {
					task.Dispose();
				}
				_Tasks = null;
			}
			if (_SummaryFile != null) {
				var sb = new StringBuilder();
				sb.AppendLine("# 全体概要");
				sb.AppendLine("|ログ種類|ログ件数|詳細|");
				sb.AppendLine("|-|-|-|");
				for (int i = 0; i < KindCount; i++) {
					var kind = (Kind)i;
					var count = GetCount(kind);
					sb.Append("|");
					sb.Append(GetLogKindFullName(kind));
					sb.Append("|");
					sb.Append(GetCount(kind).ToString());
					sb.Append("|");
					sb.AppendLine("|");
				}
				_SummaryFile.WriteText(sb.ToString());
				_SummaryFile.Dispose();
				_SummaryFile = null;
			}
		}

		public static string GetLogDir(Kind kind) {
			return _KindwiseDir[(int)kind];
		}

		public static void IncrementCount(Kind kind) {
			Interlocked.Increment(ref _KindwiseCount[(int)kind]);
		}

		public static int GetCount(Kind kind) {
			return _KindwiseCount[(int)kind];
		}

		public static string GetLogKindFullName(Kind kind) {
			switch (kind) {
			case Kind.Error:
				return "エラー";
			case Kind.Warning:
				return "警告";
			case Kind.Modify:
				return "変更";
			case Kind.Sql:
				return "実施したSQL";
			default:
				return "";
			}
		}

		public static FileType GetFileType(Kind kind) {
			switch (kind) {
			case Kind.Error:
				return FileType.Markdown;
			case Kind.Warning:
				return FileType.Markdown;
			case Kind.Modify:
				return FileType.Markdown;
			case Kind.Sql:
				return FileType.Markdown;
			default:
				return FileType.Markdown;
			}
		}

		public static string GetFileTypeExt(FileType type) {
			switch (type) {
			case FileType.Markdown:
				return ".md";
			case FileType.Csv:
				return ".csv";
			default:
				return "";
			}
		}

		public static Logger LoggerForTask(string taskName, string description) {
			lock (_Tasks) {
				Logger logger;
				if (!_Tasks.TryGetValue(taskName, out logger)) {
					_Tasks[taskName] = logger = new Logger(taskName, description);
				}
				return logger;
			}
		}

		public static void HandleException(System.Exception ex) {
			System.Exception exception = ex;
			var depth = 1;
			var sb = new StringBuilder();
			do {
				if (!string.IsNullOrEmpty(exception.Message)) {
					sb.AppendLine(new string('#', depth) + " " + exception.Message);
					sb.AppendLine(exception.StackTrace.Replace("\r\n", "  \r\n").Replace("\n", "  \n"));
					sb.AppendLine();
					depth++;
				}
				exception = exception.InnerException;
			} while (exception != null);

			var lex = ex as Logger.Exception;
			if (lex != null) {
				lex.Logger.Log(Logger.Kind.Error, sb.ToString());
			} else {
				var logger = Logger.LoggerForTask("NonTask", "タスク外の処理");
				logger.Log(Logger.Kind.Error, sb.ToString());
			}
		}

		public string TaskName { get; private set; }
		public string Description { get; private set; }
		OutputFile[] _OutputFiles;

		public Logger(string taskName, string description) {
			this.TaskName = taskName;
			this.Description = description;
			_OutputFiles = new OutputFile[KindCount];
			for(int i = 0; i < KindCount; i++) {
				var kind = (Kind)i;
				var sb = new StringBuilder();
				sb.Append("# 「");
				sb.Append(description);
				sb.Append("」タスクの");
				sb.AppendLine(GetLogKindFullName(kind));
				sb.AppendLine();
				_OutputFiles[i] = new OutputFile(Path.Combine(GetLogDir(kind), taskName), GetFileType(kind), sb.ToString());
			}
		}

		public void WriteText(Kind kind, string text) {
			_OutputFiles[(int)kind].WriteText(text);
		}

		public void Log(Kind kind, string text) {
			WriteText(kind, text);
			IncrementCount(kind);
		}

		public class Exception : ApplicationException {
			public Logger Logger { get; private set; }

			public Exception(Logger logger, string message) : base(message) {
				this.Logger = logger;
			}

			public Exception(Logger logger, System.Exception innerException) : base("", innerException) {
				this.Logger = logger;
			}
		}

		public class OutputFile : IDisposable {
			public string FileName { get; private set; }
			public FileType FileType { get; private set; }
			public string Header { get; private set; }
			FileStream _FileStream;
			StreamWriter _StreamWriter;

			public OutputFile(string fileName, FileType type, string header = null) {
				this.FileName = fileName + GetFileTypeExt(type);
				this.FileType = type;
				this.Header = header;
			}

			public void WriteText(string text) {
				lock (this) {
					if(_FileStream == null) {
						var dir = Path.GetDirectoryName(this.FileName);
						if (!Directory.Exists(dir)) {
							Directory.CreateDirectory(dir);
						}

						_FileStream = new FileStream(this.FileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
						_StreamWriter = new StreamWriter(_FileStream, _Encoding);
						if (this.Header != null) {
							_StreamWriter.Write(this.Header);
						}
					}
					_StreamWriter.Write(text);
					_StreamWriter.Flush();
				}
			}

			#region IDisposable Support
			private bool disposedValue = false; // 重複する呼び出しを検出するには

			protected virtual void Dispose(bool disposing) {
				if (!disposedValue) {
					if (_StreamWriter != null) {
						_StreamWriter.Dispose();
						_StreamWriter = null;
					}
					if (_FileStream != null) {
						_FileStream.Dispose();
						_FileStream = null;
					}
					disposedValue = true;
				}
			}

			~OutputFile() {
				// このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
				Dispose(false);
			}

			// このコードは、破棄可能なパターンを正しく実装できるように追加されました。
			public void Dispose() {
				// このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
				Dispose(true);
				GC.SuppressFinalize(this);
			}
			#endregion
		}

		#region IDisposable Support
		private bool disposedValue = false; // 重複する呼び出しを検出するには

		protected virtual void Dispose(bool disposing) {
			if (!disposedValue) {
				if (_OutputFiles != null) {
					for (int i = 0; i < _OutputFiles.Length; i++) {
						var of = _OutputFiles[i];
						if (of != null) {
							of.Dispose();
						}
					}
					_OutputFiles = null;
				}
				disposedValue = true;
			}
		}

		~Logger() {
			// このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
			Dispose(false);
		}

		// このコードは、破棄可能なパターンを正しく実装できるように追加されました。
		public void Dispose() {
			// このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}
