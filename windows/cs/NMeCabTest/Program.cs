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

			var sentence = @"電源がオンになっている仮想マシンのみの一覧を返す場合、Get-VM コマンドにフィルターを追加します。 フィルターは Where-Object コマンドを使用して追加できます。 フィルター処理の詳細については、Where-Object の使用に関するドキュメントをご覧ください。";//解析する文字列
			var node = mTagger.ParseToNode(sentence);
			while (node != null) {
				if (node.CharType > 0) {
					Console.WriteLine("{0}\t{1}", node.Surface, node.Feature);
				}
				node = node.Next;
			}
		}
	}
}
