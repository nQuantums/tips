#if BOOL2D_LOG
//#define REDUCE_WRITE
//#define SETSEG_WRITE
#endif

using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using element = System.Single;
using vector = Jk.Vector2f;
using volume = Jk.Range2f;
using polbool = Jk.PolBool2f;
using bezier = Jk.Bezier2f;
using geo = Jk.Geo2f;

namespace Jk {
	/// <summary>
	/// <see cref="PolBool2f"/>を用いたヘルパ処理クラス、これを使い方の叩き台とする
	/// </summary>
	/// <typeparam name="Material">マテリアル型</typeparam>
	public static class BoolUtil2f<Material> where Material : class {
		#region 内部クラス
		/// <summary>
		/// 座標変換デリゲート
		/// </summary>
		/// <param name="position">[in,out] 座標</param>
		public delegate void PositionRefTransform(ref vector position);


		/// <summary>
		/// <see cref="BoolUtil2f{T}"/>内で発生する例外
		/// </summary>
		public class Exception : System.ApplicationException {
			public Exception(string message)
				: base(message) {
			}
		}

		/// <summary>
		/// <see cref="PolBool2f.EPolygon.UserValue"/>のフラグ値
		/// </summary>
		public static class EPolygonFlags {
			/// <summary>
			/// ポリゴンにグループ番号が設定された
			/// </summary>
			public const ulong SrcGroupPolygonSet = 1 << 0;
			//public const ulong EdgeDataNormalized = 1 << 1;
			//public const ulong EdgeValueSet = 1 << 2;

			/// <summary>
			/// ポリゴンにセグメントが設定された
			/// </summary>
			public const ulong SegmentsSet = 1 << 3;

			/// <summary>
			/// ポリゴンの頂点数が削減された
			/// </summary>
			public const ulong VerticesReduced = 1 << 4;
		}

		/// <summary>
		/// <see cref="Vertex"/>と<see cref="PolBool2f.Edge.UserValue"/>のフラグ値
		/// </summary>
		[Flags]
		public enum Flags {
			/// <summary>
			/// 順方向に接続されているエッジが非表示かどうか
			/// </summary>
			EdgeHidden = 1 << 0,
		}

		/// <summary>
		/// ２次元上での頂点
		/// </summary>
		[Serializable]
		public struct Vertex : IJsonable {
			/// <summary>
			/// 座標
			/// </summary>
			public vector Position;

			/// <summary>
			/// 頂点またはエッジの状態を表すフラグ
			/// </summary>
			public Flags Flags;

			/// <summary>
			/// この頂点から伸びるエッジに適用するマテリアル
			/// </summary>
			public Material Material;

			public Vertex(element x, element y) {
				this.Position = new vector(x, y);
				this.Flags = 0;
				this.Material = null;
			}

			public Vertex(vector position) {
				this.Position = position;
				this.Flags = 0;
				this.Material = null;
			}

			/// <summary>
			/// ブーリアン演算結果のエッジから頂点を合成する
			/// </summary>
			/// <param name="ed">エッジ</param>
			public Vertex(polbool.EDir ed) {
				// 座標初期化
				var p = ed.From.Position;
				this.Position = new vector(p.X, p.Y);

				// 状況に応じて適切な属性を選択する
				// ※グループインデックスが大きい方が後から描画されたグループとなっているため、
				// ※エッジに関わったグループの内インデックスが最大のものから属性を取得する
				var ea = ed.Edge.UserData as EdgeAtr;
				if (ea != null) {
					// エッジから属性が取得できたらその値を使用する
					this.Flags = ea.Flags;
					this.Material = ea.Material;
				} else {
					// エッジから属性が取得できなかったら非表示
					this.Flags = Flags.EdgeHidden;
					this.Material = null;
				}
			}

			public Vertex(vector position, EdgeAtr ea) {
				this.Position = position;
				if (ea != null) {
					this.Flags = ea.Flags;
					this.Material = ea.Material;
				} else {
					this.Flags = 0;
					this.Material = null;
				}
			}

			/// <summary>
			/// ブーリアン演算に引き継ぐエッジ属性を取得する
			/// </summary>
			/// <param name="outerMaterial">外枠のデフォルトマテリアル</param>
			/// <returns>エッジ属性</returns>
			public EdgeAtr ToEdgeAtr(Material outerMaterial) {
				return new EdgeAtr(this.Flags, this.Material ?? outerMaterial);
			}

			public override string ToString() {
				return Jsonable.Fields(nameof(this.Position), this.Position, nameof(this.Flags), this.Flags, nameof(this.Material), this.Material);
			}

			public string ToJsonString() {
				return this.ToString();
			}
		}

		/// <summary>
		/// ループ状の頂点配列
		/// </summary>
		[Serializable]
		public class Loop : IJsonable {
			/// <summary>
			/// ループを構成する頂点配列
			/// </summary>
			public FList<Vertex> Vertices;

			/// <summary>
			/// ループ内の同じ属性部分をまとめたセグメントの配列
			/// </summary>
			public FList<SharedSegment> Segments;

			public Loop() {
				this.Vertices = new FList<Vertex>();
			}

			public Loop(FList<Vertex> vertices) {
				this.Vertices = vertices;
			}

			public Loop(IEnumerable<Vertex> vertices) {
				this.Vertices = new FList<Vertex>(vertices);
			}

			public Loop(IEnumerable<vector> vertices) {
				this.Vertices = new FList<Vertex>(from v in vertices select new Vertex(v));
			}

			/// <summary>
			/// 指定サイズ未満の凹凸を削除し頂点数を減らす
			/// </summary>
			/// <param name="segmentEnablesDic">セグメントID別の頂点有効フラグ配列の辞書、同一セグメントに対して頂点削減処理を複数回行わないために使用</param>
			/// <param name="threshould">凹凸サイズ</param>
			/// <param name="tf">座標変換処理</param>
			/// <param name="workVertices">作業用頂点配列</param>
			/// <param name="reducedVertices">結果格納用頂点配列</param>
			/// <returns>実際に頂点数が削減されたら true が返る</returns>
			public bool Reduce(Dictionary<int, bool[]> segmentEnablesDic, element threshould, Func<vector, vector> tf, FList<Vertex> workVertices, FList<Vertex> reducedVertices) {
				var vertices = this.Vertices;

				// 必要なら座標変換を行う
				if (tf != null) {
					var verticesCore = vertices.Core;
					workVertices.Clear();
					if (workVertices.Capacity < verticesCore.Count)
						workVertices.Capacity = verticesCore.Count;
					for (int i = 0; i < verticesCore.Count; i++) {
						var v = verticesCore.Items[i];
						v.Position = tf(v.Position);
						workVertices.Add(v);
					}
					vertices = workVertices;
				}

				// 頂点数減らす
				return BoolUtil2f<Material>.Reduce(
					segmentEnablesDic,
					vertices,
					this.Segments,
					new Func<Vertex, vector>((v) => v.Position),
					threshould,
					reducedVertices);
			}

			public Loop Clone() {
				var l = this.MemberwiseClone() as Loop;
				l.Vertices = new FList<Vertex>(l.Vertices);
				l.Segments = new FList<SharedSegment>(l.Segments);
				return l;
			}

			public override string ToString() {
				return Jsonable.Fields(nameof(this.Vertices), this.Vertices, nameof(this.Segments), this.Segments);
			}

			public string ToJsonString() {
				return this.ToString();
			}
		}

		/// <summary>
		/// ポリゴンと穴
		/// </summary>
		[Serializable]
		public class Polygon : IJsonable {
			/// <summary>
			/// ループ配列、[ポリゴンインデックス、0:ポリゴン、1...:穴]
			/// </summary>
			public FList<Loop> Loops;

			/// <summary>
			/// 表面のマテリアル
			/// </summary>
			public Material FaceMaterial;

			/// <summary>
			/// 外枠のマテリアル
			/// </summary>
			public Material EdgeMaterial;

			/// <summary>
			/// 表面のテクスチャのスケーリング値
			/// </summary>
			public vector FaceTextureScale = new vector(0.2f, 0.2f);

			/// <summary>
			/// ポリゴンの境界ボリューム
			/// </summary>
			public volume Volume = volume.InvalidValue;

			/// <summary>
			/// コンストラクタ
			/// </summary>
			public Polygon() {
				this.Loops = new FList<Loop>();
			}

			/// <summary>
			/// コンストラクタ、ループリストを指定して初期化する
			/// </summary>
			public Polygon(FList<Loop> loops) {
				this.Loops = loops;
			}

			/// <summary>
			/// コンストラクタ、ループコレクションを指定して初期化する
			/// </summary>
			public Polygon(IEnumerable<Loop> loops) {
				this.Loops = new FList<Loop>(loops);
			}

			/// <summary>
			/// コンストラクタ、単一ループを指定して初期化する
			/// </summary>
			public Polygon(IEnumerable<vector> loop) {
				this.Loops = new FList<Loop>();
				this.Loops.Add(new Loop(loop));
			}

			/// <summary>
			/// コンストラクタ、ループコレクションを指定して初期化する
			/// </summary>
			public Polygon(IEnumerable<IEnumerable<vector>> loops) {
				this.Loops = new FList<Loop>(from loop in loops select new Loop(loop));
			}

			/// <summary>
			/// コンストラクタ、ブーリアン演算出力値を使い初期化する
			/// </summary>
			/// <param name="thisPolygonId">このポリゴンを識別するID(1...)</param>
			/// <param name="pb">トポロジー作成済みブーリアン演算オブジェクト</param>
			/// <param name="epolygon">ブーリアン演算結果のポリゴン</param>
			public Polygon(int thisPolygonId, polbool pb, polbool.EPolygon epolygon) {
				// ポリゴンに設定されている属性をコピーする
				var eloopsCore = epolygon.Loops.Core;
				var srcPolygon = epolygon.UserData as Polygon;
				if (srcPolygon != null) {
					this.FaceMaterial = srcPolygon.FaceMaterial;
					this.EdgeMaterial = srcPolygon.EdgeMaterial;
					this.FaceTextureScale = srcPolygon.FaceTextureScale;
				}
				if (eloopsCore.Count != 0)
					this.Volume = eloopsCore.Items[0].Volume;
				else
					this.Volume = volume.InvalidValue;

				// エッジループ配列から頂点ループ配列を作成する
				var loops = new FList<Loop>();
				for (int iloop = 0; iloop < eloopsCore.Count; iloop++) {
					var edgesCore = eloopsCore.Items[iloop].Edges.Core;
					var vertices = new FList<Vertex>(edgesCore.Count);
					for (int iedge = 0; iedge < edgesCore.Count; iedge++) {
						vertices.Add(new Vertex(edgesCore.Items[iedge]));
					}
					loops.Add(new Loop(vertices));
				}
				this.Loops = loops;
			}

