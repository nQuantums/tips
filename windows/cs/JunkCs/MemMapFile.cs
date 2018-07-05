using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

namespace Jk {
	/// <summary>
	/// メモリマップトファイルクラス
	/// </summary>
	public class MemMapFile : IDisposable {
		IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

		[Flags]
		public enum EFileAccess : uint {
			/// <summary>
			///
			/// </summary>
			GenericRead = 0x80000000,
			/// <summary>
			///
			/// </summary>
			GenericWrite = 0x40000000,
			/// <summary>
			///
			/// </summary>
			GenericExecute = 0x20000000,
			/// <summary>
			///
			/// </summary>
			GenericAll = 0x10000000
		}

		[Flags]
		public enum EFileShare : uint {
			/// <summary>
			///
			/// </summary>
			None = 0x00000000,
			/// <summary>
			/// Enables subsequent open operations on an object to request read access.
			/// Otherwise, other processes cannot open the object if they request read access.
			/// If this flag is not specified, but the object has been opened for read access, the function fails.
			/// </summary>
			Read = 0x00000001,
			/// <summary>
			/// Enables subsequent open operations on an object to request write access.
			/// Otherwise, other processes cannot open the object if they request write access.
			/// If this flag is not specified, but the object has been opened for write access, the function fails.
			/// </summary>
			Write = 0x00000002,
			/// <summary>
			/// Enables subsequent open operations on an object to request delete access.
			/// Otherwise, other processes cannot open the object if they request delete access.
			/// If this flag is not specified, but the object has been opened for delete access, the function fails.
			/// </summary>
			Delete = 0x00000004
		}

		public enum ECreationDisposition : uint {
			/// <summary>
			/// Creates a new file. The function fails if a specified file exists.
			/// </summary>
			New = 1,
			/// <summary>
			/// Creates a new file, always.
			/// If a file exists, the function overwrites the file, clears the existing attributes, combines the specified file attributes,
			/// and flags with FILE_ATTRIBUTE_ARCHIVE, but does not set the security descriptor that the SECURITY_ATTRIBUTES structure specifies.
			/// </summary>
			CreateAlways = 2,
			/// <summary>
			/// Opens a file. The function fails if the file does not exist.
			/// </summary>
			OpenExisting = 3,
			/// <summary>
			/// Opens a file, always.
			/// If a file does not exist, the function creates a file as if dwCreationDisposition is CREATE_NEW.
			/// </summary>
			OpenAlways = 4,
			/// <summary>
			/// Opens a file and truncates it so that its size is 0 (zero) bytes. The function fails if the file does not exist.
			/// The calling process must open the file with the GENERIC_WRITE access right.
			/// </summary>
			TruncateExisting = 5
		}

		[Flags]
		public enum EFileAttributes : uint {
			Readonly = 0x00000001,
			Hidden = 0x00000002,
			System = 0x00000004,
			Directory = 0x00000010,
			Archive = 0x00000020,
			Device = 0x00000040,
			Normal = 0x00000080,
			Temporary = 0x00000100,
			SparseFile = 0x00000200,
			ReparsePoint = 0x00000400,
			Compressed = 0x00000800,
			Offline = 0x00001000,
			NotContentIndexed = 0x00002000,
			Encrypted = 0x00004000,
			Write_Through = 0x80000000,
			Overlapped = 0x40000000,
			NoBuffering = 0x20000000,
			RandomAccess = 0x10000000,
			SequentialScan = 0x08000000,
			DeleteOnClose = 0x04000000,
			BackupSemantics = 0x02000000,
			PosixSemantics = 0x01000000,
			OpenReparsePoint = 0x00200000,
			OpenNoRecall = 0x00100000,
			FirstPipeInstance = 0x00080000
		}

		[Flags]
		enum FileMapProtection : uint {
			PageReadonly = 0x02,
			PageReadWrite = 0x04,
			PageWriteCopy = 0x08,
			PageExecuteRead = 0x20,
			PageExecuteReadWrite = 0x40,
			SectionCommit = 0x8000000,
			SectionImage = 0x1000000,
			SectionNoCache = 0x10000000,
			SectionReserve = 0x4000000,
		}

