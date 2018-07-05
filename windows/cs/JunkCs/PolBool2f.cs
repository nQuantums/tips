#define POLBOOL_LOG_TIME // GlobalLogger を使い時間計測を行う
#define INTERSECT_SELF // ポリゴンの自己交差を許可する
using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using element = System.Single;
using vectori = Jk.Vector2i;
using vector = Jk.Vector2f;
using range = Jk.Range2f;
using volume = Jk.Obb2f;
using geo = Jk.Geo2f;
using polbool = Jk.PolBool2f;

namespace Jk {
	/// <summary>
	/// ポリゴンのブーリアン演算を行うクラス
	/// </summary>
	public class PolBool2f : IJsonable {
		#region クラス
		/// <summary>
		/// エッジフィルタ
		/// </summary>
		/// <param name="pb">ポリゴンブーリアン演算オブジェクト</param>
		/// <param name="edge">エッジ</param>
		/// <param name="right">右側をチェックするかどうか</param>
		/// <returns>無視するなら true、残すなら false</returns>
		public delegate bool EdgeFilter(PolBool2f pb, Edge edge, bool right);

		/// <summary>
		/// <see cref="PolBool2f"/>内で発生する例外
		/// </summary>
		public class Exception : System.ApplicationException {
			public Exception(string message)
				: base(message) {
			}
		}

		/// <summary>
		/// ポリゴンの頂点
		/// </summary>
		public struct Vertex : IJsonable {
			/// <summary>
			/// 位置
			/// </summary>
			public vector Position;

			/// <summary>
			/// ユーザーデータ
			/// </summary>
			public object UserData;

			/// <summary>
			/// コンストラクタ、位置を指定して初期化する
			/// </summary>
			/// <param name="position">位置</param>
			public Vertex(vector position) {
				this.Position = position;
				this.UserData = null;
			}

			/// <summary>
			/// コンストラクタ、位置とユーザーデータを指定して初期化する
			/// </summary>
			/// <param name="position">位置</param>
			/// <param name="userData">ユーザーデータ</param>
			public Vertex(vector position, object userData) {
				this.Position = position;
				this.UserData = userData;
			}

			/// <summary>
			/// コンストラクタ、指定ノードの情報で初期化する
			/// </summary>
			/// <param name="node">ノード</param>
			public Vertex(Node node) {
				this.Position = node.Position;
				this.UserData = null;
			}

			public override string ToString() {
				return Jsonable.Fields(nameof(this.Position), this.Position, nameof(this.UserData), this.UserData);
			}

			public string ToStringForDebug() {
				return polbool.ToString(this.Position);
			}

			public string ToJsonString() {
				return this.ToString();
			}
		}

		/// <summary>
		/// <see cref="Vertex"/>によるループデータ
		/// </summary>
		public class VLoop : IJsonable {
			public range _Volume = range.InvalidValue;

			/// <summary>
			/// 頂点配列
			/// </summary>
			public FList<Vertex> Vertices;

			/// <summary>
			/// エッジのユーザーデータ配列、null ならエッジユーザーデータ無し
			/// </summary>
			public FList<object> EdgesUserData;

			/// <summary>
			/// このループに紐づくユーザーデータ
			/// </summary>
			public object UserData;

			/// <summary>
			/// ユーザーが自由に設定＆使用できる値
			/// </summary>
			public ulong UserValue;

			/// <summary>
			/// 面積
			/// </summary>
			public element Area;

			/// <summary>
			/// 時計回りかどうか
			/// </summary>
			public bool CW;

			/// <summary>
			/// 境界ボリューム
			/// </summary>
			public range Volume {
				get {
					if (!_Volume.IsValid && this.Vertices != null) {
						var verticesCore = this.Vertices.Core;
						var vol = range.InvalidValue;
						for (int i = verticesCore.Count - 1; i != -1; i--) {
							vol.MergeSelf(verticesCore.Items[i].Position);
						}
						_Volume = vol;
					}
					return _Volume;
				}
				set {
					_Volume = value;
				}
			}

			/// <summary>
			/// コンストラクタ
			/// </summary>
			public VLoop() : this(new FList<Vertex>(), null) {
			}

			/// <summary>
			/// コンストラクタ、頂点リスト、エッジユーザーデータリストを渡して初期化する
			/// </summary>
			/// <param name="vertices">頂点リスト</param>
			/// <param name="edgesUserData">エッジユーザーデータリスト</param>
			public VLoop(FList<Vertex> vertices, FList<object> edgesUserData) {
				this.Vertices = vertices;
				this.EdgesUserData = edgesUserData;
			}

			/// <summary>
			/// コンストラクタ、頂点コレクション、エッジユーザーデータコレクションを渡して初期化する
			/// </summary>
			/// <param name="vertices">頂点コレクション</param>
			/// <param name="edgesUserData">エッジユーザーデータコレクション</param>
			public VLoop(IEnumerable<Vertex> vertices, IEnumerable<object> edgesUserData) {
				this.Vertices = new FList<Vertex>(vertices);
				this.EdgesUserData = new FList<object>(edgesUserData);
			}

			/// <summary>
			/// 平行移動する
			/// </summary>
			/// <param name="offset">移動量</param>
			public void Offset(vector offset) {
				var verticesCore = this.Vertices.Core;
				for (int i = verticesCore.Count - 1; i != -1; i--) {
					verticesCore.Items[i].Position += offset;
				}
			}

			/// <summary>
			/// 複製を作成
			/// </summary>
			/// <returns>複製</returns>
			public VLoop Clone() {
				var c = this.MemberwiseClone() as VLoop;
				if (c.Vertices != null)
					c.Vertices = new FList<Vertex>(c.Vertices);
				if (c.EdgesUserData != null)
					c.EdgesUserData = new FList<object>(c.EdgesUserData);
				return c;
			}

			public override string ToString() {
				if (InRecursiveCall(this)) {
					return Jsonable.Fields(nameof(this.Area), this.Area, nameof(this.CW), this.CW, nameof(this.Volume), this.Volume, nameof(this.UserValue), this.UserValue);
				} else {
					try {
						EnterRecursiveCall(this);
						return Jsonable.Fields(nameof(this.Area), this.Area, nameof(this.CW), this.CW, nameof(this.Volume), this.Volume, nameof(this.UserData), this.UserData, nameof(this.UserValue), this.UserValue, nameof(this.Vertices), this.Vertices, nameof(this.EdgesUserData), this.EdgesUserData);
					} finally {
						LeaveRecursiveCall(this);
					}
				}
			}

			public string ToJsonString() {
				return this.ToString();
			}
		}

		/// <summary>
		/// ポリゴン情報、頂点は３つ以上で並びは時計回り且つ自己交差してはならない
		/// </summary>
		public class Polygon : IJsonable {
			private int _VertexCount = -1;
			public range _Volume = range.InvalidValue;

			/// <summary>
			/// ループ配列、添字[0:外枠、1...:穴]
			/// </summary>
			public FList<VLoop> Loops;

			/// <summary>
			/// このポリゴンに紐づくユーザーデータ
			/// </summary>
			public object UserData;

			/// <summary>
			/// ユーザーが自由に設定＆使用できる値
			/// </summary>
			public ulong UserValue;

			/// <summary>
			/// ポリゴン内の頂点数
			/// </summary>
			public int VertexCount {
				get {
					if (_VertexCount < 0 && this.Loops != null) {
						var loopsCore = this.Loops.Core;
						int count = 0;
						for (int i = loopsCore.Count - 1; i != -1; i--) {
							var loop = loopsCore.Items[i];
							if (loop == null)
								continue;
							var vts = loop.Vertices;
							if (vts == null)
								continue;
							count += vts.Count;
						}
						_VertexCount = count;
					}
					return _VertexCount;
				}
				set {
					_VertexCount = value;
				}
			}

			/// <summary>
			/// 境界ボリューム
			/// </summary>
			public range Volume {
				get {
					if (!_Volume.IsValid && this.Loops != null) {
						var loopsCore = this.Loops.Core;
						var vol = range.InvalidValue;
						for (int i = loopsCore.Count - 1; i != -1; i--) {
							vol.MergeSelf(loopsCore.Items[i].Volume);
						}
						_Volume = vol;
					}
					return _Volume;
				}
				set {
					_Volume = value;
				}
			}

			/// <summary>
			/// グループインデックス
			/// </summary>
			public int GroupIndex = -1;

			/// <summary>
			/// ポリゴンインデックス
			/// </summary>
			public int PolygonIndex = -1;

			public polbool Owner;

			/// <summary>
			/// コンストラクタ
			/// </summary>
			public Polygon() : this(new FList<VLoop>()) {
			}

			/// <summary>
			/// コンストラクタ、頂点ループリストを渡して初期化する
			/// </summary>
			/// <param name="loops">ループリスト、添字[0:外枠、1...:穴]</param>
			public Polygon(FList<VLoop> loops) {
				this.Loops = loops;
			}

			/// <summary>
			/// コンストラクタ、頂点ループコレクションを渡して初期化する
			/// </summary>
			/// <param name="loops">頂点ループコレクション、添字[0:外枠、1...:穴]</param>
			public Polygon(IEnumerable<VLoop> loops) {
				this.Loops = new FList<VLoop>(loops);
			}

			/// <summary>
			/// Validation() メソッドの準備処理
			/// </summary>
			/// <param name="lfinder">線分検索用オブジェクト</param>
			/// <returns>使えるなら true が返る</returns>
			public ValidationResult PrepareValidation(LineFinder lfinder) {
				var loopsCore = this.Loops.Core;

				// 全ループに対して頂点数と回転方向チェック
				for (int iloop = loopsCore.Count - 1; iloop != -1; iloop--) {
					var loop = loopsCore.Items[iloop];
					var verticesCore = loop.Vertices.Core;
					element area;

					// 最低でも３点以上ないとだめ
					if (verticesCore.Count < 3)
						return new ValidationResult(false, "ループ" + (iloop + 1) + "の頂点数が３未満です。");

					// 頂点数とエッジ数が矛盾してたらだめ
					if (loop.EdgesUserData != null && loop.EdgesUserData.Count != verticesCore.Count)
						return new ValidationResult(false, "ループ" + (iloop + 1) + "の頂点数とエッジ数が矛盾しています。");

					// 時計回りチェック
					area = Area(verticesCore);
					if (iloop == 0) {
						if (0 <= area)
							return new ValidationResult(false, "ループ" + (iloop + 1) + "は時計回りでなければなりません。");
					} else {
						if (area <= 0)
							return new ValidationResult(false, "ループ" + (iloop + 1) + "は反時計回りでなければなりません。");
					}
				}

				// 全頂点とラインを検索用ツリーに登録
				int startId = 0;
				for (int iloop = loopsCore.Count - 1; iloop != -1; iloop--) {
					var vertices = loopsCore.Items[iloop].Vertices;
					lfinder.Add(this, startId, vertices);
					startId += vertices.Count;
				}

				return new ValidationResult(true, null);
			}

			/// <summary>
			/// ポリゴンがブーリアン演算の入力として使えるか調べる
			/// </summary>
			/// <param name="lfinder">線分検索用オブジェクト</param>
			/// <param name="epsilon">頂点半径</param>
			/// <returns>使えるなら true が返る</returns>
			public ValidationResult Validation(LineFinder lfinder, element epsilon) {
				var loopsCore = this.Loops.Core;
				var verticesCore0 = loopsCore.Items[0].Vertices.Core;

				// 全ループチェック
				int startId = 0;
				var epsilon2 = epsilon * epsilon;
				for (int iloop = loopsCore.Count - 1; iloop != -1; iloop--) {
					var verticesCore = loopsCore.Items[iloop].Vertices.Core;

					// 辺同士の共有と交差チェック
					var p1 = verticesCore.Items[0].Position;
					for (int i = 1; i <= verticesCore.Count; i++) {
						var id1 = i - 1;
						var id2 = startId + (id1 + 1) % verticesCore.Count;
						var id3 = startId + (id1 - 1 + verticesCore.Count) % verticesCore.Count;
						id1 += startId;

						var p2 = verticesCore.Items[i % verticesCore.Count].Position;

						if (lfinder.TestShare(p1, p2, this, id1, epsilon2))
							return new ValidationResult(false, "ループ" + (iloop + 1) + "の辺" + i + "が共有されています。");

						if (lfinder.TestIntersect(p1, p2, this, id1, id2, id3))
							return new ValidationResult(false, "ループ" + (iloop + 1) + "の辺" + i + "が自己交差しています。");

						p1 = p2;
					}

					startId += verticesCore.Count;

					// 穴が外側のポリゴン外に出ていないかチェック
					if (1 <= iloop) {
						if (!PointInPolygon2(verticesCore.Items[0].Position, verticesCore0, true)) {
							return new ValidationResult(false, "ループ" + (iloop + 1) + "はポリゴン外に出てはなりません。");
						}
					}
				}

				return new ValidationResult(true, "有効なポリゴンです。ブーリアン処理に使用できます。");
			}

			/// <summary>
			/// 複製を作成
			/// </summary>
			/// <returns>複製</returns>
			public Polygon Clone() {
				var c = this.MemberwiseClone() as Polygon;
				if (c.Loops != null)
					c.Loops = new FList<VLoop>(c.Loops);
				return c;
			}

			/// <summary>
			/// ポリゴンを平行移動する
			/// </summary>
			/// <param name="offset">移動量</param>
			public void Offset(vector offset) {
				var loopsCore = this.Loops.Core;
				for (int i = loopsCore.Count - 1; i != -1; i--) {
					loopsCore.Items[i].Offset(offset);
				}
			}

			public override string ToString() {
				if (InRecursiveCall(this)) {
					return Jsonable.Fields(nameof(this.GroupIndex), this.GroupIndex, nameof(this.PolygonIndex), this.PolygonIndex, nameof(this.Volume), this.Volume, nameof(this.VertexCount), this.VertexCount, nameof(this.UserValue), this.UserValue);
				} else {
					try {
						EnterRecursiveCall(this);
						return Jsonable.Fields(nameof(this.GroupIndex), this.GroupIndex, nameof(this.PolygonIndex), this.PolygonIndex, nameof(this.Volume), this.Volume, nameof(this.VertexCount), this.VertexCount, nameof(this.UserData), this.UserData, nameof(this.UserValue), this.UserValue, nameof(this.Loops), this.Loops);
					} finally {
						LeaveRecursiveCall(this);
					}
				}
			}

