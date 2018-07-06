using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Junk;

namespace Jk {
	/// <summary>
	/// NCコード構造解析器
	/// </summary>
	public class NcCodeParser {
		class SubProgramInfo {
			public int Id;
			public int Srart;
			public int End;
		}

		NcCodeScanner _Scanner;
		List<NcCodeToken> _Tokens;
		int _Position;
		NcCodeToken _Token;
		NcCodeCmdType _LastCmdType;

		/// <summary>
		/// 現在処理中の行番号(0ベース)
		/// </summary>
		public int LineIndex {
			get { return _Token != null ? _Token.LineIndex : -1; }
		}

		/// <summary>
		/// 現在処理中の列番号(0ベース)
		/// </summary>
		public int Position {
			get { return _Token != null ? _Token.Position : -1; }
		}

		/// <summary>
		/// 現在位置を示す説明
		/// </summary>
		public string CurrentPositionDesc {
			get {
				return _Token != null ? _Token.PositionDesc : "終端付近";
			}
		}

		/// <summary>
		/// コンストラクタ
		/// </summary>
		public NcCodeParser(NcCodeScanner scanner) {
			NcCodeToken tk;

			_Scanner = scanner;
			_Tokens = new List<NcCodeToken>();
			while ((tk = scanner.GetToken()) != null) {
				_Tokens.Add(tk);
			}

			_Position = 0;
			_Token = _Position < _Tokens.Count ? _Tokens[_Position] : null;
			_LastCmdType = NcCodeCmdType.None;
		}

		/// <summary>
		/// 構造解析しコマンド配列を作成する
		/// </summary>
		public List<NcCodeCmd> Parse() {
			try {
				var list = new List<NcCodeCmd>();
				int code;
				bool skippable = false;

				while (_Token != null) {
					NcCodeCmd cmd = null;
					var oldCount = list.Count;

					switch (_Token.Symbol) {
					case 'G':
						// Gコード解析
						code = Convert.ToInt32(_Token.Arg);
						switch (code) {
						case 0:
							// 位置決め
							_LastCmdType = NcCodeCmdType.G00;
							break;

						case 1:
							// 直線補間
							_LastCmdType = NcCodeCmdType.G01;
							break;

						case 2:
							// 円弧補間(CW)
							_LastCmdType = NcCodeCmdType.G02;
							break;

						case 3:
							// 円弧補間(CCW)
							_LastCmdType = NcCodeCmdType.G03;
							break;

						case 4:
							// ドウェル　※ウェイト
							cmd = new NcCodeCmd(_Token.LineIndex);
							cmd.CmdType = NcCodeCmdType.G04;
							cmd.Flags = NcCodeFlags.XYTime;
							cmd.XYTime = Convert.ToInt32(_Token.Data) / 1000.0;
							list.Add(cmd);
							break;

						case 17:
							// XY平面指定
							cmd = new NcCodeCmd(_Token.LineIndex);
							cmd.CmdType = NcCodeCmdType.G17;
							list.Add(cmd);
							break;

						case 18:
							// ZX平面指定
							cmd = new NcCodeCmd(_Token.LineIndex);
							cmd.CmdType = NcCodeCmdType.G18;
							list.Add(cmd);
							break;

						case 19:
							// YZ平面指定
							cmd = new NcCodeCmd(_Token.LineIndex);
							cmd.CmdType = NcCodeCmdType.G19;
							list.Add(cmd);
							break;

						case 53:
							// 機械座標系選択
							cmd = new NcCodeCmd(_Token.LineIndex);
							cmd.CmdType = NcCodeCmdType.G53;
							list.Add(cmd);
							break;

						case 90:
							// アブソリュート指令
							cmd = new NcCodeCmd(_Token.LineIndex);
							cmd.CmdType = NcCodeCmdType.G90;
							list.Add(cmd);
							break;

						case 91:
							// インクリメンタル指令
							cmd = new NcCodeCmd(_Token.LineIndex);
							cmd.CmdType = NcCodeCmdType.G91;
							list.Add(cmd);
							break;

						case 92:
							// 座標系設定
							_LastCmdType = NcCodeCmdType.G92;
							break;

						default:
							throw new ApplicationException("未対応のG" + code + "が出現しました。");
						}
						NextToken();
						break;

					case 'M':
						// Mコード解析
						code = Convert.ToInt32(_Token.Arg);
						cmd = new NcCodeCmd(_Token.LineIndex);
						switch (code) {
						case 0:
							// プログラムストップ
							cmd.CmdType = NcCodeCmdType.M00;
							NextToken();
							break;

						case 1:
							// オプショナルストップ
							cmd.CmdType = NcCodeCmdType.M01;
							NextToken();
							break;

						case 2:
							// プログラムエンド
							cmd.CmdType = NcCodeCmdType.M02;
							NextToken();
							break;

						case 10:
							// IO処理？
							cmd.CmdType = NcCodeCmdType.M10;
							NextToken();
							break;

						case 20:
							// IO処理？
							cmd.CmdType = NcCodeCmdType.M20;
							NextToken();
							break;

						case 98:
							// サブプログラム呼び出し
							cmd.CmdType = NcCodeCmdType.M98;
							NextToken();
							ParseP(cmd);
							ParseL(cmd, true);
							break;

						case 99:
							// サブプログラム終了
							cmd.CmdType = NcCodeCmdType.M99;
							NextToken();
							break;

						default:
							throw new ApplicationException("未対応のM" + code + "が出現しました。");
						}
						list.Add(cmd);
						break;

					case 'O':
						// サブプログラム番号
						cmd = new NcCodeCmd(_Token.LineIndex);
						cmd.CmdType = NcCodeCmdType.O;
						cmd.Flags |= NcCodeFlags.P;
						cmd.P = Convert.ToInt32(_Token.Arg);
						list.Add(cmd);
						NextToken();
						break;

					case 'X':
					case 'Y':
					case 'Z':
						// 軸指定
						if (_LastCmdType == NcCodeCmdType.None) {
							throw new ApplicationException("Gコマンドでのモーダル開始前に軸の指定が出現しました。");
						}
						cmd = new NcCodeCmd(_Token.LineIndex);
						cmd.CmdType = _LastCmdType;
						ParseAxis(cmd);
						if (_LastCmdType == NcCodeCmdType.G02 || _LastCmdType == NcCodeCmdType.G03)
							ParseR(cmd);
						ParseF(cmd, true);
						list.Add(cmd);
						break;

					case 'S':
						// スピンドル
						cmd = new NcCodeCmd(_Token.LineIndex);
						cmd.CmdType = NcCodeCmdType.S;
						cmd.S = Convert.ToDouble(_Token.Arg);
						list.Add(cmd);
						NextToken();
						break;

					case ';':
						// コメントアウト
						NextToken();
						break;

					case '/':
						// スキップ可能
						NextToken();
						skippable = true;
						break;

					default:
						throw new ApplicationException("未対応のコマンド '" + _Token.Symbol + "' が出現しました。");
					}

					// スキップ可能状態の最中に新しいコマンドが来たらそれにスキップ可能フラグをセットする
					if (skippable && oldCount != list.Count) {
						skippable = false;

						list[oldCount].Flags |= NcCodeFlags.IsSkippable;
					}
				}

				return list;
			} catch (Exception ex) {
				throw new NcCodeParserException(ex.Message, this);
			}
		}

