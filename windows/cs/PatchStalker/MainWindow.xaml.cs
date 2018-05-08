using CefSharp;
using CefSharp.Wpf;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PatchStalker {
	/// <summary>
	/// MainWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class MainWindow : Window {
		const string BinderName = "HostObj";

		/// <summary>
		/// リンク情報
		/// </summary>
		public class Link {
			/// <summary>
			/// リンクのID
			/// </summary>
			public int Id { get; private set; }

			/// <summary>
			/// リンク先アドレス
			/// </summary>
			public string Address { get; set; }

			/// <summary>
			/// リンク時のキーワード
			/// </summary>
			public string Keyword { get; set; }

			/// <summary>
			/// スタートページからリンク先アドレスまでの距離
			/// </summary>
			public int Distance { get; set; }

			/// <summary>
			/// 巡回優先度
			/// </summary>
			public int Priority { get; set; }

			/// <summary>
			/// リンク先アドレスがインストーラーかどうか
			/// </summary>
			public bool IsInstaller { get; private set; }

			/// <summary>
			/// コンストラクタ、全要素を指定して初期化する
			/// </summary>
			public Link(int id, string address, string keyword, int distance, int priority) {
				this.Id = id;
				this.Address = address;
				this.Keyword = keyword;
				this.Distance = distance;
				this.Priority = priority;
				this.IsInstaller = IsAddressInstaller(address);
			}

			public override string ToString() {
				return string.Join(" : ", this.Keyword, this.Address);
			}
		}

		/// <summary>
		/// 指定アドレスがインストーラーかどうか判定する
		/// </summary>
		/// <param name="address">アドレス</param>
		/// <returns>インストーラーなら true</returns>
		public static bool IsAddressInstaller(string address) {
			var index = address.LastIndexOf('.');
			if (index < 0) {
				return false;
			}
			var ext = address.Substring(index).ToLower();
			switch (ext) {
			case ".exe":
			case ".msi":
			case ".msu":
			case ".msp":
				return true;
			default:
				return false;
			}
		}

		/// <summary>
		/// 指定アドレス、キーワード、距離から優先度を計算する
		/// </summary>
		/// <param name="address">リンク先アドレス</param>
		/// <param name="keyword">リンク関連キーワード</param>
		/// <param name="distance">スタートページからリンク先までの距離</param>
		/// <returns>優先度</returns>
		public static int CalcPriority(string address, string keyword, int distance) {
			int priority = 0;

			// リンク先がインストーラーのファイルならかなりポイント高い
			if (IsAddressInstaller(address)) {
				priority += 1000;
			}

			// リンク先関連のキーワードに製品名があればまぁまぁポイント高い
			if (Matching.IsMatched(keyword)) {
					priority += 100;
			}

			// なるべく開始ページから近いものを選びたいので距離が離れたらポイント減
			if (distance != 0) {
				priority /= distance;
			}

			return priority;
		}

		/// <summary>
		/// 埋め込みJavaScriptとの仲介オブジェクト
		/// </summary>
		public class HostObj {
			public event Action AddLinkStart;
			public event Action AddLinkEnd;

			public int Count;

			/// <summary>
			/// スタートページから現在のページまでの距離
			/// </summary>
			public int CurrentDistance = 0;

			/// <summary>
			/// 巡回済みアドレス
			/// </summary>
			public HashSet<string> KnownLinks { get; private set; } = new HashSet<string>();

			/// <summary>
			/// これから巡回するリンク、最後尾側が優先度高
			/// </summary>
			public List<Link> Links { get; private set; } = new List<Link>();


			public void testFunc(object[] values) {
			}

			public void start() {
				var d = this.AddLinkStart;
				if (d != null) {
					d();
				}
			}
			public void addLink(int id, string address, string keyword) {
				lock (this) {
					// 既に巡回したアドレスは無視する
					if (this.KnownLinks.Contains(address)) {
						return;
					}
					this.KnownLinks.Add(address);

					// 巡回するアドレスとして登録
					this.Links.Add(new Link(id, address, keyword, this.CurrentDistance, CalcPriority(address, keyword, this.CurrentDistance)));
				}
			}
			public void setTable(int id, object[] cells) {
				// TODO: テーブルを構築し、リンクが関連する行や列から情報を取得しタグ付けしながら Link オブジェクトに情報を追加していく
			}
			public void setInnerText(string htmlText) {
				lock (this) {
					File.WriteAllText(this.Count + ".txt", htmlText, Encoding.UTF8);
					this.Count++;
				}
			}
			public void end() {
				lock (this) {
					this.Links.Sort((l, r) => l.Priority - r.Priority);
				}

				var d = this.AddLinkEnd;
				if (d != null) {
					d();
				}
			}
		}

		HostObj _HostObj = new HostObj();
		ChromiumWebBrowser _Browser;
		bool _LoadEnded;

		public MainWindow() {
			var asm = Assembly.GetExecutingAssembly();
			var sb = new StringBuilder();
			using (var jquery = new StreamReader(asm.GetManifestResourceStream("PatchStalker.jquery-min.js"), Encoding.UTF8))
			using (var embedding = new StreamReader(asm.GetManifestResourceStream("PatchStalker.embedding.js"), Encoding.UTF8)) {
				sb.AppendLine(jquery.ReadToEnd());
				sb.AppendLine(embedding.ReadToEnd());
			}
			var embeddingjs = sb.ToString();

			_HostObj.CurrentDistance = 1;
			_HostObj.AddLinkEnd += () => {
				// ページ内の全リンク取得後の処理
				this.Dispatcher.BeginInvoke(new Action(() => {
					lock (_HostObj) {
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
			_Browser = new ChromiumWebBrowser();
			Grid.SetRow(_Browser, 1);

			// ロケールを日本語とする
			_Browser.BrowserSettings.AcceptLanguageList = "ja-JP";

			// 埋め込みJavaScriptとの仲介オブジェクト登録
			_Browser.JavascriptObjectRepository.Register(BinderName, _HostObj);

			// ページ読み込みが完了したらJavaScript側から処理を呼び出す
			_Browser.LoadingStateChanged += (s, a) => {
				if (_LoadEnded && !a.IsLoading) {
					_Browser.ExecuteScriptAsync(embeddingjs);
				}
			};
			_Browser.FrameLoadStart += (sender, args) => {
				_LoadEnded = false;
			};
			_Browser.FrameLoadEnd += (sender, args) => {
				_LoadEnded = true;
			};

			InitializeComponent();

			this.gridRoot.Children.Add(_Browser);
			this.Loaded += MainWindow_Loaded;

			//Navigate("https://jvndb.jvn.jp/");
			//Navigate("https://helpx.adobe.com/security/products/acrobat/apsb18-02.html");
			//Navigate("https://supportdownloads.adobe.com/product.jsp?product=1&platform=Windows");
			Navigate(System.IO.Path.Combine(Directory.GetCurrentDirectory(), "test1.html"));
		}

		void Navigate(string url) {
			this.tbUrl.Text = _Browser.Address = url;
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e) {
			_Browser.ShowDevTools();
		}
	}
}
