using System;
using System.Text;

namespace NMeCab.Core
{
	public class Viterbi : IDisposable
	{
		private class ThreadData
		{
			public MeCabNode EosNode;

			public MeCabNode BosNode;

			public MeCabNode[] EndNodeList;

			public MeCabNode[] BeginNodeList;

			public float Z;
		}

		private unsafe delegate void AnalyzeAction(char* str, int len, ThreadData work);

		private delegate void ConnectAction(int pos, MeCabNode rNode, ThreadData work);

		private delegate MeCabNode BuildLatticeFunc(ThreadData work);

		private readonly Tokenizer tokenizer = new Tokenizer();

		private readonly Connector connector = new Connector();

		private MeCabLatticeLevel level;

		private float theta;

		private int costFactor;

		private AnalyzeAction analyze;

		private ConnectAction connect;

		private BuildLatticeFunc buildLattice;

		private bool disposed;

		public float Theta
		{
			get
			{
				return this.theta * (float)this.costFactor;
			}
			set
			{
				this.theta = value / (float)this.costFactor;
			}
		}

		public unsafe MeCabLatticeLevel LatticeLevel
		{
			get
			{
				return this.level;
			}
			set
			{
				this.level = value;
				this.connect = this.ConnectNomal;
				this.analyze = this.DoViterbi;
				if (value >= MeCabLatticeLevel.One)
				{
					this.connect = this.ConnectWithAllPath;
				}
				if (value >= MeCabLatticeLevel.Two)
				{
					this.analyze = this.ForwardBackward;
				}
			}
		}

		public bool Partial
		{
			get;
			set;
		}

		public bool AllMorphs
		{
			get
			{
				return this.buildLattice == new BuildLatticeFunc(this.BuildAllLattice);
			}
			set
			{
				if (value)
				{
					this.buildLattice = this.BuildAllLattice;
				}
				else
				{
					this.buildLattice = this.BuildBestLattice;
				}
			}
		}

		public void Open(MeCabParam param)
		{
			this.tokenizer.Open(param);
			this.connector.Open(param);
			this.costFactor = param.CostFactor;
			this.Theta = param.Theta;
			this.LatticeLevel = param.LatticeLevel;
			this.Partial = param.Partial;
			this.AllMorphs = param.AllMorphs;
		}

		public unsafe MeCabNode Analyze(char* str, int len)
		{
			ThreadData threadData = new ThreadData();
			threadData.EndNodeList = new MeCabNode[len + 4];
			threadData.BeginNodeList = new MeCabNode[len + 4];
			ThreadData work = threadData;
			if (this.Partial)
			{
				string text = this.InitConstraints(str, len, work);
				fixed (char* str2 = text)
				{
					this.analyze(str2, text.Length, work);
					return this.buildLattice(work);
				}
			}
			this.analyze(str, len, work);
			return this.buildLattice(work);
		}

		private unsafe void ForwardBackward(char* sentence, int len, ThreadData work)
		{
			this.DoViterbi(sentence, len, work);
			work.EndNodeList[0].Alpha = 0f;
			for (int i = 0; i <= len; i++)
			{
				for (MeCabNode meCabNode = work.BeginNodeList[i]; meCabNode != null; meCabNode = meCabNode.BNext)
				{
					this.CalcAlpha(meCabNode, (double)this.theta);
				}
			}
			work.BeginNodeList[len].Beta = 0f;
			for (int num = len; num >= 0; num--)
			{
				for (MeCabNode meCabNode2 = work.EndNodeList[num]; meCabNode2 != null; meCabNode2 = meCabNode2.ENext)
				{
					this.CalcBeta(meCabNode2, (double)this.theta);
				}
			}
			work.Z = work.BeginNodeList[len].Alpha;
			for (int j = 0; j <= len; j++)
			{
				for (MeCabNode meCabNode3 = work.BeginNodeList[j]; meCabNode3 != null; meCabNode3 = meCabNode3.BNext)
				{
					meCabNode3.Prob = (float)Math.Exp((double)(meCabNode3.Alpha + meCabNode3.Beta - work.Z));
				}
			}
		}