		[Flags]
		public enum FileMapAccess : uint {
			FileMapCopy = 0x0001,
			FileMapWrite = 0x0002,
			FileMapRead = 0x0004,
			FileMapAllAccess = 0x001f,
			fileMapExecute = 0x0020,
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SYSTEM_INFO {
			public ushort processorArchitecture;
			ushort reserved;
			public uint pageSize;
			public IntPtr minimumApplicationAddress;
			public IntPtr maximumApplicationAddress;
			public IntPtr activeProcessorMask;
			public uint numberOfProcessors;
			public uint processorType;
			public uint allocationGranularity;
			public ushort processorLevel;
			public ushort processorRevision;
		}



		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern IntPtr CreateFileW(
		   string lpFileName,
		   EFileAccess dwDesiredAccess,
		   EFileShare dwShareMode,
		   IntPtr lpSecurityAttributes,
		   ECreationDisposition dwCreationDisposition,
		   EFileAttributes dwFlagsAndAttributes,
		   IntPtr hTemplateFile);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool CloseHandle(IntPtr hHandle);

		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		static extern IntPtr CreateFileMappingW(
			IntPtr hFile,
			IntPtr lpFileMappingAttributes,
			FileMapProtection flProtect,
			uint dwMaximumSizeHigh,
			uint dwMaximumSizeLow,
			IntPtr lpName);

		[DllImport("kernel32.dll", SetLastError = true)]
		static unsafe extern IntPtr MapViewOfFile(
			IntPtr hFileMappingObject,
			FileMapAccess dwDesiredAccess,
			uint dwFileOffsetHigh,
			uint dwFileOffsetLow,
			IntPtr dwNumberOfBytesToMap);

		[DllImport("kernel32.dll", SetLastError = true)]
		static unsafe extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

		[DllImport("kernel32.dll")]
		static extern bool GetFileSizeEx(IntPtr hFile, out ulong lpFileSize);

		[DllImport("kernel32.dll")]
		static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);



		static uint AllocationGranularity;


		string m_FileName; // ファイル名
		IntPtr m_FileHandle = IntPtr.Zero; // ファイルハンドル
		ulong m_FileSize; // ファイルサイズ(bytes)
		IntPtr m_MappingHandle = IntPtr.Zero; // ファイルマッピングハンドル
		FileMapProtection m_FileMapProtection; // ファイルマッピング保護モード
		FileMapAccess m_FileMapAccess; // ファイルマップアクセスモード
		Dictionary<IntPtr, MemMapView> m_Views = new Dictionary<IntPtr, MemMapView>(); // マッピングされたメモリのアドレスコレクション


		/// <summary>
		/// ファイル名
		/// </summary>
		public string FileName {
			get { return m_FileName; }
		}

		/// <summary>
		/// ファイルハンドル
		/// </summary>
		public IntPtr FileHandle {
			get { return m_FileHandle; }
		}

		/// <summary>
		/// マッピングハンドル
		/// </summary>
		public IntPtr MappingHandle {
			get { return m_MappingHandle; }
		}

		/// <summary>
		/// ファイルサイズ(bytes)
		/// </summary>
		public ulong FileSize {
			get { return m_FileSize; }
		}


		/// <summary>
		/// コンストラクタ 指定されたファイルを指定されたアクセスモードと共有モードで開く
		/// </summary>
		/// <param name="fileName">ファイル名</param>
		/// <param name="fileAccess">ファイルアクセスモード</param>
		/// <param name="fileShare">ファイル共有モード</param>
		public MemMapFile(string fileName, FileAccess fileAccess, FileShare fileShare) {
			EFileAccess efa;
			EFileShare efs;

			efa = 0;
			if ((fileAccess & FileAccess.Read) == FileAccess.Read)
				efa |= EFileAccess.GenericRead;
			if ((fileAccess & FileAccess.Write) == FileAccess.Write)
				efa |= EFileAccess.GenericWrite;

			efs = EFileShare.None;
			if ((fileShare & FileShare.Read) == FileShare.Read)
				efs |= EFileShare.Read;
			if ((fileShare & FileShare.Write) == FileShare.Write)
				efs |= EFileShare.Write;

			m_FileName = fileName;

			m_FileHandle = CreateFileW(fileName, efa, efs, IntPtr.Zero, ECreationDisposition.OpenExisting, EFileAttributes.Normal, IntPtr.Zero);
			if (m_FileHandle == INVALID_HANDLE_VALUE) {
				Dispose();
				throw new MemMapFileException(Marshal.GetLastWin32Error(), fileName);
			}

			if (!GetFileSizeEx(m_FileHandle, out m_FileSize)) {
				Dispose();
				throw new MemMapFileException(Marshal.GetLastWin32Error(), fileName);
			}

			m_FileMapProtection = 0;
			m_FileMapAccess = 0;
			if ((efa & EFileAccess.GenericRead) == EFileAccess.GenericRead) {
				m_FileMapProtection |= FileMapProtection.PageReadonly;
				m_FileMapAccess |= FileMapAccess.FileMapRead;
			}
			if ((efa & EFileAccess.GenericWrite) == EFileAccess.GenericWrite) {
				m_FileMapProtection = FileMapProtection.PageReadWrite; // 書き込み時は FileMapProtection.PageReadonly を消したいので代入してる
				m_FileMapAccess |= FileMapAccess.FileMapWrite;
			}

			m_MappingHandle = CreateFileMappingW(m_FileHandle, IntPtr.Zero, m_FileMapProtection, 0, 0, IntPtr.Zero);
			if (m_MappingHandle == IntPtr.Zero) {
				Dispose();
				throw new MemMapFileException(Marshal.GetLastWin32Error(), fileName);
			}
		}

