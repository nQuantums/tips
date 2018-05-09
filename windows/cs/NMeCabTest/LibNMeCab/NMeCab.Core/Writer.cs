using System;
using System.Text;

namespace NMeCab.Core
{
	public class Writer
	{
		private delegate void WriteAction(StringBuilder os, MeCabNode bosNode);

		private const string FloatFormat = "f6";

		private WriteAction write;

		private string outputFormatType;

		public string OutputFormatType
		{
			get
			{
				return this.outputFormatType;
			}
			set
			{
				this.outputFormatType = value;
				switch (value)
				{
				case "lattice":
					this.write = this.WriteLattice;
					break;
				case "wakati":
					this.write = this.WriteWakati;
					break;
				case "none":
					this.write = this.WriteNone;
					break;
				case "dump":
					this.write = this.WriteDump;
					break;
				case "em":
					this.write = this.WriteEM;
					break;
				default:
					throw new ArgumentOutOfRangeException(value + " is not supported Format");
				}
			}
		}

		public void Open(MeCabParam param)
		{
			this.OutputFormatType = param.OutputFormatType;
		}

		public void Write(StringBuilder os, MeCabNode bosNode)
		{
			this.write(os, bosNode);
		}

		public void WriteLattice(StringBuilder os, MeCabNode bosNode)
		{
			MeCabNode next = bosNode.Next;
			while (next.Next != null)
			{
				os.Append(next.Surface);
				os.Append("\t");
				os.Append(next.Feature);
				os.AppendLine();
				next = next.Next;
			}
			os.AppendLine("EOS");
		}

		public void WriteWakati(StringBuilder os, MeCabNode bosNode)
		{
			MeCabNode next = bosNode.Next;
			if (next.Next != null)
			{
				os.Append(next.Surface);
				next = next.Next;
				while (next.Next != null)
				{
					os.Append(" ");
					os.Append(next.Surface);
					next = next.Next;
				}
			}
			os.AppendLine();
		}

		public void WriteNone(StringBuilder os, MeCabNode bosNode)
		{
		}

		public void WriteUser(StringBuilder os, MeCabNode bosNode)
		{
			throw new NotImplementedException();
		}

		public void WriteEM(StringBuilder os, MeCabNode bosNode)
		{
			for (MeCabNode meCabNode = bosNode; meCabNode != null; meCabNode = meCabNode.Next)
			{
				if (meCabNode.Prob >= 0.0001f)
				{
					os.Append("U\t");
					if (meCabNode.Stat == MeCabNodeStat.Bos)
					{
						os.Append("BOS");
					}
					else if (meCabNode.Stat == MeCabNodeStat.Eos)
					{
						os.Append("EOS");
					}
					else
					{
						os.Append(meCabNode.Surface);
					}
					os.Append("\t").Append(meCabNode.Feature);
					os.Append("\t").Append(meCabNode.Prob.ToString("f6"));
					os.AppendLine();
				}
				for (MeCabPath meCabPath = meCabNode.LPath; meCabPath != null; meCabPath = meCabPath.LNext)
				{
					if (meCabPath.Prob >= 0.0001f)
					{
						os.Append("B\t").Append(meCabPath.LNode.Feature);
						os.Append("\t").Append(meCabNode.Feature);
						os.Append("\t").Append(meCabPath.Prob.ToString("f6"));
						os.AppendLine();
					}
				}
			}
			os.AppendLine("EOS");
		}

		public void WriteDump(StringBuilder os, MeCabNode bosNode)
		{
			for (MeCabNode meCabNode = bosNode; meCabNode != null; meCabNode = meCabNode.Next)
			{
				if (meCabNode.Stat == MeCabNodeStat.Bos)
				{
					os.Append("BOS");
				}
				else if (meCabNode.Stat == MeCabNodeStat.Eos)
				{
					os.Append("EOS");
				}
				else
				{
					os.Append(meCabNode.Surface);
				}
				os.Append(" ").Append(meCabNode.Feature);
				os.Append(" ").Append(meCabNode.BPos);
				os.Append(" ").Append(meCabNode.EPos);
				os.Append(" ").Append(meCabNode.RCAttr);
				os.Append(" ").Append(meCabNode.LCAttr);
				os.Append(" ").Append(meCabNode.PosId);
				os.Append(" ").Append(meCabNode.CharType);
				os.Append(" ").Append((int)meCabNode.Stat);
				os.Append(" ").Append(meCabNode.IsBest ? "1" : "0");
				os.Append(" ").Append(meCabNode.Alpha.ToString("f6"));
				os.Append(" ").Append(meCabNode.Beta.ToString("f6"));
				os.Append(" ").Append(meCabNode.Prob.ToString("f6"));
				os.Append(" ").Append(meCabNode.Cost);
				for (MeCabPath meCabPath = meCabNode.LPath; meCabPath != null; meCabPath = meCabPath.LNext)
				{
					os.Append(" ");
					os.Append(":").Append(meCabPath.Cost);
					os.Append(":").Append(meCabPath.Prob.ToString("f6"));
				}
				os.AppendLine();
			}
		}

