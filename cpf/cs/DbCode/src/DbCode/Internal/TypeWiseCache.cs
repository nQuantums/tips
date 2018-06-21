using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;

namespace DbCode.Internal {
	/// <summary>
	/// 型毎情報のキャッシュ
	/// </summary>
	/// <typeparam name="T">キャッシュ対象の型</typeparam>
	public static class TypewiseCache<T> {
		static Func<T> _Creator;
		static Func<T, T> _Cloner;
		static Action<ElementCode, T> _AddValues;

		/// <summary>
		/// <c>T</c>型のオブジェクトを生成するファンクション、デフォルトコンストラクタがあればそれを優先し無ければメンバを引数として渡すものを探しデフォルト値を渡す
		/// </summary>
		public static Func<T> Creator {
			get {
				if (_Creator == null) {
					var type = typeof(T);

					var defCtor = type.GetConstructor(new Type[0]);
					if (defCtor != null) {
						_Creator = Expression.Lambda<Func<T>>(Expression.Block(Expression.New(defCtor))).Compile();
					} else {
						var properties = type.GetProperties();
						var propertyTypes = (from p in properties select p.PropertyType).ToArray();
						var memberPassCtor = type.GetConstructor(propertyTypes);
						if (memberPassCtor != null) {
							var args = new Expression[properties.Length];
							for (int i = 0; i < properties.Length; i++) {
								args[i] = Expression.Default(properties[i].PropertyType);
							}
							_Creator = Expression.Lambda<Func<T>>(Expression.New(memberPassCtor, args)).Compile();
						} else {
							throw new ApplicationException();
						}
					}
				}
				return _Creator;
			}
		}

		/// <summary>
		/// <c>T</c>型のクローンを生成するファンクション、メンバーを渡すコンストラクタがあればそれを優先し無ければデフォルトコンストラクタ呼び出し後にメンバにアサインする
		/// </summary>
		public static Func<T, T> Cloner {
			get {
				if (_Cloner == null) {
					var type = typeof(T);
					var param = Expression.Parameter(type);
					var properties = type.GetProperties();
					var propertyTypes = (from p in properties select p.PropertyType).ToArray();

					var memberPassCtor = type.GetConstructor(propertyTypes);
					if (memberPassCtor != null) {
						var args = new Expression[properties.Length];
						for (int i = 0; i < properties.Length; i++) {
							args[i] = Expression.Property(param, properties[i]);
						}
						_Cloner = Expression.Lambda<Func<T, T>>(Expression.New(memberPassCtor, args), param).Compile();
					} else {
						var defCtor = type.GetConstructor(new Type[0]);
						if (defCtor != null) {
							var result = Expression.Parameter(type);
							var exprs = new Expression[properties.Length + 2];
							exprs[0] = Expression.Assign(result, Expression.New(defCtor));
							for (int i = 0; i < properties.Length; i++) {
								exprs[i + 1] = Expression.Assign(Expression.Property(result, properties[i]), Expression.Property(param, properties[i]));
							}
							exprs[properties.Length + 1] = result;
							_Cloner = Expression.Lambda<Func<T, T>>(Expression.Block(new[] { result }, exprs), param).Compile();
						} else {
							throw new ApplicationException();
						}
					}
				}
				return _Cloner;
			}
		}

		/// <summary>
		/// <see cref="ElementCode"/>にカンマ区切りでプロパティを列挙するファンクション
		/// </summary>
		public static Action<ElementCode, T> AddValues {
			get {
				if (_AddValues == null) {
					var type = typeof(T);
					var context = typeof(ElementCode);
					var param1 = Expression.Parameter(typeof(ElementCode));
					var param2 = Expression.Parameter(type);
					var properties = type.GetProperties();
					var expressions = new Expression[properties.Length * 2 - 1];
					var addComma = context.GetMethod(nameof(ElementCode.AddComma));

					for (int i = 0, j = 0; i < properties.Length; i++) {
						var pi = properties[i];
						if (i != 0) {
							expressions[j++] = Expression.Call(param1, addComma);
						}
						var method = context.GetMethod(nameof(ElementCode.Add), new[] { pi.PropertyType });
						if (method is null) {
							throw new ApplicationException();
						}
						expressions[j++] = Expression.Call(param1, method, Expression.Property(param2, pi));
					}

					var expr = Expression.Lambda<Action<ElementCode, T>>(Expression.Block(expressions), param1, param2);
					_AddValues = (Action<ElementCode, T>)expr.Compile();
				}
				return _AddValues;
			}
		}
	}
}
