using NMeCab.Core;
using NMeCab.Properties;
using System.IO;
using System.Text;

namespace NMeCab
{
	public class MeCabParam
	{
		public const float DefaultTheta = 0.75f;

		public const string DefaultRcFile = "dicrc";

		public string DicDir
		{
			get;
			set;
		}

		public string[] UserDic
		{
			get;
			set;
		}

		public int MaxGroupingSize
		{
			get;
			set;
		}

		public string BosFeature
		{
			get;
			set;
		}

		public string UnkFeature
		{
			get;
			set;
		}

		public bool AlloCateSentence
		{
			get;
			set;
		}

		public int CostFactor
		{
			get;
			set;
		}

		public float Theta
		{
			get;
			set;
		}

		public MeCabLatticeLevel LatticeLevel
		{
			get;
			set;
		}

		public bool Partial
		{
			get;
			set;
		}

		public bool AllMorphs
		{
			get;
			set;
		}

		public string OutputFormatType
		{
			get;
			set;
		}

		public string RcFile
		{
			get;
			set;
		}

		public MeCabParam()
		{
			this.Theta = 0.75f;
			this.RcFile = "dicrc";
			Settings @default = Settings.Default;
			this.DicDir = @default.DicDir;
			this.UserDic = this.SplitStringArray(@default.UserDic, ',');
			this.OutputFormatType = @default.OutputFormatType;
		}

		public void LoadDicRC()
		{
			string path = Path.Combine(this.DicDir, this.RcFile);
			this.Load(path);
		}

		public void Load(string path)
		{
			IniParser iniParser = new IniParser();
			iniParser.Load(path, Encoding.ASCII);
			this.CostFactor = int.Parse(iniParser["cost-factor"] ?? "0");
			this.BosFeature = iniParser["bos-feature"];
		}

		private string[] SplitStringArray(string configStr, char separator)
		{
			if (string.IsNullOrEmpty(configStr))
			{
				return new string[0];
			}
			string[] array = configStr.Split(separator);
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = array[i].Trim();
			}
			return array;
		}
	}
}
