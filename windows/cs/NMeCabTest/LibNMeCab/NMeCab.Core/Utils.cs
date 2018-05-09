using System;

namespace NMeCab.Core
{
	public static class Utils
	{
		public static double LogSumExp(double x, double y, bool flg)
		{
			if (flg)
			{
				return y;
			}
			double num = Math.Min(x, y);
			double num2 = Math.Max(x, y);
			if (num2 > num + 50.0)
			{
				return num2;
			}
			return num2 + Math.Log(Math.Exp(num - num2) + 1.0);
		}
	}
}
