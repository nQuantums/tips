using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using NMeCab;

namespace NMeCabTest {
	class Program {
		static readonly Regex RegexCve = new Regex("cve-(|([0-9]+(|-(|([0-9]+)))))$", RegexOptions.Compiled);
		static readonly Regex RegexJvndb = new Regex("jvndb-(|([0-9]+(|-(|([0-9]+)))))$", RegexOptions.Compiled);
		static readonly Regex RegexCveComplete = new Regex("cve-[0-9]+-[0-9]+$", RegexOptions.Compiled);
		static readonly Regex RegexJvndbComplete = new Regex("jvndb-[0-9]+-[0-9]+$", RegexOptions.Compiled);

		static Func<string, bool> SpecialKeyword(string keyword, Action<string> addTodic) {
			switch (keyword) {
			case "cve": {
					var list = new List<string>();
					list.Add(keyword);
					return new Func<string, bool>(k => {
						var n = list.Count;
						if (k.Length != 0) {
							list.Add(k);
						}
						if (k.Length == 0 || !RegexCve.IsMatch(string.Concat(list))) {
							var c = string.Concat(list.Take(n));
							if (RegexCveComplete.IsMatch(c)) {
								addTodic(c);
							} else {
								list.ForEach(i => addTodic(i));
							}
							return false;
						} else {
							return true;
						}
					});
				}
			case "jvndb": {
					var list = new List<string>();
					list.Add(keyword);
					return new Func<string, bool>(k => {
						var n = list.Count;
						if (k.Length != 0) {
							list.Add(k);
						}
						if (k.Length == 0 || !RegexJvndb.IsMatch(string.Concat(list))) {
							var c = string.Concat(list.Take(n));
							if (RegexJvndbComplete.IsMatch(c)) {
								addTodic(c);
							} else {
								list.ForEach(i => addTodic(i));
							}
							return false;
						} else {
							return true;
						}
					});
				}
			default:
				return null;
			}
		}

		static void SpecialKeywordProc(List<Func<string, bool>> receivers, string keyword) {
			for (int i = receivers.Count - 1; i != -1; --i) {
				if (!receivers[i](keyword)) {
					receivers.RemoveAt(i);
				}
			}
		}

		static void Main(string[] args) {
			var mPara = new MeCabParam();

			//辞書ファイルがあるフォルダを指定(NuGetで入れれば勝手に入る)
			mPara.DicDir = @"c:\dic\mecab-ipadic-neologd";

			var mTagger = MeCabTagger.Create(mPara);
			string line = null;
			var receivers = new List<Func<string, bool>>();
			while ((line = Console.ReadLine()) != null) {
				var node = mTagger.ParseToNode(line);
				while (node != null) {
					if (node.CharType > 0) {
						var l = node.Surface.ToLower();
						SpecialKeywordProc(receivers, l);

						var r = SpecialKeyword(l, k => Console.WriteLine(k));
						if (r is null) {
							Console.WriteLine("{0}\t{1}", node.Surface, node.Feature);
						} else {
							receivers.Add(r);
						}
					}
					node = node.Next;
				}
			}
		}
	}
}