			public string ToStringForDebug() {
				var sb = new StringBuilder();
				var loopsCore = this.Loops.Core;
				for (int iloop = 0; iloop < loopsCore.Count; iloop++) {
					var loop = loopsCore.Items[iloop];

					if (iloop == 0)
						sb.AppendLine("polygon");
					else
						sb.AppendLine("hole");

					var verticesCore = loop.Vertices.Core;
					for (int i = 0; i < verticesCore.Count; i++) {
						sb.AppendLine(verticesCore.Items[i].ToStringForDebug());
					}
				}
				return sb.ToString();
			}

			public string ToJsonString() {
				return this.ToString();
			}
		}

		/// <summary>
		/// エッジに付与するフラグ
		/// </summary>
		[Flags]
		public enum EdgeFlags {
			/// <summary>
			/// 右側がポリゴン化済み
			/// </summary>
			RightPolygonized = 1 << 0,

			/// <summary>
			/// 左側がポリゴン化済み
			/// </summary>
			LeftPolygonized = 1 << 1,

			/// <summary>
			/// 右側が摘出済み
			/// </summary>
			RightRemoved = 1 << 2,

			/// <summary>
			/// 左側が摘出済み
			/// </summary>
			LeftRemoved = 1 << 3,

			/// <summary>
			/// 右側が有効なポリゴンとして取得されたどうか
			/// </summary>
			RightEnabled = 1 << 4,

			/// <summary>
			/// 左側が有効なポリゴンとして取得されたどうか
			/// </summary>
			LeftEnabled = 1 << 5,
		}

		/// <summary>
		/// エッジへのノード挿入情報
		/// </summary>
		public struct NodeInsertion {
			public class ParameterComparer : IComparer<NodeInsertion> {
				public int Compare(NodeInsertion x, NodeInsertion y) {
					return Math.Sign(y.Parameter - x.Parameter);
				}
			}

			/// <summary>
			/// エッジの線分パラメータ
			/// </summary>
			public element Parameter;

			/// <summary>
			/// 挿入するノード
			/// </summary>
			public Node Node;

			public NodeInsertion(element parameter, Node node) {
				this.Parameter = parameter;
				this.Node = node;
			}
		}

		/// <summary>
		/// ノード、多角形の頂点と多角形の交点部分に作成される
		/// </summary>
		public class Node : IGridSpaceItem2f, IJsonable {
			/// <summary>
			/// ユニークなノードインデックス、 NodeManager 内で同じ値があってはならない
			/// </summary>
			public uint UniqueIndex;

			/// <summary>
			/// 座標
			/// </summary>
			public vector Position;

			/// <summary>
			/// ユーザーデータ、[グループインデックス]
			/// </summary>
			public object[] UserData;

			/// <summary>
			/// このノードに接続されているエッジ一覧
			/// </summary>
			public FList<Edge> Edges = new FList<Edge>();

			/// <summary>
			/// エッジインデックス配列の取得
			/// </summary>
			public uint[] EdgeIndices {
				get {
					var edgesCore = this.Edges.Core;
					var indices = new uint[edgesCore.Count];
					for (int i = edgesCore.Count - 1; i != -1; i--)
						indices[i] = edgesCore.Items[i].UniqueIndex;
					return indices;
				}
			}

			private Node() {
			}

			/// <summary>
			/// コンストラクタ、インデックスと位置を指定して初期化する
			/// </summary>
			/// <param name="uniqueIndex">ユニークなインデックス</param>
			/// <param name="position">位置</param>
			public Node(uint uniqueIndex, vector position) {
				this.UniqueIndex = uniqueIndex;
				this.Position = position;
			}

			/// <summary>
			/// 指定のエッジへのリンクを追加する
			/// </summary>
			/// <param name="edge">エッジ</param>
			public void LinkEdge(Edge edge) {
				var edges = this.Edges;
				if (edges.Contains(edge))
					return;
				edges.Add(edge);
			}

			/// <summary>
			/// 指定のエッジへのリンクを削除する
			/// </summary>
			/// <param name="edge">エッジ</param>
			public void Remove(Edge edge) {
				var edges = this.Edges;
				if (!edges.Contains(edge))
					return;
				edges.Remove(edge);
			}

			/// <summary>
			/// 指定ポリゴン用のユーザーデータを設定する
			/// </summary>
			/// <param name="groupIndex">グループインデックス</param>
			/// <param name="userData">ユーザーデータ</param>
			public void SetUserData(int groupIndex, object userData) {
				if (this.UserData == null)
					this.UserData = new object[groupIndex + 1];
				else if (this.UserData.Length <= groupIndex)
					Array.Resize(ref this.UserData, groupIndex + 1);
				this.UserData[groupIndex] = userData;
			}

			/// <summary>
			/// 指定ポリゴン用のユーザーデータを取得する
			/// </summary>
			/// <param name="groupIndex">グループインデックス</param>
			/// <returns>ユーザーデータ又は null</returns>
			public object GetUserData(int groupIndex) {
				var ud = this.UserData;
				if (ud == null)
					return null;
				if (groupIndex < ud.Length)
					return ud[groupIndex];
				else
					return null;
			}

			/// <summary>
			/// 指定されたポリゴンがリンクされているかどうか調べる
			/// </summary>
			/// <param name="groupIndex">グループインデックス</param>
			/// <param name="polygonIndex">ポリゴンインデックス</param>
			/// <returns>リンクされているなら true</returns>
			public bool IsPolygonLinked(int groupIndex, int polygonIndex) {
				var edgesCore = this.Edges.Core;
				for (int i = edgesCore.Count - 1; i != -1; i--) {
					if (edgesCore.Items[i].IsPolygonLinked(groupIndex, polygonIndex))
						return true;
				}
				return false;
			}

			/// <summary>
			/// 指定されたポリゴンがリンクされているかどうか調べる
			/// </summary>
			/// <param name="groupIndex">グループインデックス</param>
			/// <param name="polygonIndex">ポリゴンインデックス</param>
			/// <param name="excludeEdge">チェックから除外するエッジ</param>
			/// <returns>リンクされているなら true</returns>
			public bool IsPolygonLinked(int groupIndex, int polygonIndex, Edge excludeEdge) {
				var edgesCore = this.Edges.Core;
				for (int i = edgesCore.Count - 1; i != -1; i--) {
					var edge = edgesCore.Items[i];
					if (edge != excludeEdge && edge.IsPolygonLinked(groupIndex, polygonIndex))
						return true;
				}
				return false;
			}

			public volume GetVolume() {
				return new volume(this.Position, vector.Zero, vector.AxisX, vector.AxisY);
			}

			public override string ToString() {
				if (InRecursiveCall(this, this.Edges)) {
					return Jsonable.Fields(nameof(this.UniqueIndex), this.UniqueIndex, nameof(this.Position), this.Position);
				} else {
					try {
						EnterRecursiveCall(this);
						return Jsonable.Fields(nameof(this.UniqueIndex), this.UniqueIndex, nameof(this.Position), this.Position, nameof(this.UserData), this.UserData, nameof(this.Edges), this.Edges);
					} finally {
						LeaveRecursiveCall(this);
					}
				}
			}

			public string ToStringForDebug() {
				var sb = new StringBuilder();
				sb.Append("UniqueIndex=" + this.UniqueIndex);
				sb.Append("\tPosition=" + polbool.ToString(this.Position));
				sb.Append("\tEdgeIndices=" + polbool.ToString(this.EdgeIndices));
				return sb.ToString();
			}

			public string ToJsonString() {
				return this.ToString();
			}
		}

		/// <summary>
		/// ２グループ分のポリゴンインデックス配列
		/// </summary>
		public struct PolygonIndices : ICollection<int>, IJsonable {
			/// <summary>
			/// 全要素をパックした値
			/// </summary>
			uint _Value;

			/// <summary>
			/// 指定グループのポリゴンインデックスの設定と取得
			/// </summary>
			/// <param name="index">グループインデックス</param>
			/// <returns>ポリゴンインデックス</returns>
			public int this[int index] {
				get {
					// ビットイメージ
					// 0...15: ０グループ目ポリゴンインデックス
					// 16...29: １グループ目ポリゴンインデックス
					// 30...31: 全グループ数

					// まず指定インデックスの最上位ビットが31ビット目に来るようにシフトする
					// そして31ビット目が15ビット目に来るようにシフトする、符号も自動調整される
					return unchecked((int)(this._Value << (17 - index * 15)) >> 17);
				}
				set {
					unchecked {
						var v = this._Value;
						var shift = index * 15;
						v &= ~(0x7fffu << shift);
						v |= ((uint)value & 0x7fffu) << shift;
						this._Value = v;
					}
				}
			}

			/// <summary>
			/// 全グループ数
			/// </summary>
			public int Length {
				get {
					return (int)((uint)this._Value >> 30);
				}
			}

			/// <summary>
			/// 有効なポリゴンインデックスが設定されているグループ数
			/// </summary>
			public int Count {
				get {
					var v = this._Value;
					int c = 0;
					if ((v & 0x7fffu) != 0x7fffu)
						c++;
					if ((v & (0x7fffu << 15)) != (0x7fffu << 15))
						c++;
					return c;
				}
			}

			public bool IsReadOnly {
				get {
					return false;
				}
			}

			public PolygonIndices(int length) {
				this._Value = 0x7fffu | (0x7fffu << 15) | ((uint)length << 30);
			}

			public PolygonIndices(IEnumerable<int> indices) {
				var array = indices.ToArray();
				if (2 < array.Length)
					throw new Exception("Too many polygon indices.");

				this = new PolygonIndices(0);

				for (int index = 0; index < array.Length; index++) {
					var value = array[index];
					unchecked {
						var v = _Value;
						var shift = index * 15;
						v &= ~(0x7fffu << shift);
						v |= ((uint)value & 0x7fffu) << shift;
						this._Value = v;
					}
				}
			}

			public override string ToString() {
				var sb = new StringBuilder();
				sb.Append("[ ");
				for (int i = 0, n = this.Length; i < n; i++) {
					if (i != 0)
						sb.Append(", ");
					sb.Append(this[i]);
				}
				sb.Append(" ]");
				return sb.ToString();
			}

			public string ToJsonString() {
				return this.ToString();
			}

			public void Add(int item) {
				if (_Value == 0)
					Clear();
				if (this.Count == 2)
					throw new Exception("Too many polygon indices.");
				this[this.Count] = item;
			}

			public void Clear() {
				this = new PolygonIndices(0);
			}

			public bool Contains(int item) {
				return this[0] == item || this[1] == item;
			}

			public void CopyTo(int[] array, int arrayIndex) {
				int index = 0;
				for (int i = 0; i < 2; i++) {
					var value = this[i];
					if (0 <= value) {
						array[arrayIndex + index] = value;
						index++;
					}
				}
			}

			public bool Remove(int item) {
				if (this[0] == item) {
					this[0] = -1;
					return true;
				}
				if (this[1] == item) {
					this[1] = -1;
					return true;
				}
				return false;
			}

			public IEnumerator<int> GetEnumerator() {
				for (int i = 0; i < 2; i++) {
					var value = this[i];
					if (0 <= value)
						yield return value;
				}
			}

			IEnumerator IEnumerable.GetEnumerator() {
				return this.GetEnumerator();
			}
		}

		/// <summary>
		/// エッジ、ノード同士を繋ぎ位相情報を持たせる
		/// </summary>
		public class Edge : IGridSpaceItem2f, IJsonable {
			/// <summary>
			/// エッジの情報フラグ
			/// </summary>
			public EdgeFlags Flags;

			/// <summary>
			/// ユニークなエッジインデックス、 EdgeManager 内で同じ値があってはならない
			/// </summary>
			public uint UniqueIndex;

			/// <summary>
			/// 開始ノード
			/// </summary>
			public Node From;

			/// <summary>
			/// 終了ノード
			/// </summary>
			public Node To;

			/// <summary>
			/// 開始ノードのインデックス
			/// </summary>
			public uint FromIndex {
				get {
					return this.From.UniqueIndex;
				}
			}

			/// <summary>
			/// 終了ノードのインデックス
			/// </summary>
			public uint ToIndex {
				get {
					return this.To.UniqueIndex;
				}
			}

			/// <summary>
			/// エッジの長さ
			/// </summary>
			public element Length;

			/// <summary>
			/// 進行方向From→Toの右側のグループ内ポリゴンインデックス一覧、[グループインデックス]
			/// </summary>
			public PolygonIndices RightPolygons;

			/// <summary>
			/// 進行方向From→Toの左側のグループ内ポリゴンインデックス一覧、[グループインデックス]
			/// </summary>
			public PolygonIndices LeftPolygons;

			/// <summary>
			/// 進行方向From→Toの右側のユーザーデータ、[グループインデックス]
			/// </summary>
			public object[] RightUserData;

			/// <summary>
			/// 進行方向From→Toの左側のユーザーデータ、[グループインデックス]
			/// </summary>
			public object[] LeftUserData;

			/// <summary>
			/// ユーザーが自由に設定＆使用できるユーザーデータ
			/// </summary>
			public object UserData;

			/// <summary>
			/// ユーザーが自由に設定＆使用できる値
			/// </summary>
			public ulong UserValue;

			/// <summary>
			/// 進行方向From→Toの右側のグループインデックス最大値
			/// </summary>
			public int RightGroupMax = -1;

			/// <summary>
			/// 進行方向From→Toの左側のグループインデックス最大値
			/// </summary>
			public int LeftGroupMax = -1;

			/// <summary>
			/// null でない場合にはエッジ上にノードを挿入する予定であることを示す
			/// </summary>
			public FList<NodeInsertion> NodeInsertions;

			/// <summary>
			/// エッジの中心座標
			/// </summary>
			public vector Center {
				get {
					return (this.From.Position + this.To.Position) * (element)0.5;
				}
			}

			/// <summary>
			/// 進行方向From→Toの右側に存在するグループ数
			/// </summary>
			public int RightGroupCount {
				get {
					return this.RightPolygons.Count;
				}
			}

			/// <summary>
			/// 進行方向From→Toの左側に存在するグループ数
			/// </summary>
			public int LeftGroupCount {
				get {
					return this.LeftPolygons.Count;
				}
			}

			private Edge() {
			}

			/// <summary>
			/// コンストラクタ、インデックスとノードを指定して初期化する
			/// </summary>
			/// <param name="uniqueIndex">ユニークなインデックス</param>
			/// <param name="from">エッジの開始ノード</param>
			/// <param name="to">エッジの終了ノード</param>
			public Edge(int groupCount, uint uniqueIndex, Node from, Node to) {
				this.UniqueIndex = uniqueIndex;
				this.From = from;
				this.To = to;
				this.Length = (to.Position - from.Position).Length;
				this.RightPolygons = new PolygonIndices(groupCount);
				this.LeftPolygons = new PolygonIndices(groupCount);
				from.LinkEdge(this);
				to.LinkEdge(this);
			}

