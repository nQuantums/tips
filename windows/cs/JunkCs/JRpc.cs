using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Runtime;
using System.Runtime.InteropServices;

namespace Junk {
	/// <summary>
	/// RPC共通処理クラス
	/// </summary>
	public class JRpc {
		#region 内部クラス
		/// <summary>
		/// 内部で使用する結果コード
		/// </summary>
		public enum Rc : int {
			/// <summary>
			/// 正常終了
			/// </summary>
			OK = 0,

			// IO関係
			/// <summary>
			/// パケット読み込み中にエラーが発生したかデータが終了した
			/// </summary>
			IoPktReadFailed = -1000,

			/// <summary>
			/// 応答パケットの読み込みに失敗した
			/// </summary>
			IoPktResReadFailed = -1001,

			/// <summary>
			/// パケット書き込み中にエラーが発生した
			/// </summary>
			IoPktWriteFailed = -1002,

			/// <summary>
			/// 応答パケット書き込み中にエラーが発生した
			/// </summary>
			IoPktResWriteFailed = -1003,

			// シリアライズ関係
			/// <summary>
			/// 不明なコマンドを受け取った
			/// </summary>
			SlUnknownCmdId = -2000,

			/// <summary>
			/// 引数フォーマットに不明なタイプが含まれている
			/// </summary>
			SlFmtUnknown = -2001,

			/// <summary>
			/// 引数フォーマットに対してパケットのサイズが小さい
			/// </summary>
			SlPktSizeNotEnough = -2002,

			/// <summary>
			/// 戻り値フォーマットに対して応答パケットのサイズが小さい
			/// </summary>
			SlPktResSizeNotEnough = -2003,

			/// <summary>
			/// 引数フォーマットで指定されたサイズとパケットサイズが異なる
			/// </summary>
			SlPktSizeMismatch = -2004,

			/// <summary>
			/// 戻り値フォーマットで指定されたサイズと応答パケットサイズが異なる
			/// </summary>
			SlPktResSizeMismatch = -2005,

			/// <summary>
			/// パケット内のデータサイズ部分(Pkt::Size)が大きすぎるか小さすぎる
			/// </summary>
			SlPktSizeInvalid = -2006,

			/// <summary>
			/// 応答パケット内のデータサイズ部分(PktRes::Size)が大きすぎるか小さすぎる
			/// </summary>
			SlPktResSizeInvalid = -2007,

			/// <summary>
			/// 送信するパケットに引数をマーシャリング中にバッファサイズが足りなくなった
			/// </summary>
			SlPktBufSizeNotEnough = -2008,

			/// <summary>
			/// 送信する応答パケットに戻り値をマーシャリング中にバッファサイズが足りなくなった
			/// </summary>
			SlPktResBufSizeNotEnough = -2009,

			// サーバー内部処理関係
			/// <summary>
			/// サーバー内部でのエラー
			/// </summary>
			SvError = -3000,
		}

		/// <summary>
		/// RPC例外クラス
		/// </summary>
		public class Exc : ApplicationException {
			/// <summary>
			/// 結果コード
			/// </summary>
			public readonly Rc Code;

			/// <summary>
			/// コンストラクタ、結果コードを指定して初期化する
			/// </summary>
			/// <param name="code">結果コード</param>
			public Exc(Rc code) : base("RPC error code = " + code) {
				this.Code = code;
			}

			/// <summary>
			/// コンストラクタ、結果コードとメッセージを指定して初期化する
			/// </summary>
			/// <param name="code">結果コード</param>
			public Exc(Rc code, string message)
				: base("RPC error code = " + code + "\n" + message) {
				this.Code = code;
			}
		}

		/// <summary>
		/// バイナリデータバッファ
		/// </summary>
		public class ByteBuffer {
			byte[] _Array; // データの配列
			int _Position; // 配列内での現在位置
			int _Length; // 有効データサイズ(bytes)

