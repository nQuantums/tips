using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DbCode.Internal;

namespace DbCode.Query {
	/// <summary>
	/// FROM句を含むSELECT
	/// </summary>
	/// <typeparam name="TSelectedColumns">プロパティを列として扱う<see cref="TableDef{TSelectedColumns}"/>のTColumnsに該当するクラス</typeparam>
	public class SelectFrom<TSelectedColumns> : ISelect<TSelectedColumns> {
		#region プロパティ
		/// <summary>
		/// ノードが属するSQLオブジェクト
		/// </summary>
		public Sql Owner => this.Parent.Owner;

		/// <summary>
		/// 親ノード
		/// </summary>
		public IQueryNode Parent { get; private set; }

		/// <summary>
		/// 子ノード一覧
		/// </summary>
		public IEnumerable<IQueryNode> Children {
			get {
				if (this.From != null) {
					yield return this.From;
				}
			}
		}

		/// <summary>
		/// 生成元のFROM
		/// </summary>
		public IFrom From { get; private set; }

		/// <summary>
		/// 列をプロパティとして持つオブジェクト
		/// </summary>
		public TSelectedColumns Columns { get; private set; }

		/// <summary>
		/// 列をプロパティとして持つオブジェクト
		/// </summary>
		public TSelectedColumns _ => this.Columns;

		/// <summary>
		/// テーブルが直接保持する列定義の取得
		/// </summary>
		public ColumnMap ColumnMap { get; private set; }
		#endregion

		#region コンストラクタ
		/// <summary>
		/// コンストラクタ、親ノードと列指定式を指定して初期化する
		/// </summary>
		/// <param name="parent">親ノード</param>
		/// <param name="body">生成元の式</param>
		public SelectFrom(IFrom parent, Expression body) {
			this.Parent = parent;
			this.From = parent;
			this.ColumnMap = new ColumnMap();
			this.Columns = TypeWiseCache<TSelectedColumns>.Creator();

			if (body.NodeType == ExpressionType.New) {
				// new 演算子でのクラス生成式に対応

				// クラスのプロパティ数とコンストラクタ引数の数が異なるならエラーとする
				var newexpr = body as NewExpression;
				var args = newexpr.Arguments;
				var properties = typeof(TSelectedColumns).GetProperties();
				if (args.Count != properties.Length) {
					throw new ApplicationException();
				}

				// プロパティと列定義を結びつけその生成元としてコンストラクタ引数を指定する
				var owner = this.Owner;
				var environment = owner.Environment;
				var allColumns = owner.AllColumns;
				for (int i = 0; i < properties.Length; i++) {
					var pi = properties[i];
					if (pi.PropertyType != args[i].Type) {
						throw new ApplicationException();
					}
					BindColumn(pi.Name, "c" + i, environment.CreateDbTypeFromType(pi.PropertyType), 0, new ElementCode(args[i], allColumns));
				}
			} else if (body.NodeType == ExpressionType.MemberInit) {
				// メンバ初期化式に対応

				// クラスのプロパティ数とコンストラクタ引数の数が異なるならエラーとする
				var initexpr = body as MemberInitExpression;
				var bindings = initexpr.Bindings;

				// プロパティと列定義を結びつけその生成元としてコンストラクタ引数を指定する
				var owner = this.Owner;
				var environment = owner.Environment;
				var allColumns = owner.AllColumns;
				for (int i = 0; i < bindings.Count; i++) {
					var binding = bindings[i];
					if (binding.BindingType != MemberBindingType.Assignment) {
						throw new ApplicationException();
					}
					var assign = binding as MemberAssignment;

					var member = binding.Member;
					if (member.MemberType != System.Reflection.MemberTypes.Property) {
						throw new ApplicationException();
					}
					var property = (PropertyInfo)member;
					BindColumn(property.Name, "c" + i, environment.CreateDbTypeFromType(property.PropertyType), 0, new ElementCode(assign.Expression, allColumns));
				}
			} else {
				throw new ApplicationException();
			}

		}
		#endregion

		#region 公開メソッド
		/// <summary>
		/// プロパティに列定義をバインドして取得する、バインド済みなら取得のみ行われる
		/// </summary>
		/// <param name="propertyName">プロパティ名</param>
		/// <param name="name">DB上での列名</param>
		/// <param name="dbType">DB上での型</param>
		/// <param name="flags">列に対するオプションフラグ</param>
		/// <param name="source">列を生成する基となった式</param>
		/// <returns>列定義</returns>
		public Column BindColumn(string propertyName, string name, IDbType dbType, ColumnFlags flags = 0, ElementCode source = null) {
			var column = this.ColumnMap.TryGetByPropertyName(propertyName);
			if (column == null) {
				this.ColumnMap.Add(column = new Column(this.Owner.Environment, this.Columns, typeof(TSelectedColumns).GetProperty(propertyName), this, name, dbType, flags, source));
			}
			return column;
		}

		/// <summary>
		/// エイリアス用にクローンを作成する
		/// </summary>
		/// <returns>クローン</returns>
		public SelectFrom<TSelectedColumns> AliasedClone() {
			return this;
		}

		/// <summary>
		/// SQL文を生成する
		/// </summary>
		/// <param name="context">生成先のコンテキスト</param>
		public void ToElementCode(ElementCode context) {
			int i = 0;
			context.Add(SqlKeyword.Select);
			context.AddColumns(this.ColumnMap, column => {
				context.Add(column.Source);
				context.Add(SqlKeyword.As);
				context.Concat("c" + (i++));
			});
		}
		#endregion

		#region 非公開メソッド
		ITable<TSelectedColumns> ITable<TSelectedColumns>.AliasedClone() => this.AliasedClone();
		ITable ITable.AliasedClone() => this.AliasedClone();
		#endregion
	}
}