			/// <summary>
			/// ノードから切断する
			/// </summary>
			public void Disconnect() {
				if (this.From != null) {
					this.From.Remove(this);
					this.From = null;
				}
				if (this.To != null) {
					this.To.Remove(this);
					this.To = null;
				}
			}

			/// <summary>
			/// ポリゴンへリンクする
			/// </summary>
			/// <param name="right">右側へリンクするなら true、左側なら false</param>
			/// <param name="groupIndex">グループインデックス</param>
			/// <param name="polygonIndex">グループ内のポリゴンインデックス</param>
			public void LinkPolygon(bool right, int groupIndex, int polygonIndex) {
				var p = right ? this.RightPolygons : this.LeftPolygons;
				if (0 <= p[groupIndex])
					return; // 既にリンク済みならスキップ　※同じ側に同グループのポリゴンはリンクされない仕様なので polygonIndex はチェックしなくて良い

				p[groupIndex] = polygonIndex;

				if (right) {
					this.RightPolygons = p;
					if (this.RightGroupMax < groupIndex)
						this.RightGroupMax = groupIndex;
				} else {
					this.LeftPolygons = p;
					if (this.LeftGroupMax < groupIndex)
						this.LeftGroupMax = groupIndex;
				}
			}

			/// <summary>
			/// ポリゴンへリンクする
			/// </summary>
			/// <param name="from">ポリゴン内でのエッジの開始ノード</param>
			/// <param name="groupIndex">グループインデックス</param>
			/// <param name="polygonIndex">グループ内のポリゴンインデックス</param>
			public void LinkPolygon(Node from, int groupIndex, int polygonIndex) {
				LinkPolygon(from == this.From, groupIndex, polygonIndex);
			}

			/// <summary>
			/// 指定エッジの属性を方向を考慮しつつコピーする
			/// </summary>
			/// <param name="edge">エッジ</param>
			/// <param name="sameDir">指定エッジが同じ方向かどうか</param>
			/// <param name="userDataCloner">ユーザーデータの複製を作成するデリゲート</param>
			public void CopyAttributes(Edge edge, bool sameDir, Func<object, object> userDataCloner) {
				PolygonIndices rp, lp;
				if (sameDir) {
					rp = edge.RightPolygons;
					lp = edge.LeftPolygons;
				} else {
					lp = edge.RightPolygons;
					rp = edge.LeftPolygons;
				}
				for (int i = rp.Length - 1; i != -1; i--) {
					if (0 <= rp[i]) {
						this.LinkPolygon(true, i, rp[i]);
						var d = edge.GetUserData(sameDir, i);
						if (d != null)
							this.SetUserData(true, i, userDataCloner != null ? userDataCloner(d) : d);
					}
				}
				for (int i = lp.Length - 1; i != -1; i--) {
					if (0 <= lp[i]) {
						this.LinkPolygon(false, i, lp[i]);
						var d = edge.GetUserData(!sameDir, i);
						if (d != null)
							this.SetUserData(false, i, userDataCloner != null ? userDataCloner(d) : d);
					}
				}
			}

			/// <summary>
			/// 指定されたグループがリンクされているかどうか調べる
			/// </summary>
			/// <param name="groupIndex">グループインデックス</param>
			/// <returns>リンクされているなら true</returns>
			public bool IsGroupLinked(int groupIndex) {
				return 0 <= this.RightPolygons[groupIndex] || 0 <= this.LeftPolygons[groupIndex];
			}

			/// <summary>
			/// 指定されたポリゴンがリンクされているかどうか調べる
			/// </summary>
			/// <param name="groupIndex">グループインデックス</param>
			/// <param name="polygonIndex">ポリゴンインデックス</param>
			/// <returns>リンクされているなら true</returns>
			public bool IsPolygonLinked(int groupIndex, int polygonIndex) {
				if (this.RightPolygons[groupIndex] == polygonIndex)
					return true;
				if (this.LeftPolygons[groupIndex] == polygonIndex)
					return true;
				return false;
			}

			/// <summary>
			/// 指定グループ用のユーザーデータを設定する
			/// </summary>
			/// <param name="right">true なら右側に false なら左側に設定する</param>
			/// <param name="groupIndex">グループインデックス</param>
			/// <param name="userData">ユーザーデータ</param>
			public void SetUserData(bool right, int groupIndex, object userData) {
				var d = right ? this.RightUserData : this.LeftUserData;
				if (d == null) {
					d = new object[groupIndex + 1];
				} else if (d.Length <= groupIndex) {
					Array.Resize(ref d, groupIndex + 1);
				}
				d[groupIndex] = userData;
				if (right) {
					this.RightUserData = d;
				} else {
					this.LeftUserData = d;
				}
			}

			/// <summary>
			/// 指定グループ用のユーザーデータを取得する
			/// </summary>
			/// <param name="right">true なら右側に false なら左側から取得する</param>
			/// <param name="groupIndex">グループインデックス</param>
			/// <returns>ユーザーデータ又は null</returns>
			public object GetUserData(bool right, int groupIndex) {
				var d = right ? this.RightUserData : this.LeftUserData;
				if (d == null || d.Length <= groupIndex)
					return null;
				return d[groupIndex];
			}

			/// <summary>
			/// 位相構造上指定エッジと交差する可能性があるかどうか調べる
			/// </summary>
			/// <param name="edge">エッジ</param>
			/// <returns>交差し得るなら true</returns>
			public bool IsCrossable(Edge edge) {
				// 同じエッジなら交差するはずがない
				if (this == edge)
					return false;
#if INTERSECT_SELF
				// リンクしているノードから生えているエッジは交差し得ない
				if (this.From.Edges.Contains(edge) || this.To.Edges.Contains(edge))
					return false;

				return true;
#else
				// 同グループのポリゴンは自己交差しない前提なので
				// リンクしているグループが両方とも同じなら交差しない
				var r1 = this.RightPolygons;
				var l1 = this.LeftPolygons;
				var r2 = edge.RightPolygons;
				var l2 = edge.LeftPolygons;
				for (int i = r1.Length - 1; i != -1; i--) {
					if ((0 <= r1[i] || 0 <= l1[i]) != (0 <= r2[i] || 0 <= l2[i]))
						return true;
				}

				return false;
#endif
			}

			public void SetNodeInsertion(element t, Node node) {
				if (this.NodeInsertions == null)
					this.NodeInsertions = new FList<NodeInsertion>();
				this.NodeInsertions.Add(new NodeInsertion(t, node));
			}

			/// <summary>
			/// ノードの組み合わせからユニークなエッジIDを取得する
			/// </summary>
			public static ulong GetID(Node n1, Node n2) {
				var nid1 = n1.UniqueIndex;
				var nid2 = n2.UniqueIndex;
				return nid2 <= nid1 ? (ulong)nid1 << 32 | nid2 : (ulong)nid2 << 32 | nid1;
			}

			/// <summary>
			/// エッジの組み合わせからユニークな組み合わせIDを取得する
			/// ※エッジ同士の交差判定でテスト済みの組み合わせを再度テストしないために使う
			/// </summary>
			public static ulong GetCombID(Edge e1, Edge e2) {
				var nid1 = e1.UniqueIndex;
				var nid2 = e2.UniqueIndex;
				return nid2 <= nid1 ? (ulong)nid1 << 32 | nid2 : (ulong)nid2 << 32 | nid1;
			}

			public range GetRange() {
				return new range(this.From.Position, this.To.Position, true);
			}

			public volume GetVolume() {
				var p1 = this.From.Position;
				var p2 = this.To.Position;
				var l = this.Length;
				var c = (p1 + p2) * (element)0.5;
				var v = p2 - p1;
				var ax = v * ((element)1 / l);
				var ay = ax.RightAngle();
				return new volume(c, new vector(l * (element)0.5, 0), ax, ay);
			}

			public override string ToString() {
				if (InRecursiveCall(this, this.From, this.To)) {
					return Jsonable.Fields(nameof(this.UniqueIndex), this.UniqueIndex, nameof(this.Flags), this.Flags, nameof(this.RightPolygons), this.RightPolygons, nameof(this.LeftPolygons), this.LeftPolygons, nameof(this.UserValue), this.UserValue);
				} else {
					try {
						EnterRecursiveCall(this);
						return Jsonable.Fields(nameof(this.UniqueIndex), this.UniqueIndex, nameof(this.Flags), this.Flags, nameof(this.From), this.From, nameof(this.To), this.To, nameof(this.RightPolygons), this.RightPolygons, nameof(this.LeftPolygons), this.LeftPolygons, nameof(this.UserData), this.UserData, nameof(this.UserValue), this.UserValue);
					} finally {
						LeaveRecursiveCall(this);
					}
				}
			}

			public string ToStringForDebug() {
				var sb = new StringBuilder();
				sb.Append("UniqueIndex=" + this.UniqueIndex);
				sb.Append("\tFlags=" + this.Flags);
				sb.Append("\tFromIndex=" + this.FromIndex);
				sb.Append("\tToIndex=" + this.ToIndex);
				sb.Append("\tRightPolygons=" + polbool.ToString(this.RightPolygons));
				sb.Append("\tLeftPolygons=" + polbool.ToString(this.LeftPolygons));
				sb.Append("\tRightGroupMax=" + this.RightGroupMax);
				sb.Append("\tLeftGroupMax=" + this.LeftGroupMax);
				return sb.ToString();
			}

			public string ToJsonString() {
				return this.ToString();
			}
		}

		/// <summary>
		/// バリデーション結果
		/// </summary>
		public struct ValidationResult {
			/// <summary>
			/// 有効かどうか
			/// </summary>
			public bool IsValid;

			/// <summary>
			/// エラーがあるならエラー内容メッセージが入る
			/// </summary>
			public string Message;

			public ValidationResult(bool isValid, string message) {
				this.IsValid = isValid;
				this.Message = message;
			}
		}

		public struct PointWithId {
			public int Id;
			public vector P;

			public PointWithId(int id, vector p) {
				this.Id = id;
				this.P = p;
			}
		}

		public struct LineWithId : IGridSpaceItem2f {
			public Polygon Polygon;
			public int Id;
			public vector P1;
			public vector P2;

			public LineWithId(Polygon polygon, int id, vector p1, vector p2) {
				this.Polygon = polygon;
				this.Id = id;
				this.P1 = p1;
				this.P2 = p2;
			}

			public volume GetVolume() {
				var p = this.P1;
				var c = (p + this.P2) * (element)0.5;
				var v = p - c;
				v.AbsSelf();

				var ax = v.Normalize();
				var ay = ax.RightAngle();
				return new volume(c, new vector(v.X, 0), ax, ay);
			}
		}

		/// <summary>
		/// 座標の範囲で頂点間のラインを検索するヘルパクラス
		/// </summary>
		public class LineFinder {
			GridSpace2f<LineWithId> _Grid;

			/// <summary>
			/// コンストラクタ、全体の範囲と分割数を指定して初期化する
			/// </summary>
			/// <param name="volume">全体範囲</param>
			/// <param name="division">分割数</param>
			/// <param name="epsilon">頂点の半径</param>
			public LineFinder(range volume, vectori division, element epsilon) {
				_Grid = new GridSpace2f<LineWithId>(volume, division);
				_Grid.VolumeExpansion = epsilon;
			}

			public void Add(Polygon polygon, int startId, FList<Vertex> vertices) {
				var grid = _Grid;
				var verticesCore = vertices.Core;
				var v1 = verticesCore.Items[0].Position;

				startId--;

				for (int i = 1; i <= verticesCore.Count; i++) {
					var v2 = verticesCore.Items[i % verticesCore.Count].Position;
					grid.Add(new LineWithId(polygon, startId + i, v1, v2));
					v1 = v2;
				}
			}

			public void Optimize() {
			}

			/// <summary>
			/// 指定範囲内の頂点を検索しインデックス番号配列を取得する
			/// </summary>
			/// <param name="volume">検索範囲を表す境界ボリューム</param>
			/// <returns>範囲内の頂点インデックス番号配列</returns>
			public IEnumerable<LineWithId> Query(volume volume) {
				return _Grid.Query(volume);
			}

			/// <summary>
			/// 指定線分と交差する線分があるか調べる
			/// </summary>
			/// <param name="p1">線分の点１</param>
			/// <param name="p2">線分の点２</param>
			/// <param name="polygon">除外するIDのオブジェクト</param>
			/// <param name="id1">除外する線分ID１</param>
			/// <param name="id2">除外する線分ID２</param>
			/// <param name="id3">除外する線分ID３</param>
			/// <returns>接触しているならtrue</returns>
			public bool TestIntersect(vector p1, vector p2, object polygon, int id1, int id2, int id3) {
				var c = (p1 + p2) * (element)0.5;
				var v = p1 - c;
				v.AbsSelf();

				var ax = v.Normalize();
				var ay = ax.RightAngle();
				foreach (var q in Query(new volume(c, new vector(v.X, 0), ax, ay))) {
					if ((q.Polygon != polygon || q.Id != id1 && q.Id != id2 && q.Id != id3) && LineIntersect(p1, p2, q.P1, q.P2)) {
						return true;
					}
				}
				return false;
			}

			/// <summary>
			/// 指定線分と同じ座標の線分があるか調べる
			/// </summary>
			/// <param name="p1">線分の点１</param>
			/// <param name="p2">線分の点２</param>
			/// <param name="polygon">除外するIDのオブジェクト</param>
			/// <param name="id">除外する線分ID</param>
			/// <param name="epsilon">同一座標判定用最小距離値の二乗、この距離以下の距離は同一座標とみなす</param>
			/// <returns>接触しているならtrue</returns>
			public bool TestShare(vector p1, vector p2, object polygon, int id, element epsilon2) {
				if (p2.LessIdThan(p1)) {
					var t = p1;
					p1 = p2;
					p2 = t;
				}

				var c = (p1 + p2) * (element)0.5;
				var v = p1 - c;
				v.AbsSelf();

				var ax = v.Normalize();
				var ay = ax.RightAngle();
				foreach (var tq in Query(new volume(c, new vector(v.X, 0), vector.AxisX, vector.AxisY))) {
					if (tq.Polygon != polygon || tq.Id == id)
						continue;

					var q = tq;
					if (q.P2.LessIdThan(q.P1)) {
						var t = q.P1;
						q.P1 = q.P2;
						q.P2 = t;
					}

					if ((q.P1 - p1).LengthSquare <= epsilon2 && (q.P2 - p2).LengthSquare <= epsilon2)
						return true;
				}
				return false;
			}
		}