			/// <summary>
			/// コンストラクタ、配列を指定して初期化する
			/// </summary>
			/// <param name="a">配列</param>
			public ByteBuffer(byte[] a) {
				if (a == null)
					throw new ArgumentException();
				_Array = a;
				_Position = 0;
				_Length = 0;
			}

			/// <summary>
			/// バッファの容量(bytes)を取得
			/// </summary>
			public int Capacity {
				get {
					return _Array.Length;
				}
			}

			/// <summary>
			/// 読み込み、書き込み位置
			/// </summary>
			public int Position {
				get { return _Position; }
				set {
					if (value < 0 || this.Capacity < value)
						throw new ArgumentException();
					_Position = value;
				}
			}

			/// <summary>
			/// 有効データサイズ(bytes)
			/// </summary>
			public int Length {
				get { return _Length; }
				set {
					if (value < 0 || this.Capacity < value)
						throw new ArgumentException();
					_Length = value;
				}
			}

			/// <summary>
			/// バッファデータを保持する配列の取得
			/// </summary>
			public byte[] Array {
				get { return _Array; }
			}


			/// <summary>
			/// byte 型値を書き込む
			/// </summary>
			/// <param name="value">値</param>
			public void Put(byte value) {
				_Array[_Position] = value;
				_Position++;
			}

			/// <summary>
			/// int 型値を書き込む
			/// </summary>
			/// <param name="value">値</param>
			public void Put(int value) {
				if (this.Capacity < _Position + 4)
					throw new IndexOutOfRangeException();
				unsafe {
					fixed (byte* p = _Array) {
						*(int*)(p + _Position) = value;
						_Position += 4;
					}
				}
			}

			/// <summary>
			/// int 型値を指定位置に書き込む
			/// </summary>
			/// <param name="position">書き込み位置</param>
			/// <param name="value">値</param>
			public void Put(int position, int value) {
				if (position < 0 || this.Capacity < position + 4)
					throw new IndexOutOfRangeException();
				unsafe {
					fixed (byte* p = _Array) {
						*(int*)(p + position) = value;
					}
				}
			}

			/// <summary>
			/// long 型値を書き込む
			/// </summary>
			/// <param name="value">値</param>
			public void Put(long value) {
				if (this.Capacity < _Position + 8)
					throw new IndexOutOfRangeException();
				unsafe {
					fixed (byte* p = _Array) {
						*(long*)(p + _Position) = value;
						_Position += 8;
					}
				}
			}

			/// <summary>
			/// float 型値を書き込む
			/// </summary>
			/// <param name="value">値</param>
			public void Put(float value) {
				if (this.Capacity < _Position + 4)
					throw new IndexOutOfRangeException();
				unsafe {
					fixed (byte* p = _Array) {
						*(float*)(p + _Position) = value;
						_Position += 4;
					}
				}
			}

			/// <summary>
			/// double 型値を書き込む
			/// </summary>
			/// <param name="value">値</param>
			public void Put(double value) {
				if (this.Capacity < _Position + 8)
					throw new IndexOutOfRangeException();
				unsafe {
					fixed (byte* p = _Array) {
						*(double*)(p + _Position) = value;
						_Position += 8;
					}
				}
			}

			/// <summary>
			/// string 型値を書き込む
			/// </summary>
			/// <param name="value">値</param>
			public void Put(string value) {
				if (value == null) {
					value = "";
				}

				byte[] bytes = Encoding.UTF8.GetBytes(value);
				int len = bytes.Length;
				int size = len + 4;

				if (this.Capacity < _Position + size)
					throw new IndexOutOfRangeException();

				unsafe {
					fixed (byte* ps = bytes)
					fixed (byte* pdst = _Array) {
						byte* pd = pdst + _Position;
						*(int*)pd = len;
						Copy(ps, pd + 4, len);
						_Position += size;
					}
				}
			}

