using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NMeCab.Core
{
	public class IniParser
	{
		private readonly Dictionary<string, string> dic = new Dictionary<string, string>();

		public char SplitChar
		{
			get;
			set;
		}

		public char[] SkipChars
		{
			get;
			set;
		}

		public char[] TrimChars
		{
			get;
			set;
		}

		public bool IsRewrites
		{
			get;
			set;
		}

		public string this[string key]
		{
			get
			{
				return this.dic[key];
			}
			set
			{
				this.dic[key] = value;
			}
		}

		public IniParser()
		{
			this.SplitChar = '=';
			this.SkipChars = new char[2]
			{
				';',
				'#'
			};
			this.TrimChars = new char[2]
			{
				' ',
				'\t'
			};
		}

		public void Load(string fileName, Encoding encoding)
		{
			using (TextReader reader = new StreamReader(fileName, encoding))
			{
				this.Load(reader, fileName);
			}
		}

		public void Load(TextReader reader, string fileName = null)
		{
			int num = 0;
			string text = reader.ReadLine();
			while (true)
			{
				if (text != null)
				{
					num++;
					text = text.Trim(this.TrimChars);
					if (!(text == "") && Array.IndexOf(this.SkipChars, text[0]) == -1)
					{
						int num2 = text.IndexOf(this.SplitChar);
						if (num2 > 0)
						{
							string key = text.Substring(0, num2).TrimEnd(this.TrimChars);
							if (this.IsRewrites || !this.dic.ContainsKey(key))
							{
								string value = text.Substring(num2 + 1).TrimStart(this.TrimChars);
								this.dic[key] = value;
							}
							goto IL_00b1;
						}
						break;
					}
					goto IL_00b1;
				}
				return;
				IL_00b1:
				text = reader.ReadLine();
			}
			throw new MeCabFileFormatException("Format error.", fileName, num, text);
		}

		public void Clear()
		{
			this.dic.Clear();
		}
	}
}
