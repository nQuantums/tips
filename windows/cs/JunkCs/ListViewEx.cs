using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using System.Reflection;

namespace Jk
{
	/// <summary>
	///	列の表示非表示、状態の保存、実データによるソート機能を実装したリストビュークラス。
	/// </summary>
	public partial class ListViewEx : Control
	{
		#region クラス宣言
		/// <summary>
		///	ソート用アイテム比較処理実行クラス。
		/// </summary>
		public class ItemComparer : System.Collections.Generic.IComparer<ListViewItemEx>
		{
			#region フィールド
			private ColumnHeaderEx m_Column;
			private SortOrder m_Order;
			#endregion

			#region コンストラクタ
			/// <summary>
			/// コンストラクタ。
			/// </summary>
			public ItemComparer()
			{
				m_Order = SortOrder.Ascending;
			}

			/// <summary>
			/// コンストラクタ。
			/// </summary>
			public ItemComparer(ColumnHeaderEx column, SortOrder order)
			{
				m_Column = column;
				m_Order = order;
			}
			#endregion

			#region プロパティ
			/// <summary>
			///	ソート対象のカラムの取得と設定。
			/// </summary>
			public ColumnHeaderEx Column
			{
				get { return m_Column; }
				set { m_Column = value; }
			}

			/// <summary>
			///	ソート順序。
			/// </summary>
			public virtual SortOrder Order
			{
				get { return m_Order; }
				set { m_Order = value; }
			}
			#endregion

			#region メソッド
			/// <summary>
			/// 比較する。
			/// </summary>
			/// <param name="item1">アイテム１．</param>
			/// <param name="item2">アイテム２．</param>
			/// <returns>item1＜item2:-1、item1==item2:0、item1＞item2:1。</returns>
			public virtual int Compare(ListViewItemEx item1, ListViewItemEx item2)
			{
				bool ascending = this.Order == SortOrder.Ascending;

				//	どちらかのアイテムが null の場合は nullの方 < nullじゃない方 になるようにする
				if (item1 == null || item2 == null)
				{
					if (item1 == null && item2 == null)
						return 0;
					else if (item1 == null)
						return ascending ? -1 : 1;
					else
						return ascending ? 1 : -1;
				}

				//	アイテムの実データを取得して比較する
				IComparable value1 = m_Column.GetItemValue(item1) as IComparable;
				object value2 = m_Column.GetItemValue(item2);

				if (value1 == null || value2 == null)
				{
					if (value1 == null && value2 == null)
						return 0;
					else if (value1 == null)
						return ascending ? -1 : 1;
					else
						return ascending ? 1 : -1;
				}

				int cmp = value1.CompareTo(value2);
				return ascending ? cmp : -cmp;
			}
			#endregion
		}

		/// <summary>
		/// 状態保存用クラス。
		/// </summary>
		[XmlType("Ore.ListViewEx.SettingInfo")]
		public class SettingInfo
		{
			#region クラス宣言
			/// <summary>
			/// カラム状態保存用クラス。
			/// </summary>
			[XmlType("Ore.ListViewEx.SettingInfo.ColInfo")]
			public class ColInfo
			{
				#region フィールド
				private string m_Text = null;
				private HorizontalAlignment m_TextAlign = HorizontalAlignment.Left;
				private string m_ItemTextMember = null;
				private string m_ItemValueMember = null;
				private int m_DataColIndex = -1;
				private int m_Visible = -1;
				private int m_DisplayIndex = -1;
				private int m_Width = -1;
				#endregion

				#region コンストラクタ
				/// <summary>
				/// コンストラクタ。
				/// </summary>
				public ColInfo()
				{
				}

				/// <summary>
				/// コンストラクタ。
				/// </summary>
				public ColInfo(string text, HorizontalAlignment textAlign)
				{
					m_Text = text;
					m_TextAlign = textAlign;
					m_Visible = 1;
					m_Width = 100;
				}

				/// <summary>
				/// コンストラクタ。
				/// </summary>
				public ColInfo(string text, HorizontalAlignment textAlign, int dataColIndex)
				{
					m_Text = text;
					m_TextAlign = textAlign;
					m_DataColIndex = dataColIndex;
					m_Visible = 1;
					m_Width = 100;
				}

				/// <summary>
				/// コンストラクタ。
				/// </summary>
				public ColInfo(string text, HorizontalAlignment textAlign, string itemTextMember, string itemValueMember)
				{
					m_Text = text;
					m_TextAlign = textAlign;
					m_ItemTextMember = itemTextMember;
					m_ItemValueMember = itemValueMember;
					m_Visible = 1;
					m_Width = 100;
				}
				#endregion

				#region プロパティ
				/// <summary>
				///	カラムのテキストの取得と設定。
				/// </summary>
				[System.Xml.Serialization.XmlIgnoreAttribute]
				public string Text
				{
					get { return m_Text; }
					set { m_Text = value; }
				}

				/// <summary>
				///	カラムの文字寄せの取得と設定。
				/// </summary>
				[System.Xml.Serialization.XmlIgnoreAttribute]
				public HorizontalAlignment TextAlign
				{
					get { return m_TextAlign; }
					set { m_TextAlign = value; }
				}


				/// <summary>
				///	ColumnHeaderEx の ItemTextMember に相当するプロパティ。
				/// </summary>
				[System.Xml.Serialization.XmlIgnoreAttribute]
				public string ItemTextMember
				{
					get { return m_ItemTextMember; }
					set { m_ItemTextMember = value; }
				}

				/// <summary>
				///	ColumnHeaderEx の ItemValueMember に相当するプロパティ。
				/// </summary>
				[System.Xml.Serialization.XmlIgnoreAttribute]
				public string ItemValueMember
				{
					get { return m_ItemValueMember; }
					set { m_ItemValueMember = value; }
				}

				/// <summary>
				/// このカラムで表示するデータインデックス番号の取得と設定。
				/// </summary>
				[System.Xml.Serialization.XmlIgnoreAttribute]
				public int DataColIndex
				{
					get { return m_DataColIndex; }
					set { m_DataColIndex = value; }
				}

				/// <summary>
				///	カラムの表示位置の取得と設定。
				/// </summary>
				public int DisplayIndex
				{
					get { return m_DisplayIndex; }
					set { m_DisplayIndex = value; }
				}

				/// <summary>
				///	カラムの幅の取得と設定。
				/// </summary>
				public int Width
				{
					get { return m_Width; }
					set { m_Width = value; }
				}

				/// <summary>
				///	カラムの可視状態の取得と設定。
				/// </summary>
				public bool Visible
				{
					get
					{
						if (m_Visible < 0)
							return true;
						return m_Visible != 0 ? true : false;
					}
					set
					{
						m_Visible = value ? 1 : 0;
					}
				}
				#endregion

				#region メソッド
				/// <summary>
				///	指定されたオブジェクトでこのオブジェクトを初期化する。
				/// 初期値が入っている変数は指定された info の値で上書きされる。
				/// </summary>
				/// <param name="info">初期化用データ。</param>
				public virtual void Initialize(ColInfo info)
				{
					m_Text = info.m_Text; // 表示文字列は絶対設定
					m_TextAlign = info.m_TextAlign; // 文字寄せも絶対設定
					m_ItemTextMember = info.m_ItemTextMember; // 表示名称メンバ名も絶対設定
					m_ItemValueMember = info.m_ItemValueMember; // データ値メンバ名も絶対設定
					m_DataColIndex = info.m_DataColIndex; // 表示データインデックスも絶対設定
					if (m_DisplayIndex < 0) m_DisplayIndex = info.m_DisplayIndex;
					if (m_Width <= 0) m_Width = info.m_Width;
					if (m_Visible < 0) m_Visible = info.m_Visible;
				}
				#endregion
			}
			#endregion

			#region フィールド
			private ColInfo[] m_Cols;
			#endregion

			#region コンストラクタ
			/// <summary>
			/// コンストラクタ。
			/// </summary>
			public SettingInfo()
			{
			}
			#endregion

			#region プロパティ
			/// <summary>
			///	カラム設定情報配列の取得と設定。
			/// </summary>
			public ColInfo[] Cols
			{
				get { return m_Cols; }
				set { m_Cols = value; }
			}
			#endregion

			#region メソッド
			/// <summary>
			///	指定されたオブジェクトでこのオブジェクトを初期化する。初期値が入っている変数は指定された info の値で上書きされる。
			/// </summary>
			/// <param name="info">初期化用データ。</param>
			public void Initialize(SettingInfo info)
			{
				if (info.Cols != null)
				{
					//	列データ数は絶対 info の方に合わせる
					int n = info.Cols.Length;
					if (m_Cols == null)
						m_Cols = new ColInfo[n];
					else if (m_Cols.Length != n)
						System.Array.Resize(ref m_Cols, n);
					for (int i = 0; i < n; i++)
					{
						if (m_Cols[i] == null)
							m_Cols[i] = new ColInfo();

						ColInfo c = info.Cols[i];
						if (c != null)
						{
							m_Cols[i].Initialize(c);
						}
					}
				}
			}

			/// <summary>
			/// 指定されたリストビューへ設定を適用する。
			/// </summary>
			/// <param name="listView">設定を適用するリストビュー。</param>
			public virtual void Set(ListViewEx listView)
			{
				listView.Columns.Clear();

				if (m_Cols == null || m_Cols.Length == 0)
					return;

				int nCols = m_Cols.Length;

				//	まずカラムを追加
				for (int i = 0; i < nCols; i++)
				{
					ColInfo info = m_Cols[i];
					ColumnHeaderEx colh = new ColumnHeaderEx();
					colh.Text = info.Text;
					colh.TextAlign = info.TextAlign;
					colh.ItemTextMember = info.ItemTextMember;
					colh.ItemValueMember = info.ItemValueMember;
					colh.DataColIndex = 0 <= info.DataColIndex ? info.DataColIndex : i;
					colh.Width = 0 < info.Width ? info.Width : 100;
					colh.Visible = info.Visible;

					listView.Columns.Add(colh);
				}

				//	追加されたカラムを調整
				for (int i = 0; i < nCols; i++)
				{
					ColInfo info = m_Cols[i];
					ColumnHeaderEx colh = listView.Columns[i];
					if (0 <= info.DisplayIndex)
						colh.DisplayIndex = info.DisplayIndex;
				}
			}

			/// <summary>
			/// 指定されたリストビューから設定を取得する。
			/// </summary>
			/// <param name="listView">設定を取得するリストビュー。</param>
			public virtual void Get(ListViewEx listView)
			{
				IList<ColumnHeaderEx> colhs = listView.Columns;
				int nCols = colhs.Count;
				m_Cols = new ColInfo[nCols];

				for (int i = 0; i < nCols; i++)
				{
					ColInfo ci = new ColInfo();

					ci.Text = colhs[i].Text;
					ci.TextAlign = colhs[i].TextAlign;
					ci.ItemTextMember = colhs[i].ItemTextMember;
					ci.ItemValueMember = colhs[i].ItemValueMember;
					ci.DataColIndex = colhs[i].DataColIndex;
					ci.Visible = colhs[i].Visible;
					ci.DisplayIndex = colhs[i].DisplayIndex;
					ci.Width = colhs[i].Width;

					m_Cols[i] = ci;
				}
			}
			#endregion
		}
		#endregion

		#region イベント関係宣言

		#region イベント引数クラス
		/// <summary>
		///	カラムヘッダのコンテキストメニュー表示イベント引数クラス。
		/// </summary>
		public class ShowColumnMenuEventArgs : EventArgs
		{
			private ColumnHeaderEx m_Column;
			private bool m_Cancel = false;

			/// <summary>
			/// コンストラクタ。
			/// </summary>
			/// <param name="column">メニュー表示対象カラム。</param>
			public ShowColumnMenuEventArgs(ColumnHeaderEx column)
			{
				m_Column = column;
			}

			/// <summary>
			/// メニュー表示対象のカラムオブジェクトの取得。
			/// </summary>
			public ColumnHeaderEx Column
			{
				get { return m_Column; }
			}

			/// <summary>
			/// デフォルトの処理をキャンセルするかどうかのフラグ。デフォルトの処理を行いたく無いときは true をセットする。
			/// </summary>
			public bool Cancel
			{
				get { return m_Cancel; }
				set { m_Cancel = value; }
			}
		}
		/// <summary>
		///	カラムヘッダメニュー取得イベント引数クラス。
		/// </summary>
		public class GetColumnMenuEventArgs : EventArgs
		{
			private ColumnHeaderEx m_Column;
			private ContextMenuStrip m_Menu;

			/// <summary>
			/// コンストラクタ。
			/// </summary>
			/// <param name="column">メニュー取得対象カラム。</param>
			public GetColumnMenuEventArgs(ColumnHeaderEx column)
			{
				m_Column = column;
			}

			/// <summary>
			/// メニュー取得対象のカラム。
			/// </summary>
			public ColumnHeaderEx Column
			{
				get { return m_Column; }
			}

			/// <summary>
			/// 表示するメニューを書き込む。
			/// </summary>
			public ContextMenuStrip Menu
			{
				get { return m_Menu; }
				set { m_Menu = value; }
			}
		}
		/// <summary>
		/// アイテムのコンテキストメニュー表示イベント引数クラス。
		/// </summary>
		public class ShowItemMenuEventArgs : EventArgs
		{
			private ListViewItemEx m_Item;
			private bool m_Cancel = false;

			/// <summary>
			/// コンストラクタ。
			/// </summary>
			/// <param name="item">メニュー表示対象のアイテム。</param>
			public ShowItemMenuEventArgs(ListViewItemEx item)
			{
				m_Item = item;
			}

			/// <summary>
			/// メニュー表示対象のアイテムの取得。
			/// </summary>
			public ListViewItemEx Item
			{
				get { return m_Item; }
			}

			/// <summary>
			/// デフォルトの処理をキャンセルするかどうかのフラグ。デフォルトの処理を行いたく無いときは true をセットする。
			/// </summary>
			public bool Cancel
			{
				get { return m_Cancel; }
				set { m_Cancel = value; }
			}
		}
		/// <summary>
		///	アイテムメニュー取得イベント引数クラス。
		/// </summary>
		public class GetItemMenuEventArgs : EventArgs
		{
			private ListViewItemEx m_Item;
			private ContextMenuStrip m_Menu;

