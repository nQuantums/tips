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
		public MainWindow() {
			InitializeComponent();

			//Navigate("https://jvndb.jvn.jp/");
			//Navigate("https://helpx.adobe.com/security/products/acrobat/apsb18-02.html");
			//Navigate("https://supportdownloads.adobe.com/product.jsp?product=1&platform=Windows");
			this.tbUrl.Text = "https://www.catalog.update.microsoft.com/Search.aspx?q=KB4056893";
			//this.tbUrl.Text = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "test1.html");

		}

		private void btnGo_Click(object sender, RoutedEventArgs e) {
			var wnd = new BrowserWindow(this.tbUrl.Text);
			wnd.Owner = this;
			wnd.Show();
		}
	}
}
