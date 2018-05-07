using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PatchStalker {
	/// <summary>
	/// テキストから意味がわかる要素を抜き出す処理など
	/// </summary>
	public class Matching {
		public class Ent {
			public string Pattern { get; private set; }
			public string Group { get; private set; }

			public Ent(string pattern, string group) {
				this.Pattern = pattern;
				this.Group = group;
			}
		}

		static readonly string[] OsNames = new[] {
			"Windows",
			"Windows *2000",
			"Windows *XP",
			"Windows *Vista",
			"Windows *7",
			"Windows *8",
			"Windows *8.1",
			"Windows *10",
			"Windows *Server *2003",
			"Windows *Server *2008",
			"Windows *Server *2008 *R2",
			"Windows *Server *2012",
			"Windows *Server *2012 *R2",
			"Windows *Server *2016",
		};

		static readonly string[] ProductNames = new[] {
			"Acrobat *Reader",
			"Adobe *Reader",
			"Adobe *Acrobat *Reader",
		};

		static Regex _EntityMatcher;
		static string[] _GroupNames;

		static Matching() {
			var entities = new List<Ent>();
			entities.AddRange(from os in OsNames select new Ent(os, "os"));
			entities.AddRange(from product in ProductNames select new Ent(product, "p"));
			entities.Sort((l, r) => r.Pattern.Length - l.Pattern.Length);

			var pattern = string.Join("|", from e in entities select $"(?<{e.Group}>{e.Pattern})");
			_EntityMatcher = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
			_GroupNames = (from name in _EntityMatcher.GetGroupNames() where name != "0" select name).ToArray();
		}

		public static bool IsMatched(string keyword) {
			foreach (Match m in _EntityMatcher.Matches(keyword)) {
				var groups = m.Groups;
				var matchedGroup = (from gn in _GroupNames let g = groups[gn] where g.Success select g).FirstOrDefault();
				if (matchedGroup != null) {
					return true;
				}
			}
			return false;
		}
	}
}
