using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Jk {
	/// <summary>
	/// CSV形式ストリーム書き込みクラス
	/// </summary>
	public class CsvWriter : IDisposable {
		/// <summary>
		/// テキスト書き込みオブジェクト
		/// </summary>
		public TextWriter TextWriter {
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
		public CsvWriter(TextWriter tw) {
			this.TextWriter = tw;
			this.Separator = ',';
			this.Bundler = '"';
		}

		/// <summary>
		/// 行を書き込む
		/// </summary>
		/// <param name="fields">フィールド配列</param>
		public void WriteRow(IEnumerable<string> fields) {
			this.TextWriter.WriteLine(Csv.Combine(fields, this.Separator, this.Bundler));
		}

		/// <summary>
		/// 行を書き込む
		/// </summary>
		/// <param name="fields">フィールド配列</param>
		public void WriteRow(params object[] fields) {
			var stringFields = new string[fields.Length];
			for (int i = 0; i < stringFields.Length; i++) {
				stringFields[i] = fields[i].ToString();
			}
			this.TextWriter.WriteLine(Csv.Combine(stringFields, this.Separator, this.Bundler));
		}

		/// <summary>
		/// ストリームを閉じる
		/// </summary>
		public void Close() {
			var tw = this.TextWriter;
			if (tw != null) {
				tw.Close();
				this.TextWriter = null;
			}
		}

		/// <summary>
		/// ファイナライザ
		/// </summary>
		~CsvWriter() {
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