			/// <summary>
			/// バッファに文字列をシリアライズする、その際バッファの制限位置を超えないようにする切り詰める
			/// </summary>
			/// <param name="value">値</param>
			public void PutTruncate(string value) {
				int blank = this.Capacity - _Position;
				if (blank < 5)
					return;

				if (value == null) {
					value = "";
				}

				byte[] bytes = Encoding.UTF8.GetBytes(value);
				int len = bytes.Length;

				blank -= 4;
				if (blank < len)
					len = blank;

				int size = len + 4;

				unsafe {
					fixed (byte* ps = bytes)
					fixed (byte* pdst = _Array) {
						byte* pd = pdst + _Position;
						*(int*)pd = len;
						Copy(ps, pd + 4, len);
						_Position += size;
					}
				}
			}


			/// <summary>
			/// byte 型配列値を書き込む
			/// </summary>
			/// <param name="value">値</param>
			/// <param name="length">開始インデックス</param>
			/// <param name="start">要素数</param>
			public void Put(byte[] value, int start, int length) {
				int size = length + 4;
				if (start < 0 || length < 0 || value.Length < start + length || this.Capacity < _Position + size)
					throw new IndexOutOfRangeException();
				unsafe {
					fixed (byte* ps = value)
					fixed (byte* pdst = _Array) {
						byte* pd = pdst + _Position;
						*(int*)pd = length;
						Copy(ps, pd + 4, length);
						_Position += size;
					}
				}
			}

			/// <summary>
			/// byte 型配列値を書き込む
			/// </summary>
			/// <param name="value">値</param>
			public void Put(byte[] value) {
				if (value == null) {
					value = new byte[0];
				}
				Put(value, 0, value.Length);
			}

			/// <summary>
			/// int 型配列値を書き込む
			/// </summary>
			/// <param name="value">値</param>
			/// <param name="length">開始インデックス</param>
			/// <param name="start">要素数</param>
			public void Put(int[] value, int start, int length) {
				const int ElementSize = 4;
				int byteLen = length * ElementSize;
				int size = byteLen + 4;
				if (start < 0 || length < 0 || value.Length < start + length || this.Capacity < _Position + size)
					throw new IndexOutOfRangeException();
				unsafe {
					fixed (int* ps = value)
					fixed (byte* pdst = _Array) {
						byte* pd = pdst + _Position;
						*(int*)pd = length;
						Copy(ps + ElementSize * start, pd + 4, byteLen);
						_Position += size;
					}
				}
			}

			/// <summary>
			/// int 型配列値を書き込む
			/// </summary>
			/// <param name="value">値</param>
			public void Put(int[] value) {
				if (value == null) {
					value = new int[0];
				}
				Put(value, 0, value.Length);
			}

			/// <summary>
			/// long 型配列値を書き込む
			/// </summary>
			/// <param name="value">値</param>
			/// <param name="length">開始インデックス</param>
			/// <param name="start">要素数</param>
			public void Put(long[] value, int start, int length) {
				const int ElementSize = 8;
				int byteLen = length * ElementSize;
				int size = byteLen + 4;
				if (start < 0 || length < 0 || value.Length < start + length || this.Capacity < _Position + size)
					throw new IndexOutOfRangeException();
				unsafe {
					fixed (long* ps = value)
					fixed (byte* pdst = _Array) {
						byte* pd = pdst + _Position;
						*(int*)pd = length;
						Copy(ps + ElementSize * start, pd + 4, byteLen);
						_Position += size;
					}
				}
			}

			/// <summary>
			/// long 型配列値を書き込む
			/// </summary>
			/// <param name="value">値</param>
			public void Put(long[] value) {
				if (value == null) {
					value = new long[0];
				}
				Put(value, 0, value.Length);
			}

