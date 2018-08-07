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
		/// <summary>
		/// サーバーアドレス
		/// </summary>
		public const string LocalHttpServerUrl = "http://localhost:9999/";

		public ChromeDriver Chrome { get; private set; }
		CancellationTokenSource _HttpServerCts;
		Task _HttpServerTask;

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
		public void Navigate(string url) {
			lock (this.Chrome) {
				// Chrome側を指定URLへナビゲート
				this.Chrome.Navigate().GoToUrl(url);

				// Chrome側でページの表示が完了するまで待機
				var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(this.Chrome, TimeSpan.FromSeconds(30.00));
				wait.Until(driver => ((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState").Equals("complete"));
			}
		}

		/// <summary>
		/// Chrome側に指定のJavaScriptを仕込んで実行し結果を文字列で取得する
		/// </summary>
		/// <param name="script">JavaScriptソースコード</param>
		/// <param name="args">引数列</param>
		/// <returns>実行結果の文字列</returns>
		public string ExecuteScript(string script, params object[] args) {
			return this.Chrome.ExecuteScript(script, args).ToString();
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
					//switch (req.QueryString["event"]) {
					//case "PageAfterInit":
					//	OnPageAfterInit(Json.ToObject<PageAfterInitArgs>(req.InputStream));
					//	break;
					//}

					// レスポンス内容設定
					res.ContentEncoding = Encoding.UTF8;
					res.ContentType = "text/plain";

					var sw = new StreamWriter(res.OutputStream, Encoding.UTF8);
					sw.WriteLine(resp);
					sw.Flush();
				}
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
