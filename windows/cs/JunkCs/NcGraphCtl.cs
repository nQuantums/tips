using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Lt.Graph;
using Junk;

namespace GCodeViewer {
	public partial class NcGraphCtl : UserControl {
		public enum ToolStateEnum {
			None,
			Scrolling,
		}

		#region フィールド
		CodSystem _AxisX;
		CodSystem _AxisY;
		IEnumerable<NcCodeCmd> _NcCommands;
		NcCodeCmd.PlaneEnum _Plane;
		Pen _PenG00;
		Pen _PenG01;
		Pen _PenG02;
		Pen _PenG03;
		int _MaxStep;
		int _Step;
		Vector3d _Position;
		double _Time;
		double _ViewMag = 1.0; // 背景表示倍率
		PointD _ViewPos = new PointD(0, 0); // ビューの中心座標
		bool _IgnoreGuiEvents; // GUIイベント無視するかどうか
		ToolStateEnum _ToolState; // ツール状態
		Point _OrgMousePos; // ツール開始時マウス座標
		PointD _OrgMousePosOnGraph; // ツール開始時のグラフ内座標系でのマウス座標
		Boundary _GraphBoundary;
		ValueTextBox tbMag;
		#endregion

		#region プロパティ
		/// <summary>
		/// 表示倍率テキストボックス
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ValueTextBox ViewMagTextBox {
			get { return this.tbMag;  }
			set {
				if (value == tbMag)
					return;
				if (tbMag != null)
					tbMag.AfterValueChange -= tbMag_AfterValueChange;
				tbMag = value;
				if (tbMag != null) {
					tbMag.Double = _ViewMag;
					tbMag.AfterValueChange += tbMag_AfterValueChange;
				}
			}
		}

		/// <summary>
		/// 表示倍率
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public double ViewMag {
			get { return _ViewMag; }
			set {
				if (value == _ViewMag)
					return;

				var old = _IgnoreGuiEvents;
				_IgnoreGuiEvents = true;
				try {
					_ViewMag = value;
					this.tbMag.Double = value * 100.0;
					ApplyScalePosition();
				} finally {
					_IgnoreGuiEvents = old;
				}
			}
		}

		/// <summary>
		/// 表示中心位置
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public PointD ViewPos {
			get { return _ViewPos; }
			set {
				if (value == _ViewPos)
					return;
				var old = _IgnoreGuiEvents;
				_IgnoreGuiEvents = true;
				try {
					_ViewPos = value;
					ApplyScalePosition();
				} finally {
					_IgnoreGuiEvents = old;
				}
			}
		}

		/// <summary>
		/// 現在の位置
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Vector3d Position {
			get { return _Position; }
		}