			/// <summary>
			/// float 型配列値を書き込む
			/// </summary>
			/// <param name="value">値</param>
			/// <param name="length">開始インデックス</param>
			/// <param name="start">要素数</param>
			public void Put(float[] value, int start, int length) {
				const int ElementSize = 4;
				int byteLen = length * ElementSize;
				int size = byteLen + 4;
				if (start < 0 || length < 0 || value.Length < start + length || this.Capacity < _Position + size)
					throw new IndexOutOfRangeException();
				unsafe {
					fixed (float* ps = value)
					fixed (byte* pdst = _Array) {
						byte* pd = pdst + _Position;
						*(int*)pd = length;
						Copy(ps + ElementSize * start, pd + 4, byteLen);
						_Position += size;
					}
				}
			}

			/// <summary>
			/// float 型配列値を書き込む
			/// </summary>
			/// <param name="value">値</param>
			public void Put(float[] value) {
				if (value == null) {
					value = new float[0];
				}
				Put(value, 0, value.Length);
			}

			/// <summary>
			/// double 型配列値を書き込む
			/// </summary>
			/// <param name="value">値</param>
			/// <param name="length">開始インデックス</param>
			/// <param name="start">要素数</param>
			public void Put(double[] value, int start, int length) {
				const int ElementSize = 8;
				int byteLen = length * ElementSize;
				int size = byteLen + 4;
				if (start < 0 || length < 0 || value.Length < start + length || this.Capacity < _Position + size)
					throw new IndexOutOfRangeException();
				unsafe {
					fixed (double* ps = value)
					fixed (byte* pdst = _Array) {
						byte* pd = pdst + _Position;
						*(int*)pd = length;
						Copy(ps + ElementSize * start, pd + 4, byteLen);
						_Position += size;
					}
				}
			}

			/// <summary>
			/// double 型配列値を書き込む
			/// </summary>
			/// <param name="value">値</param>
			public void Put(double[] value) {
				if (value == null) {
					value = new double[0];
				}
				Put(value, 0, value.Length);
			}

			/// <summary>
			/// string 型配列値を書き込む
			/// </summary>
			/// <param name="value">値</param>
			/// <param name="length">開始インデックス</param>
			/// <param name="start">要素数</param>
			public void Put(string[] value, int start, int length) {
				if (start < 0 || length < 0 || value.Length < start + length)
					throw new IndexOutOfRangeException();
				Put(length);
				for (int i = start, n = start + length; i < n; i++)
					Put(value[i]);
			}

			/// <summary>
			/// string 型配列値を書き込む
			/// </summary>
			/// <param name="value">値</param>
			public void Put(string[] value) {
				if (value == null) {
					value = new string[0];
				}
				Put(value, 0, value.Length);
			}

			/// <summary>
			/// byte 型値を読み込む
			/// </summary>
			/// <returns>値</returns>
			public byte Get_byte() {
				if (this.Length < _Position + 1)
					throw new IndexOutOfRangeException();
				var r = _Array[_Position];
				_Position++;
				return r;
			}

			/// <summary>
			/// int 型値を読み込む
			/// </summary>
			/// <returns>値</returns>
			public int Get_int() {
				if (this.Length < _Position + 4)
					throw new IndexOutOfRangeException();
				unsafe {
					fixed (byte* p = _Array) {
						var r = *(int*)(p + _Position);
						_Position += 4;
						return r;
					}
				}
			}

			/// <summary>
			/// 指定位置から int 型値を読み込む
			/// </summary>
			/// <returns>値</returns>
			public int Get_int(int position) {
				if (position < 0 || this.Length < position + 4)
					throw new IndexOutOfRangeException();
				unsafe {
					fixed (byte* p = _Array) {
						var r = *(int*)(p + _Position);
						return r;
					}
				}
			}

			/// <summary>
			/// long 型値を読み込む
			/// </summary>
			/// <returns>値</returns>
			public long Get_long() {
				if (this.Length < _Position + 8)
					throw new IndexOutOfRangeException();
				unsafe {
					fixed (byte* p = _Array) {
						var r = *(long*)(p + _Position);
						_Position += 8;
						return r;
					}
				}
			}

