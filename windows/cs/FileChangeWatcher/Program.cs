using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace FileChangeWatcher {
	class Program {
		static void Main(string[] args) {
			var handlers = new Dictionary<string, Action<FileDeltaReader>>();
			for (int i = 1; i < args.Length; i++) {
				var fileName = args[i];
				Process p = null;
				Action<FileDeltaReader> handler = r => {
					if (p == null) {
						p = new Process();
						p.StartInfo.CreateNoWindow = true;
						p.StartInfo.UseShellExecute = true;
						p.StartInfo.RedirectStandardInput = true;
						p.StartInfo.RedirectStandardOutput = false;
						p.StartInfo.FileName = "ColoredLogViewer.exe";
						var result = p.Start();
					}

					var text = r.ReadDeltaString();
					foreach (var line in text.Split('\n')) {
						p.StandardInput.WriteLine(line.Replace("\r", ""));
					}
				};
				handlers.Add(fileName, handler);
			}

			var fcw = new TailReader(args[0], "*.log", handlers);

			Console.ReadKey();
		}
	}
}
