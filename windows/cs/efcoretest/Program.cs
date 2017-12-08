using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Linq;

namespace efcoretest
{
    class Program
    {
        static void Main(string[] args)
        {
			using(var dc = new wsus_exportedContext()) {
				// ロガープロバイダーを設定する
				var serviceProvider = dc.GetInfrastructure();
				var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
				loggerFactory.AddProvider(new AppLoggerProvider());

				// 適当にクエリしてみる
				var targetUuid = new Guid("{771DC7C7-F33A-4012-94EC-A1802563F52D}");
				var amd64 = new Guid("{59653007-e2e9-4f71-8525-2ff588527978}");
				var windows7 = new Guid("{bfe5b177-a086-47a0-b102-097e4fa1f807}");
				var table = from r in dc.TblUpdates where r.Categories.Contains(windows7) select r;

				var nsmgr = new XmlNamespaceManager(new NameTable());
				nsmgr.AddNamespace("pub", "http://schemas.microsoft.com/msus/2002/12/Publishing");
				nsmgr.AddNamespace("bar", "http://schemas.microsoft.com/msus/2002/12/BaseApplicabilityRules");
				nsmgr.AddNamespace("cat", "http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/Category");
				nsmgr.AddNamespace("lar", "http://schemas.microsoft.com/msus/2002/12/LogicalApplicabilityRules");
				nsmgr.AddNamespace("upd", "http://schemas.microsoft.com/msus/2002/12/Update");

				foreach (var row in table) {
					Console.WriteLine(row.UpdateId);
					Console.WriteLine(row.Title);
					var metaData = XDocument.Parse(row.Xml);

					var dependings = metaData.XPathSelectElements("/upd:Update/upd:Relationships/upd:Prerequisites", nsmgr);
					foreach(var d in dependings) {
						var updateIds = d.XPathSelectElements("//upd:UpdateIdentity", nsmgr);
						foreach(var uid in  updateIds) {
							foreach(var s in uid.Attributes("UpdateID")) {
								Console.WriteLine(s);
							}
						}
					}
					Console.WriteLine(row.Xml);
				}

				//foreach (var row in dc.TblUpdates.FromSql("select * from tbl_updates limit 10;")) {
				//	Console.WriteLine(row.UpdateId);
				//}
				//foreach (var row in (from r in dc.TblUpdates select r).Take(10)) {
				//	Console.WriteLine(row.UpdateId);
				//}
			}
        }
    }

	// ロガープロバイダー
	public class AppLoggerProvider : ILoggerProvider {
		// ロガーを生成
		public ILogger CreateLogger(string categoryName) {
			return new ConsoleLogger();
		}

		public void Dispose() {
		}

		// ロガー
		private class ConsoleLogger : ILogger {
			public IDisposable BeginScope<TState>(TState state) => null;
			public bool IsEnabled(LogLevel logLevel) => true;

			// ログを出力
			public void Log<TState>(
				LogLevel logLevel, EventId eventId,
				TState state, Exception exception,
				Func<TState, Exception, string> formatter) {
				// コンソールに出力
				Console.WriteLine(formatter(state, exception));
			}
		}
	}
}
