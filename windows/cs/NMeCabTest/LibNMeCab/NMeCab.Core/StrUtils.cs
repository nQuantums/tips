using System.Text;

namespace NMeCab.Core
{
	public static class StrUtils
	{
		private const byte Nul = 0;

		public static string GetString(byte[] bytes, Encoding enc)
		{
			return StrUtils.GetString(bytes, 0L, enc);
		}

		public unsafe static string GetString(byte[] bytes, long offset, Encoding enc)
		{
			fixed (byte* ptr = bytes)
			{
				return StrUtils.GetString(ptr + offset, enc);
			}
		}

		public unsafe static string GetString(byte* bytes, Encoding enc)
		{
			int num = 0;
			while (*bytes != 0)
			{
				num = checked(num + 1);
				bytes++;
			}
			bytes -= num;
			int maxCharCount = enc.GetMaxCharCount(num);
			char[] array = new char[maxCharCount];
			fixed (char* ptr = array)
			{
				int chars = enc.GetChars(bytes, num, ptr, maxCharCount);
				return new string(ptr, 0, chars);
			}
		}
	}
}
