namespace NMeCab.Core
{
	public class MeCabPath
	{
		public MeCabNode RNode
		{
			get;
			set;
		}

		public MeCabPath RNext
		{
			get;
			set;
		}

		public MeCabNode LNode
		{
			get;
			set;
		}

		public MeCabPath LNext
		{
			get;
			set;
		}

		public int Cost
		{
			get;
			set;
		}

		public float Prob
		{
			get;
			set;
		}

		public override string ToString()
		{
			return string.Format("[Cost:{0}][Prob:{1}][LNode:{2}][RNode;{3}]", this.Cost, this.Prob, this.LNode, this.RNode);
		}
	}
}
