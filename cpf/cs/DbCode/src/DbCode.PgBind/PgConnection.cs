using System;
using System.Collections.Generic;
using System.Text;
using DbCode;
using DbCode.Internal;
using Npgsql;
using NpgsqlTypes;

namespace DbCode.PgBind {
	public class PgConnection : IDbCodeConnection {
		NpgsqlConnection _Core;

		public object Core => _Core;

		public PgConnection(NpgsqlConnection core) {
			_Core = core;
		}

		public IDbCodeCommand CreateCommand() {
			try {
				return new PgCommand(_Core.CreateCommand(), this);
			} catch (PostgresException ex) {
				throw new PgEnvironmentException(ex);
			}
		}
		public void Open() {
			try {
				_Core.Open();
			} catch (PostgresException ex) {
				throw new PgEnvironmentException(ex);
			}
		}

		#region IDisposable Support
		private bool disposedValue = false; // 重複する呼び出しを検出するには

		protected virtual void Dispose(bool disposing) {
			if (!disposedValue) {
				if (disposing) {
					// マネージ状態を破棄します (マネージ オブジェクト)。
					if (_Core != null) {
						_Core.Dispose();
						_Core = null;
					}
				}

				// アンマネージ リソース (アンマネージ オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
				// 大きなフィールドを null に設定します。
				disposedValue = true;
			}
		}

		~PgConnection() {
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
