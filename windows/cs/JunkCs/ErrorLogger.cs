using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Reflection;
using System.Drawing;

namespace Jk {
	/// <summary>
	/// エラー関係ロガー
	/// </summary>
	public static class ErrorLogger {
		/// <summary>
		/// ログレベル列挙値
		/// </summary>
		public enum Level {
			/// <summary>
			/// 情報レベル
			/// </summary>
			Info,

			/// <summary>
			/// 警告レベル
			/// </summary>
			Warning,

			/// <summary>
			/// エラーレベル
			/// </summary>
			Error,

			/// <summary>
			/// デバッグ用
			/// </summary>
			Debug,
		}

		static object _Sync = new object(); // 同期用オブジェクト
		static Logger _Logger; // ロガー
		static List<string>[] _RingBuffer; // 最新ログ保持用のリングバッファ
		static int _RingBufferPtr; // 次回書き込み先リングバッファインデックス
		static int _RingBufferLen; // リングバッファに入っている有効項目数
		static ImageList _ImageList; // ログリストビューに表示するイメージリスト

		/// <summary>
		/// 静的コンストラクタ
		/// </summary>
		static ErrorLogger() {
			_RingBuffer = new List<string>[100];
			_RingBufferPtr = 0;
		}

		/// <summary>
		/// ログ追加イベントハンドラ
		/// </summary>
		public delegate void AddedEventHandler();

		/// <summary>
		/// ログ追加時に発生するイベント
		/// </summary>
		public static event AddedEventHandler Added;

		/// <summary>
		/// ロガー初期化
		/// </summary>
		/// <param name="logRootDirPath">ログのルートディレクトリパス名</param>
		/// <param name="prefix">ログファイル名の先頭に付く名称</param>
		public static void Initialize(string logRootDirPath) {
			lock (_Sync) {
				_Logger = new Logger(logRootDirPath, "ERR", ".CSV");
				_Logger.OpenLogFile(DateTime.Now);
			}
		}

		/// <summary>
		/// ロガー終了処理
		/// </summary>
		public static void Uninitialize() {
			lock (_Sync) {
				if (_Logger == null)
					return;
				_Logger.Close();
				_Logger = null;
			}
		}

		/// <summary>
		/// ログを出力する
		/// </summary>
		/// <param name="now">現在日時</param>
		/// <param name="level">ログの重大度レベル</param>
		/// <param name="texts">ログ出力内容</param>
		public static void AddLog(DateTime now, Level level, params string[] texts) {
			lock (_Sync) {
				if (_Logger == null)
					return;

				List<string> fields;
				var args = new string[texts.Length + 1];
				args[0] = ((int)level).ToString();
				for (int i = 0; i < texts.Length; i++)
					args[i + 1] = texts[i];

				fields = _Logger.AddLogLine(now, args);

				var capacity = _RingBuffer.Length;
				_RingBuffer[_RingBufferPtr] = fields;
				_RingBufferPtr = (_RingBufferPtr + 1) % capacity;
				_RingBufferLen++;
				if (capacity < _RingBufferLen)
					_RingBufferLen = capacity;
			}

			var d = Added;
			if (d != null)
				d();
		}

		/// <summary>
		/// ログを出力する
		/// </summary>
		/// <param name="level">ログの重大度レベル</param>
		/// <param name="texts">ログ出力内容</param>
		public static void AddLog(Level level, params string[] texts) {
			AddLog(DateTime.Now, level, texts);
		}

		/// <summary>
		/// 最新の規定個数のログを取得する
		/// </summary>
		/// <returns>ログ配列</returns>
		public static List<string>[] GetLatestLogs() {
			lock (_Sync) {
				int n = _RingBufferLen;
				var rb = _RingBuffer;
				var capacity = rb.Length;
				var ptr = (_RingBufferPtr + capacity - n) % capacity;
				var logs = new List<string>[n];

				for (int i = 0; i < n; i++) {
					logs[i] = rb[(i + ptr) % capacity];
				}

				return logs;
			}
		}

		/// <summary>
		/// ログ表示用にリストビューをセットアップする
		/// </summary>
		/// <param name="lv">リストビュー</param>
		/// <param name="logs">表示対象のログリスト</param>
		public static void SetupListView(ListView lv, List<List<string>> logs) {
			if (_ImageList == null) {
				_ImageList = new ImageList();
				_ImageList.ImageSize = new Size(16, 16);
				_ImageList.Images.Add(SystemIcons.Exclamation);
				_ImageList.Images.Add(SystemIcons.Error);
			}

			// ダブルバッファリングを有効にしてちらつきを抑える
			EnableDoubleBuffering(lv);

			// スタイル設定
			lv.View = View.Details;
			lv.BackColor = System.Drawing.Color.Black;
			lv.ForeColor = System.Drawing.Color.Lime;

			// リストビューにイメージリスト設定
			lv.SmallImageList = _ImageList;
			lv.FullRowSelect = true;
			lv.HideSelection = false;

			// リストビューのカラム作成
			int dateTimeWidth;
			using (var g = lv.CreateGraphics()) {
				dateTimeWidth = (int)(g.MeasureString("8888/88/88 88:88:88", lv.Font).Width + 0.5f);
			}
			dateTimeWidth += 32 + 8;

			lv.Columns.Clear();
			lv.Columns.Add("日時", dateTimeWidth);
			lv.Columns.Add("内容", lv.ClientSize.Width - dateTimeWidth);

			// リストビューを仮想モードにして独自に保持しているデータを描画する
			lv.VirtualMode = true;

			// リストビューの仮想モード用データ取得イベント登録
			lv.RetrieveVirtualItem += new RetrieveVirtualItemEventHandler((s, e2) => {
				var log = logs[e2.ItemIndex];
				var lvi = new ListViewItem(0 < log.Count ? log[0] : "");
				var level = 1 < log.Count ? log[1] : "";
				lvi.SubItems.Add(2 < log.Count ? log[2] : "");

				switch (level) {
				case "1":
					lvi.ForeColor = Color.Yellow;
					lvi.ImageIndex = 0;
					break;
				case "2":
					lvi.ForeColor = Color.Red;
					lvi.ImageIndex = 1;
					break;
				case "3":
					lvi.ForeColor = Color.Gray;
					break;
				}

				lvi.Font = lv.Font;

				e2.Item = lvi;
			});

			lv.Tag = logs;
		}

		/// <summary>
		/// ログ表示用リストビューの表示を更新する
		/// </summary>
		/// <param name="lv">リストビュー</param>
		/// <param name="logs">表示ログ</param>
		public static void UpdateListView(ListView lv, List<List<string>> logs) {
			if (logs == null) {
				logs = lv.Tag as List<List<string>>;
				if (logs == null)
					return;
			}
			lv.BeginUpdate();
			lv.VirtualListSize = logs.Count;
			if (logs.Count != 0)
				lv.EnsureVisible(logs.Count - 1);
			lv.EndUpdate();
			lv.Refresh();
		}

		/// <summary>
		/// コントロールのDoubleBufferedプロパティをTrueにする
		/// </summary>
		/// <param name="control">対象のコントロール</param>
		public static void EnableDoubleBuffering(Control control) {
			control.GetType().InvokeMember(
			   "DoubleBuffered",
			   BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
			   null,
			   control,
			   new object[] { true });
		}
	}
}
