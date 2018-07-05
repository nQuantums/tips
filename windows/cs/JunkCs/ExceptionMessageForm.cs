using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Jk {
	/// <summary>
	/// 例外メッセージ表示フォーム
	/// 例外の詳細内容表示も可能。
	/// </summary>
	public partial class ExceptionMessageForm : Form {
		new const int Padding = 8;

		string m_MainMessage;
		Exception m_Exception;
		Icon m_Icon;
		Size m_InitialSize;

		public ExceptionMessageForm() {
			InitializeComponent();
			this.TopMost = true;
		}

		protected override void OnLoad(EventArgs ev) {
			base.OnLoad(ev);

			Exception e = m_Exception;

			// キャプション生成
			this.Text = "エラー";

			// アイコンセット
			if (m_Icon == null)
				m_Icon = SystemIcons.Exclamation;
			this.Icon = m_Icon;

			// 表示メッセージ取得
			this.lblMessage.Text = m_MainMessage != null ? m_MainMessage : e.Message;

			Rectangle picRect = new Rectangle(Padding, Padding, m_Icon.Size.Width, m_Icon.Size.Height);
			this.lblMessage.Left = picRect.Right + Padding;
			this.lblMessage.Top = picRect.Top + Padding;

			Rectangle msgRect = this.lblMessage.Bounds;
			Rectangle btnRect = this.btnOK.Bounds;
			Size panelSize = this.pnlMain.ClientSize;
			int right = Math.Max(msgRect.Right, this.ClientSize.Width);
			int bottom = Math.Max(msgRect.Bottom, picRect.Bottom);
			Size size = new Size(right + Padding, bottom + Padding + (panelSize.Height - btnRect.Top));

			this.pnlMain.ClientSize = size;
			m_InitialSize = SizeFromClientSize(size);
			this.Size = this.MinimumSize = this.MaximumSize = m_InitialSize;

			this.MinimumSize = m_InitialSize;

			this.pnlMain.Paint += new PaintEventHandler(pnlMain_Paint);

			// 内部例外のメッセージとスタックとレースを取得する
			StringBuilder sb = new StringBuilder();

			sb.AppendLine("-------- メッセージ --------");
			e = m_Exception;
			for (int i = 0; e != null; i++) {
				if (i != 0)
					sb.AppendLine();
				sb.AppendLine(e.Message);
				e = e.InnerException;
			}

			//	内部例外も含めて全てのスタックトレースを追加する
			sb.AppendLine("-------- スタックトレース --------");
			e = m_Exception;
			for (int i = 0; e != null; i++) {
				if (i != 0)
					sb.AppendLine("--------");
				sb.AppendLine(e.StackTrace);
				e = e.InnerException;
			}

			this.tbDetail.Text = sb.ToString();
		}

		void pnlMain_Paint(object sender, PaintEventArgs e) {
			e.Graphics.DrawIcon(m_Icon, Padding, Padding);
		}

		/// <summary>
		/// 例外を表示する
		/// </summary>
		/// <param name="owner">親ウィンドウ</param>
		/// <param name="ex">例外</param>
		static public void Show(IWin32Window owner, Exception ex) {
			using (var f = new ExceptionMessageForm()) {
				f.m_Exception = ex;
				f.ShowDialog(owner);
			}
		}

		/// <summary>
		/// 例外を表示する
		/// </summary>
		/// <param name="owner">親ウィンドウ</param>
		/// <param name="ex">例外</param>
		/// <param name="message">メインメッセージ</param>
		static public void Show(IWin32Window owner, Exception ex, string message) {
			using (var f = new ExceptionMessageForm()) {
				f.m_Exception = ex;
				f.m_MainMessage = message;
				f.ShowDialog(owner);
			}
		}

		/// <summary>
		/// 例外を表示する
		/// </summary>
		/// <param name="owner">親ウィンドウ</param>
		/// <param name="ex">例外</param>
		/// <param name="message">メインメッセージ</param>
		/// <param name="icon">アイコン</param>
		static public void Show(IWin32Window owner, Exception ex, string message, MessageBoxIcon icon) {
			using (var f = new ExceptionMessageForm()) {
				f.m_Exception = ex;
				f.m_MainMessage = message;
				f.m_Icon = GetIcon(icon);
				f.ShowDialog(owner);
			}
		}

		/// <summary>
		/// OKボタン
		/// </summary>
		private void btnOK_Click(object sender, EventArgs e) {
			this.DialogResult = DialogResult.OK;
		}

		/// <summary>
		/// エラー内容詳細表示
		/// </summary>
		private void chkViewDetail_CheckedChanged(object sender, EventArgs e) {
			this.tbDetail.Visible = this.chkViewDetail.Checked;
			if (this.chkViewDetail.Checked) {
				this.MaximumSize = Size.Empty;
				this.Size = new Size(m_InitialSize.Width, m_InitialSize.Height + Padding * 2 + 100);
			} else {
				this.Size = this.MaximumSize = m_InitialSize;
			}
		}

		/// <summary>
		/// メッセージボックスアイコン値から実際のアイコンを取得する
		/// </summary>
		static Icon GetIcon(MessageBoxIcon icon) {
			switch (icon) {
			case MessageBoxIcon.Exclamation:
				return SystemIcons.Exclamation;
			case MessageBoxIcon.Error:
				return SystemIcons.Error;
			case MessageBoxIcon.Information:
				return SystemIcons.Information;
			default:
				return null;
			}
		}
	}
}
