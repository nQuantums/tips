using NMeCab.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace NMeCab
{
	public class MeCabTagger : IDisposable
	{
		private readonly Viterbi viterbi = new Viterbi();

		private readonly Writer writer = new Writer();

		private bool disposed;

		public bool Partial
		{
			get
			{
				this.ThrowIfDisposed();
				return this.viterbi.Partial;
			}
			set
			{
				this.ThrowIfDisposed();
				this.viterbi.Partial = value;
			}
		}

		public float Theta
		{
			get
			{
				this.ThrowIfDisposed();
				return this.viterbi.Theta;
			}
			set
			{
				this.ThrowIfDisposed();
				this.viterbi.Theta = value;
			}
		}

		public MeCabLatticeLevel LatticeLevel
		{
			get
			{
				this.ThrowIfDisposed();
				return this.viterbi.LatticeLevel;
			}
			set
			{
				this.ThrowIfDisposed();
				this.viterbi.LatticeLevel = value;
			}
		}

		public bool AllMorphs
		{
			get
			{
				this.ThrowIfDisposed();
				return this.viterbi.AllMorphs;
			}
			set
			{
				this.ThrowIfDisposed();
				this.viterbi.AllMorphs = value;
			}
		}

		public string OutPutFormatType
		{
			get
			{
				this.ThrowIfDisposed();
				return this.writer.OutputFormatType;
			}
			set
			{
				this.ThrowIfDisposed();
				this.writer.OutputFormatType = value;
			}
		}

		private MeCabTagger()
		{
		}

		private void Open(MeCabParam param)
		{
			this.viterbi.Open(param);
			this.writer.Open(param);
		}

		public static MeCabTagger Create()
		{
			MeCabParam meCabParam = new MeCabParam();
			meCabParam.LoadDicRC();
			return MeCabTagger.Create(meCabParam);
		}

		public static MeCabTagger Create(MeCabParam param)
		{
			MeCabTagger meCabTagger = new MeCabTagger();
			meCabTagger.Open(param);
			return meCabTagger;
		}

		public unsafe string Parse(string str)
		{
			fixed (char* str2 = str)
			{
				return this.Parse(str2, str.Length);
			}
		}

		public unsafe string Parse(char* str, int len)
		{
			MeCabNode meCabNode = this.ParseToNode(str, len);
			if (meCabNode == null)
			{
				return null;
			}
			StringBuilder stringBuilder = new StringBuilder();
			this.writer.Write(stringBuilder, meCabNode);
			return stringBuilder.ToString();
		}

		public unsafe MeCabNode ParseToNode(string str)
		{
			if (str == null)
			{
				throw new ArgumentNullException("str");
			}
			fixed (char* str2 = str)
			{
				return this.ParseToNode(str2, str.Length);
			}
		}

		public unsafe MeCabNode ParseToNode(char* str, int len)
		{
			this.ThrowIfDisposed();
			return this.viterbi.Analyze(str, len);
		}

		public unsafe IEnumerable<MeCabNode> ParseNBestToNode(string str)
		{
			fixed (char* str2 = str)
			{
				return this.ParseNBestToNode(str2, str.Length);
			}
		}

		public unsafe IEnumerable<MeCabNode> ParseNBestToNode(char* str, int len)
		{
			if (this.LatticeLevel == MeCabLatticeLevel.Zero)
			{
				throw new InvalidOperationException("Please set one or more to LatticeLevel.");
			}
			MeCabNode node = this.ParseToNode(str, len);
			NBestGenerator nBestGenerator = new NBestGenerator();
			nBestGenerator.Set(node);
			return nBestGenerator.GetEnumerator();
		}

		public unsafe string ParseNBest(int n, string str)
		{
			fixed (char* str2 = str)
			{
				return this.ParseNBest(n, str2, str.Length);
			}
		}

		public unsafe string ParseNBest(int n, char* str, int len)
		{
			if (n <= 0)
			{
				throw new ArgumentOutOfRangeException("n", "");
			}
			if (n == 1)
			{
				return this.Parse(str, len);
			}
			StringBuilder stringBuilder = new StringBuilder();
			foreach (MeCabNode item in this.ParseNBestToNode(str, len))
			{
				this.writer.Write(stringBuilder, item);
				if (--n == 0)
				{
					break;
				}
			}
			return stringBuilder.ToString();
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
					this.viterbi.Dispose();
				}
				this.disposed = true;
			}
		}

		~MeCabTagger()
		{
			this.Dispose(false);
		}

		private void ThrowIfDisposed()
		{
			if (!this.disposed)
			{
				return;
			}
			throw new ObjectDisposedException(base.GetType().FullName);
		}
	}
}
