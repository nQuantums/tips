using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Jk
{
	/// <summary>
	/// シェルアイテムの種類列挙値。
	/// </summary>
	public enum ShellItemType
	{
		/// <summary>
		/// シェルアイテムはコントロールパネルやゴミ箱などの特殊フォルダ。
		/// </summary>
		SpecialFolder,

		/// <summary>
		/// シェルアイテムはドライブ。
		/// </summary>
		Drive,

		/// <summary>
		/// シェルアイテムは通常のフォルダ。
		/// </summary>
		Folder,

		/// <summary>
		/// シェルアイテムはコントロールパネル内の項目のような特殊アイテム。
		/// </summary>
		SpecialItem,

		/// <summary>
		/// シェルアイテムは通常のファイル。
		/// </summary>
		File
	}

	/// <summary>
	///	フォルダ、ファイルなどを指し示すクラス。
	/// </summary>
	public class ShellItem
	{
		#region クラスなど
		/// <summary>
		/// ファイルシステムの情報。
		/// </summary>
		public class FileInfo
		{
			internal DateTime m_CreationTime;
			internal DateTime m_LastAccessTime;
			internal DateTime m_LastWriteTime;
			internal long m_FileSize;

			/// <summary>
			/// ファイル作成時間。
			/// </summary>
			public DateTime CreationTime
			{
				get { return m_CreationTime; }
				set { m_CreationTime = value; }
			}

			/// <summary>
			/// ファイル最終アクセス時間。
			/// </summary>
			public DateTime LastAccessTime
			{
				get { return m_LastAccessTime; }
				set { m_LastAccessTime = value; }
			}

			/// <summary>
			/// ファイル最終更新時間。
			/// </summary>
			public DateTime LastWriteTime
			{
				get { return m_LastWriteTime; }
				set { m_LastWriteTime = value; }
			}

			/// <summary>
			/// ファイルサイズ。
			/// </summary>
			public long FileSize
			{
				get { return m_FileSize; }
				set { m_FileSize = value; }
			}
		}
		#endregion

		#region フィールド
		[Flags]
		enum FlagsEnum
		{
			IsFolder = 1 << 0,
			IsFileSystem = 1 << 1,
			HasSubFolder = 1 << 2,

			//HasSubFolderInitialized = 1 << 16,
		}

		/// <summary>
		/// InitAtr での初期化モード。
		/// </summary>
		enum InitMode
		{
			/// <summary>
			/// デスクトップアイテム用の初期化。
			/// </summary>
			Desktop,

			/// <summary>
			/// パスから作成時用の初期化。
			/// </summary>
			Path,

			/// <summary>
			/// ShellItem の子として作成時用の初期化。
			/// </summary>
			Child,
		}

		private static ShellItem m_DesktopShellItem = null;

		private ShellAPI.IShellFolder m_ShellFolder = null;
		private ShellAPI.IShellFolder m_ParentShellFolder = null;
		private IntPtr m_pIDL = IntPtr.Zero;         // このアイテムの絶対パスIDL
		private IntPtr m_pRelativeIDL = IntPtr.Zero; // ひとつ上の親からの相対パスIDL
		private string m_DisplayName = "";
		private string m_Path = "";
		private string m_FileName = "";
		private string m_TypeName = "";
		private Int32 m_iIconIndex = -1;
		private Int32 m_iSelectedIconIndex = -1;
		private ShellItemType m_ItemType = ShellItemType.SpecialFolder;
		private FlagsEnum m_Flags = 0;
		#endregion

		#region コンストラクタ
		/// <summary>
		/// コンストラクタ。指定されたパスを指し示すように初期化する。
		/// </summary>
		/// <param name="path">フルパス名。</param>
		public ShellItem(string path)
		{
			//	指定されたパスを指し示すITEMIDLを取得する
			m_pIDL = ShellAPI.ILCreateFromPathW(path);
			if (m_pIDL == IntPtr.Zero)
				throw new Exception(string.Format("{0} は有効なパスではありません。", path));

			IntPtr parentIdl = IntPtr.Zero;
			try
			{
				//	指定されたパスの親のIDLを取得
				parentIdl = ShellAPI.ILClone(m_pIDL);
				ShellAPI.ILRemoveLastID(parentIdl);

				//	親からの相対IDLを検索
				IntPtr relativeIdl = ShellAPI.ILFindLastID(m_pIDL);

				//	相対IDLを取得（メモリ解放漏れさせないために即座にメンバ変数へ格納）
				m_pRelativeIDL = ShellAPI.ILClone(relativeIdl);
				//	指定されたパスの親のIShellFolderを取得
				m_ParentShellFolder = ShellAPI.BindToIShellFolder(DesktopShellItem.ShellFolder, parentIdl);

				//	属性などいろいろ初期化する
				InitAtr(InitMode.Path, m_pRelativeIDL, m_ParentShellFolder);
			}
			finally
			{
				if (parentIdl != IntPtr.Zero)
					Marshal.FreeCoTaskMem(parentIdl);
			}
		}

		/// <summary>
		///	コンストラクタ。デスクトップを指し示すように初期化する。
		/// </summary>
		private ShellItem()
		{
			//	デスクトップのIDLを取得する
			int hRes = ShellAPI.SHGetSpecialFolderLocation(IntPtr.Zero, ShellAPI.CSIDL.CSIDL_DESKTOP, ref m_pIDL);
			if (hRes != 0)
				Marshal.ThrowExceptionForHR(hRes);

			//	デスクトップの IShellFolder インターフェースを取得する
			hRes = ShellAPI.SHGetDesktopFolder(out m_ShellFolder);
			if (hRes != 0)
				Marshal.ThrowExceptionForHR(hRes);

			//	属性などいろいろ初期化する
			InitAtr(InitMode.Desktop, IntPtr.Zero, null);
		}

		/// <summary>
		/// コンストラクタ。指定された ShellItem の子として初期化する。
		/// 初期化途中で例外が発生したら pIDL を所有してはならない。
		/// </summary>
		/// <param name="pIDL">子アイテムのITEMIDL。</param>
		/// <param name="shParent">親シェルアイテムオブジェクト。</param>
		private ShellItem(IntPtr pIDL, ShellItem shParent)
		{
			//	ITEMIDLを連結
			m_pIDL = ShellAPI.ILCombine(shParent.PIDL, pIDL);
			//	属性などいろいろ初期化する
			InitAtr(InitMode.Child, pIDL, shParent.ShellFolder);

			//	親 IShellFolder と親からの相対IDLを取得
			m_ParentShellFolder = shParent.ShellFolder;
			m_pRelativeIDL = pIDL;
		}
		#endregion

		#region デストラクタ
		/// <summary>
		///	デストラクタ。
		/// </summary>
		~ShellItem()
		{
			if (m_pIDL != IntPtr.Zero) Marshal.FreeCoTaskMem(m_pIDL);
			if (m_pRelativeIDL != IntPtr.Zero) Marshal.FreeCoTaskMem(m_pRelativeIDL);
			GC.SuppressFinalize(this);
		}
		#endregion

		#region プロパティ
		/// <summary>
		/// デスクトップを指し示すシェルアイテムオブジェクトを取得する。
		/// </summary>
		public static ShellItem DesktopShellItem
		{
			get
			{
				if (m_DesktopShellItem == null)
					m_DesktopShellItem = new ShellItem();
				return m_DesktopShellItem;
			}
		}

		/// <summary>
		///	シェルアイテムの表示名称の取得。
		/// </summary>
		public string DisplayName
		{
			get { return m_DisplayName; }
		}

		/// <summary>
		///	シェルアイテムの種類名称の取得。
		/// </summary>
		public string TypeName
		{
			get { return m_TypeName; }
		}

		/// <summary>
		///	このシェルアイテムのアイコンのシステムイメージリスト内でのインデックス番号の取得。
		/// </summary>
		public Int32 IconIndex
		{
			get { return m_iIconIndex; }
		}

		/// <summary>
		///	このシェルアイテムの選択状態のアイコンのシステムイメージリスト内でのインデックス番号の取得。
		/// </summary>
		public Int32 SelectedIconIndex
		{
			get { return m_iSelectedIconIndex; }
		}

		/// <summary>
		///	このシェルアイテムの IShellFolder インターフェースを取得する。
		/// </summary>
		internal ShellAPI.IShellFolder ShellFolder
		{
			get { return m_ShellFolder; }
		}

		/// <summary>
		/// このアイテムのITEMIDLを取得する。
		/// </summary>
		public IntPtr PIDL
		{
			get { return m_pIDL; }
		}

		/// <summary>
		///	このシェルアイテムがフォルダかどうかのフラグの取得。通常のファイルフォルダ、システムフォルダの場合に true が取得されます。
		/// </summary>
		public bool IsFolder
		{
			get { return GetFlags(FlagsEnum.IsFolder); }
			private set { SetFlags(FlagsEnum.IsFolder, value); }
		}

		/// <summary>
		///	このシェルアイテムがファイルシステムかどうかのフラグの取得。
		/// </summary>
		public bool IsFileSystem
		{
			get { return GetFlags(FlagsEnum.IsFileSystem); }
			private set { SetFlags(FlagsEnum.IsFileSystem, value); }
		}

		/// <summary>
		///	このシェルアイテムの種類の取得。
		/// </summary>
		public ShellItemType ItemType
		{
			get { return m_ItemType; }
		}

		/// <summary>
		///	このシェルアイテムに子フォルダがあるかどうかのフラグの取得。
		/// </summary>
		public bool HasSubFolder
		{
			get
			{
				//if (!GetFlags(FlagsEnum.HasSubFolderInitialized))
				//{
				//    //	このアイテムのプロパティを取得する
				//    ShellAPI.SFGAOF uFlags = ShellAPI.SFGAOF.SFGAO_HASSUBFOLDER;
				//    m_Parent.ShellFolder.GetAttributesOf(1, out m_pRelativeIDL, out uFlags);
				//    this.HasSubFolder = Convert.ToBoolean(uFlags & ShellAPI.SFGAOF.SFGAO_HASSUBFOLDER);
				//}
				return GetFlags(FlagsEnum.HasSubFolder);
			}
			private set
			{
				SetFlags(FlagsEnum.HasSubFolder, value);
				//SetFlags(FlagsEnum.HasSubFolderInitialized, true);
			}
		}

		/// <summary>
		///	このシェルアイテムのパス名を取得。
		/// </summary>
		public string Path
		{
			get { return m_Path; }
		}

		/// <summary>
		///	ファイル名の取得。
		/// </summary>
		public string FileName
		{
			get { return m_FileName; }
		}
		#endregion

		#region メソッド

		/// <summary>
		/// このアイテムの現在のファイル情報を取得する。
		/// ファイルシステム以外やデスクトップからファイル情報を取得しようとする例外が出ます。
		/// </summary>
		/// <returns>ファイル情報。</returns>
		public FileInfo GetFileInfo()
		{
			if (m_ParentShellFolder == null || m_pRelativeIDL == IntPtr.Zero)
			{
				throw new Exception(string.Format("この ShellItem(\"{0}\") からファイル情報を取得することはできません。", m_Path));
			}

			//	自分の IDL からファイル情報を取得
			ShellAPI.WIN32_FIND_DATA wfd = new ShellAPI.WIN32_FIND_DATA();
			int hr = ShellAPI.SHGetDataFromIDListW(m_ParentShellFolder, m_pRelativeIDL, ShellAPI.SHGDFIL_FINDDATA, out wfd, Marshal.SizeOf(wfd));
			if (hr < 0)
			{
				//	デスクトップのファイルは何故か SHGetDataFromIDListW が失敗するので FindFirstFileW を使う
				IntPtr hFind = ShellAPI.FindFirstFileW(m_Path, out wfd);
				if (hFind != new IntPtr(-1))
					ShellAPI.FindClose(hFind);
			}

			//	FileInfo の中身を設定する
			FileInfo fi = new FileInfo();
			ShellAPI.FILETIME ft = new ShellAPI.FILETIME();
			ShellAPI.SYSTEMTIME st = new ShellAPI.SYSTEMTIME();

			ShellAPI.FileTimeToLocalFileTime(ref wfd.ftCreationTime, out ft);
			ShellAPI.FileTimeToSystemTime(ref ft, out st);
			fi.m_CreationTime = new DateTime((int)st.wYear, (int)st.wMonth, (int)st.wDay, (int)st.wHour, (int)st.wMinute, (int)st.wSecond, (int)st.wMilliseconds);

			ShellAPI.FileTimeToLocalFileTime(ref wfd.ftLastAccessTime, out ft);
			ShellAPI.FileTimeToSystemTime(ref ft, out st);
			fi.m_LastAccessTime = new DateTime((int)st.wYear, (int)st.wMonth, (int)st.wDay, (int)st.wHour, (int)st.wMinute, (int)st.wSecond, (int)st.wMilliseconds);

			ShellAPI.FileTimeToLocalFileTime(ref wfd.ftLastWriteTime, out ft);
			ShellAPI.FileTimeToSystemTime(ref ft, out st);
			fi.m_LastWriteTime = new DateTime((int)st.wYear, (int)st.wMonth, (int)st.wDay, (int)st.wHour, (int)st.wMinute, (int)st.wSecond, (int)st.wMilliseconds);

			fi.m_FileSize = ((long)wfd.nFileSizeHigh << 32) | (long)wfd.nFileSizeLow;

			return fi;
		}

		/// <summary>
		/// サブフォルダ配列を取得する。
		/// </summary>
		/// <returns>ShellItem のリスト。</returns>
		public List<ShellItem> GetSubFolders()
		{
			//	Make sure we have a folder.
			if (this.IsFolder == false)
				throw new Exception("フォルダ以外からは子フォルダは取得できません。");

			List<ShellItem> arrChildren = new List<ShellItem>();

			//	IEnumIDList インターフェースポインタを取得する
			ShellAPI.IEnumIDList pEnum = null;
			uint hRes = m_ShellFolder.EnumObjects(
				IntPtr.Zero,
				ShellAPI.SHCONTF.SHCONTF_FOLDERS | ShellAPI.SHCONTF.SHCONTF_INCLUDEHIDDEN,
				out pEnum);
			if (hRes != 0)
				Marshal.ThrowExceptionForHR((int)hRes);

			//	子のIDLを列挙する
			Int32 iGot;
			IntPtr child = IntPtr.Zero;
			try
			{
				while (true)
				{
					if (pEnum.Next(1, out child, out iGot) != 0)
						break;
					arrChildren.Add(new ShellItem(child, this)); // child は作成した ShellItem に所有される
					child = IntPtr.Zero;
				}
			}
			finally
			{
				if (child != IntPtr.Zero)
					Marshal.FreeCoTaskMem(child); // 例外が起きて child の所有権が移動していなかったら自分で解放
				Marshal.ReleaseComObject(pEnum); // pEnum は外部に公開していないので即座に Release
			}

			return arrChildren;
		}

		/// <summary>
		/// サブアイテム配列を取得する。
		/// </summary>
		/// <returns>ShellItem のリスト。</returns>
		public List<ShellItem> GetSubItems()
		{
			//	Make sure we have a folder.
			if (this.IsFolder == false)
				throw new Exception("フォルダ以外からは子アイテムは取得できません。");

			List<ShellItem> arrChildren = new List<ShellItem>();

			//	IEnumIDList インターフェースポインタを取得する
			ShellAPI.IEnumIDList pEnum = null;
			uint hRes = m_ShellFolder.EnumObjects(
				IntPtr.Zero,
				ShellAPI.SHCONTF.SHCONTF_FOLDERS | ShellAPI.SHCONTF.SHCONTF_NONFOLDERS | ShellAPI.SHCONTF.SHCONTF_INCLUDEHIDDEN,
				out pEnum);
			if (hRes != 0)
				Marshal.ThrowExceptionForHR((int)hRes);

			//	子のIDLを列挙する
			Int32 iGot;
			IntPtr child = IntPtr.Zero;
			try
			{
				while (true)
				{
					if (pEnum.Next(1, out child, out iGot) != 0)
						break;
					arrChildren.Add(new ShellItem(child, this)); // child は作成した ShellItem に所有される
					child = IntPtr.Zero;
				}
			}
			finally
			{
				if (child != IntPtr.Zero)
					Marshal.FreeCoTaskMem(child); // 例外が起きて child の所有権が移動していなかったら自分で解放
				Marshal.ReleaseComObject(pEnum); // pEnum は外部に公開していないので即座に Release
			}

			return arrChildren;
		}

		/// <summary>
		/// 指定されたシェルアイテムが自分と同じものを指すシェルアイテムかどうか調べる。
		/// </summary>
		/// <param name="shellItem">同じかどうか調べたいシェルアイテム。</param>
		/// <returns>同じ:true、同じじゃない:false。</returns>
		public bool IsSameShellItem(ShellItem shellItem)
		{
			return Convert.ToBoolean(ShellAPI.ILIsEqual(m_pIDL, shellItem.PIDL));
		}

		/// <summary>
		/// 指定されたシェルアイテムが自分の子アイテムなのか調べる。
		/// </summary>
		/// <param name="shellItem">子かどうか調べたいシェルアイテム。</param>
		/// <param name="immediate">自分の直接の子供かどうかを調べたい場合に true をセットする。</param>
		/// <returns>自分の子:true、自分の子じゃない:false。</returns>
		public bool IsChild(ShellItem shellItem, bool immediate)
		{
			return Convert.ToBoolean(ShellAPI.ILIsParent(m_pIDL, shellItem.PIDL, immediate ? 1 : 0));
		}
		#endregion

		#region 内部メソッド
		/// <summary>
		///	属性を初期化する。
		/// </summary>
		private void InitAtr(InitMode mode, IntPtr pIDL, ShellAPI.IShellFolder parentSf)
		{
			//	基本的な属性を初期化する
			ShellAPI.SFGAOF uFlags = 0;
			switch (mode)
			{
				case InitMode.Desktop:
					{
						this.IsFolder = true;
						this.IsFileSystem = false;
						this.HasSubFolder = true;
					}
					break;
				case InitMode.Path:
				case InitMode.Child:
					{
						uFlags =
							ShellAPI.SFGAOF.SFGAO_FOLDER |
							ShellAPI.SFGAOF.SFGAO_FILESYSTEM |
							ShellAPI.SFGAOF.SFGAO_FILESYSANCESTOR;
						parentSf.GetAttributesOf(1, out pIDL, out uFlags);
						this.IsFolder = Convert.ToBoolean(uFlags & ShellAPI.SFGAOF.SFGAO_FOLDER);
						this.IsFileSystem = Convert.ToBoolean(uFlags & ShellAPI.SFGAOF.SFGAO_FILESYSTEM);

						if (this.IsFolder)
						{
							//	GetAttributesOf で SFGAO_HASSUBFOLDER を調べるとかなり遅くなる、
							//	そもそも this.HasSubFolder はヒントとして使用するだけなのでフォルダの場合は問答無用で
							//	this.HasSubFolder に true をセットする
							this.HasSubFolder = true;

							//	このオブジェクト用の IShellFolder インターフェース取得
							m_ShellFolder = ShellAPI.BindToIShellFolder(parentSf, pIDL);
						}
					}
					break;
			}

			//	パス名取得
			m_Path = ShellAPI.SHGetPathFromIDList(m_pIDL);

			//	アイコンインデックス番号とかの属性取得
			ShellAPI.SHFILEINFO shInfo = new ShellAPI.SHFILEINFO();
			ShellAPI.SHGetFileInfoW(
				m_pIDL,
				0,
				out shInfo,
				(uint)Marshal.SizeOf(shInfo),
				ShellAPI.SHGFI.SHGFI_SMALLICON |
				ShellAPI.SHGFI.SHGFI_SYSICONINDEX |
				ShellAPI.SHGFI.SHGFI_PIDL |
				ShellAPI.SHGFI.SHGFI_DISPLAYNAME |
				ShellAPI.SHGFI.SHGFI_TYPENAME
			);
			m_DisplayName = shInfo.szDisplayName;
			m_TypeName = shInfo.szTypeName;
			m_iIconIndex = shInfo.iIcon;

			//	選択時のアイコンインデックス番号取得
			ShellAPI.SHGetFileInfoW(
				m_pIDL,
				0,
				out shInfo,
				(uint)Marshal.SizeOf(shInfo),
				ShellAPI.SHGFI.SHGFI_PIDL |
				ShellAPI.SHGFI.SHGFI_SMALLICON |
				ShellAPI.SHGFI.SHGFI_SYSICONINDEX |
				ShellAPI.SHGFI.SHGFI_OPENICON
			);
			m_iSelectedIconIndex = shInfo.iIcon;

			//	いろいろ調べてアイテム種類(m_ItemType)を確定する
			if (this.IsFileSystem)
			{
				bool bValidated = false;
				m_FileName = "";

				try
				{
					m_FileName = System.IO.Path.GetFileName(m_Path);

					if (this.IsFolder)
					{
						if (System.IO.Path.GetPathRoot(m_Path) == m_Path)
						{
							//	パス名とルートパスが同じならドライブのはず
							m_ItemType = ShellItemType.Drive;
							bValidated = true;
						}
						else if (Convert.ToBoolean(uFlags & ShellAPI.SFGAOF.SFGAO_FILESYSANCESTOR))
						{
							//	ファイルシステムアイテムを含むことができるフォルダ＝ファイルフォルダ（だと思う）
							m_ItemType = ShellItemType.Folder;
							bValidated = true;
						}
					}
				}
				catch
				{
					//	システムのアイテムの場合まともなパス名ではなくて、例外が出ちゃうので
					//	ここで例外キャッチ
				}

				if (!bValidated)
				{
					m_ItemType = ShellItemType.File;
					bValidated = true;
				}
			}
			else
			{
				if (this.IsFolder)
				{
					m_ItemType = ShellItemType.SpecialFolder;
				}
				else
				{
					m_ItemType = ShellItemType.SpecialItem;
				}
			}
		}

		/// <summary>
		/// フラグの値取得。
		/// </summary>
		private bool GetFlags(FlagsEnum type)
		{
			return (m_Flags & type) == type;
		}

		/// <summary>
		/// フラグセット。
		/// </summary>
		private void SetFlags(FlagsEnum type, bool b)
		{
			if (b)
				m_Flags |= type;
			else
				m_Flags &= ~type;
		}
		#endregion
	}
}