		/// <summary>
		/// 指定されたNCコードコマンド配列内のサブルーチンやループなどを展開する
		/// </summary>
		public List<NcCodeCmd> Expand(List<NcCodeCmd> cmds) {
			var list = new List<NcCodeCmd>();
			var dic = SearchSubPrograms(cmds);

			for (int i = 0; i < cmds.Count; i++) {
				var cmd = cmds[i];

				if (cmd.CmdType == NcCodeCmdType.M02)
					break;

				if (cmd.CmdType == NcCodeCmdType.M98) {
					if (!dic.ContainsKey(cmd.P))
						throw new NcCodeParserException("指定されたサブプログラム" + cmd.P + "が存在しません。", cmd);

					var sbi = dic[cmd.P];
					var loopCount = (cmd.Flags & NcCodeFlags.L) != 0 ? cmd.L : 1;
					for (int loop = 0; loop < loopCount; loop++) {
						for (int j = sbi.Srart + 1; j < sbi.End; j++) {
							list.Add(cmds[j].Clone());
						}
					}
					continue;
				}

				switch (cmd.CmdType) {
				case NcCodeCmdType.O:
				case NcCodeCmdType.M99:
					continue;
				}

				list.Add(cmd.Clone());
			}

			return list;
		}

		Dictionary<int, SubProgramInfo> SearchSubPrograms(List<NcCodeCmd> cmds) {
			var dic = new Dictionary<int, SubProgramInfo>();
			SubProgramInfo sbi = null;

			for (int i = 0; i < cmds.Count; i++) {
				var cmd = cmds[i];

				if (sbi == null) {
					if (cmd.CmdType == NcCodeCmdType.O) {
						sbi = new SubProgramInfo();
						sbi.Id = cmd.P;
						sbi.Srart = i;
						sbi.End = i;
						dic.Add(sbi.Id, sbi);
					}
				} else {
					if (cmd.CmdType == NcCodeCmdType.O) {
						throw new NcCodeParserException("サブプログラムが入れ子になっています。", cmd);
					} else if (cmd.CmdType == NcCodeCmdType.M98) {
						throw new NcCodeParserException("サブプログラム内からのサブプログラム呼び出しには対応していません。", cmd);
					} else if (cmd.CmdType == NcCodeCmdType.M99) {
						sbi.End = i;
						sbi = null;
					}
				}
			}

			return dic;
		}