		/// <summary>
		/// ノードとエッジで構成されるポリゴン
		/// </summary>
		public class EPolygon : IJsonable {
			/// <summary>
			/// ループ配列、添字[0:外枠、1...:穴]
			/// </summary>
			public FList<ELoop> Loops;

			/// <summary>
			/// このポリゴンに紐づくユーザーデータ
			/// </summary>
			public object UserData;

			/// <summary>
			/// ユーザーが自由に設定＆使用できる値
			/// </summary>
			public ulong UserValue;

			/// <summary>
			/// グループインデックス
			/// </summary>
			public int GroupIndex = -1;

			/// <summary>
			/// グループインデックス
			/// </summary>
			public int PolygonIndex = -1;

			public EPolygon() {
				this.Loops = new FList<ELoop>();
			}

			public EPolygon(FList<ELoop> loops) {
				this.Loops = loops;
			}

			public EPolygon(IEnumerable<ELoop> loops) {
				this.Loops = new FList<ELoop>(loops);
			}

			/// <summary>
			/// 指定座標を包含しているか調べる
			/// </summary>
			/// <param name="c">座標</param>
			/// <returns>包含しているなら true</returns>
			public bool Contains(vector c) {
				var loopsCore = this.Loops.Core;
				for (int i = 0; i < loopsCore.Count; i++) {
					var loop = loopsCore.Items[i];
					var touch = loop.Volume.Contains(c);
					if (touch) {
						touch = PointTouchPolygon2(c, loop.Edges.Core, true) != 0;
					}
					if (i == 0) {
						if (!touch)
							return false;
					} else {
						if (touch)
							return false;
					}
				}
				return true;
			}

			/// <summary>
			/// 指定ポリゴンのエッジを包含しているならこのポリゴンをリンクさせる
			/// </summary>
			/// <param name="polygon">包含チェック対象ポリゴン</param>
			/// <returns>立場を逆にしたパターンを調べる必要が無いなら true</returns>
			public void LinkPolygonIfContainsEdge(EPolygon polygon) {
				var thisGroupIndex = this.GroupIndex;
				var thisPolygonIndex = this.PolygonIndex;
				var myVolume = this.Loops[0].Volume;
				var targetLoopsCore = polygon.Loops.Core;

				for (int iloop = 0; iloop < targetLoopsCore.Count; iloop++) {
					var loop = targetLoopsCore.Items[iloop];
					if (iloop == 0 && !myVolume.Intersects(loop.Volume)) {
						// 外枠同士が接触していないなら何もする必要無し
						break;
					}

					var edgesCore = loop.Edges.Core;
					if (edgesCore.Count == 0)
						continue;

					// 交点を探す
					// 位相構造構築後なので共有されているノードが交点となっている
					var intersects = false;
					for (int i = edgesCore.Count - 1; i != -1; i--) {
						var edge = edgesCore.Items[i];
						if (edge.From.IsPolygonLinked(thisGroupIndex, thisPolygonIndex)) {
							intersects = true;
							break;
						}
					}

					// もし交点が無いなら完全に包含されているか完全にポリゴン外と言える
					// 適当な頂点を選び内外判定結果が「内」ならエッジの両側に自分をリンクする
					if (!intersects) {
						var position = edgesCore.Items[0].From.Position;
						if (myVolume.Contains(position) && Contains(position)) {
							for (int i = edgesCore.Count - 1; i != -1; i--) {
								var edge = edgesCore.Items[i];
								edge.Edge.LinkPolygon(true, thisGroupIndex, thisPolygonIndex);
								edge.Edge.LinkPolygon(false, thisGroupIndex, thisPolygonIndex);
							}
						}
						continue;
					}

					// 交点があるということは包含されているエッジとそうでないものがある
					// 包含されているエッジは両側に自分をリンクする、但し共有されているエッジは除く
					// エッジ包含の調べ方は以下の通り
					// 一度共有されていないエッジが現れると共有ノードが現れるまで包含／非包含状態が継続する
					// つまり最初に現れた非共有エッジの中間点を内外判定すればおのずとわかる
					int unshareStartIndex = -1;
					var edgeShared = edgesCore.Items[0].Edge.IsPolygonLinked(thisGroupIndex, thisPolygonIndex);
					var sharedEdgeExists = edgeShared;
					for (int i = edgesCore.Count - 1; i != -1; i--) {
						var es = edgesCore.Items[i].Edge.IsPolygonLinked(thisGroupIndex, thisPolygonIndex);
						if (edgeShared && !es) {
							unshareStartIndex = i;
							break;
						}
						edgeShared = es;
						sharedEdgeExists |= es;
					}
					if (unshareStartIndex < 0) {
						if (sharedEdgeExists)
							continue; // 全エッジが共有済みなら何もする必要無し
						else
							unshareStartIndex = 0; // 全エッジが非共有なら適当に選択
					}

					var edgeInclusion = false;
					edgeShared = true;
					for (int i = edgesCore.Count; i != 0; i--) {
						var iedge = (unshareStartIndex + i) % edgesCore.Count;
						var edge = edgesCore.Items[iedge];
						var es = edge.Edge.IsPolygonLinked(thisGroupIndex, thisPolygonIndex);

						if (edgeShared && !es) {
							var position = edge.Edge.Center;
							edgeInclusion = myVolume.Contains(position) && Contains(position);
						}
						if (edgeInclusion) {
							edge.Edge.LinkPolygon(true, thisGroupIndex, thisPolygonIndex);
							edge.Edge.LinkPolygon(false, thisGroupIndex, thisPolygonIndex);
						}
						if (edge.From.IsPolygonLinked(thisGroupIndex, thisPolygonIndex)) {
							edgeInclusion = false;
							edgeShared = true;
						} else {
							edgeShared = es;
						}
					}
				}
			}

			public override string ToString() {
				if (InRecursiveCall(this)) {
					return Jsonable.Fields(nameof(this.GroupIndex), this.GroupIndex, nameof(this.PolygonIndex), this.PolygonIndex, nameof(this.UserValue), this.UserValue);
				} else {
					try {
						EnterRecursiveCall(this);
						return Jsonable.Fields(nameof(this.GroupIndex), this.GroupIndex, nameof(this.PolygonIndex), this.PolygonIndex, nameof(this.UserData), this.UserData, nameof(this.UserValue), this.UserValue, nameof(this.Loops), this.Loops);
					} finally {
						LeaveRecursiveCall(this);
					}
				}
			}

			public string ToJsonString() {
				return this.ToString();
			}
		}

		/// <summary>
		/// ノード管理クラス
		/// </summary>
		class NodeManager {
			uint _UniqueIndex;
			HashSet<Node> _Nodes;
			GridSpace2f<Node> _Grid;
			element _Epsilon;

			/// <summary>
			/// 全ノード一覧の取得
			/// </summary>
			public ICollection<Node> Items {
				get {
					return _Nodes;
				}
			}

			/// <summary>
			/// コンストラクタ
			/// </summary>
			/// <param name="volume">アイテム分布の最大範囲</param>
			/// <param name="division">範囲の分割数</param>
			/// <param name="epsilon">ノードの半径</param>
			public NodeManager(range volume, vectori division, element epsilon) {
				_Nodes = new HashSet<Node>();
				_Grid = new GridSpace2f<Node>(volume, division);
				_Grid.VolumeExpansion = epsilon;
				_Epsilon = epsilon;
			}

			/// <summary>
			/// ノードを新規作成する、指定ノード位置に接触するノードが既に存在する場合そのノードを返す
			/// </summary>
			/// <param name="position">ノード位置</param>
			/// <returns>ノード</returns>
			public Node New(vector position) {
				// ノードの境界ボリューム計算
				var vol = new volume(position, vector.Zero, vector.AxisX, vector.AxisY);
				vol.Extents += _Epsilon;

				// 境界ボリューム同士が接触するノード一覧取得
				// ノード一覧内で最も距離が近いものを探す
				Node node = null;
				var mindist2 = _Epsilon * _Epsilon;
				foreach (var nd in _Grid.Query(vol)) {
					var dist2 = (position - nd.Position).LengthSquare;
					if (dist2 <= mindist2) {
						node = nd;
						mindist2 = dist2;
					}
				}

				// 接触しているノードが無かった場合のみ新規作成
				if (node == null) {
					node = new Node(++_UniqueIndex, position);
					_Nodes.Add(node);
					_Grid.Add(node);
				}

				return node;
			}

			/// <summary>
			/// 指定されたノードを取り除く
			/// </summary>
			/// <param name="node">ノード</param>
			public void Remove(Node node) {
				if (!_Nodes.Remove(node))
					return;
				_Grid.Remove(node);
			}

			/// <summary>
			/// 指定された境界ボリュームに接触するノード一覧取得
			/// </summary>
			/// <param name="volume">境界ボリューム</param>
			/// <returns>ノード一覧</returns>
			public IEnumerable<Node> Query(volume volume) {
				return _Grid.Query(volume);
			}

			/// <summary>
			/// 座標による検索用ツリーを最適化する
			/// </summary>
			public void Optimize() {
			}
		}

		/// <summary>
		/// エッジ管理クラス
		/// </summary>
		class EdgeManager {
			/// <summary>
			/// グループ数
			/// </summary>
			public int GroupCount;

			uint _UniqueIndex;
			Dictionary<ulong, Edge> _Edges;
			GridSpace2f<Edge> _Grid;
			element _Epsilon;

			/// <summary>
			/// 全エッジ一覧の取得
			/// </summary>
			public ICollection<Edge> Items {
				get {
					return _Edges.Values;
				}
			}

			/// <summary>
			/// コンストラクタ
			/// </summary>
			/// <param name="volume">アイテム分布の最大範囲</param>
			/// <param name="division">範囲の分割数</param>
			/// <param name="epsilon">ノード半径</param>
			public EdgeManager(range volume, vectori division, element epsilon) {
				_Edges = new Dictionary<ulong, Edge>();
				_Grid = new GridSpace2f<Edge>(volume, division);
				_Grid.VolumeExpansion = epsilon;
				_Epsilon = epsilon;
			}

			/// <summary>
			/// エッジを新規作成する、指定ノード２つに接続されているエッジが既に存在したらそのエッジを返す
			/// </summary>
			/// <param name="from"></param>
			/// <param name="to"></param>
			/// <returns>エッジ</returns>
			public Edge New(Node from, Node to) {
				var id = Edge.GetID(from, to);
				Edge edge;
				if (_Edges.TryGetValue(id, out edge)) {
					return edge;
				} else {
					edge = new Edge(this.GroupCount, ++_UniqueIndex, from, to);
					_Edges[id] = edge;
					_Grid.Add(edge);
					return edge;
				}
			}

			/// <summary>
			/// 指定されたエッジを取り除く
			/// </summary>
			/// <param name="edge">エッジ</param>
			public void Remove(Edge edge) {
				var id = Edge.GetID(edge.From, edge.To);
				if (!_Edges.ContainsKey(id))
					return;
				_Edges.Remove(id);
				_Grid.Remove(edge);

				edge.Disconnect();
			}

			/// <summary>
			/// 指定された境界ボリュームに接触するエッジ一覧取得
			/// </summary>
			/// <param name="volume">境界ボリューム</param>
			/// <returns>エッジ一覧</returns>
			public IEnumerable<Edge> Query(volume volume) {
				return _Grid.Query(volume);
			}

			/// <summary>
			/// 座標による検索用ツリーを最適化する
			/// </summary>
			public void Optimize() {
			}
		}

		/// <summary>
		/// エッジと方向
		/// </summary>
		public struct EDir : IJsonable {
			/// <summary>
			/// エッジ
			/// </summary>
			public Edge Edge;

			/// <summary>
			/// 方向、true なら順方向、false なら逆方向
			/// </summary>
			public bool TraceRight;

			public EDir(Edge edge, bool right) {
				this.Edge = edge;
				this.TraceRight = right;
			}

			public Node From {
				get {
					return this.TraceRight ? this.Edge.From : this.Edge.To;
				}
			}

			public Node To {
				get {
					return this.TraceRight ? this.Edge.To : this.Edge.From;
				}
			}

			public override string ToString() {
				return Jsonable.Fields(nameof(this.TraceRight), this.TraceRight, nameof(this.Edge), this.Edge);
				//var from = this.TraceRight ? this.Edge.From : this.Edge.To;
				//var to = this.TraceRight ? this.Edge.To : this.Edge.From;
				//var s = string.Format("{{ {0} => {1} {2} }}", from.Position, to.Position, this.TraceRight ? "Right" : "Left");
				//var rp = this.Edge.RightPolygons;
				//var lp = this.Edge.LeftPolygons;

				//var sb1 = new StringBuilder();
				//for (int i = 0; i < rp.Length; i++) {
				//	if (0 <= rp[i]) {
				//		if (sb1.Length != 0)
				//			sb1.Append(", ");
				//		sb1.Append(i.ToString());
				//	}
				//}
				//var sb2 = new StringBuilder();
				//for (int i = 0; i < lp.Length; i++) {
				//	if (0 <= lp[i]) {
				//		if (sb2.Length != 0)
				//			sb2.Append(", ");
				//		sb2.Append(i.ToString());
				//	}
				//}

				//return s + " [" + sb1.ToString() + "] [" + sb2.ToString() + "]";
			}

			public string ToStringForDebug() {
				return (this.TraceRight ? "Right\t" : "Left\t") + this.Edge.ToStringForDebug();
			}

			public string ToJsonString() {
				return this.ToString();
			}
		}

		/// <summary>
		/// 配列のインデックス番号範囲を示す
		/// </summary>
		public struct IndexRange {
			/// <summary>
			/// インデックス開始値
			/// </summary>
			public int Start;

			/// <summary>
			/// 要素数
			/// </summary>
			public int Count;

			/// <summary>
			/// コンストラクタ、インデックス開始値と要素数を指定して初期化する
			/// </summary>
			/// <param name="start">インデックス開始値</param>
			/// <param name="count">要素数</param>
			public IndexRange(int start, int count) {
				this.Start = start;
				this.Count = count;
			}
		}

		/// <summary>
		/// エッジによるループデータ
		/// </summary>
		public class ELoop : IJsonable {
			/// <summary>
			/// ループを構成するエッジ配列
			/// </summary>
			public FList<EDir> Edges;

			/// <summary>
			/// 面積
			/// </summary>
			public element Area;

			/// <summary>
			/// 時計回りかどうか
			/// </summary>
			public bool CW;

