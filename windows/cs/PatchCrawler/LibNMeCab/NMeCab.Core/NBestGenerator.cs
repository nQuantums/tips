using System;
using System.Collections.Generic;

namespace NMeCab.Core
{
	public class NBestGenerator
	{
		private class QueueElement : IComparable<QueueElement>
		{
			public MeCabNode Node
			{
				get;
				set;
			}

			public QueueElement Next
			{
				get;
				set;
			}

			public long Fx
			{
				get;
				set;
			}

			public long Gx
			{
				get;
				set;
			}

			public int CompareTo(QueueElement other)
			{
				return this.Fx.CompareTo(other.Fx);
			}

			public override string ToString()
			{
				return this.Node.ToString();
			}
		}

		private PriorityQueue<QueueElement> agenda = new PriorityQueue<QueueElement>();

		public void Set(MeCabNode node)
		{
			while (node.Next != null)
			{
				node = node.Next;
			}
			this.agenda.Clear();
			QueueElement queueElement = new QueueElement();
			queueElement.Node = node;
			queueElement.Next = null;
			queueElement.Fx = 0L;
			queueElement.Gx = 0L;
			QueueElement item = queueElement;
			this.agenda.Push(item);
		}

		public MeCabNode Next()
		{
			while (this.agenda.Count != 0)
			{
				QueueElement queueElement = this.agenda.Pop();
				MeCabNode node = queueElement.Node;
				if (node.Stat == MeCabNodeStat.Bos)
				{
					QueueElement queueElement2 = queueElement;
					while (queueElement2.Next != null)
					{
						queueElement2.Node.Next = queueElement2.Next.Node;
						queueElement2.Next.Node.Prev = queueElement2.Node;
						queueElement2 = queueElement2.Next;
					}
					return node;
				}
				for (MeCabPath meCabPath = node.LPath; meCabPath != null; meCabPath = meCabPath.LNext)
				{
					QueueElement queueElement3 = new QueueElement();
					queueElement3.Node = meCabPath.LNode;
					queueElement3.Gx = meCabPath.Cost + queueElement.Gx;
					queueElement3.Fx = meCabPath.LNode.Cost + meCabPath.Cost + queueElement.Gx;
					queueElement3.Next = queueElement;
					QueueElement item = queueElement3;
					this.agenda.Push(item);
				}
			}
			return null;
		}

		public IEnumerable<MeCabNode> GetEnumerator()
		{
			for (MeCabNode rNode = this.Next(); rNode != null; rNode = this.Next())
			{
				yield return rNode;
			}
		}
	}
}
