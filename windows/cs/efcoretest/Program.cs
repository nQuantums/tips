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

namespace efcoretest {
	class Program {
		static void Main(string[] args) {
			//Add();
			Query();
		}

		static void Add() {
			using (var dc = new eftestContext()) {
				// ロガープロバイダーを設定する
				var serviceProvider = dc.GetInfrastructure();
				var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
				loggerFactory.AddProvider(new AppLoggerProvider());

				// レコード適当に追加
				dc.TblContents.Add(new TblContents { ContentsId = 3, Contents = "agagaga agaga", Tags = new string[] { "aga" } });
				dc.SaveChanges();
			}
		}

		static void Query() {
			using (var dc = new eftestContext()) {
				// ロガープロバイダーを設定する
				var serviceProvider = dc.GetInfrastructure();
				var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
				loggerFactory.AddProvider(new AppLoggerProvider());

				// 適当にクエリしてみる
				var tags = new[] { "abc", "afe" };
				var table = from r in dc.TblContents where r.Tags.Contains("abc") && r.Tags.Contains("afe") select r;
				//var table = from r in dc.TblContents where r.Tags.All(i => tags.Contains(i)) select r;
				foreach (var record in table) {
					Console.WriteLine(record.ContentsId);
				}
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
