using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LensShiftDetector {
	/// <summary>
	/// ウィンドウ処理ヘルパクラス
	/// </summary>
	public class WndUtil {
		/// <summary>
		/// 右上の閉じるボタンが無効化されたフォーム
		/// </summary>
		public class CloseButtonDisabledForm : Form {
			/// <summary>
			/// オーバーライドして閉じるボタンを無効化する
			/// </summary>
			protected override CreateParams CreateParams {
				[System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags = System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
				get {
					const int CS_NOCLOSE = 0x200;
					CreateParams cp = base.CreateParams;
					cp.ClassStyle = cp.ClassStyle | CS_NOCLOSE;
					return cp;
				}
			}
		}
	}
}
