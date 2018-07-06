using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.IO;

namespace Jk
{
	/// <summary>
	/// フォルダツリークラス。
	/// </summary>
	public partial class FolderTreeView : TreeView
	{
		#region 定数
		//	ツリービューメッセージ定数
		private const UInt32 TVSIL_NORMAL = 0;
		private const UInt32 TVM_SETIMAGELIST = 4361;
		#endregion

		#region コンストラクタ
		/// <summary>
		/// コンストラクタ。
		/// </summary>
		public FolderTreeView()
		{
			InitializeComponent();
			SetSystemImageList();
		}
		#endregion

		#region プロパティ
		/// <summary>
		/// ツリー ビュー コントロールに割り当てられているツリー ノードのコレクションを取得します。
		/// </summary>
		[Browsable(false)]
		[Localizable(true)]
		[MergableProperty(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] 
		public new TreeNodeCollection Nodes
		{
			get
			{
				return base.Nodes;
			}
		}

		/// <summary>
		/// ルートシェルアイテムの取得と設定。
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ShellItem RootShellItem
		{
			get
			{
				TreeNodeCollection node = this.Nodes;
				if (node.Count == 0)
					return null;
				FolderTreeNode ftn = node[0] as FolderTreeNode;
				if (ftn == null)
					return null;
				return ftn.ShellItem;
			}
			set
			{
				this.Nodes.Clear();
				if (value == null)
					return;

				FolderTreeNode tvwRoot = new FolderTreeNode(value, value.IsFileSystem);
				tvwRoot.UpdateChildren();
				//BeginUpdate();

				//	子ノード数が０の場合は展開することができず、 OnBeforeExpand 内でのファイル監視が開始されず
				//	サブフォルダができたことを判断できないため、子ノード数が０ならダミーの子ノード追加する
				if (tvwRoot.Nodes.Count == 0)
					tvwRoot.Nodes.Add("PH");

				this.Nodes.Add(tvwRoot);
				tvwRoot.Expand();
				//Sort();
				//EndUpdate();
			}
		}

		/// <summary>
		/// ルートアイテムのフルパス名の取得と設定。
		/// </summary>
		[Category("FolderTreeView")]
		[Description("ルートアイテムのフルパス名を指定します。")]
		public string RootPath
		{
			get
			{
				ShellItem si = this.RootShellItem;
				if (si == null)
					return null;
				if(si == ShellItem.DesktopShellItem)
					return "Desktop";
				return si.Path;
			}
			set
			{
				if (value == null || value.Length == 0)
				{
					this.RootShellItem = null;
				}
				else
				{
					if (value == this.RootPath)
						return;
					if(value == "Desktop")
						this.RootShellItem = ShellItem.DesktopShellItem;
					else
						this.RootShellItem = new ShellItem(value);
				}
			}
		}

		/// <summary>
		/// ツリー ビュー コントロールで現在選択されているツリー ノードを取得または設定します。
		/// </summary>
		[Browsable(false)]
		[Category("FolderTreeView")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new FolderTreeNode SelectedNode
		{
			get
			{
				return (FolderTreeNode)base.SelectedNode;
			}
			set
			{
				base.SelectedNode = value;
			}
		}

		/// <summary>
		/// 現在選択されているシェルアイテムの取得と設定。
		/// </summary>
		[Browsable(false)]
		[Category("FolderTreeView")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ShellItem SelectedShellItem
		{
			get
			{
				FolderTreeNode ftn = this.SelectedNode;
				if (ftn == null)
					return null;
				return ftn.ShellItem;
			}
			set
			{
				if(this.SelectedShellItem == value)
					return;
				if (value != null)
				{
					FolderTreeNode ftn = Find(value);
					if (ftn == null)
						return;
					this.SelectedNode = ftn;
				}
				else
				{
					this.SelectedNode = null;
				}
			}
		}

		/// <summary>
		/// 選択されたアイテムのパス名の取得と設定。
		/// </summary>
		[Category("FolderTreeView")]
		[Description("選択されたアイテムのパス名です。この値を変更すると指定されたパス名に対応するアイテムが選択される。")]
		public string SelectedPath
		{
			get
			{
				ShellItem sh = this.SelectedShellItem;
				if (sh != null)
					return sh.Path;
				else
					return null;
			}
			set
			{
				if (value == this.SelectedPath)
					return;
				if (value == null || value.Length == 0)
					this.SelectedShellItem = null;
				else
					this.SelectedShellItem = new ShellItem(value);
			}
		}
		#endregion

		#region メソッド
		/// <summary>
		/// 指定されたシェルアイテムに対応するツリーノードを取得する。
		/// </summary>
		/// <param name="shellItem">探したいシェルアイテム。</param>
		/// <returns>見つかった:ツりーノード、見つからなかった:null。</returns>
		public FolderTreeNode Find(ShellItem shellItem)
		{
			if (this.Nodes.Count == 0)
				return null;

			IntPtr idl1 = shellItem.PIDL;
			TreeNodeCollection nodes = this.Nodes;

			while (true)
			{
				bool found = false;
				foreach (TreeNode n in nodes)
				{
					FolderTreeNode ftn = n as FolderTreeNode;
					if (ftn == null)
						continue;

					IntPtr idl2 = ftn.ShellItem.PIDL;
					if (ShellAPI.ILIsParent(idl2, idl1, 0) != 0)
					{
						if (ShellAPI.ILIsEqual(idl2, idl1) != 0)
						{
							return ftn;
						}
						else
						{
							ftn.Expand();
							nodes = ftn.Nodes;
							found = true;
							break;
						}
					}
				}
				if (!found)
					return null;
			}
		}

		/// <summary>
		/// 指定されたパスに対応するツリーノードを検索する。
		/// </summary>
		/// <param name="path">検索したいパス名。</param>
		/// <returns>見つかった:ツりーノード、見つからなかった:null。</returns>
		public FolderTreeNode Find(string path)
		{
			return Find(new ShellItem(path));
		}
		#endregion

		#region オーバーライド
		/// <summary>
		/// OnHandleDestroyed オーバーライド。
		/// </summary>
		protected override void OnHandleDestroyed(EventArgs e)
		{
			//	全てのノードのアンマネージドリソースを解放する
			DisposeAllNodes();

			base.OnHandleDestroyed(e);
		}

		/// <summary>
		///	BeforeExpand イベントトリガをオーバーライド。
		/// </summary>
		protected override void OnBeforeExpand(TreeViewCancelEventArgs e)
		{
			FolderTreeNode ftn = e.Node as FolderTreeNode;
			if (ftn != null)
			{
				//	ファイル監視開始
				ftn.StartFileWatch();

				//	子ノード更新
				ftn.UpdateChildren();
			}
			base.OnBeforeExpand(e);
		}
		#endregion

		#region 内部メソッド
		/// <summary>
		/// システムイメージリストをこのツリービューに設定する。
		/// </summary>
		private void SetSystemImageList()
		{
			ShellAPI.SendMessage(this.Handle, TVM_SETIMAGELIST, TVSIL_NORMAL, SystemImageList.SmallImageList);
		}

		/// <summary>
		/// 全てのノードのアンマネージリソースを解放する。
		/// </summary>
		public virtual void DisposeAllNodes()
		{
			foreach (TreeNode node in this.Nodes)
			{
				FolderTreeNode ftn = node as FolderTreeNode;
				if (ftn != null)
				{
					ftn.DisposeAllChilds();
				}
			}
		}
		#endregion
	}

	/// <summary>
	/// フォルダツリーアイテムクラス。
	/// </summary>
	public class FolderTreeNode : TreeNode, IDisposable
	{
		#region フィールド
		private ShellItem m_ShellItem;
		private FileSystemWatcher m_Watcher;
		private bool m_IsFileSystemRoot;
		#endregion

		#region コンストラクタ
		/// <summary>
		///	コンストラクタ。指定されたシェルアイテムを参照するアイテムとして初期化する。
		/// </summary>
		/// <param name="sh">シェルアイテム。</param>
		/// <param name="isFileSystemRoot">このアイテムがファイルシステムのルートアイテムとするかどうかを指定する。</param>
		public FolderTreeNode(ShellItem sh, bool isFileSystemRoot)
		{
			m_ShellItem = sh;
			m_IsFileSystemRoot = isFileSystemRoot && sh.IsFileSystem;
			base.Text = sh.DisplayName;
			base.ImageIndex = sh.IconIndex;
			base.SelectedImageIndex = sh.SelectedIconIndex;
		}

		/// <summary>
		///	コンストラクタ。指定されたシェルアイテムを参照するアイテムとして初期化する。
		/// </summary>
		/// <param name="sh">シェルアイテム。</param>
		public FolderTreeNode(ShellItem sh)
			: this(sh, false)
		{
		}
		#endregion

		#region プロパティ
		/// <summary>
		///	このツリーアイテムのシェルアイテムの取得。
		/// </summary>
		public ShellItem ShellItem
		{
			get { return m_ShellItem; }
		}

		/// <summary>
		/// パス名の取得。
		/// </summary>
		public string Path
		{
			get { return m_ShellItem.Path; }
		}

		/// <summary>
		/// このアイテムがファイルシステムのルートアイテムかどうかの取得。
		/// </summary>
		public bool IsFileSystemRoot
		{
			get { return m_IsFileSystemRoot; }
		}
		#endregion

		#region メソッド
		/// <summary>
		/// 子ノードを更新する。
		/// </summary>
		public void UpdateChildren()
		{
			//if(this.TreeView != null)
			//    this.TreeView.BeginUpdate();

			this.Nodes.Clear();
			foreach (ShellItem shChild in ShellItem.GetSubFolders())
			{
				bool fileSystemRoot = !m_ShellItem.IsFileSystem && shChild.IsFileSystem;
				FolderTreeNode tvwChild = new FolderTreeNode(shChild, fileSystemRoot);

				//	もしこのアイテムがフォルダで、子フォルダを持っているならプレースフォルダアイテムを追加する(+マークを表示させるため)
				if (shChild.IsFolder && shChild.HasSubFolder)
					tvwChild.Nodes.Add("PH");
				this.Nodes.Add(tvwChild);
			}

			//if (this.TreeView != null)
			//{
			//    this.TreeView.Sort();
			//    this.TreeView.EndUpdate();
			//}
		}

		/// <summary>
		/// アンマネージ リソースの解放およびリセットに関連付けられているアプリケーション定義のタスクを実行します。
		/// </summary>
		public virtual void Dispose()
		{
			if (m_Watcher != null)
			{
				m_Watcher.Dispose();
				m_Watcher = null;
			}
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// 自分と全ての子ノードのアンマネージリソースを解放する。
		/// </summary>
		public virtual void DisposeAllChilds()
		{
			Dispose();
			foreach(TreeNode node in this.Nodes)
			{
				FolderTreeNode ftn = node as FolderTreeNode;
				if (ftn != null)
				{
					ftn.DisposeAllChilds();
				}
			}
		}
		#endregion

		#region 内部メソッド
		/// <summary>
		/// ファイル更新の監視開始
		/// </summary>
		internal void StartFileWatch()
		{
			//	このアイテムがファイルシステムのルートアイテムならファイル監視を開始する
			if (m_IsFileSystemRoot)
			{
				if (Directory.Exists(m_ShellItem.Path))
				{
					try
					{
						if (m_Watcher == null)
						{
							m_Watcher = new FileSystemWatcher();

							m_Watcher.Path = m_ShellItem.Path;
							m_Watcher.IncludeSubdirectories = true;
							m_Watcher.NotifyFilter = NotifyFilters.DirectoryName; // 監視対象はディレクトリのみ
							m_Watcher.Created += new FileSystemEventHandler(SubdirCreated);
							m_Watcher.Deleted += new FileSystemEventHandler(SubdirDeleted);
							m_Watcher.Renamed += new RenamedEventHandler(SubdirRenamed);
							m_Watcher.EnableRaisingEvents = true;
						}
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
		/// このアイテムの全ての子から指定されたパスに対応するアイテムを検索する。
		/// </summary>
		private FolderTreeNode FindChild(string path)
		{
			foreach (TreeNode node in this.Nodes)
			{
				FolderTreeNode ftn = node as FolderTreeNode;
				if (ftn != null)
				{
					if (string.Compare(ftn.Path, path, true) == 0)
						return ftn;
					if (ftn.IsExpanded)
					{
						if (path.StartsWith(ftn.Path + @"\", StringComparison.OrdinalIgnoreCase))
						{
							return ftn.FindChild(path);
						}
					}
				}
			}
			return null;
		}

		/// <summary>
		/// サブディレクトリが作成された時の処理。
		/// </summary>
		private void SubdirCreated(string path)
		{
			//	アイテムが展開されていない場合は何もしない（再展開時に更新されるので）
			if (!this.IsExpanded)
			{
				if (this.Nodes.Count == 0)
					this.Nodes.Add("PH"); // 子ノード数が０だと次回展開することができないのでダミー追加
				return;
			}

			//	作成されたディレクトリの親に対応するアイテムを検索
			FolderTreeNode parentNode;
			string parentPath = System.IO.Path.GetDirectoryName(path);
			if (string.Compare(parentPath, m_ShellItem.Path, true) == 0)
			{
				parentNode = this;
			}
			else
			{
				parentNode = FindChild(parentPath);
				if (parentNode == null)
					return;
			}

			FolderTreeNode node;
			try
			{
				//	作成されたディレクトリに対応するノードを作成
				node = new FolderTreeNode(new ShellItem(path));
			}
			catch (Exception)
			{
				//	作成された直後に削除されたりすると対応する ShellItem が作れずに例外が発生するので普通にリターン
				return;
			}

			//	作成されたディレクトリに対応するアイテムを追加
			node.UpdateChildren();

			//if (this.TreeView != null)
			//    this.TreeView.BeginUpdate();

			parentNode.Nodes.Add(node);

			//if (this.TreeView != null)
			//{
			//    this.TreeView.Sort();
			//    this.TreeView.EndUpdate();
			//}
		}

		/// <summary>
		/// サブディレクトリが作成された時の処理。
		/// </summary>
		private void SubdirDeleted(string path)
		{
			//	アイテムが展開されていない場合は何もしない（再展開時に更新されるので）
			if (!this.IsExpanded)
				return;

			//	削除されたディレクトリに対応するアイテムを検索
			FolderTreeNode node = FindChild(path);
			if (node == null)
				return;

			//	アイテムを削除
			if (node.Parent != null)
				node.Parent.Nodes.Remove(node);
		}

		/// <summary>
		/// サブディレクトリ名が変更された時の処理。
		/// </summary>
		private void SubdirRenamed(string oldPath, string path)
		{
			//	アイテムが展開されていない場合は何もしない（再展開時に更新されるので）
			if (!this.IsExpanded)
				return;

			//	名前変更されたディレクトリに対応するアイテムを検索
			FolderTreeNode node = FindChild(oldPath);
			if (node == null)
				return;

			//	名前を更新
			node.UpdateName(path);
		}

		/// <summary>
		/// 新しいパスで名前などを更新。
		/// </summary>
		private void UpdateName(string path)
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
			base.Text = m_ShellItem.DisplayName;
			base.ImageIndex = m_ShellItem.IconIndex;
			base.SelectedImageIndex = m_ShellItem.SelectedIconIndex;
		}
		#endregion

		#region ファイルシステム監視イベントハンドラ
		/// <summary>
		/// サブディレクトリが作成された時に呼び出される。
		/// </summary>
		private void SubdirCreated(object source, FileSystemEventArgs e)
		{
			FolderTreeView tv = (FolderTreeView)this.TreeView;
			tv.BeginInvoke(new MethodInvoker(delegate
			{
				SubdirCreated(e.FullPath);
			}));
		}

		/// <summary>
		/// サブディレクトリが削除された時に呼び出される。
		/// </summary>
		private void SubdirDeleted(object source, FileSystemEventArgs e)
		{
			FolderTreeView tv = (FolderTreeView)this.TreeView;
			tv.BeginInvoke(new MethodInvoker(delegate
			{
				SubdirDeleted(e.FullPath);
			}));
		}

		/// <summary>
		/// サブディレクトリ名が変更された時に呼び出される。
		/// </summary>
		private void SubdirRenamed(object sender, RenamedEventArgs e)
		{
			FolderTreeView tv = (FolderTreeView)this.TreeView;
			tv.BeginInvoke(new MethodInvoker(delegate
			{
				SubdirRenamed(e.OldFullPath, e.FullPath);
			}));
		}
		#endregion
	}
}