		void NextToken() {
			_Position++;
			_Token = _Position < _Tokens.Count ? _Tokens[_Position] : null;
		}

		void ParseAxis(NcCodeCmd cmd) {
			int lineIndex = -1;

			for (; ; ) {
				if (_Token == null)
					break;

				// 行番号が変わったら他のコマンドとなる
				if (lineIndex < 0) {
					lineIndex = _Token.LineIndex;
				} else {
					if (lineIndex != _Token.LineIndex)
						break;
				}

				bool noMatch = false;
				switch (_Token.Symbol) {
				case 'X':
					cmd.Flags |= NcCodeFlags.X;
					cmd.X = cmd.OrgX = Convert.ToDouble(_Token.Arg);
					break;

				case 'Y':
					cmd.Flags |= NcCodeFlags.Y;
					cmd.Y = cmd.OrgY = Convert.ToDouble(_Token.Arg);
					break;

				case 'Z':
					cmd.Flags |= NcCodeFlags.Z;
					cmd.Z = cmd.OrgZ = Convert.ToDouble(_Token.Arg);
					break;

				default:
					noMatch = true;
					break;
				}

				if (noMatch)
					break;

				NextToken();
			}

			if ((cmd.Flags & (NcCodeFlags.X | NcCodeFlags.Y | NcCodeFlags.Z)) == 0) {
				throw new ApplicationException("軸が指定されていません。");
			}
		}

		void ParseF(NcCodeCmd cmd, bool isOptional) {
			if (_Token != null && _Token.Symbol == 'F') {
				cmd.Flags |= NcCodeFlags.F;
				cmd.F = Convert.ToDouble(_Token.Arg);
				NextToken();
			} else {
				if (!isOptional) {
					throw new ApplicationException("速度の指定がありません。");
				}
			}
		}

		void ParseR(NcCodeCmd cmd) {
			if (_Token != null && _Token.Symbol == 'R') {
				cmd.Flags |= NcCodeFlags.R;
				cmd.R = Convert.ToDouble(_Token.Arg);
				NextToken();
			} else {
				throw new ApplicationException("半径の指定がありません。");
			}
		}

		void ParseP(NcCodeCmd cmd) {
			if (_Token != null && _Token.Symbol == 'P') {
				cmd.Flags |= NcCodeFlags.P;
				cmd.P = Convert.ToInt32(_Token.Arg);
				NextToken();
			} else {
				throw new ApplicationException("サブプログラム番号の指定がありません。");
			}
		}

		void ParseL(NcCodeCmd cmd, bool isOptional) {
			if (_Token != null && _Token.Symbol == 'L') {
				cmd.Flags |= NcCodeFlags.L;
				cmd.L = Convert.ToInt32(_Token.Arg);
				NextToken();
			} else {
				if (!isOptional) {
					throw new ApplicationException("ループ回数の指定がありません。");
				}
			}
		}
	}

	/// <summary>
	/// NCコード字句解析器
	/// </summary>
	public class NcCodeScanner {
		string[] _Lines; // NCコードプログラム全行
		int _LineIndex; // 現在解析中の行番号(0ベース)
		int _Position; // 現在解析中の行内位置(0ベース)
		string _Line; // 現在処理中の行文字列

		/// <summary>
		/// 現在処理中の行番号(0ベース)
		/// </summary>
		public int LineIndex {
			get { return _LineIndex; }
		}

		/// <summary>
		/// 現在処理中の列番号(0ベース)
		/// </summary>
		public int Position {
			get { return _Position; }
		}

		/// <summary>
		/// 現在位置を示す説明
		/// </summary>
		public string CurrentPositionDesc {
			get {
				return (_LineIndex + 1).ToString() + "行目" + (_Position + 1) + "列目: ";
			}
		}

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="lines">NCコードプログラム行配列</param>
		public NcCodeScanner(IEnumerable<string> lines) {
			_Lines = lines.ToArray();
			for (int i = 0; i < _Lines.Length; i++)
				_Lines[i] = _Lines[i].Replace("\r", "");
			_LineIndex = 0;
			_Position = 0;
			_Line = _LineIndex < _Lines.Length ? _Lines[_LineIndex] : null;

			// 1行目はコメントらしい
			NextLine();
		}

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="text">NCコードプログラムテキスト</param>
		public NcCodeScanner(string text)
			: this(text.Split('\n')) {
		}

