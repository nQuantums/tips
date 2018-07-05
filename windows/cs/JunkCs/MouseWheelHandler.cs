using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Jk {
	/// <summary>
	/// マウスホイールをハンドリングするためのクラス
	/// </summary>
	public static class MouseWheelHandler {
		[DllImport("user32.dll")]
		static extern IntPtr WindowFromPoint(Vector2i pos);

		public static void Add(Control ctrl, Action<MouseEventArgs> onMouseWheel) {
			if (ctrl == null || onMouseWheel == null)
				throw new ArgumentNullException();

			var filter = new MouseWheelMessageFilter(ctrl, onMouseWheel);
			Application.AddMessageFilter(filter);
			ctrl.Disposed += (s, e) => Application.RemoveMessageFilter(filter);
		}

		class MouseWheelMessageFilter
			: IMessageFilter {
			private readonly Control _ctrl;
			private readonly Action<MouseEventArgs> _onMouseWheel;

			public MouseWheelMessageFilter(Control ctrl, Action<MouseEventArgs> onMouseWheel) {
				_ctrl = ctrl;
				_onMouseWheel = onMouseWheel;
			}

			public bool PreFilterMessage(ref Message m) {
				// WM_MOUSEWHEEL, find the control at screen position m.LParam
				if (m.Msg != 0x20a || !_ctrl.IsHandleCreated)
					return false;

				var ulWParam = (ulong)m.WParam.ToInt64();
				var ulLParam = (ulong)m.LParam.ToInt64();
				var uiWParam = unchecked((uint)ulWParam);
				var uiLParam = unchecked((uint)ulLParam);
				var x = (short)(ushort)(uiLParam & 0xffff);
				var y = (short)(ushort)(uiLParam >> 16);

				var handle = WindowFromPoint(new Vector2i(x, y));
				if (_ctrl.Handle != handle)
					return false;

				var buttons = MouseButtons.None;
				if ((ulWParam & 0x0001) != 0) buttons |= MouseButtons.Left;
				if ((ulWParam & 0x0010) != 0) buttons |= MouseButtons.Middle;
				if ((ulWParam & 0x0002) != 0) buttons |= MouseButtons.Right;
				if ((ulWParam & 0x0020) != 0) buttons |= MouseButtons.XButton1;
				if ((ulWParam & 0x0040) != 0) buttons |= MouseButtons.XButton2;
				// Not matching for these /*MK_SHIFT=0x0004;MK_CONTROL=0x0008*/

				var delta = (short)(ushort)(uiWParam >> 16);
				var cp = _ctrl.PointToClient(new Point(x, y));
				var e = new MouseEventArgs(buttons, 0, cp.X, cp.Y, delta);
				_onMouseWheel(e);

				return true;
			}
		}
	}
}