			/// <summary>
			/// ブーリアン入力ポリゴンを取得する
			/// </summary>
			/// <param name="segmentEnablesDic">セグメントID別の頂点有効フラグ配列の辞書、同一セグメントに対して頂点削減処理を複数回行わないために使用</param>
			/// <param name="tf">座標変換処理、null指定可</param>
			/// <param name="faceMat">デフォルト表面マテリアル、null指定可</param>
			/// <param name="edgeMat">デフォルト外枠マテリアル、null指定可</param>
			/// <param name="threshould">結果のポリゴンからこの凹凸サイズより小さい頂点が削除される</param>
			/// <returns>ブーリアン演算用ポリゴン</returns>
			public polbool.Polygon ToBooleanInput(Dictionary<int, bool[]> segmentEnablesDic, Func<vector, vector> tf, Material faceMat, Material edgeMat, element threshould) {
				var srcLoopsCore = this.Loops.Core;
				var dstLoops = new FList<polbool.VLoop>();

				if (this.FaceMaterial == null)
					this.FaceMaterial = faceMat;
				if (this.EdgeMaterial == null)
					this.EdgeMaterial = edgeMat;

				// ループ配列をブーリアン演算の頂点ループ配列に変換する
				var edgeMaterial = this.EdgeMaterial;
				var workVertices = new FList<Vertex>();
				var verticesReduced = false;
				for (int iloop = 0; iloop < srcLoopsCore.Count; iloop++) {
#if BOOL2D_LOG
				using (Dbg.NewFrame("Lop", iloop + 1))
#endif
					{
						var srcLoop = srcLoopsCore.Items[iloop];

						// まず元の頂点数を減らす
						var reducedVertices = new FList<Vertex>(srcLoop.Vertices.Count);
						if (srcLoop.Reduce(segmentEnablesDic, threshould, tf, workVertices, reducedVertices))
							verticesReduced = true;

						// ブーリアン演算用のループを作成する
						var reducedVerticesCore = reducedVertices.Core;
						var dstVertices = new FList<polbool.Vertex>(reducedVerticesCore.Count);
						var dstEdgesUserdata = new FList<object>(reducedVerticesCore.Count);
						var dstLoopVolume = volume.InvalidValue;
						for (int i = 0; i < reducedVerticesCore.Count; i++) {
							var v = reducedVerticesCore.Items[i];
							dstVertices.Add(new polbool.Vertex(v.Position));
							dstEdgesUserdata.Add(v.ToEdgeAtr(edgeMaterial));
							dstLoopVolume.Merge(v.Position);
						}
						var dstLoop = new polbool.VLoop(dstVertices, dstEdgesUserdata);
						dstLoop.Volume = dstLoopVolume;
						dstLoop.UserData = srcLoop.Segments;

						dstLoops.Add(dstLoop);
					}
				}

				// ループ配列からポリゴンを作成する
				var polygon = new polbool.Polygon(dstLoops);
				polygon.UserData = this;
				if (verticesReduced)
					polygon.UserValue |= EPolygonFlags.VerticesReduced;

				return polygon;
			}

			/// <summary>
			/// 指定された座標変換を行う
			/// </summary>
			/// <param name="transform">ベクトルを入力し座標変換</param>
			public void Transform(PositionRefTransform transform) {
				foreach (var loop in this.Loops) {
					var verticesCore = loop.Vertices.Core;
					for (int i = verticesCore.Count - 1; i != -1; i--) {
						transform(ref verticesCore.Items[i].Position);
					}
				}
			}

			/// <summary>
			/// ポリゴンを平行移動する
			/// </summary>
			/// <param name="offset">移動量</param>
			public void Offset(vector offset) {
				foreach (var loop in this.Loops) {
					var verticesCore = loop.Vertices.Core;
					for (int i = verticesCore.Count - 1; i != -1; i--) {
						verticesCore.Items[i].Position += offset;
					}
				}
			}

			public Polygon Clone() {
				var p = this.MemberwiseClone() as Polygon;
				p.Loops = new FList<Loop>(p.Loops);
				var loopsCore = p.Loops.Core;
				for (int i = loopsCore.Count - 1; i != -1; i--) {
					loopsCore.Items[i] = loopsCore.Items[i].Clone();
				}
				return p;
			}

			public override string ToString() {
				return Jsonable.Fields(nameof(this.Loops), this.Loops, nameof(this.Volume), this.Volume, nameof(this.FaceTextureScale), this.FaceTextureScale, nameof(this.FaceMaterial), this.FaceMaterial, nameof(this.EdgeMaterial), this.EdgeMaterial);
			}

			public string ToJsonString() {
				return this.ToString();
			}
		}

		/// <summary>
		/// ポリゴンブーリアン演算に引き継ぐエッジ属性
		/// </summary>
		public class EdgeAtr : IJsonable {
			/// <summary>
			/// 頂点の状態を表すフラグ
			/// </summary>
			public Flags Flags;

			/// <summary>
			/// エッジに適用するマテリアル
			/// </summary>
			public Material Material;

			public EdgeAtr(Flags flags, Material material) {
				this.Flags = flags;
				this.Material = material;
			}

			public EdgeAtr Clone() {
				return this.MemberwiseClone() as EdgeAtr;
			}

			public override string ToString() {
				return Jsonable.Fields(nameof(this.Flags), this.Flags, nameof(this.Material), this.Material);
			}

			public string ToJsonString() {
				return this.ToString();
			}
		}

		/// <summary>
		/// 他のポリゴンと共有するセグメント、同じ属性をもつ頂点範囲を指す
		/// </summary>
		[Serializable]
		public struct SharedSegment : IJsonable {
			/// <summary>
			/// セグメントID（０～）
			/// </summary>
			public int Id;

			/// <summary>
			/// <see cref="Loop.Vertices"/>内での開始インデックス
			/// </summary>
			public int Start;

			/// <summary>
			/// <see cref="Loop.Vertices"/>内での終了インデックス
			/// </summary>
			public int End;

			/// <summary>
			/// 頂点数減らす際に最低でも１頂点を有効化し三角形以上にする必要があるかどうか
			/// </summary>
			public bool ForceTri;

			public SharedSegment(int id, int start, int end) {
				this.Id = id;
				this.Start = start;
				this.End = end;
				this.ForceTri = false;
			}

			public static SharedSegment Parse(string s) {
				SharedSegment seg = new SharedSegment();
				int state = 0;
				int start = 0;
				for (int i = 0, n = s.Length; i < n; i++) {
					var c = s[i];
					switch (state) {
					case 0:
						if (c < '0' || '9' < c) {
							continue;
						} else {
							state++;
							start = i;
							continue;
						}
					case 1:
						if (c < '0' || '9' < c) {
							state++;
							seg.Id = int.Parse(s.Substring(start, i - start));
							continue;
						} else {
							continue;
						}
					case 2:
						if (c < '0' || '9' < c) {
							continue;
						} else {
							state++;
							start = i;
							continue;
						}
					case 3:
						if (c < '0' || '9' < c) {
							state++;
							seg.Start = int.Parse(s.Substring(start, i - start));
							continue;
						} else {
							continue;
						}
					case 4:
						if (c < '0' || '9' < c) {
							continue;
						} else {
							state++;
							start = i;
							continue;
						}
					case 5:
						if (c < '0' || '9' < c) {
							state++;
							seg.End = int.Parse(s.Substring(start, i - start));
							continue;
						} else {
							continue;
						}
					case 6:
						if ((c < 'a' || 'z' < c) && (c < 'A' || 'Z' < c)) {
							continue;
						} else {
							state++;
							start = i;
							continue;
						}
					case 7:
						if ((c < 'a' || 'z' < c) && (c < 'A' || 'Z' < c)) {
							state++;
							seg.ForceTri = bool.Parse(s.Substring(start, i - start));
							continue;
						} else {
							continue;
						}
					}
				}
				return seg;
			}

			public override string ToString() {
				return Jsonable.Fields(nameof(this.Id), this.Id, nameof(this.Start), this.Start, nameof(this.End), this.End, nameof(this.ForceTri), this.ForceTri);
			}

			public string ToJsonString() {
				return this.ToString();
			}
		}

		/// <summary>
		/// 同じ属性をもつ頂点またはエッジの範囲を示すセグメント
		/// </summary>
		struct VertexSegment {
			/// <summary>
			/// 開始インデックス
			/// </summary>
			public int Start;

			/// <summary>
			/// 終了インデックス
			/// </summary>
			public int End;

			/// <summary>
			/// スタックの深さ
			/// </summary>
			public int Depth;

			public VertexSegment(int start, int end, int depth) {
				this.Start = start;
				this.End = end;
				this.Depth = depth;
			}

			public override string ToString() {
				return string.Concat("(", this.Start, "...", this.End, ")");
			}
		}

		/// <summary>
		/// ポリゴンブーリアン演算の結果をシームレスに入力側に持っていける様にするための構造体
		/// </summary>
		public struct IO : IJsonable {
			/// <summary>
			/// ブーリアン演算入力
			/// </summary>
			public FList<polbool.Polygon> Input;

			/// <summary>
			/// ブーリアン演算に使用した PolBoolF
			/// </summary>
			public polbool Pb;

			/// <summary>
			/// ブーリアン演算結果
			/// </summary>
			public FList<polbool.EPolygon> Output;

			/// <summary>
			/// ブーリアン演算結果を作成するのに使用したエッジフィルタ
			/// </summary>
			public EdgeFilters EdgeFilters;

			/// <summary>
			/// ポリゴン数が０かどうか
			/// </summary>
			public bool IsNull {
				get {
					return (this.Input == null || this.Input.Count == 0) && (this.Pb == null || this.Output == null || this.Output.Count == 0);
				}
			}

			/// <summary>
			/// コンストラクタ、指定ポリゴンで初期化する
			/// </summary>
			/// <param name="toInput">true なら入力値側へ設定され、false なら出力値側に不要なエッジを取り除かれたポリゴンが設定される</param>
			/// <param name="polygons">ポリゴン配列</param>
			public IO(bool toInput, params Polygon[] polygons)
				: this(toInput, new FList<Polygon>(polygons)) {
			}

