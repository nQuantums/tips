using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Jk {
	/// <summary>
	/// byte 配列内の任意ビットにアクセスするためのクラス
	/// リトルエンディアンではLSB0ビットナンバリングを、ビッグエンディアンではMSB0ビットナンバリングを前提としている
	/// </summary>
	public class BitAccessor : IDisposable {
		[DllImport("Kernel32.dll", EntryPoint = "RtlMoveMemory", SetLastError = false)]
		static unsafe extern void MoveMemory(void* dest, void* src, IntPtr size);

		GCHandle _BufferHandle;
		unsafe byte* _pBuffer;
		long _BitLength;
		bool _LittleEndian = true;

		/// <summary>
		/// バッファ内のデータがリトルエンディアンであるかどうか
		/// </summary>
		public bool LittleEndian
		{
			get { return _LittleEndian; }
			set { _LittleEndian = value; }
		}

		/// <summary>
		/// コンストラクタ、アクセス対象バッファを指定して初期化する
		/// </summary>
		/// <param name="buffer">アクセス対象バッファ</param>
		public BitAccessor(byte[] buffer) {
			_BufferHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
			var ptr = _BufferHandle.AddrOfPinnedObject();
			unsafe
			{
				_pBuffer = (byte*)ptr.ToPointer();
			}
			_BitLength = buffer.Length * 8;
		}

		/// <summary>
		/// 指定ビット位置から指定バイト数取得しビット位置を次の位置へ移動する
		/// </summary>
		/// <param name="position">ビット位置</param>
		/// <param name="count">バイト数</param>
		/// <returns>byte 配列</returns>
		public byte[] ReadBytes(ref int position, int count) {
			var r = GetBytes(position, count);
			position += count * 8;
			return r;
		}

		/// <summary>
		/// 指定ビット位置から最大32bitまでの整数を取得しビット位置を次の位置へ移動する
		/// </summary>
		/// <param name="position">ビット位置</param>
		/// <param name="bits">ビット数</param>
		/// <returns>整数</returns>
		public int ReadInt(ref int position, int bits) {
			var r = GetInt(position, bits);
			position += bits;
			return r;
		}

		/// <summary>
		/// 指定ビット位置から最大32bitまでの符号なし整数を取得しビット位置を次の位置へ移動する
		/// </summary>
		/// <param name="position">ビット位置</param>
		/// <param name="bits">ビット数</param>
		/// <returns>整数</returns>
		public uint ReadUInt(ref int position, int bits) {
			var r = GetUInt(position, bits);
			position += bits;
			return r;
		}

		/// <summary>
		/// 指定ビット位置から指定バイト数取得する
		/// </summary>
		/// <param name="position">ビット位置</param>
		/// <param name="count">バイト数</param>
		/// <returns>byte 配列</returns>
		public byte[] GetBytes(int position, int count) {
			int bits = count * 8;
			long l = _BitLength;
			if (count < 0 || (ulong)l < (uint)position || (ulong)l < (uint)(position + bits))
				throw new IndexOutOfRangeException("It has exceeded the size of the array. Argument \"position\"(" + position + ") + \"count\"(" + count + ") * 8.");

			int index1 = position / 8; // バイト配列インデックス
			int lsb = position % 8; // index1 から最下位ビットへのオフセットビット数
			byte[] bytes = new byte[count];
			unsafe
			{
				byte* pSrc = _pBuffer + index1;
				fixed (byte* pDst = bytes)
				{
					if (lsb == 0) {
						MoveMemory(pDst, pSrc, new IntPtr(count));
					} else if (count != 0) {
						int shift = 8 - lsb;
						byte t1 = pSrc[0];
						for (int i = 0; i < count; i++) {
							byte t2 = pSrc[i + 1];
							pDst[i] = (byte)((t2 << shift) | (t1 >> lsb));
							t1 = t2;
						}
					}
				}
			}

			return bytes;
		}

		/// <summary>
		/// 指定ビット位置から最大32bitまでの整数を取得する
		/// </summary>
		/// <param name="position">ビット位置</param>
		/// <param name="bits">ビット数</param>
		/// <returns>整数</returns>
		public int GetInt(int position, int bits) {
			long l = _BitLength;
			if (bits == 0 || 32 < (uint)bits || (ulong)l < (uint)position || (ulong)l < (uint)(position + bits))
				throw new ArgumentException();

			int index = position / 8; // バイト配列インデックス
			int sb = position % 8; // index から先頭ビットへのオフセットビット数
			int eb = sb + bits; // index から最終ビットへのオフセット+1
			int size = (eb + 7) / 8; // バイト境界を考慮した値取得の必要変数サイズ

			// byte 配列からビットシフトを用いて32ビット整数を取得する
			// C#でのビットシフトは int,uint,long,ulong のみ定義されているのでその型でシフトを行う
			unsafe
			{
				byte* p = _pBuffer + index;
				switch (size) {
				case 1: {
						if (this.LittleEndian) {
							int t = (int)p[0] << (32 - eb);
							t >>= 32 - bits;
							return t;
						} else {
							int t = (int)p[0] << (sb + 24);
							t >>= 32 - bits;
							return t;
						}
					}

				case 2: {
						if (this.LittleEndian) {
							int t = *(ushort*)p << (32 - eb);
							t >>= 32 - bits;
							return t;
						} else {
							int s = (sb + 24);
							int t = (int)p[0] << s;
							s -= 8 - sb;
							t |= (int)p[1] << s;
							t >>= 32 - bits;
							return t;
						}
					}

				case 3: {
						if (this.LittleEndian) {
							int t = *(int*)p << (32 - eb);
							t >>= 32 - bits;
							return t;
						} else {
							int s = (sb + 24);
							int t = (int)p[0] << s;
							s -= 8 - sb;
							t |= (int)p[1] << s;
							s -= 8;
							t |= (int)p[2] << s;
							t >>= 32 - bits;
							return t;
						}
					}

				case 4: {
						if (this.LittleEndian) {
							int t = *(int*)p << (32 - eb);
							t >>= 32 - bits;
							return t;
						} else {
							int s = (sb + 24);
							int t = (int)p[0] << s;
							s -= 8 - sb;
							t |= (int)p[1] << s;
							s -= 8;
							t |= (int)p[2] << s;
							s -= 8;
							t |= (int)p[3] << s;
							t >>= 32 - bits;
							return t;
						}
					}

				case 5: {
						if (this.LittleEndian) {
							long t = *(long*)p << (64 - eb);
							t >>= 64 - bits;
							return (int)t;
						} else {
							int s = (sb + 56);
							long t = (long)p[0] << s;
							s -= 8 - sb;
							t |= (long)p[1] << s;
							s -= 8;
							t |= (long)p[2] << s;
							s -= 8;
							t |= (long)p[3] << s;
							s -= 8;
							t |= (long)p[4] << s;
							t >>= 32 - bits;
							return (int)t;
						}
					}

				default:
					throw new NotImplementedException();
				}
			}
		}

		/// <summary>
		/// 指定ビット位置から最大32bitまでの符号なし整数を取得する
		/// </summary>
		/// <param name="position">ビット位置</param>
		/// <param name="bits">ビット数</param>
		/// <returns>整数</returns>
		public uint GetUInt(int position, int bits) {
			long l = _BitLength;
			if (bits == 0 || 32 < (uint)bits || (ulong)l < (uint)position || (ulong)l < (uint)(position + bits))
				throw new ArgumentException();

			int index = position / 8; // バイト配列インデックス
			int sb = position % 8; // index から先頭ビットへのオフセットビット数
			int eb = sb + bits; // index から最終ビットへのオフセット+1
			int size = (eb + 7) / 8; // バイト境界を考慮した値取得の必要変数サイズ

			// byte 配列からビットシフトを用いて32ビット整数を取得する
			// C#でのビットシフトは int,uint,long,ulong のみ定義されているのでその型でシフトを行う
			unsafe
			{
				byte* p = _pBuffer + index;
				switch (size) {
				case 1: {
						if (this.LittleEndian) {
							uint t = (uint)p[0] << (32 - eb);
							t >>= 32 - bits;
							return t;
						} else {
							uint t = (uint)p[0] << (sb + 24);
							t >>= 32 - bits;
							return t;
						}
					}

				case 2: {
						if (this.LittleEndian) {
							uint t = (uint)(*(ushort*)p << (32 - eb));
							t >>= 32 - bits;
							return t;
						} else {
							int s = (sb + 24);
							uint t = (uint)p[0] << s;
							s -= 8 - sb;
							t |= (uint)p[1] << s;
							t >>= 32 - bits;
							return t;
						}
					}

				case 3: {
						if (this.LittleEndian) {
							uint t = *(uint*)p << (32 - eb);
							t >>= 32 - bits;
							return t;
						} else {
							int s = (sb + 24);
							uint t = (uint)p[0] << s;
							s -= 8 - sb;
							t |= (uint)p[1] << s;
							s -= 8;
							t |= (uint)p[2] << s;
							t >>= 32 - bits;
							return t;
						}
					}

				case 4: {
						if (this.LittleEndian) {
							uint t = *(uint*)p << (32 - eb);
							t >>= 32 - bits;
							return t;
						} else {
							int s = (sb + 24);
							uint t = (uint)p[0] << s;
							s -= 8 - sb;
							t |= (uint)p[1] << s;
							s -= 8;
							t |= (uint)p[2] << s;
							s -= 8;
							t |= (uint)p[3] << s;
							t >>= 32 - bits;
							return t;
						}
					}

				case 5: {
						if (this.LittleEndian) {
							ulong t = *(ulong*)p << (64 - eb);
							t >>= 64 - bits;
							return (uint)t;
						} else {
							int s = (sb + 56);
							ulong t = (ulong)p[0] << s;
							s -= 8 - sb;
							t |= (ulong)p[1] << s;
							s -= 8;
							t |= (ulong)p[2] << s;
							s -= 8;
							t |= (ulong)p[3] << s;
							s -= 8;
							t |= (ulong)p[4] << s;
							t >>= 32 - bits;
							return (uint)t;
						}
					}

				default:
					throw new NotImplementedException();
				}
			}
		}

		/// <summary>
		/// 指定ビット位置から最大32bitまでの符号なし整数を取得する
		/// </summary>
		/// <param name="position">ビット位置</param>
		/// <param name="bits">ビット数</param>
		/// <returns>整数</returns>
		public float GetFloat(int position, int bits) {
			var u = GetUInt(position, bits);
			unsafe
			{
				return *(float*)&u;
			}
		}

		/// <summary>
		/// 内部 Dispose 処理
		/// </summary>
		/// <param name="disposing">Dispose() 内から呼ばれているかどうか</param>
		protected virtual void Dispose(bool disposing) {
			if (_BufferHandle.IsAllocated) {
				_BufferHandle.Free();
				unsafe
				{
					_pBuffer = null;
				}
			}
		}

		/// <summary>
		/// アンマネージ リソースの解放およびリセットに関連付けられているアプリケーション定義のタスクを実行します。
		/// </summary>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);  // Violates rule
		}

		/// <summary>
		/// ファイナライザ
		/// </summary>
		~BitAccessor() {
			Dispose(false);
		}
	}
}