			/// <summary>
			/// float 型値を読み込む
			/// </summary>
			/// <returns>値</returns>
			public float Get_float() {
				if (this.Length < _Position + 4)
					throw new IndexOutOfRangeException();
				unsafe {
					fixed (byte* p = _Array) {
						var r = *(float*)(p + _Position);
						_Position += 4;
						return r;
					}
				}
			}

			/// <summary>
			/// double 型値を読み込む
			/// </summary>
			/// <returns>値</returns>
			public double Get_double() {
				if (this.Length < _Position + 8)
					throw new IndexOutOfRangeException();
				unsafe {
					fixed (byte* p = _Array) {
						var r = *(double*)(p + _Position);
						_Position += 8;
						return r;
					}
				}
			}

			/// <summary>
			/// string 型値を読み込む
			/// </summary>
			/// <returns>値</returns>
			public string Get_string() {
				int pos = _Position;
				if (this.Length < pos + 4)
					throw new IndexOutOfRangeException();
				unsafe {
					fixed (byte* psst = _Array) {
						byte* ps = psst + pos;
						int len = *(int*)ps;

						if (len < 0 || this.Length < pos + len + 4)
							throw new IndexOutOfRangeException();
						pos += 4;

						var r = Encoding.UTF8.GetString(_Array, pos, len);
						pos += len;
						_Position = pos;
						return r;
					}
				}
			}


			/// <summary>
			/// byte 型配列値を読み込む
			/// </summary>
			/// <returns>値</returns>
			public byte[] Get_byte_array() {
				int pos = _Position;
				if (this.Length < pos + 4)
					throw new IndexOutOfRangeException();
				unsafe {
					fixed (byte* psst = _Array) {
						byte* ps = psst + pos;
						int len = *(int*)ps;

						if (len < 0 || this.Length < pos + len + 4)
							throw new IndexOutOfRangeException();
						pos += 4;

						byte[] r = new byte[len];
						fixed (byte* pd = r) {
							Copy(ps + 4, pd, len);
						}

						pos += len;
						_Position = pos;

						return r;
					}
				}
			}

			/// <summary>
			/// int 型配列値を読み込む
			/// </summary>
			/// <returns>値</returns>
			public int[] Get_int_array() {
				int pos = _Position;
				if (this.Length < pos + 4)
					throw new IndexOutOfRangeException();
				unsafe {
					fixed (byte* psst = _Array) {
						byte* ps = psst + pos;
						int len = *(int*)ps;
						long size = (long)len * 4;

						if (len < 0 || this.Length < pos + size + 4)
							throw new IndexOutOfRangeException();
						pos += 4;

						int[] r = new int[len];
						fixed (int* pd = r) {
							Copy(ps + 4, pd, (int)size);
						}

						pos += len;
						_Position = pos;

						return r;
					}
				}
			}

			/// <summary>
			/// long 型配列値を読み込む
			/// </summary>
			/// <returns>値</returns>
			public long[] Get_long_array() {
				int pos = _Position;
				if (this.Length < pos + 4)
					throw new IndexOutOfRangeException();
				unsafe {
					fixed (byte* psst = _Array) {
						byte* ps = psst + pos;
						int len = *(int*)ps;
						long size = (long)len * 8;

						if (len < 0 || this.Length < pos + size + 4)
							throw new IndexOutOfRangeException();
						pos += 4;

						long[] r = new long[len];
						fixed (long* pd = r) {
							Copy(ps + 4, pd, (int)size);
						}

						pos += len;
						_Position = pos;

						return r;
					}
				}
			}

