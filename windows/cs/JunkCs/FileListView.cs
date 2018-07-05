using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;

namespace Jk
{
	/// <summary>
	///	ファイルリストビューコントロール。
	/// </summary>
	public partial class FileListView : ListViewEx
	{
		#region フィールド
		private ShellItem m_FolderShellItem;
		private FileListViewSelectedShellItemCollection m_SelectedShellItems;
		private FileSystemWatcher m_Watcher;
		#endregion

		#region クラス、構造体
		/// <summary>
		///	ソート用アイテム比較処理実行クラス。
		/// </summary>
		public new class ItemComparer : ListViewEx.ItemComparer
		{
			/// <summary>
			/// 比較する。
			/// </summary>
			/// <param name="item1">アイテム１．</param>
			/// <param name="item2">アイテム２．</param>
			/// <returns>item1＜item2:-1、item1==item2:0、item1＞item2:1。</returns>
			public override int Compare(ListViewItemEx item1, ListViewItemEx item2)
			{
				int cmp;
				bool ascending = this.Order == SortOrder.Ascending;

				FileListViewItem flvi1 = item1 as FileListViewItem;
				FileListViewItem flvi2 = item2 as FileListViewItem;
				if (flvi1 == null || flvi1.Cols == null || flvi2 == null || flvi2.Cols == null)
				{
					//	どちらかのアイテムが FileListViewItem では内場合は FileListViewItem以外<FileListViewItem になるようにする
					if (flvi1 == null && flvi2 == null)
						return 0;
					else if (flvi1 == null)
						return ascending ? -1 : 1;
					else
						return ascending ? 1 : -1;
				}

				ShellItem shi1 = flvi1.ShellItem;
				ShellItem shi2 = flvi2.ShellItem;
				if (shi1.ItemType != shi2.ItemType)
				{
					//	アイテムの種類が異なる場合は種類に応じて大小判定
					int i1 = (int)shi1.ItemType;
					int i2 = (int)shi2.ItemType;
					if (i1 == i2)
						cmp = 0;
					else if (i1 < i2)
						cmp = -1;
					else
						cmp = 1;
					return ascending ? cmp : -cmp;
				}

				if (shi1.ItemType == ShellItemType.Drive)
				{
					//	ドライブ同士を比較する場合はドライブ名で比較
					cmp = shi1.Path.CompareTo(shi2.Path);
					return ascending ? cmp : -cmp;
				}

				return base.Compare(item1, item2);
			}
		}
		#endregion

		#region イベント関係宣言

		#region イベント引数クラス
		/// <summary>
		///	リストビューアイテム作成イベント引数。
		/// </summary>
		public class ItemCreateEventArgs : EventArgs
		{
			/// <summary>
			/// コンストラクタ。
			/// </summary>
			public ItemCreateEventArgs(ShellItem shellItem)
			{
				this.ShellItem = shellItem;
				this.Cancel = false;
			}

			/// <summary>
			///	リストビューアイテムに対応するシェルアイテムの取得。
			/// </summary>
			public readonly ShellItem ShellItem;

			/// <summary>
			/// 作成したリストビューアイテムを設定する。デフォルトのアイテムオブジェクト以外を使用する場合にこのメンバに独自のアイテムオブジェクトを設定してください。
			/// </summary>
			public FileListViewItem Item = null;

			/// <summary>
			///	リストにアイテムを追加しない場合はこのメンバを true に設定してください。
			/// </summary>
			public bool Cancel;
		}
		/// <summary>
		///	リストビューアイテム更新イベント引数。
		/// </summary>
		public class ItemUpdateEventArgs : EventArgs
		{
			/// <summary>
			/// コンストラクタ。
			/// </summary>
			public ItemUpdateEventArgs(FileListViewItem item)
			{
				this.Item = item;
				this.Cancel = false;
			}

			/// <summary>
			///	更新するアイテム。
			/// </summary>
			public readonly FileListViewItem Item;

