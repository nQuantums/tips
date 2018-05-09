namespace NMeCab.Core
{
	public struct CharInfo
	{
		private readonly uint bits;

		public uint Type
		{
			get
			{
				return BitUtils.GetBitField(this.bits, 0, 18);
			}
		}

		public uint DefaultType
		{
			get
			{
				return BitUtils.GetBitField(this.bits, 18, 8);
			}
		}

		public uint Length
		{
			get
			{
				return BitUtils.GetBitField(this.bits, 26, 4);
			}
		}

		public bool Group
		{
			get
			{
				return BitUtils.GetFlag(this.bits, 30);
			}
		}

		public bool Invoke
		{
			get
			{
				return BitUtils.GetFlag(this.bits, 31);
			}
		}

		public CharInfo(uint bits)
		{
			this.bits = bits;
		}

		public bool IsKindOf(CharInfo c)
		{
			return BitUtils.CompareAnd(this.bits, c.bits, 0, 18);
		}

		public override string ToString()
		{
			return string.Format("[Type:{0}][DefaultType:{1}][Length:{2}][Group:{3}][Invoke:{4}]", this.Type, this.DefaultType, this.Length, this.Group, this.Invoke);
		}
	}
}
