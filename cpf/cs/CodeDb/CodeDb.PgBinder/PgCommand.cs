using System;
using System.Collections.Generic;
using System.Text;
using CodeDb;
using CodeDb.Internal;
using Npgsql;
using NpgsqlTypes;

namespace CodeDb.PgBinder {
	public class PgCommand : ICodeDbCommand {
		NpgsqlCommand _Core;
		PgConnection _Connection;

		public object Core => _Core;
		public ICodeDbConnection Connection => _Connection;
		public string CommandText { get => _Core.CommandText; set => _Core.CommandText = value; }
		public int CommandTimeout { get => _Core.CommandTimeout; set => _Core.CommandTimeout = value; }

		public PgCommand(NpgsqlCommand core, PgConnection connection) {
			_Core = core;
			_Connection = connection;
		}

		public void Apply(SqlProgram program) {
			_Core.CommandText = program.CommandText;
			var prms = _Core.Parameters;
			prms.Clear();
			foreach (var p in program.ParameterNameAndValues) {
				prms.AddWithValue(p.Name, p.Value);
			}
		}
		public void Cancel() {
			try {
				_Core.Cancel();
			} catch (PostgresException ex) {
				throw new PgEnvironmentException(ex);
			}
		}
		public int ExecuteNonQuery() {
			try {
				return _Core.ExecuteNonQuery();
			} catch (PostgresException ex) {
				throw new PgEnvironmentException(ex);
			}
		}
		public ICodeDbDataReader ExecuteReader() {
			try {
				return new PgDataReader(_Core.ExecuteReader());
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
					_Connection = null;
				}

				// アンマネージ リソース (アンマネージ オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
				// 大きなフィールドを null に設定します。
				disposedValue = true;
			}
		}

		~PgCommand() {
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