			/// <summary>
			/// コンストラクタ。
			/// </summary>
			/// <param name="item">メニュー取得対象アイテム。</param>
			public GetItemMenuEventArgs(ListViewItemEx item)
			{
				m_Item = item;
			}

			/// <summary>
			/// メニュー取得対象のアイテムの取得。
			/// </summary>
			public ListViewItemEx Item
			{
				get { return m_Item; }
			}

			/// <summary>
			/// 表示するメニューを書き込む。
			/// </summary>
			public ContextMenuStrip Menu
			{
				get { return m_Menu; }
				set { m_Menu = value; }
			}
		}
		#endregion

		#region デリゲート
		/// <summary>
		///	カラムヘッダのコンテキストメニュー表示イベントハンドラデリゲート。
		/// </summary>
		public delegate void ShowColumnMenuEventHandler(object sender, ShowColumnMenuEventArgs e);
		/// <summary>
		///	カラムヘッダメニュー取得イベントハンドラデリゲート。
		/// </summary>
		public delegate void GetColumnMenuEventHandler(object sender, GetColumnMenuEventArgs e);
		/// <summary>
		/// アイテムのコンテキストメニュー表示イベントハンドラデリゲート。
		/// </summary>
		public delegate void ShowItemMenuEventHandler(object sender, ShowItemMenuEventArgs e);
		/// <summary>
		///	アイテムメニュー取得イベントハンドラデリゲート。
		/// </summary>
		public delegate void GetItemMenuEventHandler(object sender, GetItemMenuEventArgs e);
		#endregion

		#region イベント
		/// <summary>
		///	カラムヘッダのコンテキストメニュー表示イベント。
		/// </summary>
		[Category("ListViewEx")]
		[Description("カラムヘッダのコンテキストメニュー表示時に発生するイベントです。")]
		public event ShowColumnMenuEventHandler ShowColumnMenu;
		/// <summary>
		///	カラムヘッダメニュー取得イベント。
		/// </summary>
		[Category("ListViewEx")]
		[Description("カラムヘッダメニュー取得時に発生するイベントです。")]
		public event GetColumnMenuEventHandler GetColumnMenu;
		/// <summary>
		///	アイテムのコンテキストメニュー表示イベント。
		/// </summary>
		[Category("ListViewEx")]
		[Description("アイテムのコンテキストメニュー表示時に発生するイベントです。")]
		public event ShowItemMenuEventHandler ShowItemMenu;
		/// <summary>
		///	アイテムメニュー取得イベント。
		/// </summary>
		[Category("ListViewEx")]
		[Description("アイテムメニュー取得時に発生するイベントです。")]
		public event GetItemMenuEventHandler GetItemMenu;
		/// <summary>
		///	選択変更後イベント。
		/// </summary>
		[Category("ListViewEx")]
		[Description("選択状態が変更された後に発生するイベントです。")]
		public event EventHandler SelectionChanged;
		#endregion

		#endregion

		#region フィールド
		static readonly private int m_NeedCallSelChangeEventMessage = Win32API.RegisterWindowMessageW("55CCBFD0-B01D-437f-85C1-2C64AF6DD757");

		private SettingInfo m_Setting;

		private ColumnHeaderCollectionEx m_Columns;
		private ListViewItemCollectionEx m_Items;
		private ListViewSelectedIndexCollectionEx m_SelectedIndices;
		private ListViewSelectedItemCollectionEx m_SelectedItems;

		private ImageList m_LargeImageList;
		private ImageList m_SmallImageList;

		private bool m_AllowColClickSort = true;

		private SortOrder m_Sorting = SortOrder.None;
		private ColumnHeaderEx m_SortColumn = null;
		private ItemComparer m_ListViewItemSorter;

		private int m_IndexModifiedCount = 0;
		private int m_LastItemCount = 0;
		private bool m_Modified = false;
		private bool m_NeedSort = false;
		private bool m_NeedSelChangeEventCall = false;

		private bool m_SearchItemOnKeyDown;
		private string m_FindingText; // キー入力で検索中の文字列
		#endregion

		#region コンストラクタ
		/// <summary>
		/// コンストラクタ。
		/// </summary>
		public ListViewEx()
		{
			InitializeComponent();

			m_Columns = CreateColumnCollection();
			m_Items = CreateItemCollection();
			m_SelectedIndices = CreateSelectedIndexCollection();
			m_SelectedItems = CreateSelectedItemCollection();
			this.SetStyle(ControlStyles.UserPaint, false);

			m_SearchItemOnKeyDown = true;

			Invalidate();
		}
		#endregion

