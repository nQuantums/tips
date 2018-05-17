using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using Npgsql;
using NpgsqlTypes;

namespace DbCode.PgBind {
	public class PgRecordReader<T> : IRecordReader<T> {
		#region 内部クラス
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
		#endregion

		#region フィールド
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

		Func<NpgsqlDataReader, T> _Getter;
		PropertyInfo[] _AssignedProperties;
		#endregion

		#region コンストラクタ
		public PgRecordReader(PropertyInfo[] propertiesToAssign) {
			_Getter = CreateRecordGetter(propertiesToAssign, out _AssignedProperties);
		}
		#endregion

		#region 公開メソッド
		public IEnumerable<T> Enumerate(IDbCodeDataReader dataReader) {
			var core = dataReader.Core as NpgsqlDataReader;
			if (core == null) {
				throw new ApplicationException();
			}
			if (core.FieldCount != _AssignedProperties.Length) {
				throw new ApplicationException();
			}
			var getter = _Getter;
			while (core.Read()) {
				T result;
				try {
					result = getter(core);
				} catch (PostgresException ex) {
					throw new PgEnvironmentException(ex);
				}
				yield return result;
			}
		}
		#endregion

		#region 非公開メソッド
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

			// TODO: 配列にも対応する
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
				CvmInfo info;
				if (type.IsArray) {
					// 配列なら要素の型で判断
					var elementType = type.GetElementType();
					if (!CvmInfos.TryGetValue(elementType, out info)) {
						throw new ApplicationException();
					}
					// 配列は現状直接取得できるメソッドが無いので as で変換しておく
					return Expression.TypeAs(Expression.Call(dr, drtype.GetMethod(nameof(NpgsqlDataReader.GetValue)), Expression.Constant(ordinal)), type);
				} else {
					// null 許容型でも配列でもない場合には null 判定が必要無いため最適なメソッドで値を取得する
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
		}

		static Func<NpgsqlDataReader, T> CreateRecordGetter(PropertyInfo[] propertiesToAssign, out PropertyInfo[] assignedProperties) {
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
				assignedProperties = null;
			} else {
				// 各列を取得する式を生成する
				var properties = type.GetProperties();
				if (propertiesToAssign == null) {
					propertiesToAssign = properties;
					assignedProperties = properties;
				} else {
					assignedProperties = propertiesToAssign;
				}
				var getExprs = new Expression[propertiesToAssign.Length];
				var indices1 = new Tuple<int, int>[propertiesToAssign.Length];
				for (int i = 0; i < propertiesToAssign.Length; i++) {
					var p = propertiesToAssign[i];
					var index = Array.IndexOf(properties, p);
					if (index < 0) {
						// 対象クラス、構造体に存在しないプロパティが指定されていたらエラー
						throw new ApplicationException();
					}

					indices1[i] = new Tuple<int, int>(i, index);
					getExprs[i] = GenerateGet(p.PropertyType, param, i, variables);
				}

				// 型の順番を allProperties の順に沿うに変更して対応するコンストラクタを探す
				var indices2 = (from i in indices1 orderby i.Item2 select i).ToArray();
				var argTypes = (from i in indices2 select properties[i.Item2].PropertyType).ToArray();
				var assignCtor = type.GetConstructor(argTypes);
				if (assignCtor != null) {
					result = Expression.New(type.GetConstructor(argTypes), (from i in indices2 select getExprs[i.Item1]).ToArray());
				} else {
					// 見つからないならデフォルトコンストラクタを探してメンババインディングで対応する
					var defCtor = type.GetConstructor(new Type[0]);
					if (defCtor == null) {
						throw new ApplicationException();
					}
					result = Expression.MemberInit(
						Expression.New(defCtor),
						from i in indices1 select Expression.Bind((MemberInfo)properties[i.Item2], getExprs[i.Item1])
					);
				}
			}

			// 一応一時変数使う場合と使わない場合に分けておく
			if (variables.Count != 0) {
				return Expression.Lambda<Func<NpgsqlDataReader, T>>(Expression.Block(variables, new[] { result }), param).Compile();
			} else {
				return Expression.Lambda<Func<NpgsqlDataReader, T>>(result, param).Compile();
			}
		}
		#endregion
	}
}
