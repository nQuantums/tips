using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.ComponentModel;

namespace Junk {
	/// <summary>
	/// 値入力テキストボックス
	/// </summary>
	public class ValueTextBox : TextBox {
		#region PInvoke
		const int WM_CHAR = 0x0102;
		const int WM_SETTEXT = 0x000C;
		const int WM_PASTE = 0x302;
		const int WM_COMMAND = 0x0111;

		const int EN_CHANGE = 0x0300;

		const int VK_BACK = 0x08;
		const int VK_ESCAPE = 0x1B;
		const int VK_LBUTTON = 0x01;
		const int VK_CANCEL = 0x03;
		const int VK_TAB = 0x09;
		const int VK_DELETE = 0x2E;
		#endregion

		#region クラス、インターフェース宣言
		/// <summary>
		///	値入力用テキストボックスの実際の値と文字列の相互変換を行うためのインターフェース。
		/// </summary>
		public interface IFormatter {
			/// <summary>
			/// 実際の値から文字列への変換を行う。
			/// </summary>
			/// <param name="sender">呼び出し元の数値入力テキストボックス。</param>
			/// <param name="val">実際の値。</param>
			/// <returns>変換された文字列。</returns>
			string ValueToString(ValueTextBox sender, object val);

			/// <summary>
			/// 文字列から実際の値への変換を行う。
			/// </summary>
			/// <param name="sender">呼び出し元の数値入力テキストボックス。</param>
			/// <param name="text">変換する文字列。</param>
			/// <param name="errMsg">変換に失敗した場合のエラーメッセージが返る。エラーメッセージ無しの場合は null が返る。</param>
			/// <returns>変換成功:実際の値が返る、変換失敗:null。</returns>
			object StringToValue(ValueTextBox sender, string text, out string errMsg);

			/// <summary>
			/// 指定された文字列が正しく数値に変換できる文字列か判定する
			/// </summary>
			/// <param name="sender">呼び出し元の数値入力テキストボックス。</param>
			/// <param name="text">文字列</param>
			/// <param name="underEditing">編集途中の文字列かどうか</param>
			/// <returns>true:正しく変換できる、false:正しくない</returns>
			bool IsValue(ValueTextBox sender, string text, bool underEditing);

			/// <summary>
			/// 値の比較を行う
			/// </summary>
			/// <param name="val1">値１</param>
			/// <param name="val2"><値２/param>
			/// <returns>値１＜値２なら負数、値１＝値２なら０、値１＞値２なら正数が返る</returns>
			int Compare(object val1, object val2);
		}

