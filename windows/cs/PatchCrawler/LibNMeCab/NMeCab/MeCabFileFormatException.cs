using System.Text;

namespace NMeCab
{
	public class MeCabFileFormatException : MeCabInvalidFileException
	{
		public int LineNo
		{
			get;
			private set;
		}

		public string Line
		{
			get;
			private set;
		}

		public override string Message
		{
			get
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append(base.Message);
				if (this.LineNo > 0)
				{
					stringBuilder.AppendFormat("[LineNo:{0}]", this.LineNo);
				}
				if (this.Line != null)
				{
					stringBuilder.AppendFormat("[Line:{0}]", this.Line);
				}
				return stringBuilder.ToString();
			}
		}

		public MeCabFileFormatException(string message, string fileName = null, int lineNo = -1, string line = null)
			: base(message, fileName)
		{
			this.LineNo = lineNo;
			this.Line = line;
		}
	}
}