		/// <summary>
		/// 所要時間
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public double Time {
			get { return _Time; }
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int Step {
			get { return _Step; }
			set {
				if (value < 0)
					value = 0;
				if (_MaxStep < value)
					value = _MaxStep;
				if (value == _Step)
					return;
				_Step = value;
				UpdateVisible();
			}
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int MaxStep {
			get { return _MaxStep; }
		}


		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public NcCodeCmd.PlaneEnum Plane {
			get { return _Plane; }
			set {
				if (value == _Plane)
					return;
				_Plane = value;
				UpdateCommands();
				AutoZoom();
			}
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IEnumerable<NcCodeCmd> NcCommands {
			get { return _NcCommands; }
			set {
				_NcCommands = value;
				UpdateCommands();
			}
		}
		#endregion

		#region メソッド
		public NcGraphCtl() {
			InitializeComponent();
		}

		protected override void OnLoad(EventArgs e) {
			base.OnLoad(e);

			if (this.DesignMode)
				return;

			_PenG00 = new Pen(Color.FromArgb(64, 127, 64));
			_PenG01 = new Pen(Color.FromArgb(0, 0, 0));
			_PenG02 = new Pen(Color.FromArgb(255, 0, 0));
			_PenG03 = new Pen(Color.FromArgb(0, 0, 255));

			_AxisX = new CodSystem(CodSysId.NormX);
			_AxisX.UnitName = "変位";
			_AxisX.UnitSymbol = "mm";
			_AxisX.Range = new RangeD(-100, 100);
			_AxisX.RangeMinSize = 0.0001;
			_AxisX.Scale.Pen = new Pen(Color.FromArgb(224, 224, 224), 1);
			_AxisX.Scale.Pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
			_AxisX.Scale.PenOrg = new Pen(Color.FromArgb(192, 192, 192), 1);
			_AxisX.Scale.Division = 0.1;
			_AxisX.Scale.MinDivisionInDevice = 32;
			this.graphCtl1.CodSystems[1] = _AxisX;

			_AxisY = new CodSystem(CodSysId.NormY);
			_AxisY.UnitName = "変位";
			_AxisY.UnitSymbol = "mm";
			_AxisY.Range = new RangeD(-100, 100);
			_AxisY.RangeMinSize = 0.0001;
			_AxisY.Scale.Pen = new Pen(Color.FromArgb(224, 224, 224), 1);
			_AxisY.Scale.Pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
			_AxisY.Scale.PenOrg = new Pen(Color.FromArgb(192, 192, 192), 1);
			_AxisY.Scale.Division = 0.1;
			_AxisY.Scale.MinDivisionInDevice = 32;
			this.graphCtl1.CodSystems[2] = _AxisY;

			this.scaleCtl2.UnitFormat = "{0}\n[{1}]";

			// イベントハンドラ登録
			this.graphCtl1.Resize += graphCtl1_Resize;
			this.graphCtl1.MouseDown += graphCtl1_MouseDown;
			this.graphCtl1.MouseUp += graphCtl1_MouseUp;
			this.graphCtl1.MouseMove += graphCtl1_MouseMove;

			Junk.MouseWheelHandler.Add(this.graphCtl1, MyOnMouseWheel);
		}

		PointD Transform(Vector3d v) {
			switch (_Plane) {
			case NcCodeCmd.PlaneEnum.XY:
				return new PointD(v.X, v.Y);
			case NcCodeCmd.PlaneEnum.ZX:
				return new PointD(v.Z, v.X);
			case NcCodeCmd.PlaneEnum.YZ:
				return new PointD(v.Y, v.Z);
			default:
				throw new NotImplementedException();
			}
		}

		public void AutoZoom() {
			if (_GraphBoundary == null)
				return;

			var rangex = _GraphBoundary.RangeX;
			var rangey = _GraphBoundary.RangeY;
			var cx = rangex.Center;
			var cy = rangey.Center;

			var ga = this.graphCtl1.GraphArea;
			var rx = (ga.Width - 1) / rangex.Size;
			var ry = (ga.Height - 1) / rangey.Size;

			var old = _IgnoreGuiEvents;
			_IgnoreGuiEvents = true;
			try {
				_ViewMag = rx < ry ? rx : ry;
				_ViewPos = new PointD(cx, cy);
				ApplyScalePosition();
				this.tbMag.Double = _ViewMag * 100.0;
			} finally {
				_IgnoreGuiEvents = old;
			}
		}

		public void ZoomOut() {
			var scaling = 1.10;
			var newMag = _ViewMag / scaling;

			_ViewMag = newMag;

			_IgnoreGuiEvents = true;
			this.tbMag.Double = _ViewMag * 100.0;
			_IgnoreGuiEvents = false;

			ApplyScalePosition();
		}

		public void ZoomIn() {
			var scaling = 1.10;
			var newMag = _ViewMag * scaling;

			_ViewMag = newMag;

			_IgnoreGuiEvents = true;
			this.tbMag.Double = _ViewMag * 100.0;
			_IgnoreGuiEvents = false;

			ApplyScalePosition();
		}

		void UpdateCommands() {

			switch (_Plane) {
			case NcCodeCmd.PlaneEnum.XY:
				_AxisX.UnitName = "X";
				_AxisY.UnitName = "Y";
				break;
			case NcCodeCmd.PlaneEnum.ZX:
				_AxisX.UnitName = "Z";
				_AxisY.UnitName = "X";
				break;
			case NcCodeCmd.PlaneEnum.YZ:
				_AxisX.UnitName = "Y";
				_AxisY.UnitName = "Z";
				break;
			}


			var lines = this.graphCtl1.Lines;
			lines.Clear();

			if (_NcCommands != null) {
				int index = 0;

				foreach (var cmd in _NcCommands) {
					switch (cmd.CmdType) {
					case NcCodeCmdType.G00:
						// 位置決め
						{
							var l = new Line2D();
							l.XCodSysId = 1;
							l.YCodSysId = 2;
							l.Data = new LineDataArray2D(new PointD[] {
									Transform(cmd.StartPos),
									Transform(cmd.EndPos),
								});
							l.Pen = _PenG00;
							l.Tag = cmd;
							lines[index++] = l;
						}
						break;

					case NcCodeCmdType.G01:
						// 位置決め
						{
							var l = new Line2D();
							l.XCodSysId = 1;
							l.YCodSysId = 2;
							l.Data = new LineDataArray2D(new PointD[] {
								Transform(cmd.StartPos),
								Transform(cmd.EndPos),
							});
							l.Pen = _PenG01;
							l.Tag = cmd;
							lines[index++] = l;
						}
						break;

					case NcCodeCmdType.G02:
					case NcCodeCmdType.G03:
						// 円弧(CW)、(CCW)
						{
							if (cmd.IsNoMove)
								break;
							int div = Math.Max(10, (int)Math.Ceiling(Math.Abs(180.0 * cmd.Arc.Gamma / Math.PI)));
							var pts = new PointD[div];
							int n = div - 1;
							var plane = cmd.Plane;

							// 指定平面外の軸の開始位置と変化量取得
							double zs, dz;
							switch (plane) {
							case NcCodeCmd.PlaneEnum.XY:
								zs = cmd.StartPos.Z;
								dz = cmd.EndPos.Z - cmd.StartPos.Z;
								break;
							case NcCodeCmd.PlaneEnum.ZX:
								zs = cmd.StartPos.Y;
								dz = cmd.EndPos.Y - cmd.StartPos.Y;
								break;
							case NcCodeCmd.PlaneEnum.YZ:
								zs = cmd.StartPos.X;
								dz = cmd.EndPos.X - cmd.StartPos.X;
								break;
							default:
								zs = dz = 0.0;
								break;
							}

							// 円弧を直線に分割する
							pts[0] = Transform(cmd.StartPos);
							for (int i = 1; i < n; i++) {
								var t = (double)i / n;

								// 円弧補間で指定平面座標計算
								var v2 = cmd.Arc.GetArcPoint(t);

								// 指定平面外の軸値計算
								var z = zs + dz * t;

								// 一応３次元座標計算
								Vector3d v;
								switch (plane) {
								case NcCodeCmd.PlaneEnum.XY:
									v.X = v2.X;
									v.Y = v2.Y;
									v.Z = z;
									break;
								case NcCodeCmd.PlaneEnum.ZX:
									v.Z = v2.X;
									v.X = v2.Y;
									v.Y = z;
									break;
								case NcCodeCmd.PlaneEnum.YZ:
									v.Y = v2.X;
									v.Z = v2.Y;
									v.X = z;
									break;
								default:
									v = new Vector3d(0.0, 0.0, 0.0);
									break;
								}

								pts[i] = Transform(v);
							}
							pts[n] = Transform(cmd.EndPos);

							var l = new Line2D();
							l.XCodSysId = 1;
							l.YCodSysId = 2;
							l.Data = new LineDataArray2D(pts);
							l.Pen = cmd.CmdType == NcCodeCmdType.G02 ? _PenG02 : _PenG03;
							l.Tag = cmd;
							lines[index++] = l;
						}
						break;

					case NcCodeCmdType.G04:
						// ドウエル
						{
							var l = new Line2D();
							l.XCodSysId = 1;
							l.YCodSysId = 2;
							l.Tag = cmd;
							lines[index++] = l;
						}
						break;
					}
				}

				Boundary b = new Boundary();
				foreach (var l in lines) {
					var bb = l.Boundary;
					b.Union(bb);
				}
				_GraphBoundary = b;
				AutoZoom();

				_MaxStep = index;
				_Step = index;

				UpdateVisible();
			} else {
				_MaxStep = 0;
				_Step = 0;

				UpdateVisible();
			}
		}

		void UpdateVisible() {
			var lines = this.graphCtl1.Lines;
			double time = 0.0;
			for (int i = 0; i < _MaxStep; i++) {
				var l = lines[i];
				var visible = i < _Step;
				l.Visible = visible;
				if (visible) {
					var cmd = l.Tag as NcCodeCmd;
					time += cmd.XYTime;
				}
			}
			_Time = time;

			if (_Step < _MaxStep) {
				var l = lines[_Step];
				_Position = ((NcCodeCmd)l.Tag).StartPos;
			} else {
				var l = lines.GetItemWithoutException(_Step - 1);
				if (l != null) {
					_Position = ((NcCodeCmd)l.Tag).EndPos;
				} else {
					_Position = new Vector3d(0, 0, 0);
				}
			}
		}

		RangeD[] CalcRanges(double mag) {
			var ga = this.graphCtl1.GraphArea;
			var w = (ga.Width - 1) / mag;
			var h = (ga.Height - 1) / mag;
			var w2 = w / 2.0;
			var h2 = h / 2.0;
			return new RangeD[] {
				new RangeD(_ViewPos.X - w2, _ViewPos.X + w2),
				new RangeD(_ViewPos.Y - h2, _ViewPos.Y + h2),
			};
		}

		public void ApplyScalePosition() {
			var ga = this.graphCtl1.GraphArea;
			var ranges = CalcRanges(_ViewMag);
			_AxisX.Range = ranges[0];
			_AxisY.Range = ranges[1];
		}
		#endregion

		#region イベントハンドラ

		/// <summary>
		/// マウスホイールで拡大縮小
		/// </summary>
		void MyOnMouseWheel(MouseEventArgs e) {
			if (e.Delta < 0)
				ZoomOut();
			else
				ZoomIn();
			ApplyScalePosition();
		}

		void graphCtl1_MouseDown(object sender, MouseEventArgs e) {
			switch (_ToolState) {
			case ToolStateEnum.None:
				if (e.Button == System.Windows.Forms.MouseButtons.Left) {
					var tfx = _AxisX.DeviceTransform;
					var tfy = _AxisY.DeviceTransform;

					this.graphCtl1.Capture = true;
					_OrgMousePos = new Point(e.X, e.Y);
					_OrgMousePosOnGraph = new PointD(tfx.InvCnv(e.X), tfy.InvCnv(e.Y));
					_ToolState = ToolStateEnum.Scrolling;
				}
				break;
			}
		}

		void graphCtl1_MouseUp(object sender, MouseEventArgs e) {
			switch (_ToolState) {
			case ToolStateEnum.Scrolling:
				if (e.Button == System.Windows.Forms.MouseButtons.Left) {
					this.graphCtl1.Capture = false;
					_ToolState = ToolStateEnum.None;
				}
				break;
			}
		}

		void graphCtl1_MouseMove(object sender, MouseEventArgs e) {
			var tfx = _AxisX.DeviceTransform;
			var tfy = _AxisY.DeviceTransform;
			var x = tfx.InvCnv(e.X);
			var y = tfy.InvCnv(e.Y);
			//this.tbCursorPos.Text = x.ToString("F2") + ", " + y.ToString("F2");

			switch (_ToolState) {
			case ToolStateEnum.Scrolling: {
					_ViewPos.X += _OrgMousePosOnGraph.X - x;
					_ViewPos.Y += _OrgMousePosOnGraph.Y - y;
					ApplyScalePosition();

					_IgnoreGuiEvents = true;
					_IgnoreGuiEvents = false;
				}
				break;
			}
		}

		private void tbMag_AfterValueChange(object sender, EventArgs e) {
			ValueTextBox tb = sender as ValueTextBox;
			if (tb == null)
				return;

			if (_IgnoreGuiEvents)
				return;
			if (!string.IsNullOrEmpty(tb.ErrorMessage))
				return;

			if (tb == this.tbMag) {
				_ViewMag = this.tbMag.Double / 100.0;
			}

			ApplyScalePosition();
		}

		void graphCtl1_Resize(object sender, EventArgs e) {
			ApplyScalePosition();
		}
		#endregion
	}
}
