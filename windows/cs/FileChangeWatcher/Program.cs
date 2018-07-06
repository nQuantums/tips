using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace FileChangeWatcher {
	class Program {
		static Dictionary<string, LogFileReader> LogFiles = new Dictionary<string, LogFileReader>();
		static FileSystemWatcher Watcher;
		static volatile bool Queuing;
		static object Sync = new object();

		static void Main(string[] args) {
			Watcher = new FileSystemWatcher();
			//監視するディレクトリを指定
			Watcher.Path = Directory.GetCurrentDirectory();
			//最終アクセス日時、最終更新日時、ファイル、フォルダ名の変更を監視する
			Watcher.NotifyFilter =
				(NotifyFilters.LastAccess
				| NotifyFilters.LastWrite
				| NotifyFilters.FileName
				| NotifyFilters.DirectoryName);
			//すべてのファイルを監視
			Watcher.Filter = "*.log";

			//イベントハンドラの追加
			Watcher.Changed += (s, e) => {
				if (TryEnter()) {
					Task.Run(() => {
						// FileSystemEventArgs は１回の変更で複数回イベントが発生するので、一定時間待って吸収する
						Thread.Sleep(100);

						// 変更ファイルに対応する LogFile オブジェクト取得
						var fileName = e.Name;
						LogFileReader lf;
						lock (LogFiles) {
							if (!LogFiles.TryGetValue(fileName, out lf)) {
								LogFiles[fileName] = lf = new LogFileReader(e.FullPath);
							}
						}

						// 変更を読み込む
						var text = lf.ReadDelta();
						Console.WriteLine(fileName);
						Console.WriteLine(text);

						Leave();
					});
				}
			};

			//監視を開始する
			Watcher.EnableRaisingEvents = true;
			Console.WriteLine("カレントディレクトリの監視を開始しました。");

			Console.ReadKey();

			//監視を終了
			Watcher.EnableRaisingEvents = false;
			Watcher.Dispose();
			Watcher = null;
		}

		static bool TryEnter() {
			lock (Sync) {
				if (Queuing) {
					return false;
				} else {
					Queuing = true;
					return true;
				}
			}
		}

		static void Leave() {
			lock (Sync) {
				Queuing = false;
			}
		}
	}
}
