using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.IO;

namespace Jk {
	/// <summary>
	/// 排他ファイルロック可能になるまで待つ際に表示されるフォーム
	/// </summary>
	public partial class FileCriticalSectionForm : Form {
		Thread _SpinThread;
		volatile bool _RequestCancel;

		public FileCriticalSection FileCriticalSection {
			get;
			private set;
		}

		public FileCriticalSectionForm() {
			InitializeComponent();
		}

		protected override void OnLoad(EventArgs e) {
			base.OnLoad(e);

			this.lblMessage.Text = string.Format("排他ファイル \"{0}\" が使用可能になるのを待っています。", this.FileCriticalSection.FilePath);
			_SpinThread = new Thread(SpinThreadProc);
			_SpinThread.Start();
		}

		protected override void OnClosing(CancelEventArgs e) {
			if(_SpinThread != null)
				_SpinThread.Join();
			base.OnClosing(e);
		}

		/// <summary>
		/// 排他ファイルロック試行をループするスレッド
		/// </summary>
		void SpinThreadProc() {
			var fcs = this.FileCriticalSection;
			while (!_RequestCancel) {
				try {
					fcs.Lock();
					Thread.Sleep(100);
				} catch (IOException) {
					continue;
				}
				break;
			}
			if (_RequestCancel) {
				this.BeginInvoke(new Action(() => {
					this.DialogResult = DialogResult.Cancel;
				}));
			} else {
				this.BeginInvoke(new Action(() => {
					this.DialogResult = DialogResult.OK;
				}));
			}
		}

		/// <summary>
		/// 排他処理可能になるまでメッセージ表示しながら待つ
		/// </summary>
		/// <param name="owner">所有者</param>
		/// <param name="fileCriticalSection">排他ファイルクリティカルセクション</param>
		/// <returns>ダイアログ結果</returns>
		public static DialogResult ShowDialog(IWin32Window owner, FileCriticalSection fileCriticalSection) {
			using (var f = new FileCriticalSectionForm()) {
				f.FileCriticalSection = fileCriticalSection;
				return f.ShowDialog(owner);
			}
		}

		/// <summary>
		/// キャンセルボタン
		/// </summary>
		private void btnCancel_Click(object sender, EventArgs e) {
			_RequestCancel = true;
		}
	}
}
