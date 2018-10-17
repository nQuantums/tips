using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ColoredLogViewer {
	class Program {
		public class Pattern {
			public string GroupName;
			public string Format;
			public ConsoleColor Color;

			public Pattern(string groupName, string format, ConsoleColor color) {
				this.GroupName = groupName;
				this.Format = format;
				this.Color = color;
			}
		}

		static void Main(string[] args) {
			Pattern[] patterns;
			Regex rx;

			{
				var ptns = new List<Pattern>();
				ptns.Add(new Pattern("dt", @"[0-9]{4}/[0-9]{2}/[0-9]{2} +[0-9]{2}:[0-9]{2}:[0-9]{2}(\.[0-9]+)", ConsoleColor.Cyan));
				ptns.Add(new Pattern("err", @"error|exception|failed|fail|エラー|失敗", ConsoleColor.Red));
				ptns.Add(new Pattern("num", @"[0-9]+", ConsoleColor.Green));
				var sb = new StringBuilder();
				foreach (var p in ptns) {
					if (sb.Length != 0) {
						sb.Append("|");
					}
					sb.AppendFormat("(?<{0}>{1})", p.GroupName, p.Format);
				}
				patterns = ptns.ToArray();
				rx = new Regex(sb.ToString(), RegexOptions.IgnoreCase | RegexOptions.Compiled);
			}

			for (; ; ) {
				var line = Console.ReadLine();
				if (line.Length == 0) {
					continue;
				}

				var colors = new ConsoleColor[line.Length];
				for (int i = 0; i < colors.Length; i++) {
					colors[i] = ConsoleColor.White;
				}

				rx.Replace(line, match => {
					var groups = match.Groups;
					for (int i = 0; i < patterns.Length; i++) {
						var p = patterns[i];
						var g = groups[p.GroupName];
						if (g.Success) {
							var s = g.Index;
							var c = p.Color;
							for (int j = 0, m = g.Length; j < m; j++) {
								colors[s + j] = c;
							}
						}
					}
					return "";
				});

				int start = 0;
				int lastColor = (int)colors[0];
				Console.ForegroundColor = (ConsoleColor)lastColor;
				for (int i = 0; i < colors.Length; i++) {
					var c = (int)colors[i];
					if (c != lastColor) {
						Console.ForegroundColor = (ConsoleColor)lastColor;
						Console.Write(line.Substring(start, i - start));
						start = i;
					}
					lastColor = c;
				}
				Console.ForegroundColor = (ConsoleColor)lastColor;
				Console.WriteLine(line.Substring(start, line.Length - start));
			}
		}
	}
}