		public unsafe void WriteNode(StringBuilder os, char* p, string sentence, MeCabNode node)
		{
			while (*p != 0)
			{
				char c = *p;
				if (c != '%')
				{
					os.Append(*p);
				}
				else
				{
					switch (*(++p))
					{
					default:
						os.Append("unkonwn meta char ").Append(*p);
						break;
					case 'S':
						os.Append(sentence);
						break;
					case 'L':
						os.Append(sentence.Length);
						break;
					case 'm':
						os.Append(node.Surface);
						break;
					case 'M':
						os.Append(sentence, node.BPos - node.RLength + node.Length, node.RLength);
						break;
					case 'h':
						os.Append(node.PosId);
						break;
					case '%':
						os.Append('%');
						break;
					case 'c':
						os.Append(node.WCost);
						break;
					case 'H':
						os.Append(node.Feature);
						break;
					case 't':
						os.Append(node.CharType);
						break;
					case 's':
						os.Append(node.Stat);
						break;
					case 'P':
						os.Append(node.Prob);
						break;
					case 'p':
						switch (*(++p))
						{
						default:
							throw new ArgumentException("[iseSCwcnblLh] is required after %p");
						case 'i':
							throw new ArgumentException("%pi is not supported");
						case 'S':
							os.Append(sentence, node.BPos, node.RLength - node.Length);
							break;
						case 's':
							os.Append(node.BPos);
							break;
						case 'e':
							os.Append(node.EPos);
							break;
						case 'C':
							os.Append(node.Cost - node.Prev.Cost - node.WCost);
							break;
						case 'w':
							os.Append(node.WCost);
							break;
						case 'c':
							os.Append(node.Cost);
							break;
						case 'n':
							os.Append(node.Cost - node.Prev.Cost);
							break;
						case 'b':
							os.Append((char)(node.IsBest ? 42 : 32));
							break;
						case 'P':
							os.Append(node.Prob);
							break;
						case 'A':
							os.Append(node.Alpha);
							break;
						case 'B':
							os.Append(node.Beta);
							break;
						case 'l':
							os.Append(node.Length);
							break;
						case 'L':
							os.Append(node.RLength);
							break;
						case 'h':
							switch (*(++p))
							{
							default:
								throw new ArgumentException("lr is required after %ph");
							case 'l':
								os.Append(node.LCAttr);
								break;
							case 'r':
								os.Append(node.RCAttr);
								break;
							}
							break;
						case 'p':
						{
							char c2 = *(++p);
							char c3 = *(++p);
							if (c3 == '\\')
							{
								c3 = this.GetEscapedChar(*(++p));
							}
							if (node.LPath == null)
							{
								throw new InvalidOperationException("no path information, use -l option");
							}
							for (MeCabPath meCabPath = node.LPath; meCabPath != null; meCabPath = meCabPath.LNext)
							{
								if (meCabPath != node.LPath)
								{
									os.Append(c3);
								}
								switch (c2)
								{
								case 'i':
									os.Append(meCabPath.LNode.PosId);
									break;
								case 'c':
									os.Append(meCabPath.Cost);
									break;
								case 'P':
									os.Append(meCabPath.Prob);
									break;
								default:
									throw new ArgumentException("[icP] is required after %pp");
								}
							}
							break;
						}
						}
						break;
					case 'F':
					case 'f':
					{
						char value = '\t';
						if (*p == 'F')
						{
							value = ((*(++p) != '\\') ? (*p) : this.GetEscapedChar(*(++p)));
						}
						if (*(++p) != '[')
						{
							throw new ArgumentException("cannot find '['");
						}
						string[] array = node.Feature.Split(',');
						int num = 0;
						while (true)
						{
							if (char.IsDigit(*(++p)))
							{
								num = num * 10 + (*p - 48);
							}
							else
							{
								if (num >= array.Length)
								{
									throw new ArgumentException("given index is out of range");
								}
								os.Append(array[num]);
								if (*(++p) != ',')
								{
									break;
								}
								os.Append(value);
								num = 0;
							}
						}
						if (*p == ']')
						{
							break;
						}
						throw new ArgumentException("cannot find ']'");
					}
					}
				}
				p++;
			}
		}

		private char GetEscapedChar(char p)
		{
			switch (p)
			{
			case '0':
				return '\0';
			case 'a':
				return '\a';
			case 'b':
				return '\b';
			case 't':
				return '\t';
			case 'n':
				return '\n';
			case 'v':
				return '\v';
			case 'f':
				return '\f';
			case 'r':
				return '\r';
			case 's':
				return ' ';
			case '\\':
				return '\\';
			default:
				return '\0';
			}
		}
	}
}