			/// <summary>
			///	デフォルトのアイテム更新処理をキャンセルする場合にはこのメンバを true に設定してください。
			/// </summary>
			public bool Cancel;
		}
		/// <summary>
		///	アイテム実行イベント引数。
		/// </summary>
		public class ItemExecuteEventArgs : EventArgs
		{
			private FileListViewItem m_Item;
			private bool m_Cancel;

			/// <summary>
			/// コンストラクタ。
			/// </summary>
			public ItemExecuteEventArgs(FileListViewItem item)
			{
				m_Item = item;
			}

			/// <summary>
			/// 実行するアイテムの取得と設定。
			/// </summary>
			public FileListViewItem Item
			{
				get { return m_Item; }
				set { m_Item = value; }
			}

			/// <summary>
			/// デフォルトの処理を中止したい場合に true をセットする。
			/// </summary>
			public bool Cancel
			{
				get { return m_Cancel; }
				set { m_Cancel = value; }
			}
		}
		/// <summary>
		///	複数アイテム実行イベント引数。
		/// </summary>
		public class ItemsExecuteEventArgs : EventArgs
		{
			private IList<FileListViewItem> m_Items;
			private bool m_Cancel;

			/// <summary>
			/// コンストラクタ。
			/// </summary>
			public ItemsExecuteEventArgs(IList<FileListViewItem> items)
			{
				m_Items = items;
			}

			/// <summary>
			/// 実行するアイテムのコレクションの取得と設定。
			/// </summary>
			public IList<FileListViewItem> Items
			{
				get { return m_Items; }
				set { m_Items = value; }
			}

			/// <summary>
			/// デフォルトの処理を中止したい場合に true をセットする。
			/// </summary>
			public bool Cancel
			{
				get { return m_Cancel; }
				set { m_Cancel = value; }
			}
		}
		#endregion

		#region デリゲート
		/// <summary>
		///	リストビューアイテム作成イベントハンドラデリゲート。
		/// </summary>
		public delegate void ItemCreateEventHandler(object sender, ItemCreateEventArgs e);
		/// <summary>
		///	リストビューアイテム更新イベントハンドラデリゲート。
		/// </summary>
		public delegate void ItemUpdateEventHandler(object sender, ItemUpdateEventArgs e);
		/// <summary>
		///	アイテム実行イベントハンドラデリゲート。
		/// </summary>
		public delegate void ItemExecuteEventHandler(object sender, ItemExecuteEventArgs e);
		/// <summary>
		///	複数アイテム実行イベントハンドラデリゲート。
		/// </summary>
		public delegate void ItemsExecuteEventHandler(object sender, ItemsExecuteEventArgs e);
		#endregion

		#region イベント
		/// <summary>
		/// リストビューアイテム作成イベント。
		/// </summary>
		[Category("FileListView")]
		[Description("シェルアイテムに対応するリストビューアイテム作成時に発生するイベントです。このイベントを処理することでフィルタリングが可能です。")]
		public event ItemCreateEventHandler ItemCreate;
		/// <summary>
		/// リストビューアイテム更新イベント。
		/// </summary>
		[Category("FileListView")]
		[Description("リストビューアイテム更新時に発生するイベントです。")]
		public event ItemUpdateEventHandler ItemUpdate;
		/// <summary>
		///	アイテム実行イベント。
		/// </summary>
		[Category("FileListView")]
		[Description("ダブルクリックや Enter キーなどで選択されているアイテムを実行時に発生するイベントです。")]
		public event ItemExecuteEventHandler ItemExecute;
		/// <summary>
		///	複数アイテム実行イベント。
		/// </summary>
		[Category("FileListView")]
		[Description("アイテムダブルクリックや、アイテム選択後 Enter キーなどで選択アイテムを実行時に発生するイベントです。")]
		public event ItemsExecuteEventHandler ItemsExecute;
		#endregion

		#endregion

		#region コンストラクタ
		/// <summary>
		/// コンストラクタ。
		/// </summary>
		public FileListView()
		{
			InitializeComponent();

			m_SelectedShellItems = new FileListViewSelectedShellItemCollection(this);

			base.ListViewItemSorter = new ItemComparer();
		}
		#endregion