			/// <summary>
			/// コンストラクタ、指定ポリゴンで初期化する
			/// </summary>
			/// <param name="toInput">true なら入力値側へ設定され、false なら出力値側に不要なエッジを取り除かれたポリゴンが設定される</param>
			/// <param name="polygons">ポリゴン配列</param>
			/// <param name="tf">座標変換処理、null指定可</param>
			/// <param name="faceMat">デフォルト表面マテリアル、null指定可</param>
			/// <param name="outerMat">デフォルト外枠マテリアル、null指定可</param>
			/// <param name="threshould">結果のポリゴンからこの凹凸サイズより小さい頂点が削除される、負数なら頂点削除は行われない</param>
			public IO(bool toInput, FList<Polygon> polygons, Func<vector, vector> tf = null, Material faceMat = null, Material outerMat = null, element threshould = -1) {
#if BOOL2D_LOG
			using (Dbg.NewFrame("Io"))
#endif
				{
					// とりあえず入力値を作成
					var segmentEnablesDic = new Dictionary<int, bool[]>();
					var polygonsCore = polygons.Core;
					var input = new FList<polbool.Polygon>(polygonsCore.Count);
#if BOOL2D_LOG
				using (0 <= threshould ? Dbg.NewFrameIncrement("Reduce") : Dbg.NullFrame())
#endif
					{
#if REDUCE_WRITE
					Dbg.StackWrite(polygons, "Before.csv");
#endif

						for (int i = 0, n = polygonsCore.Count; i < n; i++) {
#if BOOL2D_LOG
						using (Dbg.NewFrame("Pol", i + 1))
#endif
							{
								input.Add(polygonsCore.Items[i].ToBooleanInput(segmentEnablesDic, tf, faceMat, outerMat, threshould));
							}
						}

#if REDUCE_WRITE
					Dbg.StackWrite(input, "After.csv");
#endif
					}

					if (toInput) {
						// 作成した入力値をそのまま使用して初期化
						this.Input = input;
						this.Pb = null;
						this.Output = null;
						this.EdgeFilters = null;
					} else {
						// 作成した入力値をトポロジー化し不要なエッジを取り除く

						// 不要なエッジを無視するフィルタ
						var edgeFilter = new polbool.EdgeFilter(
							(pbFilter, edge, right) => {
								int rightGroupMax, leftGroupMax;
								polbool.PolygonIndices rightPolygons, leftPolygons;

								if (right) {
									rightGroupMax = edge.RightGroupMax;
									leftGroupMax = edge.LeftGroupMax;
									rightPolygons = edge.RightPolygons;
									leftPolygons = edge.LeftPolygons;
								} else {
									leftGroupMax = edge.RightGroupMax;
									rightGroupMax = edge.LeftGroupMax;
									leftPolygons = edge.RightPolygons;
									rightPolygons = edge.LeftPolygons;
								}

								// 進行方向右側にポリゴンが無ければ無視
								if (rightGroupMax < 0)
									return true;

								// ポリゴン外との境界だったら無視はできない
								if (leftGroupMax < 0)
									return false;

								// 左右のポリゴンが同じなら無視する
								var rightPolygonIndex = rightPolygons[rightGroupMax];
								var leftPolygonIndex = leftPolygons[leftGroupMax];
								if (rightPolygonIndex == leftPolygonIndex)
									return true;

								// 左右のマテリアルが同じなら無視する
								var rightMaterial = polygons[rightPolygonIndex].FaceMaterial;
								var leftMaterial = polygons[leftPolygonIndex].FaceMaterial;
								if (rightMaterial == leftMaterial)
									return true;

								return false;
							}
						);

						var pb = new polbool(Epsilon);
						pb.AddPolygon(input);
						pb.CreateTopology(false);

						var output = pb.Filtering(edgeFilter, polbool.EdgeFlags.RightEnabled, polbool.EdgeFlags.LeftEnabled);
						var outputCore = output.Core;

						// 輪郭に対応するエッジに属性を設定する
						SetEdgeAtr(pb);

						// ブーリアン演算入力から引き継いだ属性をコピーする
						var groupsCore = pb.Groups.Core;
						for (int ipolygon = outputCore.Count - 1; ipolygon != -1; ipolygon--) {
							var epolygon = outputCore.Items[ipolygon];
							SetSrcGroupPolygon(epolygon);
							epolygon.UserData = groupsCore.Items[epolygon.GroupIndex][epolygon.PolygonIndex].UserData;
						}

						this.Input = null;
						this.Pb = pb;
						this.Output = output;
						this.EdgeFilters = new EdgeFilters(edgeFilter, OrEdgeFilter);
					}
				}
			}

			/// <summary>
			/// コンストラクタ、ブーリアン演算入力値として初期化
			/// </summary>
			/// <param name="input">ブーリアン演算入力値</param>
			public IO(FList<polbool.Polygon> input) {
				this.Input = input;
				this.Pb = null;
				this.Output = null;
				this.EdgeFilters = null;
			}

			/// <summary>
			/// コンストラクタ、ブーリアン演算出力値として初期化
			/// </summary>
			/// <param name="pb">ブーリアン演算に使ったオブジェクト</param>
			/// <param name="edgeFilters">output を作成するのに使用したフィルタ</param>
			/// <param name="output">ブーリアン演算出力値</param>
			public IO(polbool pb, EdgeFilters edgeFilters, FList<polbool.EPolygon> output) {
				// 輪郭に対応するエッジに属性を設定する
				SetEdgeAtr(pb);

				// ブーリアン演算入力から引き継いだ属性をコピーする
				var outputCore = output.Core;
				var groupsCore = pb.Groups.Core;
				for (int ipolygon = outputCore.Count - 1; ipolygon != -1; ipolygon--) {
					var epolygon = outputCore.Items[ipolygon];
					SetSrcGroupPolygon(epolygon);
					epolygon.UserData = groupsCore.Items[epolygon.GroupIndex][epolygon.PolygonIndex].UserData;
				}

				this.Input = null;
				this.Pb = pb;
				this.Output = output;
				this.EdgeFilters = edgeFilters;
			}

			/// <summary>
			/// 出力値から指定凹凸サイズ未満の頂点を削除する
			/// </summary>
			/// <param name="threshould">凹凸サイズ</param>
			/// <returns>頂点が減らされた Pbio</returns>
			public void ReduceOutput(element threshould) {
#if BOOL2D_LOG
			using (Dbg.NewFrameIncrement("Reduce"))
#endif
				{
					var output = this.Output;
					if (output == null)
						return;
					if (threshould < 0)
						return;

#if REDUCE_WRITE
				Dbg.StackWrite(output, "Before.csv");
#endif

					// まずセグメントを設定する
					SetSegments(output);

					// 有効な凹凸削除用しきい値が設定されているならそれを用いてエッジ数を減らす
					var segmentEnablesDic = new Dictionary<int, bool[]>();
					var project = new Func<polbool.EDir, vector>((e) => e.From.Position);
					var outputCore = output.Core;
					for (int ipolygon = outputCore.Count - 1; ipolygon != -1; ipolygon--) {
#if BOOL2D_LOG
					using (Dbg.NewFrame("Pol", ipolygon + 1))
#endif
						{
							var epolygon = outputCore.Items[ipolygon];
							var eloopsCore = epolygon.Loops.Core;

							// セグメントを取得しセグメント内のエッジを減らす
							var reducedEdges = new FList<polbool.EDir>();
							var reducedELoops = new FList<polbool.ELoop>();
							var verticesReduced = false;
							for (int iloop = 0; iloop < eloopsCore.Count; iloop++) {
#if BOOL2D_LOG
							using (Dbg.NewFrame("Lop", iloop + 1))
#endif
								{
									var eloop = eloopsCore.Items[iloop];
									if (Reduce(segmentEnablesDic, eloop.Edges, eloop.UserData as FList<SharedSegment>, project, threshould, reducedEdges))
										verticesReduced = true;
									reducedELoops.Add(new polbool.ELoop(new FList<polbool.EDir>(reducedEdges)));
									eloop.UserData = null;
								}
							}
							epolygon.Loops = reducedELoops;

							// 自分自身のエッジ数を減らしたのでセグメントが無効になる
							epolygon.UserValue &= ~EPolygonFlags.SegmentsSet;
							if (verticesReduced)
								epolygon.UserValue |= EPolygonFlags.VerticesReduced;
						}
					}

#if REDUCE_WRITE
				Dbg.StackWrite(output, "After.csv");
#endif
				}
			}

			/// <summary>
			/// ポリゴンブーリアン演算の出力を入力に変換する、その際指定凹凸サイズ未満の凹凸頂点を削除する
			/// </summary>
			/// <param name="threshould">凹凸サイズ</param>
			/// <returns>入力値に変換後の IO、既に入力値になっていたら自分自身が返る</returns>
			public IO ToInput(element threshould = -1f) {
#if BOOL2D_LOG
			using (0 <= threshould ? Dbg.NewFrameIncrement("Reduce") : Dbg.NullFrame())
#endif
				{
					if (this.Input != null)
						return this;

					var pb = this.Pb;
					var output = this.Output;
					var inputGroups = pb.Groups;
					var inputGroupsCore = inputGroups.Core;

#if REDUCE_WRITE
				Dbg.StackWrite(output, "Before.csv");
#endif

					// 有効な凹凸削除用しきい値が設定されているならまずセグメントを設定する
					if (0 <= threshould) {
						// まずセグメントを設定する
						SetSegments(output);
					}

					var outputCore = output.Core;
					var input = new FList<polbool.Polygon>(outputCore.Count);
					var segmentEnablesDic = new Dictionary<int, bool[]>();
					for (int ipolygon = 0; ipolygon < outputCore.Count; ipolygon++) {
#if BOOL2D_LOG
					using (Dbg.NewFrame("Pol", ipolygon + 1))
#endif
						{

							var epolygon = outputCore.Items[ipolygon];
							var eloopsCore = epolygon.Loops.Core;

							// エッジループ配列から代表となるグループインデックスとポリゴンインデックスを探す
							var groupIndex = epolygon.GroupIndex;
							var polygonIndex = epolygon.PolygonIndex;
							var verticesReduced = false;

							// 有効な凹凸削除用しきい値が設定されているならそれを用いてエッジ数を減らす
							if (0 <= threshould) {
								// セグメントを取得しセグメント内のエッジを減らす
								var reducedEdges = new FList<polbool.EDir>();
								var reducedELoops = new FList<polbool.ELoop>();
								var project = new Func<polbool.EDir, vector>((e) => e.From.Position);
								for (int iloop = 0; iloop < eloopsCore.Count; iloop++) {
#if BOOL2D_LOG
								using (Dbg.NewFrame("Lop", iloop + 1))
#endif
									{

										var eloop = eloopsCore.Items[iloop];
										if (Reduce(segmentEnablesDic, eloop.Edges, eloop.UserData as FList<SharedSegment>, project, threshould, reducedEdges))
											verticesReduced = true;
										reducedELoops.Add(new polbool.ELoop(new FList<polbool.EDir>(reducedEdges)));
									}
								}
								eloopsCore = reducedELoops.Core;
							}

							// エッジループ配列から頂点ループ配列を作成する
							var vloops = new FList<polbool.VLoop>();
							for (int iloop = 0; iloop < eloopsCore.Count; iloop++) {
								var eloop = eloopsCore.Items[iloop];
								var edgesCore = eloop.Edges.Core;
								var vertices = new FList<polbool.Vertex>(edgesCore.Count);
								var edgesUserData = new FList<object>(edgesCore.Count);
								for (int iedge = 0; iedge < edgesCore.Count; iedge++) {
									var edge = edgesCore.Items[iedge];
									vertices.Add(new polbool.Vertex(edge.From.Position));
									edgesUserData.Add(FindAtr(inputGroups, edge.Edge));
								}
								var vloop = new polbool.VLoop(vertices, edgesUserData);
								vloop.Volume = eloop.Volume;
								vloop.Area = eloop.Area;
								vloop.CW = eloop.CW;
								vloops.Add(vloop);
							}

							// ブーリアン演算用のポリゴンを作成する
							var polygon = new polbool.Polygon(vloops);
							polygon.UserData = inputGroupsCore.Items[groupIndex][polygonIndex].UserData;
							if (verticesReduced)
								polygon.UserValue |= EPolygonFlags.VerticesReduced;

							input.Add(polygon);
						}
					}

#if REDUCE_WRITE
				Dbg.StackWrite(input, "After.csv");
#endif

					return new IO(input);
				}
			}