			/// <summary>
			/// 境界ボリューム
			/// </summary>
			public range Volume = range.InvalidValue;

			/// <summary>
			/// このポリゴンに紐づくユーザーデータ
			/// </summary>
			public object UserData;

			public ELoop() {
				this.Edges = new FList<EDir>();
			}

			public ELoop(IEnumerable<EDir> edges) : this(new FList<EDir>(edges)) {
			}

			public ELoop(FList<EDir> edges) : this(Area(edges.Core), edges) {
			}

			public ELoop(element area, FList<EDir> edges) {
				this.Area = Math.Abs(area);
				this.CW = area <= 0;
				this.Edges = edges;

				var edgesCore = edges.Core;
				var volume = new range(edgesCore.Items[0].From.Position);
				for (int i = edgesCore.Count - 1; i != 0; i--) {
					volume.MergeSelf(edgesCore.Items[i].From.Position);
				}

				this.Volume = volume;
			}

			public override string ToString() {
				if (InRecursiveCall(this)) {
					return Jsonable.Fields(nameof(this.Area), this.Area, nameof(this.CW), this.CW, nameof(this.Volume), this.Volume);
				} else {
					try {
						EnterRecursiveCall(this);
						return Jsonable.Fields(nameof(this.Area), this.Area, nameof(this.CW), this.CW, nameof(this.Volume), this.Volume, nameof(this.UserData), this.UserData, nameof(this.Edges), this.Edges);
					} finally {
						LeaveRecursiveCall(this);
					}
				}
			}

			public string ToJsonString() {
				return this.ToString();
			}
		}

		/// <summary>
		/// 交点ノードに対する処理を行うデリゲート
		/// </summary>
		/// <param name="edge1">交差エッジ１</param>
		/// <param name="t1">エッジ１の交点パラメータ</param>
		/// <param name="edge2">交差エッジ２</param>
		/// <param name="t2">エッジ２の交点パラメータ</param>
		/// <param name="newNode">交点のノード</param>
		public delegate void IntersectionNodeProc(Edge edge1, element t1, Edge edge2, element t2, Node newNode);
		#endregion

		#region フィールド
		const element CalcEpsilon = (element)1.0e-20;
		static readonly HashSet<object> RecursiveCaller = new HashSet<object>();

		NodeManager _NodeMgr;
		EdgeManager _EdgeMgr;
		FList<FList<Polygon>> _Groups = new FList<FList<Polygon>>();
		FList<FList<EPolygon>> _TopoGroups = new FList<FList<EPolygon>>();
		element _Epsilon;
		IntersectionNodeProc _IntersectionNodeGenerator;
		Func<object, object> _UserDataCloner;
#if POLYGONBOOLEAN_DEBUG
		static bool _Logging;
#endif
		/// <summary>
		/// オブジェクトに紐づくユーザーデータ
		/// </summary>
		public object UserData;

		/// <summary>
		/// ユーザーが自由に設定＆使用できる値
		/// </summary>
		public ulong UserValue;
		#endregion

		#region プロパティ
		/// <summary>
		/// ノード一覧の取得
		/// </summary>
		public ICollection<Node> Nodes {
			get {
				return _NodeMgr.Items;
			}
		}

		/// <summary>
		/// エッジ一覧の取得
		/// </summary>
		public ICollection<Edge> Edges {
			get {
				return _EdgeMgr.Items;
			}
		}

		/// <summary>
		/// エッジ交点にノード挿入する際の交点ノード処理デリゲート
		/// </summary>
		public IntersectionNodeProc IntersectionNodeGenerator {
			get {
				return _IntersectionNodeGenerator;
			}

			set {
				_IntersectionNodeGenerator = value;
			}
		}

		/// <summary>
		/// 処理対象のポリゴングループ一覧
		/// </summary>
		public FList<FList<Polygon>> Groups {
			get {
				return _Groups;
			}
		}

		/// <summary>
		/// トポロジー構造生成後のポリゴングループ一覧
		/// </summary>
		public FList<FList<EPolygon>> TopoGroups {
			get {
				return _TopoGroups;
			}
		}

		/// <summary>
		/// ユーザーデータのクローンを作成するデリゲート
		/// </summary>
		public Func<object, object> UserDataCloner {
			get {
				return _UserDataCloner;
			}
			set {
				_UserDataCloner = value;
			}
		}
		#endregion

		#region 公開メソッド
		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="epsilon">頂点同士、エッジと頂点の距離の最小値、これより距離が近い場合には距離０として扱い同じノードになる、計算誤差対処のため必要</param>
		public PolBool2f(element epsilon) {
			_Epsilon = epsilon;
		}

		/// <summary>
		/// ポリゴンを登録する
		/// </summary>
		/// <param name="polygons">ポリゴン</param>
		/// <returns>追加されたポリゴンのインデックス</returns>
		public int AddPolygon(IEnumerable<Polygon> polygons) {
			var result = _Groups.Count;
			_Groups.Add(new FList<Polygon>(polygons));
			return result;
		}

		/// <summary>
		/// ポリゴンをノードとエッジに分解する、ポリゴン同士の交点にはノードが追加される
		/// </summary>
		/// <param name="validation">入力ポリゴンからトポロジー作成可能か調べるならtrue</param>
		/// <returns>全てのポリゴンを構成するエッジと方向の一覧</returns>
		public void CreateTopology(bool validation) {
#if POLBOOL_LOG_TIME
			GlobalLogger.AddLogLine("----CreateTopology start----");
			var sw = new System.Diagnostics.Stopwatch();
			sw.Start();
#endif
			// まず頂点分布範囲と頂点数を取得する
			var groupsCore = _Groups.Core;
			int allVertexCount = 0;
			var allVolume = range.InvalidValue;
			var groupWiseVertexCount = new int[groupsCore.Count];
			var groupWiseVolume = new range[groupsCore.Count];
			for (int i = groupsCore.Count - 1; i != -1; i--) {
				var groupCore = groupsCore.Items[i].Core;
				for (int j = groupCore.Count - 1; j != -1; j--) {
					var pol = groupCore.Items[j];
					var count = pol.VertexCount;
					var vol = pol.Volume;

					allVertexCount += count;
					allVolume.MergeSelf(vol);

					groupWiseVertexCount[i] = count;
					groupWiseVolume[i] = vol;
				}
			}
#if POLBOOL_LOG_TIME
			GlobalLogger.AddLogLine("Range search", sw.Elapsed.TotalMilliseconds.ToString());
#endif

			// ノード、エッジ管理クラスを作成
#if POLBOOL_LOG_TIME
			sw.Reset();
			sw.Start();
#endif
			var epsilon = _Epsilon;
			var division = GridSpace2f<Node>.CalcDivisionUniform(allVertexCount, 10); // 色々試した結果１セル辺り１０要素を前提にするのが速い
			allVolume.ExpandSelf(epsilon);
			_NodeMgr = new NodeManager(allVolume, division, epsilon);
			_EdgeMgr = new EdgeManager(allVolume, division, epsilon);
			_EdgeMgr.GroupCount = groupsCore.Count;
			_TopoGroups = new FList<FList<EPolygon>>();
#if POLBOOL_LOG_TIME
			GlobalLogger.AddLogLine("Create managers", sw.Elapsed.TotalMilliseconds.ToString(), "division", division.ToString());
			GlobalLogger.AddLogLine("allVolume", allVolume.ToString(), "allVertexCount", allVertexCount.ToString());
#endif

			// ポリゴンが使用可能か調べる
			if (validation) {
				for (int groupIndex = 0; groupIndex < groupsCore.Count; groupIndex++) {
					var groupCore = groupsCore.Items[groupIndex].Core;
					var lfinder = new LineFinder(
						groupWiseVolume[groupIndex].Expand(epsilon),
						GridSpace2f<LineWithId>.CalcDivisionUniform(groupWiseVertexCount[groupIndex]),
						epsilon);

					for (int polygonIndex = 0; polygonIndex < groupCore.Count; polygonIndex++) {
						var pol = groupCore.Items[polygonIndex];
						var result = pol.PrepareValidation(lfinder);
						if (!result.IsValid) {
							throw new Exception("グループ" + (groupIndex + 1) + "-ポリゴン" + (polygonIndex + 1) + " : " + result.Message);
						}
					}

					lfinder.Optimize();

					for (int polygonIndex = 0; polygonIndex < groupCore.Count; polygonIndex++) {
						var pol = groupCore.Items[polygonIndex];
						var result = pol.Validation(lfinder, epsilon);
						if (!result.IsValid) {
							throw new Exception("グループ" + (groupIndex + 1) + "-ポリゴン" + (polygonIndex + 1) + " : " + result.Message);
						}
					}
				}
			}

			// ポリゴンからノードとエッジを作成する
#if POLBOOL_LOG_TIME
			sw.Reset();
			sw.Start();
#endif
			MakeNodesAndEdges();
#if POLBOOL_LOG_TIME
			GlobalLogger.AddLogLine("MakeNodesAndEdges", sw.Elapsed.TotalMilliseconds.ToString());
#endif

			// 交点にノードを挿入する
#if POLBOOL_LOG_TIME
			sw.Reset();
			sw.Start();
#endif
			MakeIntersectionNodes();
#if POLBOOL_LOG_TIME
			GlobalLogger.AddLogLine("MakeIntersectionNodes", sw.Elapsed.TotalMilliseconds.ToString());
#endif

			// ヒゲを取り除く
#if POLBOOL_LOG_TIME
			sw.Reset();
			sw.Start();
#endif
			RemoveBeard();
#if POLBOOL_LOG_TIME
			GlobalLogger.AddLogLine("RemoveBeard", sw.Elapsed.TotalMilliseconds.ToString());
#endif

			// _TopologicalPolygons を再構成する
#if POLBOOL_LOG_TIME
			sw.Reset();
			sw.Start();
#endif
			RebuildTpols();
#if POLBOOL_LOG_TIME
			GlobalLogger.AddLogLine("RebuildTpols", sw.Elapsed.TotalMilliseconds.ToString());
#endif

			// ポリゴンを構成する
#if POLBOOL_LOG_TIME
			sw.Reset();
			sw.Start();
#endif
			PolygonizeAll();
#if POLBOOL_LOG_TIME
			GlobalLogger.AddLogLine("PolygonizeAll", sw.Elapsed.TotalMilliseconds.ToString());
#endif
		}

		/// <summary>
		/// エッジを共有しているグループインデックスを取得する
		/// </summary>
		/// <returns>グループインデックス配列</returns>
		public FList<int> GetEdgeSharedGroups() {
			var groups = new FList<int>();

			foreach (var edge in this.Edges) {
				var rp = edge.RightPolygons;
				var lp = edge.LeftPolygons;
				int groupCount = 0;

				for (int i = rp.Length - 1; i != -1; i--) {
					if (0 <= rp[i] || 0 <= lp[i]) {
						groupCount++;
					}
				}

				if (2 <= groupCount) {
					for (int i = rp.Length - 1; i != -1; i--) {
						if (0 <= rp[i] || 0 <= lp[i]) {
							if (!groups.Contains(i)) {
								groups.Add(i);
							}
						}
					}
				}
			}

			return groups;
		}

		/// <summary>
		/// ポリゴンを構成するエッジと方向の一覧からノード一覧を生成する
		/// </summary>
		/// <param name="edges">エッジと方向の一覧</param>
		/// <returns>ノード一覧</returns>
		public static FList<Node> NodesFromEdges(IEnumerable<EDir> edges) {
			return new FList<Node>(from r in edges select r.From);
		}

		/// <summary>
		/// 指定フィルタにマッチする弧線リストを取得する
		/// </summary>
		/// <param name="edges">方向付きのエッジリスト</param>
		/// <param name="matcher">マッチ判定デリゲート</param>
		/// <returns>弧線リスト</returns>
		public FList<IndexRange> MatchSegments(FList<EDir> edges, EdgeFilter matcher) {
			var list = new FList<IndexRange>();
			var edgesCore = edges.Core;

			// まずマッチしないエッジを探す
			int start = -1;
			for (int i = 0; i < edgesCore.Count; i++) {
				var e = edgesCore.Items[i];
				if (!matcher(this, e.Edge, e.TraceRight)) {
					start = i;
					break;
				}
			}
			if (start == -1)
				return list; // 見つからなかったら終了

			// マッチしないエッジとマッチするエッジの境目を探していく
			var lastMatched = false;
			var indexRange = new IndexRange(0, 0);
			for (int i = 1; i <= edgesCore.Count; i++) {
				var index = (start + i) % edgesCore.Count;
				var e = edgesCore.Items[index];
				var match = matcher(this, e.Edge, e.TraceRight);
				if (match) {
					if (!lastMatched) {
						indexRange.Start = index;
						indexRange.Count = 1;
					} else {
						indexRange.Count++;
					}
				} else {
					if (lastMatched) {
						list.Add(indexRange);
						indexRange = new IndexRange(0, 0);
					}
				}
				lastMatched = match;
			}

			return list;
		}

		/// <summary>
		/// 指定されたエッジフィルタをパスしたエッジからポリゴンを作成する
		/// </summary>
		/// <param name="edgeFilter">フィルタ、エッジと右方向かどうかを受け取り無視するなら true を返す</param>
		/// <param name="rightEnabledFlag">有効なポリゴンとして取得されるエッジに付与するフラグ</param>
		/// <param name="leftEnabledFlag">有効なポリゴンとして取得されるエッジに付与するフラグ</param>
		/// <returns>結果のポリゴンを構成するエッジと方向の一覧</returns>
		/// <remarks>事前に CreateTopology() を呼び出しておく必要がある。</remarks>
		public FList<EPolygon> Filtering(EdgeFilter edgeFilter, EdgeFlags rightEnabledFlag, EdgeFlags leftEnabledFlag) {
			var edges = new FList<EDir>();
			var polygons = new FList<FList<EDir>>();
			GetPolygons(edges, edgeFilter, EdgeFlags.RightRemoved, EdgeFlags.LeftRemoved, rightEnabledFlag, leftEnabledFlag, polygons);
			return Distinguish(polygons);
		}

