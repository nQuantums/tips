using System;
using System.IO;

namespace NMeCab.Core
{
	public class Tokenizer : IDisposable
	{
		private const string SysDicFile = "sys.dic";

		private const string UnkDicFile = "unk.dic";

		private const int DAResultSize = 512;

		private const int DefaltMaxGroupingSize = 24;

		private const string BosKey = "BOS/EOS";

		private MeCabDictionary[] dic;

		private readonly MeCabDictionary unkDic = new MeCabDictionary();

		private string bosFeature;

		private string unkFeature;

		private Token[][] unkTokens;

		private CharInfo space;

		private readonly CharProperty property = new CharProperty();

		private int maxGroupingSize;

		private bool disposed;

		public void Open(MeCabParam param)
		{
			this.dic = new MeCabDictionary[param.UserDic.Length + 1];
			string dicDir = param.DicDir;
			this.property.Open(dicDir);
			this.unkDic.Open(Path.Combine(dicDir, "unk.dic"));
			if (this.unkDic.Type != DictionaryType.Unk)
			{
				throw new MeCabInvalidFileException("not a unk dictionary", this.unkDic.FileName);
			}
			MeCabDictionary meCabDictionary = new MeCabDictionary();
			meCabDictionary.Open(Path.Combine(dicDir, "sys.dic"));
			if (meCabDictionary.Type != 0)
			{
				throw new MeCabInvalidFileException("not a system dictionary", meCabDictionary.FileName);
			}
			this.dic[0] = meCabDictionary;
			for (int i = 0; i < param.UserDic.Length; i++)
			{
				MeCabDictionary meCabDictionary2 = new MeCabDictionary();
				meCabDictionary2.Open(Path.Combine(dicDir, param.UserDic[i]));
				if (meCabDictionary2.Type != DictionaryType.Usr)
				{
					throw new MeCabInvalidFileException("not a user dictionary", meCabDictionary2.FileName);
				}
				if (!meCabDictionary.IsCompatible(meCabDictionary2))
				{
					throw new MeCabInvalidFileException("incompatible dictionary", meCabDictionary2.FileName);
				}
				this.dic[i + 1] = meCabDictionary2;
			}
			this.unkTokens = new Token[this.property.Size][];
			for (int j = 0; j < this.unkTokens.Length; j++)
			{
				string text = this.property.Name(j);
				DoubleArray.ResultPair n = this.unkDic.ExactMatchSearch(text);
				if (n.Value == -1)
				{
					throw new MeCabInvalidFileException("cannot find UNK category: " + text, this.unkDic.FileName);
				}
				this.unkTokens[j] = this.unkDic.GetToken(n);
			}
			this.space = this.property.GetCharInfo(' ');
			this.bosFeature = param.BosFeature;
			this.unkFeature = param.UnkFeature;
			this.maxGroupingSize = param.MaxGroupingSize;
			if (this.maxGroupingSize <= 0)
			{
				this.maxGroupingSize = 24;
			}
		}

