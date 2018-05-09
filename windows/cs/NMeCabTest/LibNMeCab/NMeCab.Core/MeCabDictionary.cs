using System;
using System.IO;
using System.Text;

namespace NMeCab.Core
{
	public class MeCabDictionary : IDisposable
	{
		private const uint DictionaryMagicID = 4017196919u;

		private const uint DicVersion = 102u;

		private Token[] tokens;

		private byte[] features;

		private DoubleArray da = new DoubleArray();

		private Encoding encoding;

		private bool disposed;

		public string CharSet
		{
			get
			{
				return this.encoding.WebName;
			}
		}

		public uint Version
		{
			get;
			private set;
		}

		public DictionaryType Type
		{
			get;
			private set;
		}

		public uint LexSize
		{
			get;
			private set;
		}

		public uint LSize
		{
			get;
			private set;
		}

		public uint RSize
		{
			get;
			private set;
		}

		public string FileName
		{
			get;
			private set;
		}

		public void Open(string filePath)
		{
			this.FileName = filePath;
			using (FileStream input = new FileStream(filePath, FileMode.Open, FileAccess.Read))
			{
				using (BinaryReader reader = new BinaryReader(input))
				{
					this.Open(reader);
				}
			}
		}

		public unsafe void Open(BinaryReader reader)
		{
			uint num = reader.ReadUInt32();
			if (reader.BaseStream.CanSeek && reader.BaseStream.Length != (uint)((int)num ^ -277770377))
			{
				throw new MeCabInvalidFileException("dictionary file is broken", this.FileName);
			}
			this.Version = reader.ReadUInt32();
			if (this.Version != 102)
			{
				throw new MeCabInvalidFileException("incompatible version", this.FileName);
			}
			this.Type = (DictionaryType)reader.ReadUInt32();
			this.LexSize = reader.ReadUInt32();
			this.LSize = reader.ReadUInt32();
			this.RSize = reader.ReadUInt32();
			uint size = reader.ReadUInt32();
			uint num2 = reader.ReadUInt32();
			uint count = reader.ReadUInt32();
			reader.ReadUInt32();
			string @string = StrUtils.GetString(reader.ReadBytes(32), Encoding.ASCII);
			this.encoding = Encoding.GetEncoding(@string == "UTF8" ? "UTF-8" : @string);
			this.da.Open(reader, size);
			this.tokens = new Token[(long)num2 / (long)sizeof(Token)];
			for (int i = 0; i < this.tokens.Length; i++)
			{
				this.tokens[i] = Token.Create(reader);
			}
			this.features = reader.ReadBytes((int)count);
			if (reader.BaseStream.ReadByte() == -1)
			{
				return;
			}
			throw new MeCabInvalidFileException("dictionary file is broken", this.FileName);
		}

		public unsafe DoubleArray.ResultPair ExactMatchSearch(string key)
		{
			fixed (char* key2 = key)
			{
				return this.ExactMatchSearch(key2, key.Length, 0);
			}
		}

		public unsafe DoubleArray.ResultPair ExactMatchSearch(char* key, int len, int nodePos = 0)
		{
			int maxByteCount = this.encoding.GetMaxByteCount(len);
			byte* ptr = stackalloc byte[(int)(uint)maxByteCount];
			int bytes = this.encoding.GetBytes(key, len, ptr, maxByteCount);
			DoubleArray.ResultPair result = this.da.ExactMatchSearch(ptr, bytes, nodePos);
			result.Length = this.encoding.GetCharCount(ptr, result.Length);
			return result;
		}

		public unsafe int CommonPrefixSearch(char* key, int len, DoubleArray.ResultPair* result, int rLen)
		{
			int maxByteCount = this.encoding.GetMaxByteCount(len);
			byte* ptr = stackalloc byte[(int)(uint)maxByteCount];
			int bytes = this.encoding.GetBytes(key, len, ptr, maxByteCount);
			int num = this.da.CommonPrefixSearch(ptr, result, rLen, bytes, 0);
			for (int i = 0; i < num; i++)
			{
				result[i].Length = this.encoding.GetCharCount(ptr, result[i].Length);
			}
			return num;
		}

		public Token[] GetToken(DoubleArray.ResultPair n)
		{
			Token[] array = new Token[this.GetTokenSize(n)];
			int sourceIndex = n.Value >> 8;
			Array.Copy(this.tokens, sourceIndex, array, 0, array.Length);
			return array;
		}

		public int GetTokenSize(DoubleArray.ResultPair n)
		{
			return 0xFF & n.Value;
		}

		public string GetFeature(uint featurePos)
		{
			return StrUtils.GetString(this.features, featurePos, this.encoding);
		}

		public bool IsCompatible(MeCabDictionary d)
		{
			if (this.Version == d.Version && this.LSize == d.LSize && this.RSize == d.RSize)
			{
				return this.CharSet == d.CharSet;
			}
			return false;
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				if (disposing && this.da != null)
				{
					this.da.Dispose();
				}
				this.disposed = true;
			}
		}

		~MeCabDictionary()
		{
			this.Dispose(false);
		}
	}
}