		/// <summary>
		/// 字句を取得する
		/// </summary>
		/// <returns>字句が返る、null なら終端まで到達したことを示す</returns>
		public NcCodeToken GetToken() {
			object data;

			while (_Line != null) {
				// 無駄な余白スキップ
				SkipSpace(_Line, ref _Position);

				// 字句の開始位置
				var start = _Position;
				var c = GetCharAndNext(_Line, ref _Position);
				NcCodeToken t;
				int i;
				double d;

				try {
					switch (c) {
					case '\0':
						// 行末
						NextLine();
						break;

					case ';':
						// 行コメント
						t = new NcCodeToken(';', null, _LineIndex, start, _Line.Length - start);
						NextLine();
						return t;

					case '(':
						// コメント
						SkipToCommentEnd(_Line, ref _Position);
						return new NcCodeToken(';', null, _LineIndex, start, _Position - start);

					case 'G':
					case 'M':
					case 'N':
					case 'O':
					case 'P':
					case 'L':
						SkipSpace(_Line, ref _Position);
						i = GetInt(_Line, ref _Position);
						data = null;
		
						if (c == 'G' && i == 4) {
							// G04だけG04P...またはG04X...の様に続くコマンドがある
							// ... の部分には数字が入る
							SkipSpace(_Line, ref _Position);
							if (_Line.Length <= _Position)
								throw new ApplicationException("G04に続く時間指定がありません。");

							var c2 = _Line[_Position];
							if (c2 == 'P') {
								_Position++;
								if (_Line.Length <= _Position)
									throw new ApplicationException("G04Pに続く数値がありません。");

								data = GetInt(_Line, ref _Position);
							} else {
								throw new ApplicationException("G04の時間指定はP(ミリ秒)以外はサポートしていません。");
							}
						}

						return new NcCodeToken(c, i.ToString(), data, _LineIndex, start, _Position - start);

					case 'F':
					case 'S':
					case 'X':
					case 'Y':
					case 'Z':
					case 'I':
					case 'J':
					case 'K':
					case 'R':
						SkipSpace(_Line, ref _Position);
						d = GetDouble(_Line, ref _Position);
						return new NcCodeToken(c, d.ToString(), _LineIndex, start, _Position - start);

					case '/':
						return new NcCodeToken(c, null, _LineIndex, start, _Position - start);

					default:
						throw new ApplicationException("未対応のシンボル '" + c + "' が出現しました。");
					}
				} catch (Exception ex) {
					throw new NcCodeTokenException(ex.Message, this);
				}
			}

			return null;
		}

		/// <summary>
		/// 指定の字句に対応する元テキストを取得する
		/// </summary>
		public string GetTokenString(NcCodeToken tk) {
			var line = _Lines[tk.LineIndex];
			return line.Substring(tk.Position, tk.Length);
		}

		/// <summary>
		/// 次のへポインタを進める
		/// </summary>
		void NextLine() {
			_LineIndex++;
			_Position = 0;
			_Line = _LineIndex < _Lines.Length ? _Lines[_LineIndex] : null;
		}

		/// <summary>
		/// スペースをスキップ
		/// </summary>
		static void SkipSpace(string s, ref int index) {
			int i = index;
			for (; i < s.Length; i++) {
				var c = s[i];
				if (c != ' ' & c != '\t')
					break;
			}
			index = i;
		}

		/// <summary>
		/// '(' コメントをスキップ
		/// </summary>
		static void SkipToCommentEnd(string s, ref int index) {
			int i = index;
			for (; i < s.Length; i++) {
				if (s[i] == ')') {
					i++;
					break;
				}
			}
			index = i;
		}

		/// <summary>
		/// 数値をスキップ
		/// </summary>
		static string GetNumber(string s, ref int index) {
			var start = index;
			var i = start;
			var c = GetChar(s, i);

			if (IsSign(c))
				c = GetNextChar(s, ref i);

			if (!IsDigit(c))
				return null;

			for (; ; ) {
				c = GetNextChar(s, ref i);
				if (!IsDigit(c))
					break;
			}

			if (c == '.') {
				for (; ; ) {
					c = GetNextChar(s, ref i);
					if (!IsDigit(c))
						break;
				}
			}

			index = i;

			return s.Substring(start, i - start);
		}

		/// <summary>
		/// 整数を取得
		/// </summary>
		static int GetInt(string s, ref int index) {
			var number = GetNumber(s, ref index);
			if (number == null) {
				throw new ApplicationException("数値が必要な箇所にありません。");
			}
			try {
				return int.Parse(number);
			} catch (FormatException) {
				throw new ApplicationException("整数が必要な箇所にありません。");
			}
		}

