using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using CodeDb;
using CodeDb.Internal;
using Npgsql;
using NpgsqlTypes;

namespace CodeDb.PgBinder {
	public class PgDataReader : ICodeDbDataReader {
		/// <summary>
		/// 型毎のデータなどをキャッシュしておくクラス
		/// </summary>
		/// <typeparam name="T">キャッシュ対象の型</typeparam>
		public static class Cache<T> {
			static Func<NpgsqlDataReader, T> _DataGetter;

			public static Func<NpgsqlDataReader, T> DataGetter {
				get {
					if (_DataGetter == null) {
						_DataGetter = CreateDataGetter();
					}
					return _DataGetter;
				}
			}

			static Func<NpgsqlDataReader, T> CreateDataGetter() {
				var type = typeof(T);
				var drtype = typeof(NpgsqlDataReader);
				var param = Expression.Parameter(drtype);
				var properties = type.GetProperties();
				var args = new Expression[properties.Length];
				var argTypes = new Type[properties.Length];
				for (int i = 0; i < properties.Length; i++) {
					var pi = properties[i];
					var pt = pi.PropertyType;
					var iexpr = Expression.Constant(i);

					argTypes[i] = pt;

					Expression arg;
					if (pt == typeof(bool)) {
						arg = Expression.Call(param, drtype.GetMethod("GetBoolean"), iexpr);
					} else if (pt == typeof(char)) {
						arg = Expression.Call(param, drtype.GetMethod("GetChar"), iexpr);
					} else if (pt == typeof(int)) {
						arg = Expression.Call(param, drtype.GetMethod("GetInt32"), iexpr);
					} else if (pt == typeof(long)) {
						arg = Expression.Call(param, drtype.GetMethod("GetInt64"), iexpr);
					} else if (pt == typeof(double)) {
						arg = Expression.Call(param, drtype.GetMethod("GetDouble"), iexpr);
					} else if (pt == typeof(string)) {
						arg = Expression.Call(param, drtype.GetMethod("GetString"), iexpr);
					} else if (pt == typeof(Guid)) {
						arg = Expression.Call(param, drtype.GetMethod("GetGuid"), iexpr);
					} else if (pt == typeof(DateTime)) {
						arg = Expression.Call(param, drtype.GetMethod("GetDateTime"), iexpr);
					} else {
						arg = Expression.Convert(Expression.Call(param, drtype.GetMethod("GetValue"), iexpr), pt);
					}
					args[i] = arg;
				}

				return Expression.Lambda<Func<NpgsqlDataReader, T>>(Expression.New(type.GetConstructor(argTypes), args), param).Compile();
			}
		}

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
		public TypeOfCols Get<TypeOfCols>() {
			return Cache<TypeOfCols>.DataGetter(_Core);
		}

		public IEnumerable<TColumns> Enumerate<TColumns>() {
			var getter = Cache<TColumns>.DataGetter;
			var core = _Core;
			while (this.Read()) {
				TColumns result;
				try {
					result = getter(core);
				} catch (PostgresException ex) {
					throw new PgEnvironmentException(ex);
				}
				yield return result;
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
