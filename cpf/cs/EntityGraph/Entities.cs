using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace EntityGraph {
	public class Entity {
		public readonly string KindName;
		public string Name;
		public Entity Parent;

		public virtual string DotShape {
			get {
				return "box";
			}
		}

		public Entity(string kindName, string entityName = null) {
			this.KindName = kindName;
			this.Name = entityName;

			foreach (var f in GetEntityFields()) {
				var ft = f.FieldType;
				var e = f.GetValue(this) as Entity;
				if (e == null) {
					if (ft.GetConstructor(new Type[0]) != null) {
						e = Activator.CreateInstance(ft, new object[0]) as Entity;
					} else {
						throw new ApplicationException(ft + " のコンストラクタが未対応の形式です。");
					}
					f.SetValue(this, e);
				}
				e.Parent = this;
				if (e.Name == null) {
					e.Name = f.Name;
				}
			}
		}

		public override string ToString() {
			return this.Name;
		}

		public IEnumerable<FieldInfo> GetEntityFields() {
			foreach (var f in this.GetType().GetFields()) {
				if (f.FieldType.IsSubclassOf(typeof(Entity))) {
					yield return f;
				}
			}
		}

		public IEnumerable<Entity> GetChildEntities() {
			return GetEntityFields().Select(f => f.GetValue(this)).Cast<Entity>();
		}
	}

	public class Rel {
		public Entity E1;
		public Entity E2;

		public Rel(Entity from, Entity to) {
			this.E1 = from;
			this.E2 = to;
		}

		public bool IsSameDir(Rel relation) {
			return this.E1 == relation.E1 && this.E2 == relation.E2;
		}

		public override int GetHashCode() {
			return this.E1.GetHashCode() ^ this.E2.GetHashCode();
		}

		public override bool Equals(object obj) {
			var c = obj as Rel;
			if (!(c is null)) {
				return this == c;
			}
			return base.Equals(obj);
		}

		public static bool operator ==(Rel l, Rel r) {
			if (l.E1 == r.E1 && l.E2 == r.E2) {
				return true;
			}
			if (l.E1 == r.E2 && l.E2 == r.E1) {
				return true;
			}
			return false;
		}
		public static bool operator !=(Rel l, Rel r) {
			return !(l == r);
		}
	}

	[Flags]
	public enum FlowDirection {
		Unknown,
		Forward = 1,
		Backward = 2,
		Both = 3,
	}

	public class RelAtr {
		public Rel Rel;
		public FlowDirection Direction;

		public RelAtr(Rel relation) {
			this.Rel = relation;
		}
	}

	public class Env {
		public Dictionary<Rel, RelAtr> Rels = new Dictionary<Rel, RelAtr>();

		public Rel Rel(Entity from, Entity to) {
			var r = new Rel(from, to);

			RelAtr ra;
			if (!this.Rels.TryGetValue(r, out ra)) {
				this.Rels[r] = ra = new RelAtr(r);
			}

			if (r.IsSameDir(ra.Rel)) {
				ra.Direction |= FlowDirection.Forward;
			} else {
				ra.Direction |= FlowDirection.Backward;
			}

			return r;
		}

		public void Rel(Entity e1, Entity e2, Entity e3, params Entity[] entities) {
			this.Rel(e1, e2);
			this.Rel(e2, e3);
			var last = e3;
			foreach (var e in entities) {
				this.Rel(last, e);
				last = e;
			}
		}

		public string GetDotCode() {
			var sb = new StringBuilder();

			sb.AppendLine(@"digraph {
  graph [
    charset = ""UTF-8"";
    label = ""sample graph"",
    labelloc = ""t"",
    labeljust = ""c"",
    bgcolor = ""#343434"",
    fontcolor = white,
    fontsize = 18,
    style = ""filled"",
    rankdir = TB,
    margin = 0.2,
    splines = spline,
    ranksep = 1.0,
    nodesep = 0.9
  ];

  node [
    colorscheme = ""rdylgn11""
    style = ""solid,filled"",
    fontsize = 16,
    fontcolor = 6,
    fontname = ""Migu 1M"",
    color = 7,
    fillcolor = 11,
    fixedsize = true,
    height = 0.6,
    width = 1.2
  ];

  edge [
    style = solid,
    fontsize = 14,
    fontcolor = white,
    fontname = ""Migu 1M"",
    color = white,
    labelfloat = true,
    labeldistance = 2.5,
    labelangle = 70
  ];
");

			var entityToNode = new Dictionary<Entity, string>();
			Action<Entity> addNode = e => {
				string node;
				if (!entityToNode.TryGetValue(e, out node)) {
					entityToNode[e] = "node" + (entityToNode.Count + 1);
				}
			};
			foreach (var ratr in this.Rels.Values) {
				addNode(ratr.Rel.E1);
				addNode(ratr.Rel.E2);
			}

			foreach (var e in entityToNode) {
				sb.AppendLine(e.Value + " [label=\"" + e.Key.Name + "\" shape=" + e.Key.DotShape + "]");
			}

			foreach (var ratr in this.Rels.Values) {
				var r = ratr.Rel;
				var n1 = entityToNode[r.E1];
				var n2 = entityToNode[r.E2];

				switch (ratr.Direction) {
				case FlowDirection.Forward:
					sb.AppendLine(n1 + " -> " + n2);
					break;
				case FlowDirection.Backward:
					sb.AppendLine(n2 + " -> " + n1);
					break;
				case FlowDirection.Both:
					sb.AppendLine(n1 + " -> " + n2);
					sb.AppendLine(n2 + " -> " + n1);
					break;
				}
			}

			sb.AppendLine("}");

			return sb.ToString();
		}
	}

	public class Man : Entity {
		public Man() : base("人") {
		}
		public override string DotShape {
			get {
				return "star";
			}
		}
	}

	public class Task : Entity {
		public Task() : base("Task") {
		}
		public override string DotShape {
			get {
				return "polygon";
			}
		}
	}

	public class WebSite : Entity {
		public string Url;

		public WebSite(string url) : base("WebSite") {
			this.Url = url;
		}
		public override string DotShape {
			get {
				return "cds";
			}
		}
	}

	public class PC : Entity {
		public PC() : base("PC") {
		}
	}

	public class App : Entity {
		public App() : base("App") {
		}
	}

	public class DataFile : Entity {
		public DataFile() : base("DataFile") {
		}
		public override string DotShape {
			get {
				return "note";
			}
		}
	}

	public class Db : Entity {
		public Db() : base("DataBase") {
		}
		public override string DotShape {
			get {
				return "box3d";
			}
		}
	}

	public class Tbl : Entity {
		public Tbl() : base("Table") {
		}
	}

	public class Csv : Entity {
		public Csv() : base("Csv") {
		}
		public override string DotShape {
			get {
				return "note";
			}
		}
	}

	public class Col : Entity {
		public Type ValueType;
		public bool IsPrimaryKey;

		public Col() : base("Column") {
		}
	}

	public class Col<T> : Col {
		public Col() : base() {
			this.ValueType = typeof(T);
		}
	}

	public class ColP<T> : Col<T> {
		public ColP() : base() {
			this.IsPrimaryKey = true;
		}
	}
}