		/// <summary>
		/// AddPolygon() で登録されたポリゴン同士のORポリゴンを作成する
		/// </summary>
		/// <returns>結果のポリゴンを構成するエッジと方向の一覧</returns>
		/// <remarks>事前に CreateTopology() を呼び出しておく必要がある。</remarks>
		public FList<EPolygon> Or() {
#if POLYGONBOOLEAN_DEBUG
			_Logging = true;
			System.Diagnostics.POLYGONBOOLEAN_DEBUG.WriteLine("======== Or ========");
#endif
			// エッジの両側にポリゴンが存在するなら無視するフィルタ
			var edgeFilter = new EdgeFilter(
				(pb, e, right) => {
					return 0 <= e.RightGroupMax && 0 <= e.LeftGroupMax;
				}
			);
#if POLYGONBOOLEAN_DEBUG
			try {
#endif
			return Filtering(edgeFilter, 0, 0);
#if POLYGONBOOLEAN_DEBUG
			} finally {
				_Logging = false;
			}
#endif
		}

		/// <summary>
		/// AddPolygon() で登録されたポリゴン同士のXORポリゴンを作成する
		/// </summary>
		/// <returns>結果のポリゴンを構成するエッジと方向の一覧</returns>
		/// <remarks>事前に CreateTopology() を呼び出しておく必要がある。</remarks>
		public FList<EPolygon> Xor() {
#if POLYGONBOOLEAN_DEBUG
			_Logging = true;
			System.Diagnostics.POLYGONBOOLEAN_DEBUG.WriteLine("======== Xor ========");
#endif
			// エッジの両側のポリゴン数が同じか、指定方向に偶数ポリゴンが存在するなら無視するフィルタ
			var edgeFilter = new EdgeFilter(
				(pb, e, right) => {
					var rc = e.RightGroupCount;
					var lc = e.LeftGroupCount;
					if (rc == lc)
						return true;
					return (right ? rc : lc) % 2 == 0;
				}
			);

#if POLYGONBOOLEAN_DEBUG
			try {
#endif
			return Filtering(edgeFilter, 0, 0);
#if POLYGONBOOLEAN_DEBUG
			} finally {
				_Logging = false;
			}
#endif
		}

		/// <summary>
		/// AddPolygon() で登録されたポリゴン同士のANDポリゴンを作成する
		/// </summary>
		/// <returns>結果のポリゴンを構成するエッジと方向の一覧</returns>
		/// <remarks>事前に CreateTopology() を呼び出しておく必要がある。</remarks>
		public FList<EPolygon> And() {
			// エッジの指定方向に登録ポリゴンの内一つでも存在しないなら無視するフィルタ
			var edgeFilter = new EdgeFilter(
				(pb, e, right) => {
					var p = right ? e.RightPolygons : e.LeftPolygons;
					for (int i = p.Length - 1; i != -1; i--) {
						if (p[i] < 0)
							return true;
					}
					return false;
				}
			);

			return Filtering(edgeFilter, 0, 0);
		}

		/// <summary>
		/// 指定されたインデックスのポリゴンを減算したポリゴンを作成する
		/// </summary>
		/// <param name="groupIndex">減算するグループのインデックス</param>
		/// <returns>結果のポリゴンを構成するエッジと方向の一覧</returns>
		/// <remarks>事前に CreateTopology() を呼び出しておく必要がある。</remarks>
		public FList<EPolygon> Sub(int groupIndex) {
			// エッジの指定方向に減算ポリゴンが存在するなら無視するフィルタ
			var edgeFilter = new EdgeFilter(
				(pb, e, right) => {
					return 0 <= (right ? e.RightPolygons : e.LeftPolygons)[groupIndex];
				}
			);
			return Filtering(edgeFilter, 0, 0);
		}

		/// <summary>
		/// 指定されたインデックスのポリゴンのみを抽出したポリゴンを作成する
		/// </summary>
		/// <param name="groupIndex">抽出するグループのインデックス</param>
		/// <returns>結果のポリゴンを構成するエッジと方向の一覧</returns>
		/// <remarks>事前に CreateTopology() を呼び出しておく必要がある。</remarks>
		public FList<EPolygon> Extract(int groupIndex) {
			return _TopoGroups[groupIndex];
		}
		#endregion

		#region 非公開メソッド
		/// <summary>
		/// 指定されたフィルタでポリゴンを取得する
		/// </summary>
		/// <param name="edges">使いまわすための EdgeDir リスト</param>
		/// <param name="edgeFilter">フィルタ</param>
		/// <param name="rightFlag">エッジの右側に付与する辿ったことを示すフラグ</param>
		/// <param name="leftFlag">エッジの左側に付与する辿ったことを示すフラグ</param>
		/// <param name="rightEnabledFlag">有効なポリゴンとして取得されるエッジに付与するフラグ</param>
		/// <param name="leftEnabledFlag">有効なポリゴンとして取得されるエッジに付与するフラグ</param>
		/// <param name="resultPolygons">ポリゴン一覧が返る</param>
		private void GetPolygons(FList<EDir> edges, EdgeFilter edgeFilter, EdgeFlags rightFlag, EdgeFlags leftFlag, EdgeFlags rightEnabledFlag, EdgeFlags leftEnabledFlag, FList<FList<EDir>> resultPolygons) {
			resultPolygons.Clear();

			// 予め無視することがわかっているエッジを処理
			var flagsnot = ~(rightFlag | leftFlag | rightEnabledFlag | leftEnabledFlag);
			foreach (var edge in this.Edges) {
				edge.Flags &= flagsnot;
				if (edgeFilter(this, edge, true))
					edge.Flags |= rightFlag;
				if (edgeFilter(this, edge, false))
					edge.Flags |= leftFlag;
			}

			// 右側にまだポリゴンが存在するエッジのみ処理する
			foreach (var edge in this.Edges) {
				if ((edge.Flags & rightFlag) == 0) {
					// ポリゴンを構成するエッジと方向一覧を取得
					// 結果のポリゴン一覧に追加
					if (TracePolygon(edge, true, false, rightFlag, leftFlag, rightEnabledFlag, leftEnabledFlag, edges))
						resultPolygons.Add(new FList<EDir>(edges));
				}
			}

			// 左側にまだポリゴンが存在するエッジのみ処理する
			foreach (var edge in this.Edges) {
				if ((edge.Flags & leftFlag) == 0) {
					// ポリゴンを構成するエッジと方向一覧を取得
					// 結果のポリゴン一覧に追加
					if (TracePolygon(edge, false, false, rightFlag, leftFlag, rightEnabledFlag, leftEnabledFlag, edges))
						resultPolygons.Add(new FList<EDir>(edges));
				}
			}
		}

		/// <summary>
		/// 全エッジのうちポリゴンを構成できるところを全て構成する
		/// </summary>
		/// <returns>全てのポリゴンを構成するエッジと方向の一覧</returns>
		private void PolygonizeAll() {
			// 作成できるループを全て取得する
			var edges = new FList<EDir>();
			var loops = new FList<ELoop>();
			foreach (var edge in this.Edges) {
				if ((edge.Flags & EdgeFlags.RightPolygonized) == 0) {
					if (TracePolygon(edge, true, false, EdgeFlags.RightPolygonized, EdgeFlags.LeftPolygonized, 0, 0, edges)) {
						loops.Add(new ELoop(new FList<EDir>(edges)));
					}
				}
			}
			foreach (var edge in this.Edges) {
				if ((edge.Flags & EdgeFlags.LeftPolygonized) == 0) {
					if (TracePolygon(edge, false, false, EdgeFlags.RightPolygonized, EdgeFlags.LeftPolygonized, 0, 0, edges)) {
						loops.Add(new ELoop(new FList<EDir>(edges)));
					}
				}
			}

			// ループがリンクしているポリゴンを統一する
			var groupPolygons = new HashSet<ulong>();
			var loopsCore = loops.Core;
			for (int iloop = loopsCore.Count - 1; iloop != -1; iloop--) {
				// エッジの指定方向にあるグループ＆ポリゴン一覧を作成
				groupPolygons.Clear();
				var edgesCore = loopsCore.Items[iloop].Edges.Core;
				for (int i = edgesCore.Count - 1; i != -1; i--) {
					var eas = edgesCore.Items[i];
					var polygons = eas.TraceRight ? eas.Edge.RightPolygons : eas.Edge.LeftPolygons;
					for (int j = polygons.Length - 1; j != -1; j--) {
						var p = polygons[j];
						if (0 <= p) {
							groupPolygons.Add((ulong)j << 32 | (ulong)(uint)p);
						}
					}
				}

				// ポリゴンを構成するエッジのポリゴン情報を統一する
				for (int i = edgesCore.Count - 1; i != -1; i--) {
					var eas = edgesCore.Items[i];
					foreach (var gp in groupPolygons) {
						var g = (int)(gp >> 32);
						var p = (int)(gp & 0xffffffff);
						eas.Edge.LinkPolygon(eas.TraceRight, g, p);
					}
				}
			}
		}

		/// <summary>
		/// 指定されたエッジの指定側を辿りポリゴンを構成するエッジ一覧を取得する
		/// </summary>
		/// <param name="edge">開始エッジ、このポリゴンの右側を辿る</param>
		/// <param name="traceRight">エッジの右側を辿るなら true 、左側を辿るなら false</param>
		/// <param name="traceCCW">true なら最も反時計回り側のエッジを辿る、false なら最も時計回り側のエッジを辿る</param>
		/// <param name="rightFlag">エッジの右側に付与する辿ったことを示すフラグ</param>
		/// <param name="leftFlag">エッジの左側に付与する辿ったことを示すフラグ</param>
		/// <param name="rightEnabledFlag">有効なポリゴンとして取得されるエッジに付与するフラグ</param>
		/// <param name="leftEnabledFlag">有効なポリゴンとして取得されるエッジに付与するフラグ</param>
		/// <param name="resultEdges">結果のエッジ一覧が返る</param>
		/// <returns>指定された側にポリゴン形成できたなら true が返る</returns>
		private static bool TracePolygon(Edge edge, bool traceRight, bool traceCCW, EdgeFlags rightFlag, EdgeFlags leftFlag, EdgeFlags rightEnabledFlag, EdgeFlags leftEnabledFlag, FList<EDir> resultEdges) {
			var startEdge = edge;
			var startNode = traceRight ? edge.From : edge.To; // ポリゴンの開始ノード
			var nextNode = traceRight ? edge.To : edge.From;
			var curNode = startNode;
			var curIsRight = traceRight; // エッジの右側を辿るなら true、左側なら false
			var isNull = true;
			var rightFlagAndGotFlag = rightFlag;
			var leftFlagAndGotFlag = leftFlag;
			var enabledFlags = rightEnabledFlag | leftEnabledFlag;

			if (enabledFlags != 0) {
				rightFlagAndGotFlag |= rightEnabledFlag;
				leftFlagAndGotFlag |= leftEnabledFlag;
			}

			resultEdges.Clear();

#if POLYGONBOOLEAN_DEBUG
			if(_Logging) {
				System.Diagnostics.POLYGONBOOLEAN_DEBUG.WriteLine("==== " + (traceRight ? "Right" : "Left") + " ====");
			}
#endif

			while (true) {
				// ポリゴンを構成するエッジとして方向と共に登録
				resultEdges.Add(new EDir(edge, curIsRight));
				edge.Flags |= curIsRight ? rightFlagAndGotFlag : leftFlagAndGotFlag;

#if POLYGONBOOLEAN_DEBUG
				if (_Logging) {
					System.Diagnostics.POLYGONBOOLEAN_DEBUG.WriteLine(list[list.Count - 1].ToString() + " : " + (curIsRight ? edge.Right : edge.Left).Count);
				}
#endif

				// 指定側に１つでもポリゴンが存在すればポリゴンを形成できる
				if (isNull && 0 <= (curIsRight ? edge.RightGroupMax : edge.LeftGroupMax))
					isNull = false;

				// エッジのベクトルを計算
				var nextNodePos = nextNode.Position;
				var vec1 = curNode.Position - nextNodePos;
				var vec1v = vec1.RightAngle();
				var vecLen1 = edge.Length;

				if (traceCCW)
					vec1v = -vec1v;

				// 次のエッジを探す
				// 指定方向のノードに接続されたエッジで且つエッジ同士のなす右回りの角度が最も小さい又は大きいものを選ぶ
				var nextEdgesCore = nextNode.Edges.Core;
				Edge nextEdge = null;
				Node nextNextNode = null;
				var nextIsRight = true;
				var maxCos = element.MinValue;

				for (int i = nextEdgesCore.Count - 1; i != -1; i--) {
					var e = nextEdgesCore.Items[i];
					if (e == edge)
						continue;
					var right = e.From == nextNode;
					if (e != startEdge) {
						if ((e.Flags & (right ? rightFlag : leftFlag)) != 0)
							continue;
					}
					var n2node = right ? e.To : e.From;
					var vec2 = n2node.Position - nextNodePos;
					var vecLen2 = e.Length;
					var cos = vec1.Dot(vec2) / (vecLen1 * vecLen2);
					if (0 <= vec1v.Dot(vec2)) {
						cos += 1;
					} else {
						cos = -1 - cos;
					}

					if (maxCos < cos) {
						nextEdge = e;
						nextNextNode = n2node;
						nextIsRight = right;
						maxCos = cos;
					}
				}

				if (nextEdge == startEdge) {
					if (resultEdges.Count < 3)
						isNull = true;
					break;
				}
				if (nextEdge == null) {
#if POLYGONBOOLEAN_DEBUG
					if (_Logging) {
						System.Diagnostics.POLYGONBOOLEAN_DEBUG.WriteLine("ヒゲ");
					}
#endif
					isNull = true;
					break;
				}

				// 次回の準備
				edge = nextEdge;
				curNode = nextNode;
				nextNode = nextNextNode;
				curIsRight = nextIsRight;
			}

#if POLYGONBOOLEAN_DEBUG
			if (_Logging) {
				if (isNull)
					System.Diagnostics.POLYGONBOOLEAN_DEBUG.WriteLine("Is null");
			}
#endif
			if (isNull && enabledFlags != 0) {
				rightEnabledFlag = ~rightEnabledFlag;
				leftEnabledFlag = ~leftEnabledFlag;
				var core = resultEdges.Core;
				for (int i = core.Count - 1; i != -1; i--) {
					var ed = core.Items[i];
					ed.Edge.Flags &= ed.TraceRight ? rightEnabledFlag : leftEnabledFlag;
				}
			}

			return !isNull;
		}