		#region プロパティ
		/// <summary>
		/// コントロール内のすべての項目を格納するコレクションを取得します。
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new FileListViewItemCollection Items
		{
			get
			{
				return (FileListViewItemCollection)base.Items;
			}
		}

		/// <summary>
		///	選択されているアイテムのコレクションの取得。
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new FileListViewSelectedItemCollection SelectedItems
		{
			get { return (FileListViewSelectedItemCollection)base.SelectedItems; }
		}

		/// <summary>
		///	親フォルダのシェルアイテムの取得と設定。
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ShellItem FolderShellItem
		{
			get
			{
				return m_FolderShellItem;
			}
			set
			{
				if (value == m_FolderShellItem)
					return;
				SetFolderShellItem(value);
			}
		}

		/// <summary>
		///	親フォルダのパスの取得と設定。
		/// </summary>
		[Category("FileListView")]
		[Description("内容を表示するフォルダのフルパス名を指定します。")]
		public string FolderPath
		{
			get
			{
				if (this.FolderShellItem == null)
					return null;
				return this.FolderShellItem.Path;
			}
			set
			{
				if (value == this.FolderPath)
					return;
				if (value == null || value.Length == 0)
					this.FolderShellItem = null;
				else
					this.FolderShellItem = new ShellItem(value);
			}
		}

		/// <summary>
		/// 選択されているシェルアイテムのコレクションを取得する。
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public FileListViewSelectedShellItemCollection SelectedShellItems
		{
			get { return m_SelectedShellItems; }
		}
		#endregion

		#region メソッド
		/// <summary>
		/// システムイメージリストをこのツリービューに設定する。
		/// </summary>
		public void SetSystemImageList()
		{
			//	APIのSendMessageを使って直接システムイメージリストハンドルを設定
			SetLargeImageListHandle(SystemImageList.LargeImageList);
			SetSmallImageListHandle(SystemImageList.SmallImageList);
		}

		/// <summary>
		///	既定のカラムを作成する。呼び出し以前に存在しているカラムは削除される。
		/// </summary>
		public void CreateDefaultColumns()
		{
			this.Columns.Clear();

			ColumnHeaderEx c;

			c = new ColumnHeaderEx();
			c.Text = "名前";
			c.Width = 200;
			c.ItemTextMember = "DisplayName";
			c.ItemValueMember = "DisplayName";
			this.Columns.Add(c);

			c = new ColumnHeaderEx();
			c.Text = "サイズ";
			c.Width = 100;
			c.TextAlign = HorizontalAlignment.Right;
			c.ItemTextMember = "FileSizeText";
			c.ItemValueMember = "FileSize";
			this.Columns.Add(c);

			c = new ColumnHeaderEx();
			c.Text = "種類";
			c.Width = 200;
			c.ItemTextMember = "FileType";
			c.ItemValueMember = "FileType";
			this.Columns.Add(c);

			c = new ColumnHeaderEx();
			c.Text = "更新日時";
			c.Width = 100;
			c.ItemTextMember = "DateUpdatedText";
			c.ItemValueMember = "DateUpdated";
			this.Columns.Add(c);
		}

		/// <summary>
		/// ファイルリストを更新する。
		/// カラムが作成されていなければデフォルトのカラムを作成する。
		/// </summary>
		public void UpdateFileList()
		{
			//	カラムが作成されていなければカラムを初期化する
			if (this.Columns.Count == 0)
				CreateDefaultColumns();

			//	とりあえずクリア
			Items.Clear();

			//	ソート方法が設定されていなければソート方法を初期化
			if (this.SortColumn == null)
			{
				if (this.Columns.Count != 0)
				{
					this.SortColumn = this.Columns[0];
				}
			}

			//	表示するフォルダが指定されていない場合は何もしない
			if (this.FolderShellItem == null)
				return;

			//	設定されているフォルダ内のシェルアイテムに対応するリストビューアイテムを作成
			List<ListViewItemEx> items = new List<ListViewItemEx>();
			List<ShellItem> children = this.FolderShellItem.GetSubItems();
			foreach (ShellItem shi in children)
			{
			    FileListViewItem item = CreateItem(shi);
			    if (item != null)
			    {
					UpdateItem(item);
					items.Add(item);
			    }
			}
			this.Items.AddRange(items);
		}

