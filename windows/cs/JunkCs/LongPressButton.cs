using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Jk {
	/// <summary>
	/// 長押し用ボタン
	/// </summary>
	public class LongPressButton : Button {
		#region PInvoke
		[DllImport("winmm.dll", EntryPoint = "timeGetTime")]
		public static extern uint MM_GetTime();
		#endregion

		uint _StartTime; // 押下開始時のシステム時間(ms)
		bool _IsPressed; // ボタンが押されているかどうか、制御処理のループの中で参照する
		bool _Captured; // ボタン押されてマウスイベントキャプチャ状態かどうか

		/// <summary>
		/// 押下状態変更イベント引数クラス
		/// </summary>
		public class PressingChangedEventArgs : EventArgs {
			/// <summary>
			/// 新しい押下状態
			/// </summary>
			public readonly bool IsPressed;

			/// <summary>
			/// コンストラクタ
			/// </summary>
			/// <param name="isPressed">新しい押下状態</param>
			public PressingChangedEventArgs(bool isPressed) {
				this.IsPressed = isPressed;
			}
		}

		/// <summary>
		/// 押下状態変更デリゲート
		/// </summary>
		/// <param name="sender">送り主</param>
		/// <param name="e">イベント引数</param>
		public delegate void PressingChanged(object sender, PressingChangedEventArgs e);

		/// <summary>
		/// 押下状態変更イベント
		/// </summary>
		public event PressingChanged AfterPressingChanged;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		public LongPressButton() {
			// ダブルクリックを禁止する
			SetStyle(ControlStyles.StandardDoubleClick, false);
		}

		/// <summary>
		/// 押下開始時のシステム時間(ms)
		/// </summary>
		[Category("LongPressButton")]
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public uint StartTime {
			get { return _StartTime; }
			set { _StartTime = value; }
		}

		/// <summary>
		/// 押下開始からの経過時間(ms)、ボタン押されていない場合には無意味な値が返る
		/// </summary>
		[Category("LongPressButton")]
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public uint ElapsedTime {
			get { return MM_GetTime() - _StartTime; }
		}

		/// <summary>
		/// ボタンが押されているかどうか、制御処理のループの中で参照する
		/// </summary>
		[Category("LongPressButton")]
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool IsPressed {
			get { return _IsPressed; }
			private set {
				if (value == _IsPressed)
					return;

				_IsPressed = value;

				if (AfterPressingChanged != null)
					AfterPressingChanged(this, new PressingChangedEventArgs(value));
			}
		}

		protected override void OnMouseDown(MouseEventArgs mevent) {
			base.OnMouseDown(mevent);
			_Captured = true;
			if (!IsPressed)
				_StartTime = MM_GetTime(); // 押下開始時のシステム時間セット
			IsPressed = true;
		}

		protected override void OnMouseUp(MouseEventArgs mevent) {
			base.OnMouseUp(mevent);
			IsPressed = false;
			_Captured = false;
		}

		protected override void OnMouseLeave(EventArgs e) {
			base.OnMouseLeave(e);
			IsPressed = false;
			_Captured = false;
		}

		protected override void OnLeave(EventArgs e) {
			base.OnLeave(e);
			IsPressed = false;
			_Captured = false;
		}

		void LongPressButton_LostFocus(object sender, EventArgs e) {
			base.OnLeave(e);
			IsPressed = false;
			_Captured = false;
		}

		protected override void OnMouseMove(MouseEventArgs mevent) {
			base.OnMouseMove(mevent);

			Rectangle rc = this.ClientRectangle;
			Point pt = new Point(mevent.X, mevent.Y);

			if (rc.Contains(pt)) {
				if (_Captured) {
					if (!_IsPressed)
						_StartTime = MM_GetTime(); // 押下開始時のシステム時間セット
					IsPressed = true; // キャプチャされた状態で再度クライアント領域内に入ったら押された状態にする
				}
			} else {
				IsPressed = false; // クライアント領域外に出たら放された状態にする
			}
		}
	}
}