			/// <summary>
			/// ポリゴンブーリアン演算の入力を出力に変換する
			/// </summary>
			/// <returns>出力値に変換後の IO、既に出力値になっていたら自分自身が返る</returns>
			public IO ToOutput() {
				if (this.Output != null)
					return this;

				// 入力値を Polygon に変換する
				var input = this.Input;

				// 不要なエッジを無視するフィルタ
				var edgeFilter = new polbool.EdgeFilter(
					(pbFilter, edge, right) => {
						int rightGroupMax, leftGroupMax;
						polbool.PolygonIndices rightPolygons, leftPolygons;

						if (right) {
							rightGroupMax = edge.RightGroupMax;
							leftGroupMax = edge.LeftGroupMax;
							rightPolygons = edge.RightPolygons;
							leftPolygons = edge.LeftPolygons;
						} else {
							leftGroupMax = edge.RightGroupMax;
							rightGroupMax = edge.LeftGroupMax;
							leftPolygons = edge.RightPolygons;
							rightPolygons = edge.LeftPolygons;
						}

						// 進行方向右側にポリゴンが無ければ無視
						if (rightGroupMax < 0)
							return true;

						// ポリゴン外との境界だったら無視はできない
						if (leftGroupMax < 0)
							return false;

						// 左右のポリゴンが同じなら無視する
						var rightPolygonIndex = rightPolygons[rightGroupMax];
						var leftPolygonIndex = leftPolygons[leftGroupMax];
						if (rightPolygonIndex == leftPolygonIndex)
							return true;

						// 左右のマテリアルが同じなら無視する
						var rightPolygon = input[rightPolygonIndex].UserData as Polygon;
						var leftPolygon = input[leftPolygonIndex].UserData as Polygon;
						var rightMaterial = rightPolygon != null ? rightPolygon.FaceMaterial : null;
						var leftMaterial = leftPolygon != null ? leftPolygon.FaceMaterial : null;
						if (rightMaterial == leftMaterial)
							return true;

						return false;
					}
				);

				// 入力値をトポロジー化
				var pb = new polbool(Epsilon);
				pb.AddPolygon(input);
				pb.CreateTopology(false);

				// 必要な境界部分だけ取得
				return new IO(pb, new EdgeFilters(edgeFilter, OrEdgeFilter), pb.Filtering(edgeFilter, polbool.EdgeFlags.RightEnabled, polbool.EdgeFlags.LeftEnabled));
			}

			/// <summary>
			/// 入力または出力値を<see cref="Polygon"/> 配列に変換する
			/// </summary>
			/// <returns><see cref="Polygon"/> 配列</returns>
			public FList<Polygon> ToPolygons() {
				// とりあえず出力値側に変換する
				var io = this.ToOutput();
				var output = io.Output;
				var outputCore = output.Core;
				var pb = this.Pb;

				// 出力値側にセグメントをセット
				SetSegments(output);

				// 出力値を Polygon に変換する
				var polygons = new FList<Polygon>(outputCore.Count);
				for (int ipolygon = 0; ipolygon < outputCore.Count; ipolygon++) {
					var epolygon = outputCore.Items[ipolygon];
					var polygon = new Polygon(ipolygon + 1, pb, epolygon);
					var eloopsCore = epolygon.Loops.Core;
					var loopsCore = polygon.Loops.Core;

					// セグメントを取得する
					for (int iloop = eloopsCore.Count - 1; iloop != -1; iloop--) {
						loopsCore.Items[iloop].Segments = eloopsCore.Items[iloop].UserData as FList<SharedSegment>;
					}

					polygons.Add(polygon);
				}

				return polygons;
			}

			public override string ToString() {
				return Jsonable.Fields(nameof(this.Input), this.Input, nameof(this.Pb), this.Pb, nameof(this.Output), this.Output);
			}

			public string ToJsonString() {
				return this.ToString();
			}

#if UNITY_5_3_OR_NEWER
			[System.Diagnostics.Conditional("BOOL2D_LOG")]
			public void DbgStackWrite(params object[] fileName) {
				var baseName = string.Concat(string.Concat(fileName), "_");
				if (this.Input != null) {
					Dbg.StackWrite(this.Input, baseName, "Input.csv");
				} else {
					Dbg.StackWrite(this.Pb, baseName, "Topo.csv");
					for (int i = 0; i < this.Pb.Groups.Count; i++) {
						Dbg.StackWrite(this.Pb.Groups[i], baseName, "Input", i + 1, ".csv");
					}
					Dbg.StackWrite(this.Output, baseName, "Output.csv");
				}
			}
#endif
		}

		/// <summary>
		/// メッシュ生成用とコライダー用にポリゴンを抽出するためのフィルタ
		/// </summary>
		public class EdgeFilters {
			/// <summary>
			/// メッシュ抽出用
			/// </summary>
			public polbool.EdgeFilter ForMesh;

			/// <summary>
			/// コライダーパス抽出用
			/// </summary>
			public polbool.EdgeFilter ForCollider;

			public EdgeFilters(polbool.EdgeFilter forMesh, polbool.EdgeFilter forCollider) {
				this.ForMesh = forMesh;
				this.ForCollider = forCollider;
			}
		}
		#endregion

		#region フィールド
		const element CalcEpsilon = (element)1.0e-20;
		const element BezierCircle = (element)0.55228;
		const element BezierCircle2 = BezierCircle * 2;

		/// <summary>
		/// ブーリアン演算時の０距離判定値
		/// </summary>
		public const element Epsilon = 0.001f;

		static readonly HashSet<object> RecursiveCaller = new HashSet<object>();

		#region エッジフィルタ
		/// <summary>
		/// エッジの両側にポリゴンが存在するなら無視するフィルタ
		/// </summary>
		public static readonly polbool.EdgeFilter OrEdgeFilter = new polbool.EdgeFilter(
			(pb, e, right) => {
				return 0 <= e.RightGroupMax && 0 <= e.LeftGroupMax;
			}
		);

		/// <summary>
		/// エッジの進行方向右側に全グループが存在しないなら無視するフィルタ
		/// </summary>
		public static readonly polbool.EdgeFilter AndEdgeFilter = new polbool.EdgeFilter(
			(pb, edge, right) => {
				polbool.PolygonIndices rp;

				if (right) {
					rp = edge.RightPolygons;
				} else {
					rp = edge.LeftPolygons;
				}

				// 進行方向右側にひとつでもポリゴンが無ければ無視
				if (rp[0] < 0 || rp[1] < 0)
					return true;

				return false;
			}
		);

		/// <summary>
		/// AndEdgeFilter に対応するコライダーパス抽出用フィルタ
		/// </summary>
		public static readonly polbool.EdgeFilter AndEdgeColliderFilter = new polbool.EdgeFilter(
			(pb, edge, right) => {
				polbool.PolygonIndices rp, lp;

				if (right) {
					rp = edge.RightPolygons;
					lp = edge.LeftPolygons;
				} else {
					lp = edge.RightPolygons;
					rp = edge.LeftPolygons;
				}

				// 進行方向右側にひとつでもポリゴンが無ければ無視
				if (rp[0] < 0 || rp[1] < 0)
					return true;

				// 進行方向左側も同じ状態なら無視
				if (0 <= lp[0] && 0 <= lp[1])
					return true;

				return false;
			}
		);

		/// <summary>
		/// 上描きを表現するフィルタ
		/// </summary>
		public static readonly polbool.EdgeFilter PaintEdgeFilter = new polbool.EdgeFilter(
			(pb, edge, right) => {
				int rightGroupMax, leftGroupMax;
				polbool.PolygonIndices rightPolygons, leftPolygons;

				if (right) {
					rightGroupMax = edge.RightGroupMax;
					leftGroupMax = edge.LeftGroupMax;
					rightPolygons = edge.RightPolygons;
					leftPolygons = edge.LeftPolygons;
				} else {
					leftGroupMax = edge.RightGroupMax;
					rightGroupMax = edge.LeftGroupMax;
					leftPolygons = edge.RightPolygons;
					rightPolygons = edge.LeftPolygons;
				}

				// 進行方向右側にポリゴンが無ければ無視
				if (rightGroupMax < 0)
					return true;

				// ポリゴン外との境界だったら無視はできない
				if (leftGroupMax < 0)
					return false;

				// 左右のポリゴンが同じなら無視する
				var rightPolygonIndex = rightPolygons[rightGroupMax];
				var leftPolygonIndex = leftPolygons[leftGroupMax];
				if (rightGroupMax == leftGroupMax && rightPolygonIndex == leftPolygonIndex)
					return true;

				// 左右のマテリアルが違うなら無視できない
				var groups = pb.Groups;
				var rightMaterial = (groups[rightGroupMax][rightPolygonIndex].UserData as Polygon).FaceMaterial;
				var leftMaterial = (groups[leftGroupMax][leftPolygonIndex].UserData as Polygon).FaceMaterial;
				if (rightMaterial != leftMaterial)
					return false;

				return true;
			}
		);

		/// <summary>
		/// 先頭グループ内のエッジのみ残すエッジフィルタ
		/// </summary>
		public static readonly polbool.EdgeFilter ClipEdgeFilter = new polbool.EdgeFilter(
			(pb, edge, right) => {
				int rightGroupMax, leftGroupMax;
				polbool.PolygonIndices rightPolygons, leftPolygons;

				if (right) {
					rightGroupMax = edge.RightGroupMax;
					leftGroupMax = edge.LeftGroupMax;
					rightPolygons = edge.RightPolygons;
					leftPolygons = edge.LeftPolygons;
				} else {
					leftGroupMax = edge.RightGroupMax;
					rightGroupMax = edge.LeftGroupMax;
					leftPolygons = edge.RightPolygons;
					rightPolygons = edge.LeftPolygons;
				}

				// 進行方向右側に０グループ目が存在しなかったら無視する
				if (rightPolygons[0] < 0)
					return true;

				// 進行方向左側に０グループ目が存在しなかったら輪郭なので無視できない
				if (leftPolygons[0] < 0)
					return false;

				// 左右のマテリアルが違うなら無視できない
				var groups = pb.Groups;
				var rightPolygonIndex = rightPolygons[rightGroupMax];
				var leftPolygonIndex = leftPolygons[leftGroupMax];
				var rightMaterial = (groups[rightGroupMax][rightPolygonIndex].UserData as Polygon).FaceMaterial;
				var leftMaterial = (groups[leftGroupMax][leftPolygonIndex].UserData as Polygon).FaceMaterial;
				if (rightMaterial != leftMaterial)
					return false;

				return true;
			}
		);

