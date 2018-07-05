using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Junk {
	/// <summary>
	/// エラーログ用リストビュー、エラーログが追加されると自動的に描画更新される
	/// </summary>
	public class ErrorLogListView : ListView {
		List<List<string>> _ErrorLogs = new List<List<string>>(); // エラーログ一覧

		public ErrorLogListView() {
		}

		/// <summary>
		/// ハンドル作成後イベント処理、初期化を行う
		/// </summary>
		protected override void OnHandleCreated(EventArgs e) {
			base.OnHandleCreated(e);

			// エラーログ追加後イベントハンドラ登録
			ErrorLogger.Added += ErrorLogger_Added;
		}

		protected override void InitLayout() {
			base.InitLayout();

			// リストビューをログ表示用にセットアップする
			if (!this.DesignMode)
				ErrorLogger.SetupListView(this, _ErrorLogs);
		}

		/// <summary>
		/// ハンドル破棄後イベント処理、終了処理を行う
		/// </summary>
		protected override void OnHandleDestroyed(EventArgs e) {
			ErrorLogger.Added -= ErrorLogger_Added;
			base.OnHandleDestroyed(e);
		}

		/// <summary>
		/// エラーログ追加後イベントを処理
		/// </summary>
		void ErrorLogger_Added() {
			if (!this.IsHandleCreated)
				return;
			if (this.InvokeRequired) {
				this.BeginInvoke(new Action(() => {
					UpdateLogList();
				}));
			} else {
				UpdateLogList();
			}
		}

		/// <summary>
		/// エラーログリスト更新
		/// </summary>
		public void UpdateLogList() {
			_ErrorLogs.Clear();
			_ErrorLogs.AddRange(ErrorLogger.GetLatestLogs());
			ErrorLogger.UpdateListView(this, _ErrorLogs);
		}
	}
}
