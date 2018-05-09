using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
	public partial class BrowserWindow : Window, ILifeSpanHandler, ILoadHandler {
		const string BinderName = "HostObj";
		static string _EmbeddingJs;

		public ChromiumWebBrowser BrowserControl { get; private set; }
		public bool LoadEnded { get; private set; }
		public Bridge Bridge { get; private set; }

		public string Address {
			get {
				return this.tbUrl.Text;
			}
			set {
				this.BrowserControl.Address = this.tbUrl.Text = value;
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
			this.BrowserControl = browserControl;
			Grid.SetRow(this.BrowserControl, 1);

			// ロケールを日本語とする
			this.BrowserControl.BrowserSettings.AcceptLanguageList = "ja-JP";

			// 埋め込みJavaScriptとの仲介オブジェクト登録
			this.BrowserControl.JavascriptObjectRepository.Register(BinderName, this.Bridge = new Bridge());

			// イベントハンドラ設定
			this.BrowserControl.LifeSpanHandler = this;
			this.BrowserControl.LoadHandler = this;

			InitializeComponent();

			this.gridRoot.Children.Add(this.BrowserControl);

			this.Loaded += BrowserWindow_Loaded;
		}

		private void BrowserWindow_Loaded(object sender, RoutedEventArgs e) {
			//this.BrowserControl.ShowDevTools();
		}

		public BrowserWindow() : this(new ChromiumWebBrowser()) {
		}

		public bool OnBeforePopup(IWebBrowser browserControl, IBrowser browser, IFrame frame, string targetUrl, string targetFrameName, WindowOpenDisposition targetDisposition, bool userGesture, IPopupFeatures popupFeatures, IWindowInfo windowInfo, IBrowserSettings browserSettings, ref bool noJavascriptAccess, out IWebBrowser newBrowser) {
			var chromiumWebBrowser = (ChromiumWebBrowser)browserControl;

			ChromiumWebBrowser chromiumBrowser = null;

			var windowX = windowInfo.X;
			var windowY = windowInfo.Y;
			var windowWidth = (windowInfo.Width == int.MinValue) ? 600 : windowInfo.Width;
			var windowHeight = (windowInfo.Height == int.MinValue) ? 800 : windowInfo.Height;

			chromiumWebBrowser.Dispatcher.Invoke(new Action(() => {
				chromiumBrowser = new ChromiumWebBrowser();
				chromiumBrowser.SetAsPopup();

				var popup = new BrowserWindow(chromiumBrowser) {
					Address = targetUrl,
					Left = windowX,
					Top = windowY,
					Width = windowWidth,
					Height = windowHeight,
				};

				popup.Owner = this;
				popup.Show();
			}));

			newBrowser = chromiumBrowser;

			return false;
		}

		public void OnAfterCreated(IWebBrowser browserControl, IBrowser browser) {
		}

		public bool DoClose(IWebBrowser browserControl, IBrowser browser) {
			return false;
		}

		public void OnBeforeClose(IWebBrowser browserControl, IBrowser browser) {
		}

		public void OnLoadingStateChange(IWebBrowser browserControl, LoadingStateChangedEventArgs loadingStateChangedArgs) {
			if (this.LoadEnded && !loadingStateChangedArgs.IsLoading) {
				// ページ読み込みが完了したらJavaScript側から処理を呼び出す
				this.BrowserControl.ExecuteScriptAsync(_EmbeddingJs);
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
	}
}