		/// <summary>
		/// 実数を取得
		/// </summary>
		static double GetDouble(string s, ref int index) {
			var number = GetNumber(s, ref index);
			if (number == null) {
				throw new ApplicationException("数値が必要な箇所にありません。");
			}
			try {
				return double.Parse(number);
			} catch (FormatException) {
				throw new ApplicationException("実数が必要な箇所にありません。");
			}
		}

		static char GetChar(string s, int index) {
			return index < s.Length ? s[index] : '\0';
		}

		static char GetNextChar(string s, ref int index) {
			index++;
			return index < s.Length ? s[index] : '\0';
		}

		static char GetCharAndNext(string s, ref int index) {
			var c = index < s.Length ? s[index] : '\0';
			index++;
			return c;
		}

		static bool IsDigit(char c) {
			return '0' <= c && c <= '9';
		}

		static bool IsSign(char c) {
			return c == '+' || c == '-';
		}
	}

	/// <summary>
	/// NCコード字句
	/// </summary>
	public class NcCodeToken {
		/// <summary>
		/// コマンドシンボル
		/// </summary>
		public char Symbol;

		/// <summary>
		/// 引数、現状はコマンドインデックス番号として使ってる　※G03 なら3となる
		/// </summary>
		public object Arg;

		/// <summary>
		/// データ、コマンドに付与するデータ　※G04 のウェイト時間など
		/// </summary>
		public object Data;

		/// <summary>
		/// 行番号
		/// </summary>
		public int LineIndex;

		/// <summary>
		/// 列番号
		/// </summary>
		public int Position;

		/// <summary>
		/// 字句の長さ
		/// </summary>
		public int Length;

		/// <summary>
		/// 現在位置を示す説明
		/// </summary>
		public string PositionDesc {
			get {
				return (this.LineIndex + 1).ToString() + "行目" + (this.Position + 1) + "列目: ";
			}
		}

		/// <summary>
		/// コンストラクタ
		/// </summary>
		public NcCodeToken(char symbol, object arg, int lineIndex, int position, int length) {
			this.Symbol = symbol;
			this.Arg = arg;
			this.LineIndex = lineIndex;
			this.Position = position;
			this.Length = length;
		}

		/// <summary>
		/// コンストラクタ、データ追加版
		/// </summary>
		public NcCodeToken(char symbol, object arg, object data, int lineIndex, int position, int length) {
			this.Symbol = symbol;
			this.Arg = arg;
			this.Data = data;
			this.LineIndex = lineIndex;
			this.Position = position;
			this.Length = length;
		}

		public override string ToString() {
			return this.Symbol.ToString() + (this.Arg != null ? this.Arg : "");
		}
	}

	/// <summary>
	/// NCコードコマンド内の有効フィールド指定フラグ
	/// </summary>
	[Flags]
	public enum NcCodeFlags {
		X = 1 << 0,
		Y = 1 << 1,
		Z = 1 << 2,
		R = 1 << 3,
		F = 1 << 4,
		P = 1 << 5,
		L = 1 << 6,
		StartPos = 1 << 7,
		EndPos = 1 << 8,
		XYTime = 1 << 9,
		XYLength = 1 << 10,
		Arc = 1 << 11,
		IsSkippable = 1 << 12,
	}

	/// <summary>
	/// NCコードコマンド種類
	/// </summary>
	public enum NcCodeCmdType {
		None,
		G00,
		G01,
		G02,
		G03,
		G04,
		G17,
		G18,
		G19,
		G53,
		G90,
		G91,
		G92,
		M00,
		M01,
		M02,
		M10,
		M20,
		M98,
		M99,
		O,
		S,
	}

	/// <summary>
	/// NCコードコマンド
	/// </summary>
	public class NcCodeCmd {
		/// <summary>
		/// 平面種類
		/// </summary>
		public enum PlaneEnum {
			/// <summary>
			/// XY平面
			/// </summary>
			XY,

			/// <summary>
			/// ZX平面
			/// </summary>
			ZX,

			/// <summary>
			/// YZ平面
			/// </summary>
			YZ,
		}

		/// <summary>
		/// 現在の座標、絶対／相対モードなどの状態
		/// </summary>
		public class State {
			/// <summary>
			/// 絶対座標モードかどうか
			/// </summary>
			public bool AbsoluteMode;

			/// <summary>
			/// G53処理中（NCMATEだとワンショットとして扱われるらしく、次の移動命令が来た時に解除される）
			/// </summary>
			public bool G53Processing;

			/// <summary>
			/// 平面指定
			/// </summary>
			public PlaneEnum Plane;

			/// <summary>
			/// 現在の機械座標
			/// </summary>
			public Vector3d MachinePosition;

			/// <summary>
			/// ワークの原点座標(機械座標)
			/// </summary>
			public Vector3d WorkO;

