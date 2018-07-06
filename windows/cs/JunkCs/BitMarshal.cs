using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Reflection;

namespace Jk {
	/// <summary>
	/// ビット単位でフィールドマーシャルするクラス
	/// </summary>
	public static class BitMarshal {
		delegate object Extract(BitAccessor ba, int offset, int bits);
		static Dictionary<Type, Extract> _SupportedTypes = new Dictionary<Type, Extract>();

		/// <summary>
		/// 静的コンストラクタで静的変数初期化
		/// </summary>
		static BitMarshal() {
			_SupportedTypes.Add(typeof(Boolean), (ba, offset, bits) => ba.GetUInt(offset, bits) != 0);
			_SupportedTypes.Add(typeof(Byte), (ba, offset, bits) => (Byte)ba.GetUInt(offset, bits));
			_SupportedTypes.Add(typeof(SByte), (ba, offset, bits) => (SByte)ba.GetInt(offset, bits));
			_SupportedTypes.Add(typeof(Int16), (ba, offset, bits) => (Int16)ba.GetInt(offset, bits));
			_SupportedTypes.Add(typeof(UInt16), (ba, offset, bits) => (UInt16)ba.GetUInt(offset, bits));
			_SupportedTypes.Add(typeof(Int32), (ba, offset, bits) => (Int32)ba.GetInt(offset, bits));
			_SupportedTypes.Add(typeof(UInt32), (ba, offset, bits) => (UInt32)ba.GetUInt(offset, bits));
			//_SupportedTypes.Add(typeof(Int64));
			//_SupportedTypes.Add(typeof(UInt64));
			//_SupportedTypes.Add(typeof(IntPtr));
			//_SupportedTypes.Add(typeof(UIntPtr));
			_SupportedTypes.Add(typeof(Char), (ba, offset, bits) => (Char)ba.GetUInt(offset, bits));
			//_SupportedTypes.Add(typeof(Double));
			_SupportedTypes.Add(typeof(Single), (ba, offset, bits) => (Single)ba.GetFloat(offset, bits));
			_SupportedTypes.Add(typeof(byte[]), (ba, offset, bits) => ba.GetBytes(offset, bits / 8));
		}

		/// <summary>
		/// 指定されたビットアクセッサの指定位置から指定タイプのオブジェクトを取得する
		/// </summary>
		/// <param name="ba">ビットアクセッサ、LittleEndian プロパティが取得構造体の BitStructAttribute 属性に合わせて変更されるので注意</param>
		/// <param name="position">ビット位置</param>
		/// <param name="obj">コピー先オブジェクト</param>
		/// <returns>オブジェクト</returns>
		public static void CopyToObject(BitAccessor ba, int position, object obj) {
			var type = obj.GetType();
			var atr = type.GetCustomAttributes(typeof(BitStructAttribute), true).FirstOrDefault() as BitStructAttribute;
			var fields = type.GetFields().OrderBy(field => field.MetadataToken);
			int offset = 0;

			if (atr != null)
				ba.LittleEndian = atr.LittleEndian;

			foreach (var f in fields) {
				var ft = f.FieldType;
				Extract ext;
				if (!_SupportedTypes.TryGetValue(ft, out ext))
					throw new NotSupportedException("Field \"" + f.Name + "\" type of " + ft.FullName + "\" is not supported.");
				var fatr = f.GetCustomAttributes(typeof(BitsAttribute), true).FirstOrDefault() as BitsAttribute;
				if(fatr == null)
					throw new NotSupportedException("Field \"" + f.Name + "\" has no BitsAttribute.");
				if (0 <= fatr.Offset)
					offset = fatr.Offset;
				try {
					f.SetValue(obj, ext(ba, position + offset, fatr.Bits));
				} catch (Exception ex) {
					throw new IndexOutOfRangeException("\"" + f.Name + "\" has exceeded the size of the array.", ex);
				}
				offset += fatr.Bits;
			}
		}