		/// <summary>
		/// AddPolygon() で追加されたポリゴンをノードとエッジに分解する
		/// </summary>
		private void MakeNodesAndEdges() {
			var nm = _NodeMgr;
			var em = _EdgeMgr;
			var groupsCore = _Groups.Core;
			for (int groupIndex = 0; groupIndex < groupsCore.Count; groupIndex++) {
				var groupCore = groupsCore.Items[groupIndex].Core;
				var tpols = new FList<EPolygon>();
				for (int polygonIndex = 0; polygonIndex < groupCore.Count; polygonIndex++) {
					var pol = groupCore.Items[polygonIndex];
					var tpol = new EPolygon();
					pol.GroupIndex = groupIndex;
					pol.PolygonIndex = polygonIndex;
					tpol.GroupIndex = groupIndex;
					tpol.PolygonIndex = polygonIndex;

					foreach (var loop in pol.Loops) {
						// 頂点をノードに変換する、半径 _Epsilon を使いノードの接触を調べ、接触しているなら既存のノードを使用する
						var verticesCore = loop.Vertices.Core;
						var nodes = new Node[verticesCore.Count];
						for (int i = 0; i < verticesCore.Count; i++) {
							var v = verticesCore.Items[i];
							var node = _NodeMgr.New(v.Position);
							node.SetUserData(groupIndex, v.UserData);
							nodes[i] = node;
						}

						// ラインをエッジに変換する、既存エッジに同じノード組み合わせのものが存在したらそちらを使用する
						var edges = new FList<EDir>(verticesCore.Count);
						var node1 = nodes[0];
						for (int i = 1; i <= verticesCore.Count; i++) {
							var node2 = nodes[i % verticesCore.Count];
							var edge = _EdgeMgr.New(node1, node2);
							var right = node1 == edge.From;
							edge.LinkPolygon(right, groupIndex, polygonIndex);
							if (loop.EdgesUserData != null)
								edge.SetUserData(right, groupIndex, loop.EdgesUserData[i - 1]);
							edges.Add(new EDir(edge, right));
							node1 = node2;
						}
						tpol.Loops.Add(new ELoop(edges));
					}

					tpols.Add(tpol);
				}
				_TopoGroups.Add(tpols);
			}
			nm.Optimize();
			em.Optimize();
		}

		/// <summary>
		/// エッジとノードの接触、エッジ同士の交点部分にノードを挿入する
		/// </summary>
		private void MakeIntersectionNodes() {
			// エッジ同士の交点を調べ、ノード挿入情報を作成する
			InsertIntersectionNodeToEdge();

			// 作成されたノード挿入情報を基にノードを挿入する
			EdgeDivide();
		}

		/// <summary>
		/// 全エッジの交差を調べ交差しているならエッジにノード挿入予約を行う
		/// </summary>
		private void InsertIntersectionNodeToEdge() {
			var comb = new HashSet<ulong>(); // 同じ組み合わせのチェックを排除するためのテーブル

			// 全エッジをチェックし、エッジ上にノードがあったら挿入予約を行う
			// 挿入予約を行ったノードから伸びるエッジは交差判定無視リストに登録する　※これを行わないと位相構造が壊れる
			var epsilon = _Epsilon;
			var epsilon2 = epsilon * epsilon;
			var edges = this.Edges;
			foreach (var edge in this.Edges) {
				var p1 = edge.From.Position;
				var p2 = edge.To.Position;
				var v = p2 - p1;

				// エッジに接触する可能性があるノード探す
				var vol = edge.GetVolume();
				vol.Extents += _Epsilon;
				foreach (var node in _NodeMgr.Query(vol)) {
					// ノードが現在処理中のエッジに繋がっていたらスキップ
					if (edge.From == node || edge.To == node)
						continue;

					// ノードとの線分最近点のパラメータを計算
					var t = LinePointNearestParam(p1, v, node.Position);

					// 線分の範囲外ならスキップ
					if (t < 0 || 1 < t)
						continue;

					// 最近点とノードとの距離がノード半径を超えていたらスキップ
					var c = p1 + v * t;
					if (epsilon2 < (node.Position - c).LengthSquare)
						continue;

					// 計算した座標がエッジ両端のノードに接触するならスキップ
					if ((c - p1).LengthSquare <= epsilon2 || (c - p2).LengthSquare <= epsilon2)
						continue;

					// 挿入するノードとして登録
					edge.SetNodeInsertion(t, node);

					// ノードにつながるエッジを交差判定無視リストに登録する
					var nodeEdgesCore = node.Edges.Core;
					for (int i = nodeEdgesCore.Count - 1; i != -1; i--) {
						comb.Add(Edge.GetCombID(edge, nodeEdgesCore.Items[i]));
					}
				}
			}

			// エッジ同士の交差判定を行い、交差しているならノード挿入予約を行う
			var ing = _IntersectionNodeGenerator;
			foreach (var edge1 in this.Edges) {
				ulong uidx1 = edge1.UniqueIndex;

				// エッジから線分の情報取得
				var from1 = edge1.From;
				var to1 = edge1.To;
				var p1 = from1.Position;
				var v1 = to1.Position - p1;

				// エッジに接触する可能性があるエッジ探す
				var vol = edge1.GetVolume();
				vol.Extents += _Epsilon;
				foreach (var edge2 in _EdgeMgr.Query(vol)) {
					ulong uidx2 = edge2.UniqueIndex;

					// すでにチェック済みの組み合わせならスキップ
					var combid = uidx1 <= uidx2 ? uidx2 << 32 | uidx1 : uidx1 << 32 | uidx2;
					if (comb.Contains(combid))
						continue;
					comb.Add(combid);

					// 位相構造上交差するはずが無いならスキップ
					if (!edge1.IsCrossable(edge2))
						continue;

					// エッジから線分の情報取得
					var from2 = edge2.From;
					var to2 = edge2.To;
					var p2 = from2.Position;
					var v2 = to2.Position - p2;

					// 交点のパラメータを計算、交点がノードと重なるなら交差してないことにする
					var divisor = geo.LineIntersectDivisor(v1, v2);
					if (Math.Abs(divisor) <= CalcEpsilon)
						continue;
					var pv = p2 - p1;
					var t1 = geo.LineIntersectParam(pv, v1, v2, divisor);
					if (t1 <= 0 || 1 <= t1)
						continue;
					var t2 = geo.LineIntersectParam(pv, v2, v1, divisor);
					if (t2 <= 0 || 1 <= t2)
						continue;

					// 交点座標計算
					var position = p1 + v1 * t1;

					// 交点座標のノードを作成
					var node = _NodeMgr.New(position);
					var edge1touch = from1 == node || to1 == node;
					var edge2touch = from2 == node || to2 == node;
					if (edge1touch && edge2touch)
						continue; // 両方のエッジ両端ノードに接触するなら挿入はキャンセル

					// ノードデータ生成デリゲートがあったら処理する
					if (ing != null)
						ing(edge1, t1, edge2, t2, node);

					// このノードは内外が入れ替わるノード
					// ノード挿入予約
					if (!edge1touch)
						edge1.SetNodeInsertion(t1, node);
					if (!edge2touch)
						edge2.SetNodeInsertion(t2, node);
				}
			}
		}

		/// <summary>
		/// エッジのノード挿入情報を基にノードを挿入する
		/// </summary>
		private void EdgeDivide() {
			var userDataCloner = _UserDataCloner;
			var comparer = new NodeInsertion.ParameterComparer();

			// 現時点での全エッジを対象に処理する
			var edges = new FList<Edge>(this.Edges);
			var edgesCore = edges.Core;
			for (int iedge = edgesCore.Count - 1; iedge != -1; iedge--) {
				var edge = edgesCore.Items[iedge];
				var nis = edge.NodeInsertions;
				if (nis == null)
					continue;

				// 線分のパラメータで昇順にソート
				nis.Sort(comparer);

				// エッジを作成していく
				var node1 = edge.From;
				var nisCore = nis.Core;
				for (int i = nisCore.Count - 1; i != -1; i--) {
					var ni = nisCore.Items[i];
					var newEdge = _EdgeMgr.New(node1, ni.Node);

					newEdge.CopyAttributes(edge, node1 == newEdge.From, userDataCloner);

					node1 = ni.Node;
				}

				var newEdge2 = _EdgeMgr.New(node1, edge.To);
				newEdge2.CopyAttributes(edge, node1 == newEdge2.From, userDataCloner);

				// ノード挿入情報をクリア
				edge.NodeInsertions = null;

				// 元のエッジ削除登録
				_EdgeMgr.Remove(edge);
			}
		}

		/// <summary>
		/// ヒゲを取り除く
		/// </summary>
		private void RemoveBeard() {
			var nodes = new FList<Node>();
			var nm = _NodeMgr;
			var em = _EdgeMgr;
			for (;;) {
				// リンク数が１のノードを収集する
				nodes.AddRange(from n in this.Nodes where n.Edges.Count == 1 select n);
				var nodesCore = nodes.Core;
				if (nodesCore.Count == 0)
					break;

				// ノードとリンクしているエッジを取り除く
				for (int i = nodesCore.Count - 1; i != -1; i--) {
					var node = nodesCore.Items[i];
					nm.Remove(node);
					if (node.Edges.Count != 0)
						em.Remove(node.Edges[0]);
				}
				nodes.Clear();
			}
		}

		/// <summary>
		/// ノードとエッジに分解後のデータを使い<see cref="_TopoGroups"/>を再構成する
		/// </summary>
		/// <remarks>先に<see cref="MakeNodesAndEdges"/>、<see cref="MakeIntersectionNodes"/>を呼び出しておく必要がある</remarks>
		private void RebuildTpols() {
#if POLYGONBOOLEAN_DEBUG
			_Logging = true;
#endif
			var inGroupsCore = _Groups.Core;
			var topoGroupsCore = _TopoGroups.Core;
			var edges = new FList<EDir>();
			var tmpEPolygonEdges = new FList<FList<EDir>>();

			for (int igroup = topoGroupsCore.Count - 1; igroup != -1; igroup--) {
				var inGroupCore = inGroupsCore.Items[igroup].Core;
				var epols = topoGroupsCore.Items[igroup];
				var groupIndex = igroup;
				epols.Clear();

				for (int ipolygon = inGroupCore.Count - 1; ipolygon != -1; ipolygon--) {
					var polygonIndex = ipolygon;

					// エッジの指定方向に指定ポリゴンが存在しないなら無視するフィルタ
					var edgeFilter = new EdgeFilter(
						(pb, e, right) => {
							return (right ? e.RightPolygons : e.LeftPolygons)[groupIndex] != polygonIndex;
						}
					);

					// トポロジー構造から目的のポリゴンだけ抽出する
					GetPolygons(edges, edgeFilter, EdgeFlags.RightRemoved, EdgeFlags.LeftRemoved, 0, 0, tmpEPolygonEdges);
					var epolygonsCore = Distinguish(tmpEPolygonEdges).Core;
					for (int iepolygon = epolygonsCore.Count - 1; iepolygon != -1; iepolygon--) {
						var epolygon = epolygonsCore.Items[iepolygon];
						epolygon.GroupIndex = igroup;
						epolygon.PolygonIndex = ipolygon;
					}
					epols.AddRange(epolygonsCore);
				}
			}

			// 他グループに包含されているエッジがあれば包含しているポリゴンをリンクする
			// エッジの両側に包含ポリゴンをリンクする
			// ※ポリゴンを構成するエッジが共有されているなら PolygonizeAll() によりリンクされるので必要ない
			for (int groupIndex1 = topoGroupsCore.Count - 1; groupIndex1 != -1; groupIndex1--) {
				var tpols1Core = topoGroupsCore.Items[groupIndex1].Core;
				for (int polygonIndex1 = tpols1Core.Count - 1; polygonIndex1 != -1; polygonIndex1--) {
					var tpol1 = tpols1Core.Items[polygonIndex1];

					for (int groupIndex2 = topoGroupsCore.Count - 1; groupIndex2 != -1; groupIndex2--) {
#if !INTERSECT_SELF
						if (groupIndex2 == groupIndex1)
							continue; // 同グループ内なら交差しない
#endif

						var tpols2Core = topoGroupsCore.Items[groupIndex2].Core;
						for (int polygonIndex2 = tpols2Core.Count - 1; polygonIndex2 != -1; polygonIndex2--) {
#if INTERSECT_SELF
							if (groupIndex2 == groupIndex1 && polygonIndex2 == polygonIndex1)
								continue; // 同グループかつ同ポリゴンなら交差しない
#endif
							tpol1.LinkPolygonIfContainsEdge(tpols2Core.Items[polygonIndex2]);
						}
					}
				}
			}

#if POLYGONBOOLEAN_DEBUG
			_Logging = false;
#endif
		}

