using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CefSharp;
using CefSharp.Wpf;

namespace PatchStalker {
	/// <summary>
	/// BrowserWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class BrowserWindow : Window, ILifeSpanHandler, ILoadHandler, IRequestHandler {
		const string BinderName = "HostObj";
		static string _EmbeddingJs;

		List<Window> _Popups = new List<Window>();

		public ChromiumWebBrowser BrowserControl { get; private set; }
		public bool LoadEnded { get; private set; }
		public Bridge Bridge { get; private set; }

		public string Address {
			get {
				return this.tbUrl.Text;
			}
			set {
				this.tbUrl.Text = value;
				if (this.BrowserControl != null) {
					this.BrowserControl.Address = value;
				}
			}
		}

		static BrowserWindow() {
			var asm = Assembly.GetExecutingAssembly();
			var sb = new StringBuilder();
			using (var jquery = new StreamReader(asm.GetManifestResourceStream("PatchStalker.jquery-min.js"), Encoding.UTF8))
			using (var embedding = new StreamReader(asm.GetManifestResourceStream("PatchStalker.embedding.js"), Encoding.UTF8)) {
				sb.AppendLine(jquery.ReadToEnd());
				sb.AppendLine(embedding.ReadToEnd());
			}
			_EmbeddingJs = sb.ToString();
		}

		public BrowserWindow(ChromiumWebBrowser browserControl) {
			InitializeComponent();

			this.Bridge = new Bridge();
			this.Bridge.CurrentDistance = 1;
			this.Bridge.AddLinkEnd += () => {
				// ページ内の全リンク取得後の処理
				this.Dispatcher.BeginInvoke(new Action(() => {
					lock (this.Bridge) {
						//var links = _HostObj.Links;
						//while (links.Count != 0) {
						//	// 優先度が最高のものを取り出す
						//	var index = _HostObj.Links.Count - 1;
						//	var link = _HostObj.Links[index];
						//	links.RemoveAt(index);

						//	// スタートページから遠いものは破棄する
						//	if (4 <= link.Distance) {
						//		continue;
						//	}

						//	// 表示先ページの距離を計算して表示する
						//	_HostObj.CurrentDistance = link.Distance + 1;
						//	_LoadEnded = false;
						//	Navigate(_Browser.Address = link.Address);
						//	System.Diagnostics.Debug.WriteLine(_Browser.Address);
						//	break;
						//}
					}
				}));
			};

			// ブラウザコントロールの初期化
			if (browserControl != null) {
				this.BrowserControl = browserControl;
				Grid.SetRow(browserControl, 1);
				this.tbUrl.Text = browserControl.Address;

				// ロケールを日本語とする
				browserControl.BrowserSettings.AcceptLanguageList = "ja-JP";

				// 埋め込みJavaScriptとの仲介オブジェクト登録
				browserControl.JavascriptObjectRepository.Register(BinderName, this.Bridge = new Bridge());

				// イベントハンドラ設定
				browserControl.LifeSpanHandler = this;
				browserControl.LoadHandler = this;
				browserControl.RequestHandler = this;

				this.gridRoot.Children.Add(browserControl);
			}

			this.Loaded += BrowserWindow_Loaded;
			this.Closing += BrowserWindow_Closing;
		}

		//public BrowserWindow(string url) : this(null as ChromiumWebBrowser) {
		//}
		public BrowserWindow(string url) : this(new ChromiumWebBrowser() { Address = url }) {
		}

		public BrowserWindow() : this(new ChromiumWebBrowser()) {
		}

		private void BrowserWindow_Loaded(object sender, RoutedEventArgs e) {
		}