		/// <summary>
		/// 静的コンストラクタ
		/// </summary>
		static MemMapFile() {
			SYSTEM_INFO si;
			GetSystemInfo(out si);
			AllocationGranularity = si.allocationGranularity;
		}

		/// <summary>
		/// ファイナライザ
		/// </summary>
		~MemMapFile() {
			Dispose();
		}

		/// <summary>
		///	オブジェクト破棄
		/// </summary>
		public void Dispose() {
			if (m_MappingHandle != IntPtr.Zero) {
				ReleaseViews();
				CloseHandle(m_MappingHandle);
				m_MappingHandle = IntPtr.Zero;
			}
			if (m_FileHandle != IntPtr.Zero) {
				CloseHandle(m_FileHandle);
				m_FileHandle = IntPtr.Zero;
			}
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// 指定されたファイル位置をメモリに割り当てる
		/// </summary>
		/// <param name="position">位置</param>
		/// <param name="byteCount">割り当てバイト数</param>
		/// <returns>メモリアドレス</returns>
		/// <remarks>割り当てられたメモリは使い終わったら ReleasePtr または ReleaseAllPtrs で開放する必要がある</remarks>
		public MemMapView AllocateView(ulong position, ulong byteCount) {
			if (this.FileSize < position + byteCount)
				byteCount = this.FileSize - position;

			ulong offset = position % AllocationGranularity;
			ulong mapPosition = position;
			if (offset != 0)
				mapPosition -= offset;

			unsafe
			{
				//	指定された位置をメモリにマッピング
				IntPtr viewAddress = MapViewOfFile(m_MappingHandle, m_FileMapAccess, (uint)(mapPosition >> 32), (uint)(mapPosition & 0xffffffff), new IntPtr((long)byteCount + (long)offset));
				if (viewAddress == IntPtr.Zero)
					throw new MemMapFileException(Marshal.GetLastWin32Error(), m_FileName);

				//	取得されたポインタを保存
				IntPtr address = new IntPtr((byte*)viewAddress.ToPointer() + offset);
				MemMapView v = new MemMapView(this, position, address, viewAddress, byteCount);
				m_Views.Add(address, v);

				return v;
			}
		}

		/// <summary>
		///	割り当てられたメモリを開放する
		/// </summary>
		/// <param name="view">開放するビュー</param>
		public void ReleaseView(MemMapView view) {
			if (m_Views.ContainsKey(view.Address)) {
				if (!UnmapViewOfFile(m_Views[view.Address].ViewAddress))
					throw new MemMapFileException(Marshal.GetLastWin32Error(), m_FileName);
				m_Views.Remove(view.Address);
			}
		}

		/// <summary>
		/// 全ての割り当てられたメモリを開放する
		/// </summary>
		private void ReleaseViews() {
			unsafe
			{
				foreach (MemMapView v in m_Views.Values) {
					if (!UnmapViewOfFile(v.ViewAddress))
						throw new MemMapFileException(Marshal.GetLastWin32Error(), m_FileName);
				}
				m_Views.Clear();
			}
		}
	}

	/// <summary>
	///	メモリに割り当てられたビュー
	/// </summary>
	public class MemMapView : IDisposable {
		/// <summary>
		///	このビューの割り当て元の MemMapFile オブジェクト
		/// </summary>
		public MemMapFile Source { get; protected set; }

		/// <summary>
		/// ビューが対応するファイル内での位置
		/// </summary>
		public ulong Position { get; protected set; }

