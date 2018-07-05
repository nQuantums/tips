using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Forms;

namespace Jk {
	/// <summary>
	/// 例外処理手法をまとめたクラス
	/// </summary>
	public static class ExceptionHandler {
		/// <summary>
		/// 警告メッセージボックスキャプション
		/// </summary>
		public const string WarningCaption = "警告";

		/// <summary>
		/// ユーザーによるUI操作時に発生した例外を処理する、例外内容をメッセージ表示する
		/// </summary>
		/// <param name="owner">オーナーウィンドウ</param>
		/// <param name="ex">例外</param>
		/// <param name="message">エラーへの対処方法などユーザーに促すメッセージ</param>
		/// <returns>例外が処理されたならtrue、それ以外はfalseが返る</returns>
		static public bool HandleUiException(IWin32Window owner, Exception ex, string message) {
			if (ex is System.AccessViolationException)
				return false;

			string msg = message;
			if (msg == null)
				msg = "";
			if (msg.Length != 0)
				msg += "\n";
			if (ex != null)
				msg += ex.Message;

			if (ex is WarningException) {
				// メッセージ表示用例外の場合には詳細情報は必要ないので普通のメッセージボックスを使う
				MessageBox.Show(owner, msg, WarningCaption, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			} else {
				// 例外のスタックとレースなどの詳細を表示する
				ExceptionMessageForm.Show(owner, ex, message);
			}

			return true;
		}
	}
}