			/// <summary>
			/// float 型配列値を読み込む
			/// </summary>
			/// <returns>値</returns>
			public float[] Get_float_array() {
				int pos = _Position;
				if (this.Length < pos + 4)
					throw new IndexOutOfRangeException();
				unsafe {
					fixed (byte* psst = _Array) {
						byte* ps = psst + pos;
						int len = *(int*)ps;
						long size = (long)len * 4;

						if (len < 0 || this.Length < pos + size + 4)
							throw new IndexOutOfRangeException();
						pos += 4;

						float[] r = new float[len];
						fixed (float* pd = r) {
							Copy(ps + 4, pd, (int)size);
						}

						pos += len;
						_Position = pos;

						return r;
					}
				}
			}

			/// <summary>
			/// double 型配列値を読み込む
			/// </summary>
			/// <returns>値</returns>
			public double[] Get_double_array() {
				int pos = _Position;
				if (this.Length < pos + 4)
					throw new IndexOutOfRangeException();
				unsafe {
					fixed (byte* psst = _Array) {
						byte* ps = psst + pos;
						int len = *(int*)ps;
						long size = (long)len * 8;

						if (len < 0 || this.Length < pos + size + 4)
							throw new IndexOutOfRangeException();
						pos += 4;

						double[] r = new double[len];
						fixed (double* pd = r) {
							Copy(ps + 4, pd, (int)size);
						}

						pos += len;
						_Position = pos;

						return r;
					}
				}
			}

			/// <summary>
			/// string 型配列値を読み込む
			/// </summary>
			/// <returns>値</returns>
			public string[] Get_string_array() {
				int len = Get_int();
				long size = (long)len * 4;
				if (len < 0 || this.Length < _Position + size + 4)
					throw new IndexOutOfRangeException();
				var r = new string[len];
				for (int i = 0; i < len; i++) {
					r[i] = Get_string();
				}
				return r;
			}


			/// <summary>
			/// メモリ内容をコピーする
			/// </summary>
			/// <param name="pSrc">コピー元ポインタ</param>
			/// <param name="pDst">コピー先ポインタ</param>
			/// <param name="size">コピーサイズ(bytes)</param>
			static unsafe void Copy(void* pSrc, void* pDst, int size) {
				int block = sizeof(void*);
				byte* ps = (byte*)pSrc;
				byte* pd = (byte*)pDst;
				byte* pe = ps + size - size % block;
				byte* pe2 = ps + size;

				while (ps < pe) {
					*(void**)pd = *(void**)ps;
					ps += block;
					pd += block;
				}

				while (ps < pe2) {
					*pd = *ps;
					ps++;
					pd++;
				}
			}
		}
		#endregion

		#region 公開メソッド
		/// <summary>
		/// 指定されたサイズまでソケットから読み込み
		/// </summary>
		/// <param name="sk">ソケット</param>
		/// <param name="buf">読み込み先バッファ</param>
		/// <param name="offset">読み込み先バッファのオフセット</param>
		/// <param name="size">読み込みサイズ</param>
		/// <returns>成功:true、失敗:false</returns>
		public static bool ReadToSize(Socket sk, byte[] buf, int offset, int size) {
			while (size != 0) {
				int n = sk.Receive(buf, offset, size, 0);
				if (n <= 0)
					return false;
				offset += n;
				size -= n;
			}
			return true;
		}

		/// <summary>
		/// 指定されたサイズまでソケットへ書き込む
		/// </summary>
		/// <param name="sk">ソケット</param>
		/// <param name="buf">書き込み元バッファ</param>
		/// <param name="offset">書き込み元バッファのオフセット</param>
		/// <param name="size">書き込みサイズ</param>
		/// <returns>成功:true、失敗:false</returns>
		public static bool WriteToSize(Socket sk, byte[] buf, int offset, int size) {
			while (size != 0) {
				int n = sk.Send(buf, offset, size, 0);
				if (n <= 0)
					return false;
				offset += n;
				size -= n;
			}
			return true;
		}

