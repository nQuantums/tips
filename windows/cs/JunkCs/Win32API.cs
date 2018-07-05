using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Jk
{
	/// <summary>
	///	Win32API アクセス用クラス
	/// </summary>
	public static class Win32API
	{
		public const int VK_LBUTTON = 0x01;
		public const int VK_RBUTTON = 0x02;
		public const int VK_CANCEL = 0x03;
		public const int VK_MBUTTON = 0x04;
		public const int VK_XBUTTON1 = 0x05;
		public const int VK_XBUTTON2 = 0x06;
		public const int VK_BACK = 0x08;
		public const int VK_TAB = 0x09;
		public const int VK_CLEAR = 0x0C;
		public const int VK_RETURN = 0x0D;
		public const int VK_SHIFT = 0x10;
		public const int VK_CONTROL = 0x11;
		public const int VK_MENU = 0x12;
		public const int VK_PAUSE = 0x13;
		public const int VK_CAPITAL = 0x14;
		public const int VK_KANA = 0x15;
		public const int VK_HANGEUL = 0x15;
		public const int VK_HANGUL = 0x15;
		public const int VK_JUNJA = 0x17;
		public const int VK_FINAL = 0x18;
		public const int VK_HANJA = 0x19;
		public const int VK_KANJI = 0x19;
		public const int VK_ESCAPE = 0x1B;
		public const int VK_CONVERT = 0x1C;
		public const int VK_NONCONVERT = 0x1D;
		public const int VK_ACCEPT = 0x1E;
		public const int VK_MODECHANGE = 0x1F;
		public const int VK_SPACE = 0x20;
		public const int VK_PRIOR = 0x21;
		public const int VK_NEXT = 0x22;
		public const int VK_END = 0x23;
		public const int VK_HOME = 0x24;
		public const int VK_LEFT = 0x25;
		public const int VK_UP = 0x26;
		public const int VK_RIGHT = 0x27;
		public const int VK_DOWN = 0x28;
		public const int VK_SELECT = 0x29;
		public const int VK_PRINT = 0x2A;
		public const int VK_EXECUTE = 0x2B;
		public const int VK_SNAPSHOT = 0x2C;
		public const int VK_INSERT = 0x2D;
		public const int VK_DELETE = 0x2E;
		public const int VK_HELP = 0x2F;
		public const int VK_LWIN = 0x5B;
		public const int VK_RWIN = 0x5C;
		public const int VK_APPS = 0x5D;
		public const int VK_SLEEP = 0x5F;
		public const int VK_NUMPAD0 = 0x60;
		public const int VK_NUMPAD1 = 0x61;
		public const int VK_NUMPAD2 = 0x62;
		public const int VK_NUMPAD3 = 0x63;
		public const int VK_NUMPAD4 = 0x64;
		public const int VK_NUMPAD5 = 0x65;
		public const int VK_NUMPAD6 = 0x66;
		public const int VK_NUMPAD7 = 0x67;
		public const int VK_NUMPAD8 = 0x68;
		public const int VK_NUMPAD9 = 0x69;
		public const int VK_MULTIPLY = 0x6A;
		public const int VK_ADD = 0x6B;
		public const int VK_SEPARATOR = 0x6C;
		public const int VK_SUBTRACT = 0x6D;
		public const int VK_DECIMAL = 0x6E;
		public const int VK_DIVIDE = 0x6F;
		public const int VK_F1 = 0x70;
		public const int VK_F2 = 0x71;
		public const int VK_F3 = 0x72;
		public const int VK_F4 = 0x73;
		public const int VK_F5 = 0x74;
		public const int VK_F6 = 0x75;
		public const int VK_F7 = 0x76;
		public const int VK_F8 = 0x77;
		public const int VK_F9 = 0x78;
		public const int VK_F10 = 0x79;
		public const int VK_F11 = 0x7A;
		public const int VK_F12 = 0x7B;
		public const int VK_F13 = 0x7C;
		public const int VK_F14 = 0x7D;
		public const int VK_F15 = 0x7E;
		public const int VK_F16 = 0x7F;
		public const int VK_F17 = 0x80;
		public const int VK_F18 = 0x81;
		public const int VK_F19 = 0x82;
		public const int VK_F20 = 0x83;
		public const int VK_F21 = 0x84;
		public const int VK_F22 = 0x85;
		public const int VK_F23 = 0x86;
		public const int VK_F24 = 0x87;
		public const int VK_NUMLOCK = 0x90;
		public const int VK_SCROLL = 0x91;
		public const int VK_OEM_NEC_EQUAL = 0x92;
		public const int VK_OEM_FJ_JISHO = 0x92;
		public const int VK_OEM_FJ_MASSHOU = 0x93;
		public const int VK_OEM_FJ_TOUROKU = 0x94;
		public const int VK_OEM_FJ_LOYA = 0x95;
		public const int VK_OEM_FJ_ROYA = 0x96;
		public const int VK_LSHIFT = 0xA0;
		public const int VK_RSHIFT = 0xA1;
		public const int VK_LCONTROL = 0xA2;
		public const int VK_RCONTROL = 0xA3;
		public const int VK_LMENU = 0xA4;
		public const int VK_RMENU = 0xA5;
		public const int VK_BROWSER_BACK = 0xA6;
		public const int VK_BROWSER_FORWARD = 0xA7;
		public const int VK_BROWSER_REFRESH = 0xA8;
		public const int VK_BROWSER_STOP = 0xA9;
		public const int VK_BROWSER_SEARCH = 0xAA;
		public const int VK_BROWSER_FAVORITES = 0xAB;
		public const int VK_BROWSER_HOME = 0xAC;
		public const int VK_VOLUME_MUTE = 0xAD;
		public const int VK_VOLUME_DOWN = 0xAE;
		public const int VK_VOLUME_UP = 0xAF;
		public const int VK_MEDIA_NEXT_TRACK = 0xB0;
		public const int VK_MEDIA_PREV_TRACK = 0xB1;
		public const int VK_MEDIA_STOP = 0xB2;
		public const int VK_MEDIA_PLAY_PAUSE = 0xB3;
		public const int VK_LAUNCH_MAIL = 0xB4;
		public const int VK_LAUNCH_MEDIA_SELECT = 0xB5;
		public const int VK_LAUNCH_APP1 = 0xB6;
		public const int VK_LAUNCH_APP2 = 0xB7;
		public const int VK_OEM_1 = 0xBA;
		public const int VK_OEM_PLUS = 0xBB;
		public const int VK_OEM_COMMA = 0xBC;
		public const int VK_OEM_MINUS = 0xBD;
		public const int VK_OEM_PERIOD = 0xBE;
		public const int VK_OEM_2 = 0xBF;
		public const int VK_OEM_3 = 0xC0;
		public const int VK_OEM_4 = 0xDB;
		public const int VK_OEM_5 = 0xDC;
		public const int VK_OEM_6 = 0xDD;
		public const int VK_OEM_7 = 0xDE;
		public const int VK_OEM_8 = 0xDF;
		public const int VK_OEM_AX = 0xE1;
		public const int VK_OEM_102 = 0xE2;
		public const int VK_ICO_HELP = 0xE3;
		public const int VK_ICO_00 = 0xE4;
		public const int VK_PROCESSKEY = 0xE5;
		public const int VK_ICO_CLEAR = 0xE6;
		public const int VK_PACKET = 0xE7;
		public const int VK_OEM_RESET = 0xE9;
		public const int VK_OEM_JUMP = 0xEA;
		public const int VK_OEM_PA1 = 0xEB;
		public const int VK_OEM_PA2 = 0xEC;
		public const int VK_OEM_PA3 = 0xED;
		public const int VK_OEM_WSCTRL = 0xEE;
		public const int VK_OEM_CUSEL = 0xEF;
		public const int VK_OEM_ATTN = 0xF0;
		public const int VK_OEM_FINISH = 0xF1;
		public const int VK_OEM_COPY = 0xF2;
		public const int VK_OEM_AUTO = 0xF3;
		public const int VK_OEM_ENLW = 0xF4;
		public const int VK_OEM_BACKTAB = 0xF5;
		public const int VK_ATTN = 0xF6;
		public const int VK_CRSEL = 0xF7;
		public const int VK_EXSEL = 0xF8;
		public const int VK_EREOF = 0xF9;
		public const int VK_PLAY = 0xFA;
		public const int VK_ZOOM = 0xFB;
		public const int VK_NONAME = 0xFC;
		public const int VK_PA1 = 0xFD;
		public const int VK_OEM_CLEAR = 0xFE;

		public const int WM_CREATE = 0x0001;
		public const int WM_USER = 0x0400;
		public const int WM_REFLECT = WM_USER + 0x1C00;
		public const int WM_ERASEBKGND = 0x0014;
		public const int WM_PAINT = 0x000F;
		public const int WM_CONTEXTMENU = 0x007B;
		public const int WM_NOTIFY = 0x004E;
		public const int WM_MOUSEHOVER = 0x02A1;
		public const int WM_MOUSELEAVE = 0x02A3;
		public const int WM_KEYDOWN = 0x0100;
		public const int WM_CHAR = 0x0102;
		public const int WM_KILLFOCUS = 0x0008;
		public const int WM_SETREDRAW = 0x000B;
		public const int WM_MOVE = 0x0003;

		public const int HDM_FIRST = 0x1200;
		public const int HDM_GETITEMRECT = (HDM_FIRST + 7);

		public const int WS_OVERLAPPED       = 0x0000000;
		public const int WS_POPUP            = 0x8000000;
		public const int WS_CHILD            = 0x4000000;
		public const int WS_MINIMIZE         = 0x2000000;
		public const int WS_VISIBLE          = 0x1000000;
		public const int WS_DISABLED         = 0x0800000;
		public const int WS_CLIPSIBLINGS     = 0x0400000;
		public const int WS_CLIPCHILDREN     = 0x0200000;
		public const int WS_MAXIMIZE         = 0x0100000;
		public const int WS_CAPTION          = 0x00C0000;
		public const int WS_BORDER           = 0x0080000;
		public const int WS_DLGFRAME         = 0x0040000;
		public const int WS_VSCROLL          = 0x0020000;
		public const int WS_HSCROLL          = 0x0010000;
		public const int WS_SYSMENU          = 0x0008000;
		public const int WS_THICKFRAME       = 0x0004000;
		public const int WS_GROUP            = 0x0002000;
		public const int WS_TABSTOP          = 0x0001000;
		public const int WS_MINIMIZEBOX      = 0x0002000;
		public const int WS_MAXIMIZEBOX      = 0x0001000;

		public const int WS_EX_DLGMODALFRAME     = 0x00000001;
		public const int WS_EX_NOPARENTNOTIFY    = 0x00000004;
		public const int WS_EX_TOPMOST           = 0x00000008;
		public const int WS_EX_ACCEPTFILES       = 0x00000010;
		public const int WS_EX_TRANSPARENT       = 0x00000020;
		public const int WS_EX_MDICHILD          = 0x00000040;
		public const int WS_EX_TOOLWINDOW        = 0x00000080;
		public const int WS_EX_WINDOWEDGE        = 0x00000100;
		public const int WS_EX_CLIENTEDGE        = 0x00000200;
		public const int WS_EX_CONTEXTHELP       = 0x00000400;
		public const int WS_EX_RIGHT             = 0x00001000;
		public const int WS_EX_LEFT              = 0x00000000;
		public const int WS_EX_RTLREADING        = 0x00002000;
		public const int WS_EX_LTRREADING        = 0x00000000;
		public const int WS_EX_LEFTSCROLLBAR     = 0x00004000;
		public const int WS_EX_RIGHTSCROLLBAR    = 0x00000000;
		public const int WS_EX_CONTROLPARENT     = 0x00010000;
		public const int WS_EX_STATICEDGE        = 0x00020000;
		public const int WS_EX_APPWINDOW         = 0x00040000;
		public const int WS_EX_OVERLAPPEDWINDOW  = (WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE);
		public const int WS_EX_PALETTEWINDOW     = (WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST);
		public const int WS_EX_LAYERED           = 0x00080000;
		public const int WS_EX_NOINHERITLAYOUT   = 0x00100000;
		public const int WS_EX_LAYOUTRTL         = 0x00400000;
		public const int WS_EX_COMPOSITED        = 0x02000000;
		public const int WS_EX_NOACTIVATE        = 0x08000000;

		public const int GWL_WNDPROC = -4;
		public const int GWL_HINSTANCE = -6;
		public const int GWL_HWNDPARENT = -8;
		public const int GWL_STYLE = -16;
		public const int GWL_EXSTYLE = -20;
		public const int GWL_USERDATA = -21;
		public const int GWL_ID = -12;

		[StructLayout(LayoutKind.Sequential)]
		public struct NMHDR
		{
			public IntPtr hwndFrom;
			public UInt32 idFrom;
			public UInt32 code;
		}

		/// <summary>
		/// LPARAM を作成する。
		/// </summary>
		public static IntPtr MakeLParam(int  wLow, int wHigh)
		{
			return (IntPtr)(((uint)wLow & 0xffff) | (((uint)wHigh & 0xffff) << 16));
		}

		#region HeaderCtrl 用メッセージ、定数、構造体
		public const uint HDN_FIRST = unchecked(0u - 300u);
		public const uint HDN_ITEMCHANGINGA       = (HDN_FIRST-0);
		public const uint HDN_ITEMCHANGINGW       = (HDN_FIRST-20);
		public const uint HDN_ITEMCHANGEDA        = (HDN_FIRST-1);
		public const uint HDN_ITEMCHANGEDW        = (HDN_FIRST-21);
		public const uint HDN_ITEMCLICKA          = (HDN_FIRST-2);
		public const uint HDN_ITEMCLICKW          = (HDN_FIRST-22);
		public const uint HDN_ITEMDBLCLICKA       = (HDN_FIRST-3);
		public const uint HDN_ITEMDBLCLICKW       = (HDN_FIRST-23);
		public const uint HDN_DIVIDERDBLCLICKA    = (HDN_FIRST-5);
		public const uint HDN_DIVIDERDBLCLICKW    = (HDN_FIRST-25);
		public const uint HDN_BEGINTRACKA         = (HDN_FIRST-6);
		public const uint HDN_BEGINTRACKW         = (HDN_FIRST-26);
		public const uint HDN_ENDTRACKA           = (HDN_FIRST-7);
		public const uint HDN_ENDTRACKW           = (HDN_FIRST-27);
		public const uint HDN_TRACKA              = (HDN_FIRST-8);
		public const uint HDN_TRACKW              = (HDN_FIRST-28);
		public const uint HDN_GETDISPINFOA        = (HDN_FIRST-9);
		public const uint HDN_GETDISPINFOW        = (HDN_FIRST-29);
		public const uint HDN_BEGINDRAG           = (HDN_FIRST-10);
		public const uint HDN_ENDDRAG             = (HDN_FIRST-11);
		public const uint HDN_FILTERCHANGE        = (HDN_FIRST-12);
		public const uint HDN_FILTERBTNCLICK      = (HDN_FIRST-13);

		[StructLayoutAttribute(LayoutKind.Sequential)]
		public unsafe struct NMHEADER_Unsafe
		{
			public NMHDR hdr;
			public int iItem;
			public int iButton;
			public HDITEM_Unsafe* pitem;
		}

		[StructLayoutAttribute(LayoutKind.Sequential)]
		public unsafe struct HDITEM_Unsafe
		{
			public uint mask;
			public int cxy;
			public char* pszText;
			public IntPtr hbm;
			public int cchTextMax;
			public int fmt;
			public IntPtr lParam;
			public int iImage;
			public int iOrder;
			public uint type;
			public void* pvFilter;
		}
		#endregion

		#region ListView 用メッセージ、定数
		public const int LVM_FIRST = 0x1000;
		public const int LVM_GETHEADER = (LVM_FIRST + 31);
		public const int LVM_HITTEST = (LVM_FIRST + 18);
		public const int LVM_SETIMAGELIST = (LVM_FIRST + 3);
		public const int LVM_GETIMAGELIST = (LVM_FIRST + 2);
		public const int LVM_SETITEMSTATE = (LVM_FIRST + 43);
		public const int LVM_GETITEMSTATE = (LVM_FIRST + 44);
		public const int LVM_SETITEMCOUNT = LVM_FIRST + 47;
		public const int LVM_GETITEMCOUNT = LVM_FIRST + 4;
		public const int LVM_INSERTCOLUMNW = (LVM_FIRST + 97);
		public const int LVM_DELETECOLUMN = (LVM_FIRST + 28);
		public const int LVM_GETCOLUMNW = (LVM_FIRST + 95);
		public const int LVM_SETCOLUMNW = (LVM_FIRST + 96);
		public const int LVM_SETCOLUMNORDERARRAY = (LVM_FIRST + 58);
		public const int LVM_GETCOLUMNORDERARRAY = (LVM_FIRST + 59);
		public const int LVM_SETCOLUMNWIDTH = (LVM_FIRST + 30);
		public const int LVM_GETCOLUMNWIDTH = (LVM_FIRST + 29);
		public const int LVM_INSERTITEMA = (LVM_FIRST + 7);
		public const int LVM_INSERTITEMW = (LVM_FIRST + 77);
        public const int LVM_GETITEMA = (LVM_FIRST + 5);
        public const int LVM_GETITEMW = (LVM_FIRST + 75);
		public const int LVM_SETEXTENDEDLISTVIEWSTYLE = (LVM_FIRST + 54);
		public const int LVM_GETEXTENDEDLISTVIEWSTYLE = (LVM_FIRST + 55);
		public const int LVM_SETSELECTIONMARK = (LVM_FIRST + 67);
		public const int LVM_GETSELECTIONMARK = (LVM_FIRST + 66);
		public const int LVM_ENSUREVISIBLE = (LVM_FIRST + 19);

		public const uint LVN_FIRST = unchecked(0u - 100u);
		public const uint LVN_ITEMCHANGING        = (LVN_FIRST-0);
		public const uint LVN_ITEMCHANGED         = (LVN_FIRST-1);
		public const uint LVN_INSERTITEM          = (LVN_FIRST-2);
		public const uint LVN_DELETEITEM          = (LVN_FIRST-3);
		public const uint LVN_DELETEALLITEMS      = (LVN_FIRST-4);
		public const uint LVN_BEGINLABELEDITA     = (LVN_FIRST-5);
		public const uint LVN_BEGINLABELEDITW     = (LVN_FIRST-75);
		public const uint LVN_ENDLABELEDITA       = (LVN_FIRST-6);
		public const uint LVN_ENDLABELEDITW       = (LVN_FIRST-76);
		public const uint LVN_COLUMNCLICK         = (LVN_FIRST-8);
		public const uint LVN_BEGINDRAG           = (LVN_FIRST-9);
		public const uint LVN_BEGINRDRAG          = (LVN_FIRST-11);
		public const uint LVN_ODCACHEHINT         = (LVN_FIRST-13);
		public const uint LVN_ODFINDITEMA         = (LVN_FIRST-52);
		public const uint LVN_ODFINDITEMW         = (LVN_FIRST-79);
		public const uint LVN_ITEMACTIVATE        = (LVN_FIRST-14);
		public const uint LVN_ODSTATECHANGED      = (LVN_FIRST-15);
		public const uint LVN_HOTTRACK            = (LVN_FIRST-21);
		public const uint LVN_GETDISPINFOA        = (LVN_FIRST-50);
		public const uint LVN_GETDISPINFOW        = (LVN_FIRST-77);
		public const uint LVN_SETDISPINFOA        = (LVN_FIRST-51);
		public const uint LVN_SETDISPINFOW        = (LVN_FIRST-78);
		
		public const int LVS_ICON                = 0x0000;
		public const int LVS_REPORT              = 0x0001;
		public const int LVS_SMALLICON           = 0x0002;
		public const int LVS_LIST                = 0x0003;
		public const int LVS_TYPEMASK            = 0x0003;
		public const int LVS_SINGLESEL           = 0x0004;
		public const int LVS_SHOWSELALWAYS       = 0x0008;
		public const int LVS_SORTASCENDING       = 0x0010;
		public const int LVS_SORTDESCENDING      = 0x0020;
		public const int LVS_SHAREIMAGELISTS     = 0x0040;
		public const int LVS_NOLABELWRAP         = 0x0080;
		public const int LVS_AUTOARRANGE         = 0x0100;
		public const int LVS_EDITLABELS          = 0x0200;
		public const int LVS_OWNERDATA           = 0x1000;
		public const int LVS_NOSCROLL            = 0x2000;
		public const int LVS_TYPESTYLEMASK       = 0xfc00;
		public const int LVS_ALIGNTOP            = 0x0000;
		public const int LVS_ALIGNLEFT           = 0x0800;
		public const int LVS_ALIGNMASK           = 0x0c00;
		public const int LVS_OWNERDRAWFIXED      = 0x0400;
		public const int LVS_NOCOLUMNHEADER      = 0x4000;
		public const int LVS_NOSORTHEADER        = 0x8000;

		public const int LVS_EX_GRIDLINES        = 0x00000001;
		public const int LVS_EX_SUBITEMIMAGES    = 0x00000002;
		public const int LVS_EX_CHECKBOXES       = 0x00000004;
		public const int LVS_EX_TRACKSELECT      = 0x00000008;
		public const int LVS_EX_HEADERDRAGDROP   = 0x00000010;
		public const int LVS_EX_FULLROWSELECT    = 0x00000020;
		public const int LVS_EX_ONECLICKACTIVATE = 0x00000040;
		public const int LVS_EX_TWOCLICKACTIVATE = 0x00000080;
		public const int LVS_EX_FLATSB           = 0x00000100;
		public const int LVS_EX_REGIONAL         = 0x00000200;
		public const int LVS_EX_INFOTIP          = 0x00000400;
		public const int LVS_EX_UNDERLINEHOT     = 0x00000800;
		public const int LVS_EX_UNDERLINECOLD    = 0x00001000;
		public const int LVS_EX_MULTIWORKAREAS   = 0x00002000;
		public const int LVS_EX_LABELTIP         = 0x00004000;
		public const int LVS_EX_BORDERSELECT     = 0x00008000;
		public const int LVS_EX_DOUBLEBUFFER     = 0x00010000;
		public const int LVS_EX_HIDELABELS       = 0x00020000;
		public const int LVS_EX_SINGLEROW        = 0x00040000;
		public const int LVS_EX_SNAPTOGRID       = 0x00080000;
		public const int LVS_EX_SIMPLESELECT     = 0x00100000;

		public const uint LVSIL_NORMAL = 0;
		public const uint LVSIL_SMALL = 1;
		public const uint LVSIL_STATE = 2;

		public const int LVIF_TEXT = 0x0001;
		public const int LVIF_IMAGE = 0x0002;
		public const int LVIF_PARAM = 0x0004;
		public const int LVIF_STATE = 0x0008;
		public const int LVIF_INDENT = 0x0010;
		public const int LVIF_NORECOMPUTE = 0x0800;
		public const int LVIF_GROUPID = 0x0100;
		public const int LVIF_COLUMNS = 0x0200;

		public const int LVIS_FOCUSED = 0x0001;
		public const int LVIS_SELECTED = 0x0002;
		public const int LVIS_CUT = 0x0004;
		public const int LVIS_DROPHILITED = 0x0008;
		public const int LVIS_GLOW = 0x0010;
		public const int LVIS_ACTIVATING = 0x0020;
		public const int LVIS_OVERLAYMASK = 0x0F00;
		public const int LVIS_STATEIMAGEMASK = 0xF000;

		public const int LVCF_FMT                = 0x0001;
		public const int LVCF_WIDTH              = 0x0002;
		public const int LVCF_TEXT               = 0x0004;
		public const int LVCF_SUBITEM            = 0x0008;
		public const int LVCF_IMAGE              = 0x0010;
		public const int LVCF_ORDER              = 0x0020;

		public const int LVCFMT_LEFT             = 0x0000;
		public const int LVCFMT_RIGHT            = 0x0001;
		public const int LVCFMT_CENTER           = 0x0002;
		public const int LVCFMT_JUSTIFYMASK      = 0x0003;
		public const int LVCFMT_IMAGE            = 0x0800;
		public const int LVCFMT_BITMAP_ON_RIGHT  = 0x1000;
		public const int LVCFMT_COL_HAS_IMAGES   = 0x8000;

		public const int LVSCW_AUTOSIZE           = -1;
		public const int LVSCW_AUTOSIZE_USEHEADER = -2;

		[DllImport("user32.dll", EntryPoint = "SendMessageW")]
		public static extern IntPtr SendMessage_LvHitTest(IntPtr hWnd, int Msg, IntPtr nIndex, ref LVHITTESTINFO pHitTestInfo);
		[DllImport("user32.dll", EntryPoint = "SendMessageW")]
		public static extern IntPtr SendMessage_LvItem(IntPtr hWnd, int Msg, IntPtr nIndex, ref LVITEMW pItem);
        [DllImport("user32.dll", EntryPoint = "SendMessageW")]
        public unsafe static extern IntPtr SendMessage_LvItemUnsafe(IntPtr hWnd, int Msg, IntPtr nIndex, LVITEMW_Unsafe* pItem);

		[DllImport("user32.dll", EntryPoint = "SendMessageW")]
		public static extern IntPtr SendMessage_LvColumn(IntPtr hWnd, int Msg, IntPtr nIndex, ref LVCOLUMNW pColumn);
		[DllImport("user32.dll", EntryPoint = "SendMessageW")]
		public static extern IntPtr SendMessage_LvSetColumn(IntPtr hWnd, int Msg, IntPtr nIndex, ref LVCOLUMNW_Set pColumn);
		[DllImport("user32.dll", EntryPoint = "SendMessageW")]
		public unsafe static extern IntPtr SendMessage_LvColumnUnsafe(IntPtr hWnd, int Msg, IntPtr nIndex, LVCOLUMNW_Unsafe* pColumn);
		
		[DllImport("user32.dll", EntryPoint = "SendMessageW")]
		public unsafe static extern IntPtr SendMessage_LvSetColOrderArray(IntPtr hWnd, int Msg, IntPtr nCount, int* pArray);
		[DllImport("user32.dll", EntryPoint = "SendMessageW")]
		public unsafe static extern IntPtr SendMessage_LvGetColOrderArray(IntPtr hWnd, int Msg, IntPtr nCount, int* pArray);
		#endregion

		[DllImport("user32.dll", EntryPoint = "SendMessageW")]
		public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
		[DllImport("user32.dll", EntryPoint = "SendMessageW")]
		public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr nIndex, out RECT lpRect);
		[DllImport("user32.dll")]
		public static extern int PostMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
		[DllImport("user32.dll")]
		public static extern Int32 GetClientRect(IntPtr hWnd, out RECT lpRect);
		[DllImport("user32.dll")]
		public static extern Int32 GetWindowRect(IntPtr hWnd, out RECT lpRect);
		[DllImport("user32.dll")]
		public static extern Int32 ScreenToClient(IntPtr hWnd, ref POINT lpPoint);
		[DllImport("user32.dll")]
		public static extern Int32 ClientToScreen(IntPtr hWnd, ref POINT lpPoint);
		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		public static extern int RegisterWindowMessageW(string lpString);
		[DllImport("user32.dll")]
		public static extern int GetWindowLongW(IntPtr hWnd, int nIndex);
		[DllImport("user32.dll")]
		public static extern int SetWindowLongW(IntPtr hWnd, int nIndex, int dwNewLong);
		[DllImport("User32.dll")]
		public extern static IntPtr WindowFromPoint(POINT Point);
		[DllImport("User32.dll")]
		public extern static Int16 GetKeyState(int nVirtKey);

		#region デバイスコンテキスト関係関数インポート
		[DllImport("user32.dll")]
		public static extern IntPtr GetDC(IntPtr hWnd);
		[DllImport("user32.dll")]
		public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
		[DllImport("Gdi32.dll")]
		public static extern int SetROP2(IntPtr hDC, int fnDrawMode);
		[DllImport("Gdi32.dll")]
		public static extern int GetROP2(IntPtr hDC);
		[DllImport("Gdi32.dll")]
		public static extern int SaveDC(IntPtr hDC);
		[DllImport("Gdi32.dll")]
		public static extern int RestoreDC(IntPtr hDC, int nSavedDC);
		[DllImport("Gdi32.dll")]
		public static extern int DeleteObject(IntPtr hObject);
		[DllImport("Gdi32.dll")]
		public static extern IntPtr CreatePen(int fnPenStyle, int nWidth, int crColor);
		[DllImport("Gdi32.dll")]
		public static extern IntPtr CreateBrushIndirect(ref LOGBRUSH lplb);
		[DllImport("Gdi32.dll")]
		public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObj);
		[DllImport("Gdi32.dll")]
		public static extern int MoveToEx(IntPtr hDC, int X, int Y, IntPtr lpPoint);
		[DllImport("Gdi32.dll")]
		public static extern int LineTo(IntPtr hDC, int nXEnd, int nYEnd);
		[DllImport("User32.dll")]
		public static extern int FillRect(IntPtr hDC, ref RECT lprc, IntPtr hbr);
		[DllImport("Gdi32.dll")]
		public static extern int Rectangle(IntPtr hdc, int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);
		[DllImport("Gdi32.dll")]
		public static extern int IntersectClipRect(IntPtr hDC, int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);
		[DllImport("Gdi32.dll")]
		public static extern int BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, UInt32 dwRop);
		[DllImport("Gdi32.dll")]
		public static extern IntPtr CreateDCW(string lpszDriver, string lpszDevice, string lpszOutput, IntPtr lpInitData);
		[DllImport("Gdi32.dll")]
		public static extern IntPtr CreateCompatibleDC(IntPtr hdc);
		[DllImport("Gdi32.dll")]
		public static extern int DeleteDC(IntPtr hdc);
		#endregion

		[StructLayout(LayoutKind.Sequential)]
		public struct POINT
		{
			public int x;
			public int y;

			public POINT(int val)
			{
				this.x = val & 0xffff;
				this.y = (val >> 16) & 0xffff;
			}

			public POINT(int x, int y)
			{
				this.x = x;
				this.y = y;
			}

			public void FromPackedInt(int val)
			{
				this.x = val & 0xffff;
				this.y = (val >> 16) & 0xffff;
			}
		}

		/// <summary>
		/// 矩形
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct RECT
		{
			public int left;
			public int top;
			public int right;
			public int bottom;

			public RECT(int l, int t, int r, int b)
			{
				this.left = l;
				this.top = t;
				this.right = r;
				this.bottom = b;
			}

			public bool PtInRect(POINT pt)
			{
				return this.left <= pt.x && this.top <= pt.y && pt.x < this.right && pt.y < this.bottom;
			}

			/// <summary>
			/// 幅取得
			/// </summary>
			public int Width
			{
				get { return this.right - this.left; }
			}

			/// <summary>
			/// 高さ取得
			/// </summary>
			public int Height
			{
				get { return this.bottom - this.top; }
			}

			/// <summary>
			/// 矩形の左上のポイント
			/// </summary>
			public POINT LeftTop
			{
				get { return new POINT(left, top); }
				set
				{
					this.left = value.x;
					this.top = value.y;
				}
			}

			/// <summary>
			/// 矩形の右下のポイント
			/// </summary>
			public POINT RightBottom
			{
				get { return new POINT(right, bottom); }
				set
				{
					this.right = value.x;
					this.bottom = value.y;
				}
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct LOGBRUSH
		{
			public uint lbStyle;
			public int lbColor;
			public int lbHatch;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct LVHITTESTINFO
		{
			public POINT pt;
			public UInt32 flags;
			public int iItem;
			public int iSubItem;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct NMLISTVIEW
		{
			public NMHDR   hdr;
			public int     iItem;
			public int     iSubItem;
			public UInt32  uNewState;
			public UInt32  uOldState;
			public UInt32  uChanged;
			public POINT   ptAction;
			public IntPtr  lParam;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct LVITEMW
		{
			public UInt32 mask;
			public int iItem;
			public int iSubItem;
			public UInt32 state;
			public UInt32 stateMask;
			[MarshalAs(UnmanagedType.LPWStr)]
			public string pszText;
			public int cchTextMax;
			public int iImage;
			public IntPtr lParam;
			public int iIndent;
			public int iGroupId;
			public UInt32 cColumns;
			public IntPtr puColumns;
		}

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct LVITEMW_Unsafe
        {
            public UInt32 mask;
            public int iItem;
            public int iSubItem;
            public UInt32 state;
            public UInt32 stateMask;
            public char* pszText;
            public int cchTextMax;
            public int iImage;
            public IntPtr lParam;
            public int iIndent;
            public int iGroupId;
            public UInt32 cColumns;
            public IntPtr puColumns;
        }

        [StructLayout(LayoutKind.Sequential)]
		public struct NMLVODSTATECHANGE
		{
			public NMHDR hdr;
			public int iFrom;
			public int iTo;
			public UInt32 uNewState;
			public UInt32 uOldState;
		}

		[StructLayoutAttribute(LayoutKind.Sequential)]
		public unsafe struct NMLVDISPINFOW
		{
			public IntPtr hwndFrom;
			public UInt32 idFrom;
			public UInt32 code;
			public UInt32 mask;
			public Int32 iItem;
			public Int32 iSubItem;
			public UInt32 state;
			public UInt32 stateMask;
			public char* pszText;
			public Int32 cchTextMax;
			public Int32 iImage;
			public Int32 lParam;
		}

		[StructLayoutAttribute(LayoutKind.Sequential)]
		public struct LVCOLUMNW
		{
			public UInt32 mask;
			public int fmt;
			public int cx;
			public IntPtr pszText;
			public int cchTextMax;
			public int iSubItem;
			public int iImage;
			public int iOrder;
		}

		[StructLayoutAttribute(LayoutKind.Sequential)]
		public struct LVCOLUMNW_Set
		{
			public UInt32 mask;
			public int fmt;
			public int cx;
			[MarshalAs(UnmanagedType.LPWStr)]
			public string pszText;
			public int cchTextMax;
			public int iSubItem;
			public int iImage;
			public int iOrder;
		}

		[StructLayoutAttribute(LayoutKind.Sequential)]
		public unsafe struct LVCOLUMNW_Unsafe
		{
			public UInt32 mask;
			public int fmt;
			public int cx;
			public unsafe char* pszText;
			public int cchTextMax;
			public int iSubItem;
			public int iImage;
			public int iOrder;
		}
	}
}