		/// <summary>
		/// ClipEdgeFilter に対応するコライダーパス抽出用フィルタ
		/// </summary>
		public static readonly polbool.EdgeFilter ClipEdgeColliderFilter = new polbool.EdgeFilter(
			(pb, edge, right) => {
				bool r, l;
				if (right) {
					r = 0 <= edge.RightPolygons[0];
					l = 0 <= edge.LeftPolygons[0];
				} else {
					l = 0 <= edge.RightPolygons[0];
					r = 0 <= edge.LeftPolygons[0];
				}
				// 進行方向右側に０グループ目が存在しなかったら無視する
				if (!r)
					return true;
				// 両側に０グループ目があっても無視する
				if (l)
					return true;
				return false;
			}
		);

		/// <summary>
		/// 最後尾グループ内のエッジのみ残すエッジフィルタ
		/// </summary>
		public static readonly polbool.EdgeFilter MaskEdgeFilter = new polbool.EdgeFilter(
			(pb, edge, right) => {
				return 0 <= (right ? edge.RightPolygons : edge.LeftPolygons)[0];
			}
		);

		/// <summary>
		/// MaskEdgeFilter に対応するコライダーパス抽出用フィルタ
		/// </summary>
		public static readonly polbool.EdgeFilter MaskEdgeColliderFilter = new polbool.EdgeFilter(
			(pb, edge, right) => {
				return 0 <= (right ? edge.RightPolygons : edge.LeftPolygons)[0];
			}
		);

		/// <summary>
		/// 先頭グループで削り取るエッジフィルタ
		/// </summary>
		public static readonly polbool.EdgeFilter EraseEdgeFilter1 = new polbool.EdgeFilter(
			(pb, edge, right) => {
				return 0 <= (right ? edge.RightPolygons : edge.LeftPolygons)[0];
			}
		);

		/// <summary>
		/// EraseEdgeFilter1 に対応するコライダーパス抽出用フィルタ
		/// </summary>
		public static readonly polbool.EdgeFilter EraseEdgeColliderFilter1 = new polbool.EdgeFilter(
			(pb, edge, right) => {
				polbool.PolygonIndices rp, lp;

				if (right) {
					rp = edge.RightPolygons;
					lp = edge.LeftPolygons;
				} else {
					lp = edge.RightPolygons;
					rp = edge.LeftPolygons;
				}

				// 指定側に消しゴムがあるなら無視
				if (0 <= rp[0])
					return true;

				// 両側に実体あり消しゴムが接触していないならコライダーに不要なエッジなので無視
				if (0 <= rp[1] && 0 <= lp[1] && lp[0] < 0)
					return true;

				return false;
			}
		);

		/// <summary>
		/// 最後尾グループで削り取るエッジフィルタ
		/// </summary>
		public static readonly polbool.EdgeFilter EraseEdgeFilter2 = new polbool.EdgeFilter(
			(pb, edge, right) => {
				return 0 <= (right ? edge.RightPolygons : edge.LeftPolygons)[1];
			}
		);

		/// <summary>
		/// EraseEdgeFilter2 に対応するコライダーパス抽出用フィルタ
		/// </summary>
		public static readonly polbool.EdgeFilter EraseEdgeColliderFilter2 = new polbool.EdgeFilter(
			(pb, edge, right) => {
				polbool.PolygonIndices rp, lp;

				if (right) {
					rp = edge.RightPolygons;
					lp = edge.LeftPolygons;
				} else {
					lp = edge.RightPolygons;
					rp = edge.LeftPolygons;
				}

				// 指定側に消しゴムがあるなら無視
				if (0 <= rp[1])
					return true;

				// 両側に実体あり消しゴムが接触していないならコライダーに不要なエッジなので無視
				if (0 <= rp[0] && 0 <= lp[0] && lp[1] < 0)
					return true;

				return false;
			}
		);
		#endregion
		#endregion

		#region 公開メソッド
		/// <summary>
		/// ブーリアン演算入出力値からトポロジー構造を作成する
		/// </summary>
		/// <returns>トポロジー構造作成済みの PolBoolF</returns>
		public static polbool Topology(IO input1) {
			var pb = new polbool(Epsilon);
			pb.UserDataCloner = new Func<object, object>(CloneEdgeAtr);
			pb.AddPolygon(input1.ToInput().Input);
			try {
				pb.CreateTopology(false);
			} catch {
#if UNITY_5_3_OR_NEWER
				for (int i = 0, n = pb.Groups.Count; i < n; i++) {
					Dbg.StackWrite(pb.Groups[i], "TopologyFailed", i + 1, ".csv");
				}
#endif
				throw;
			}
			return pb;
		}

		/// <summary>
		/// ブーリアン演算入出力値からトポロジー構造を作成する
		/// </summary>
		/// <returns>トポロジー構造作成済みの PolBoolF</returns>
		public static polbool Topology(IO input1, IO input2) {
			var pb = new polbool(Epsilon);
			pb.UserDataCloner = CloneEdgeAtr;
			pb.AddPolygon(input1.ToInput().Input);
			pb.AddPolygon(input2.ToInput().Input);
			try {
				pb.CreateTopology(false);
			} catch {
#if UNITY_5_3_OR_NEWER
				for (int i = 0, n = pb.Groups.Count; i < n; i++) {
					Dbg.StackWrite(pb.Groups[i], "TopologyFailed", i + 1, ".csv");
				}
#endif
				throw;
			}
			return pb;
		}

		/// <summary>
		/// ORでポリゴンを結合する
		/// </summary>
		/// <param name="pb">トポロジー構造作成済みの PolBoolF</param>
		/// <returns>ブーリアン演算出力値</returns>
		public static IO Or(polbool pb) {
			var result = pb.Filtering(OrEdgeFilter, polbool.EdgeFlags.RightEnabled, polbool.EdgeFlags.LeftEnabled);
			return new IO(pb, new EdgeFilters(OrEdgeFilter, OrEdgeFilter), result);
		}

		/// <summary>
		/// 指定された全ポリゴンをORを結合する
		/// </summary>
		/// <param name="polygons">ポリゴン配列</param>
		/// <returns>ポリゴン配列</returns>
		public static IO Or(FList<Polygon> polygons) {
			var polygonsCore = polygons.Core;
			var last = polygonsCore.Count - 1;
			if (last < 0)
				return new IO();

			// 演算入力形式に変換する
			var ios = new IO[polygonsCore.Count];
			for (int i = ios.Length - 1; i != -1; i--) {
				var pols = new FList<Polygon>();
				pols.Add(polygonsCore.Items[i]);
				ios[i] = new IO(true, pols);
			}

			// 配列内で隣り合う２つをORして最終的に１つにする
			while (last != 0) {
				var l = last;
				for (int i = 0; i <= last; i += 2) {
					var pb = i < last ? Topology(ios[i], ios[i + 1]) : Topology(ios[i]);
					l = i / 2;
					ios[l] = Or(pb);
				}
				last = l;
			}

			return ios[0];
		}

		/// <summary>
		/// AND処理
		/// </summary>
		/// <param name="pb">トポロジー構造作成済みの PolBoolF</param>
		/// <returns>ブーリアン演算出力値</returns>
		public static IO And(polbool pb) {
			var result = pb.Filtering(AndEdgeFilter, polbool.EdgeFlags.RightEnabled, polbool.EdgeFlags.LeftEnabled);
			return new IO(pb, new EdgeFilters(AndEdgeFilter, AndEdgeColliderFilter), result);
		}

		/// <summary>
		/// 上書きするようにポリゴンをくっつける処理
		/// </summary>
		/// <param name="pb">トポロジー構造作成済みの PolBoolF</param>
		/// <returns>ブーリアン演算出力値</returns>
		public static IO Paint(polbool pb) {
			var result = pb.Filtering(PaintEdgeFilter, polbool.EdgeFlags.RightEnabled, polbool.EdgeFlags.LeftEnabled);
			return new IO(pb, new EdgeFilters(PaintEdgeFilter, OrEdgeFilter), result);
		}

		/// <summary>
		/// 最終グループを減算する
		/// </summary>
		/// <param name="pb">トポロジー構造作成済みの PolBoolF</param>
		/// <param name="eraserGroupIndex">消しゴムとして使用するグループインデックス</param>
		/// <returns>ブーリアン演算出力値</returns>
		public static IO Erase(polbool pb, int eraserGroupIndex) {
			var filter = eraserGroupIndex == 0 ? EraseEdgeFilter1 : EraseEdgeFilter2;
			var colliderFilter = eraserGroupIndex == 0 ? EraseEdgeColliderFilter1 : EraseEdgeColliderFilter2;
			var result = pb.Filtering(filter, polbool.EdgeFlags.RightEnabled, polbool.EdgeFlags.LeftEnabled);
			return new IO(pb, new EdgeFilters(filter, colliderFilter), result);
		}

		/// <summary>
		/// 先頭グループ内のエッジのみ残す
		/// </summary>
		/// <param name="pb">トポロジー構造作成済みの PolBoolF</param>
		/// <returns>ブーリアン演算出力値</returns>
		public static IO Clip(polbool pb) {
			var result = pb.Filtering(ClipEdgeFilter, polbool.EdgeFlags.RightEnabled, polbool.EdgeFlags.LeftEnabled);
			return new IO(pb, new EdgeFilters(ClipEdgeFilter, ClipEdgeColliderFilter), result);
		}

		/// <summary>
		/// 最後尾グループ内のエッジのみ残す
		/// </summary>
		/// <param name="pb">トポロジー構造作成済みの PolBoolF</param>
		/// <returns>ブーリアン演算出力値</returns>
		public static IO Mask(polbool pb) {
			// トポロジーからフィルタを使ってループ一覧を取り出す
			var result = pb.Filtering(MaskEdgeFilter, polbool.EdgeFlags.RightEnabled, polbool.EdgeFlags.LeftEnabled);
			return new IO(pb, new EdgeFilters(MaskEdgeFilter, MaskEdgeColliderFilter), result);
		}