		/// <summary>
		/// 指定されたシェルアイテムに対応するリストビューアイテムを検索する
		/// </summary>
		/// <param name="shellItem">検索したいシェルアイテム。</param>
		/// <returns>見つかった:リストビューアイテム、見つからなかった:null。</returns>
		public FileListViewItem Find(ShellItem shellItem)
		{
			if(m_FolderShellItem == null)
				return null;
			if(!m_FolderShellItem.IsChild(shellItem, true))
				return null;

			foreach(ListViewItemEx item in this.Items)
			{
				FileListViewItem flvi = item as FileListViewItem;
				if(flvi == null)
					continue;

				if(flvi.ShellItem.IsSameShellItem(shellItem))
					return flvi;
			}

			return null;
		}

		/// <summary>
		///	EndUpdate メソッドが呼び出されるまで、コントロールを描画しないようにする。 
		/// </summary>
		public void BeginUpdate()
		{
			Win32API.SendMessage(this.Handle, Win32API.WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
		}

		/// <summary>
		///	BeginUpdate メソッドによって中断されていた描画を再開します。 
		/// </summary>
		public void EndUpdate()
		{
			Win32API.SendMessage(this.Handle, Win32API.WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
		}
		#endregion

		#region 内部メソッド
		/// <summary>
		///	指定されたシェルアイテムをリストに追加する。
		/// </summary>
		/// <param name="shellItem">シェルアイテム。</param>
		private void AddItem(ShellItem shellItem)
		{
			FileListViewItem item = CreateItem(shellItem);
			if (item != null)
			{
				UpdateItem(item);
				this.Items.Add(item);
			}
		}

		/// <summary>
		/// 指定されたシェルアイテムに対応するリストビューアイテムを作成する。
		/// </summary>
		/// <param name="shellItem">シェルアイテム。</param>
		/// <returns>アイテムを表示する場合:リストビューアイテム、表示しない場合:null。</returns>
		private FileListViewItem CreateItem(ShellItem shellItem)
		{
			FileListViewItem item = null;

			if (this.ItemCreate != null)
			{
				ItemCreateEventArgs e = new ItemCreateEventArgs(shellItem);
				this.ItemCreate(this, e);
				if (e.Cancel)
					return null;
				item = e.Item;
			}

			if (item == null)
				item = new FileListViewItem(shellItem);

			return item;
		}

		/// <summary>
		/// 指定されたリストビューアイテムを更新する。
		/// </summary>
		/// <param name="item">更新したいリストビューアイテム。</param>
		private void UpdateItem(FileListViewItem item)
		{
			if (this.ItemUpdate != null)
			{
				ItemUpdateEventArgs e = new ItemUpdateEventArgs(item);
				this.ItemUpdate(this, e);
				if (e.Cancel)
					return;
			}
				
			item.UpdateFileInfo();
		}

		/// <summary>
		///	表示するフォルダを設定する。
		/// </summary>
		private void SetFolderShellItem(ShellItem shellItem)
		{
			m_FolderShellItem = shellItem;
			UpdateFileList();

			//	以前のファイル監視オブジェクトを破棄
			if (m_Watcher != null)
			{
				m_Watcher.EnableRaisingEvents = false;
				m_Watcher.Dispose();
				m_Watcher = null;
			}

			//	フォルダがファイルシステムならファイル監視を開始
			if (m_FolderShellItem.IsFileSystem)
			{
				if (m_Watcher == null)
				{
					try
					{
						m_Watcher = new FileSystemWatcher();

						m_Watcher.Path = m_FolderShellItem.Path;
						m_Watcher.IncludeSubdirectories = false;
						m_Watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size | NotifyFilters.LastWrite;
						m_Watcher.Created += new FileSystemEventHandler(FileCreated);
						m_Watcher.Deleted += new FileSystemEventHandler(FileDeleted);
						m_Watcher.Renamed += new RenamedEventHandler(FileRenamed);
						m_Watcher.Changed += new FileSystemEventHandler(FileChanged);
						m_Watcher.EnableRaisingEvents = true;
					}
					catch (ArgumentException)
					{
						if (m_Watcher != null)
						{
							m_Watcher.Dispose();
							m_Watcher = null;
						}
					}
					catch (FileNotFoundException)
					{
						if (m_Watcher != null)
						{
							m_Watcher.Dispose();
							m_Watcher = null;
						}
					}
				}
			}
		}

		/// <summary>
		/// リストから指定されたパスに対応するアイテムを検索する。
		/// </summary>
		private FileListViewItem FindPath(string path)
		{
			ListViewItemCollectionEx items = this.Items;
			int nItems = items.Count;
			for(int i = 0; i < nItems; i++)
			{
				FileListViewItem flvi = items[i] as FileListViewItem;
				if (flvi != null)
				{
					if (string.Compare(flvi.FilePath, path, true) == 0)
						return flvi;
				}
			}
			return null;
		}

		/// <summary>
		/// ファイルが作成された時に呼び出される。
		/// </summary>
		private void FileCreated(string path)
		{
			try
			{
				ShellItem si = new ShellItem(path);
				AddItem(si);
			}
			catch (Exception)
			{
			}
		}

		/// <summary>
		/// ファイルが削除された時に呼び出される。
		/// </summary>
		private void FileDeleted(string path)
		{
			FileListViewItem item = FindPath(path);
			if (item != null)
			{
				this.Items.Remove(item);
			}
		}

		/// <summary>
		/// ファイル名が変更された時に呼び出される。
		/// </summary>
		private void FileRenamed(string oldPath, string path)
		{
			FileListViewItem item = FindPath(oldPath);
			if (item != null)
			{
				item.UpdateName(path);
				UpdateItem(item);
			}
		}

		/// <summary>
		///	ファイルの日付など属性が変わった時に呼び出される。
		/// </summary>
		private void FileChanged(string path)
		{
			FileListViewItem item = FindPath(path);
			if (item != null)
			{
				UpdateItem(item);
			}
		}

		/// <summary>
		/// ファイル監視オブジェクトを処分する。
		/// </summary>
		private void DisposeWatcher()
		{
			//	ファイル監視オブジェクトはもう使わない
			if (m_Watcher != null)
			{
				m_Watcher.Dispose();
				m_Watcher = null;
			}
		}
		#endregion

		#region イベントトリガ
		/// <summary>
		/// ItemExecute イベントを発生させる。
		/// </summary>
		protected virtual void OnItemExecute(ItemExecuteEventArgs e)
		{
			if (ItemExecute != null)
				ItemExecute(this, e);
		}

		/// <summary>
		/// ItemsExecute イベントを発生させる。
		/// </summary>
		protected virtual void OnItemsExecute(ItemsExecuteEventArgs e)
		{
			if (ItemsExecute != null)
				ItemsExecute(this, e);
			if (e.Cancel)
				return;
			foreach (FileListViewItem item in e.Items)
			{
				ItemExecuteEventArgs e2 = new ItemExecuteEventArgs(item);
				OnItemExecute(e2);
				if (e2.Cancel)
					break;
			}
		}
		#endregion

		#region オーバーライド
		/// <summary>
		/// WndProc オーバーライド。
		/// </summary>
		protected override void WndProc(ref Message m)
		{
			base.WndProc(ref m);
			if (m.Msg == Win32API.WM_CREATE)
			{
				//	このタイミングではだめらしいので後で呼び出すように登録する
				BeginInvoke(new MethodInvoker(delegate
				{
					SetSystemImageList();
				}));
			}
		}

		/// <summary>
		/// キーボード押下トリガをオーバーライド。
		/// </summary>
		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);

			//	リターンキーがおされたら
			//	選択状態にあるアイテムを実行する
			if (e.KeyCode == Keys.Enter && this.SelectedItems.Count != 0)
			{
			    List<FileListViewItem> items = new List<FileListViewItem>();
			    foreach (FileListViewItem item in this.SelectedItems)
			    {
			        items.Add(item);
			    }
			    ItemsExecuteEventArgs e2 = new ItemsExecuteEventArgs(items);
			    OnItemsExecute(e2);
			}
		}