		/// <summary>
		/// 指定されたエッジによるポリゴン配列を外枠ポリゴンと穴に区別する
		/// </summary>
		/// <param name="edges">エッジによるポリゴン配列</param>
		/// <returns>エッジによるポリゴン配列</returns>
		/// <remarks>外枠ポリゴンは時計回り、穴は反時計回りとなる</remarks>
		private static FList<EPolygon> Distinguish(FList<FList<EDir>> edges) {
			var edgesCore = edges.Core;

			// まず面積を求め、面積降順に並び替える
			var loops = new ELoop[edgesCore.Count];
			for (int i = loops.Length - 1; i != -1; i--) {
				var e = edgesCore.Items[i];
				loops[i] = new ELoop(Area(e.Core), e);
			}
			Array.Sort(loops, (a, b) => {
				if (b.Area == a.Area) {
					if (a.CW == b.CW)
						return 0;
					return a.CW ? 1 : -1; // 同じ面積でも時計回りの方を小さくする、穴同士で親子関係にならないようにしなければならない
				} else {
					return Math.Sign(b.Area - a.Area);
				}
			});

			// 親子関係を調べる
			var parentIndices = new int[loops.Length];
			var nodeHashes = new HashSet<Node>[loops.Length];
			var edgeHashes = new HashSet<Edge>[loops.Length];
			for (int i = loops.Length - 1; i != -1; i--) {
				// 子を取得
				var child = loops[i];
				var childVolume = child.Volume;
				var childEdgesCore = child.Edges.Core;

				// 親を探す
				parentIndices[i] = -1;
				for (int j = i - 1; j != -1; j--) {
					var parent = loops[j];

					// TODO: 同じ座標でも時計回りが子になるようにしてあるのでこのチェックいらないはず、どういう理由でこの処理入れたのか忘れた・・・
					//// 面積が同じなら穴なので親にはできない、
					//// ポリゴン同士は重複あり得ないので親は存在しないことになる
					//if (child.Area == parent.Area)
					//	continue;

					// 親境界ボリュームが子境界ボリュームを包含していないならポリゴン包含はあり得ない
					if (!parent.Volume.Contains(childVolume))
						continue;

					// 親のノードがハッシュに登録されてなかったら登録する
					var parentEdgesCore = parent.Edges.Core;
					var parentNodes = nodeHashes[j];
					if (parentNodes == null) {
						nodeHashes[j] = parentNodes = new HashSet<Node>();
						for (int k = parentEdgesCore.Count - 1; k != -1; k--) {
							parentNodes.Add(parentEdgesCore.Items[k].From);
						}
					}

					// 子と親が共用しているノードを1とし、それ以外を0とする
					// 0→1へ変化した際の0と1→0へ変化した際の0がポリゴンに包含されているか調べる
					// 0↔1の変化が無い場合には適当に選んだノードの包含を調べる
					var intersects = false;
					var parentContainsChild = false;
					var node1 = childEdgesCore.Items[0].From;
					var type1 = parentNodes.Contains(node1); // 子ノードが親ノードに含まれているなら true になる
					var allNodesShared = true; // 全ノードを親と共有しているかどうか
					for (int k = childEdgesCore.Count - 1; k != -1; k--) {
						//if (type1 && (node1.Flags & NodeFlags.InsideOutside) != 0) {
						//	intersects = true;
						//	break; // TODO: 貫通フラグ見てるけど計算ポリゴン数２つだからこそできてる
						//}

						var node2 = childEdgesCore.Items[k].From;
						var type2 = parentNodes.Contains(node2); // 子ノードが親ノードに含まれているなら true になる
						if (type1 != type2) {
							intersects = true;
							if (PointTouchPolygon2(type1 ? node2.Position : node1.Position, parentEdgesCore, true) != 0) {
								parentContainsChild = true;
								break;
							}
						}
						node1 = node2;
						type1 = type2;

						if (!type2) {
							allNodesShared = false;
						}
					}
					if (allNodesShared) {
						// 全ノードを親と共有しているなら判定に工夫が必要になる
						// まず全エッジを親共有しているか調べ、共有していないエッジがあればその中心座標のポリゴン接触判定行う
						// 全エッジを共有しているならエッジ数が同じならポリゴン全体が親ポリゴンに含まれている、同じでないなら含まれていない

						// 親のエッジがハッシュに登録されてなかったら登録する
						var parentEdgesHash = edgeHashes[j];
						if (parentEdgesHash == null) {
							edgeHashes[j] = parentEdgesHash = new HashSet<Edge>();
							for (int k = parentEdgesCore.Count - 1; k != -1; k--) {
								parentEdgesHash.Add(parentEdgesCore.Items[k].Edge);
							}
						}

						// エッジの共有判定
						var allEdgesShared = true;
						for (int k = childEdgesCore.Count - 1; k != -1; k--) {
							var edge = childEdgesCore.Items[k].Edge;
							if (!parentEdgesHash.Contains(edge)) {
								allEdgesShared = false;
								parentContainsChild = PointTouchPolygon2((edge.From.Position + edge.To.Position) * 0.5f, parentEdgesCore, true) != 0;
								break;
							}
						}
						if (allEdgesShared) {
							parentContainsChild = childEdgesCore.Count == parentEdgesCore.Count;
						}
					} else {
						if (!intersects) {
							// 適当に選んだノードの包含を調べる
							if (PointTouchPolygon2(node1.Position, parentEdgesCore, true) != 0) {
								parentContainsChild = true;
							}
						}
					}
					if (parentContainsChild) {
						parentIndices[i] = j;
						break;
					}
				}
			}

			// 親子関係を組んだリストを作成
			var result = new FList<EPolygon>();
			var used = new bool[loops.Length];
			for (int i = 0, n = loops.Length; i < n; i++) {
				if (used[i])
					continue;

				var list = new FList<ELoop>();

				// 親を取得
				var parent = loops[i];
				if (!parent.CW) {
					var parentEdges = parent.Edges;
					parentEdges.Reverse();
					var parentEdgesCore = parentEdges.Core;
					for (int j = parentEdgesCore.Count - 1; j != -1; j--) {
						parentEdgesCore.Items[j].TraceRight = !parentEdgesCore.Items[j].TraceRight;
					}
					parent.CW = true;
				}
				list.Add(parent);
				used[i] = true;

				// 子（穴）を取得
				// 穴になるのは直接の子のみ
				for (int j = i + 1; j < n; j++) {
					if (used[j] || parentIndices[j] != i)
						continue;

					var child = loops[j];
					if (child.CW) {
						var childEdges = child.Edges;
						childEdges.Reverse();
						var childEdgesCore = childEdges.Core;
						for (int k = childEdgesCore.Count - 1; k != -1; k--) {
							var e = childEdgesCore.Items[k];
							e.TraceRight = !e.TraceRight;
							childEdgesCore.Items[k] = e;
						}
						child.CW = false;
					}
					list.Add(child);
					used[j] = true;
				}

				result.Add(new EPolygon(list));
			}

			return result;
		}

		/// <summary>
		/// pを開始点としvを方向ベクトルとする線分と点cとの最近点の線分パラメータを計算する
		/// </summary>
		/// <param name="p">[in] 線分の開始点</param>
		/// <param name="v">[in] 線分の方向ベクトル</param>
		/// <param name="c">[in] 最近点を調べる点c</param>
		/// <returns>線分のパラメータ</returns>
		private static element LinePointNearestParam(vector p, vector v, vector c) {
			return (c - p).Dot(v) / v.LengthSquare;
		}

		/// <summary>
		/// ２次元線分同士が交差しているか調べる、交点のパラメータは計算しない（整数ベクトル使用可）
		/// </summary>
		/// <param name="s1">[in] 線分１の開始点</param>
		/// <param name="e1">[in] 線分１の終了点</param>
		/// <param name="s2">[in] 線分２の開始点</param>
		/// <param name="e2">[in] 線分２の終了点</param>
		/// <returns>交差しているなら true が返る</returns>
		private static bool LineIntersect(vector s1, vector e1, vector s2, vector e2) {
			var v = s1 - e1;
			var ox = s2.Y - s1.Y;
			var oy = s1.X - s2.X;
			if (0 <= (v.X * ox + v.Y * oy) * (v.X * (e2.Y - s1.Y) + v.Y * (s1.X - e2.X)))
				return false;
			v = s2 - e2;
			if (0 <= -(v.X * ox + v.Y * oy) * (v.X * (e1.Y - s2.Y) + v.Y * (s2.X - e1.X)))
				return false;
			return true;
		}

		/// <summary>
		/// 多角形の面積と回り方向を計算する
		/// </summary>
		/// <param name="verticesCore">[in] 頂点配列とカウント</param>
		/// <returns>多角形の面積が返る、ポリゴンが反時計回りなら正数、時計回りなら負数となる</returns>
		private static element Area(FList<Vertex>.CoreElement verticesCore) {
			int i;
			var p1 = verticesCore.Items[0].Position;
			element s = 0;
			for (i = 1; i <= verticesCore.Count; i++) {
				var p2 = verticesCore.Items[i % verticesCore.Count].Position;
				s += (p1.X - p2.X) * (p1.Y + p2.Y);
				p1 = p2;
			}
			return s / 2;
		}

		/// <summary>
		/// 多角形の面積と回り方向を計算する
		/// </summary>
		/// <param name="edgesCore">[in] エッジ配列とカウント</param>
		/// <returns>多角形の面積が返る、ポリゴンが反時計回りなら正数、時計回りなら負数となる</returns>
		private static element Area(FList<EDir>.CoreElement edgesCore) {
			if (edgesCore.Count == 0)
				return 0;
			int i;
			var p1 = edgesCore.Items[0].From.Position;
			element s = 0;
			for (i = 1; i <= edgesCore.Count; i++) {
				var p2 = edgesCore.Items[i % edgesCore.Count].From.Position;
				s += (p1.X - p2.X) * (p1.Y + p2.Y);
				p1 = p2;
			}
			return s / 2;
		}

		/// <summary>
		/// 点が２次元多角形の内にあるか調べる、辺と点上の座標は接触しているとみなされない
		/// </summary>
		/// <param name="c">[in] 点の座標</param>
		/// <param name="verticesCore">[in] 要点配列とカウント</param>
		/// <param name="close">[in] 多角形の始点と終点を閉じて判定をする場合はtrueを指定する</param>
		/// <returns>点が多角形内にあるならtrueが返る</returns>
		private static bool PointInPolygon2(vector c, FList<Vertex>.CoreElement verticesCore, bool close = false) {
			int i, j = 0;
			int n = close ? verticesCore.Count + 1 : verticesCore.Count;
			var p1 = verticesCore.Items[0].Position;
			var x1 = c.X - p1.X;
			element zero = 0;
			for (i = 1; i < n; i++) {
				var p2 = verticesCore.Items[close ? i % verticesCore.Count : i].Position;
				var x2 = c.X - p2.X;
				if ((x1 < zero && zero <= x2) || (zero <= x1 && x2 < zero))
					j += x1 * (p2.Y - p1.Y) < (c.Y - p1.Y) * (p2.X - p1.X) ? -1 : 1;
				p1 = p2;
				x1 = x2;
			}
			return j != 0;
		}

		/// <summary>
		/// 点が２次元多角形の内にあるか調べる、辺と点上の座標は接触しているとみなされない
		/// </summary>
		/// <param name="c">[in] 点の座標</param>
		/// <param name="edgesCore">[in] エッジ配列とカウント</param>
		/// <param name="close">[in] 多角形の始点と終点を閉じて判定をする場合はtrueを指定する</param>
		/// <returns>点が多角形内にあるならtrueが返る</returns>
		private static bool PointInPolygon2(vector c, FList<EDir>.CoreElement edgesCore, bool close = false) {
			int i, j = 0;
			int n = close ? edgesCore.Count + 1 : edgesCore.Count;
			var p1 = edgesCore.Items[0].From.Position;
			var x1 = c.X - p1.X;
			element zero = 0;
			for (i = 1; i < n; i++) {
				var p2 = edgesCore.Items[close ? i % edgesCore.Count : i].From.Position;
				var x2 = c.X - p2.X;
				if ((x1 < zero && zero <= x2) || (zero <= x1 && x2 < zero))
					j += x1 * (p2.Y - p1.Y) < (c.Y - p1.Y) * (p2.X - p1.X) ? -1 : 1;
				p1 = p2;
				x1 = x2;
			}
			return j != 0;
		}

		/// <summary>
		/// 点が２次元多角形に接触しているか調べる、辺と点上の座標は接触しているとみなす
		/// </summary>
		/// <param name="c">[in] 点の座標</param>
		/// <param name="edgesCore">エッジ配列とカウント</param>
		/// <param name="close">[in] 多角形の始点と終点を閉じて判定をする場合は０以外を指定する</param>
		/// <returns>点が多角形内にあるなら2、辺または頂点上にあるなら1、それ以外なら0が返る</returns>
		private static int PointTouchPolygon2(vector c, FList<EDir>.CoreElement edgesCore, bool close = false) {
			int i, j = 0;
			int n = close ? edgesCore.Count + 1 : edgesCore.Count;
			var p1 = edgesCore.Items[0].From.Position;
			var y1 = c.Y - p1.Y;
			element zero = 0;
			for (i = 1; i < n; i++) {
				var p2 = edgesCore.Items[close ? i % edgesCore.Count : i].From.Position;
				var y2 = c.Y - p2.Y;
				if ((y1 < zero && zero <= y2) || (zero <= y1 && y2 < zero)) {
					var rx = c.X - p1.X;
					var dy = p2.Y - p1.Y;
					var dx = p2.X - p1.X;
					var t1 = y1 * dx;
					var t2 = rx * dy;
					if (t1 == t2)
						return 1;
					j += t1 < t2 ? -1 : 1;
				} else if (y1 == zero && y2 == zero) {
					var x1 = c.X - p1.X;
					var x2 = c.X - p2.X;
					if ((x1 <= zero) != (x2 <= zero) || x1 == zero || x2 == zero)
						return 1;
				} else if (c == p1) {
					return 1;
				}
				p1 = p2;
				y1 = y2;
			}
			return j != 0 ? 2 : 0;
		}

		private static string ToString(PolygonIndices value) {
			var sb = new StringBuilder();
			sb.Append("[");
			for (int i = 0, n = value.Length; i < n; i++) {
				sb.Append(" ");
				sb.Append(value[i].ToString());
			}
			sb.Append(" ]");
			return sb.ToString();
		}

		private static string ToString(int[] value) {
			var sb = new StringBuilder();
			sb.Append("[");
			for (int i = 0, n = value.Length; i < n; i++) {
				sb.Append(" ");
				sb.Append(value[i].ToString());
			}
			sb.Append(" ]");
			return sb.ToString();
		}

		private static string ToString(uint[] value) {
			var sb = new StringBuilder();
			sb.Append("[");
			for (int i = 0, n = value.Length; i < n; i++) {
				sb.Append(" ");
				sb.Append(value[i].ToString());
			}
			sb.Append(" ]");
			return sb.ToString();
		}

		private static string ToString(vector value) {
			var sb = new StringBuilder();
			sb.Append("(");
			sb.Append(value.X.ToString("F15"));
			sb.Append(",");
			sb.Append(value.Y.ToString("F15"));
			sb.Append(")");
			return sb.ToString();
		}

		static bool InRecursiveCall(object obj) {
			lock (RecursiveCaller) {
				return RecursiveCaller.Contains(obj);
			}
		}

		static bool InRecursiveCall(object _this, IEnumerable<object> objs) {
			lock (RecursiveCaller) {
				if (RecursiveCaller.Contains(_this))
					return true;
				foreach (var obj in objs)
					if (RecursiveCaller.Contains(obj))
						return true;
				return false;
			}
		}

		static bool InRecursiveCall(IEnumerable<object> objs) {
			lock (RecursiveCaller) {
				foreach (var obj in objs)
					if (RecursiveCaller.Contains(obj))
						return true;
				return false;
			}
		}

		static bool InRecursiveCall(params object[] objs) {
			lock (RecursiveCaller) {
				foreach (var obj in objs)
					if (RecursiveCaller.Contains(obj))
						return true;
				return false;
			}
		}

		static void EnterRecursiveCall(object obj) {
			lock (RecursiveCaller) {
				RecursiveCaller.Add(obj);
			}
		}

		static void LeaveRecursiveCall(object obj) {
			lock (RecursiveCaller) {
				RecursiveCaller.Remove(obj);
			}
		}

		public override string ToString() {
			if (InRecursiveCall(this)) {
				return "{}";
			} else {
				try {
					EnterRecursiveCall(this);
					return Jsonable.Fields(nameof(this.Groups), this.Groups, nameof(this.TopoGroups), this.TopoGroups);
				} finally {
					LeaveRecursiveCall(this);
				}
			}
		}

		public string ToJsonString() {
			return this.ToString();
		}
		#endregion
	}
}
