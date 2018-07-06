using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Jk {
	/// <summary>
	/// CSV形式ストリーム読み込みクラス
	/// </summary>
	public class CsvReader : IDisposable {
		/// <summary>
		/// テキスト書き込みオブジェクト
		/// </summary>
		public TextReader TextReader {
			get;
			private set;
		}

		/// <summary>
		/// 区切り文字
		/// </summary>
		public char Separator {
			get;
			set;
		}

		/// <summary>
		/// 括り文字
		/// </summary>
		public char Bundler {
			get;
			set;
		}

		/// <summary>
		/// コンストラクタ、テキストライターを指定して初期化する
		/// </summary>
		/// <param name="tw">テキストライター</param>
		public CsvReader(TextReader tw) {
			this.TextReader = tw;
			this.Separator = ',';
			this.Bundler = '"';
		}

		/// <summary>
		/// コンストラクタ、テキストライターを指定して初期化する
		/// </summary>
		/// <param name="tw">テキストライター</param>
		/// <param name="separator">区切り文字</param>
		/// <param name="bundler">括り文字</param>
		public CsvReader(TextReader tw, char separator, char bundler) {
			this.TextReader = tw;
			this.Separator = separator;
			this.Bundler = bundler;
		}

		/// <summary>
		/// 行を書き込む
		/// </summary>
		/// <param name="fields">フィールド配列</param>
		public List<string> ReadRow() {
			var line = this.TextReader.ReadLine();
			if (line == null)
				return null;
			return Csv.Split(line, this.Separator, this.Bundler);
		}

		/// <summary>
		/// ストリームを閉じる
		/// </summary>
		public void Close() {
			var tw = this.TextReader;
			if (tw != null) {
				tw.Close();
				this.TextReader = null;
			}
		}

		/// <summary>
		/// ファイナライザ
		/// </summary>
		~CsvReader() {
			Dispose(false);
		}

		/// <summary>
		/// アンマネージ リソースの解放およびリセットに関連付けられているアプリケーション定義のタスクを実行します。
		/// </summary>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// アンマネージ リソースの解放およびリセットに関連付けられているアプリケーション定義のタスクを実行します。
		/// </summary>
		/// <param name="disposing">解放するかどうか</param>
		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				// Clean up all managed resources
				Close();
			}
			// Clean up all native resources
		}
	}
}
