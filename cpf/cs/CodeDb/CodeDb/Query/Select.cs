using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CodeDb.Internal;

namespace CodeDb.Query {
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

		/// <summary>
		/// <see cref="ICodeDbDataReader"/>から<see cref="TColumns"/>を列挙するファンクション
		/// </summary>
		public Func<ICodeDbDataReader, IEnumerable<TColumns>> Reader => TypeWiseCache<TColumns>.Reader;
		#endregion

		#region コンストラクタ
		/// <summary>
		/// コンストラクタ、親ノードと列プロパティと設定する値の式を指定して初期化する
		/// </summary>
		/// <param name="parent">親ノード</param>
		/// <param name="properties">列に対応するプロパティ</param>
		/// <param name="values">列の値の式</param>
		public Select(IQueryNode parent, PropertyInfo[] properties, ElementCode[] values) {
			if (properties.Length != values.Length) {
				throw new ApplicationException();
			}

			this.Parent = parent;
			this.ColumnMap = new ColumnMap();
			this.Columns = TypeWiseCache<TColumns>.Creator();

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
		public Select(IQueryNode parent, PropertyInfo[] properties, Expression[] values) {
			if (properties.Length != values.Length) {
				throw new ApplicationException();
			}

			this.Parent = parent;
			this.ColumnMap = new ColumnMap();
			this.Columns = TypeWiseCache<TColumns>.Creator();

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
		public Select(IQueryNode parent, Expression<Func<TColumns>> columnsExpression) {
			this.Parent = parent;

			this.ColumnMap = new ColumnMap();
			this.Columns = TypeWiseCache<TColumns>.Creator();

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
			if (source != null && typeof(Variable).IsAssignableFrom(type)) {
				var variable = source.FindVariables().FirstOrDefault();
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
		/// WHERE句の式を登録する
		/// </summary>
		/// <param name="expression">式</param>
		public Where Where(Expression<Func<bool>> expression) {
			var where = new Where(this, expression);
			this.WhereNode = where;
			return where;
		}

		/// <summary>
		/// WHERE句の式を登録する
		/// </summary>
		/// <param name="expression">式</param>
		public Where Where(ElementCode expression) {
			var where = new Where(this, expression);
			this.WhereNode = where;
			return where;
		}

		/// <summary>
		/// WHERE句のノードを新規作成する
		/// </summary>
		public Where Where() {
			var where = new Where(this);
			this.WhereNode = where;
			return where;
		}

		/// <summary>
		/// SQL文を生成する
		/// </summary>
		/// <param name="context">生成先のコンテキスト</param>
		public void BuildSql(ElementCode context) {
			int i = 0;
			context.Add(SqlKeyword.Select);
			context.AddColumns(this.ColumnMap, column => {
				context.Add(column.Source);
				context.Add(SqlKeyword.As);
				context.Concat("c" + (i++));
			});

			if (this.WhereNode != null) {
				this.WhereNode.BuildSql(context);
			}
		}
		#endregion

		#region 非公開メソッド
		ITable<TColumns> ITable<TColumns>.AliasedClone() => this.AliasedClone();
		ITable ITable.AliasedClone() => this.AliasedClone();
		#endregion
	}
}