		private void CalcAlpha(MeCabNode n, double beta)
		{
			n.Alpha = 0f;
			for (MeCabPath meCabPath = n.LPath; meCabPath != null; meCabPath = meCabPath.LNext)
			{
				n.Alpha = (float)Utils.LogSumExp((double)n.Alpha, (0.0 - beta) * (double)meCabPath.Cost + (double)meCabPath.LNode.Alpha, meCabPath == n.LPath);
			}
		}

		private void CalcBeta(MeCabNode n, double beta)
		{
			n.Beta = 0f;
			for (MeCabPath meCabPath = n.RPath; meCabPath != null; meCabPath = meCabPath.RNext)
			{
				n.Beta = (float)Utils.LogSumExp((double)n.Beta, (0.0 - beta) * (double)meCabPath.Cost + (double)meCabPath.RNode.Beta, meCabPath == n.RPath);
			}
		}

		private unsafe void DoViterbi(char* sentence, int len, ThreadData work)
		{
			work.BosNode = this.tokenizer.GetBosNode();
			work.BosNode.Length = len;
			char* ptr = sentence + len;
			work.BosNode.Surface = new string(sentence, 0, len);
			work.EndNodeList[0] = work.BosNode;
			for (int i = 0; i < len; i++)
			{
				if (work.EndNodeList[i] != null)
				{
					MeCabNode node = this.tokenizer.Lookup(sentence + i, ptr);
					node = this.FilterNode(node, i, work);
					node.BPos = i;
					node.EPos = i + node.RLength;
					work.BeginNodeList[i] = node;
					this.connect(i, node, work);
				}
			}
			work.EosNode = this.tokenizer.GetEosNode();
			work.EosNode.Surface = ptr->ToString();
			work.BeginNodeList[len] = work.EosNode;
			int num = len;
			while (true)
			{
				if (num >= 0)
				{
					if (work.EndNodeList[num] == null)
					{
						num--;
						continue;
					}
					break;
				}
				return;
			}
			this.connect(num, work.EosNode, work);
		}

		private void ConnectWithAllPath(int pos, MeCabNode rNode, ThreadData work)
		{
			while (true)
			{
				if (rNode != null)
				{
					long num = 2147483647L;
					MeCabNode meCabNode = null;
					for (MeCabNode meCabNode2 = work.EndNodeList[pos]; meCabNode2 != null; meCabNode2 = meCabNode2.ENext)
					{
						int num2 = this.connector.Cost(meCabNode2, rNode);
						long num3 = meCabNode2.Cost + num2;
						if (num3 < num)
						{
							meCabNode = meCabNode2;
							num = num3;
						}
						MeCabPath meCabPath = new MeCabPath();
						meCabPath.Cost = num2;
						meCabPath.RNode = rNode;
						meCabPath.LNode = meCabNode2;
						meCabPath.LNext = rNode.LPath;
						meCabPath.RNext = meCabNode2.RPath;
						MeCabPath meCabPath4 = meCabNode2.RPath = (rNode.LPath = meCabPath);
					}
					if (meCabNode != null)
					{
						rNode.Prev = meCabNode;
						rNode.Next = null;
						rNode.Cost = num;
						int num4 = rNode.RLength + pos;
						rNode.ENext = work.EndNodeList[num4];
						work.EndNodeList[num4] = rNode;
						rNode = rNode.BNext;
						continue;
					}
					break;
				}
				return;
			}
			throw new ArgumentException("too long sentence.");
		}

		private void ConnectNomal(int pos, MeCabNode rNode, ThreadData work)
		{
			while (true)
			{
				if (rNode != null)
				{
					long num = 2147483647L;
					MeCabNode meCabNode = null;
					for (MeCabNode meCabNode2 = work.EndNodeList[pos]; meCabNode2 != null; meCabNode2 = meCabNode2.ENext)
					{
						long num2 = meCabNode2.Cost + this.connector.Cost(meCabNode2, rNode);
						if (num2 < num)
						{
							meCabNode = meCabNode2;
							num = num2;
						}
					}
					if (meCabNode != null)
					{
						rNode.Prev = meCabNode;
						rNode.Next = null;
						rNode.Cost = num;
						int num3 = rNode.RLength + pos;
						rNode.ENext = work.EndNodeList[num3];
						work.EndNodeList[num3] = rNode;
						rNode = rNode.BNext;
						continue;
					}
					break;
				}
				return;
			}
			throw new MeCabException("too long sentence.");
		}

