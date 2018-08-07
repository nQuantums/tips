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
using System.Runtime.Serialization.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Support;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.Events;

namespace SeleniumTest {
	/// <summary>
	/// Chromeを制御するためのサーバー
	/// </summary>
	public class Server : IDisposable {
		class Stub {
			public int Id { get; private set; }
			public Action Action { get; private set; }

			public Stub(int id, Action action) {
				this.Id = id;
				this.Action = action;
			}
		}

		[DataContract]
		class PageAfterInitArgs {
			[DataMember]
			public string url;
			[DataMember]
			public string title;
			[DataMember]
			public int id;
		}

		/// <summary>
		/// サーバーアドレス
		/// </summary>
		public const string LocalHttpServerUrl = "http://localhost:9999/";

		public ChromeDriver Chrome { get; private set; }
		Dictionary<int, Stub> _Stubs = new Dictionary<int, Stub>();
		int _StubId = 0;
		static string _SetEventsJs;
		CancellationTokenSource _HttpServerCts;
		Task _HttpServerTask;


		static Server() {
			var asm = Assembly.GetExecutingAssembly();

			using (var sr = new StreamReader(asm.GetManifestResourceStream("SeleniumTest.SetEvents.js"), Encoding.UTF8)) {
				_SetEventsJs = sr.ReadToEnd();
			}
		}

		public Server() {
		}

		/// <summary>
		/// Chromeの制御を開始する
		/// </summary>
		public void Start() {
			Stop();

			// Chromeドライバ作成
			var chromeOptions = new ChromeOptions();
			chromeOptions.Proxy = null;
			this.Chrome = new ChromeDriver(Environment.CurrentDirectory, chromeOptions);

			// Chromeに仕込んだJavaScriptからの要求受け付けるHTTPサーバー開始
			_HttpServerCts = new CancellationTokenSource();
			_HttpServerTask = RunHttpServer();
		}

		/// <summary>
		/// Chromeの制御を停止する
		/// </summary>
		public void Stop() {
			// HTTPサーバーを止める
			if (_HttpServerCts != null) {
				_HttpServerCts.Cancel();
				_HttpServerCts = null;
			}
			if (_HttpServerTask != null) {
				try {
					_HttpServerTask.Wait();
				} catch { }
				_HttpServerTask = null;
			}

			// Chromeを閉じる
			if (this.Chrome != null) {
				this.Chrome.Quit();
				this.Chrome.Dispose();
				this.Chrome = null;
			}
		}

		/// <summary>
		/// Chromeの現在のタブを指定のURLを表示するようナビゲートする
		/// </summary>
		/// <param name="url">移動先URL</param>
		/// <param name="afterPageInitialized">ページ初期化後に呼び出されるハンドラ</param>
		public void Navigate(string url, Action afterPageInitialized) {
			// 移動先ページ初期化後に呼び出される処理登録
			lock (this._Stubs) {
				_StubId++;
				this._Stubs.Add(_StubId, new Stub(_StubId, afterPageInitialized));
			}

			// ページ移動してそのページにJavaScriptを仕込む
			lock (this.Chrome) {
				// Chrome側を指定URLへナビゲート
				this.Chrome.Navigate().GoToUrl(url);

				// Chrome側が準備できるまで待機
				this.Chrome.ExecuteScript("return document.readyState;");

				// Chromeで現在表示中のページにJavaScriptを埋め込む
				this.Chrome.ExecuteScript(_SetEventsJs, this.Chrome.CurrentWindowHandle, LocalHttpServerUrl, _StubId);
			}
		}


		/// <summary>
		/// ブラウザのページ内イベント取得とタブ間で同じ値を保持するためにHTTPサーバーを開始する
		/// </summary>
		async Task RunHttpServer() {
			var ct = _HttpServerCts.Token;
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

					// リクエストをハンドリング
					var resp = "";
					switch (req.QueryString["event"]) {
					case "PageAfterInit":
						OnPageAfterInit(Json.ToObject<PageAfterInitArgs>(req.InputStream));
						break;
					}

					// レスポンス内容設定
					res.ContentEncoding = Encoding.UTF8;
					res.ContentType = "text/plain";

					var sw = new StreamWriter(res.OutputStream, Encoding.UTF8);
					sw.WriteLine(resp);
					sw.Flush();
				}
			}
		}

		void OnPageAfterInit(PageAfterInitArgs args) {
			var id = args.id;
			Stub stub;
			lock (_Stubs) {
				if (!_Stubs.TryGetValue(id, out stub)) {
					return;
				}
				_Stubs.Remove(id);
			}
			if (stub.Action != null) {
				stub.Action();
			}
		}

		#region IDisposable Support
		private bool disposedValue = false; // 重複する呼び出しを検出するには

		protected virtual void Dispose(bool disposing) {
			if (!disposedValue) {
				Stop();
				disposedValue = true;
			}
		}

		~Server() {
			// このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
			Dispose(false);
		}

		// このコードは、破棄可能なパターンを正しく実装できるように追加されました。
		public void Dispose() {
			// このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}