		/// <summary>
		/// パケットを書き込む
		/// </summary>
		/// <param name="sk">ソケット</param>
		/// <param name="bufPkt">パケットデータが入ったバッファ</param>
		/// <param name="size">パケットサイズ</param>
		public static void WritePkt(Socket sk, byte[] bufPkt, int size) {
			if (!WriteToSize(sk, bufPkt, 0, size)) {
				throw new Exc(Rc.IoPktWriteFailed);
			}
		}

		/// <summary>
		/// 応答パケットを書き込む
		/// </summary>
		/// <param name="sk">ソケット</param>
		/// <param name="bufPktRes">応答パケットデータが入ったバッファ</param>
		/// <param name="size">パケットサイズ</param>
		public static void WritePktRes(Socket sk, byte[] bufPktRes, int size) {
			if (!WriteToSize(sk, bufPktRes, 0, size)) {
				throw new Exc(Rc.IoPktResWriteFailed);
			}
		}

		/// <summary>
		/// パケットを読み込む
		/// </summary>
		/// <param name="sk">ソケット</param>
		/// <param name="bufPkt">パケットデータが入ったバッファ</param>
		/// <param name="pktSizeMin">最小パケットデータ部サイズ</param>
		/// <param name="pktSizeMax">最大パケットデータ部サイズ</param>
		/// <returns>読み込まれたバイト数</returns>
		public static int ReadPkt(Socket sk, byte[] bufPkt, int pktSizeMin, int pktSizeMax) {
			// サイズ部分を読み込む
			if (!ReadToSize(sk, bufPkt, 0, 4)) {
				throw new Exc(Rc.IoPktReadFailed);
			}

			// パケットサイズチェック
			int size = BitConverter.ToInt32(bufPkt, 0);
			if (size < pktSizeMin || pktSizeMax < size) {
				throw new Exc(Rc.SlPktSizeInvalid);
			}

			// パケットサイズ分読み込む
			if (!ReadToSize(sk, bufPkt, 4, size)) {
				throw new Exc(Rc.IoPktReadFailed);
			}

			return size + 4;
		}

		/// <summary>
		/// パケットを書き込み応答パケットを読み込む
		/// </summary>
		/// <param name="sk">ソケット</param>
		/// <param name="bufPkt">パケットデータが入ったバッファ</param>
		/// <param name="pktSize">パケットサイズ</param>
		/// <param name="bufPktRes">応答パケットデータが入るバッファ</param>
		/// <param name="pktResSizeMin">応答パケットデータ部サイズ最小値</param>
		/// <param name="pktResSizeMax">応答パケットデータ部サイズ最大値</param>
		/// <returns>読み込まれた応答パケットバイト数</returns>
		public static int WritePktReadPktRes(Socket sk, byte[] bufPkt, int pktSize, byte[] bufPktRes, int pktResSizeMin, int pktResSizeMax) {
			// パケットを書き込む
			if (!WriteToSize(sk, bufPkt, 0, pktSize)) {
				throw new Exc(Rc.IoPktWriteFailed);
			}

			// サイズ部分を読み込む
			if (!ReadToSize(sk, bufPktRes, 0, 4)) {
				throw new Exc(Rc.IoPktResReadFailed);
			}

			// パケットサイズチェック
			int size = BitConverter.ToInt32(bufPktRes, 0);
			if (size < pktResSizeMin || pktResSizeMax < size) {
				throw new Exc(Rc.SlPktResSizeInvalid);
			}

			// パケットサイズ分読み込む
			if (!ReadToSize(sk, bufPktRes, 4, size)) {
				throw new Exc(Rc.IoPktReadFailed);
			}

			// 結果コードを判定
			int result = BitConverter.ToInt32(bufPktRes, 4);
			if (result < 0) {
				if(12 < size) {
					var bb = new ByteBuffer(bufPktRes);
					bb.Length = size + 4;
					bb.Position = 8;
					throw new Exc((Rc)result, bb.Get_string());
				} else {
					throw new Exc((Rc)result);
				}
			}

			return size + 4;
		}
		#endregion
	}
}