		public unsafe MeCabNode Lookup(char* begin, char* end)
		{
			MeCabNode meCabNode = null;
			if (end - begin > 65535)
			{
				end = begin + 65535;
			}
			CharInfo charInfo = default(CharInfo);
			int num = default(int);
			char* ptr = this.property.SeekToOtherType(begin, end, this.space, &charInfo, &num);
			DoubleArray.ResultPair* ptr2 = stackalloc DoubleArray.ResultPair[512];
			MeCabDictionary[] array = this.dic;
			foreach (MeCabDictionary meCabDictionary in array)
			{
				int num2 = meCabDictionary.CommonPrefixSearch(ptr, (int)(end - ptr), ptr2, 512);
				for (int j = 0; j < num2; j++)
				{
					Token[] token = meCabDictionary.GetToken(ptr2[j]);
					for (int k = 0; k < token.Length; k++)
					{
						MeCabNode newNode = this.GetNewNode();
						this.ReadNodeInfo(meCabDictionary, token[k], newNode);
						newNode.Length = ptr2[j].Length;
						newNode.RLength = (int)(ptr - begin) + ptr2[j].Length;
						newNode.Surface = new string(ptr, 0, ptr2[j].Length);
						newNode.Stat = MeCabNodeStat.Nor;
						newNode.CharType = charInfo.DefaultType;
						newNode.BNext = meCabNode;
						meCabNode = newNode;
					}
				}
			}
			if (meCabNode != null && !charInfo.Invoke)
			{
				return meCabNode;
			}
			char* ptr3 = ptr + 1;
			char* ptr4 = null;
			if (ptr3 > end)
			{
				this.AddUnknown(ref meCabNode, charInfo, begin, ptr, ptr3);
				return meCabNode;
			}
			if (charInfo.Group)
			{
				char* ptr5 = ptr3;
				CharInfo charInfo2 = default(CharInfo);
				ptr3 = this.property.SeekToOtherType(ptr3, end, charInfo, &charInfo2, &num);
				if (num <= this.maxGroupingSize)
				{
					this.AddUnknown(ref meCabNode, charInfo, begin, ptr, ptr3);
				}
				ptr4 = ptr3;
				ptr3 = ptr5;
			}
			for (int l = 1; l <= charInfo.Length; l++)
			{
				if (ptr3 > end)
				{
					break;
				}
				if (ptr3 != ptr4)
				{
					num = l;
					this.AddUnknown(ref meCabNode, charInfo, begin, ptr, ptr3);
					if (!charInfo.IsKindOf(this.property.GetCharInfo(*ptr3)))
					{
						break;
					}
					ptr3++;
				}
			}
			if (meCabNode == null)
			{
				this.AddUnknown(ref meCabNode, charInfo, begin, ptr, ptr3);
			}
			return meCabNode;
		}

		private void ReadNodeInfo(MeCabDictionary dic, Token token, MeCabNode node)
		{
			node.LCAttr = token.LcAttr;
			node.RCAttr = token.RcAttr;
			node.PosId = token.PosId;
			node.WCost = token.WCost;
			node.SetFeature(token.Feature, dic);
		}

		private unsafe void AddUnknown(ref MeCabNode resultNode, CharInfo cInfo, char* begin, char* begin2, char* begin3)
		{
			Token[] array = this.unkTokens[cInfo.DefaultType];
			for (int i = 0; i < array.Length; i++)
			{
				MeCabNode newNode = this.GetNewNode();
				this.ReadNodeInfo(this.unkDic, array[i], newNode);
				newNode.CharType = cInfo.DefaultType;
				newNode.Surface = new string(begin2, 0, (int)(begin3 - begin2));
				newNode.Length = (int)(begin3 - begin2);
				newNode.RLength = (int)(begin3 - begin);
				newNode.BNext = resultNode;
				newNode.Stat = MeCabNodeStat.Unk;
				if (this.unkFeature != null)
				{
					newNode.Feature = this.unkFeature;
				}
				resultNode = newNode;
			}
		}

		public MeCabNode GetBosNode()
		{
			MeCabNode newNode = this.GetNewNode();
			newNode.Surface = "BOS/EOS";
			newNode.Feature = this.bosFeature;
			newNode.IsBest = true;
			newNode.Stat = MeCabNodeStat.Bos;
			return newNode;
		}

		public MeCabNode GetEosNode()
		{
			MeCabNode bosNode = this.GetBosNode();
			bosNode.Stat = MeCabNodeStat.Eos;
			return bosNode;
		}

		public MeCabNode GetNewNode()
		{
			return new MeCabNode();
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
					if (this.dic != null)
					{
						MeCabDictionary[] array = this.dic;
						foreach (MeCabDictionary meCabDictionary in array)
						{
							if (meCabDictionary != null)
							{
								meCabDictionary.Dispose();
							}
						}
					}
					if (this.unkDic != null)
					{
						this.unkDic.Dispose();
					}
				}
				this.disposed = true;
			}
		}

		~Tokenizer()
		{
			this.Dispose(false);
		}
	}
}