		#region プロパティ
		/// <summary>
		/// このコントロールに関連付けられているテキストを取得または設定します。
		/// </summary>
		[Localizable(true)]
		[Browsable(false)]
		[DispId(-517)]
		[Bindable(true)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override string Text
		{
			get { return base.Text; }
			set { base.Text = value; }
		}

		/// <summary>
		/// カラムコレクションの取得。
		/// </summary>
		[Category("ListViewEx")]
		[Browsable(false)]
		[Description("カラム一覧です。")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ColumnHeaderCollectionEx Columns
		{
			get { return m_Columns; }
		}

		/// <summary>
		/// アイテムコレクションの取得。
		/// </summary>
		[Category("ListViewEx")]
		[Browsable(false)]
		[Description("アイテム一覧です。")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ListViewItemCollectionEx Items
		{
			get { return m_Items; }
		}

		/// <summary>
		/// 選択アイテムのインデックス番号コレクションの取得。
		/// </summary>
		[Category("ListViewEx")]
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ListViewSelectedIndexCollectionEx SelectedIndices
		{
			get { return m_SelectedIndices; }
		}

		/// <summary>
		/// 選択アイテムのコレクションの取得。
		/// </summary>
		[Category("ListViewEx")]
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ListViewSelectedItemCollectionEx SelectedItems
		{
			get { return m_SelectedItems; }
		}


		/// <summary>
		/// 項目をコントロールで大きいアイコンとして表示するときに使用する System.Windows.Forms.ImageList を取得または設定します。
		/// </summary>
		[Category("ListViewEx")]
		[DefaultValue("")]
		[Description("項目をコントロールで大きいアイコンとして表示するときに使用する System.Windows.Forms.ImageList を取得または設定します。")]
		public ImageList LargeImageList
		{
			get { return m_LargeImageList; }
			set
			{
				if (m_LargeImageList != null)
					m_LargeImageList.RecreateHandle -= new EventHandler(LargeImageList_RecreateHandle);

				m_LargeImageList = value;

				if (m_LargeImageList != null)
					m_LargeImageList.RecreateHandle += new EventHandler(LargeImageList_RecreateHandle);

				SetLargeImageListHandle(m_LargeImageList != null ? m_LargeImageList.Handle : IntPtr.Zero);
				Invalidate();
			}
		}

		/// <summary>
		/// 項目をコントロールで小さいアイコンとして表示するときに使用する System.Windows.Forms.ImageList を取得または設定します。
		/// </summary>
		[Category("ListViewEx")]
		[DefaultValue("")]
		[Description("項目をコントロールで小さいアイコンとして表示するときに使用する System.Windows.Forms.ImageList を取得または設定します。")]
		public ImageList SmallImageList
		{
			get { return m_SmallImageList; }
			set
			{
				if (m_SmallImageList != null)
					m_SmallImageList.RecreateHandle -= new EventHandler(SmallImageList_RecreateHandle);

				m_SmallImageList = value;

				if (m_SmallImageList != null)
					m_SmallImageList.RecreateHandle += new EventHandler(SmallImageList_RecreateHandle);

				SetSmallImageListHandle(m_SmallImageList != null ? m_SmallImageList.Handle : IntPtr.Zero);
				Invalidate();
			}
		}

		/// <summary>
		/// コントロールに項目を表示する方法を取得または設定します。
		/// </summary>
		[Category("ListViewEx")]
		[Description("コントロールに項目を表示する方法を指定します。")]
		public View View
		{
			get
			{
				int style = Win32API.GetWindowLongW(this.Handle, Win32API.GWL_STYLE);
				if ((style & Win32API.LVS_REPORT) == Win32API.LVS_REPORT)
				{
					return View.Details;
				}
				else if ((style & Win32API.LVS_SMALLICON) == Win32API.LVS_SMALLICON)
				{
					return View.SmallIcon;
				}
				else if ((style & Win32API.LVS_LIST) == Win32API.LVS_LIST)
				{
					return View.List;
				}
				//	View.Tile はどのフラグで判定していいのかわからない・・・
				return View.LargeIcon; // デフォルトは LVS_ICON
			}
			set
			{
				int style = Win32API.GetWindowLongW(this.Handle, Win32API.GWL_STYLE) & ~Win32API.LVS_TYPEMASK;
				switch (value)
				{
					case View.LargeIcon: style |= Win32API.LVS_ICON; break;
					case View.Details: style |= Win32API.LVS_REPORT; break;
					case View.SmallIcon: style |= Win32API.LVS_SMALLICON; break;
					case View.List: style |= Win32API.LVS_LIST; break;
					default: throw new NotSupportedException("ListViewEx では View.Tile はサポートされていません。");
				}
				Win32API.SetWindowLongW(this.Handle, Win32API.GWL_STYLE, style);
			}
		}

		/// <summary>
		/// 複数の項目を選択できるかどうかを示す値を取得または設定します。
		/// </summary>
		[Category("ListViewEx")]
		[DefaultValue(true)]
		[Description("小さいアイコンのイメージリストを指定します。")]
		public bool MultiSelect
		{
			get
			{
				return (WndGetStyle() & Win32API.LVS_SINGLESEL) == 0;
			}
			set
			{
				WndModifyStyle(Win32API.LVS_SINGLESEL, value ? 0 : Win32API.LVS_SINGLESEL);
			}
		}

		/// <summary>
		/// 項目をクリックするとそのすべてのサブ項目を選択するかどうかを示す値を取得または設定します。
		/// </summary>
		[Category("ListViewEx")]
		[DefaultValue(false)]
		[Description("選択アイテムの全てのサブ項目を強調表示させるかどうかを指定します。")]
		public bool FullRowSelect
		{
			get
			{
				return (LvGetExtendedListViewStyleEx() & Win32API.LVS_EX_FULLROWSELECT) != 0;
			}
			set
			{
				LvSetExtendedListViewStyleEx(Win32API.LVS_EX_FULLROWSELECT, value ? Win32API.LVS_EX_FULLROWSELECT : 0);
			}
		}

		/// <summary>
		/// コントロールの項目とサブ項目を含む行と列の間にグリッド線を表示するかどうかを示す値を取得または設定します。
		/// </summary>
		[Category("ListViewEx")]
		[DefaultValue(false)]
		[Description("グリッド線を表示するかどうかを指定します。")]
		public bool GridLines
		{
			get
			{
				return (LvGetExtendedListViewStyleEx() & Win32API.LVS_EX_GRIDLINES) != 0;
			}
			set
			{
				LvSetExtendedListViewStyleEx(Win32API.LVS_EX_GRIDLINES, value ? Win32API.LVS_EX_GRIDLINES  : 0);
			}
		}

		/// <summary>
		/// ユーザーが列ヘッダーをドラッグしてコントロールの列の並べ替えができるかどうかを示す値を、取得または設定します。
		/// </summary>
		[Category("ListViewEx")]
		[DefaultValue(false)]
		[Description("ユーザーが列ヘッダーをドラッグしてコントロールの列の並べ替えができるかどうかを指定します。")]
		public bool AllowColumnReorder
		{
			get
			{
				return (LvGetExtendedListViewStyleEx() & Win32API.LVS_EX_HEADERDRAGDROP) != 0;
			}
			set
			{
				LvSetExtendedListViewStyleEx(Win32API.LVS_EX_HEADERDRAGDROP, value ? Win32API.LVS_EX_HEADERDRAGDROP : 0);
			}
		}

		/// <summary>
		/// アイコンが自動的に配置されるかどうかを取得または設定します。
		/// </summary>
		[Category("ListViewEx")]
		[DefaultValue(true)]
		[Description("アイコンが自動的に配置されるかどうかを指定します。")]
		public bool AutoArrange
		{
			get
			{
				return (WndGetStyle() & Win32API.LVS_AUTOARRANGE) != 0;
			}
			set
			{
				WndModifyStyle(Win32API.LVS_AUTOARRANGE, value ? Win32API.LVS_AUTOARRANGE : 0);
			}
		}

		/// <summary>
		/// コントロールがフォーカスを失ったときに、そのコントロールで選択されている項目が強調表示されたままかどうかを示す値を取得または設定します。
		/// </summary>
		[Category("ListViewEx")]
		[DefaultValue(true)]
		[Description("コントロールがフォーカスを失ったときに、そのコントロールで選択されている項目が強調表示されたままかどうかを示す値を取得または設定します。")]
		public bool HideSelection
		{
			get
			{
				return (WndGetStyle() & Win32API.LVS_SHOWSELALWAYS) == 0;
			}
			set
			{
				WndModifyStyle(Win32API.LVS_SHOWSELALWAYS, value ? 0 : Win32API.LVS_SHOWSELALWAYS);
			}
		}

		/// <summary>
		///	アイテムの並べ替え順序の取得と設定。
		/// </summary>
		[Category("ListViewEx")]
		[Description("アイテムの並べ替え順序を指定します。")]
		public SortOrder Sorting
		{
			get
			{
				return m_Sorting;
			}
			set
			{
				if (value == m_Sorting)
					return;
				m_Sorting = value;

				if (!m_NeedSort)
				{
					m_NeedSort = true;
					this.Invalidate();
				}
			}
		}

		/// <summary>
		/// ソート対象のカラムの取得と設定。
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ColumnHeaderEx SortColumn
		{
			get
			{
				return m_SortColumn;
			}
			set
			{
				if (value == m_SortColumn)
					return;
				m_SortColumn = value;

				if (!m_NeedSort)
				{
					m_NeedSort = true;
					this.Invalidate();
				}
			}
		}

		/// <summary>
		///	アイテムソート時の比較処理実行オブジェクトの取得と設定。
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ItemComparer ListViewItemSorter
		{
			get
			{
				return m_ListViewItemSorter;
			}
			set
			{
				if (value == m_ListViewItemSorter)
					return;
				m_ListViewItemSorter = value;

				if (!m_NeedSort)
				{
					m_NeedSort = true;
					this.Invalidate();
				}
			}
		}

		/// <summary>
		/// カラムクリックソートの許可フラグの取得と設定。
		/// </summary>
		[Category("ListViewEx")]
		[Description("カラムクリックによるソートを許可するかどうかを指定します。")]
		public bool AllowColClickSort
		{
			get { return m_AllowColClickSort; }
			set { m_AllowColClickSort = value; }
		}

		/// <summary>
		///	コントロールの設定情報。
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public SettingInfo Setting
		{
			get { return m_Setting; }
			set { m_Setting = value; }
		}


		/// <summary>
		/// キー入力時にアイテムを検索するかどうかの設定
		/// </summary>
		[Category("ListViewEx")]
		[DefaultValue(true)]
		[Description("キー入力時に入力された文字を持つアイテムを検索し、選択するかどうかを示す値を取得または設定します。")]
		public bool SearchItemOnKeyDown
		{
			get { return m_SearchItemOnKeyDown; }
			set { m_SearchItemOnKeyDown = value; }
		}
		#endregion

		#region メソッド
		/// <summary>
		///	指定された設定情報で初期化する。
		///	コントロール破棄時に指定された設定に上書きされます。
		/// </summary>
		/// <param name="settingInfo">設定情報。</param>
		public void LoadSetting(SettingInfo settingInfo)
		{
			this.Setting = settingInfo;
			if (this.Setting != null)
			{
				this.Setting.Set(this);
			}
		}

		/// <summary>
		///	LoadSetting で渡されている設定情報へ現在の設定を保存する。
		/// </summary>
		public void SaveSetting()
		{
			if (this.Setting != null)
			{
				this.Setting.Get(this);
			}
		}

		/// <summary>
		/// 全てのカラムを表示し、カラムの順序を初期状態にする。
		/// </summary>
		public void NormalizeColumnHeader()
		{
			int nCols = this.Columns.Count;
			for (int i = 0; i < nCols; i++)
			{
				m_Columns[i].Width = 100;
				m_Columns[i].Visible = true;
			}
			for (int i = 0; i < nCols; i++)
			{
				m_Columns[i].DisplayIndex = i;
			}
		}

		/// <summary>
		/// 指定されたカラムの実データをキーにして指定された順序でソートする。即座にソートされる。
		/// </summary>
		/// <param name="column">ソートのキーとして使用するデータのカラム。</param>
		/// <param name="order">ソート順序。</param>
		public void Sort(ColumnHeaderEx column, SortOrder order)
		{
			if (!SortItems(column, order))
				return;
			m_SortColumn = column;
			m_Sorting = order;
		}

		/// <summary>
		///	大きいアイコンのイメージリストのハンドルを設定する。
		/// </summary>
		/// <param name="handle">イメージリストハンドル。</param>
		public void SetLargeImageListHandle(IntPtr handle)
		{
			Win32API.SendMessage(this.Handle, Win32API.LVM_SETIMAGELIST, (IntPtr)Win32API.LVSIL_NORMAL, handle);
		}

		/// <summary>
		///	小さいアイコンのイメージリストのハンドルを設定する。
		/// </summary>
		/// <param name="handle">イメージリストハンドル。</param>
		public void SetSmallImageListHandle(IntPtr handle)
		{
			Win32API.SendMessage(this.Handle, Win32API.LVM_SETIMAGELIST, (IntPtr)Win32API.LVSIL_SMALL, handle);
		}

		/// <summary>
		///	大きいアイコンのイメージリストのハンドルを取得する。
		/// </summary>
		public IntPtr GetLargeImageListHandle()
		{
			return Win32API.SendMessage(this.Handle, Win32API.LVM_GETIMAGELIST, (IntPtr)Win32API.LVSIL_NORMAL, IntPtr.Zero);
		}

		/// <summary>
		///	小さいアイコンのイメージリストのハンドルを取得する。
		/// </summary>
		public IntPtr GetSmallImageListHandle()
		{
			return Win32API.SendMessage(this.Handle, Win32API.LVM_GETIMAGELIST, (IntPtr)Win32API.LVSIL_SMALL, IntPtr.Zero);
		}

		/// <summary>
		/// 指定されたインデックス番号のアイテムの選択状態を取得する。
		/// </summary>
		/// <param name="iItem">アイテムのインデックス番号。</param>
		/// <returns>選択されている:true、選択されていない:false。</returns>
		public bool GetItemSelected(int iItem)
		{
			return LvGetItemState(iItem, Win32API.LVIS_SELECTED) != 0;
		}

		/// <summary>
		/// 指定されたインデックス番号のアイテムの選択状態を設定する。
		/// </summary>
		/// <param name="iItem">アイテムのインデックス番号。</param>
		/// <param name="selected">アイテムの選択状態。</param>
		public void SetItemSelected(int iItem, bool selected)
		{
			LvSetItemState(iItem, selected ? Win32API.LVIS_SELECTED : 0, Win32API.LVIS_SELECTED);
		}

		/// <summary>
		/// 指定されたインデックス番号のアイテムのフォーカス状態を取得する。
		/// </summary>
		/// <param name="iItem">アイテムのインデックス番号。</param>
		/// <returns>フォーカスされている:true、フォーカスされていない:false。</returns>
		public bool GetItemFocused(int iItem)
		{
			return LvGetItemState(iItem, Win32API.LVIS_FOCUSED) != 0;
		}

		/// <summary>
		/// 指定されたインデックス番号のアイテムのフォーカス状態を設定する。
		/// </summary>
		/// <param name="iItem">アイテムのインデックス番号。</param>
		/// <param name="focused">アイテムのフォーカス状態。</param>
		public void SetItemFocused(int iItem, bool focused)
		{
			LvSetItemState(iItem, focused ? Win32API.LVIS_FOCUSED : 0, Win32API.LVIS_FOCUSED);
		}

		/// <summary>
		/// 指定されたインデックス番号のアイテムが表示されるようにスクロールする。
		/// </summary>
		/// <param name="iItem">アイテムのインデックス番号。</param>
		public void EnsureVisibleItem(int iItem)
		{
			LvEnsureVisible(iItem, false);
		}

		/// <summary>
		/// 指定されたポイントと接触しているアイテムを探す。
		/// </summary>
		/// <returns>接触しているアイテムがある：アイテムインデックス番号、接触していない：-1。</returns>
		public int HitTestItem(int x, int y)
		{
			Win32API.LVHITTESTINFO htinfo = new Win32API.LVHITTESTINFO();
			htinfo.pt.x = x;
			htinfo.pt.y = y;
			return LvHitTest(ref htinfo);
		}

		/// <summary>
		/// 全アイテムを選択状態にする
		/// </summary>
		public void SelectAllItems()
		{
			for (int i = 0, n = m_Items.Count; i < n; i++)
				SetItemSelected(i, true);
		}
		#endregion

		#region イベントトリガ
		/// <summary>
		/// ShowColumnMenu イベントを発生させる。
		/// </summary>
		protected virtual void OnShowColumnMenu(ShowColumnMenuEventArgs e)
		{
			if (ShowColumnMenu != null)
				ShowColumnMenu(this, e);
			if (e.Cancel)
				return;

		}

		/// <summary>
		/// GetColumnMenu イベントを発生させる。
		/// </summary>
		protected virtual void OnGetColumnMenu(GetColumnMenuEventArgs e)
		{
			if (GetColumnMenu != null)
				GetColumnMenu(this, e);
		}

		/// <summary>
		/// ShowItemMenu イベントを発生させる。
		/// </summary>
		protected virtual void OnShowItemMenu(ShowItemMenuEventArgs e)
		{
			if (ShowItemMenu != null)
				ShowItemMenu(this, e);
			if (e.Cancel)
				return;
		}

		/// <summary>
		/// GetItemMenu イベントを発生させる。
		/// </summary>
		protected virtual void OnGetItemMenu(GetItemMenuEventArgs e)
		{
			if (GetItemMenu != null)
				GetItemMenu(this, e);
		}

		/// <summary>
		/// SelectionChanged イベントを発生させる。
		/// </summary>
		protected virtual void OnSelectionChanged(EventArgs e)
		{
			if (SelectionChanged != null)
				SelectionChanged(this, e);
		}

		/// <summary>
		/// カラムクリック時のイベントトリガ。
		/// </summary>
		/// <param name="e"></param>
		protected virtual void OnColumnClick(ColumnClickEventArgs e)
		{
			//	カラムクリックによるソートが許可されていたらソートする
			if (m_AllowColClickSort)
			{
				ColumnHeaderEx col = this.Columns[e.Column];
				SortOrder order;
				if (object.ReferenceEquals(col, m_SortColumn))
				{
					order = m_Sorting == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
				}
				else
				{
					order = SortOrder.Ascending;
				}
				Sort(col, order);
			}
		}
		#endregion

		#region アセンブリ内公開プロパティ
		/// <summary>
		/// アイテムインデックス変更カウントの取得。
		/// </summary>
		internal int IndexModifiedCount
		{
			get { return m_IndexModifiedCount; }
		}
		#endregion

		#region アセンブリ内公開メソッド
		/// <summary>
		/// カラムコレクションが変更された時に呼び出される。
		/// </summary>
		/// <param name="sender"></param>
		internal void OnColumnCollectionChanged(ColumnHeaderCollectionEx sender)
		{
		}

		/// <summary>
		/// カラムのデータインデックス番号変更イベントハンドラ。
		/// </summary>
		internal void OnColumnModified(ColumnHeaderEx sender)
		{
		}

		/// <summary>
		/// アイテムのコレクションが変更されたときに呼び出される。
		/// </summary>
		internal void OnItemCollectionChanged(ListViewItemCollectionEx sender)
		{
			int nItems = this.Items.Count;
			if (nItems != m_LastItemCount)
			{
				this.LvSetItemCount(nItems);
				m_LastItemCount = nItems;
			}
			if (!m_Modified)
			{
				m_Modified = true;
				m_NeedSort = true;
				this.Invalidate();
			}
			m_IndexModifiedCount++;
		}

		/// <summary>
		/// アイテムが変更されたときに呼び出される。
		/// </summary>
		internal void OnItemChanged(ListViewItemEx sender)
		{
			if (!m_Modified)
			{
				m_Modified = true;
				this.Invalidate();
			}
		}

		/// <summary>
		/// アイテムの実データが変更された時に呼び出される。
		/// </summary>
		internal void OnItemValueChanged(ListViewItemEx sender)
		{
			if (!m_Modified)
			{
				m_Modified = true;
				m_NeedSort = true;
				this.Invalidate();
			}
		}

		/// <summary>
		/// アイテムが削除される直前に呼ばれる。
		/// </summary>
		/// <param name="sender">送り主のコレクション。</param>
		/// <param name="startIndex">削除開始アイテムインデックス番号。</param>
		/// <param name="count">削除アイテム数。</param>
		internal void OnBeforeItemRemove(ListViewItemCollectionEx sender, int startIndex, int count)
		{
			for (int i = startIndex, end = startIndex + count; i < end; i++)
			{
				//	削除されるアイテムが選択状態だった場合は選択状態変更されたとみなす
				if (GetItemSelected(i))
				{
					SelectionModified();
					break;
				}
			}
		}

		/// <summary>
		/// アイテムが削除された直後に呼ばれる。
		/// </summary>
		/// <param name="sender">送り主のコレクション。</param>
		/// <param name="startIndex">削除開始アイテムインデックス番号。</param>
		/// <param name="count">削除アイテム数。</param>
		internal void OnAfterItemRemove(ListViewItemCollectionEx sender, int startIndex, int count)
		{
			OnItemCollectionChanged(sender);
		}
		#endregion

		#region Win32アクセス
		internal int WndGetStyle()
		{
			return Win32API.GetWindowLongW(this.Handle, Win32API.GWL_STYLE);
		}
		internal void WndSetStyle(int style)
		{
			Win32API.SetWindowLongW(this.Handle, Win32API.GWL_STYLE, style);
		}
		internal void WndModifyStyle(int mask, int dw)
		{
			int style = WndGetStyle();
			WndSetStyle(style & ~mask | dw);
		}
		internal int WndGetStyleEx()
		{
			return Win32API.GetWindowLongW(this.Handle, Win32API.GWL_EXSTYLE);
		}
		internal void WndSetStyleEx(int style)
		{
			Win32API.SetWindowLongW(this.Handle, Win32API.GWL_EXSTYLE, style);
		}
		internal void WndModifyStyleEx(int mask, int dw)
		{
			int style = WndGetStyleEx();
			WndSetStyleEx(style & ~mask | dw);
		}

		internal void LvSetItemState(int iItem, int data, int mask)
		{
			Win32API.LVITEMW lvi = new Win32API.LVITEMW();
			lvi.stateMask = (uint)mask;
			lvi.state = (uint)data;
			Win32API.SendMessage_LvItem(this.Handle, Win32API.LVM_SETITEMSTATE, (IntPtr)iItem, ref lvi);
		}
		internal int LvGetItemState(int iItem, int mask)
		{
			return (int)Win32API.SendMessage(this.Handle, Win32API.LVM_GETITEMSTATE, (IntPtr)iItem, (IntPtr)mask);
		}
		internal void LvEnsureVisible(int iItem, bool partialOk)
		{
			int fPartialOK = partialOk ? 1 : 0;
			Win32API.SendMessage(this.Handle, Win32API.LVM_ENSUREVISIBLE, (IntPtr)iItem, Win32API.MakeLParam(fPartialOK, 0));
		}
		internal void LvSetExtendedListViewStyleEx(int mask, int dw)
		{
			Win32API.SendMessage(this.Handle, Win32API.LVM_SETEXTENDEDLISTVIEWSTYLE, (IntPtr)mask, (IntPtr)dw);
		}
		internal int LvGetExtendedListViewStyleEx()
		{
			return (int)Win32API.SendMessage(this.Handle, Win32API.LVM_GETEXTENDEDLISTVIEWSTYLE, IntPtr.Zero, IntPtr.Zero);
		}
		internal void LvSetItemCount(int count)
		{
			Win32API.SendMessage(this.Handle, Win32API.LVM_SETITEMCOUNT, (IntPtr)count, IntPtr.Zero);
		}
		internal int LvGetItemCount()
		{
			return (int)Win32API.SendMessage(this.Handle, Win32API.LVM_GETITEMCOUNT, IntPtr.Zero, IntPtr.Zero);
		}
		internal void LvDeleteColumn(int iColumn)
		{
			Win32API.SendMessage(this.Handle, Win32API.LVM_DELETECOLUMN, (IntPtr)iColumn, IntPtr.Zero);
		}
		internal void LvSetColumn(int iColumn, ref Win32API.LVCOLUMNW lvc)
		{
			Win32API.SendMessage_LvColumn(this.Handle, Win32API.LVM_SETCOLUMNW, (IntPtr)iColumn, ref lvc);
		}
		internal void LvSetColumn(int iColumn, ref Win32API.LVCOLUMNW_Set lvc)
		{
			Win32API.SendMessage_LvSetColumn(this.Handle, Win32API.LVM_SETCOLUMNW, (IntPtr)iColumn, ref lvc);
		}
		internal void LvGetColumn(int iColumn, ref Win32API.LVCOLUMNW lvc)
		{
			Win32API.SendMessage_LvColumn(this.Handle, Win32API.LVM_GETCOLUMNW, (IntPtr)iColumn, ref lvc);
		}
		internal void LvSetColumnWidth(int iColumn, int cx)
		{
			Win32API.SendMessage(this.Handle, Win32API.LVM_SETCOLUMNWIDTH, (IntPtr)iColumn, Win32API.MakeLParam(cx, 0));
		}
		internal int LvInsertColumn(int iColumn, ref Win32API.LVCOLUMNW lvc)
		{
			return (int)Win32API.SendMessage_LvColumn(this.Handle, Win32API.LVM_INSERTCOLUMNW, (IntPtr)iColumn, ref lvc);
		}
		internal int LvInsertColumn(int iColumn, ref Win32API.LVCOLUMNW_Set lvc)
		{
			return (int)Win32API.SendMessage_LvSetColumn(this.Handle, Win32API.LVM_INSERTCOLUMNW, (IntPtr)iColumn, ref lvc);
		}
		internal IntPtr LvGetHeader()
		{
			return Win32API.SendMessage(this.Handle, Win32API.LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero);
		}
		internal int LvHitTest(ref Win32API.LVHITTESTINFO htinfo)
		{
			return (int)Win32API.SendMessage_LvHitTest(this.Handle, Win32API.LVM_HITTEST, IntPtr.Zero, ref htinfo);
		}
		internal int LvGetSelectionMark()
		{
			return (int)Win32API.SendMessage(this.Handle, Win32API.LVM_GETSELECTIONMARK, IntPtr.Zero, IntPtr.Zero);
		}
		internal int LvSetSelectionMark(int iItem)
		{
			return (int)Win32API.SendMessage(this.Handle, Win32API.LVM_SETSELECTIONMARK, IntPtr.Zero, (IntPtr)iItem);
		}
		#endregion

		#region 内部メソッド
		/// <summary>
		///	アイテムコレクションをソートする。
		/// </summary>
		/// <param name="column">ソートのキーとして使用するデータのカラム。</param>
		/// <param name="order">ソート順序。</param>
		private bool SortItems(ColumnHeaderEx column, SortOrder order)
		{
			if (order == SortOrder.None)
				return false; // 現状ソート無しに戻すことは出来ない

			int index = this.Columns.IndexOf(column);
			if (index < 0)
				return false; // ソート対象のカラムが見つからない

			//	ソートする
			ItemComparer cmp;
			if (m_ListViewItemSorter == null)
			{
				cmp = new ItemComparer();
			}
			else
			{
				cmp = m_ListViewItemSorter;
			}
			cmp.Column = column;
			cmp.Order = order;
			m_Items.Sort(cmp);

			return true;
		}

		/// <summary>
		/// カラムヘッダが右クリックされたときの処理。
		/// </summary>
		private void ColumnContextMenu(ColumnHeaderEx column)
		{
			//	コンテキストメニュー表示イベントをトリガ
			ShowColumnMenuEventArgs e1 = new ShowColumnMenuEventArgs(column);
			OnShowColumnMenu(e1);
			if (e1.Cancel)
				return;

			//	メニュー取得イベントをトリガ
			GetColumnMenuEventArgs e2 = new GetColumnMenuEventArgs(column);
			e2.Menu = new ContextMenuStrip();
			foreach (ColumnHeaderEx col in this.Columns)
			{
				ColumnHeaderEx c = col; // 匿名メソッド内で col にアクセスすると必ず最後のカラムになってしまうのでローカル変数へコピー(C#のバグじゃないかと思う)

				ToolStripMenuItem ti = new ToolStripMenuItem();
				ti.Text = c.Text;
				ti.Checked = c.Visible;
				ti.Click += delegate
					{
						if (c.Visible)
						{
							if (this.Columns.VisibleColumns.Count <= 1)
								return; // 一つだけ列が表示されている時にその列を非表示にできない
						}
						c.Visible = !c.Visible;
					};
				e2.Menu.Items.Add(ti);
			}
			e2.Menu.Items.Add(new ToolStripSeparator());

			ToolStripMenuItem ti2 = new ToolStripMenuItem();
			ti2.Text = "表示を初期化(&I)";
			ti2.Click += delegate
				{
					NormalizeColumnHeader();
				};
			e2.Menu.Items.Add(ti2);

			OnGetColumnMenu(e2);
			if (e2.Menu == null)
				return;

			//	メニュー表示
			e2.Menu.Show(this, this.PointToClient(Control.MousePosition));
		}

		/// <summary>
		/// アイテムのコンテキストメニューを表示。
		/// </summary>
		private void ItemContextMenu(ListViewItemEx item)
		{
			//	コンテキストメニュー表示イベントをトリガ
			ShowItemMenuEventArgs e1 = new ShowItemMenuEventArgs(item);
			OnShowItemMenu(e1);
			if (e1.Cancel)
				return;

			//	メニュー取得イベントをトリガ
			GetItemMenuEventArgs e2 = new GetItemMenuEventArgs(item);
			OnGetItemMenu(e2);
			if (e2.Menu == null)
				return;

			//	メニュー表示
			e2.Menu.Show(this, this.PointToClient(Control.MousePosition));
		}

		/// <summary>
		///	選択状態が変化したときに呼び出す。
		/// </summary>
		private void SelectionModified()
		{
			m_NeedSelChangeEventCall = true;
			m_SelectedIndices.SelectionModified();
			Win32API.PostMessage(this.Handle, m_NeedCallSelChangeEventMessage, IntPtr.Zero, IntPtr.Zero);
		}

		/// <summary>
		/// イメージリストを切り離す。
		/// </summary>
		private void UnlinkImageList()
		{
			//	イメージリストからデリゲートが呼ばれないようにする
			if (m_SmallImageList != null)
			{
				m_SmallImageList.RecreateHandle -= new EventHandler(SmallImageList_RecreateHandle);
				m_SmallImageList = null;
			}
			if (m_LargeImageList != null)
			{
				m_LargeImageList.RecreateHandle -= new EventHandler(LargeImageList_RecreateHandle);
				m_LargeImageList = null;
			}
		}

		/// <summary>
		///	カラムコレクション作成。必要に応じてオーバーライドする。
		/// </summary>
		/// <returns>カラムコレクションオブジェクト。</returns>
		protected virtual ColumnHeaderCollectionEx CreateColumnCollection()
		{
			return new ColumnHeaderCollectionEx(this);
		}

		/// <summary>
		///	アイテムコレクション作成。必要に応じてオーバーライドする。
		/// </summary>
		/// <returns>アイテムコレクションオブジェクト。</returns>
		protected virtual ListViewItemCollectionEx CreateItemCollection()
		{
			return new ListViewItemCollectionEx(this);
		}

		/// <summary>
		///	アイテムコレクション作成。必要に応じてオーバーライドする。
		/// </summary>
		/// <returns>アイテムコレクションオブジェクト。</returns>
		protected virtual ListViewSelectedIndexCollectionEx CreateSelectedIndexCollection()
		{
			return new ListViewSelectedIndexCollectionEx(this);
		}

		/// <summary>
		///	選択されたアイテムコレクション作成。必要に応じてオーバーライドする。
		/// </summary>
		/// <returns>選択アイテムコレクションオブジェクト。</returns>
		protected virtual ListViewSelectedItemCollectionEx CreateSelectedItemCollection()
		{
			return new ListViewSelectedItemCollectionEx(this);
		}

		/// <summary>
		/// 指定された位置から指定されたテキストに前方一致するアイテムを検索する。
		/// </summary>
		protected int FindKeyInputItem(int iStart, string text)
		{
			if(m_Columns.VisibleColumns.Count == 0)
				return -1;

			string find = text.ToLower();
			ColumnHeaderEx col = m_Columns.VisibleColumns[0];

			//	一旦指定された位置から最後尾まで検索
			if (iStart < 0)
				iStart = 0;
			int n = m_Items.Count;
			for (int i = iStart; i < n; i++)
			{
				ListViewItemEx item = m_Items[i];
				string t = col.GetItemText(item);
				if (t != null)
				{
					t = t.ToLower();
					if (t.StartsWith(find))
						return i;
				}
			}

			//	先頭から開始位置まで検索
			if (n < iStart)
				iStart = n;
			for (int i = 0; i < iStart; i++)
			{
				ListViewItemEx item = m_Items[i];
				string t = col.GetItemText(item);
				if (t != null)
				{
					t = t.ToLower();
					if (t.StartsWith(find))
						return i;
				}
			}

			return -1;
		}
		#endregion

		#region オーバーライド
		/// <summary>
		/// CreateParams プロパティオーバーライド。
		/// </summary>
		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams p = base.CreateParams;
				p.ClassName = "SysListView32";
				p.ExStyle |= Win32API.WS_EX_CLIENTEDGE;
				p.Style |= Win32API.LVS_OWNERDATA | Win32API.LVS_REPORT | Win32API.LVS_SHAREIMAGELISTS;
				return p;
			}
		}

        /// <summary>
        /// WndProc オーバーライド。
        /// </summary>
		protected override void WndProc(ref Message m)
		{
			//	選択変更後イベントを発生させる
			if (m.Msg == m_NeedCallSelChangeEventMessage)
			{
				if (m_NeedSelChangeEventCall)
				{
					m_NeedSelChangeEventCall = false;
					OnSelectionChanged(EventArgs.Empty);
				}
				return;
			}

			//	メッセージにより処理を分ける
			switch (m.Msg)
			{
				case Win32API.WM_KEYDOWN:
					{
						//	ESCとかカーソルキーとか選択変更されるキー押されたら検索中の文字列をクリア
						int code = (int)m.WParam;
						switch (code)
						{
							case 'A':
								if (Win32API.GetKeyState(Win32API.VK_CONTROL) != 0)
								{
									m_FindingText = null;
									SelectAllItems();
								}
								break;
							case Win32API.VK_LEFT:
							case Win32API.VK_UP:
							case Win32API.VK_RIGHT:
							case Win32API.VK_DOWN:
							case Win32API.VK_ESCAPE:
							case Win32API.VK_NEXT:
							case Win32API.VK_PRIOR:
							case Win32API.VK_HOME:
							case Win32API.VK_END:
								m_FindingText = null;
								break;
						}
					}
					break;

				case Win32API.WM_CHAR:
					if(m_SearchItemOnKeyDown)
					{
						//	キー入力でアイテムを検索する
						char c = (char)m.WParam;
						if (m_FindingText == null)
							m_FindingText = new string(c, 1);
						else
							m_FindingText += c;

						if(1 <= m_Columns.VisibleColumns.Count)
						{
							int selIndex = 0;
							for (int i = 0, n = m_Items.Count; i < n; i++)
							{
								if (GetItemSelected(i))
								{
									selIndex = i;
									break;
								}
							}

							int foundIndex = FindKeyInputItem(selIndex, m_FindingText); // とりあえず検索する

							//	見つからなかったら入力された文字に限定して探す
							if (foundIndex < 0)
							{
								m_FindingText = new string(c, 1);
								foundIndex = FindKeyInputItem(selIndex + 1, m_FindingText);
							}

							if (0 <= foundIndex)
							{
								//	見つかったらアイテム選択
								SetItemSelected(foundIndex, true);
								SetItemFocused(foundIndex, true);
								EnsureVisibleItem(foundIndex);
							}
							else
							{
								//	ここまでやって見つからないならこの検索文字列は無意味なので null にする
								m_FindingText = null;
							}
						}
					}
					return;

				case Win32API.WM_MOUSEHOVER:
					//	なぜかこのメッセージを処理するとマウスが停止したときに勝手にアイテム選択されるので
					//	メッセージ処理をスキップする
					return;

				case Win32API.WM_PAINT:
					//	ソート方法が変更されていたらソートする
					if (m_NeedSort)
					{
						SortItems(m_SortColumn, m_Sorting);
					}
					//	デフォルトの描画処理
					base.WndProc(ref m);
					//	描画されたので変更関係フラグをクリア
					m_Modified = false;
					m_NeedSort = false;
					return;

				case Win32API.WM_CONTEXTMENU:
					//	コンテキストメニュー処理
					{
						Win32API.POINT pt = new Win32API.POINT(m.LParam.ToInt32());

						IntPtr hHeaderCtrl = LvGetHeader();
						if (m.WParam == hHeaderCtrl)
						{
							//	ヘッダ右クリックされたのならイベントを発生させる
							Win32API.ScreenToClient(hHeaderCtrl, ref pt);

							int nCols = this.Columns.Count;
							ColumnHeaderEx column = null;
							for (int i = 0; i < nCols; i++)
							{
								Win32API.RECT rc;
								Win32API.SendMessage(hHeaderCtrl, Win32API.HDM_GETITEMRECT, new IntPtr(i), out rc);
								if (rc.PtInRect(pt))
									column = this.Columns[i];
							}
							ColumnContextMenu(column);
							return;
						}
						else if (m.WParam == this.Handle)
						{
							//	リストビュー右クリック時のイベント
							Win32API.ScreenToClient(this.Handle, ref pt);
							int iItem = HitTestItem(pt.x, pt.y);
							if (0 <= iItem)
							{
								ItemContextMenu(m_Items[iItem]);
							}
						}
					}
					break;

				case Win32API.WM_NOTIFY:
					{
						Win32API.NMHDR hdr = (Win32API.NMHDR)m.GetLParam(typeof(Win32API.NMHDR));
						switch (hdr.code)
						{
							case Win32API.HDN_ENDTRACKA:
							case Win32API.HDN_ENDTRACKW:
								{
									//	カラムヘッダ幅変更後処理
									unsafe
									{
										Win32API.NMHEADER_Unsafe* inf = (Win32API.NMHEADER_Unsafe*)m.LParam;
										if (inf->pitem != null)
										{
											//	変更されたカラムの幅をセットする
											m_Columns.VisibleColumns[inf->iItem].WidthInternal = inf->pitem->cxy;
										}
									}
								}
								break;
						}
					}
					break;

				case (Win32API.WM_NOTIFY + Win32API.WM_REFLECT):
					{
						Win32API.NMHDR hdr = (Win32API.NMHDR)m.GetLParam(typeof(Win32API.NMHDR));
						switch (hdr.code)
						{
							case Win32API.LVN_ITEMCHANGED: OnLvnItemChanged(m.LParam); break;
							case Win32API.LVN_ODSTATECHANGED: OnLvnOdStateChanged(m.LParam); break;
							case Win32API.LVN_GETDISPINFOW: OnLvnGetDispInfo(m.LParam); return;
							case Win32API.LVN_COLUMNCLICK: OnLvnColumnClick(m.LParam); break;
						}
					}
					break;
			}
			base.WndProc(ref m);
		}

		/// <summary>
		/// OnHandleDestroyed オーバーライド。
		/// </summary>
		protected override void OnHandleDestroyed(EventArgs e)
		{
			//	コントロール破棄時には設定を保存
			SaveSetting();
			//	イメージリストからデリゲートが呼ばれないようにする
			UnlinkImageList();
			base.OnHandleDestroyed(e);
		}
		#endregion

		#region イベントハンドラ

		/// <summary>
		/// LVN_ITEMCHANGED イベントを処理する。
		/// </summary>
		/// <param name="lParam"></param>
		private unsafe void OnLvnItemChanged(IntPtr lParam)
		{
			Win32API.NMLISTVIEW* pNmlv = (Win32API.NMLISTVIEW*)lParam;
			bool oldsel = (pNmlv->uOldState & Win32API.LVIS_SELECTED) != 0;
			bool newsel = (pNmlv->uNewState & Win32API.LVIS_SELECTED) != 0;
			if (oldsel != newsel)
			{
				SelectionModified();
			}
		}

		/// <summary>
		/// LVN_ODSTATECHANGED イベントを処理する。
		/// </summary>
		/// <param name="lParam"></param>
		private unsafe void OnLvnOdStateChanged(IntPtr lParam)
		{
			Win32API.NMLVODSTATECHANGE* pNmlv = (Win32API.NMLVODSTATECHANGE*)lParam;
			bool oldsel = (pNmlv->uOldState & Win32API.LVIS_SELECTED) != 0;
			bool newsel = (pNmlv->uNewState & Win32API.LVIS_SELECTED) != 0;
			if (oldsel != newsel)
			{
				SelectionModified();
			}
		}

		/// <summary>
		/// LVN_GETDISPINFO イベントを処理する。
		/// </summary>
		private unsafe void OnLvnGetDispInfo(IntPtr lParam)
		{
			Win32API.NMLVDISPINFOW* inf = (Win32API.NMLVDISPINFOW*)lParam;

			int iItem = inf->iItem;
			if (m_Items.Count <= iItem)
				return; // アイテム番号が無効
			if (m_Columns.VisibleColumns.Count <= inf->iSubItem)
				return; // サブアイテム番号が無効

			ListViewItemEx item = m_Items[iItem];

			if ((inf->mask & Win32API.LVIF_TEXT) != 0)
			{
				//	テキストを取得
				ColumnHeaderEx col = m_Columns.VisibleColumns[inf->iSubItem];
				string text = col.GetItemText(item);
				if (text != null)
				{
					int n = System.Math.Min(inf->cchTextMax - 1, text.Length);

					int i;
					char* pText = inf->pszText;
					for (i = 0; i < n; i++)
					{
						pText[i] = text[i];
					}
					pText[i] = '\0';
				}
			}

			if ((inf->mask & Win32API.LVIF_IMAGE) != 0)
			{
				//	イメージ番号を取得
				inf->iImage = item.ImageIndex;
			}
		}

		/// <summary>
		/// LVN_COLUMNCLICK イベントを処理する。
		/// </summary>
		private void OnLvnColumnClick(IntPtr lParam)
		{
			int iSubItem;
			unsafe
			{
				Win32API.NMLISTVIEW* pNmlv = (Win32API.NMLISTVIEW*)lParam;
				iSubItem = pNmlv->iSubItem;
			}

			int iColumn = m_Columns.VisibleColumns[iSubItem].Index;
			ColumnClickEventArgs e = new ColumnClickEventArgs(iColumn);
			OnColumnClick(e);
		}

		/// <summary>
		/// 大イメージリストハンドル再作成ハンドら。
		/// </summary>
		private void LargeImageList_RecreateHandle(object sender, EventArgs e)
		{
			SetLargeImageListHandle(m_LargeImageList != null ? m_LargeImageList.Handle : IntPtr.Zero);
			Invalidate();
		}

		/// <summary>
		/// 小イメージリストハンドル再作成ハンドら。
		/// </summary>
		private void SmallImageList_RecreateHandle(object sender, EventArgs e)
		{
			SetSmallImageListHandle(m_SmallImageList != null ? m_SmallImageList.Handle : IntPtr.Zero);
			Invalidate();
		}

		#endregion
	}