		/// <summary>
		///	実数用の値、文字列相互変換クラス。
		/// </summary>
		public class DoubleFormatter : IFormatter {
			static Regex RxUnderEditing = new Regex(@"[\+\-]?([0-9]+(\.[0-9]*)?(e[\+\-]?[0-9]*)?)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
			static Regex Rx = new Regex(@"[\+\-]?[0-9]+(\.[0-9]+)?(e[\+\-]?[0-9]+)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
			static Regex RxUnderEditingPlus = new Regex(@"[\+]?([0-9]+(\.[0-9]*)?(e[\+\-]?[0-9]*)?)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
			static Regex RxPlus = new Regex(@"[\+]?[0-9]+(\.[0-9]+)?(e[\+\-]?[0-9]+)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

			/// <summary>
			/// 実数から文字列へ変換する。
			/// </summary>
			public string ValueToString(ValueTextBox sender, object val) {
				if (val == null)
					return "";

				var v = Convert.ToDouble(val);
				if (sender.DecimalPlaces == 0) {
					return v.ToString();
				} else {
					return v.ToString(string.Format("F{0}", sender.DecimalPlaces));
				}
			}

			/// <summary>
			/// 文字列から実際の値への変換を行う。
			/// </summary>
			public object StringToValue(ValueTextBox sender, string text, out string errMsg) {
				double v;
				bool result = double.TryParse(text, out v);
				if (result) {
					errMsg = null;
					return v;
				} else {
					errMsg = NotANumber;
					return null;
				}
			}

			/// <summary>
			/// 指定された文字列が正しく数値に変換できる文字列か判定する
			/// </summary>
			public bool IsValue(ValueTextBox sender, string text, bool underEditing) {
				Match m;
				if (underEditing) {
					if (sender.AllowNegativeValue)
						m = RxUnderEditing.Match(text);
					else
						m = RxUnderEditingPlus.Match(text);
				} else {
					if (sender.AllowNegativeValue)
						m = Rx.Match(text);
					else
						m = RxPlus.Match(text);
				}
				return m.Success && m.Index == 0 && m.Length == text.Length;
			}

			/// <summary>
			/// 値の比較を行う
			/// </summary>
			public int Compare(object val1, object val2) {
				if (val1 == null) {
					return val2 != null ? -1 : 0;
				}
				if (val2 == null) {
					return val1 != null ? 1 : 0;
				}
				return Math.Sign(Convert.ToDouble(val1) - Convert.ToDouble(val2));
			}
		}

		/// <summary>
		///	整数用の値、文字列相互変換クラス。
		/// </summary>
		public class IntFormatter : IFormatter {
			static Regex RxUnderEditing = new Regex(@"[\+\-]?([0-9]+(e[\+\-]?[0-9]*)?)?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
			static Regex Rx = new Regex(@"[\+\-]?[0-9]+(e[\+\-]?[0-9]+)?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
			static Regex RxUnderEditingPlus = new Regex(@"[\+]?([0-9]+(e[\+\-]?[0-9]*)?)?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
			static Regex RxPlus = new Regex(@"[\+]?[0-9]+(e[\+\-]?[0-9]+)?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

			/// <summary>
			/// 実数から文字列へ変換する。
			/// </summary>
			public string ValueToString(ValueTextBox sender, object val) {
				if (val == null)
					return "";

				var v = Convert.ToInt64(val);
				return v.ToString();
			}

			/// <summary>
			/// 文字列から実際の値への変換を行う。
			/// </summary>
			public object StringToValue(ValueTextBox sender, string text, out string errMsg) {
				long v;
				bool result = long.TryParse(text, out v);
				if (result) {
					errMsg = null;
					return v;
				} else {
					errMsg = NotANumber;
					return null;
				}
			}
			/// <summary>
			/// 指定された文字列が正しく数値に変換できる文字列か判定する
			/// </summary>
			public bool IsValue(ValueTextBox sender, string text, bool underEditing) {
				Match m;
				if (underEditing) {
					if (sender.AllowNegativeValue)
						m = RxUnderEditing.Match(text);
					else
						m = RxUnderEditingPlus.Match(text);
				} else {
					if (sender.AllowNegativeValue)
						m = Rx.Match(text);
					else
						m = RxPlus.Match(text);
				}
				return m.Success && m.Index == 0 && m.Length == text.Length;
			}

			/// <summary>
			/// 値の比較を行う
			/// </summary>
			public int Compare(object val1, object val2) {
				if (val1 == null) {
					return val2 != null ? -1 : 0;
				}
				if (val2 == null) {
					return val1 != null ? 1 : 0;
				}
				return Math.Sign(Convert.ToInt64(val1) - Convert.ToInt64(val2));
			}
		}

		/// <summary>
		///	数値入力モード列挙値
		/// </summary>
		public enum FormatModeEnum {
			/// <summary>
			/// 実数モード
			/// </summary>
			Double,

			/// <summary>
			/// 整数モード
			/// </summary>
			Int,

			/// <summary>
			/// カスタマイズされた入力モード
			/// </summary>
			Custom
		}

		/// <summary>
		/// 範囲チェックモード
		/// </summary>
		public enum RangeModeEnum {
			/// <summary>
			/// 範囲チェックを実行しない。
			/// </summary>
			None,

			/// <summary>
			/// 下限値以上なら範囲内
			/// </summary>
			Min,

			/// <summary>
			/// 上限値以下なら範囲内
			/// </summary>
			Max,

			/// <summary>
			/// 下限値より大きければ範囲内
			/// </summary>
			GMin,

			/// <summary>
			/// 上限値より小さければ範囲内
			/// </summary>
			LMax,

			/// <summary>
			/// 下限値以上で上限値以下なら範囲内
			/// </summary>
			MinMax,

			/// <summary>
			/// 下限値以上で上限値より小さければ範囲内
			/// </summary>
			MinLMax,

			/// <summary>
			/// 下限値より大きく上限値以下ならば範囲内
			/// </summary>
			GMinMax,

			/// <summary>
			/// 下限値より大きく上限値より小さければ範囲内
			/// </summary>
			GMinLMax,
		}
		#endregion

		#region フィールド
		const string NotANumber = "数値ではありません。";

		object _Value = null; // 値
		FormatModeEnum _FormatMode = FormatModeEnum.Int; // 値入力形式モード
		IFormatter _Formatter = new IntFormatter(); // 値⇔文字列変換オブジェクト
		IFormatter _CustomFormatter = null; // ユーザーにより設定された値⇔文字列変換オブジェクト
		RangeModeEnum _RangeMode = RangeModeEnum.None; // 範囲チェックモード
		object _MinValue = null; // 最小値
		object _MaxValue = null; // 最大値
		bool _AllowNegativeValue = true; // 負数にならないかどうか
		int _DecimalPlaces = 0; // 小数点以下桁数
		ErrorProvider _ErrorProvider; // エラープロバイダ
		string _ErrorMessage; // エラーメッセージ
		bool _IgnoreGuiEvents; // GUIイベントを無視するかどうか
		#endregion

		#region イベント関係
		/// <summary>
		/// 値変更後イベント
		/// </summary>
		public event EventHandler AfterValueChange;
		#endregion

		#region コンストラクタ
		/// <summary>
		///	コンストラクタ。
		/// </summary>
		public ValueTextBox() {
			////コンテキストメニューを非表示にする
			//this.ContextMenu = new ContextMenu();
			//this.ImeMode = System.Windows.Forms.ImeMode.Disable;
		}
		#endregion

		#region プロパティ
		/// <summary>
		/// 値
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public object Value {
			get { return _Value; }
			set {
				SetValue(value, true);
			}
		}

		/// <summary>
		/// テキストボックスの現在の文字列の設定と取得。
		/// </summary>
		public override string Text {
			get {
				return base.Text;
			}
			set {
				string errMsg;
				var val = StringToValue(base.Text, out errMsg);
				if (val == null) {
					this.ErrorMessage = errMsg;
					return;
				}

				SetValue(val, true);
			}
		}

		/// <summary>
		/// 実数値の設定と取得。
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public double Double {
			get {
				var v = this.Value;
				return v != null ? Convert.ToDouble(v) : 0.0;
			}
			set {
				this.Value = value;
			}
		}

		/// <summary>
		/// 整数値の設定と取得。
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int Int {
			get {
				var v = this.Value;
				return v != null ? Convert.ToInt32(v) : 0;
			}
			set {
				this.Value = value;
			}
		}

		/// <summary>
		/// 64ビット整数値の設定と取得。
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public long Long {
			get {
				var v = this.Value;
				return v != null ? Convert.ToInt64(v) : 0;
			}
			set {
				this.Value = value;
			}
		}

		/// <summary>
		/// 値入力形式モード
		/// </summary>
		[Category("ValueTextBox")]
		[Description("値入力形式モード。")]
		[DefaultValue(FormatModeEnum.Int)]
		public FormatModeEnum FormatMode {
			get { return _FormatMode; }
			set {
				if (value == _FormatMode)
					return;
				_FormatMode = value;
				switch (value) {
				case FormatModeEnum.Int:
					_Formatter = new IntFormatter();
					break;
				case FormatModeEnum.Double:
					_Formatter = new DoubleFormatter();
					break;
				}
			}
		}

		/// <summary>
		/// 現在内部で使用している値⇔文字列変換オブジェクトの取得、null になることはない
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IFormatter Formatter {
			get {
				if (_FormatMode == FormatModeEnum.Custom)
					return _CustomFormatter;
				else
					return _Formatter;
			}
		}

		/// <summary>
		/// ユーザーにより設定された値⇔文字列変換オブジェクト
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IFormatter CustomFormatter {
			get { return _CustomFormatter; }
			set { _CustomFormatter = value; }
		}

		/// <summary>
		/// 値入力範囲チェックモード
		/// </summary>
		[Category("ValueTextBox")]
		[Description("値入力範囲チェックモード。")]
		public RangeModeEnum RangeMode {
			get { return _RangeMode; }
			set { _RangeMode = value; }
		}

		/// <summary>
		/// 最小値
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public object MinValue {
			get { return _MinValue; }
			set { _MinValue = value; }
		}

		/// <summary>
		/// 最大値
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public object MaxValue {
			get { return _MaxValue; }
			set { _MaxValue = value; }
		}

		/// <summary>
		/// 最小値を文字列で表したもの
		/// </summary>
		[Category("ValueTextBox")]
		[Description("最小値を文字列で表したもの。")]
		public string MinValueString {
			get { return _MinValue != null ? _MinValue.ToString() : ""; }
			set {
				if(value == null)
					value = "";
				string errMsg;
				_MinValue = StringToValue(value, out errMsg);
			}
		}

		/// <summary>
		/// 最大値を文字列で表したもの
		/// </summary>
		[Category("ValueTextBox")]
		[Description("最大値を文字列で表したもの。")]
		public string MaxValueString {
			get { return _MaxValue != null ? _MaxValue.ToString() : ""; }
			set {
				if (value == null)
					value = "";
				string errMsg;
				_MaxValue = StringToValue(value, out errMsg);
			}
		}

		/// <summary>
		///	入力値有効範囲の説明文の取得。
		/// </summary>
		[Category("ValueTextBox")]
		[Description("表示される入力値有効範囲の説明文です。")]
		public string RangeDescription {
			get {
				switch (_RangeMode) {
				case RangeModeEnum.Min:
					return this.MinValueString + "≦入力値";
				case RangeModeEnum.Max:
					return "入力値≦" + this.MaxValueString;
				case RangeModeEnum.GMin:
					return this.MinValueString + "＜入力値";
				case RangeModeEnum.LMax:
					return "入力値＜" + this.MaxValueString;
				case RangeModeEnum.MinMax:
					return this.MinValueString + "≦入力値≦" + this.MaxValueString;
				case RangeModeEnum.MinLMax:
					return this.MinValueString + "≦入力値＜" + this.MaxValueString;
				case RangeModeEnum.GMinMax:
					return this.MinValueString + "＜入力値≦" + this.MaxValueString;
				case RangeModeEnum.GMinLMax:
					return this.MinValueString + "＜入力値＜" + this.MaxValueString;
				default:
					return "";
				}
			}
		}

		/// <summary>
		/// 負数を許可するかどうか
		/// </summary>
		[Category("ValueTextBox")]
		[Description("負数を許可するかどうか。")]
		[DefaultValue(true)]
		public bool AllowNegativeValue {
			get { return _AllowNegativeValue; }
			set { _AllowNegativeValue = value; }
		}

		/// <summary>
		///	数値表示の小数点以下桁数の設定と取得。0だと最高精度になるように小数点以下桁数が設定される。
		/// </summary>
		[Category("ValueTextBox")]
		[Description("小数点以下桁数を設定します。0を指定すると桁数は自動になります。")]
		public int DecimalPlaces {
			get {
				return _DecimalPlaces;
			}
			set {
				if (value < 0 || 30 < value)
					return;
				_DecimalPlaces = value;
			}
		}

		/// <summary>
		/// エラーメッセージを表示するエラープロバイダーコントロールを取得または設定します。既定値は null です。
		/// </summary>
		[Category("ValueTextBox")]
		[DefaultValue(null)]
		[Description("エラーメッセージを表示するエラープロバイダーコントロールを取得または設定します。既定値は null です。")]
		public ErrorProvider ErrorProvider {
			get { return _ErrorProvider; }
			set {
				if (value == _ErrorProvider)
					return;
				if (_ErrorProvider != null)
					_ErrorProvider.SetError(this, null);
				_ErrorProvider = value;
				if (_ErrorProvider != null)
					_ErrorProvider.SetError(this, _ErrorMessage);
			}
		}

		/// <summary>
		/// エラーメッセージに使用するメッセージを取得または設定します。既定値は null です。
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string ErrorMessage {
			get { return _ErrorMessage; }
			set {
				if (value == _ErrorMessage)
					return;
				_ErrorMessage = value;
				if (_ErrorProvider != null)
					_ErrorProvider.SetError(this, _ErrorMessage);
			}
		}

		/// <summary>
		/// this.Value のエラー判定状態を取得する
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool HasError {
			get {
				return !string.IsNullOrEmpty(_ErrorMessage);
			}
		}
		#endregion

		#region メソッド
		/// <summary>
		/// 現在の設定で値を文字列に変換する。
		/// </summary>
		/// <param name="val">値</param>
		/// <returns>文字列</returns>
		public string ValueToString(object val) {
			return this.Formatter.ValueToString(this, val);
		}

		/// <summary>
		/// 現在の設定で文字列を値に変換する。
		/// </summary>
		/// <param name="text">変換する文字列。</param>
		/// <param name="errMsg">変換に失敗した場合のエラーメッセージが返る。エラーメッセージ無しの場合は null が返る。</param>
		/// <returns>変換成功:実際の値が返る、変換失敗:null。</returns>
		public object StringToValue(string text, out string errMsg) {
			return this.Formatter.StringToValue(this, text, out errMsg);
		}

		/// <summary>
		/// 現在のテキスト入力内容をもとにエラーメッセージを更新する
		/// </summary>
		public string UpdateErrorMessage() {
			this.ErrorMessage = null;
			ValidateValue();
			return this.ErrorMessage;
		}
		#endregion

		#region 内部メソッド
		/// <summary>
		/// 現在値の範囲チェックを行う
		/// </summary>
		bool ValidateValue() {
			var val = _Value;
			if (val == null)
				return false;

			// 負数チェックを行う
			if (!_AllowNegativeValue) {
				if (Convert.ToDouble(val) < 0.0) {
					this.ErrorMessage = "負数は入力できません。";
					return false;
				}
			}

			//	範囲チェックを行う
			bool rangeOver = false;
			var fmt = this.Formatter;

			switch (_RangeMode) {
			case RangeModeEnum.Min:
				rangeOver = fmt.Compare(val, _MinValue) < 0;
				break;
			case RangeModeEnum.Max:
				rangeOver = fmt.Compare(val, _MaxValue) > 0;
				break;
			case RangeModeEnum.GMin:
				rangeOver = fmt.Compare(val, _MinValue) <= 0;
				break;
			case RangeModeEnum.LMax:
				rangeOver = fmt.Compare(val, _MaxValue) >= 0;
				break;
			case RangeModeEnum.MinMax:
				rangeOver = fmt.Compare(val, _MinValue) < 0 || fmt.Compare(val, _MaxValue) > 0;
				break;
			case RangeModeEnum.MinLMax:
				rangeOver = fmt.Compare(val, _MinValue) < 0 || fmt.Compare(val, _MaxValue) >= 0;
				break;
			case RangeModeEnum.GMinMax:
				rangeOver = fmt.Compare(val, _MinValue) <= 0 || fmt.Compare(val, _MaxValue) > 0;
				break;
			case RangeModeEnum.GMinLMax:
				rangeOver = fmt.Compare(val, _MinValue) <= 0 || fmt.Compare(val, _MaxValue) >= 0;
				break;
			}
			if (rangeOver) {
				this.ErrorMessage = string.Format("値が有効範囲外です。\n有効範囲: {0}", RangeDescription);
				return false;
			}

			return true;
		}

		/// <summary>
		/// テキストを設定する
		/// </summary>
		/// <param name="text">テキスト</param>
		protected void SetText(string text) {
			if (base.Text == text)
				return;
			var old = _IgnoreGuiEvents;
			_IgnoreGuiEvents = true;
			try {
				base.Text = text;
			} finally {
				_IgnoreGuiEvents = old;
			}
		}

		/// <summary>
		/// 値を設定する
		/// </summary>
		/// <param name="value">値</param>
		/// <param name="updateText">テキストを更新するかどうか</param>
		/// <returns>Value が指定した値になり値が有効範囲内なら true 、それ以外は false が返る</returns>
		protected bool SetValue(object value, bool updateText) {
			if ((value != null) == (_Value != null)) {
				if (value.Equals(_Value))
					return true;
			}
			_Value = value;

			if (updateText) {
				SetText(ValueToString(value));
			}

			this.ErrorMessage = null;

			var result = ValidateValue();

			var d = this.AfterValueChange;
			if (d != null) {
				d(this, EventArgs.Empty);
			}

			return result;
		}
		#endregion

		#region オーバーライド
		[System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.LinkDemand, Flags = System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
		protected override void WndProc(ref Message m) {
			if (m.Msg == WM_CHAR) {
				// 入力文字取得
				int nChar = m.WParam.ToInt32();

				// LBUTTON , Ctrl + Break
				if (nChar == VK_LBUTTON || nChar == VK_CANCEL) {
					base.WndProc(ref m);
					return;
				}

				// 入力された文字に応じて現在の文字列を変更する
				StringBuilder sb = new StringBuilder(base.Text);
				int start = this.SelectionStart;
				int count = this.SelectionLength;

				if (count != 0) {
					sb.Remove(start, count);
				} else {
					if (nChar == VK_BACK && 1 <= start)
						sb.Remove(start - 1, 1);
				}

				if (nChar != VK_BACK)
					sb.Insert(start, (char)nChar);

				// 文字列が数値なのか判定する
				string text = sb.ToString();
				if (text.Length != 0) {
					if (!this.Formatter.IsValue(this, text, true)) {
						return;
					}
				}

				// 文字列から値を取得
				string errMsg;
				object val = StringToValue(text, out errMsg);
				if (val != null) {
					SetValue(val, false);
				} else {
					this.ErrorMessage = errMsg;
				}
			} else if (m.Msg == WM_PASTE) {
				return;
			}
			base.WndProc(ref m);
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
			//Ctrl+Vを無効にする
			if ((keyData & Keys.Control) == Keys.Control &&
				(keyData & Keys.KeyCode) == Keys.V)
				return true;
			else
				return base.ProcessCmdKey(ref msg, keyData);
		}

		/// <summary>
		/// 検証イベントをオーバーライド。
		/// </summary>
		protected override void OnValidating(CancelEventArgs e) {
			if (!ValidateValue()) {
				e.Cancel = true;
			}
			base.OnValidating(e);
		}

		/// <summary>
		/// 検証後イベントをオーバーライド。
		/// </summary>
		protected override void OnValidated(EventArgs e) {
			// 値をテキストに変換することで文字列を書式化する
			SetText(ValueToString(_Value));
			base.OnValidated(e);
		}

		/// <summary>
		/// OnTextChanged オーバーライド。
		/// </summary>
		protected override void OnTextChanged(EventArgs e) {
			if (!_IgnoreGuiEvents) {
				// 変更中のテキストを即座に値に変換する
				var text = base.Text;
				object val;
				string errMsg;

				// 文字列を数値に変換
				val = StringToValue(text, out errMsg);
				if (val != null) {
					SetValue(val, false);
				} else {
					this.ErrorMessage = errMsg;
				}
			}

			base.OnTextChanged(e);
		}
		#endregion
	}
}