		/// <summary>
		/// 指定サイズ未満の凹凸を削除し頂点数を減らす
		/// </summary>
		/// <param name="segmentEnablesDic">セグメントID別の頂点有効フラグ配列の辞書、同一セグメントに対して頂点削減処理を複数回行わないために使用</param>
		/// <param name="vertices">元の頂点配列</param>
		/// <param name="segments">セグメント配列</param>
		/// <param name="project">頂点から２次元座標を取得する関数</param>
		/// <param name="threshould">凹凸サイズ</param>
		/// <param name="reducedVertices">結果格納用頂点配列</param>
		/// <typeparam name="T">頂点型</typeparam>
		/// <returns>実際に頂点数が削減されたら true が返る</returns>
		public static bool Reduce<T>(Dictionary<int, bool[]> segmentEnablesDic, FList<T> vertices, FList<SharedSegment> segments, Func<T, vector> project, element threshould, FList<T> reducedVertices) {
			var verticesCore = vertices.Core;

			// 結果の入れ物をとりあえずクリア
			reducedVertices.Clear();

			// 凹凸サイズが無効なら頂点減らさない
			if (threshould < 0f) {
				reducedVertices.AddRange(verticesCore);
				return false;
			}

			// セグメントが１つ以下ならば全体を１つのセグメントとして頂点数を減らす
			if (segments == null || segments.Count <= 1) {
				var segmentEnables = new bool[verticesCore.Count];
				EnableVertices(vertices, 0, verticesCore.Count, segmentEnables, project, threshould, false);
				ExtractEnabledVertices(verticesCore, segmentEnables, reducedVertices);
				return verticesCore.Count != reducedVertices.Count;
			}

			// セグメント別に頂点数減らし処理をしていく
			var enables = new bool[verticesCore.Count];
			var segmentsCore = segments.Core;
			for(int isegment = 0; isegment < segmentsCore.Count; isegment++) {
				var segment = segmentsCore.Items[isegment];
				bool[] segmentEnables;
				bool reverse;
				if (!segmentEnablesDic.TryGetValue(segment.Id, out segmentEnables)) {
					// セグメント内の頂点有効フラグ配列が作られていなければ作成する
					segmentEnables = new bool[segment.End - segment.Start + 1];
					reverse = false;
					EnableVertices(vertices, segment.Start, segment.End, segmentEnables, project, threshould, segment.ForceTri);
					segmentEnablesDic.Add(segment.Id, segmentEnables);
				} else {
					// セグメント内の頂点有効フラグ配列が既に存在したらそれを再利用する
					// その際、反対側ポリゴンからの参照なので逆順とする
					reverse = true;
				}
				CopyVertexEnables(segment.Start, segmentEnables, reverse, enables);
			}
			ExtractEnabledVertices(verticesCore, enables, reducedVertices);

			return verticesCore.Count != reducedVertices.Count;
		}


		/// <summary>
		/// 指定された頂点列を繋ぐ線分に太さを持たせてポリゴン化する、折り返し部分でポリゴンを切り分けている
		/// </summary>
		/// <param name="points">頂点列</param>
		/// <param name="width">太さ</param>
		/// <param name="faceMaterial">表面マテリアル</param>
		/// <param name="edgeMaterial">エッジマテリアル</param>
		/// <returns>ポリゴン列</returns>
		public static FList<Polygon> PolygonsFromPolyline(FList<vector> points, float width, Material faceMaterial, Material edgeMaterial) {
			var list = new FList<Polygon>();

			var pointsCore = points.Core;
			if (pointsCore.Count < 2)
				return list;

			var p1 = pointsCore.Items[0]; // １つ前の頂点
			var p2 = pointsCore.Items[1]; // 注目中の頂点
			var v1 = p2 - p1; // １つ前から注目頂点へのベクトル

			int start = 0;

			if (3 <= pointsCore.Count) {
				for (int i = 2; i < pointsCore.Count; i++) {
					var p3 = pointsCore.Items[i]; // １つ先の頂点
					var v2 = p3 - p2; // 注目頂点から１つ先の頂点へのベクトル

					// １つ前のベクトルに対して逆方向へ折れ曲がるなら切る
					if (v1.Dot(v2) < 0) {
						list.Add(PolygonFromPolyline(pointsCore.Items, start, i - start, width, faceMaterial, edgeMaterial));
						start = i - 1;
					}

					p1 = p2;
					p2 = p3;
					v1 = v2;
				}
			}

			list.Add(PolygonFromPolyline(pointsCore.Items, start, pointsCore.Count - start, width, faceMaterial, edgeMaterial));

			return list;
		}
		#endregion

		#region 非公開メソッド
		/// <summary>
		/// ポリゴンを構成するエッジから対応する入力値のグループ、ポリゴンのインデックスを探して設定する
		/// </summary>
		/// <param name="epolygon">ブーリアン演算結果のポリゴン</param>
		static void SetSrcGroupPolygon(polbool.EPolygon epolygon) {
			// 既に設定済みなら何もしない
			if ((epolygon.UserValue & EPolygonFlags.SrcGroupPolygonSet) != 0)
				return;

			// エッジループ配列から代表となるグループインデックスとポリゴンインデックスを探す
			int srcGroupIndex = -1;
			int srcPolygonIndex = -1;
			var eloopsCore = epolygon.Loops.Core;
			for (int iloop = 0; iloop < eloopsCore.Count; iloop++) {
				var edgesCore = eloopsCore.Items[iloop].Edges.Core;
				for (int i = edgesCore.Count - 1; i != -1; i--) {
					var edge = edgesCore.Items[i];
					int rgm;
					polbool.PolygonIndices rp;

					// 指定方向で取得
					if (edge.TraceRight) {
						rgm = edge.Edge.RightGroupMax;
						rp = edge.Edge.RightPolygons;
					} else {
						rgm = edge.Edge.LeftGroupMax;
						rp = edge.Edge.LeftPolygons;
					}

					// グループインデックスが大きい方を優先する
					if (srcGroupIndex < rgm) {
						srcGroupIndex = rgm;
						srcPolygonIndex = rp[rgm];
						if (srcGroupIndex == rp.Length - 1)
							break;
					}
				}
			}
			if (srcGroupIndex == -1) {
				for (int iloop = 0; iloop < eloopsCore.Count; iloop++) {
					var edgesCore = eloopsCore.Items[iloop].Edges.Core;
					for (int i = edgesCore.Count - 1; i != -1; i--) {
						var edge = edgesCore.Items[i];
						int rgm;
						polbool.PolygonIndices rp;

						// 逆方向で取得
						if (edge.TraceRight) {
							rgm = edge.Edge.LeftGroupMax;
							rp = edge.Edge.LeftPolygons;
						} else {
							rgm = edge.Edge.RightGroupMax;
							rp = edge.Edge.RightPolygons;
						}

						// グループインデックスが大きい方を優先する
						if (srcGroupIndex < rgm) {
							srcGroupIndex = rgm;
							srcPolygonIndex = rp[rgm];
							if (srcGroupIndex == rp.Length - 1)
								break;
						}
					}
				}
			}

			if (srcGroupIndex == -1)
				throw new Exception("No group found from edges."); // TODO: 通常あり得ないのにここに来ることがある

			// 値をセット
			epolygon.GroupIndex = srcGroupIndex;
			epolygon.PolygonIndex = srcPolygonIndex;

			// グループ、ポリゴンインデックス設定したことを記録
			epolygon.UserValue |= EPolygonFlags.SrcGroupPolygonSet;
		}

		/// <summary>
		/// 最終的に輪郭となるエッジに属性を設定していく
		/// </summary>
		/// <param name="pb">ブーリアン演算に使用した PolBoolF</param>
		static void SetEdgeAtr(polbool pb) {
			var groups = pb.Groups;
			foreach (var edge in pb.Edges) {
				var enableFlags = edge.Flags & (polbool.EdgeFlags.RightEnabled | polbool.EdgeFlags.LeftEnabled);
				if (enableFlags == 0)
					continue;

				if (enableFlags == (polbool.EdgeFlags.RightEnabled | polbool.EdgeFlags.LeftEnabled)) {
					// エッジの両側にポリゴンが存在するなら
					// Z方向に伸びるポリゴンは表示する必要がなくなる
					// すなわちマテリアルは必要ない
					edge.UserData = null;
				} else {
					// 指定されたエッジに設定されている属性の中で最も優先順位が高いものを取得する
					edge.UserData = FindAtr(groups, edge);
				}
			}
		}