		/// <summary>
		///	マウスボタンダブルクリックトリガをオーバーライド。
		/// </summary>
		protected override void OnMouseDoubleClick(MouseEventArgs e)
		{
			base.OnMouseDoubleClick(e);

			//	フォルダアイテムが左ボタンダブルクリックされたら
			//	アイテムを実行する
			if (e.Button == MouseButtons.Left && this.SelectedItems.Count != 0)
			{
				int iItem = HitTestItem(e.X, e.Y);
				if (0 <= iItem)
				{
					FileListViewItem flvi = this.Items[iItem];
					List<FileListViewItem> items = new List<FileListViewItem>();
					items.Add(flvi);
					ItemsExecuteEventArgs e2 = new ItemsExecuteEventArgs(items);
					OnItemsExecute(e2);
				}
			}
		}

		/// <summary>
		/// OnHandleDestroyed オーバーライド。
		/// </summary>
		protected override void OnHandleDestroyed(EventArgs e)
		{
			//	ファイル監視オブジェクトはもう使わない
			DisposeWatcher();
			base.OnHandleDestroyed(e);
		}

		/// <summary>
		///	CreateItemCollection オーバーライド。
		/// </summary>
		protected override ListViewItemCollectionEx CreateItemCollection()
		{
			return new FileListViewItemCollection(this);
		}

