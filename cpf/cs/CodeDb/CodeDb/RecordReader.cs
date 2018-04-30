using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CodeDb {
	/// <summary>
	/// <typeparamref name="T"/>型の値を列挙する機能を提供
	/// </summary>
	/// <typeparam name="T">列挙される型</typeparam>
	public class RecordReader<T> : IDisposable {
		/// <summary>
		/// 内部で使用するデータリーダー
		/// </summary>
		public ICodeDbDataReader DataReader { get; private set; }

		/// <summary>
		/// レコード列挙
		/// </summary>
		public IEnumerable<T> Records => this.DataReader.Enumerate<T>();

		/// <summary>
		/// コンストラクタ、<see cref="ICodeDbCommand.ExecuteReader(Commandable)"/>の戻り値を指定して初期化する
		/// </summary>
		/// <param name="dataReader"><see cref="ICodeDbCommand.ExecuteReader(Commandable)"/>の戻り値</param>
		public RecordReader(ICodeDbDataReader dataReader) {
			this.DataReader = dataReader;
		}

		#region IDisposable Support
		protected virtual void Dispose(bool disposing) {
			if (this.DataReader != null) {
				this.DataReader.Dispose();
				this.DataReader = null;
			}
		}

		~RecordReader() {
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
