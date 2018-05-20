using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DbCode.Internal;

namespace DbCode.Query {
	/// <summary>
	/// FROM句を含まないSELECT
	/// </summary>
	/// <typeparam name="TColumns">プロパティを列として扱う<see cref="TableDef{TColumns}"/>のTColumnsに該当するクラス</typeparam>
	public class Select<TColumns> : ISelect<TColumns> {
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
				if (this.WhereNode != null) {
					yield return this.WhereNode;
				}
			}
		}

		/// <summary>
		/// WHERE句のノード
		/// </summary>
		public IWhere WhereNode { get; private set; }

		/// <summary>
		/// 列をプロパティとして持つオブジェクト
		/// </summary>
		public TColumns Columns { get; private set; }

		/// <summary>
		/// 列をプロパティとして持つオブジェクト
		/// </summary>
		public TColumns _ => this.Columns;

		/// <summary>
		/// テーブルが直接保持する列定義の取得
		/// </summary>
		public ColumnMap ColumnMap { get; private set; }
		#endregion

		#region コンストラクタ
		/// <summary>
		/// コンストラクタ、親ノードと列プロパティと設定する値の式を指定して初期化する
		/// </summary>
		/// <param name="parent">親ノード</param>
		/// <param name="properties">列に対応するプロパティ</param>
		/// <param name="values">列の値の式</param>
		[SqlMethod]
		public Select(IQueryNode parent, PropertyInfo[] properties, ElementCode[] values) {
			if (properties.Length != values.Length) {
				throw new ApplicationException();
			}

			this.Parent = parent;
			this.ColumnMap = new ColumnMap();
			this.Columns = TypewiseCache<TColumns>.Creator();

			// プロパティと列定義を結びつけその生成元としてコンストラクタ引数を指定する
			var environment = this.Owner.Environment;
			for (int i = 0; i < properties.Length; i++) {
				BindColumn(properties[i], values[i]);
			}
		}

		/// <summary>
		/// コンストラクタ、親ノードと列プロパティと設定する値の式を指定して初期化する
		/// </summary>
		/// <param name="parent">親ノード</param>
		/// <param name="properties">列に対応するプロパティ</param>
		/// <param name="values">列の値の式</param>
		[SqlMethod]
		public Select(IQueryNode parent, PropertyInfo[] properties, Expression[] values) {
			if (properties.Length != values.Length) {
				throw new ApplicationException();
			}

			this.Parent = parent;
			this.ColumnMap = new ColumnMap();
			this.Columns = TypewiseCache<TColumns>.Creator();

			// プロパティと列定義を結びつけその生成元としてコンストラクタ引数を指定する
			var environment = this.Owner.Environment;
			for (int i = 0; i < properties.Length; i++) {
				BindColumn(properties[i], new ElementCode(values[i], null));
			}
		}

		/// <summary>
		/// コンストラクタ、親ノードと列指定式を指定して初期化する
		/// </summary>
		/// <param name="parent">親ノード</param>
		/// <param name="columnsExpression">プロパティが列指定として扱われるクラスを生成する () => new { Name = "test", ID = 1 } の様な式</param>
		[SqlMethod]
		public Select(IQueryNode parent, Expression<Func<TColumns>> columnsExpression) {
			this.Parent = parent;

			this.ColumnMap = new ColumnMap();
			this.Columns = TypewiseCache<TColumns>.Creator();

			// new 演算子でクラスを生成するもの以外はエラーとする
			var body = columnsExpression.Body;
			if (body.NodeType != ExpressionType.New) {
				throw new ApplicationException();
			}

			// クラスのプロパティ数とコンストラクタ引数の数が異なるならエラーとする
			var newexpr = body as NewExpression;
			var args = newexpr.Arguments;
			var properties = typeof(TColumns).GetProperties();
			if (args.Count != properties.Length) {
				throw new ApplicationException();
			}

			// プロパティと列定義を結びつけその生成元としてコンストラクタ引数を指定する
			var environment = this.Owner.Environment;
			for (int i = 0; i < properties.Length; i++) {
				BindColumn(properties[i], new ElementCode(args[i], null));
			}
		}
		#endregion

		#region 公開メソッド
		/// <summary>
		/// 指定ノードを子とする、既存の親は<see cref="RemoveChild(IQueryNode)"/>で切り離す必要がある
		/// </summary>
		/// <param name="child">子ノード</param>
		public void AddChild(IQueryNode child) {
			var where = child as IWhere;
			if (where != null) {
				this.Where(where);
			} else {
				throw new ApplicationException();
			}
		}

		/// <summary>
		/// 指定の子ノードを取り除く
		/// </summary>
		/// <param name="child">子ノード</param>
		public void RemoveChild(IQueryNode child) {
			if (this.WhereNode == child) {
				this.WhereNode = null;
			}
		}

		/// <summary>
		/// 親ノードが変更された際に呼び出される
		/// </summary>
		/// <param name="parent">新しい親ノード</param>
		public void ChangeParent(IQueryNode parent) {
			this.Parent = parent;
		}

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
				this.ColumnMap.Add(column = new Column(this.Owner.Environment, this.Columns, typeof(TColumns).GetProperty(propertyName), this, name, dbType, flags, source));
			}
			return column;
		}

		/// <summary>
		/// プロパティに列定義をバインドして取得する、バインド済みなら取得のみ行われる
		/// </summary>
		/// <param name="property">プロパティ情報</param>
		/// <param name="source">列を生成する基となった式</param>
		/// <returns></returns>
		Column BindColumn(PropertyInfo property, ElementCode source = null) {
			var column = this.ColumnMap.TryGetByPropertyName(property.Name);
			if (column != null) {
				return column;
			}

			// もし型が Variable なら内部の値の型を取得する
			var type = property.PropertyType;
			if (source != null && typeof(Argument).IsAssignableFrom(type)) {
				var variable = source.FindArguments().FirstOrDefault();
				if (!(variable is null) && variable.Value != null) {
					type = variable.Value.GetType();
				}
			}

			// プロパティに列をバインド
			return BindColumn(property.Name, "c" + this.ColumnMap.Count, this.Owner.Environment.CreateDbTypeFromType(type), 0, source);
		}

		/// <summary>
		/// エイリアス用にクローンを作成する
		/// </summary>
		/// <returns>クローン</returns>
		public Select<TColumns> AliasedClone() {
			return this;
		}

		/// <summary>
		/// WHERE句のノードを登録する
		/// </summary>
		/// <param name="where">WHERE句ノード</param>
		[SqlMethod]
		public Select<TColumns> Where(IWhere where) {
			if (this.WhereNode != null) {
				throw new ApplicationException();
			}
			QueryNodeHelper.SwitchParent(where, this);
			this.WhereNode = where;
			return this;
		}

		/// <summary>
		/// WHERE句の式を登録する
		/// </summary>
		/// <param name="expression">式</param>
		[SqlMethod]
		public Where Where(Expression<Func<bool>> expression) {
			var where = new Where(this, expression);
			this.WhereNode = where;
			return where;
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

			if (this.WhereNode != null) {
				this.WhereNode.ToElementCode(context);
			}

			context.Add(this);
		}
		#endregion

		#region 非公開メソッド
		ITable<TColumns> ITable<TColumns>.AliasedClone() => this.AliasedClone();
		ITable ITable.AliasedClone() => this.AliasedClone();
		#endregion
	}
}