		/// <summary>
		/// CreateSelectedItemCollection オーバーライド。
		/// </summary>
		protected override ListViewSelectedItemCollectionEx CreateSelectedItemCollection()
		{
			return new FileListViewSelectedItemCollection(this);
		}
		#endregion

		#region イベントハンドラ
		/// <summary>
		/// フォルダツリービューのアイテム選択変更イベント。
		/// </summary>
		private void FolderTreeView_AfterSelect(Object sender, TreeViewEventArgs e)
		{
			FolderTreeNode ftn = e.Node as FolderTreeNode;
			if (ftn == null)
				return;

			FolderShellItem = ftn.ShellItem;
		}
		#endregion

		#region ファイルシステム監視イベントハンドラ
		/// <summary>
		/// ファイルが作成された時に呼び出される。
		/// </summary>
		private void FileCreated(object source, FileSystemEventArgs e)
		{
			BeginInvoke(new MethodInvoker(delegate
			{
				FileCreated(e.FullPath);
			}));
		}

		/// <summary>
		/// ファイルが削除された時に呼び出される。
		/// </summary>
		private void FileDeleted(object source, FileSystemEventArgs e)
		{
			BeginInvoke(new MethodInvoker(delegate
			{
				FileDeleted(e.FullPath);
			}));
		}

		/// <summary>
		/// ファイル名が変更された時に呼び出される。
		/// </summary>
		private void FileRenamed(object sender, RenamedEventArgs e)
		{
			BeginInvoke(new MethodInvoker(delegate
			{
				FileRenamed(e.OldFullPath, e.FullPath);
			}));
		}

		/// <summary>
		///	ファイルの日付など属性が変わった時に呼び出される。
		/// </summary>
		private void FileChanged(object sender, FileSystemEventArgs e)
		{
			BeginInvoke(new MethodInvoker(delegate
			{
				FileChanged(e.FullPath);
			}));
		}
		#endregion
	}

	/// <summary>
	///	FileListViewItem のコレクション。
	/// </summary>
	public class FileListViewItemCollection : ListViewItemCollectionEx
	{
		#region コンストラクタ
		/// <summary>
		///	コンストラクタ。
		/// </summary>
		public FileListViewItemCollection(FileListView owner)
			: base(owner)
		{
		}
		#endregion

		#region プロパティ
		/// <summary>
		/// 指定されたインデックス位置のアイテムを取得します。
		/// </summary>
		public new FileListViewItem this[int index]
		{
			get
			{
				return (FileListViewItem)base[index];
			}
		}
		#endregion
	}

	/// <summary>
	/// ファイルリストビューのアイテム。
	/// </summary>
	public class FileListViewItem : ListViewItemEx
	{
		#region フィールド
		private ShellItem m_ShellItem;
		private bool m_NeedGetFileInfo = true;
		private long m_FileSize = 0;
		private bool m_FileSizeAvailable = false;
		private DateTime m_DateUpdated;
		private bool m_DateUpdatedAvailable = false;
		#endregion

