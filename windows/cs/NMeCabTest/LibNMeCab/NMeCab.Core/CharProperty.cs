using System.IO;
using System.Text;

namespace NMeCab.Core
{
	public class CharProperty
	{
		private const string CharPropertyFile = "char.bin";

		private string[] cList;

		private readonly CharInfo[] charInfoList = new CharInfo[65535];

		public int Size
		{
			get
			{
				return this.cList.Length;
			}
		}

		public void Open(string dicDir)
		{
			string text = Path.Combine(dicDir, "char.bin");
			using (FileStream input = new FileStream(text, FileMode.Open, FileAccess.Read))
			{
				using (BinaryReader reader = new BinaryReader(input))
				{
					this.Open(reader, text);
				}
			}
		}

		public void Open(BinaryReader reader, string fileName = null)
		{
			uint num = reader.ReadUInt32();
			if (reader.BaseStream.CanSeek)
			{
				long num2 = 4 + 32 * num + 4 * this.charInfoList.Length;
				if (reader.BaseStream.Length != num2)
				{
					throw new MeCabInvalidFileException("invalid file size", fileName);
				}
			}
			this.cList = new string[num];
			for (int i = 0; i < this.cList.Length; i++)
			{
				this.cList[i] = StrUtils.GetString(reader.ReadBytes(32), Encoding.ASCII);
			}
			for (int j = 0; j < this.charInfoList.Length; j++)
			{
				this.charInfoList[j] = new CharInfo(reader.ReadUInt32());
			}
		}

		public string Name(int i)
		{
			return this.cList[i];
		}

		public unsafe char* SeekToOtherType(char* begin, char* end, CharInfo c, CharInfo* fail, int* cLen)
		{
			char* ptr = begin;
			*cLen = 0;
			*fail = this.GetCharInfo(*ptr);
			while (ptr != end && c.IsKindOf(*fail))
			{
				ptr++;
				(*cLen)++;
				c = *fail;
				*fail = this.GetCharInfo(*ptr);
			}
			return ptr;
		}

		public CharInfo GetCharInfo(char c)
		{
			return this.charInfoList[c];
		}
	}
}
