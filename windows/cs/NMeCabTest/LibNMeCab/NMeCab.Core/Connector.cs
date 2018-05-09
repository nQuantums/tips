using System;
using System.IO;

namespace NMeCab.Core
{
	public class Connector : IDisposable
	{
		private const string MatrixFile = "matrix.bin";

		private short[] matrix;

		private bool disposed;

		public ushort LSize
		{
			get;
			private set;
		}

		public ushort RSize
		{
			get;
			private set;
		}

		public void Open(MeCabParam param)
		{
			string fileName = Path.Combine(param.DicDir, "matrix.bin");
			this.Open(fileName);
		}

		public void Open(string fileName)
		{
			using (FileStream input = new FileStream(fileName, FileMode.Open, FileAccess.Read))
			{
				using (BinaryReader reader = new BinaryReader(input))
				{
					this.Open(reader, fileName);
				}
			}
		}

		public void Open(BinaryReader reader, string fileName = null)
		{
			this.LSize = reader.ReadUInt16();
			this.RSize = reader.ReadUInt16();
			this.matrix = new short[this.LSize * this.RSize];
			for (int i = 0; i < this.matrix.Length; i++)
			{
				this.matrix[i] = reader.ReadInt16();
			}
			if (reader.BaseStream.ReadByte() == -1)
			{
				return;
			}
			throw new MeCabInvalidFileException("file size is invalid", fileName);
		}

		public int Cost(MeCabNode lNode, MeCabNode rNode)
		{
			int num = lNode.RCAttr + this.LSize * rNode.LCAttr;
			return this.matrix[num] + rNode.WCost;
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

		~Connector()
		{
			this.Dispose(false);
		}
	}
}