	/// <summary>
	///	ListViewExInternal 用のカラムコレクションクラス。
	/// </summary>
	public class ColumnHeaderCollectionEx : IList<ColumnHeaderEx>, ICollection<ColumnHeaderEx>, IEnumerable<ColumnHeaderEx>
	{
		#region フィールド
		private ListViewEx m_Owner;
		private List<ColumnHeaderEx> m_Columns = new List<ColumnHeaderEx>();
		private List<ColumnHeaderEx> m_VisibleColumns = new List<ColumnHeaderEx>(); // 可視カラムリスト　リストビューのヘッダに表示されているカラムと一致していなければならない
		#endregion

		#region コンストラクタ
		/// <summary>
		///	コンストラクタ。
		/// </summary>
		/// <param name="owner">このコレクションを所有するリストビュー。</param>
		public ColumnHeaderCollectionEx(ListViewEx owner)
		{
			m_Owner = owner;
		}
		#endregion

		#region プロパティ
		/// <summary>
		///	全てのカラム一覧のインデクサ。
		/// </summary>
		public ColumnHeaderEx this[int index]
		{
			get
			{
				return m_Columns[index];
			}
		}

		/// <summary>
		///	可視状態カラム一覧を取得する。
		/// </summary>
		public IList<ColumnHeaderEx> VisibleColumns
		{
			get
			{
				return m_VisibleColumns.AsReadOnly();
			}
		}