		/// <summary>
		/// 同じエッジ属性コード範囲をまとめたセグメントを設定する
		/// </summary>
		/// <param name="epolygons">設定先のループを含むポリゴン</param>
		/// <returns>セグメント配列</returns>
		public static void SetSegments(FList<polbool.EPolygon> epolygons) {
#if BOOL2D_LOG
		using(Dbg.NewFrameIncrement("SetSeg"))
#endif
			{
				var epolygonsCore = epolygons.Core;

				// １つでも既に設定済みなら処理しない
				for (int ipolygon = epolygonsCore.Count - 1; ipolygon != -1; ipolygon--) {
					if ((epolygonsCore.Items[ipolygon].UserValue & EPolygonFlags.SegmentsSet) != 0)
						return;
				}

#if SETSEG_WRITE
			Dbg.StackWrite(EPolygonsDebug(epolygons), "BeforeValue.txt");
#endif

				// 先ずマテリアルにインデックスを付与するため使用中の全マテリアルを列挙する
				var materials = new FList<Material>();
				for (int ipolygon = epolygonsCore.Count - 1; ipolygon != -1; ipolygon--) {
					var loopsCore = epolygonsCore.Items[ipolygon].Loops.Core;
					for (int iloop = loopsCore.Count - 1; iloop != -1; iloop--) {
						var edgesCore = loopsCore.Items[iloop].Edges.Core;
						for (int i = 0; i < edgesCore.Count; i++) {
							var ea = edgesCore.Items[i].Edge.UserData as EdgeAtr;
							var material = ea != null ? ea.Material : null;
							if (!materials.Contains(material))
								materials.Add(material);
						}
					}
				}

				// エッジに左右ポリゴンIDとマテリアルインデックスをコード化した値をセットしていく
				for (int ipolygon = epolygonsCore.Count - 1; ipolygon != -1; ipolygon--) {
					// エッジに属性からコード化した値を設定していく
					var polygonId = (ulong)(ipolygon + 1);
					var loopsCore = epolygonsCore.Items[ipolygon].Loops.Core;
					for (int iloop = loopsCore.Count - 1; iloop != -1; iloop--) {
						var edgesCore = loopsCore.Items[iloop].Edges.Core;
						for (int i = 0; i < edgesCore.Count; i++) {
							var ed = edgesCore.Items[i];
							var edge = ed.Edge;
							var euv = edge.UserValue;

							if ((euv & (1UL << 63)) != 0)
								euv = 0; // 既にセグメント生成済みだったら一旦クリアする必要がある

							// エッジ値を設定する必要があるかどうか調べる
							// ビットフォーマットは以下の通り
							//   0-14: ポリゴンID1
							//  15-29: マテリアルID1
							//  30-44: ポリゴンID2
							//  45-59: マテリアルID2
							//     63: セグメント生成済みフラグ
							int hilo;
							if ((euv & 0x7fffL) == 0)
								hilo = 1;
							else if ((euv & (0x7fffL << 30)) == 0)
								hilo = 2;
							else
								hilo = 0;

							// エッジ値を設定する必要があるならば処理する
							if (hilo != 0) {
								// エッジから属性を取得する
								Flags flags;
								ulong materialId;
								var ea = edge.UserData as EdgeAtr;

								if (ea != null) {
									// エッジから属性が取得できたらその値を使用する
									flags = ea.Flags;
									materialId = (ulong)(uint)materials.IndexOf(ea.Material);
								} else {
									// エッジから属性が取得できなかったら非表示となる
									flags = Flags.EdgeHidden;
									materialId = (ulong)(uint)materials.IndexOf(null);
								}

								// エッジの属性情報からエッジ値を作成する
								// 後の工程で同じエッジ値のエッジがセグメントにまとめられる
								var value = polygonId;
								if ((flags & Flags.EdgeHidden) != 0)
									value ^= 0x7fffL;
								value |= materialId << 15;

								// 未使用領域へ値を入れる
								euv |= hilo == 1 ? value : value << 30;
								edge.UserValue = euv;
							}
						}
					}
				}

#if SETSEG_WRITE
			Dbg.StackWrite(EPolygonsDebug(epolygons), "AfterValue.txt");
#endif

				// 同一コード値が連続する範囲をセグメントとして切り出していく
				var segmentIds = new Dictionary<ulong, int>();
				int segmentId = 0;
				var forceTriSegmentIds = new FList<int>();
				for (int ipolygon = epolygonsCore.Count - 1; ipolygon != -1; ipolygon--) {
					var epolygon = epolygonsCore.Items[ipolygon];
					var eloopsCore = epolygon.Loops.Core;
					for (int iloop = eloopsCore.Count - 1; iloop != -1; iloop--) {
						var segments = new FList<SharedSegment>();
						var eloop = eloopsCore.Items[iloop];
						var edgesCore = eloop.Edges.Core;
						var end = edgesCore.Count;
						ulong lastValue = edgesCore.Items[0].Edge.UserValue;
						int segmentStart = -1;
						for (int i = 1; i <= end; i++) {
							var j = i % edgesCore.Count;
							var euv = edgesCore.Items[j].Edge.UserValue;
							if (euv != lastValue) {
								if (segmentStart == -1) {
									// 最初に変化を見つけた時は終了位置を１回転後の i の位置へ移動する
									end += i;
								} else {
									// ２回め以降の変化はセグメントを登録していく
									var segmentCode = lastValue;

									// まだセグメントとして切り出されていないエッジなら
									// エッジ値をセグメントコードに置き換える
									if ((segmentCode & (1UL << 63)) == 0) {
										segmentCode = (1UL << 63) | (ulong)(uint)segmentId;
										for (int k = segmentStart; k < i; k++)
											edgesCore.Items[k % edgesCore.Count].Edge.UserValue = segmentCode;
									}

									// セグメントコードからセグメントIDを取得し登録する
									int id;
									if (segmentIds.TryGetValue(segmentCode, out id)) {
										segments.Add(new SharedSegment(id, segmentStart, i));
									} else {
										segments.Add(new SharedSegment(segmentId, segmentStart, i));
										segmentIds.Add(segmentCode, segmentId);
										segmentId++;
									}
								}
								segmentStart = i;
							}
							lastValue = euv;
						}
						if (segmentStart == -1) {
							// 変化が無いなら全体を１つのセグメントとする
							var segmentCode = lastValue;

							// まだセグメントとして切り出されていないエッジなら
							// エッジ値をセグメントコードに置き換える
							if ((segmentCode & (1UL << 63)) == 0) {
								segmentCode = (1UL << 63) | (ulong)(uint)segmentId;
								for (int k = edgesCore.Count - 1; k != -1; k--)
									edgesCore.Items[k].Edge.UserValue = segmentCode;
							}

							// セグメントコードからセグメントIDを取得し登録する
							int id;
							if (segmentIds.TryGetValue(segmentCode, out id)) {
								segments.Add(new SharedSegment(id, 0, edgesCore.Count));
							} else {
								segments.Add(new SharedSegment(segmentId, 0, edgesCore.Count));
								segmentIds.Add(segmentCode, segmentId);
								segmentId++;
							}
						}

						// セグメント数が２つの場合頂点数減らした際に三角形未満になり得るので
						// 最低でも三角形以上になることを保証する
						if (segments.Count == 2) {
							for (int i = 1; i != -1; i--) {
								var id = segments[i].Id;
								if (!forceTriSegmentIds.Contains(id))
									forceTriSegmentIds.Add(id);
							}
						}

						eloop.UserData = segments;
					}

					// セグメント設定したことを記録
					epolygon.UserValue |= EPolygonFlags.SegmentsSet;
				}

#if SETSEG_WRITE
			Dbg.StackWrite(EPolygonsDebug(epolygons), "AfterSet.txt");
#endif

				// 三角形以上を保証するセグメントがあるならフラグセットする
				if (forceTriSegmentIds.Count != 0) {
					for (int ipolygon = epolygonsCore.Count - 1; ipolygon != -1; ipolygon--) {
						var eloopsCore = epolygonsCore.Items[ipolygon].Loops.Core;
						for (int iloop = eloopsCore.Count - 1; iloop != -1; iloop--) {
							var segments = eloopsCore.Items[iloop].UserData as FList<SharedSegment>;
							var segmentsCore = segments.Core;
							for (int isegment = segmentsCore.Count - 1; isegment != -1; isegment--) {
								var s = segmentsCore.Items[isegment];
								if (forceTriSegmentIds.Contains(s.Id)) {
									s.ForceTri = true;
									segmentsCore.Items[isegment] = s;
								}
							}
						}
					}
				}

#if SETSEG_WRITE
			var debugSegments = new Dictionary<int, FList<SharedSegment>>();
			for (int ipolygon = npolygons - 1; ipolygon != -1; ipolygon--) {
				var eloops = epolygons[ipolygon].Loops;
				for (int iloop = eloops.Count - 1; iloop != -1; iloop--) {
					var segments = eloops[iloop].UserData as FList<SharedSegment>;
					for (int isegment = segments.Count - 1; isegment != -1; isegment--) {
						var s = segments[isegment];

						FList<SharedSegment> list;
						debugSegments.TryGetValue(s.Id, out list);
						if (list == null) {
							list = new FList<SharedSegment>();
							debugSegments.Add(s.Id, list);
						}

						list.Add(s);
					}
				}
			}

			foreach (var kvp in debugSegments) {
				var first = true;
				int count = 0;
				foreach (var s in kvp.Value) {
					if (first) {
						first = false;
						count = s.End - s.Start;
					} else {
						if (s.End - s.Start != count) {
							Debug.LogWarning(string.Concat("Segment", s.Id, ": Error"));
						}
					}
				}
			}
#endif
			}
		}

#if BOOL2D_LOG
	static string EPolygonsDebug(FList<PolBoolF.EPolygon> epolygons) {
		var sb = new System.Text.StringBuilder();
		for (int ipolygon = 0, npolygons = epolygons.Count; ipolygon < npolygons; ipolygon++) {
			sb.AppendLine(string.Concat("----Polygon", ipolygon + 1, "----"));
			var epolygon = epolygons[ipolygon];
			var eloops = epolygon.Loops;
			for (int iloop = 0, nloops = eloops.Count; iloop < nloops; iloop++) {
				if (iloop != 0)
					sb.AppendLine(string.Concat("--Loop", iloop + 1, "--"));
				var eloop = eloops[iloop];
				for (int i = 0; i < eloop.Edges.Count; i++) {
					sb.AppendLine(eloop.Edges[i].Edge.UserValue.ToString("X16"));
				}
			}
		}
		return sb.ToString();
	}
#endif

		/// <summary>
		/// 指定されたエッジに設定されている属性の中で最も優先順位が高いものを取得する
		/// </summary>
		/// <param name="groups">ブーリアン演算の入力</param>
		/// <param name="edge">対象エッジ</param>
		/// <returns>属性</returns>
		static EdgeAtr FindAtr(FList<FList<polbool.Polygon>> groups, polbool.Edge edge) {
			// 状況に応じて適切な属性を選択する
			// ※グループインデックスが大きい方が後から描画されたグループとなっているため、
			// ※エッジに関わったグループの内インデックスが最大のものから属性を取得する
			var groupMax = Math.Max(edge.RightGroupMax, edge.LeftGroupMax);
			var ea = (edge.GetUserData(true, groupMax) as EdgeAtr) ?? (edge.GetUserData(false, groupMax) as EdgeAtr);

			if (ea != null) {
				// エッジから属性が取得できたらその値を使用する
				return ea;
			} else {
				// エッジから属性が取得できなかったら最大グループインデックスのポリゴンから取得する
				var polygonIndex = edge.RightPolygons[groupMax];
				if (polygonIndex < 0)
					polygonIndex = edge.LeftPolygons[groupMax];
				return new EdgeAtr(0, (groups[groupMax][polygonIndex].UserData as Polygon).EdgeMaterial);
			}
		}

		/// <summary>
		/// エッジの属性を複製する
		/// </summary>
		/// <param name="obj">元属性</param>
		/// <returns>複製された属性</returns>
		static object CloneEdgeAtr(object obj) {
			return (obj as EdgeAtr).Clone();
		}

