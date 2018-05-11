using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using NMeCab;

namespace NMeCabTest {
	class Program {
		static void Main(string[] args) {
			var dir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\"));

			var mPara = new MeCabParam();

			//辞書ファイルがあるフォルダを指定(NuGetで入れれば勝手に入る)
			mPara.DicDir = Path.Combine(dir, @"dic\mecab-ipadic-neologd");

			var mTagger = MeCabTagger.Create(mPara);
			string line = null;
			while ((line = Console.ReadLine()) != null) {
				var node = mTagger.ParseToNode(line);
				while (node != null) {
					if (node.CharType > 0) {
						Console.WriteLine("{0}\t{1}", node.Surface, node.Feature);
					}
					node = node.Next;
				}
			}
		}
	}
}
