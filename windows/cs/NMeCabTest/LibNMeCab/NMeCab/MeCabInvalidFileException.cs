using System.Text;

namespace NMeCab
{
	public class MeCabInvalidFileException : MeCabException
	{
		public string FileName
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
				if (this.FileName != null)
				{
					stringBuilder.AppendFormat("[FileName:{0}]", this.FileName);
				}
				return stringBuilder.ToString();
			}
		}

		public MeCabInvalidFileException(string message, string fileName)
			: base(message)
		{
			this.FileName = fileName;
		}
	}
}
