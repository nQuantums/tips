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
		class CvmInfo {
			public bool IsClass { get; private set; }
			public string MethodNameOfGet { get; private set; }
			public string MethodNameOfConvert { get; private set; }

			public CvmInfo(bool isClass, string methodNameGet, string methodNameConvert) {
				this.IsClass = isClass;
				this.MethodNameOfGet = methodNameGet;
				this.MethodNameOfConvert = methodNameConvert;
			}
		}

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

			static readonly Dictionary<Type, CvmInfo> CvmInfos = new Dictionary<Type, CvmInfo> {
				{ typeof(bool), new CvmInfo(false, nameof(NpgsqlDataReader.GetBoolean), nameof(Convert.ToBoolean)) },
				{ typeof(char), new CvmInfo(false, nameof(NpgsqlDataReader.GetChar), nameof(Convert.ToChar)) },
				{ typeof(int), new CvmInfo(false, nameof(NpgsqlDataReader.GetInt32), nameof(Convert.ToInt32)) },
				{ typeof(long), new CvmInfo(false, nameof(NpgsqlDataReader.GetInt64), nameof(Convert.ToInt64)) },
				{ typeof(double), new CvmInfo(false, nameof(NpgsqlDataReader.GetDouble), nameof(Convert.ToDouble)) },
				{ typeof(string), new CvmInfo(true, null, null) },
				{ typeof(Guid), new CvmInfo(false, nameof(NpgsqlDataReader.GetGuid), null) },
				{ typeof(DateTime), new CvmInfo(false, nameof(NpgsqlDataReader.GetDateTime), nameof(Convert.ToDateTime)) },
			};

			static Expression GenerateCast(Type primtype, CvmInfo info, Expression source) {
				if (info.MethodNameOfConvert != null) {
					return Expression.Call(typeof(Convert).GetMethod(info.MethodNameOfConvert, new[] { typeof(object) }), source);
				} else {
					if (info.IsClass) {
						return Expression.TypeAs(source, primtype);
					} else {
						return Expression.Convert(source, primtype);
					}
				}
			}

			static Expression GenerateGet(Type type, Expression dr, int ordinal, List<ParameterExpression> variables) {
				var drtype = typeof(NpgsqlDataReader);
				var cvtype = typeof(Convert);

				if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)) {
					// null 許容型の場合は一旦 GetValue() で取得して System.DBNull 以外ならキャストする
					var primtype = type.GenericTypeArguments[0];

					CvmInfo info;
					if (!CvmInfos.TryGetValue(primtype, out info)) {
						throw new ApplicationException();
					}

					// 一時変数宣言
					var temp = Expression.Parameter(typeof(object));
					variables.Add(temp);

					// 一時変数に GetValue() で取得
					var assign = Expression.Assign(
						temp,
						Expression.Call(dr, drtype.GetMethod(nameof(NpgsqlDataReader.GetValue)), Expression.Constant(ordinal))
					);

					// 一時変数が DBNull.Value なら Nullable のデフォルト値を、それ以外はキャストして取得する
					return Expression.Condition(
						Expression.Equal(assign, Expression.Field(null, typeof(DBNull), nameof(DBNull.Value))),
						Expression.New(type),
						Expression.New(type.GetConstructor(new Type[] { primtype }), GenerateCast(primtype, info, assign)));
				} else {
					// null 許容型でない場合には null 判定が必要無いため最適なメソッドで値を取得する
					CvmInfo info;
					if (!CvmInfos.TryGetValue(type, out info)) {
						throw new ApplicationException();
					}

					if (info.MethodNameOfGet != null) {
						// Get* メソッドで直接目的の型が取得できるもの
						return Expression.Call(dr, drtype.GetMethod(info.MethodNameOfGet), Expression.Constant(ordinal));
					} else {
						// GetValue メソッドで一旦 object 取得後に変換が必要なもの
						return GenerateCast(type, info, Expression.Call(dr, drtype.GetMethod(nameof(NpgsqlDataReader.GetValue)), Expression.Constant(ordinal)));
					}
				}
			}

			static Func<NpgsqlDataReader, T> CreateDataGetter() {
				var type = typeof(T);
				var drtype = typeof(NpgsqlDataReader);
				var param = Expression.Parameter(drtype);
				var variables = new List<ParameterExpression>();
				Expression result;
				CvmInfo info;

				CvmInfos.TryGetValue(type, out info);

				if (info != null || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)) {
					// 単一の値の場合、読み込んだ値がそのまま結果となる
					result = GenerateGet(type, param, 0, variables);
				} else {
					// クラスや構造体の場合、コンストラクタに読み込んだ値を渡して new する
					var properties = type.GetProperties();
					var args = new Expression[properties.Length];
					var argTypes = new Type[properties.Length];
					for (int i = 0; i < properties.Length; i++) {
						var pi = properties[i];
						var pt = pi.PropertyType;
						var iexpr = Expression.Constant(i);
						argTypes[i] = pt;
						args[i] = GenerateGet(pt, param, i, variables);
					}
					result = Expression.New(type.GetConstructor(argTypes), args);
				}

				// 一応一時変数使う場合と使わない場合に分けておく
				if (variables.Count != 0) {
					return Expression.Lambda<Func<NpgsqlDataReader, T>>(Expression.Block(variables, new[] { result }), param).Compile();
				} else {
					return Expression.Lambda<Func<NpgsqlDataReader, T>>(result, param).Compile();
				}
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
		public TColumns Get<TColumns>() {
			return Cache<TColumns>.DataGetter(_Core);
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