			/// <summary>
			/// 現在のワーク座標(PositionOからの相対値)
			/// </summary>
			public Vector3d WorkPosition {
				get {
					return this.MachinePosition - this.WorkO;
				}
			}

			/// <summary>
			/// マシンスピード(mm/s)
			/// </summary>
			public Vector3d MachineSpeed;

			/// <summary>
			/// 送り速度
			/// </summary>
			public double Speed;

			/// <summary>
			/// コンストラクタ
			/// </summary>
			public State() {
				this.AbsoluteMode = true;
				this.G53Processing = false;
				this.Plane = PlaneEnum.XY;
				this.MachinePosition = new Vector3d(0.0, 0.0, 0.0);
				this.WorkO = new Vector3d(0.0, 0.0, 0.0);
				this.MachineSpeed = new Vector3d(0.0, 0.0, 0.0);
				this.Speed = 0.0;
			}
		}

		public int LineIndex;
		public NcCodeCmdType CmdType;
		public NcCodeFlags Flags;
		public double X;
		public double Y;
		public double Z;
		public double R;
		public double F;
		public double S;
		public int P;
		public int L;

		/// <summary>
		/// 開始位置
		/// </summary>
		public Vector3d StartPos;

		/// <summary>
		/// 終了位置
		/// </summary>
		public Vector3d EndPos;

		/// <summary>
		/// XY平面上を移動する所要時間(s)
		/// </summary>
		public double XYTime;

		/// <summary>
		/// XY平面上での移動距離
		/// </summary>
		public double XYLength;

		/// <summary>
		/// 円弧補間時の平面指定
		/// </summary>
		public PlaneEnum Plane;

		/// <summary>
		/// 円弧補間情報
		/// </summary>
		public ArcInterpolation Arc;

		/// <summary>
		/// 変換前のX座標
		/// </summary>
		public double OrgX;

		/// <summary>
		/// 変換前のY座標
		/// </summary>
		public double OrgY;

		/// <summary>
		/// 変換前のZ座標
		/// </summary>
		public double OrgZ;

		/// <summary>
		/// 移動しないかどうか
		/// </summary>
		public bool IsNoMove {
			get { return this.StartPos == this.EndPos; }
		}

		/// <summary>
		/// 現在の絶対／相対モードと指定コマンドによりポジションを更新する、コマンドのXYZは機械座標系に書き換わる
		/// </summary>
		/// <param name="state">状態</param>
		/// <param name="cmd">コマンド</param>
		static void MovePosition(State state, NcCodeCmd cmd) {
			if (state.AbsoluteMode) {
				// 絶対モードならコマンドの座標をそのまま現在座標に設定
	
				// G53モード中なら機械座標が指定されたものとするためオフセットは０になる
				var offset = state.G53Processing ? new Vector3d(0.0, 0.0, 0.0) : state.WorkO;

				if ((cmd.Flags & NcCodeFlags.X) != 0) {
					state.MachinePosition.X = cmd.X + offset.X;
					cmd.X = state.MachinePosition.X;
				}
				if ((cmd.Flags & NcCodeFlags.Y) != 0) {
					state.MachinePosition.Y = cmd.Y + offset.Y;
					cmd.Y = state.MachinePosition.Y;
				}
				if ((cmd.Flags & NcCodeFlags.Z) != 0) {
					state.MachinePosition.Z = cmd.Z + offset.Z;
					cmd.Z = state.MachinePosition.Z;
				}
			} else {
				// 相対モードなら現在座標に加算し、コマンド側を加算後の絶対座標で書き換える
				if ((cmd.Flags & NcCodeFlags.X) != 0) {
					state.MachinePosition.X += cmd.X;
					cmd.X = state.MachinePosition.X;
				}
				if ((cmd.Flags & NcCodeFlags.Y) != 0) {
					state.MachinePosition.Y += cmd.Y;
					cmd.Y = state.MachinePosition.Y;
				}
				if ((cmd.Flags & NcCodeFlags.Z) != 0) {
					state.MachinePosition.Z += cmd.Z;
					cmd.Z = state.MachinePosition.Z;
				}
			}

			// 移動したら解除
			state.G53Processing = false;
		}

		/// <summary>
		/// ３次元座標を指定平面座標に変換する
		/// </summary>
		/// <param name="plane">平面指定</param>
		/// <param name="position">３次元座標</param>
		/// <returns>２次元座標</returns>
		public static Vector2d TransformPlane(PlaneEnum plane, Vector3d position) {
			switch (plane) {
			case PlaneEnum.XY:
				return new Vector2d(position.X, position.Y);
			case PlaneEnum.ZX:
				return new Vector2d(position.Z, position.X);
			case PlaneEnum.YZ:
				return new Vector2d(position.Y, position.Z);
			default:
				throw new NotImplementedException();
			}
		}