		/// <summary>
		/// Position に対応するメモリアドレス
		/// </summary>
		public IntPtr Address { get; protected set; }

		/// <summary>
		/// 実際に MapViewOfFile で割り当てられたメモリアドレス
		/// </summary>
		public IntPtr ViewAddress { get; protected set; }

		/// <summary>
		/// 割り当てサイズ(bytes)
		/// </summary>
		public ulong Size { get; protected set; }

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="source">このビューの割り当て元の MemMapFile オブジェクト</param>
		/// <param name="position">ビューが対応するファイル内での位置</param>
		/// <param name="address">position に対応するメモリアドレス</param>
		/// <param name="viewAddress">実際に MapViewOfFile で割り当てられたメモリアドレス</param>
		/// <param name="size">割り当てサイズ(bytes)</param>
		public MemMapView(MemMapFile source, ulong position, IntPtr address, IntPtr viewAddress, ulong size) {
			this.Source = source;
			this.Position = position;
			this.Address = address;
			this.ViewAddress = viewAddress;
			this.Size = size;
		}

		/// <summary>
		/// ファイナライザ
		/// </summary>
		~MemMapView() {
			Dispose();
		}

		/// <summary>
		///	オブジェクト破棄
		/// </summary>
		public void Dispose() {
			if (this.Source != null) {
				this.Source.ReleaseView(this);
				this.Source = null;
			}
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// インデクサ
		/// </summary>
		/// <param name="index">アクセスする byte のインデックス番号(ビューの先頭からのカウント)</param>
		public byte this[ulong index] {
			get {
				unsafe
				{
					if (this.Size <= index)
						throw new MemMapFileException("MemMapView[index] インデクサに渡された index がビューの範囲を超えています", this.Source.FileName);

					byte* p = (byte*)this.Address.ToPointer();
					return p[index];
				}
			}
			set {
				unsafe
				{
					if (this.Size <= index)
						throw new MemMapFileException("MemMapView[index] インデクサに渡された index がビューの範囲を超えています", this.Source.FileName);

					byte* p = (byte*)this.Address.ToPointer();
					p[index] = value;
				}
			}
		}

		/// <summary>
		/// バイト配列を読み込む
		/// </summary>
		/// <param name="offset">ビューの先頭からのオフセット(bytes)</param>
		/// <param name="length">読み込むサイズ(bytes)</param>
		/// <returns>バイト配列</returns>
		public byte[] ReadBytes(ulong offset, uint length) {
			if (this.Size < offset)
				throw new MemMapFileException("MemMapView.ReadBytes に渡された offset がビューの範囲を超えています", this.Source.FileName);

			if (this.Size < offset + length)
				length = (uint)(this.Size - offset);

			unsafe
			{
				byte[] buffer = new byte[length];
				Marshal.Copy(new IntPtr((byte*)this.Address.ToPointer() + offset), buffer, 0, (int)length);
				return buffer;
			}
		}

		/// <summary>
		/// バイト配列を読み込む
		/// </summary>
		/// <param name="offset">ビューの先頭からのオフセット(bytes)</param>
		/// <param name="buffer">読込先バッファ</param>
		/// <param name="startIndex">buffer の先頭位置</param>
		/// <param name="length">読み込むサイズ(bytes)</param>
		/// <returns>読み込まれたよう素数</returns>
		public uint Read(ulong offset, byte[] buffer, int startIndex, uint length) {
			if (this.Size < offset)
				throw new MemMapFileException("MemMapView.Read に渡された offset がビューの範囲を超えています", this.Source.FileName);

			if (this.Size < offset + length)
				length = (uint)(this.Size - offset);

			unsafe
			{
				Marshal.Copy(new IntPtr((byte*)this.Address.ToPointer() + offset), buffer, startIndex, (int)length);
				return length;
			}
		}
	}

	/// <summary>
	///	動的にメモリ割り当て範囲を変更するビュー
	/// </summary>
	public class DynamicMemMapView : IDisposable {
		const int BlockSize = 0x6400000;

		/// <summary>
		///	このビューの割り当て元の MemMapFile オブジェクト
		/// </summary>
		public MemMapFile Source { get; protected set; }

		/// <summary>
		/// ファイルサイズ(bytes)
		/// </summary>
		public ulong FileSize { get; protected set; }

		/// <summary>
		/// 現在のビュー
		/// </summary>
		public MemMapView View { get; protected set; }

