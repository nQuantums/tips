using System;
using System.Collections.Generic;

namespace NMeCab.Core
{
	public class PriorityQueue<T> where T : IComparable<T>
	{
		private readonly List<T> list = new List<T>();

		public int Count
		{
			get
			{
				return this.list.Count;
			}
		}

		public void Clear()
		{
			this.list.Clear();
		}

		public int Push(T item)
		{
			int num = this.SearchIndex(this.list, item);
			this.list.Insert(num, item);
			return num;
		}

		public T Pop()
		{
			if (this.Count == 0)
			{
				throw new InvalidOperationException("Empty");
			}
			T result = this.list[0];
			this.list.RemoveAt(0);
			return result;
		}

		private int SearchIndex(List<T> list, T item)
		{
			int num = 0;
			int num2 = list.Count;
			while (num < num2)
			{
				int num3 = (num + num2) / 2;
				if (list[num3].CompareTo(item) <= 0)
				{
					num = num3 + 1;
				}
				else
				{
					num2 = num3;
				}
			}
			return num;
		}
	}
}