		/// <summary>
		/// コマンドから動きをシミュレートし、移動所要時間、移動距離、円弧補間情報などセットする
		/// </summary>
		/// <param name="state">状態</param>
		/// <param name="cmd">コマンド</param>
		public static void Simulate(State state, NcCodeCmd cmd) {
			switch (cmd.CmdType) {
			case NcCodeCmdType.G00:
				// 位置決め
				{
					cmd.StartPos = state.MachinePosition;
					MovePosition(state, cmd);
					cmd.EndPos = state.MachinePosition;

					var move = cmd.EndPos - cmd.StartPos;
					var time = move.Abs() / state.MachineSpeed;
					cmd.XYTime = 60.0 * time.Max; // 全要素の中の最大値を取得
					cmd.XYLength = move.Length; // 移動距離
					cmd.Flags |= NcCodeFlags.StartPos | NcCodeFlags.EndPos | NcCodeFlags.XYTime | NcCodeFlags.XYLength;
					break;
				}

			case NcCodeCmdType.G01:
				// 直線補間
				{
					cmd.StartPos = state.MachinePosition;
					MovePosition(state, cmd);
					cmd.EndPos = state.MachinePosition;

					if ((cmd.Flags & NcCodeFlags.F) != 0)
						state.Speed = cmd.F;

					var move = cmd.EndPos - cmd.StartPos;
					var time = 60.0 * new Vector2d(move.X, move.Y).Length / state.Speed;
					cmd.XYTime = time; // 移動時間　※コントローラが２軸の補間しかできないらしいので平面の距離から時間計算している
					cmd.XYLength = move.Length; // 移動距離
					cmd.Flags |= NcCodeFlags.StartPos | NcCodeFlags.EndPos | NcCodeFlags.XYTime | NcCodeFlags.XYLength;
					break;
				}

			case NcCodeCmdType.G02:
			case NcCodeCmdType.G03:
				// 円弧補間
				{
					cmd.StartPos = state.MachinePosition;
					MovePosition(state, cmd);
					cmd.EndPos = state.MachinePosition;

					if ((cmd.Flags & NcCodeFlags.F) != 0)
						state.Speed = cmd.F;

					if (cmd.StartPos == cmd.EndPos) {
						// 始点と終点が完全に同じ、無視して良い命令
						cmd.Flags |= NcCodeFlags.StartPos | NcCodeFlags.EndPos | NcCodeFlags.XYTime | NcCodeFlags.XYLength;
						cmd.XYLength = 0.0; // 移動距離
						cmd.XYTime = 0.0; // 移動時間
						break;
					}

					var start2 = TransformPlane(state.Plane, cmd.StartPos);
					var end2 = TransformPlane(state.Plane, cmd.EndPos);
					if (start2 == end2) {
						var planeName = state.Plane.ToString();
						throw new WarningException((cmd.LineIndex + 1) + "行目: 円弧補間の" + planeName + "始点と終点が同じで他軸値だけ異なっています。");
					}

					var arc = new ArcInterpolation(start2, end2, cmd.R, cmd.CmdType == NcCodeCmdType.G02);
					if (arc.NoArcCenter)
						throw new WarningException((cmd.LineIndex + 1) + "行目: 円弧計算解なし");
					cmd.XYLength = Math.Abs(arc.Distance * arc.Gamma); // 移動距離
					cmd.XYTime = 60.0 * cmd.XYLength / state.Speed; // 移動時間
					cmd.Plane = state.Plane;
					cmd.Arc = arc;
					cmd.Flags |= NcCodeFlags.StartPos | NcCodeFlags.EndPos | NcCodeFlags.XYTime | NcCodeFlags.XYLength | NcCodeFlags.Arc;
					break;
				}

			case NcCodeCmdType.G17:
				// XY平面指定
				state.Plane = PlaneEnum.XY;
				break;

			case NcCodeCmdType.G18:
				// ZX平面指定
				state.Plane = PlaneEnum.ZX;
				break;

			case NcCodeCmdType.G19:
				// YZ平面指定
				state.Plane = PlaneEnum.YZ;
				break;

			case NcCodeCmdType.G53:
				// 機械座標系選択
				// ※NCMATEにあわせるためワンショット扱い、次の移動命令が来たら解除される
				state.G53Processing = true;
				break;

			case NcCodeCmdType.G90:
				// アブソリュート指令
				state.AbsoluteMode = true;
				break;

			case NcCodeCmdType.G91:
				// インクリメンタル指令
				state.AbsoluteMode = false;
				break;

			case NcCodeCmdType.G92:
				// ワーク座標設定
				state.WorkO = state.MachinePosition - new Vector3d(cmd.OrgX, cmd.OrgY, cmd.OrgZ);
				break;
			}
		}

