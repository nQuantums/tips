using System;

namespace NMeCab
{
	public class MeCabException : Exception
	{
		public MeCabException(string message)
			: base(message)
		{
		}

		public MeCabException(string message, Exception ex)
			: base(message, ex)
		{
		}
	}
}
