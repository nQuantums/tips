using System;
using System.Collections.Generic;
using System.Text;

namespace DbCode {
	/// <summary>
	/// <see cref="IDbCodeDataReader"/>から<typeparamref name="T"/>型のレコードを列挙するクラス
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class RecordEnumerator<T> : IDisposable {
		/// <summary>
		/// DBからのデータ読み取りオブジェクト
		/// </summary>
		public IDbCodeDataReader DataReader { get; private set; }

		/// <summary>
		/// <see cref="DataReader"/>から<typeparamref name="T"/>型のレコードを列挙するオブジェクト
		/// </summary>
		public IRecordReader<T> RecordReader { get; private set; }

		/// <summary>
		/// レコードを列挙する
		/// </summary>
		public IEnumerable<T> Records => this.RecordReader.Enumerate(this.DataReader);

		/// <summary>
		/// コンストラクタ、全要素を指定して初期化する
		/// </summary>
		/// <param name="dataReader">DBからのデータ読み取りオブジェクト</param>
		/// <param name="recordReader"><see cref="DataReader"/>から<typeparamref name="T"/>型のレコードを列挙するオブジェクト</param>
		public RecordEnumerator(IDbCodeDataReader dataReader, IRecordReader<T> recordReader) {
			this.DataReader = dataReader;
			this.RecordReader = recordReader;
		}

		#region IDisposable Support
		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				if (this.DataReader != null) {
					this.DataReader.Dispose();
				}
				this.RecordReader = null;
			}
		}

		~RecordEnumerator() {
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
