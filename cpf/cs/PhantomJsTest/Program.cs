using System;
using System.IO;
using System.Drawing;
using OpenQA.Selenium;
using OpenQA.Selenium.PhantomJS;
using System.Text;
using System.Threading.Tasks;

namespace PhantomJsTest {
	class Program {
		static void Main(string[] args) {
			//using (var driver = new PhantomJSDriver()) {
			//	const string phantomScript = "var page=this;page.onResourceRequested = function(requestData, request) { var reg =  /\\.png/gi; var isPng = reg.test(requestData['url']); console.log(isPng,requestData['url']); if (isPng){console.log('Aborting: ' + requestData['url']);request.abort();}}";
			//	var script = driver.ExecutePhantomJS(phantomScript);
			//	driver.Navigate().GoToUrl("https://www.catalog.update.microsoft.com/Search.aspx?q=KB4056893/");
			//	driver.GetScreenshot().SaveAsFile("updatecatalog.png", ScreenshotImageFormat.Png);
			//}

			var dir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../../../"));
			ProcessScript(Path.Combine(dir, "test.js"));
		}

		static void ProcessScript(string script, params string[] args) {
			using (var process = new System.Diagnostics.Process()) {
				bool exit = false;

				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.RedirectStandardInput = true;
				process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
				process.StartInfo.FileName = "phantomjs.exe";
				process.StartInfo.Arguments = $"{script} {string.Join(" ", args)}";
				process.EnableRaisingEvents = true;
				process.Exited += (o, s) => {
					exit = true;
				};

				process.Start();

				var reader = Task.Run(() => {
					string line;
					while (!exit && (line = process.StandardOutput.ReadLine()) != null) {
						Console.WriteLine(line);
					}
				});
				{
					string line;
					while (!exit && (line = Console.ReadLine()) != null) {
						process.StandardInput.WriteLine(line);
					}
				}

				reader.Wait();

				//var result = process.StandardOutput.ReadToEnd();
				//process.WaitForExit();

				Console.WriteLine($"ProcessScript() -> Code {process.ExitCode}: {process.ExitTime - process.StartTime} has elapsed.");
			}
		}
	}
}
