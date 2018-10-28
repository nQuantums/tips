using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FileChangeWatcher {
	[DataContract]
	public class ConsoleStyle {
		static DataContractJsonSerializer JsonSerializer;

		static ConsoleStyle() {
			JsonSerializer = new DataContractJsonSerializer(typeof(ConsoleStyle));
		}

		[StructLayout(LayoutKind.Sequential)]
		struct COORD {
			public short X;
			public short Y;
		}
		[StructLayout(LayoutKind.Sequential)]
		struct SMALL_RECT {
			public short Left;
			public short Top;
			public short Right;
			public short Bottom;
		}
		[StructLayout(LayoutKind.Sequential)]
		struct COLORREF {
			[DataMember]
			public uint ColorDWORD;

			public COLORREF(Color color) {
				ColorDWORD = (uint)color.R + (((uint)color.G) << 8) + (((uint)color.B) << 16);
			}

			public COLORREF(uint r, uint g, uint b) {
				ColorDWORD = r + (g << 8) + (b << 16);
			}

			public Color GetColor() {
				return Color.FromArgb((int)(0x000000FFU & ColorDWORD),
				   (int)(0x0000FF00U & ColorDWORD) >> 8, (int)(0x00FF0000U & ColorDWORD) >> 16);
			}

			public void SetColor(uint r, uint g, uint b) {
				ColorDWORD = r | ((g) << 8) | ((b) << 16);
			}

			public void SetColor(Color color) {
				ColorDWORD = (uint)color.R + (((uint)color.G) << 8) + (((uint)color.B) << 16);
			}
		}
		[StructLayout(LayoutKind.Sequential)]
		struct CONSOLE_SCREEN_BUFFER_INFO_EX {
			public int cbSize;
			public COORD dwSize;
			public COORD dwCursorPosition;
			public ushort wAttributes;
			public SMALL_RECT srWindow;
			public COORD dwMaximumWindowSize;
			public ushort wPopupAttributes;
			public bool bFullscreenSupported;
			public COLORREF black;
			public COLORREF darkBlue;
			public COLORREF darkGreen;
			public COLORREF darkCyan;
			public COLORREF darkRed;
			public COLORREF darkMagenta;
			public COLORREF darkYellow;
			public COLORREF gray;
			public COLORREF darkGray;
			public COLORREF blue;
			public COLORREF green;
			public COLORREF cyan;
			public COLORREF red;
			public COLORREF magenta;
			public COLORREF yellow;
			public COLORREF white;

			public void SetColors(ConsoleColors colors) {
				this.black.SetColor(ColorTranslator.FromHtml(colors.black));
				this.darkBlue.SetColor(ColorTranslator.FromHtml(colors.darkBlue));
				this.darkGreen.SetColor(ColorTranslator.FromHtml(colors.darkGreen));
				this.darkCyan.SetColor(ColorTranslator.FromHtml(colors.darkCyan));
				this.darkRed.SetColor(ColorTranslator.FromHtml(colors.darkRed));
				this.darkMagenta.SetColor(ColorTranslator.FromHtml(colors.darkMagenta));
				this.darkYellow.SetColor(ColorTranslator.FromHtml(colors.darkYellow));
				this.gray.SetColor(ColorTranslator.FromHtml(colors.gray));
				this.darkGray.SetColor(ColorTranslator.FromHtml(colors.darkGray));
				this.blue.SetColor(ColorTranslator.FromHtml(colors.blue));
				this.green.SetColor(ColorTranslator.FromHtml(colors.green));
				this.cyan.SetColor(ColorTranslator.FromHtml(colors.cyan));
				this.red.SetColor(ColorTranslator.FromHtml(colors.red));
				this.magenta.SetColor(ColorTranslator.FromHtml(colors.magenta));
				this.yellow.SetColor(ColorTranslator.FromHtml(colors.yellow));
				this.white.SetColor(ColorTranslator.FromHtml(colors.white));
			}

			public ConsoleColors GetColors() {
				var colors = new ConsoleColors();
				colors.black = ColorTranslator.ToHtml(this.black.GetColor());
				colors.darkBlue = ColorTranslator.ToHtml(this.darkBlue.GetColor());
				colors.darkGreen = ColorTranslator.ToHtml(this.darkGreen.GetColor());
				colors.darkCyan = ColorTranslator.ToHtml(this.darkCyan.GetColor());
				colors.darkRed = ColorTranslator.ToHtml(this.darkRed.GetColor());
				colors.darkMagenta = ColorTranslator.ToHtml(this.darkMagenta.GetColor());
				colors.darkYellow = ColorTranslator.ToHtml(this.darkYellow.GetColor());
				colors.gray = ColorTranslator.ToHtml(this.gray.GetColor());
				colors.darkGray = ColorTranslator.ToHtml(this.darkGray.GetColor());
				colors.blue = ColorTranslator.ToHtml(this.blue.GetColor());
				colors.green = ColorTranslator.ToHtml(this.green.GetColor());
				colors.cyan = ColorTranslator.ToHtml(this.cyan.GetColor());
				colors.red = ColorTranslator.ToHtml(this.red.GetColor());
				colors.magenta = ColorTranslator.ToHtml(this.magenta.GetColor());
				colors.yellow = ColorTranslator.ToHtml(this.yellow.GetColor());
				colors.white = ColorTranslator.ToHtml(this.white.GetColor());
				return colors;
			}
		}

		const int STD_OUTPUT_HANDLE = -11;                                        // per WinBase.h
		internal static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);    // per WinBase.h

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr GetStdHandle(int nStdHandle);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool GetConsoleScreenBufferInfoEx(IntPtr hConsoleOutput, ref CONSOLE_SCREEN_BUFFER_INFO_EX csbe);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool SetConsoleScreenBufferInfoEx(IntPtr hConsoleOutput, ref CONSOLE_SCREEN_BUFFER_INFO_EX csbe);

		[DataContract]
		public class ConsoleColors {
			[DataMember] public string black;
			[DataMember] public string darkBlue;
			[DataMember] public string darkGreen;
			[DataMember] public string darkCyan;
			[DataMember] public string darkRed;
			[DataMember] public string darkMagenta;
			[DataMember] public string darkYellow;
			[DataMember] public string gray;
			[DataMember] public string darkGray;
			[DataMember] public string blue;
			[DataMember] public string green;
			[DataMember] public string cyan;
			[DataMember] public string red;
			[DataMember] public string magenta;
			[DataMember] public string yellow;
			[DataMember] public string white;
		}

		[DataContract]
		public class Highlight {
			[DataMember]
			public string name;
			[DataMember]
			public string pattern;
			[DataMember]
			public string fore;
            [DataMember]
            public string back;

            public ConsoleColor ForeColor;
            public ConsoleColor BackColor;

			public Highlight(string name, string format, string fore, string back) {
				this.name = name;
				this.pattern = format;
                this.fore = fore;
                this.back = back;
			}

			[OnDeserialized]
			void OnDeserialized(StreamingContext context) {
				switch (this.fore) {
				case "black":
					this.ForeColor = ConsoleColor.Black;
					break;
				case "darkBlue":
					this.ForeColor = ConsoleColor.DarkBlue;
					break;
				case "darkGreen":
					this.ForeColor = ConsoleColor.DarkGreen;
					break;
				case "darkCyan":
					this.ForeColor = ConsoleColor.DarkCyan;
					break;
				case "darkRed":
					this.ForeColor = ConsoleColor.DarkRed;
					break;
				case "darkMagenta":
					this.ForeColor = ConsoleColor.DarkMagenta;
					break;
				case "darkYellow":
					this.ForeColor = ConsoleColor.DarkYellow;
					break;
				case "gray":
					this.ForeColor = ConsoleColor.Gray;
					break;
				case "darkGray":
					this.ForeColor = ConsoleColor.DarkGray;
					break;
				case "blue":
					this.ForeColor = ConsoleColor.Blue;
					break;
				case "green":
					this.ForeColor = ConsoleColor.Green;
					break;
				case "cyan":
					this.ForeColor = ConsoleColor.Cyan;
					break;
				case "red":
					this.ForeColor = ConsoleColor.Red;
					break;
				case "magenta":
					this.ForeColor = ConsoleColor.Magenta;
					break;
				case "yellow":
					this.ForeColor = ConsoleColor.Yellow;
					break;
				case "white":
					this.ForeColor = ConsoleColor.White;
					break;
				default:
					this.ForeColor = ConsoleColor.White;
					break;
				}

                switch (this.back) {
                case "black":
                    this.BackColor = ConsoleColor.Black;
                    break;
                case "darkBlue":
                    this.BackColor = ConsoleColor.DarkBlue;
                    break;
                case "darkGreen":
                    this.BackColor = ConsoleColor.DarkGreen;
                    break;
                case "darkCyan":
                    this.BackColor = ConsoleColor.DarkCyan;
                    break;
                case "darkRed":
                    this.BackColor = ConsoleColor.DarkRed;
                    break;
                case "darkMagenta":
                    this.BackColor = ConsoleColor.DarkMagenta;
                    break;
                case "darkYellow":
                    this.BackColor = ConsoleColor.DarkYellow;
                    break;
                case "gray":
                    this.BackColor = ConsoleColor.Gray;
                    break;
                case "darkGray":
                    this.BackColor = ConsoleColor.DarkGray;
                    break;
                case "blue":
                    this.BackColor = ConsoleColor.Blue;
                    break;
                case "green":
                    this.BackColor = ConsoleColor.Green;
                    break;
                case "cyan":
                    this.BackColor = ConsoleColor.Cyan;
                    break;
                case "red":
                    this.BackColor = ConsoleColor.Red;
                    break;
                case "magenta":
                    this.BackColor = ConsoleColor.Magenta;
                    break;
                case "yellow":
                    this.BackColor = ConsoleColor.Yellow;
                    break;
                case "white":
                    this.BackColor = ConsoleColor.White;
                    break;
                default:
                    this.BackColor = ConsoleColor.Black;
                    break;
                }
            }
        }

		[DataMember]
		public ConsoleColors colors;

		[DataMember]
		public Highlight[] highlights;

		public void Apply() {
			var sbi = GetScreenBufferInfoEx();
			sbi.SetColors(this.colors);
			SetScreenBufferInfoEx(sbi);
		}

		public void SaveToJson(string fileName) {
			using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)) {
				JsonSerializer.WriteObject(fs, this);
			}
		}

		public static ConsoleStyle FromJsonFile(string fileName) {
			using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
				return (ConsoleStyle)JsonSerializer.ReadObject(fs);
			}
		}

		static IntPtr GetOutputHandle() {
			var result = GetStdHandle(STD_OUTPUT_HANDLE);
			if (result == INVALID_HANDLE_VALUE) {
				throw new Win32Exception();
			}
			return result;
		}

		static CONSOLE_SCREEN_BUFFER_INFO_EX GetScreenBufferInfoEx() {
			var result = new CONSOLE_SCREEN_BUFFER_INFO_EX();
			result.cbSize = (int)Marshal.SizeOf(result);
			if (!GetConsoleScreenBufferInfoEx(GetOutputHandle(), ref result)) {
				throw new Win32Exception();
			}
			return result;
		}

		static void SetScreenBufferInfoEx(CONSOLE_SCREEN_BUFFER_INFO_EX value) {
			++value.srWindow.Bottom;
			++value.srWindow.Right;
			SetConsoleScreenBufferInfoEx(GetOutputHandle(), ref value);
		}
	}
}