		/// <summary>
		/// コマンドから動きをシミュレートし、移動所要時間、移動距離、円弧補間情報などセットする
		/// </summary>
		/// <param name="state">状態</param>
		/// <param name="cmds">情報設定先コマンド配列</param>
		public static void Simulate(State state, IEnumerable<NcCodeCmd> cmds) {
			foreach (var cmd in cmds) {
				Simulate(state, cmd);
			}
		}

		/// <summary>
		/// 現在位置を示す説明
		/// </summary>
		public string PositionDesc {
			get {
				return (this.LineIndex + 1).ToString() + "行目: ";
			}
		}

		public NcCodeCmd(int lineIndex) {
			this.LineIndex = lineIndex;
		}

		public override string ToString() {
			var sb = new StringBuilder();

			if ((this.Flags & NcCodeFlags.IsSkippable) != 0)
				sb.Append('/');

			sb.Append(Enum.GetName(typeof(NcCodeCmdType), this.CmdType));

			switch (this.CmdType) {
			case NcCodeCmdType.G00:
			case NcCodeCmdType.G01:
			case NcCodeCmdType.G02:
			case NcCodeCmdType.G03:
				if ((this.Flags & NcCodeFlags.X) != 0) sb.Append(" X" + this.OrgX);
				if ((this.Flags & NcCodeFlags.Y) != 0) sb.Append(" Y" + this.OrgY);
				if ((this.Flags & NcCodeFlags.Z) != 0) sb.Append(" Z" + this.OrgZ);
				if ((this.Flags & NcCodeFlags.R) != 0) sb.Append(" R" + this.R);
				if ((this.Flags & NcCodeFlags.F) != 0) sb.Append(" F" + this.F);
				break;

			case NcCodeCmdType.G04:
				sb.Append("P" + (int)Math.Ceiling(this.XYTime * 1000.0));
				break;

			case NcCodeCmdType.G92:
				if ((this.Flags & NcCodeFlags.X) != 0) sb.Append(" X" + this.OrgX);
				if ((this.Flags & NcCodeFlags.Y) != 0) sb.Append(" Y" + this.OrgY);
				if ((this.Flags & NcCodeFlags.Z) != 0) sb.Append(" Z" + this.OrgZ);
				break;

			case NcCodeCmdType.M98:
				if ((this.Flags & NcCodeFlags.P) != 0) sb.Append(" P" + this.P);
				if ((this.Flags & NcCodeFlags.L) != 0) sb.Append(" L" + this.L);
				break;

			case NcCodeCmdType.O:
				sb.Append(this.P);
				break;

			case NcCodeCmdType.S:
				sb.Append(this.S);
				break;
			}

			return sb.ToString();
		}

		/// <summary>
		/// 自分と指定コマンドの実行内容が同じかどうか調べる
		/// </summary>
		/// <param name="cmd">判定対象コマンド</param>
		/// <returns>true:同じ、false:異なる</returns>
		public bool CmdEquals(NcCodeCmd cmd) {
			if (this.CmdType != cmd.CmdType) return false;
			if (this.Flags != cmd.Flags) return false;
			if (this.X != cmd.X) return false;
			if (this.Y != cmd.Y) return false;
			if (this.Z != cmd.Z) return false;
			if (this.R != cmd.R) return false;
			if (this.F != cmd.F) return false;
			if (this.S != cmd.S) return false;
			if (this.P != cmd.P) return false;
			if (this.L != cmd.L) return false;
			return true;
		}

		/// <summary>
		/// 複製を作成する
		/// </summary>
		public NcCodeCmd Clone() {
			var c = this.MemberwiseClone() as NcCodeCmd;
			if (c.Arc != null)
				c.Arc = c.Arc.Clone();
			return c;
		}
	}

	/// <summary>
	/// NCコード字句解析での例外
	/// </summary>
	public class NcCodeTokenException : ApplicationException {
		public int LineIndex;
		public int Position;

		public NcCodeTokenException(string message, NcCodeScanner scanner) :
			base(scanner.CurrentPositionDesc + message) {
			this.LineIndex = scanner.LineIndex;
			this.Position = scanner.Position;
		}
	}

	/// <summary>
	/// NCコード構造解析での例外
	/// </summary>
	public class NcCodeParserException : ApplicationException {
		public int LineIndex;
		public int Position;

		public NcCodeParserException(string message, NcCodeParser parser) :
			base(parser.CurrentPositionDesc + message) {
			this.LineIndex = parser.LineIndex;
			this.Position = parser.Position;
		}

		public NcCodeParserException(string message, NcCodeCmd cmd) :
			base(cmd.PositionDesc + message) {
			this.LineIndex = cmd.LineIndex;
			this.Position = 0;
		}
	}
}