		private MeCabNode BuildAllLattice(ThreadData work)
		{
			if (this.BuildBestLattice(work) == null)
			{
				return null;
			}
			MeCabNode meCabNode = work.BosNode;
			for (int i = 0; i < work.BeginNodeList.Length; i++)
			{
				for (MeCabNode meCabNode2 = work.BeginNodeList[i]; meCabNode2 != null; meCabNode2 = meCabNode2.BNext)
				{
					meCabNode.Next = meCabNode2;
					meCabNode2.Prev = meCabNode;
					meCabNode = meCabNode2;
					for (MeCabPath meCabPath = meCabNode2.LPath; meCabPath != null; meCabPath = meCabPath.LNext)
					{
						meCabPath.Prob = meCabPath.LNode.Alpha - this.theta * (float)meCabPath.Cost + meCabPath.RNode.Beta - work.Z;
					}
				}
			}
			return work.BosNode;
		}

		private MeCabNode BuildBestLattice(ThreadData work)
		{
			MeCabNode meCabNode = work.EosNode;
			while (meCabNode.Prev != null)
			{
				meCabNode.IsBest = true;
				MeCabNode prev = meCabNode.Prev;
				prev.Next = meCabNode;
				meCabNode = prev;
			}
			return work.BosNode;
		}

		private unsafe string InitConstraints(char* sentence, int sentenceLen, ThreadData work)
		{
			string text = new string(sentence, 0, sentenceLen);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(' ');
			int num = 0;
			string[] array = text.Split('\r', '\n');
			foreach (string text2 in array)
			{
				if (!(text2 == ""))
				{
					if (text2 == "EOS")
					{
						break;
					}
					string[] array2 = text2.Split('\t');
					stringBuilder.Append(array2[0]).Append(' ');
					int length = array2[0].Length;
					if (array2.Length == 2)
					{
						if (array2[1] == "\0")
						{
							throw new ArgumentException("use \\t as separator");
						}
						MeCabNode newNode = this.tokenizer.GetNewNode();
						newNode.Surface = array2[0];
						newNode.Feature = array2[1];
						newNode.Length = length;
						newNode.RLength = length + 1;
						newNode.BNext = null;
						newNode.WCost = 0;
						work.BeginNodeList[num] = newNode;
					}
					num += length + 1;
				}
			}
			return stringBuilder.ToString();
		}

		private MeCabNode FilterNode(MeCabNode node, int pos, ThreadData work)
		{
			if (!this.Partial)
			{
				return node;
			}
			MeCabNode meCabNode = work.BeginNodeList[pos];
			if (meCabNode == null)
			{
				return node;
			}
			bool flag = meCabNode.Feature == "*";
			MeCabNode meCabNode2 = null;
			MeCabNode meCabNode3 = null;
			for (MeCabNode meCabNode4 = node; meCabNode4 != null; meCabNode4 = meCabNode4.BNext)
			{
				if (meCabNode.Surface == meCabNode4.Surface && (flag || this.PartialMatch(meCabNode.Feature, meCabNode4.Feature)))
				{
					if (meCabNode2 != null)
					{
						meCabNode2.BNext = meCabNode4;
						meCabNode2 = meCabNode4;
					}
					else
					{
						meCabNode3 = meCabNode4;
						meCabNode2 = meCabNode3;
					}
				}
			}
			if (meCabNode3 == null)
			{
				meCabNode3 = meCabNode;
			}
			if (meCabNode2 != null)
			{
				meCabNode2.BNext = null;
			}
			return meCabNode3;
		}

		private bool PartialMatch(string f1, string f2)
		{
			string[] array = f1.Split(',');
			string[] array2 = f2.Split(',');
			int num = Math.Min(array.Length, array2.Length);
			for (int i = 0; i < num; i++)
			{
				if (array[i] != "*" && array2[i] != "*" && array[i] != array2[i])
				{
					return false;
				}
			}
			return true;
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
				if (disposing)
				{
					this.tokenizer.Dispose();
					this.connector.Dispose();
				}
				this.disposed = true;
			}
		}

		~Viterbi()
		{
			this.Dispose(false);
		}
	}
}