		/// <summary>
		///	アイテム数の取得。
		/// </summary>
		public int Count
		{
			get
			{
				return m_Columns.Count;
			}
		}

		/// <summary>
		///	読み取り専用フラグの取得。
		/// </summary>
		public bool IsReadOnly
		{
			get
			{
				return false;
			}
		}
		#endregion

		#region メソッド
		/// <summary>
		/// アイテム追加。
		/// </summary>
		public virtual void Add(ColumnHeaderEx item)
		{
			Insert(m_Columns.Count, item);
		}

		/// <summary>
		///	アイテム挿入。
		/// </summary>
		public virtual void Insert(int index, ColumnHeaderEx item)
		{
			if(item.ListView != null)
			{
				throw new System.ArgumentException(string.Format("複数の場所にカラムヘッダ '{0}' を追加することはできません。", item.Text));
			}

			//	データインデックスが指定されていない場合は自動で設定
			if (item.DataColIndex < 0)
			{
				if (0 < index && index - 1 < m_Columns.Count)
				{
					//	直前のアイテムのデータインデックス＋１とする
					item.DataColIndex = m_Columns[index - 1].DataColIndex + 1;
				}
				else
				{
					item.DataColIndex = 0;
				}
			}

			//	もしアイテムが可視状態希望ならリストビューのカラムヘッダと可視アイテム一覧へ追加する
			if (item.Visible)
			{
				//	直前の可視状態のカラムを取得
				ColumnHeaderEx preVisibleCol = FindPreviousVisibleColumn(index);
				int iVisibleCol = preVisibleCol != null ? m_VisibleColumns.IndexOf(preVisibleCol) + 1 : 0;
				//	可視アイテムリストへ追加
				InsertVisibleColumn(iVisibleCol, item);
			}

			//	アイテム全体の一覧へ挿入
			m_Columns.Insert(index, item);

			//	アイテムの所有者を設定
			Link(item);

			//	変更があったことをリストビューへ通知
			Modify();
		}

		/// <summary>
		/// 指定されたアイテムを取り除く。
		/// </summary>
		public virtual bool Remove(ColumnHeaderEx item)
		{
			int index = m_Columns.IndexOf(item);
			if (index < 0)
				return false;
			RemoveAt(index);
			return true;
		}

		/// <summary>
		///	指定されたインデックスのアイテムを取り除く。
		/// </summary>
		public virtual void RemoveAt(int index)
		{
			ColumnHeaderEx item = m_Columns[index];

			//	全アイテム一覧から削除
			m_Columns.RemoveAt(index);

			//	可視アイテム一覧から削除
			RemoveVisibleColumn(item);

			Unlink(item);

			//	変更があったことをリストビューへ通知
			Modify();
		}

		/// <summary>
		///	クリア。
		/// </summary>
		public virtual void Clear()
		{
			for (int i = m_Columns.Count - 1; 0 <= i; i--)
			{
				ColumnHeaderEx item = m_Columns[i];
				int iVisibleCol = m_VisibleColumns.IndexOf(item);

				//	全体一覧から削除
				m_Columns.RemoveAt(i);

				//	可視アイテム一覧にあったら削除
				if (0 <= iVisibleCol)
				{
					m_VisibleColumns.RemoveAt(iVisibleCol);
					m_Owner.LvDeleteColumn(iVisibleCol);
				}

				Unlink(item);
			}
			
			//	変更があったことをリストビューへ通知
			Modify();
		}

		/// <summary>
		/// 指定されたアイテムのインデックス番号を取得する。
		/// </summary>
		/// <param name="item">アイテム。</param>
		/// <returns>このコレクション内にアイテムが存在している:インデックス番号、存在していない:-1。</returns>
		public int IndexOf(ColumnHeaderEx item)
		{
			return m_Columns.IndexOf(item);
		}

		/// <summary>
		/// 指定されたアイテムの可視カラム一覧内でのインデックス番号を取得する。
		/// </summary>
		/// <param name="item">アイテム。</param>
		/// <returns>このコレクション内にアイテムが存在している:インデックス番号、存在していない:-1。</returns>
		public int VisibleItemIndex(ColumnHeaderEx item)
		{
			return m_VisibleColumns.IndexOf(item);
		}

		/// <summary>
		///	指定されたアイテムを含んでいるか調べる。
		/// </summary>
		public bool Contains(ColumnHeaderEx item)
		{
			return m_Columns.Contains(item);
		}

		/// <summary>
		/// 指定された配列へコピーする。
		/// </summary>
		/// <param name="array">コピー先の配列。</param>
		/// <param name="arrayIndex">コピー開始位置のインデックス番号。</param>
		public void CopyTo(ColumnHeaderEx[] array, int arrayIndex)
		{
			m_Columns.CopyTo(array, arrayIndex);
		}

		/// <summary>
		///	イテレータを取得。
		/// </summary>
		public virtual IEnumerator<ColumnHeaderEx> GetEnumerator()
		{
			return m_Columns.GetEnumerator();
		}

		/// <summary>
		/// 指定されたデータインデックス番号でカラムを検索する。
		/// </summary>
		/// <param name="dataColIndex">データインデックス番号。</param>
		/// <returns>見つかった:カラムオブジェクト、見つからなかった:null。</returns>
		public ColumnHeaderEx FindByDataColIndex(int dataColIndex)
		{
			foreach (ColumnHeaderEx col in m_Columns)
			{
				if (col.DataColIndex == dataColIndex)
					return col;
			}
			return null;
		}
		#endregion

		#region アセンブリ内公開
		/// <summary>
		/// カラムのデータインデックス番号変更イベントハンドラ。
		/// </summary>
		internal void OnColumnModified(ColumnHeaderEx sender)
		{
		}

		/// <summary>
		/// カラム可視状態変更イベントハンドラ。
		/// </summary>
		internal void OnColumnVisibleChanged(ColumnHeaderEx sender)
		{
			ColumnHeaderEx column = (ColumnHeaderEx)sender;
			if (column.Visible == true)
			{
				ColumnHeaderEx prevHeader = FindPreviousVisibleColumn(column);
				if (prevHeader == null)
				{
					InsertVisibleColumn(0, column);
				}
				else
				{
					int prevIndex = m_VisibleColumns.IndexOf(prevHeader);
					InsertVisibleColumn(prevIndex + 1, column);
				}
			}
			else
			{
				RemoveVisibleColumn(column);
			}
			Modify();
		}

		/// <summary>
		/// カラムのデータインデックス番号変更イベントハンドラ。
		/// </summary>
		internal void OnColumnColDataIndexChanged(ColumnHeaderEx sender)
		{
			Modify();
		}
		#endregion

		#region 内部メソッド
		/// <summary>
		/// 指定されたカラムにリンクする。
		/// </summary>
		protected void Link(ColumnHeaderEx column)
		{
			column.ListView = m_Owner;
		}