		#region コンストラクタ
		/// <summary>
		///	コンストラクタ。指定されたシェルアイテムを参照するアイテムとして初期化する。
		/// </summary>
		/// <param name="sh">シェルアイテム。</param>
		public FileListViewItem(ShellItem sh)
		{
			m_ShellItem = sh;
			base.ImageIndex = sh.IconIndex;
		}

		/// <summary>
		///	コンストラクタ。指定されたパスを参照するアイテムとして初期化する。
		/// </summary>
		/// <param name="path">フルパス名。</param>
		public FileListViewItem(string path) : this(new ShellItem(path))
		{
		}
		#endregion

		#region プロパティ
		/// <summary>
		///	このリストビューアイテムのシェルアイテムの取得。
		/// </summary>
		public ShellItem ShellItem
		{
			get
			{
				return m_ShellItem;
			}
		}

		/// <summary>
		/// ファイル表示名の取得。
		/// </summary>
		public string DisplayName
		{
			get
			{
				return m_ShellItem.DisplayName;
			}
		}

		/// <summary>
		/// ファイル名の取得。
		/// </summary>
		public string FileName
		{
			get
			{
				return m_ShellItem.FileName;
			}
		}

		/// <summary>
		/// ファイルのフルパス名の取得。
		/// </summary>
		public string FilePath
		{
			get
			{
				return m_ShellItem.Path;
			}
		}

		/// <summary>
		/// ファイルの親ディレクトリのフルパス名の取得。
		/// </summary>
		public string ParentPath
		{
			get
			{
				return System.IO.Path.GetDirectoryName(m_ShellItem.Path);
			}
		}

		/// <summary>
		/// ファイルサイズの取得。
		/// </summary>
		public long FileSize
		{
			get
			{
				GetFileInfo();
				return m_FileSize;
			}
		}

		/// <summary>
		/// ファイル種類の取得。
		/// </summary>
		public string FileType
		{
			get
			{
				return m_ShellItem.TypeName;
			}
		}

		/// <summary>
		/// ファイル更新日付の取得。
		/// </summary>
		public DateTime DateUpdated
		{
			get
			{
				GetFileInfo();
				return m_DateUpdated;
			}
		}

		/// <summary>
		///	ファイルサイズの表示文字列の取得。
		/// </summary>
		public string FileSizeText
		{
			get
			{
				GetFileInfo();
				if(m_FileSizeAvailable)
					return string.Format("{0} KB", (m_FileSize + 1023) >> 10);
				else
					return null;
			}
		}

		/// <summary>
		/// ファイル更新日付の表示文字列の取得。
		/// </summary>
		public string DateUpdatedText
		{
			get
			{
				GetFileInfo();
				if (m_DateUpdatedAvailable)
					return m_DateUpdated.ToString("yyyy/MM/dd hh:mm:ss");
				else
					return null;
			}
		}
		#endregion

		#region メソッド
		/// <summary>
		///	ファイル情報を更新する必要があるときに呼び出す。
		/// </summary>
		public void UpdateFileInfo()
		{
			m_NeedGetFileInfo = true;
		}
		#endregion

		#region 内部メソッド
		/// <summary>
		/// 新しいパスで名前などを更新。
		/// </summary>
		internal void UpdateName(string path)
		{
			try
			{
				m_ShellItem = new ShellItem(path);
			}
			catch (Exception)
			{
				//	指定されたパスに対応する ShellItem が作れなかったら普通にリターン
				return;
			}

			m_NeedGetFileInfo = true;
			base.ImageIndex = m_ShellItem.IconIndex;
		}