		/// <summary>
		/// 指定されたビットアクセッサの指定位置から指定タイプのオブジェクトを取得する
		/// </summary>
		/// <param name="ba">ビットアクセッサ、LittleEndian プロパティが取得構造体の BitStructAttribute 属性に合わせて変更されるので注意</param>
		/// <param name="position">ビット位置</param>
		/// <param name="type">取得するオブジェクト型</param>
		/// <returns>オブジェクト</returns>
		public static object ToObject(BitAccessor ba, int position, Type type) {
			var result = Activator.CreateInstance(type);
			CopyToObject(ba, position, result);
			return result;
		}

		/// <summary>
		/// 指定されたビットアクセッサの指定位置から指定タイプのオブジェクトを取得する
		/// </summary>
		/// <typeparam name="T">取得する型</typeparam>
		/// <param name="ba">ビットアクセッサ、LittleEndian プロパティが取得構造体の BitStructAttribute 属性に合わせて変更されるので注意</param>
		/// <param name="position">ビット位置</param>
		/// <returns>オブジェクト</returns>
		public static T ToObject<T>(BitAccessor ba, int position) where T : new() {
			var result = new T();
			CopyToObject(ba, position, result);
			return result;
		}

		/// <summary>
		/// BitStructAttribute 属性が付いた型のビット数を取得する
		/// </summary>
		/// <param name="type">型</param>
		/// <returns>BitStructAttribute 属性が付いていたらビット数が返る、付いてなかったら0が返る</returns>
		public static int BitsOf(Type type) {
			var atr = type.GetCustomAttributes(typeof(BitStructAttribute), true).FirstOrDefault() as BitStructAttribute;
			if (atr == null)
				return 0;

			var bits = atr._Bits;
			if (bits < 0) {
				var fields = type.GetFields().OrderBy(field => field.MetadataToken);
				int offset = 0;

				foreach (var f in fields) {
					var ft = f.FieldType;
					Extract ext;
					if (!_SupportedTypes.TryGetValue(ft, out ext))
						throw new NotSupportedException("Field \"" + f.Name + "\" type of " + ft.FullName + "\" is not supported.");
					var fatr = f.GetCustomAttributes(typeof(BitsAttribute), true).FirstOrDefault() as BitsAttribute;
					if (fatr == null)
						throw new NotSupportedException("Field \"" + f.Name + "\" has no BitsAttribute.");
					if (0 <= fatr.Offset)
						offset = fatr.Offset;
					offset += fatr.Bits;
					if (bits < offset)
						bits = offset;
				}

				atr._Bits = bits;
			}

			return bits;
		}
	}

	/// <summary>
	/// ビット構造を構造体またはクラスにマッピング指定する属性
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public class BitStructAttribute : Attribute {
		/// <summary>
		/// 構造体全体のビット数
		/// </summary>
		internal int _Bits = -1;

		/// <summary>
		/// リトルエンディアンかどうか
		/// </summary>
		public bool LittleEndian = true;

		/// <summary>
		/// コンストラク、リトルエンディアンで初期化する
		/// </summary>
		public BitStructAttribute() {
		}

		/// <summary>
		/// コンストラクタ、リトルエンディアンかどうか指定して初期化する
		/// </summary>
		/// <param name="littleEndian">リトルエンディアンかどうか</param>
		public BitStructAttribute(bool littleEndian) {
			this.LittleEndian = littleEndian;
		}
	}

	/// <summary>
	/// フィールドへのビット対応指定属性
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class BitsAttribute : Attribute {
		/// <summary>
		/// フィールドのビット位置、負数なら直前に宣言されたフィールドの最終ビットの次のビットとなる
		/// </summary>
		public int Offset = -1;

		/// <summary>
		/// フィールドのビット数
		/// </summary>
		public int Bits;

		/// <summary>
		/// コンストラク、ビット数を指定して初期化する
		/// </summary>
		/// <param name="bits">ビット数(0...)</param>
		public BitsAttribute(int bits) {
			if (bits <= 0)
				throw new ArgumentException("Argument \"bits\" must be over zero.");
			this.Bits = bits;
		}
	}
}