		/// <summary>
		/// 指定されたカラムとのリンクを切断する。
		/// </summary>
		protected void Unlink(ColumnHeaderEx column)
		{
			column.ListView = null;
		}

		/// <summary>
		/// 変更されたことをリストビューコントロールへ通知する。
		/// </summary>
		private void Modify()
		{
			if (m_Owner != null)
				m_Owner.OnColumnCollectionChanged(this);
		}

		/// <summary>
		/// 指定されたインデックスのカラムの前にある可視のカラムを検索する。
		/// </summary>
		/// <param name="index">検索開始するカラムインデックス番号。</param>
		/// <returns>見つかった:カラムオブジェクト、見つからなかった:null。</returns>
		private ColumnHeaderEx FindPreviousVisibleColumn(int index)
		{
			index--;
			while (0 <= index)
			{
				ColumnHeaderEx c = m_Columns[index];
				if (c.Visible)
					return c;
				index--;
			}
			return null;
		}

		/// <summary>
		/// 指定されたカラムの前にある可視のカラムを検索する。
		/// </summary>
		/// <param name="column">検索開始位置を指定するカラム。</param>
		/// <returns>見つかった:カラムオブジェクト、見つからなかった:null。</returns>
		private ColumnHeaderEx FindPreviousVisibleColumn(ColumnHeaderEx column)
		{
			return FindPreviousVisibleColumn(m_Columns.IndexOf(column));
		}

		/// <summary>
		/// 指定されたカラムを可視リストへ追加する。
		/// </summary>
		/// <param name="iVisIndex">可視リスト内でのインデックス番号。</param>
		/// <param name="item">挿入されるアイテム。</param>
		private void InsertVisibleColumn(int iVisIndex, ColumnHeaderEx item)
		{
			//	リストビューに挿入
			Win32API.LVCOLUMNW_Set lvc = item.GetLvColumn();
			int result = m_Owner.LvInsertColumn(iVisIndex, ref lvc);
			if (result < 0)
			{
				throw new Exception(string.Format("リストビューにアイテム '{0}' を挿入できませんでした。", item.Text));
			}
			//	可視アイテム一覧へ挿入
			m_VisibleColumns.Insert(iVisIndex, item);
		}

		/// <summary>
		/// 指定されたインデックスのカラムを可視リストから取り除く。
		/// </summary>
		/// <param name="iVisIndex">可視リスト内でのインデックス番号。</param>
		private void RemoveVisibleColumn(int iVisIndex)
		{
			//	可視アイテム一覧から取り除く
			m_VisibleColumns.RemoveAt(iVisIndex);
			//	リストビューから削除
			m_Owner.LvDeleteColumn(iVisIndex);
		}

		/// <summary>
		/// 指定されたカラムを可視リストから取り除く。
		/// </summary>
		/// <param name="item">取り除くカラム。</param>
		private void RemoveVisibleColumn(ColumnHeaderEx item)
		{
			int index = m_VisibleColumns.IndexOf(item);
			if(index < 0)
				return;
			RemoveVisibleColumn(index);
		}

