using System;
using System.Collections.Generic;
using System.Text;

namespace DbCode.Query {
	public class ValueSetter : ISelect {
		public ColumnMap ColumnMap { get; private set; } = new ColumnMap();

		public Sql Owner => this.Parent.Owner;

		public IQueryNode Parent { get; private set; }

		public IEnumerable<IQueryNode> Children => null;

		public IWhere WhereNode { get; private set; }

		public ValueSetter(IQueryNode parent, ColumnMap columns, ElementCode[] values) {
			if (columns.Count != values.Length) {
				throw new ApplicationException();
			}
			this.Parent = parent;
			for (int i = 0; i < values.Length; i++) {
				this.ColumnMap.Add(columns[i].Clone(values[i]));
			}
		}

		public void Where(ElementCode expression) {
			this.WhereNode = new Where(this, expression);
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
	}
}
