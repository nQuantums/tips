using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using DbCode;
using DbCode.Internal;
using Npgsql;
using NpgsqlTypes;
using System.Reflection;

namespace DbCode.PgBind {
	public class PgDataReader : IDbCodeDataReader {
		NpgsqlDataReader _Core;

		public object Core => _Core;

		public PgDataReader(NpgsqlDataReader core) {
			_Core = core;
		}

		public bool Read() {
			try {
				return _Core.Read();
			} catch (PostgresException ex) {
				throw new PgEnvironmentException(ex);
			}
		}
		public bool NextResult() {
			try {
				return _Core.NextResult();
			} catch (PostgresException ex) {
				throw new PgEnvironmentException(ex);
			}
		}

		public bool GetBoolean(int ordinal) => _Core.GetBoolean(ordinal);
		public char GetChar(int ordinal) => _Core.GetChar(ordinal);
		public int GetInt32(int ordinal) => _Core.GetInt32(ordinal);
		public long GetInt64(int ordinal) => _Core.GetInt64(ordinal);
		public double GetDouble(int ordinal) => _Core.GetDouble(ordinal);
		public string GetString(int ordinal) => _Core.GetString(ordinal);
		public Guid GetGuid(int ordinal) => _Core.GetGuid(ordinal);
		public DateTime GetDateTime(int ordinal) => _Core.GetDateTime(ordinal);
		public object GetValue(int ordinal) => _Core.GetValue(ordinal);

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

		~PgDataReader() {
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
