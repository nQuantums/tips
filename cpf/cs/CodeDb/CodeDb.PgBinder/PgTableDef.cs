using System;
using System.Collections.Generic;
using System.Text;
using CodeDb;

namespace CodeDb.PgBinder {
	public class PgTableDef : ITableDef {
		public string Name { get; private set; }
		IEnumerable<IColumnDef> ITableDef.ColumnDefs => this.ColumnDefs;
		public IPrimaryKeyDef PrimaryKey { get; set; }
		public IIndexDef[] Indices { get; set; }
		public IUniqueDef[] Uniques { get; set; }

		public List<PgColumnDef> ColumnDefs { get; private set; } = new List<PgColumnDef>();

		public PgTableDef(string name) {
			this.Name = name;
		}

		public IPrimaryKeyDef GetPrimaryKey() => this.PrimaryKey;
		public IEnumerable<IIndexDef> GetIndices() => this.Indices;
		public IEnumerable<IUniqueDef> GetUniques() => this.Uniques;
	}
}
