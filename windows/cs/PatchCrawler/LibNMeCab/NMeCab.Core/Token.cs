using System.IO;

namespace NMeCab.Core
{
	public struct Token
	{
		public ushort LcAttr
		{
			get;
			set;
		}

		public ushort RcAttr
		{
			get;
			set;
		}

		public ushort PosId
		{
			get;
			set;
		}

		public short WCost
		{
			get;
			set;
		}

		public uint Feature
		{
			get;
			set;
		}

		public uint Compound
		{
			get;
			set;
		}

		public static Token Create(BinaryReader reader)
		{
			Token result = default(Token);
			result.LcAttr = reader.ReadUInt16();
			result.RcAttr = reader.ReadUInt16();
			result.PosId = reader.ReadUInt16();
			result.WCost = reader.ReadInt16();
			result.Feature = reader.ReadUInt32();
			result.Compound = reader.ReadUInt32();
			return result;
		}

		public override string ToString()
		{
			return string.Format("[LcAttr:{0}][RcAttr:{1}][PosId:{2}][WCost:{3}][Feature:{4}][Compound:{5}]", this.LcAttr, this.RcAttr, this.PosId, this.WCost, this.Feature, this.Compound);
		}
	}
}
