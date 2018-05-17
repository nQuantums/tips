using NMeCab.Core;
using System.Text;

namespace NMeCab
{
	public class MeCabNode
	{
		private string feature;

		private uint featurePos;

		public MeCabNode Prev
		{
			get;
			set;
		}

		public MeCabNode Next
		{
			get;
			set;
		}

		public MeCabNode ENext
		{
			get;
			set;
		}

		public MeCabNode BNext
		{
			get;
			set;
		}

		internal MeCabPath RPath
		{
			get;
			set;
		}

		internal MeCabPath LPath
		{
			get;
			set;
		}

		public string Surface
		{
			get;
			set;
		}

		public string Feature
		{
			get
			{
				if (this.feature == null)
				{
					this.feature = this.Dictionary.GetFeature(this.featurePos);
				}
				return this.feature;
			}
			set
			{
				this.feature = value;
			}
		}

		private MeCabDictionary Dictionary
		{
			get;
			set;
		}

		public int Length
		{
			get;
			set;
		}

		public int RLength
		{
			get;
			set;
		}

		public ushort RCAttr
		{
			get;
			set;
		}

		public ushort LCAttr
		{
			get;
			set;
		}

		public ushort PosId
		{
			get;
			set;
		}

		public uint CharType
		{
			get;
			set;
		}

		public MeCabNodeStat Stat
		{
			get;
			set;
		}

		public bool IsBest
		{
			get;
			set;
		}

		public float Alpha
		{
			get;
			set;
		}

		public float Beta
		{
			get;
			set;
		}

		public float Prob
		{
			get;
			set;
		}

		public short WCost
		{
			get;
			set;
		}

		public long Cost
		{
			get;
			set;
		}

		public int BPos
		{
			get;
			set;
		}

		public int EPos
		{
			get;
			set;
		}

		internal void SetFeature(uint featurePos, MeCabDictionary dic)
		{
			this.feature = null;
			this.featurePos = featurePos;
			this.Dictionary = dic;
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("[Surface:");
			if (this.Stat == MeCabNodeStat.Bos)
			{
				stringBuilder.Append("BOS");
			}
			else if (this.Stat == MeCabNodeStat.Eos)
			{
				stringBuilder.Append("EOS");
			}
			else
			{
				stringBuilder.Append(this.Surface);
			}
			stringBuilder.Append("]");
			stringBuilder.Append("[Feature:").Append(this.Feature).Append("]");
			stringBuilder.Append("[BPos:").Append(this.BPos).Append("]");
			stringBuilder.Append("[EPos:").Append(this.EPos).Append("]");
			stringBuilder.Append("[RCAttr:").Append(this.RCAttr).Append("]");
			stringBuilder.Append("[LCAttr:").Append(this.LCAttr).Append("]");
			stringBuilder.Append("[PosId:").Append(this.PosId).Append("]");
			stringBuilder.Append("[CharType:").Append(this.CharType).Append("]");
			stringBuilder.Append("[Stat:").Append((int)this.Stat).Append("]");
			stringBuilder.Append("[IsBest:").Append(this.IsBest).Append("]");
			stringBuilder.Append("[Alpha:").Append(this.Alpha).Append("]");
			stringBuilder.Append("[Beta:").Append(this.Beta).Append("]");
			stringBuilder.Append("[Prob:").Append(this.Prob).Append("]");
			stringBuilder.Append("[Cost:").Append(this.Cost).Append("]");
			for (MeCabPath meCabPath = this.LPath; meCabPath != null; meCabPath = meCabPath.LNext)
			{
				stringBuilder.Append("[Path:");
				stringBuilder.Append("(Cost:").Append(meCabPath.Cost).Append(")");
				stringBuilder.Append("(Prob:").Append(meCabPath.Prob).Append(")");
				stringBuilder.Append("]");
			}
			return stringBuilder.ToString();
		}
	}
}
