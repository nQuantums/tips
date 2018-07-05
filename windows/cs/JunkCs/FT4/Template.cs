using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FT4 {
	/// <summary>
	/// 指定テンプレート文字列からコード生成をサポートするクラス
	/// </summary>
	public class Template {
		struct Replacement {
			public Regex Rx;
			public string Replace;

			public Replacement(Regex rx, string replace) {
				this.Rx = rx;
				this.Replace = replace;
			}
		}

		static readonly Regex Rx_i = new Regex(@"(?<i>\b_i\b)", RegexOptions.Compiled);
		static readonly Regex Rx_E = new Regex(@"(?<p>[^_]?)(?<e>_E)", RegexOptions.Compiled);
		static readonly Regex Rx_e = new Regex(@"(?<p>[^_]?)(?<e>_e)", RegexOptions.Compiled);
		static readonly Regex[] Rx_Ep;
		static readonly Regex[] Rx_ep;
		static readonly Regex[] Rx_Em;
		static readonly Regex[] Rx_em;

		/// <summary>
		/// テンプレート内の _E はこの文字列に置き換わる、現在列挙中の要素を示す
		/// </summary>
		public string E;

		/// <summary>
		/// テンプレート内の _e はこの文字列に置き換わる、現在列挙中の要素を示す
		/// </summary>
		public string e;

		/// <summary>
		/// テンプレート内の _Ep1n (nは1以上の数字)はこの文字列に置き換わる、現在列挙中の要素以降の要素を示す
		/// </summary>
		public string[] Ep;

		/// <summary>
		/// テンプレート内の _ep1n (nは1以上の数字)はこの文字列に置き換わる、現在列挙中の要素以降の要素を示す
		/// </summary>
		public string[] ep;

		/// <summary>
		/// テンプレート内の _Em1n (nは1以上の数字)はこの文字列に置き換わる、現在列挙中の要素以前の要素を示す
		/// </summary>
		public string[] Em;

		/// <summary>
		/// テンプレート内の _em1n (nは1以上の数字)はこの文字列に置き換わる、現在列挙中の要素以前の要素を示す
		/// </summary>
		public string[] em;

		static Template() {
			Rx_Ep = new Regex[3];
			Rx_ep = new Regex[3];
			Rx_Em = new Regex[3];
			Rx_em = new Regex[3];
			for (int i = 0; i < 3; i++) {
				var j = i + 1;
				Rx_Ep[i] = new Regex(@"(?<p>[^_]?)(?<e" + j + @">_Ep" + j + @")", RegexOptions.Compiled);
				Rx_ep[i] = new Regex(@"(?<p>[^_]?)(?<e" + j + @">_ep" + j + @")", RegexOptions.Compiled);
				Rx_Em[i] = new Regex(@"(?<p>[^_]?)(?<e" + j + @">_Em" + j + @")", RegexOptions.Compiled);
				Rx_em[i] = new Regex(@"(?<p>[^_]?)(?<e" + j + @">_em" + j + @")", RegexOptions.Compiled);
			}
		}

		/// <summary>
		/// 大文字用の置き換え文字列から小文字の方を作成する
		/// </summary>
		public void BuildLower() {
			if (this.E != null)
				this.e = this.E.ToLower();
			if (this.Ep != null) {
				this.ep = new string[this.Ep.Length];
				for (int i = 0; i < this.Ep.Length; i++)
					this.ep[i] = this.Ep[i].ToLower();
			}
			if (this.Em != null) {
				this.em = new string[this.Em.Length];
				for (int i = 0; i < this.Em.Length; i++)
					this.em[i] = this.Em[i].ToLower();
			}
		}

		/// <summary>
		/// 指定されたテンプレートからコードを生成する
		/// </summary>
		/// <param name="template">テンプレート文字列</param>
		/// <param name="i">現在列挙中の要素インデックス</param>
		/// <returns>コード</returns>
		public string Generate(string template, int i) {
			var reps = new List<Replacement>();
			if (this.Ep != null) {
				for (int j = 0, n = Math.Min(this.Ep.Length, Rx_Ep.Length); j < n; j++)
					reps.Add(new Replacement(Rx_Ep[j], this.Ep[j]));
			}
			if (this.ep != null) {
				for (int j = 0, n = Math.Min(this.ep.Length, Rx_ep.Length); j < n; j++)
					reps.Add(new Replacement(Rx_ep[j], this.ep[j]));
			}
			if (this.Em != null) {
				for (int j = 0, n = Math.Min(this.Em.Length, Rx_Em.Length); j < n; j++)
					reps.Add(new Replacement(Rx_Em[j], this.Em[j]));
			}
			if (this.em != null) {
				for (int j = 0, n = Math.Min(this.em.Length, Rx_em.Length); j < n; j++)
					reps.Add(new Replacement(Rx_em[j], this.em[j]));
			}
			reps.Add(new Replacement(Rx_i, i.ToString()));
			reps.Add(new Replacement(Rx_E, this.E));
			reps.Add(new Replacement(Rx_e, this.e));
			return Replace(reps, template);
		}

		static string Replace(Regex rx, string template, string replace) {
			if (replace == null)
				return template;
			return rx.Replace(
				template,
				(m) => {
					return m.Groups["p"].ToString() + replace;
				}
			);
		}

		static string Replace(IEnumerable<Replacement> reps, string template) {
			foreach (var rep in reps)
				template = Replace(rep.Rx, template, rep.Replace);
			return template;
		}
	}
}
