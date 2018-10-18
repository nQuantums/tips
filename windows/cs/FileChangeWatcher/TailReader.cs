using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileChangeWatcher {
	/// <summary>
	/// ファイル変更時の終端部分読み込み処理を行う
	/// </summary>
	public class TailReader : IDisposable {
		/// <summary>
		/// ファイル変更時のハンドラ
		/// </summary>
		public class FileChangeHandler {
			volatile bool _Queuing;

			/// <summary>
			/// ファイル名
			/// </summary>
			public string FileName;

			/// <summary>
			/// ファイル変更時の処理
			/// </summary>
			public Action<FileDeltaReader> Handler;

			/// <summary>
			/// ファイル読み込み
			/// </summary>
			public FileDeltaReader Reader;

			/// <summary>
			/// コンストラクタ、ファイル名と処理を指定して初期化する
			/// </summary>
			/// <param name="fileName">ファイル名</param>
			/// <param name="handler">ファイル変更時の処理</param>
			/// <param name="reader">ファイル読み込みオブジェクト</param>
			public FileChangeHandler(string fileName, Action<FileDeltaReader> handler, FileDeltaReader reader) {
				this.FileName = fileName;
				this.Handler = handler;
				this.Reader = reader;
			}

			public bool TryEnter() {
				lock (this) {
					if (_Queuing) {
						return false;
					} else {
						_Queuing = true;
						return true;
					}
				}
			}

			public void Leave() {
				lock (this) {
					_Queuing = false;
				}
			}
		}

		FileSystemWatcher _Watcher;

		/// <summary>
		/// ファイル変更検知のルートディレクトリ名
		/// </summary>
		public string Dir { get; private set; }

		/// <summary>
		/// 検知ファイルフィルタ
		/// </summary>
		public string Filter { get; private set; }

		/// <summary>
		/// ファイル名をキーとするハンドラ列
		/// </summary>
		public Dictionary<string, FileChangeHandler> Handlers { get; private set; }

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="dir">ファイル変更検知のルートディレクトリ名</param>
		/// <param name="filter">検知ファイルフィルタ</param>
		/// <param name="handlers">ファイル名と変更時の処理のペア列</param>
		public TailReader(string dir, string filter, Dictionary<string, Action<FileDeltaReader>> handlers) {
			this.Dir = dir;
			this.Filter = filter;
			this.Handlers = handlers.ToDictionary(
				kvp => kvp.Key.ToLower(),
				kvp => new FileChangeHandler(kvp.Key.ToLower(), kvp.Value, new FileDeltaReader(Path.Combine(dir, kvp.Key)))
			);

			// 監視を開始する
			_Watcher = new FileSystemWatcher();
			_Watcher.Path = dir;
			_Watcher.Filter = filter;

			FileSystemEventHandler fsehandler = (s, e) => {
				// 変更ファイルに対応する LogFile オブジェクト取得
				FileChangeHandler fch;
				if (Handlers.TryGetValue(e.Name.ToLower(), out fch)) {
					if (fch.TryEnter()) {
						Task.Run(() => {
							// １回の変更で複数回イベントが発生するため、ファイルに対するイベントは一定時間待って吸収する
							Thread.Sleep(100);

							// 変更ファイルを基に処理を行う
							try {
								if (fch.Reader == null) {
									fch.Reader = new FileDeltaReader(e.FullPath);
								}
								fch.Handler(fch.Reader);
							} finally {
								fch.Leave();
							}
						});
					}
				}
			};
			_Watcher.Created += fsehandler;
			_Watcher.Changed += fsehandler;
			_Watcher.EnableRaisingEvents = true;
		}

		#region IDisposable Support
		private bool disposedValue = false; // 重複する呼び出しを検出するには

		protected virtual void Dispose(bool disposing) {
			if (!disposedValue) {
				if (_Watcher != null) {
					_Watcher.EnableRaisingEvents = false;
					_Watcher.Dispose();
					_Watcher = null;
				}
				disposedValue = true;
			}
		}

		~TailReader() {
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
