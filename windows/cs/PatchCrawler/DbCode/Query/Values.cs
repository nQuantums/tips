using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DbCode.Internal;

namespace DbCode.Query {
	public class ListAsValues<TColumns> : ISelect<TColumns> {
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
		public IEnumerable<IQueryNode> Children => null;

		/// <summary>
		/// 列をプロパティとして持つオブジェクト
		/// </summary>
		public TColumns Columns { get; private set; }

		/// <summary>
		/// 列をプロパティとして持つオブジェクト
		/// </summary>
		public TColumns _ => this.Columns;

		/// <summary>
		/// VALUES に置き換わるリスト
		/// </summary>
		public List<TColumns> ValueList { get; private set; }

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
		/// <param name="columnsExpression">プロパティが列指定として扱われるクラスを生成する () => new { Name = "test", ID = 1 } の様な式</param>
		[SqlMethod]
		public ListAsValues(IQueryNode parent, Expression<Func<TColumns>> columnsExpression) {
			this.Parent = parent;

			this.ColumnMap = new ColumnMap();
			this.Columns = TypewiseCache<TColumns>.Creator();
			this.ValueList = new List<TColumns>();

			// new 演算子でクラスを生成するもの以外はエラーとする
			var body = columnsExpression.Body;
			if (body.NodeType != ExpressionType.New) {
				throw new ApplicationException();
			}

			// プロパティと列定義を結びつける
			var properties = typeof(TColumns).GetProperties();
			var environment = this.Owner.Environment;
			for (int i = 0; i < properties.Length; i++) {
				BindColumn(properties[i], null);
			}
		}
		#endregion

		#region 公開メソッド
		/// <summary>
		/// 指定ノードを子とする、既存の親は<see cref="RemoveChild(IQueryNode)"/>で切り離す必要がある
		/// </summary>
		/// <param name="child">子ノード</param>
		public void AddChild(IQueryNode child) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// 指定の子ノードを取り除く
		/// </summary>
		/// <param name="child">子ノード</param>
		public void RemoveChild(IQueryNode child) {
			throw new NotImplementedException();
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
		public ListAsValues<TColumns> AliasedClone() {
			return this;
		}

		/// <summary>
		/// SQL文を生成する
		/// </summary>
		/// <param name="context">生成先のコンテキスト</param>
		public void ToElementCode(ElementCode context) {
			var delayedCode = new DelayedCodeGenerator((wb) => {
				var ec = new ElementCode();
				ec.BeginParenthesize();
				ec.Add(SqlKeyword.Values);
				var list = this.ValueList;
				for (int i = 0, n = list.Count; i < n; i++) {
					if (i != 0) {
						ec.AddComma();
					}
					ec.BeginParenthesize();
					ec.AddValues<TColumns>(list[i]);
					ec.EndParenthesize();
				}
				ec.EndParenthesize();
				ec.Add(this);
				ec.BeginParenthesize();
				ec.AddColumns(this.ColumnMap, column => ec.Concat(column.Name));
				ec.EndParenthesize();
				ec.Build(wb);
			});

			context.Add(delayedCode);
			context.RegisterBuildHandler(this, (item, wb) => {
				// delayedCode 内でエイリアス名付与するので、既存の処理をオーバーライドし何もしないようにする
			});
		}

		public override string ToString() {
			try {
				var ec = new ElementCode();
				this.ToElementCode(ec);
				return ec.ToString();
			} catch {
				return "";
			}
		}
		#endregion

		#region 非公開メソッド
		ITable<TColumns> ITable<TColumns>.AliasedClone() => this.AliasedClone();
		ITable ITable.AliasedClone() => this.AliasedClone();
		#endregion
	}
}