		/// <summary>
		/// m_NeedGetFileInfo が true の場合にファイル名以外のファイル情報を取得する。
		/// </summary>
		private void GetFileInfo()
		{
			if(!m_NeedGetFileInfo)
				return;
			m_NeedGetFileInfo = false;

			if (m_ShellItem.ItemType == ShellItemType.Folder)
			{
				ShellItem.FileInfo fi = m_ShellItem.GetFileInfo();
				m_DateUpdated = fi.LastWriteTime;
				m_DateUpdatedAvailable = true;
			}
			else if (m_ShellItem.ItemType == ShellItemType.File)
			{
				ShellItem.FileInfo fi = m_ShellItem.GetFileInfo();
				m_FileSize = fi.FileSize;
				m_FileSizeAvailable = true;
				m_DateUpdated = fi.LastWriteTime;
				m_DateUpdatedAvailable = true;
			}
		}
		#endregion
	}

	/// <summary>
	/// 選択されている FileListViewItem のコレクション。
	/// </summary>
	public class FileListViewSelectedItemCollection : ListViewSelectedItemCollectionEx
	{
		#region コンストラクタ
		/// <summary>
		/// コンストラクタ。
		/// </summary>
		public FileListViewSelectedItemCollection(FileListView owner)
			: base(owner)
		{
		}
		#endregion

		#region プロパティ
		/// <summary>
		/// インデクサ。
		/// </summary>
		public new FileListViewItem this[int index]
		{
			get
			{
				return (FileListViewItem)base[index];
			}
		}
		#endregion
	}

	/// <summary>
	/// FileListView 内で選択されている ShellItem のコレクション。
	/// </summary>
	public class FileListViewSelectedShellItemCollection : IList<ShellItem>, ICollection<ShellItem>, IEnumerable<ShellItem>
	{
		#region フィールド
		private FileListViewSelectedItemCollection m_Items;
		#endregion

		#region コンストラクタ
		/// <summary>
		/// コンストラクタ。
		/// </summary>
		public FileListViewSelectedShellItemCollection(FileListView owner)
		{
			m_Items = owner.SelectedItems;
		}
		#endregion

		#region プロパティ
		/// <summary>
		/// インデクサ。
		/// </summary>
		public ShellItem this[int index]
		{
			get
			{
				return m_Items[index].ShellItem;
			}
		}

		/// <summary>
		///	選択されているアイテム数の取得。
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
		public int IndexOf(ShellItem item)
		{
			FileListViewSelectedItemCollection items = m_Items;
			for (int i = 0, n = items.Count; i < n; i++)
			{
				ShellItem si = items[i].ShellItem;
				if (si == item)
					return i;
			}
			return -1;
		}

		/// <summary>
		///	指定されたアイテムを含んでいるか調べる。
		/// </summary>
		public bool Contains(ShellItem item)
		{
			return 0 <= IndexOf(item);
		}

		/// <summary>
		/// 指定された配列へコピーする。
		/// </summary>
		/// <param name="array">コピー先の配列。</param>
		/// <param name="arrayIndex">コピー開始位置のインデックス番号。</param>
		public void CopyTo(ShellItem[] array, int arrayIndex)
		{
			FileListViewSelectedItemCollection items = m_Items;
			for (int i = 0, n = items.Count; i < n; i++)
			{
				array[arrayIndex + i] = items[i].ShellItem;
			}
		}

		/// <summary>
		///	イテレータを取得。
		/// </summary>
		public IEnumerator<ShellItem> GetEnumerator()
		{
			foreach (FileListViewItem item in m_Items)
			{
				yield return item.ShellItem;
			}
		}
		#endregion

		#region 隠蔽用
		/// <summary>
		/// インデクサ。隠蔽用。
		/// </summary>
		ShellItem IList<ShellItem>.this[int index]
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
		///	アイテム挿入。隠蔽用。
		/// </summary>
		void IList<ShellItem>.Insert(int index, ShellItem item)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		///	アイテムを取り除く。隠蔽用。
		/// </summary>
		void IList<ShellItem>.RemoveAt(int index)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// アイテム追加。隠蔽用。
		/// </summary>
		void ICollection<ShellItem>.Add(ShellItem item)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		///	クリア。隠蔽用。
		/// </summary>
		void ICollection<ShellItem>.Clear()
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// 指定されたアイテムを取り除く。隠蔽用
		/// </summary>
		bool ICollection<ShellItem>.Remove(ShellItem item)
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