		/// <summary>
		/// インデクサ。隠蔽用。
		/// </summary>
		ColumnHeaderEx IList<ColumnHeaderEx>.this[int index]
		{
			get
			{
				return this[index];
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		/// <summary>
		/// イテレータを取得。隠蔽用。
		/// </summary>
		[DispId(-4)]
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		#endregion
	}

	/// <summary>
	///	カラムの非表示をサポートしたカラムクラス。
	/// </summary>
	public class ColumnHeaderEx
	{
		#region フィールド
		private ColumnHeaderCollectionEx m_Owner;
		private ListViewEx m_ListView;

		private bool m_Visible = true;
		private string m_ItemTextMember;
		private string m_ItemValueMember;
		private int m_DataColIndex = -1;
		private string m_Text;
		private int m_Width = 60;
		private HorizontalAlignment m_TextAlign = HorizontalAlignment.Left;
		#endregion

		#region コンストラクタ
		/// <summary>
		///	コンストラクタ。
		/// </summary>
		public ColumnHeaderEx()
		{
		}

		/// <summary>
		///	コンストラクタ。
		/// </summary>
		/// <param name="text">表示文字列。</param>
		public ColumnHeaderEx(string text)
		{
			m_Text = text;
		}

		/// <summary>
		///	コンストラクタ。
		/// </summary>
		/// <param name="text">表示文字列。</param>
		/// <param name="textAlign">アライメント。</param>
		public ColumnHeaderEx(string text, HorizontalAlignment textAlign)
		{
			m_Text = text;
			m_TextAlign = textAlign;
		}

		/// <summary>
		///	コンストラクタ。
		/// </summary>
		/// <param name="text">表示文字列。</param>
		/// <param name="width">カラム幅。</param>
		/// <param name="textAlign">アライメント。</param>
		public ColumnHeaderEx(string text, int width, HorizontalAlignment textAlign)
		{
			m_Text = text;
			m_Width = width;
			m_TextAlign = textAlign;
		}

		/// <summary>
		///	コンストラクタ。
		/// </summary>
		/// <param name="text">表示文字列。</param>
		/// <param name="width">カラム幅。</param>
		/// <param name="textAlign">アライメント。</param>
		/// <param name="itemTextMember">この列でアイテムの名称として表示するメンバ名。</param>
		/// <param name="itemValueMember">この列でアイテムのデータ値として使用するメンバ名。</param>
		public ColumnHeaderEx(string text, int width, HorizontalAlignment textAlign, string itemTextMember, string itemValueMember)
		{
			m_Text = text;
			m_Width = width;
			m_TextAlign = textAlign;
			m_ItemTextMember = itemTextMember;
			m_ItemValueMember = itemValueMember;
		}
		#endregion

		#region プロパティ
		/// <summary>
		/// 現在表示されている列を基準とした列の表示順序を取得します。
		/// </summary>
		[Localizable(true)]
		[RefreshProperties(RefreshProperties.Repaint)]
		public int DisplayIndex
		{
			get
			{
				int iVisIndex = this.VisibleItemIndex;
				if (iVisIndex < 0)
					return -1;

				Win32API.LVCOLUMNW lvc = new Win32API.LVCOLUMNW();
				lvc.mask = Win32API.LVCF_ORDER;
				m_ListView.LvGetColumn(iVisIndex, ref lvc);
				return lvc.iOrder;
			}
			set
			{
				int iVisIndex = this.VisibleItemIndex;
				if (iVisIndex < 0)
					return;

				Win32API.LVCOLUMNW lvc = new Win32API.LVCOLUMNW();
				lvc.mask = Win32API.LVCF_ORDER;
				lvc.iOrder = value;
				m_ListView.LvSetColumn(iVisIndex, ref lvc);
			}
		}

		/// <summary>
		/// ColumnHeaderCollectionEx 内でのインデックス番号の取得。
		/// </summary>
		[Browsable(false)]
		public int Index
		{
			get
			{
				if(m_Owner == null)
					return -1;
				return m_Owner.IndexOf(this);
			}
		}

		/// <summary>
		/// ColumnHeaderCollectionEx の可視カラム一覧内でのインデックス番号の取得。
		/// </summary>
		[Browsable(false)]
		public int VisibleItemIndex
		{
			get
			{
				if(m_Owner == null)
					return -1;
				return m_Owner.VisibleItemIndex(this);
			}
		}

		/// <summary>
		/// このオブジェクトを格納している ListViewEx の取得。
		/// </summary>
		[Browsable(false)]
		public ListViewEx ListView
		{
			get { return m_ListView; }
			internal set
			{
				m_ListView = value;
				if (value != null)
				{
					m_Owner = value.Columns;
				}
				else
				{
					m_Owner = null;
				}
			}
		}

		/// <summary>
		///	列ヘッダーに表示されるテキストを取得または設定します。
		/// </summary>
		[Localizable(true)]
		public string Text
		{
			get { return m_Text; }
			set
			{
				if (value == m_Text)
					return;
				m_Text = value;

				//	このアイテムが表示されているならカラムのテキストを変更
				int iVisIndex = this.VisibleItemIndex;
				if (0 <= iVisIndex)
				{
					Win32API.LVCOLUMNW_Set lvc = new Win32API.LVCOLUMNW_Set();
					lvc.mask = Win32API.LVCF_TEXT;
					lvc.pszText = m_Text;
					m_ListView.LvSetColumn(iVisIndex, ref lvc);
				}
			}
		}

		/// <summary>
		/// System.Windows.Forms.ColumnHeader で表示されたテキストの水平方向の配置を取得または設定します。
		/// </summary>
		[Localizable(true)]
		public HorizontalAlignment TextAlign
		{
			get { return m_TextAlign; }
			set
			{
				if (value == m_TextAlign)
					return;
				m_TextAlign = value;

				//	このアイテムが表示されているならカラムテキストの水平方向配置を変更
				int iVisIndex = this.VisibleItemIndex;
				if (0 <= iVisIndex)
				{
					Win32API.LVCOLUMNW lvc = new Win32API.LVCOLUMNW();
					lvc.mask = Win32API.LVCF_FMT;
					m_ListView.LvGetColumn(iVisIndex, ref lvc);
					lvc.fmt |= GetTextAlignLvcFmt();
					m_ListView.LvSetColumn(iVisIndex, ref lvc);
				}
			}
		}

		/// <summary>
		/// 列の幅を取得または設定します。
		/// </summary>
		[Localizable(true)]
		[DefaultValue(60)]
		public int Width
		{
			get { return m_Width; }
			set
			{
				if (value == m_Width)
					return;
				m_Width = value;

				//	このアイテムが表示されているならカラムの幅を変更
				int iVisIndex = this.VisibleItemIndex;
				if (0 <= iVisIndex)
				{
					m_ListView.LvSetColumnWidth(iVisIndex, m_Width);
				}
			}
		}

		/// <summary>
		///	このカラムの可視状態の取得と設定。
		/// </summary>
		public bool Visible
		{
			get { return m_Visible; }
			set
			{
				if (value == m_Visible)
					return;
				m_Visible = value;
				if (m_Owner != null)
				{
					m_Owner.OnColumnVisibleChanged(this);
				}
			}
		}

		/// <summary>
		/// このカラムが表示するデータのインデックス番号の取得と設定。
		/// </summary>
		public int DataColIndex
		{
			get { return m_DataColIndex; }
			set
			{
				if (value == m_DataColIndex)
					return;
				m_DataColIndex = value;
				if (m_Owner != null)
				{
					m_Owner.OnColumnColDataIndexChanged(this);
				}
			}
		}

		/// <summary>
		/// このカラムでアイテムの名称として使用するメンバの取得と設定。これを設定すると挿入されたアイテムの指定さればメンバが名称として表示されるようになる。DataColIndex よりこっちが優先。
		/// </summary>
		public string ItemTextMember
		{
			get { return m_ItemTextMember; }
			set
			{
				if (value == m_ItemTextMember)
					return;
				m_ItemTextMember = value;
				if (m_Owner != null)
				{
					m_Owner.OnColumnColDataIndexChanged(this);
				}
			}
		}

		/// <summary>
		/// このカラムでアイテムのデータ値として使用するメンバの取得と設定。これを設定するとアイテムソート時のキーに指定されたメンバが使用されるようになる。DataColIndex よりこっちが優先。
		/// </summary>
		public string ItemValueMember
		{
			get { return m_ItemValueMember; }
			set
			{
				if (value == m_ItemValueMember)
					return;
				m_ItemValueMember = value;
				if (m_Owner != null)
				{
					m_Owner.OnColumnColDataIndexChanged(this);
				}
			}
		}
		#endregion

		#region メソッド
		/// <summary>
		/// 指定されたアイテムをこのカラムで表示する時の文字列を取得する。
		/// </summary>
		/// <param name="item">文字列を取得したいアイテム。</param>
		/// <returns>文字列または　null。</returns>
		public string GetItemText(ListViewItemEx item)
		{
			if (m_ItemTextMember != null)
			{
				PropertyInfo pi = item.GetType().GetProperty(m_ItemTextMember);
				if(pi == null)
					return null;
				object obj = pi.GetValue(item, null);
				if (obj == null)
					return null;
				return obj.ToString();
			}
			else
			{
				if (item.Cols.Count <= m_DataColIndex)
					return null;
				return item.Cols[m_DataColIndex].Text;
			}
		}

		/// <summary>
		/// 指定されたアイテムのこのカラム用のデータ値を取得する。
		/// </summary>
		/// <param name="item">データ値を取得したいアイテム。</param>
		/// <returns>オブジェクトまたは　null。</returns>
		public object GetItemValue(ListViewItemEx item)
		{
			if (m_ItemValueMember != null)
			{
				PropertyInfo pi = item.GetType().GetProperty(m_ItemValueMember);
				if (pi == null)
					return null;
				return pi.GetValue(item, null);
			}
			else
			{
				if (item.Cols.Count <= m_DataColIndex)
					return null;
				return item.Cols[m_DataColIndex].Value;
			}
		}
		#endregion

		#region アセンブリ内公開
		/// <summary>
		/// 現在の設定から Win32API.LVCOLUMNW_Set を取得する。
		/// </summary>
		internal Win32API.LVCOLUMNW_Set GetLvColumn()
		{
			Win32API.LVCOLUMNW_Set lvc = new Win32API.LVCOLUMNW_Set();
			lvc.mask = Win32API.LVCF_TEXT | Win32API.LVCF_WIDTH | Win32API.LVCF_FMT;
			lvc.pszText = this.Text;
			lvc.cx = this.Width;
			lvc.fmt = GetTextAlignLvcFmt();
			//if (0 <= this.DisplayIndex)
			//{
			//    lvc.mask |= Win32API.LVCF_ORDER;
			//    lvc.iOrder = this.DisplayIndex;
			//}
			return lvc;
		}
		#endregion

		#region 内部プロパティ
		/// <summary>
		/// 幅の取得と設定。
		/// </summary>
		internal int WidthInternal
		{
			get { return m_Width; }
			set { m_Width = value; }
		}
		#endregion

		#region 内部メソッド
		/// <summary>
		/// 現在の水平方向テキスト配置の状態から LVCOLUMN の fmt 値を取得。
		/// </summary>
		private int GetTextAlignLvcFmt()
		{
			switch (this.TextAlign)
			{
				case HorizontalAlignment.Left: return Win32API.LVCFMT_LEFT;
				case HorizontalAlignment.Center: return Win32API.LVCFMT_CENTER;
				case HorizontalAlignment.Right: return Win32API.LVCFMT_RIGHT;
				default: return 0;
			}
		}
		#endregion
	}

	/// <summary>
	///	ListViewEx 用のカラムコレクションクラス。
	/// </summary>
	public class ListViewItemCollectionEx : IList<ListViewItemEx>, ICollection<ListViewItemEx>, IEnumerable<ListViewItemEx>
	{
		#region フィールド
		private ListViewEx m_Owner;
		private List<ListViewItemEx> m_Items = new List<ListViewItemEx>();
		#endregion

		#region コンストラクタ
		/// <summary>
		///	コンストラクタ。
		/// </summary>
		/// <param name="owner">このコレクションを所有するリストビュー。</param>
		public ListViewItemCollectionEx(ListViewEx owner)
		{
			m_Owner = owner;
		}
		#endregion

		#region プロパティ
		/// <summary>
		///	全てのカラム一覧のインデクサ。
		/// </summary>
		public ListViewItemEx this[int index]
		{
			get
			{
				return m_Items[index];
			}
		}

		/// <summary>
		///	アイテム数の取得。
		/// </summary>
		public int Count
		{
			get
			{
				return m_Items.Count;
			}
		}

		/// <summary>
		///	読み取り専用フラグの取得。
		/// </summary>
		public bool IsReadOnly
		{
			get
			{
				return false;
			}
		}
		#endregion

		#region メソッド
		/// <summary>
		/// アイテム追加。
		/// </summary>
		public virtual void Add(ListViewItemEx item)
		{
			Insert(m_Items.Count, item);
		}

		/// <summary>
		/// アイテム追加。
		/// </summary>
		public virtual void Add(params ListViewItemEx.Col[] cols)
		{
			Add(new ListViewItemEx(cols));
		}

		/// <summary>
		/// アイテム追加。
		/// </summary>
		public virtual void AddRange(IEnumerable<ListViewItemEx> collection)
		{
			InsertRange(m_Items.Count, collection);
		}

		/// <summary>
		///	アイテム挿入。
		/// </summary>
		public virtual void Insert(int index, ListViewItemEx item)
		{
			if (item.ListView != null)
			{
				throw new System.ArgumentException(string.Format("複数の場所にリストビューアイテムを追加することはできません。"));
			}

			//	アイテム一覧へ挿入
			m_Items.Insert(index, item);

			//	アイテムの所有者を設定
			Link(item);

			//	変更があったことをリストビューへ通知
			Modify();
		}

		/// <summary>
		///	アイテム挿入。
		/// </summary>
		public virtual void InsertRange(int index, IEnumerable<ListViewItemEx> collection)
		{
			foreach (ListViewItemEx item in collection)
			{
				if (item.ListView != null)
				{
					throw new System.ArgumentException(string.Format("複数の場所にリストビューアイテムを追加することはできません。"));
				}

				//	アイテムの所有者を設定
				Link(item);
			}

			//	アイテム一覧へ挿入
			m_Items.InsertRange(index, collection);

			//	変更があったことをリストビューへ通知
			Modify();
		}

		/// <summary>
		/// 指定されたアイテムを取り除く。
		/// </summary>
		public virtual bool Remove(ListViewItemEx item)
		{
			int index = m_Items.IndexOf(item);
			if (index < 0)
				return false;
			RemoveAt(index);
			return true;
		}

		/// <summary>
		///	指定されたインデックスのアイテムを取り除く。
		/// </summary>
		public virtual void RemoveAt(int index)
		{
			ListViewItemEx item = m_Items[index];

			if (m_Owner != null)
				m_Owner.OnBeforeItemRemove(this, index, 1);

			//	全アイテム一覧から削除
			m_Items.RemoveAt(index);

			//	アイテムのリンクを切断
			Unlink(item);

			//	変更があったことをリストビューへ通知
			if (m_Owner != null)
				m_Owner.OnAfterItemRemove(this, index, 1);
		}

		/// <summary>
		///	クリア。
		/// </summary>
		public virtual void Clear()
		{
			if (m_Owner != null)
				m_Owner.OnBeforeItemRemove(this, 0, m_Items.Count);

			//	アイテムのリンクを切断
			for (int i = 0, n = m_Items.Count; i < n; i++)
			{
				Unlink(m_Items[i]);
			}
			m_Items.Clear();

			//	変更があったことをリストビューへ通知
			if (m_Owner != null)
				m_Owner.OnAfterItemRemove(this, 0, m_Items.Count);
		}

		/// <summary>
		/// 指定されたアイテムのインデックス番号を取得する。
		/// </summary>
		/// <param name="item">アイテム。</param>
		/// <returns>このコレクション内にアイテムが存在している:インデックス番号、存在していない:-1。</returns>
		public int IndexOf(ListViewItemEx item)
		{
			return m_Items.IndexOf(item);
		}

		/// <summary>
		///	指定されたアイテムを含んでいるか調べる。
		/// </summary>
		public bool Contains(ListViewItemEx item)
		{
			return m_Items.Contains(item);
		}

		/// <summary>
		/// 指定された配列へコピーする。
		/// </summary>
		/// <param name="array">コピー先の配列。</param>
		/// <param name="arrayIndex">コピー開始位置のインデックス番号。</param>
		public void CopyTo(ListViewItemEx[] array, int arrayIndex)
		{
			m_Items.CopyTo(array, arrayIndex);
		}

		/// <summary>
		///	イテレータを取得。
		/// </summary>
		public virtual IEnumerator<ListViewItemEx> GetEnumerator()
		{
			return m_Items.GetEnumerator();
		}

		/// <summary>
		///	アイテムの並びをソートします。
		/// </summary>
		public void Sort()
		{
			BeforeItemMove();
			m_Items.Sort();
			AfterItemMove();
			Modify();
		}
		/// <summary>
		///	アイテムの並びをソートします。
		/// </summary>
		public void Sort(Comparison<ListViewItemEx> comparison)
		{
			BeforeItemMove();
			m_Items.Sort(comparison);
			AfterItemMove();
			Modify();
		}
		/// <summary>
		///	アイテムの並びをソートします。
		/// </summary>
		public void Sort(IComparer<ListViewItemEx> comparer)
		{
			BeforeItemMove();
			m_Items.Sort(comparer);
			AfterItemMove();
			Modify();
		}
		/// <summary>
		///	指定された位置から指定された個数のアイテムの並びをソートします。
		/// </summary>
		public void Sort(int index, int count, IComparer<ListViewItemEx> comparer)
		{
			BeforeItemMove();
			m_Items.Sort(index, count, comparer);
			AfterItemMove();
			Modify();
		}
		#endregion

		#region アセンブリ内公開
		#endregion

		#region 内部メソッド
		/// <summary>
		/// 指定されたカラムにリンクする。
		/// </summary>
		protected void Link(ListViewItemEx column)
		{
			column.ListView = m_Owner;
		}

		/// <summary>
		/// 指定されたカラムとのリンクを切断する。
		/// </summary>
		protected void Unlink(ListViewItemEx column)
		{
			column.ListView = null;
		}

		/// <summary>
		/// 変更されたことをリストビューコントロールへ通知する。
		/// </summary>
		private void Modify()
		{
			if (m_Owner != null)
				m_Owner.OnItemCollectionChanged(this);
		}

		/// <summary>
		/// アイテム移動処理の前に呼び出す。
		/// </summary>
		private void BeforeItemMove()
		{
			//	選択状態を覚える
			for(int i = 0, n = m_Items.Count; i < n; i++)
			{
				m_Items[i].SelectedChache = m_Owner.GetItemSelected(i);
			}
		}

		/// <summary>
		/// アイテム移動処理の後に呼び出す。
		/// </summary>
		private void AfterItemMove()
		{
			//	保存されていた選択状態を復元する
			for (int i = 0, n = m_Items.Count; i < n; i++)
			{
				m_Owner.SetItemSelected(i, m_Items[i].SelectedChache);
			}
		}

		/// <summary>
		/// インデクサ。隠蔽用。
		/// </summary>
		ListViewItemEx IList<ListViewItemEx>.this[int index]
		{
			get
			{
				return this[index];
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		/// <summary>
		/// イテレータを取得。隠蔽用。
		/// </summary>
		[DispId(-4)]
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		#endregion
	}

	/// <summary>
	///	ListViewEx 用のリストビューアイテム。列ごとに表示文字列と実データを持つ。
	/// </summary>
	public class ListViewItemEx
	{
		#region クラス宣言
		/// <summary>
		///	カラムのコレクション。
		/// </summary>
		public class ColCollection : List<Col>
		{
			#region フィールド
			/// <summary>
			/// 親となるコンテナ。
			/// </summary>
			protected ListViewItemEx m_Owner;
			#endregion

			#region コンストラクタ
			/// <summary>
			///	コンストラクタ。
			/// </summary>
			public ColCollection(ListViewItemEx owner)
			{
				m_Owner = owner;
			}
			#endregion

			#region プロパティ
			/// <summary>
			/// 指定されたインデックス位置のアイテムを取得します。
			/// </summary>
			public new Col this[int index]
			{
				get
				{
					return base[index];
				}
				set
				{
					Col col = base[index];
					if (value == col)
						return;
					if (col != null)
						col.ParentItem = null;
					base[index] = value;
					value.ParentItem = m_Owner;
					Modify();
				}
			}
			#endregion

			#region メソッド
			/// <summary>
			/// 変更されたことをグラフコントロールへ通知する。
			/// </summary>
			public void Modify()
			{
				if (m_Owner != null)
					m_Owner.OnColCollectionChanged(this);
			}

			/// <summary>
			///	アイテム追加します。
			/// </summary>
			public new void Add(Col item)
			{
				base.Add(item);
				item.ParentItem = m_Owner;
				Modify();
			}
			/// <summary>
			///	表示文字列と実データを指定してアイテム追加します。
			/// </summary>
			public void Add(string text, object value)
			{
				Col item = new Col(text, value);
				base.Add(item);
				item.ParentItem = m_Owner;
				Modify();
			}
			/// <summary>
			///	指定されたコレクション内のアイテムを追加します。
			/// </summary>
			public new void AddRange(IEnumerable<Col> collection)
			{
				base.AddRange(collection);
				foreach (Col item in collection)
				{
					item.ParentItem = m_Owner;
				}
				Modify();
			}
			/// <summary>
			///	クリアします。
			/// </summary>
			public new void Clear()
			{
				foreach (Col item in this)
				{
					item.ParentItem = null;
				}
				base.Clear();
				Modify();
			}
			/// <summary>
			///	各要素に対してしていされた処理を実行します。
			/// </summary>
			public new void ForEach(Action<Col> action)
			{
				base.ForEach(action);
			}
			/// <summary>
			///	指定されたインデックス位置にアイテムを挿入します。
			/// </summary>
			public new void Insert(int index, Col item)
			{
				base.Insert(index, item);
				item.ParentItem = m_Owner;
				Modify();
			}
			/// <summary>
			///	指定されたインデックス位置に指定されたコレクション内のアイテムを挿入します。
			/// </summary>
			public new void InsertRange(int index, IEnumerable<Col> collection)
			{
				base.InsertRange(index, collection);
				foreach (Col item in collection)
				{
					item.ParentItem = m_Owner;
				}
				Modify();
			}
			/// <summary>
			///	指定されたアイテムを取り除きます。
			/// </summary>
			public new bool Remove(Col item)
			{
				bool result = base.Remove(item);
				if (result)
				{
					item.ParentItem = null;
					Modify();
				}
				return result;
			}
			/// <summary>
			///	指定された処理にマッチしたアイテムを取り除きます。
			/// </summary>
			public new int RemoveAll(Predicate<Col> match)
			{
				foreach (Col item in this)
				{
					if (match(item as Col))
					{
						item.ParentItem = null;
					}
				}
				int result = base.RemoveAll(match);
				if (result != 0)
				{
					Modify();
				}
				return result;
			}
			/// <summary>
			///	指定されたインデックス位置のアイテムを取り除きます。
			/// </summary>
			public new void RemoveAt(int index)
			{
				this[index].ParentItem = null;
				base.RemoveAt(index);
				Modify();
			}
			/// <summary>
			///	指定されたインデックス位置から指定された個数のアイテムを取り除きます。
			/// </summary>
			public new void RemoveRange(int index, int count)
			{
				for (int i = 0; i < count; i++)
				{
					this[index + i].ParentItem = null;
				}
				base.RemoveRange(index, count);
				Modify();
			}
			/// <summary>
			///	コレクション内のアイテムの並びを反転させます。
			/// </summary>
			public new void Reverse()
			{
				base.Reverse();
				Modify();
			}
			/// <summary>
			///	指定されたインデックス位置から指定個数のアイテムの並びを反転させます。
			/// </summary>
			public new void Reverse(int index, int count)
			{
				base.Reverse(index, count);
				Modify();
			}
			/// <summary>
			///	アイテムの並びをソートします。
			/// </summary>
			public new void Sort()
			{
				base.Sort();
				Modify();
			}
			/// <summary>
			///	アイテムの並びをソートします。
			/// </summary>
			public new void Sort(Comparison<Col> comparison)
			{
				base.Sort(comparison);
				Modify();
			}
			/// <summary>
			///	アイテムの並びをソートします。
			/// </summary>
			public new void Sort(IComparer<Col> comparer)
			{
				base.Sort(comparer);
				Modify();
			}
			/// <summary>
			///	指定された位置から指定された個数のアイテムの並びをソートします。
			/// </summary>
			public new void Sort(int index, int count, IComparer<Col> comparer)
			{
				base.Sort(index, count, comparer);
				Modify();
			}

			/// <summary>
			///	コレクションの内容を指定された配列の内容と同じにする。
			/// </summary>
			/// <param name="cols"></param>
			public void FromArray(Col[] cols)
			{
				Clear();
				AddRange(cols);
			}
			#endregion
		}

		/// <summary>
		/// 各列の表示文字列と実データ。
		/// </summary>
		public class Col
		{
			#region フィールド
			private string m_Text;
			private object m_Value;
			private ListViewItemEx m_ParentItem;
			#endregion

			#region コンストラクタ
			/// <summary>
			/// コンストラクタ。
			/// </summary>
			public Col()
			{
			}

			/// <summary>
			///	コンストラクタ。表示文字列と実データで初期化する。
			/// </summary>
			/// <param name="text">表示文字列。</param>
			/// <param name="value">実データ。</param>
			public Col(string text, object value)
			{
				m_Text = text;
				m_Value = value;
			}
			#endregion

			#region プロパティ
			/// <summary>
			///	表示文字列の取得と設定。
			/// </summary>
			[Description("表示されるテキストを設定します。")]
			public string Text
			{
				get
				{
					return m_Text;
				}
				set
				{
					if (value == m_Text)
						return;
					m_Text = value;
					if (m_ParentItem != null)
					{
						m_ParentItem.OnColTextChanged(this);
					}
				}
			}

			/// <summary>
			/// 実データの取得と設定。
			/// </summary>
			[Description("実データを設定します。")]
			public object Value
			{
				get
				{
					return m_Value;
				}
				set
				{
					if (value == m_Value)
						return;
					m_Value = value;
					if (m_ParentItem != null)
					{
						m_ParentItem.OnColValueChanged(this);
					}
				}
			}
			#endregion

			#region 内部プロパティ
			/// <summary>
			/// 親 ListViewItemEx の取得と設定。
			/// </summary>
			internal ListViewItemEx ParentItem
			{
				get { return m_ParentItem; }
				set
				{
					if (m_ParentItem != null && value != null)
					{
						throw new Exception("Col クラスは複数のコレクションに挿入することはできません。");
					}
					m_ParentItem = value;
				}
			}
			#endregion
		}
		#endregion

		#region フィールド
		private ListViewEx m_ListView = null;
		private ColCollection m_Cols;
		private int m_Index;
		private int m_IndexModifiedCount = -1;
		private int m_ImageIndex = -1;
		private bool m_SelectedChache;
		#endregion

		#region コンストラクタ
		/// <summary>
		///	コンストラクタ。
		/// </summary>
		public ListViewItemEx()
		{
			m_Cols = new ColCollection(this);
		}

		/// <summary>
		///	コンストラクタ。指定された列データで初期化する。
		/// </summary>
		/// <param name="cols">列データ配列。</param>
		public ListViewItemEx(params Col[] cols)
			: this()
		{
			m_Cols.AddRange(cols);
		}
		#endregion

		#region プロパティ
		/// <summary>
		///	各列のデータ配列の取得と設定。
		/// </summary>
		public ColCollection Cols
		{
			get { return m_Cols; }
		}

		/// <summary>
		///	このアイテムの ListViewExInternal 内でのインデックス番号の取得。
		/// </summary>
		public int Index
		{
			get
			{
				if (m_ListView == null)
					return -1;
				//	毎回インデックス番号を検索するのは効率が悪いので何か変更があった場合のみ検索する
				int nCount = m_ListView.IndexModifiedCount;
				if (m_IndexModifiedCount != m_ListView.IndexModifiedCount)
				{
					m_IndexModifiedCount = nCount;
					m_Index = m_ListView.Items.IndexOf(this);
				}
				return m_Index;
			}
		}

		/// <summary>
		/// イメージインデックスの取得と設定。
		/// </summary>
		public int ImageIndex
		{
			get
			{
				return m_ImageIndex;
			}
			set
			{
				if (value == m_ImageIndex)
					return;
				m_ImageIndex = value;
				Modify();
			}
		}

		/// <summary>
		///	アイテムの選択状態の取得と設定。
		/// </summary>
		public bool Selected
		{
			get
			{
				int index = this.Index;
				if (index < 0)
					return false;
				return m_ListView.GetItemSelected(index);
			}
			set
			{
				int index = this.Index;
				if (index < 0)
					return;
				m_ListView.SetItemSelected(index, value);
			}
		}

		/// <summary>
		///	アイテムのフォーカス状態の取得と設定。
		/// </summary>
		public bool Focused
		{
			get
			{
				int index = this.Index;
				if (index < 0)
					return false;
				return m_ListView.GetItemFocused(index);
			}
			set
			{
				int index = this.Index;
				if (index < 0)
					return;
				m_ListView.SetItemFocused(index, value);
			}
		}

		/// <summary>
		/// 親リストビューの取得と設定。
		/// </summary>
		public ListViewEx ListView
		{
			get { return m_ListView; }
			internal set { m_ListView = value; }
		}
		#endregion

		#region メソッド
		/// <summary>
		/// 全カラムデータを一括設定する。
		/// </summary>
		/// <param name="cols">カラムデータ配列。</param>
		public void SetCols(params Col[] cols)
		{
			m_Cols.FromArray(cols);
		}

		/// <summary>
		/// アイテムに変更があった場合に呼び出す。
		/// </summary>
		public void Modify()
		{
			if (m_ListView != null)
			{
				m_ListView.OnItemChanged(this);
			}
		}

		/// <summary>
		/// 必要に応じてコントロールの内容をスクロールして、項目がコントロール内に確実に表示されるようにします。
		/// </summary>
		public void EnsureVisible()
		{
			int index = this.Index;
			if (index < 0)
				return;
			m_ListView.EnsureVisibleItem(index);
		}
		#endregion

		#region アセンブリ内部公開
		/// <summary>
		///	一時選択状態の取得と設定。同じアセンブリの内部からのみアクセス可能。
		/// </summary>
		internal bool SelectedChache
		{
			get { return m_SelectedChache; }
			set { m_SelectedChache = value; }
		}
		#endregion

		#region アセンブリ内部公開メソッド
		/// <summary>
		/// カラムデータコレクションが変更された時に呼び出される。
		/// </summary>
		internal void OnColCollectionChanged(ColCollection sender)
		{
			Modify();
		}

		/// <summary>
		/// カラムが何らかの変更を加えられた時に呼び出される。
		/// </summary>
		internal void OnColChanged(Col sender)
		{
			Modify();
		}

		/// <summary>
		/// カラムのテキストに変更を加えられた時に呼び出される。
		/// </summary>
		internal void OnColTextChanged(Col sender)
		{
			Modify();
		}

		/// <summary>
		/// カラムの実データが変更された時に呼び出される。
		/// </summary>
		internal void OnColValueChanged(Col sender)
		{
			if (m_ListView != null)
				m_ListView.OnItemValueChanged(this);
		}
		#endregion
	}

	/// <summary>
	///	ListViewEx 用の選択アイテムインデックス番号コレクションクラス。
	/// </summary>
	public class ListViewSelectedIndexCollectionEx : IList<int>, ICollection<int>, IEnumerable<int>
	{
		#region フィールド
		private ListViewEx m_Owner;
		private List<int> m_Items = new List<int>();
		private bool m_SelectionModified;
		#endregion

		#region コンストラクタ
		/// <summary>
		///	コンストラクタ。
		/// </summary>
		/// <param name="owner">このコレクションを所有するリストビュー。</param>
		public ListViewSelectedIndexCollectionEx(ListViewEx owner)
		{
			m_Owner = owner;
		}
		#endregion

		#region プロパティ
		/// <summary>
		///	全てのカラム一覧のインデクサ。
		/// </summary>
		public int this[int index]
		{
			get
			{
				Update();
				return m_Items[index];
			}
		}

		/// <summary>
		///	アイテム数の取得。
		/// </summary>
		public int Count
		{
			get
			{
				Update();
				return m_Items.Count;
			}
		}

		/// <summary>
		///	読み取り専用フラグの取得。
		/// </summary>
		public bool IsReadOnly
		{
			get
			{
				return true;
			}
		}
		#endregion

		#region メソッド
		/// <summary>
		/// 指定されたアイテムのインデックス番号を取得する。
		/// </summary>
		/// <param name="item">アイテム。</param>
		/// <returns>このコレクション内にアイテムが存在している:インデックス番号、存在していない:-1。</returns>
		public int IndexOf(int item)
		{
			Update();
			return m_Items.IndexOf(item);
		}

		/// <summary>
		///	指定されたアイテムを含んでいるか調べる。
		/// </summary>
		public bool Contains(int item)
		{
			Update();
			return m_Items.Contains(item);
		}

		/// <summary>
		/// 指定された配列へコピーする。
		/// </summary>
		/// <param name="array">コピー先の配列。</param>
		/// <param name="arrayIndex">コピー開始位置のインデックス番号。</param>
		public void CopyTo(int[] array, int arrayIndex)
		{
			Update();
			m_Items.CopyTo(array, arrayIndex);
		}

		/// <summary>
		///	イテレータを取得。
		/// </summary>
		public virtual IEnumerator<int> GetEnumerator()
		{
			Update();
			return m_Items.GetEnumerator();
		}
		#endregion

		#region アセンブリ内公開
		/// <summary>
		/// リストビューの選択状態が変化した時に ListViewEx から呼び出される。
		/// </summary>
		internal void SelectionModified()
		{
			m_SelectionModified = true;
		}
		#endregion

		#region 内部メソッド

		/// <summary>
		///	リストビューの選択状態が変更されていた場合にリストの内容を更新する。
		/// </summary>
		private void Update()
		{
			if (!m_SelectionModified)
				return;
			m_SelectionModified = false;
			m_Items.Clear();
			for (int i = 0, n = m_Owner.Items.Count; i < n; i++)
			{
				if (m_Owner.GetItemSelected(i))
					m_Items.Add(i);
			}
		}

		/// <summary>
		/// インデクサ。隠蔽用。
		/// </summary>
		int IList<int>.this[int index]
		{
			get
			{
				return this[index];
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		/// <summary>
		/// アイテム追加。
		/// </summary>
		void ICollection<int>.Add(int item)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		///	アイテム挿入。
		/// </summary>
		void IList<int>.Insert(int index, int item)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// 指定されたアイテムを取り除く。
		/// </summary>
		bool ICollection<int>.Remove(int item)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		///	指定されたインデックスのアイテムを取り除く。
		/// </summary>
		void IList<int>.RemoveAt(int index)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		///	クリア。
		/// </summary>
		void ICollection<int>.Clear()
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// イテレータを取得。隠蔽用。
		/// </summary>
		[DispId(-4)]
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		#endregion
	}


	/// <summary>
	///	ListViewEx 用の選択アイテムコレクションクラス。
	/// </summary>
	public class ListViewSelectedItemCollectionEx : IList<ListViewItemEx>, ICollection<ListViewItemEx>, IEnumerable<ListViewItemEx>
	{
		#region フィールド
		private ListViewItemCollectionEx m_Items;
		private ListViewSelectedIndexCollectionEx m_Indices;
		#endregion

		#region コンストラクタ
		/// <summary>
		///	コンストラクタ。
		/// </summary>
		/// <param name="owner">このコレクションを所有するリストビュー。</param>
		public ListViewSelectedItemCollectionEx(ListViewEx owner)
		{
			m_Items = owner.Items;
			m_Indices = owner.SelectedIndices;
		}
		#endregion

		#region プロパティ
		/// <summary>
		///	全てのカラム一覧のインデクサ。
		/// </summary>
		public ListViewItemEx this[int index]
		{
			get
			{
				return m_Items[m_Indices[index]];
			}
		}

		/// <summary>
		///	アイテム数の取得。
		/// </summary>
		public int Count
		{
			get
			{
				return m_Indices.Count;
			}
		}

		/// <summary>
		///	読み取り専用フラグの取得。
		/// </summary>
		public bool IsReadOnly
		{
			get
			{
				return true;
			}
		}
		#endregion

		#region メソッド
		/// <summary>
		/// 指定されたアイテムのインデックス番号を取得する。
		/// </summary>
		/// <param name="item">アイテム。</param>
		/// <returns>このコレクション内にアイテムが存在している:インデックス番号、存在していない:-1。</returns>
		public int IndexOf(ListViewItemEx item)
		{
			ListViewItemCollectionEx items = m_Items;
			foreach (int i in m_Indices)
			{
				if (items[i] == item)
					return i;
			}
			return -1;
		}

		/// <summary>
		///	指定されたアイテムを含んでいるか調べる。
		/// </summary>
		public bool Contains(ListViewItemEx item)
		{
			return 0 <= IndexOf(item);
		}

		/// <summary>
		/// 指定された配列へコピーする。
		/// </summary>
		/// <param name="array">コピー先の配列。</param>
		/// <param name="arrayIndex">コピー開始位置のインデックス番号。</param>
		public void CopyTo(ListViewItemEx[] array, int arrayIndex)
		{
			ListViewSelectedIndexCollectionEx indices = m_Indices;
			ListViewItemCollectionEx items = m_Items;
			for (int i = 0, n = indices.Count; i < n; i++)
			{
				array[arrayIndex + i] = items[indices[i]];
			}
		}

		/// <summary>
		///	イテレータを取得。
		/// </summary>
		public virtual IEnumerator<ListViewItemEx> GetEnumerator()
		{
			foreach (int i in m_Indices)
			{
				yield return m_Items[i];
			}
		}
		#endregion

		#region アセンブリ内公開
		#endregion

		#region 内部メソッド
		/// <summary>
		/// インデクサ。隠蔽用。
		/// </summary>
		ListViewItemEx IList<ListViewItemEx>.this[int index]
		{
			get
			{
				return this[index];
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		/// <summary>
		/// アイテム追加。
		/// </summary>
		void ICollection<ListViewItemEx>.Add(ListViewItemEx item)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		///	アイテム挿入。
		/// </summary>
		void IList<ListViewItemEx>.Insert(int index, ListViewItemEx item)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// 指定されたアイテムを取り除く。
		/// </summary>
		bool ICollection<ListViewItemEx>.Remove(ListViewItemEx item)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		///	指定されたインデックスのアイテムを取り除く。
		/// </summary>
		void IList<ListViewItemEx>.RemoveAt(int index)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		///	クリア。
		/// </summary>
		void ICollection<ListViewItemEx>.Clear()
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// イテレータを取得。隠蔽用。
		/// </summary>
		[DispId(-4)]
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		#endregion
	}
}