		/// <summary>
		/// 指定サイズを超える凹凸に有効フラグを付与する
		/// </summary>
		/// <param name="vertices">ポリゴンの頂点配列</param>
		/// <param name="segmentStart">vertices 内でのセグメントの開始インデックス</param>
		/// <param name="segmentEnd">vertices 内でのセグメントの終了インデックス</param>
		/// <param name="segmentEnables">segmentStart からの頂点有効フラグ配列が返る、配列長はセグメントの頂点数</param>
		/// <param name="project">頂点から２次元座標を取得する関数</param>
		/// <param name="threshould">凹凸サイズ</param>
		/// <param name="forceTri">最低でも１つ頂点を有効化し三角形以上になるようにするかどうか</param>
		/// <typeparam name="T">頂点型</typeparam>
		static void EnableVertices<T>(FList<T> vertices, int segmentStart, int segmentEnd, bool[] segmentEnables, Func<T, vector> project, element threshould, bool forceTri) {
			var stack = new Stack<VertexSegment>();
			var verticesCore = vertices.Core;
			var svcount = segmentEnables.Length; // セグメント内の頂点数
			int loopStart, loopEnd;

			if (verticesCore.Count <= 3) {
				// 頂点数が３以下ならこれ以上減らせないから全頂点を有効化して終了
				for (int i = svcount - 1; i != -1; i--)
					segmentEnables[i] = true;
				return;
			}

			if (segmentStart % verticesCore.Count == segmentEnd % verticesCore.Count) {
				// セグメントがループ状の場合
				// AABBを計算しその中心から最も遠い頂点へのベクトルがX軸となるような座標系を想定
				// その座標系で最もX値が大きいものと小さい頂点を選びその２つの頂点でループを切断しセグメントを２つ作る
				//var volume = new volume(from v in vertices select project(v));
				var vol = volume.InvalidValue;
				for (int i = verticesCore.Count - 1; i != -1; i--) {
					vol.Merge(project(verticesCore.Items[i]));
				}
				var c = vol.Center;
				element maxDistance = element.MinValue;
				loopStart = -1;
				vector axisX = vector.Zero;
				for (int i = verticesCore.Count - 1; i != -1; i--) {
					var v = project(verticesCore.Items[i]) - c;
					var d2 = v.LengthSquare;
					if (maxDistance < d2) {
						maxDistance = d2;
						loopStart = i;
						axisX = v;
					}
				}

				loopEnd = -1;
				maxDistance = element.MaxValue;
				for (int i = verticesCore.Count - 1; i != -1; i--) {
					var x = axisX.Dot(project(verticesCore.Items[i]) - c);
					if (x < maxDistance) {
						maxDistance = x;
						loopEnd = i;
					}
				}

				if (loopEnd < loopStart) {
					var t = loopStart;
					loopStart = loopEnd;
					loopEnd = t;
				}

				segmentEnables[loopStart] = true;
				segmentEnables[loopEnd] = true;

				stack.Push(new VertexSegment(loopStart, loopEnd, 0));
				stack.Push(new VertexSegment(loopEnd, loopStart + verticesCore.Count, 0));

				forceTri = true;
			} else {
				loopStart = loopEnd = -1;
				segmentEnables[0] = true;
				segmentEnables[svcount - 1] = true;
				stack.Push(new VertexSegment(segmentStart, segmentEnd, 0));
			}

			// 凹凸が threshould を超える頂点を有効化していく
			while (stack.Count != 0) {
				var seg = stack.Pop();
				var n = seg.End - seg.Start + 1;
				if (n <= 2)
					continue; // セグメントの頂点数が２つ以下ならこれ以上分割できないので終了

				// セグメントがループ状でない場合
				// 開始→終了ベクトルがX軸に重なる様な座標系を想定
				// Y値の絶対値が最も大きい頂点でセグメントを切り分ける
				var o = project(verticesCore.Items[seg.Start % verticesCore.Count]);
				var axisX = (project(verticesCore.Items[seg.End % verticesCore.Count]) - o).Normalize();
				var axisY = new vector(-axisX.Y, axisX.X);
				element maxY = element.MinValue;
				int index = -1;
				for (int i = seg.End - 1; seg.Start < i; i--) {
					var y = Math.Abs(axisY.Dot(project(verticesCore.Items[i % verticesCore.Count]) - o));
					if (maxY < y) {
						maxY = y;
						index = i;
					}
				}

				// 指定値を超える凹凸が無いなら終了
				if (maxY <= threshould) {
					// 必要なら強制的に頂点有効化する
					if (seg.Depth == 0 && forceTri && 0 <= index) {
						segmentEnables[(index - segmentStart) % verticesCore.Count] = true;
					}
					continue;
				}

				// 凹凸部分を有効化する
				segmentEnables[(index - segmentStart) % verticesCore.Count] = true;

				// セグメントを切り分ける
				seg.Depth++;
				stack.Push(new VertexSegment(seg.Start, index, seg.Depth));
				stack.Push(new VertexSegment(index, seg.End, seg.Depth));
			}
		}

		/// <summary>
		/// セグメントに設定されている頂点有効フラグをポリゴン全体頂点用の有効フラグ配列へコピーする
		/// </summary>
		/// <param name="segmentStart">頂点配列内でのセグメントの開始インデックス</param>
		/// <param name="segmentEnables">セグメント内の頂点有効フラグ配列、配列長はセグメント内の頂点数</param>
		/// <param name="reverse">segmentEnables を逆順で参照するかどうか</param>
		/// <param name="enables">ポリゴン全体頂点用の頂点有効フラグ配列</param>
		static void CopyVertexEnables(int segmentStart, bool[] segmentEnables, bool reverse, bool[] enables) {
			var count = enables.Length;
			if (!reverse) {
				for (int i = segmentEnables.Length - 1; i != -1; i--) {
					enables[(segmentStart + i) % count] = segmentEnables[i];
				}
			} else {
				var n = segmentEnables.Length - 1;
				for (int i = n; i != -1; i--) {
					enables[(segmentStart + n - i) % count] = segmentEnables[i];
				}
			}
		}

		/// <summary>
		/// 指定セグメント範囲内で有効な頂点のみ取得する
		/// </summary>
		/// <param name="verticesCore">頂点配列とカウント</param>
		/// <param name="enables">有効フラグ配列</param>
		/// <param name="reducedVertices">有効な頂点が追加される配列</param>
		/// <typeparam name="T">頂点型</typeparam>
		static void ExtractEnabledVertices<T>(FList<T>.CoreElement verticesCore, bool[] enables, FList<T> reducedVertices) {
			for (int i = 0; i < verticesCore.Count; i++) {
				if (enables[i])
					reducedVertices.Add(verticesCore.Items[i]);
			}
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

		/// <summary>
		/// 指定された頂点列を繋ぐ線分に太さを持たせて全体をポリゴン化する
		/// </summary>
		/// <param name="points">頂点列</param>
		/// <param name="start">頂点列内での開始位置</param>
		/// <param name="count">使用頂点数</param>
		/// <param name="width">太さ</param>
		/// <param name="faceMaterial">表面マテリアル</param>
		/// <param name="edgeMaterial">エッジマテリアル</param>
		/// <returns>ポリゴン</returns>
		static Polygon PolygonFromPolyline(vector[] points, int start, int count, float width, Material faceMaterial, Material edgeMaterial) {
			if (count < 2)
				return null;

			var ndiv = 4; // 隙間補間の分割数
			var step = (element)1 / ndiv; // 隙間ベジェ補間時のtステップ

			var p1 = points[start]; // １つ前の頂点
			var p2 = points[start + 1]; // 注目中の頂点
			var v1 = p2 - p1; // １つ前から注目頂点へのベクトル
			var ax1 = v1.Relength(width); // １つ前の直線のベクトルをX軸とする
			var ay1 = ax1.RightAngle(); // １つ前の直線のベクトルを90度回転させてY軸とする

			var u_points = new FList<vector>(); // +Y軸側頂点列
			var l_points = new FList<vector>(); // -Y軸側頂点列

			// 最初の頂点を登録
			BezierCap(u_points, ndiv, step, p1, ax1, ay1);
			u_points.Add(p1 + ay1);
			l_points.Add(p1 - ay1);

			if (3 <= count) {
				var end = start + count;
				for (int i = start + 2; i < end; i++) {
					var p3 = points[i]; // １つ先の頂点
					var v2 = p3 - p2; // 注目頂点から１つ先の頂点へのベクトル
					var ax2 = v2.Relength(width); // １つ先の直線のベクトルをX軸とする
					var ay2 = ax2.RightAngle(); // １つ先の直線のベクトルを90度回転させてY軸とする

					// 折れ曲がっているか調べる
					// 折れ曲がるなら隙間を補間する
					var divisor = geo.LineIntersectDivisor(ax1, ax2);
					if (CalcEpsilon < Math.Abs(divisor)) {
						// 交点を求める
						var u_p1 = p2 + ay1;
						var u_p2 = p2 + ay2;
						var u_pv = u_p2 - u_p1;
						var l_p1 = p2 - ay1;
						var l_p2 = p2 - ay2;
						var l_pv = l_p2 - l_p1;
						var u_t1 = geo.LineIntersectParam(u_pv, ax1, ax2, divisor);
						var l_t1 = -u_t1; //geo.LineIntersectParam(l_pv, v1, v2, divisor);

						if (0 < divisor) {
							// +Y軸側に折れている場合
							var l_t2 = geo.Clip(-l_t1, -BezierCircle, BezierCircle);
							geo.Clip(ref l_t1, -BezierCircle, BezierCircle);
							var u_c = u_p1 + u_t1 * ax1;
							var l_c1 = l_p1 + l_t1 * ax1;
							var l_c2 = l_p2 + l_t2 * ax2;

							u_points.Add(u_c);
							l_points.Add(l_p1);

							// 隙間をベジェで補間
							//BezierInterpolate(l_points, ndiv, step, l_p1, l_c1, l_c2, l_p2);

							l_points.Add(l_p2);
						} else {
							// -Y軸側に折れている場合
							var u_t2 = geo.Clip(-u_t1, -BezierCircle, BezierCircle);
							geo.Clip(ref u_t1, -BezierCircle, BezierCircle);
							var l_c = l_p1 + l_t1 * ax1;
							var u_c1 = u_p1 + u_t1 * ax1;
							var u_c2 = u_p2 + u_t2 * ax2;

							l_points.Add(l_c);
							u_points.Add(u_p1);

							// 隙間をベジェで補間
							//BezierInterpolate(u_points, ndiv, step, u_p1, u_c1, u_c2, u_p2);

							u_points.Add(u_p2);
						}
					}

					p1 = p2;
					p2 = p3;
					v1 = v2;
					ax1 = ax2;
					ay1 = ay2;
				}
			}

			// 最後の頂点を登録
			u_points.Add(p2 + ay1);
			l_points.Add(p2 - ay1);
			BezierCap(u_points, ndiv, step, p2, -ax1, -ay1);

			// 全頂点を合成する
			l_points.Reverse();
			u_points.AddRange(l_points.Core);

			// ポリゴン作成
			var polygon = new Polygon(u_points);
			polygon.FaceMaterial = faceMaterial;
			polygon.EdgeMaterial = edgeMaterial;

			return polygon;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static void BezierInterpolate(FList<vector> points, int ndiv, element step, vector p0, vector p1, vector p2, vector p3) {
			for (int i = 1; i < ndiv; i++)
				points.Add(bezier.Interpolate3(i * step, p0, p1, p2, p3));
		}

		static void BezierCap(FList<vector> points, int ndiv, element step, vector p, vector ax, vector ay) {
			var axb = ax * BezierCircle2;
			var pmay = p - ay;
			var ppay = p + ay;
			BezierInterpolate(points, ndiv, step, pmay, pmay - axb, ppay - axb, ppay);
		}
		#endregion
	}
}