		private void BrowserWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
			if (this.BrowserControl != null) {
				this.gridRoot.Children.Remove(this.BrowserControl);
				this.BrowserControl.Dispose();
				this.BrowserControl = null;
			}
			foreach (var p in _Popups) {
				p.Close();
			}
		}

		public bool OnBeforePopup(IWebBrowser browserControl, IBrowser browser, IFrame frame, string targetUrl, string targetFrameName, WindowOpenDisposition targetDisposition, bool userGesture, IPopupFeatures popupFeatures, IWindowInfo windowInfo, IBrowserSettings browserSettings, ref bool noJavascriptAccess, out IWebBrowser newBrowser) {
			var chromiumWebBrowser = (ChromiumWebBrowser)browserControl;

			ChromiumWebBrowser chromiumBrowser = null;

			//var windowX = windowInfo.X;
			//var windowY = windowInfo.Y;
			//var windowWidth = (windowInfo.Width == int.MinValue) ? 600 : windowInfo.Width;
			//var windowHeight = (windowInfo.Height == int.MinValue) ? 800 : windowInfo.Height;

			//chromiumWebBrowser.Dispatcher.Invoke(new Action(() => {
			//	chromiumBrowser = new ChromiumWebBrowser();
			//	//chromiumBrowser.SetAsPopup();

			//	var popup = new BrowserWindow(chromiumBrowser) {
			//		Address = targetUrl,
			//		//Left = windowX,
			//		//Top = windowY,
			//		//Width = windowWidth,
			//		//Height = windowHeight,
			//	};

			//	popup.Owner = this;
			//	popup.Show();

			//	_Popups.Add(popup);
			//}));

			frame.ExecuteJavaScriptAsync("console.log(window.location.href);");
			newBrowser = chromiumBrowser;

			return false;
		}

		public void OnAfterCreated(IWebBrowser browserControl, IBrowser browser) {
		}

		public bool DoClose(IWebBrowser browserControl, IBrowser browser) {
			return false;
		}

		public void OnBeforeClose(IWebBrowser browserControl, IBrowser browser) {
			//this.Dispatcher.Invoke(new Action(() => {
			//	this.gridRoot.Children.Remove(this.BrowserControl);
			//	this.BrowserControl.Dispose();
			//	this.BrowserControl = null;
			//}));
		}

		public void OnLoadingStateChange(IWebBrowser browserControl, LoadingStateChangedEventArgs loadingStateChangedArgs) {
			if (this.LoadEnded && !loadingStateChangedArgs.IsLoading) {
				browserControl.ShowDevTools();

				// ページ読み込みが完了したらJavaScript側から処理を呼び出す
				//var browser = browserControl.GetBrowser();
				//foreach (var fid in browser.GetFrameIdentifiers()) {
				//	var frame = browser.GetFrame(fid);
				//	if (frame != null) {
				//		frame.ExecuteJavaScriptAsync("console.log(window.location.href);");
				//	}
				//}
				//browserControl.ExecuteScriptAsync(_EmbeddingJs);
			}
		}

		public void OnFrameLoadStart(IWebBrowser browserControl, FrameLoadStartEventArgs frameLoadStartArgs) {
			this.LoadEnded = false;
		}

		public void OnFrameLoadEnd(IWebBrowser browserControl, FrameLoadEndEventArgs frameLoadEndArgs) {
			this.LoadEnded = true;
		}

		public void OnLoadError(IWebBrowser browserControl, LoadErrorEventArgs loadErrorArgs) {
		}

		private void btnShowDevTool_Click(object sender, RoutedEventArgs e) {
			this.BrowserControl.ShowDevTools();
		}

		public bool OnBeforeBrowse(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, bool isRedirect) {
			//browserControl.LifeSpanHandler = this;
			browserControl.LoadHandler = this;
			//browser.ExecuteScriptAsync("console.log('OnBeforeBrowse');");
			return false;
		}

		public bool OnOpenUrlFromTab(IWebBrowser browserControl, IBrowser browser, IFrame frame, string targetUrl, WindowOpenDisposition targetDisposition, bool userGesture) {
			return false;
		}

		public bool OnCertificateError(IWebBrowser browserControl, IBrowser browser, CefErrorCode errorCode, string requestUrl, ISslInfo sslInfo, IRequestCallback callback) {
			return false;
		}

		public void OnPluginCrashed(IWebBrowser browserControl, IBrowser browser, string pluginPath) {
		}

		public CefReturnValue OnBeforeResourceLoad(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IRequestCallback callback) {
			return CefReturnValue.Continue;
		}

		public bool GetAuthCredentials(IWebBrowser browserControl, IBrowser browser, IFrame frame, bool isProxy, string host, int port, string realm, string scheme, IAuthCallback callback) {
			return false;
		}

		public bool OnSelectClientCertificate(IWebBrowser browserControl, IBrowser browser, bool isProxy, string host, int port, X509Certificate2Collection certificates, ISelectClientCertificateCallback callback) {
			return false;
		}

		public void OnRenderProcessTerminated(IWebBrowser browserControl, IBrowser browser, CefTerminationStatus status) {
		}

		public bool OnQuotaRequest(IWebBrowser browserControl, IBrowser browser, string originUrl, long newSize, IRequestCallback callback) {
			return false;
		}

		public void OnResourceRedirect(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response, ref string newUrl) {
		}

		public bool OnProtocolExecution(IWebBrowser browserControl, IBrowser browser, string url) {
			return false;
		}

		public void OnRenderViewReady(IWebBrowser browserControl, IBrowser browser) {
		}

		public bool OnResourceResponse(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response) {
			return false;
		}

		public IResponseFilter GetResourceResponseFilter(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response) {
			return null;
		}

		public void OnResourceLoadComplete(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response, UrlRequestStatus status, long receivedContentLength) {
		}
	}
}
