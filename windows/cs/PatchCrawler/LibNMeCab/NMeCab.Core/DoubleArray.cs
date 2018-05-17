using System;
using System.IO;

namespace NMeCab.Core
{
	public class DoubleArray : IDisposable
	{
		private struct Unit
		{
			public readonly int Base;

			public readonly uint Check;

			public Unit(BinaryReader reader)
			{
				this.Base = reader.ReadInt32();
				this.Check = reader.ReadUInt32();
			}
		}

		public struct ResultPair
		{
			public int Value;

			public int Length;

			public ResultPair(int r, int t)
			{
				this.Value = r;
				this.Length = t;
			}
		}

		public const int UnitSize = 8;

		private Unit[] array;

		private bool disposed;

		public int Size
		{
			get
			{
				return this.array.Length;
			}
		}

		public int TotalSize
		{
			get
			{
				return this.Size * 8;
			}
		}

		public void Open(BinaryReader reader, uint size)
		{
			this.array = new Unit[size / 8u];
			for (int i = 0; i < this.array.Length; i++)
			{
				this.array[i] = new Unit(reader);
			}
		}

		public unsafe void ExactMatchSearch(byte* key, ResultPair* result, int len, int nodePos)
		{
			*result = this.ExactMatchSearch(key, len, nodePos);
		}

		public unsafe ResultPair ExactMatchSearch(byte* key, int len, int nodePos)
		{
			int num = this.ReadBase(nodePos);
			Unit unit = default(Unit);
			for (int i = 0; i < len; i++)
			{
				this.ReadUnit(num + key[i] + 1, out unit);
				if (num != unit.Check)
				{
					return new ResultPair(-1, 0);
				}
				num = unit.Base;
			}
			this.ReadUnit(num, out unit);
			int @base = unit.Base;
			if (num == unit.Check && @base < 0)
			{
				return new ResultPair(-@base - 1, len);
			}
			return new ResultPair(-1, 0);
		}

		public unsafe int CommonPrefixSearch(byte* key, ResultPair* result, int resultLen, int len, int nodePos = 0)
		{
			int num = this.ReadBase(nodePos);
			int num2 = 0;
			Unit unit = default(Unit);
			int @base;
			for (int i = 0; i < len; i++)
			{
				this.ReadUnit(num, out unit);
				@base = unit.Base;
				if (num == unit.Check && @base < 0)
				{
					if (num2 < resultLen)
					{
						result[num2] = new ResultPair(-@base - 1, i);
					}
					num2++;
				}
				this.ReadUnit(num + key[i] + 1, out unit);
				if (num != unit.Check)
				{
					return num2;
				}
				num = unit.Base;
			}
			this.ReadUnit(num, out unit);
			@base = unit.Base;
			if (num == unit.Check && @base < 0)
			{
				if (num2 < resultLen)
				{
					result[num2] = new ResultPair(-@base - 1, len);
				}
				num2++;
			}
			return num2;
		}

		private int ReadBase(int pos)
		{
			return this.array[pos].Base;
		}

		private void ReadUnit(int pos, out Unit unit)
		{
			unit = this.array[pos];
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
				this.disposed = true;
			}
		}

		~DoubleArray()
		{
			this.Dispose(false);
		}
	}
}
