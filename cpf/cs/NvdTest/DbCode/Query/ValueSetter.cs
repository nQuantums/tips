using System;
using System.Collections.Generic;
using System.Text;
using System.Linq.Expressions;
using DbCode.Internal;

namespace DbCode.Query {
	public class ValueSetter : ISelect {
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
		/// テーブルが直接保持する列定義の取得
		/// </summary>
		public ColumnMap ColumnMap { get; private set; } = new ColumnMap();

		/// <summary>
		/// WHERE句のノード
		/// </summary>
		public IWhere WhereNode { get; private set; }
		#endregion

		#region コンストラクタ
		[SqlMethod]
		public ValueSetter(IQueryNode parent, ColumnMap columns, ElementCode[] values) {
			if (columns.Count != values.Length) {
				throw new ApplicationException();
			}
			this.Parent = parent;
			for (int i = 0; i < values.Length; i++) {
				this.ColumnMap.Add(columns[i].Clone(values[i]));
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
		/// WHERE句のノードを登録する
		/// </summary>
		/// <param name="where">WHERE句ノード</param>
		[SqlMethod]
		public ValueSetter Where(IWhere where) {
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
		/// <param name="expression">WHEREの式</param>
		[SqlMethod]
		public void Where(Expression expression) {
			this.Where(new Where(this, expression));
		}

		public ITable AliasedClone() {
			return this;
		}

		public Column BindColumn(string propertyName, string name, IDbType dbType, ColumnFlags flags = 0, ElementCode source = null) {
			return null;
		}

		public void ToElementCode(ElementCode context) {
			// SELECT 部分作成
			int i = 0;
			context.Add(SqlKeyword.Select);
			context.AddColumns(this.ColumnMap, column => {
				context.Add(column.Source);
				context.Add(SqlKeyword.As);
				context.Concat("c" + (i++));
			});

			// WHERE 部分作成
			if(this.WhereNode != null) {
				this.WhereNode.ToElementCode(context);
			}
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
	}
}