		/// <summary>
		///	現在のビューの先頭位置
		/// </summary>
		public ulong CurrentViewPosition { get; protected set; }

		/// <summary>
		///	現在のビューが割り当てられたメモリアドレス
		/// </summary>
		public IntPtr CurrentViewAddress { get; protected set; }

		/// <summary>
		///	現在のビューの割り当てサイズ
		/// </summary>
		public ulong CurrentViewSize { get; protected set; }


		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="source">このビューの割り当て元の MemMapFile オブジェクト</param>
		public DynamicMemMapView(MemMapFile source) {
			this.Source = source;
			this.FileSize = source.FileSize;
		}

		/// <summary>
		/// ファイナライザ
		/// </summary>
		~DynamicMemMapView() {
			Dispose();
		}

		/// <summary>
		///	オブジェクト破棄
		/// </summary>
		public void Dispose() {
			if (this.Source != null)
				this.Source = null;
			if (this.View != null) {
				this.View.Dispose();
				this.View = null;
			}
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// インデクサ
		/// </summary>
		/// <param name="index">アクセスする byte のインデックス番号(ファイルの先頭からのカウント)</param>
		public byte this[ulong index] {
			get {
				unsafe
				{
					if (index < this.CurrentViewPosition || this.CurrentViewPosition + this.CurrentViewSize <= index)
						ReAllocateView(index);

					byte* p = (byte*)this.CurrentViewAddress.ToPointer();
					return p[index - this.CurrentViewPosition];
				}
			}
			set {
				unsafe
				{
					if (index < this.CurrentViewPosition || this.CurrentViewPosition + this.CurrentViewSize <= index)
						ReAllocateView(index);

					byte* p = (byte*)this.CurrentViewAddress.ToPointer();
					p[index - this.CurrentViewPosition] = value;
				}
			}
		}

		/// <summary>
		/// 指定された位置から構造体を取得する
		/// </summary>
		/// <typeparam name="T">構造体</typeparam>
		/// <param name="position">ファイル内での位置(bytes)</param>
		/// <returns>構造体</returns>
		public T Read<T>(ulong position) {
			return (T)Read(position, typeof(T));
		}

		/// <summary>
		/// 指定された位置から構造体を取得する
		/// </summary>
		/// <param name="position">ファイル内での位置(bytes)</param>
		/// <param name="type">構造体型</param>
		/// <returns>構造体</returns>
		public object Read(ulong position, Type type) {
			unsafe
			{
				var bits = BitMarshal.BitsOf(type);
				if (bits != 0) {
					int structSize = (bits + 7) / 8;
					var obj = Activator.CreateInstance(type);
					using (var ba = new BitAccessor(this.ReadBytes(position, structSize))) {
						BitMarshal.CopyToObject(ba, 0, obj);
					}
					return obj;
				} else {
					int structSize = Marshal.SizeOf(type);
					if (position < this.CurrentViewPosition || this.CurrentViewPosition + this.CurrentViewSize < position + (ulong)structSize)
						ReAllocateView(position);
					return Marshal.PtrToStructure(new IntPtr((byte*)this.CurrentViewAddress.ToPointer() + position - this.CurrentViewPosition), type);
				}
			}
		}

		/// <summary>
		/// 指定された位置から文字列を取得する
		/// </summary>
		/// <param name="position">ファイル内での位置(bytes)</param>
		/// <param name="len">バイト数</param>
		/// <returns>文字列</returns>
		public string ReadStringAnsi(ulong position, int len) {
			unsafe
			{
				if (position < this.CurrentViewPosition || this.CurrentViewPosition + this.CurrentViewSize < position + (ulong)len)
					ReAllocateView(position);
				return Marshal.PtrToStringAnsi(new IntPtr((byte*)this.CurrentViewAddress.ToPointer() + position - this.CurrentViewPosition), len);
			}
		}

		/// <summary>
		/// 指定された位置から sbyte 型値を読み込む
		/// </summary>
		/// <param name="position">ファイル内での位置(bytes)</param>
		/// <returns>値</returns>
		public sbyte ReadInt8(ulong position) {
			unsafe
			{
				if (position < this.CurrentViewPosition || this.CurrentViewPosition + this.CurrentViewSize < position + (ulong)1)
					ReAllocateView(position);
				return (sbyte)Marshal.ReadByte(new IntPtr((byte*)this.CurrentViewAddress.ToPointer() + position - this.CurrentViewPosition));
			}
		}

		/// <summary>
		/// 指定された位置から Int16 型値を読み込む
		/// </summary>
		/// <param name="position">ファイル内での位置(bytes)</param>
		/// <returns>値</returns>
		public Int16 ReadInt16(ulong position) {
			unsafe
			{
				if (position < this.CurrentViewPosition || this.CurrentViewPosition + this.CurrentViewSize < position + (ulong)2)
					ReAllocateView(position);
				return Marshal.ReadInt16(new IntPtr((byte*)this.CurrentViewAddress.ToPointer() + position - this.CurrentViewPosition));
			}
		}

		/// <summary>
		/// 指定された位置から Int32 型値を読み込む
		/// </summary>
		/// <param name="position">ファイル内での位置(bytes)</param>
		/// <returns>値</returns>
		public Int32 ReadInt32(ulong position) {
			unsafe
			{
				if (position < this.CurrentViewPosition || this.CurrentViewPosition + this.CurrentViewSize < position + (ulong)4)
					ReAllocateView(position);
				return Marshal.ReadInt32(new IntPtr((byte*)this.CurrentViewAddress.ToPointer() + position - this.CurrentViewPosition));
			}
		}

		/// <summary>
		/// 指定された位置から Int64 型値を読み込む
		/// </summary>
		/// <param name="position">ファイル内での位置(bytes)</param>
		/// <returns>値</returns>
		public Int64 ReadInt64(ulong position) {
			unsafe
			{
				if (position < this.CurrentViewPosition || this.CurrentViewPosition + this.CurrentViewSize < position + (ulong)8)
					ReAllocateView(position);
				return Marshal.ReadInt64(new IntPtr((byte*)this.CurrentViewAddress.ToPointer() + position - this.CurrentViewPosition));
			}
		}

		/// <summary>
		/// バイト配列を読み込む
		/// </summary>
		/// <param name="position">ファイル内での位置(bytes)</param>
		/// <param name="length">読み込むサイズ(bytes)</param>
		/// <returns>バイト配列</returns>
		public byte[] ReadBytes(ulong position, int length) {
			unsafe
			{
				if (position < this.CurrentViewPosition || this.CurrentViewPosition + this.CurrentViewSize < position + (ulong)length)
					ReAllocateView(position);

				byte[] buffer = new byte[length];
				Marshal.Copy(new IntPtr((byte*)this.CurrentViewAddress.ToPointer() + position - this.CurrentViewPosition), buffer, 0, length);
				return buffer;
			}
		}

		/// <summary>
		/// 指定された位置からビューを再割り当てする
		/// </summary>
		/// <param name="position">ファイル位置</param>
		protected void ReAllocateView(ulong position) {
			ReAllocateView(position, BlockSize);
		}

		/// <summary>
		/// 指定された位置からビューを再割り当てする
		/// </summary>
		/// <param name="position">ファイル位置</param>
		/// <param name="size">割り当てサイズ(bytes)</param>
		protected void ReAllocateView(ulong position, ulong size) {
			if (this.View != null) {
				this.View.Dispose();
				this.View = null;
				this.CurrentViewPosition = 0;
				this.CurrentViewSize = 0;
			}

			this.View = this.Source.AllocateView(position, size);
			this.CurrentViewPosition = this.View.Position;
			this.CurrentViewAddress = this.View.Address;
			this.CurrentViewSize = this.View.Size;

			if (this.CurrentViewPosition + this.CurrentViewSize <= position)
				throw new MemMapFileException("DynamicMemMapView.ReAllocateView に渡された position がファイルサイズを超えています", this.Source.FileName);
		}
	}

	/// <summary>
	/// MemMapFile 用の例外クラス。
	/// </summary>
	public class MemMapFileException : ApplicationException {
		private string m_FileName;

		/// <summary>
		/// コンストラクタ。
		/// </summary>
		public MemMapFileException(string msg, string fileName)
			: base(string.Format("{0} (ファイル名 \"{1}\")", msg, fileName)) {
			m_FileName = fileName;
		}

		/// <summary>
		/// コンストラクタ。
		/// </summary>
		public MemMapFileException(int ecode, string fileName)
			: base(string.Format("{0} (ファイル名 \"{1}\")", (new System.ComponentModel.Win32Exception(ecode)).Message, fileName)) {
			m_FileName = fileName;
		}

		/// <summary>
		///	例外発生元のファイルの名前を取得。
		/// </summary>
		public string FileName {
			get { return m_FileName; }
		}
	}
}
