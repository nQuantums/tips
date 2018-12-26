using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace EntityGraph {
	[Flags]
	public enum EntityFlags {
		/// <summary>
		/// フィルタリングされたかどうか
		/// </summary>
		IsFiltered = 1 << 0,

		/// <summary>
		/// 削除され最初から存在しないものとして扱われる
		/// </summary>
		IsDeleted = 1 << 1,

		/// <summary>
		/// 非表示にされ関連が親に引き継がれる
		/// </summary>
		IsHidden = 1 << 2,
	}

	/// <summary>
	/// グラフの要素となる
	/// </summary>
	public class Entity {
		public EntityFlags Flags;

		/// <summary>
		/// 種類名
		/// </summary>
		public readonly string KindName;

		/// <summary>
		/// 表示名
		/// </summary>
		public string Name;

		/// <summary>
		/// 親<see cref="Entity"/>
		/// </summary>
		public Entity Parent;

		/// <summary>
		/// ノードとしてグラフ描画される際の形状名
		/// </summary>
		public string InitialShape;

		/// <summary>
		/// 形状描画の色
		/// </summary>
		public string InitialColor;

		/// <summary>
		/// フォント描画の色
		/// </summary>
		public string InitialFontColor;

		/// <summary>
		/// ルート<see cref="Entity"/>なら true になる
		/// </summary>
		public bool IsRoot {
			get {
				return this.Parent == null || string.IsNullOrEmpty(this.Name);
			}
		}

		/// <summary>
		/// コンストラクタ、種類名と表示名を指定して初期化する
		/// </summary>
		/// <param name="kindName">種類名</param>
		/// <param name="entityName">表示名</param>
		/// <remarks><see cref="Entity"/>型のフィールドが存在した場合自動的にインスタンスを生成し名称としてフィールドを使うように初期化される。</remarks>
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

		/// <summary>
		/// <see cref="Entity"/>型のフィールド一覧の取得
		/// </summary>
		public IEnumerable<FieldInfo> GetEntityFields() {
			foreach (var f in this.GetType().GetFields()) {
				if (f.FieldType.IsSubclassOf(typeof(Entity))) {
					yield return f;
				}
			}
		}

		/// <summary>
		/// 直接の子となる<see cref="Entity"/>一覧の取得
		/// </summary>
		public virtual IEnumerable<Entity> GetChild() {
			return GetEntityFields().Select(f => f.GetValue(this)).Select(e => e as Entity);
		}

		/// <summary>
		/// 直接の子と子孫となる<see cref="Entity"/>一覧の取得
		/// </summary>
		public virtual IEnumerable<Entity> GetChildAndDescendants() {
			foreach (var e in this.GetChild()) {
				yield return e;
				foreach (var e2 in e.GetChildAndDescendants()) {
					yield return e;
				}
			}
		}

		/// <summary>
		/// グラフ化作業途中の作業バッファを取得する
		/// </summary>
		public WorkBuffer GetWorkBuffer(GraphBuilder gb, bool createIfNotExists = true) {
			var workBuffers = gb.WorkBuffers;
			WorkBuffer wb;
			if (!workBuffers.TryGetValue(this, out wb) && createIfNotExists) {
				workBuffers[this] = wb = new WorkBuffer(workBuffers.Count);
			}
			return wb;
		}

		/// <summary>
		/// Graphviz 属性内の label 値の取得
		/// </summary>
		public virtual string GetGraphvizLabel(GraphBuilder gb) {
			var wb = GetWorkBuffer(gb);
			var ports = wb.Ports;
			if (ports != null) {
				var sb = new StringBuilder();
				sb.Append(@"<<table border=""0"" cellborder=""1"" cellspacing=""5"">");
				sb.Append(@"<tr><td><b><i>" + this.Name + "</i></b></td></tr>");
				foreach (var kvp in ports) {
					sb.Append(@"<tr><td port=""" + kvp.Value + @""">" + kvp.Key.Name + @"</td></tr>");
				}
				sb.Append("</table>>");
				return sb.ToString();
			} else {
				return '"' + this.Name + '"';
			}
		}

		/// <summary>
		/// Graphviz ノード属性内の shape 値の取得
		/// </summary>
		public virtual string GetGraphvizNodeShape(GraphBuilder gb) {
			return this.InitialShape;
		}

		/// <summary>
		/// Graphviz 属性内の color 値の取得
		/// </summary>
		public virtual string GetGraphvizColor(GraphBuilder gb) {
			return this.InitialColor;
		}

		/// <summary>
		/// Graphviz 属性内の fontcolor 値の取得
		/// </summary>
		public virtual string GetGraphvizFontColor(GraphBuilder gb) {
			return this.InitialFontColor;
		}

		/// <summary>
		/// Graphviz node 属性の取得
		/// </summary>
		public virtual string GetGraphvizNodeAttribute(GraphBuilder gb) {
			var sb = new StringBuilder();
			sb.Append("[");
			sb.Append("label=" + this.GetGraphvizLabel(gb));
			if (this.InitialShape != null) {
				sb.Append(" shape=" + this.GetGraphvizNodeShape(gb) + "");
			}
			if (this.InitialColor != null) {
				sb.Append(" color=\"" + this.GetGraphvizColor(gb) + "\"");
			}
			if (this.InitialFontColor != null) {
				sb.Append(" fontcolor=\"" + this.GetGraphvizFontColor(gb) + "\"");
			}
			sb.Append("]");
			return sb.ToString();
		}

		/// <summary>
		/// 指定の子<see cref="Entity"/>が自身の形状の一部とできるか判定する
		/// </summary>
		/// <param name="child">子<see cref="Entity"/></param>
		/// <returns>port にできるなら true</returns>
		public virtual bool IsPortable(Entity child) {
			return false;
		}

		/// <summary>
		/// 可能ならば子<see cref="Entity"/>を自形状の port にする
		/// </summary>
		/// <param name="gb">Graphviz グラフ構築用オブジェクト</param>
		/// <returns>port になった子<see cref="Entity"/>一覧、または null</returns>
		public Dictionary<Entity, string> MakePorts(GraphBuilder gb) {
			var wb = GetWorkBuffer(gb);
			var children = wb.Children;
			if (children != null) {
				if (children.Count == children.Where(c => IsPortable(c)).Count()) {
					var ports = new Dictionary<Entity, string>();
					foreach (var c in children) {
						var portName = ports.Count.ToString();
						ports.Add(c, portName);
						c.GetWorkBuffer(gb).PortName = portName;
					}
					return wb.Ports = ports;
				}
			}
			return null;
		}

		/// <summary>
		/// 自分と子<see cref="Entity"/>に Graphviz 名を付与する
		/// </summary>
		/// <param name="gb">Graphviz グラフ構築用オブジェクト</param>
		/// <param name="parentGraphvizName">親<see cref="Entiy"/>の Graphviz 名</param>
		public void AssignGraphvizName(GraphBuilder gb, string parentGraphvizName) {
			var wb = GetWorkBuffer(gb);
			var portName = wb.PortName;
			var children = wb.Children;

			string name;
			if (portName != null) {
				name = parentGraphvizName + ":" + portName;
			} else if (children == null) {
				name = "node" + wb.Id;
			} else {
				name = "cluster" + wb.Id;
			}
			wb.GraphvizName = name;

			if (children != null) {
				foreach (var c in children) {
					c.AssignGraphvizName(gb, name);
				}
			}
		}

		/// <summary>
		/// 自分と子<see cref="Entity"/>の Graphviz 定義を取得する
		/// </summary>
		/// <param name="gb">Graphviz グラフ構築用オブジェクト</param>
		/// <param name="sb">定義書き込み先</param>
		public virtual void GraphvizDefinition(GraphBuilder gb, StringBuilder sb) {
			gb.DefinedEntities.Add(this);

			var wb = GetWorkBuffer(gb);
			if (wb.PortName != null) {
				return; // 親形状の一部となっているため自身の定義は存在しない
			}

			var name = wb.GraphvizName;
			var isSubgraph = false;
			if (name.StartsWith("node")) {
				sb.AppendLine(name + GetGraphvizNodeAttribute(gb));
			} else {
				sb.AppendLine("subgraph " + name + "{");
				sb.AppendLine("label=\"" + this.Name + "\";");
				isSubgraph = true;
			}

			var children = wb.Children;
			if (children != null) {
				foreach (var c in children) {
					c.GraphvizDefinition(gb, sb);
				}
			}

			if (isSubgraph) {
				var definedFlows = gb.DefinedFlows;
				foreach (var kvp in gb.FlowBuffer.Flows) {
					if (kvp.Key.IsContainedBy(children)) {
						kvp.Value.GraphvizDefinition(gb, sb);
					}
				}

				sb.AppendLine("}");
			}
		}
	}

	/// <summary>
	/// ２つの<see cref="Entity"/>が関連していることを示す
	/// </summary>
	public class Flow {
		/// <summary>
		/// 最初の<see cref="Entiy"/>
		/// </summary>
		public Entity E1;

		/// <summary>
		/// 次の<see cref="Entiy"/>
		/// </summary>
		public Entity E2;

		public Flow(Entity from, Entity to) {
			this.E1 = from;
			this.E2 = to;
		}

		public bool IsSameDir(Flow relation) {
			return this.E1 == relation.E1 && this.E2 == relation.E2;
		}

		public bool IsContainedBy(HashSet<Entity> entities) {
			return entities.Contains(this.E1) && entities.Contains(this.E2);
		}

		public Entity GetPair(Entity entity) {
			if (this.E1 == entity) {
				return this.E2;
			} else if (this.E2 == entity) {
				return this.E1;
			} else {
				return null;
			}
		}

		public override int GetHashCode() {
			return this.E1.GetHashCode() ^ this.E2.GetHashCode();
		}

		public override bool Equals(object obj) {
			var c = obj as Flow;
			if (!(c is null)) {
				return this == c;
			}
			return base.Equals(obj);
		}

		public static bool operator ==(Flow l, Flow r) {
			if (l.E1 == r.E1 && l.E2 == r.E2) {
				return true;
			}
			if (l.E1 == r.E2 && l.E2 == r.E1) {
				return true;
			}
			return false;
		}
		public static bool operator !=(Flow l, Flow r) {
			return !(l == r);
		}
	}

	/// <summary>
	/// <see cref="Flow"/>の関連の方向
	/// </summary>
	[Flags]
	public enum FlowDirection {
		Unknown,
		Forward = 1,
		Backward = 2,
		Both = 3,
	}

	[Flags]
	public enum FlowAtrFlags {
		/// <summary>
		/// 削除され最初から存在しないものとして扱われる
		/// </summary>
		IsDeleted = 1 << 0,

		/// <summary>
		/// 非表示にされ関連が親<
		/// </summary>
		IsHidden = 1 << 1,
		AutoGenerated = 1 << 2,
	}

	/// <summary>
	/// <see cref="Flow"/>に付随する属性
	/// </summary>
	public class FlowAtr {
		public Flow Flow;
		public FlowDirection Direction;

		public FlowAtr(Flow relation) {
			this.Flow = relation;
		}

		public void GraphvizDefinition(GraphBuilder gb, StringBuilder sb) {
			var flow = this.Flow;
			gb.DefinedFlows.Add(flow);

			var n1 = flow.E1.GetWorkBuffer(gb).GraphvizName;
			var n2 = flow.E2.GetWorkBuffer(gb).GraphvizName;
			switch (this.Direction) {
			case FlowDirection.Forward:
				sb.AppendLine(n1 + " -> " + n2 + ";");
				break;
			case FlowDirection.Backward:
				sb.AppendLine(n2 + " -> " + n1 + ";");
				break;
			case FlowDirection.Both:
				sb.AppendLine(n1 + " -> " + n2 + ";");
				sb.AppendLine(n2 + " -> " + n1 + ";");
				break;
			}
		}
	}

	public class FlowBuffer {
		public Dictionary<Flow, FlowAtr> Flows = new Dictionary<Flow, FlowAtr>();

		public FlowBuffer() {
		}

		public FlowBuffer(Dictionary<Flow, FlowAtr> flowsToAdd) {
			var flows = this.Flows;
			foreach (var kvp in flowsToAdd) {
				flows[kvp.Key] = kvp.Value;
			}
		}

		public Flow Flow(Entity from, Entity to) {
			var flow = new Flow(from, to);

			FlowAtr ra;
			if (!this.Flows.TryGetValue(flow, out ra)) {
				this.Flows[flow] = ra = new FlowAtr(flow);
			}

			if (flow.IsSameDir(ra.Flow)) {
				ra.Direction |= FlowDirection.Forward;
			} else {
				ra.Direction |= FlowDirection.Backward;
			}

			return flow;
		}

		public void Flow(Entity e1, Entity e2, Entity e3, params Entity[] entities) {
			this.Flow(e1, e2);
			this.Flow(e2, e3);
			var last = e3;
			foreach (var e in entities) {
				this.Flow(last, e);
				last = e;
			}
		}

		public Dictionary<Entity, HashSet<Flow>> GetEntities() {
			var entities = new Dictionary<Entity, HashSet<Flow>>();
			foreach (var kvp in this.Flows) {
				var flow = kvp.Key;
				var e1 = flow.E1;
				var e2 = flow.E2;

				HashSet<Flow> f;
				if (!entities.TryGetValue(e1, out f)) {
					entities[e1] = f = new HashSet<Flow>();
				}
				f.Add(flow);
				if (!entities.TryGetValue(e2, out f)) {
					entities[e2] = f = new HashSet<Flow>();
				}
				f.Add(flow);
			}
			return entities;
		}
	}

	/// <summary>
	/// <see cref="Entity"/>のグラフ化作業時に使用されるバッファ
	/// </summary>
	public class WorkBuffer {
		/// <summary>
		/// 属性ID
		/// </summary>
		public int Id;

		/// <summary>
		/// Graphviz での要素名
		/// </summary>
		public string GraphvizName;

		/// <summary>
		/// 親<see cref="Entity"/>形状の port とされたらポート名が設定される
		/// </summary>
		public string PortName;

		/// <summary>
		/// 自形状の port とする<see cref="Entity"/>一覧
		/// </summary>
		public Dictionary<Entity, string> Ports;

		/// <summary>
		/// 関連する<see cref="Flow"/>一覧
		/// </summary>
		public HashSet<Flow> Flows;

		/// <summary>
		/// 子<see cref="Entity"/>一覧
		/// </summary>
		public HashSet<Entity> Children;

		public WorkBuffer(int id) {
			this.Id = id;
		}
	}

	/// <summary>
	/// グラフ構築用
	/// </summary>
	public class GraphBuilder {
		/// <summary>
		/// 全ての関連一覧
		/// </summary>
		public FlowBuffer FlowBuffer;

		/// <summary>
		/// <see cref="Entity"/>をグラフ化する際に使用される作業用属性
		/// </summary>
		public Dictionary<Entity, WorkBuffer> WorkBuffers = new Dictionary<Entity, WorkBuffer>();

		/// <summary>
		/// 子<see cref="Entity"/>をキーとした親<see cref="Entity"/>
		/// </summary>
		public Dictionary<Entity, Entity> ChildToParent;

		/// <summary>
		/// <see cref="GenerateDefinition"/>にて定義済みの<see cref="Entity"/>一覧
		/// </summary>
		public HashSet<Entity> DefinedEntities = new HashSet<Entity>();

		/// <summary>
		/// <see cref="GenerateDefinition"/>にて定義済みの<see cref="Flow"/>一覧
		/// </summary>
		public HashSet<Flow> DefinedFlows = new HashSet<Flow>();


		/// <summary>
		/// コンストラクタ、<see cref="Entity"/>間の関連を指定して初期化する
		/// </summary>
		/// <param name="flowsToAdd"><see cref="Entity"/>間の関連</param>
		public GraphBuilder(Dictionary<Flow, FlowAtr> flowsToAdd) {
			this.FlowBuffer = new FlowBuffer(flowsToAdd);
		}

		/// <summary>
		/// フィルタリングを行い要求される抽象度の関連性のみ残す
		/// </summary>
		/// <param name="filter">フィルタ処理デリゲート、<see cref="Entity.Flags"/>にフラグをセットする</param>
		public void Filter(Action<Entity> filter) {
			var fb = this.FlowBuffer;
			var flows = fb.Flows;

			for (; ; ) {
				// フィルタリングを行い要求される抽象度の関連性のみ残す
				var entities = fb.GetEntities();
				var filtered = false;
				foreach (var kvp in entities) {
					var e = kvp.Key;
					if ((e.Flags & EntityFlags.IsFiltered) != 0) {
						continue;
					}

					// フィルタリングにてフラグをセットする
					if (filter != null) {
						filter(e);
					}
					e.Flags |= EntityFlags.IsFiltered;
					filtered = true;

					// セットされたフラグに応じて関連性の削除または切り替えを行う
					var flags = e.Flags;
					if ((flags & (EntityFlags.IsDeleted | EntityFlags.IsHidden)) != 0) {
						var p = e.Parent;
						var moveToParent = (flags & EntityFlags.IsHidden) != 0 && p != null;
						foreach (var flow in kvp.Value) {
							flows.Remove(flow);
							if (moveToParent) {
								var pair = flow.GetPair(p);
								if (pair != null) {
									fb.Flow(p, pair);
								}
							}
						}
					}
				}
				if (!filtered) {
					var attributes = this.WorkBuffers;
					foreach (var kvp in entities) {
						kvp.Key.GetWorkBuffer(this);
					}
					return;
				}
			}
		}

		/// <summary>
		/// <see cref="Entity"/>の親子関係を生成する
		/// </summary>
		public void SetParentRelationship() {
			var childToParent = new Dictionary<Entity, Entity>();

			Action<Entity> makeParentRelationship = null;
			makeParentRelationship = child => {
				var parent = child.Parent;
				if (parent != null) {
					var wb = parent.GetWorkBuffer(this);
					var children = wb.Children;
					if (children == null) {
						wb.Children = children = new HashSet<Entity>();
					}
					children.Add(child);
					childToParent[child] = parent;

					makeParentRelationship(parent);
				}
			};
			foreach (var e in this.WorkBuffers.Keys.ToArray()) {
				makeParentRelationship(e);
			}

			this.ChildToParent = childToParent;
		}

		/// <summary>
		/// <see cref="Entity"/>を node 名に変換するテーブルを生成する
		/// </summary>
		public void SetGraphvizName() {
			foreach (var e in this.WorkBuffers.Keys) {
				if (e.IsRoot) {
					e.AssignGraphvizName(this, null);
				}
			}
		}

		public void GenerateDefinition(StringBuilder sb) {
			foreach (var e in this.WorkBuffers.Keys) {
				if (e.IsRoot) {
					e.GraphvizDefinition(this, sb);
				}
			}
		}
	}

	public class Graph {
		public FlowBuffer FlowBuffer = new FlowBuffer();
		public List<Task> Tasks = new List<Task>();

		public Flow Flow(Entity from, Entity to) {
			return this.FlowBuffer.Flow(from, to);
		}

		public void Flow(Entity e1, Entity e2, Entity e3, params Entity[] entities) {
			this.FlowBuffer.Flow(e1, e2, e3, entities);
		}

		public Task Task(string name) {
			var t = new Task { Name = name, FlowBuffer = this.FlowBuffer };
			this.Tasks.Add(t);
			return t;
		}

		public string GetDotCode(Action<Entity> filter = null) {
			var sb = new StringBuilder();

			sb.AppendLine(@"digraph {
	node [color=""#5050e5"" fontcolor=""#5050e5""];
	edge [fontsize=11 color=gray50 fontcolor=gray50];
");

			var gb = new GraphBuilder(this.FlowBuffer.Flows);
			gb.Filter(filter);
			gb.SetParentRelationship();
			gb.SetGraphvizName();
			gb.GenerateDefinition(sb);

			var definedFlows = gb.DefinedFlows;
			foreach (var kvp in gb.FlowBuffer.Flows.Where(kvp => !definedFlows.Contains(kvp.Key)).ToList()) {
				kvp.Value.GraphvizDefinition(gb, sb);
			}

			sb.AppendLine("}");

			return sb.ToString();
		}
	}

	public class Group : Entity {
		public Group() : base("Group") {
		}
	}

	public class Task : Entity {
		public FlowBuffer FlowBuffer;

		public Task() : base("Task") {
		}

		public Flow Flow(Entity from, Entity to) {
			return this.FlowBuffer.Flow(from, to);
		}

		public void Flow(Entity e1, Entity e2, Entity e3, params Entity[] entities) {
			this.FlowBuffer.Flow(e1, e2, e3, entities);
		}
	}

	public class Man : Entity {
		public Man() : base("人") {
			this.InitialShape = "star";
		}
	}

	public class WebSite : Entity {
		public string Url;

		public WebSite(string url) : base("WebSite") {
			this.Url = url;
			this.InitialShape = "egg";
			this.InitialColor = "#40b9e5";
			this.InitialFontColor = "#40b9e5";
		}
	}

	public class PC : Entity {
		public PC() : base("PC") {
		}
	}

	public class App : Entity {
		public App() : base("App") {
			this.InitialShape = "doubleoctagon";
			this.InitialColor = "#ff7040";
			this.InitialFontColor = "#ff7040";
		}
	}

	public class Data : Entity {
		public Data(string kindName, string entityName = null) : base(kindName, entityName) {
			this.InitialShape = "note";
			this.InitialColor = "#10a559";
			this.InitialFontColor = "#10a559";
		}
	}

	public class DataFile : Data {
		public DataFile() : base("DataFile") {
		}
	}

	public class Db : Data {
		public Db() : base("DataBase") {
			this.InitialShape = "box3d";
		}

		public override bool IsPortable(Entity child) {
			return child is Tbl;
		}
	}

	public class Tbl : Entity {
		public Tbl() : base("Table") {
			this.InitialShape = "house";
		}

		public override bool IsPortable(Entity child) {
			return child is Col;
		}
	}

	public class Csv : Data {
		public Csv() : base("Csv") {
		}
	}

	public class Zip : Data {
		public Zip() : base("Zip") {
		}
	}

	public class Registry : Data {
		public Registry() : base("Registry") {
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
