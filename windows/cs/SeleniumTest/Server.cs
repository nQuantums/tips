using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;

namespace SeleniumTest {
	/// <summary>
	/// Chrome側に仕込んだJavaScriptからの要求を処理するHTTPサーバー
	/// </summary>
	public static class Server {
		static string SetEventsJs;

		public const string LocalHttpServerUrl = "http://localhost:9999/";

		static List<Stub> Stubs = new List<Stub>();

		public class Stub {
			public int Id { get; private set; }
			public Action Action { get; private set; }

			public Stub(int id, Action action) {
				this.Id = id;
				this.Action = action;
			}
		}

		[DataContract]
		public class PageAfterInitArgs {
			[DataMember]
			public string title;
			[DataMember]
			public string url;
			[DataMember]
			public int id;
		}

		static Server() {
			var asm = Assembly.GetExecutingAssembly();
			using (var sr = new StreamReader(asm.GetManifestResourceStream("PatchCrawler.SetEvents.js"), Encoding.UTF8)) {
				SetEventsJs = sr.ReadToEnd();
			}
		}

		/// <summary>
		/// ブラウザのページ内イベント取得とタブ間で同じ値を保持するためにHTTPサーバーを開始する
		/// </summary>
		static async Task StartHttpServer(CancellationToken ct) {
			var listener = new HttpListener();
			listener.Prefixes.Add(LocalHttpServerUrl);
			listener.Start();
			ct.Register(() => listener.Stop());

			while (!ct.IsCancellationRequested) {
				var context = await listener.GetContextAsync();
				var req = context.Request;
				using (var res = context.Response) {
					// ブラウザ内JavaScriptからの接続を許可するのに以下のヘッダが必要
					res.Headers.Add("Access-Control-Allow-Origin", "*");
					res.Headers.Add("Access-Control-Allow-Headers", "Origin, X-Requested-With, Content-Type, Accept");
					res.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");

					var responseBuffer = new StringBuilder();

					var ev = req.QueryString["event"];
					Console.WriteLine(ev);
					switch (ev) {
					case "page_after_init": {
							var args = Json.ToObject<PageAfterInitArgs>(req.InputStream);
							var id = args.id;
							lock (Stubs) {
								var index = Stubs.FindIndex(s => s.Id == id);
								if (0 <= index) {
									var stub = Stubs[index];
									Task.Run(() => stub.Action);
								}
							}
						}
						break;
					}

					// レスポンス内容設定
					res.ContentEncoding = Encoding.UTF8;
					res.ContentType = "text/plain";

					var sw = new StreamWriter(res.OutputStream, Encoding.UTF8);
					sw.WriteLine(responseBuffer.ToString());
					sw.Flush();
				}
			}
		}

		/// <summary>
		/// HTTPサーバーの停止
		/// </summary>
		static void StopHttpServer(Task t, CancellationTokenSource cts) {
			cts.Cancel();
			try {
				t.Wait();
			} catch { }
		}
	}
}
