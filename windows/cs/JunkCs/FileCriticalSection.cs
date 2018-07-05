using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;

namespace Jk {
	/// <summary>
	/// ファイルを使用したクリティカルセクション
	/// </summary>
	public class FileCriticalSection : IDisposable {
		#region フィールド
		FileStream _FileStream;
		int _LockCounter;
		#endregion

		#region プロパティ
		/// <summary>
		/// 排他ファイルパス名
		/// </summary>
		public string FilePath {
			get;
			private set;
		}

		/// <summary>
		/// 現在排他処理中かどうか判定する
		/// </summary>
		public bool IsLocked {
			get {
				return _FileStream != null;
			}
		}
		#endregion

		#region メソッド
		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="initiallyOwned">初期状態でアクセス権を所有するかどうか</param>
		/// <param name="filePath">排他ファイルパス名</param>
		public FileCriticalSection(bool initiallyOwned, string filePath) {
			this.FilePath = filePath;
			if (initiallyOwned) {
				Lock();
			}
		}

		/// <summary>
		/// 排他ロック開始、失敗したら例外が発生する
		/// </summary>
		public void Lock() {
			lock (this) {
				if (_FileStream != null) {
					_LockCounter++;
					return;
				}
				_FileStream = File.Create(this.FilePath, 4096, FileOptions.DeleteOnClose);
				_LockCounter++;
			}
		}

		/// <summary>
		/// 排他ロック終了、失敗したら例外が発生する
		/// </summary>
		public void Unlock() {
			lock (this) {
				if (_FileStream == null)
					return;

				var c = _LockCounter - 1;
				if (c == 0) {
					_FileStream.Dispose();
					_FileStream = null;
				}
				_LockCounter = c;
			}
		}

		/// <summary>
		/// ファイナライザ
		/// </summary>
		~FileCriticalSection() {
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
				Unlock();
			}
			// Clean up all native resources
		}
		#endregion
	}
}
