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
				Action startProcess = () => {
					p = new Process();
					p.StartInfo.CreateNoWindow = false;
					p.StartInfo.UseShellExecute = false;
					p.StartInfo.RedirectStandardInput = true;
					p.StartInfo.FileName = "ColoredLogViewer.exe";
					p.StartInfo.Arguments = fileName;
					p.Start();
				};
				Action<FileDeltaReader> handler = r => {
					var str = r.ReadDeltaString();
					if (p != null && p.HasExited) {
						p.Dispose();
						p = null;
					}
					if (p == null) {
						startProcess();
					}
					foreach (var line in str.Split('\n')) {
						var text = line.Replace("\r", "");
						p.StandardInput.WriteLine(text);
					}
				};
				handlers.Add(fileName, handler);
			}

			var fcw = new TailReader(args[0], "*.log", handlers);

			Console.ReadKey();
		}
	}
}
