namespace NMeCab.Core
{
	public static class BitUtils
	{
		private const uint One = 1u;

		private const uint AllZero = 0u;

		private const uint AllOne = 4294967295u;

		public static uint GetBitField(uint bits, int offset, int len)
		{
			uint num = (uint)(~(-1 << len));
			return bits >> offset & num;
		}

		public static bool GetFlag(uint bits, int offset)
		{
			uint num = (uint)(1 << offset);
			return (bits & num) != 0;
		}

		public static bool CompareAnd(uint bits1, uint bits2, int offset, int len)
		{
			uint num = (uint)(~(-1 << len) << offset);
			return (bits1 & bits2 & num) != 0;
		}
	}
}
