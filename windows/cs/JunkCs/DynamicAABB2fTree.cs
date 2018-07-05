using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using element = System.Single;
using vector = Jk.Vector2f;
using volume = Jk.Range2f;

namespace Jk {
	/// <summary>
	/// 動的境界ボリューム木、Bullet3 の btDbvt を基に作成
	/// </summary>
	/// <typeparam name="T">データ型</typeparam>
	public class DynamicAABB2fTree<T> : IEnumerable<T> {
		#region クラス
		/// <summary>
		/// ノード
		/// </summary>
		public abstract class Node {
			/// <summary>
			/// 境界ボリューム
			/// </summary>
			public volume Volume;

			/// <summary>
			/// 親ノード
			/// </summary>
			public BranchNode Parent;

			/// <summary>
			/// 枝ノードして取得する
			/// </summary>
			public abstract BranchNode AsBranch { get; }

			/// <summary>
			/// 葉ノードして取得する
			/// </summary>
			public abstract LeafNode AsLeaf { get; }

			/// <summary>
			/// このノードの親ノード内でのインデックスを検索する
			/// </summary>
			public int Index {
				get {
					return this.Parent.Child2 == this ? 1 : 0;
				}
			}

			/// <summary>
			/// コンストラクタ
			/// </summary>
			/// <param name="parent">親ノード</param>
			/// <param name="volume">境界ボリューム</param>
			/// <param name="data">データ</param>
			public Node(BranchNode parent, volume volume) {
				this.Volume = volume;
				this.Parent = parent;
			}
		}

		/// <summary>
		/// 枝ノード
		/// </summary>
		public class BranchNode : Node {
			/// <summary>
			/// 子ノード１
			/// </summary>
			public Node Child1;

			/// <summary>
			/// 子ノード２
			/// </summary>
			public Node Child2;

			/// <summary>
			/// 枝ノードして取得する
			/// </summary>
			public override BranchNode AsBranch {
				get {
					return this;
				}
			}

			/// <summary>
			/// 葉ノードして取得する
			/// </summary>
			public override LeafNode AsLeaf {
				get {
					return null;
				}
			}

			/// <summary>
			/// コンストラクタ
			/// </summary>
			/// <param name="parent">親ノード</param>
			/// <param name="volume">境界ボリューム</param>
			public BranchNode(BranchNode parent, volume volume) : base(parent, volume) {
			}

			/// <summary>
			/// コンストラクタ、２つの子ノードの境界ボリュームをマージして初期化する
			/// </summary>
			/// <param name="parent">親ノード</param>
			/// <param name="child1">子ノード１</param>
			/// <param name="child2">子ノード２</param>
			public BranchNode(BranchNode parent, Node child1, Node child2) : base(parent, child1.Volume.Merge(child2.Volume)) {
				this.Child1 = child1;
				this.Child2 = child2;
			}

			/// <summary>
			/// 指定インデックスの子ノードを取得する
			/// </summary>
			/// <param name="index">子ノードインデックス</param>
			/// <returns>子ノード</returns>
			public Node GetChild(int index) {
				return index == 0 ? this.Child1 : this.Child2;
			}

			/// <summary>
			/// 指定インデックスに子ノードを設定する
			/// </summary>
			/// <param name="index">子ノードインデックス</param>
			/// <param name="child">子ノード</param>
			public void SetChild(int index, Node child) {
				if (index == 0)
					this.Child1 = child;
				else
					this.Child2 = child;
			}

			/// <summary>
			/// ２つの子ノードの境界ボリュームを包含するように境界を計算し直す
			/// </summary>
			public void UpdateVolume() {
				this.Volume = this.Child1.Volume.Merge(this.Child2.Volume);
			}
		}

		/// <summary>
		/// 葉ノード
		/// </summary>
		public class LeafNode : Node {
			/// <summary>
			/// 値
			/// </summary>
			public T Value;


			/// <summary>
			/// 枝ノードして取得する
			/// </summary>
			public override BranchNode AsBranch {
				get {
					return null;
				}
			}

			/// <summary>
			/// 葉ノードして取得する
			/// </summary>
			public override LeafNode AsLeaf {
				get {
					return this;
				}
			}

			/// <summary>
			/// コンストラクタ
			/// </summary>
			/// <param name="parent">親ノード</param>
			/// <param name="volume">境界ボリューム</param>
			/// <param name="data">データ</param>
			public LeafNode(BranchNode parent, volume volume, T data) : base(parent, volume) {
				this.Value = data;
			}
		}
		#endregion

		#region フィールド
		static readonly vector[] Axes = new vector[] {
			new vector(1, 0),
			new vector(0, 1)
		};

		/// <summary>
		/// ルートノード
		/// </summary>
		Node _Root;

		/// <summary>
		/// 葉ノード数
		/// </summary>
		int _Count;
		#endregion

		#region プロパティ
		/// <summary>
		/// ルートノード
		/// </summary>
		public Node Root {
			get {
				return _Root;
			}
		}

		/// <summary>
		/// 葉ノード数
		/// </summary>
		public int Count {
			get {
				return _Count;
			}
		}

		/// <summary>
		/// 全ノードを反復処理する列挙子の取得
		/// </summary>
		public IEnumerable<Node> Nodes {
			get {
				Stack<Node> stack = new Stack<Node>(256);
				stack.Push(_Root);
				while (stack.Count != 0) {
					var node = stack.Pop();
					if (node == null)
						continue;

					yield return node;

					var branch = node.AsBranch;
					if (branch != null) {
						stack.Push(branch.Child1);
						stack.Push(branch.Child2);
					}
				}
			}
		}

		/// <summary>
		/// 葉ノードを反復処理する列挙子の取得
		/// </summary>
		public IEnumerable<Node> Leaves {
			get {
				Stack<Node> stack = new Stack<Node>(256);
				stack.Push(_Root);
				while (stack.Count != 0) {
					var node = stack.Pop();
					if (node == null)
						continue;

					var leaf = node.AsLeaf;
					if (leaf != null) {
						yield return leaf;
					} else {
						var branch = node.AsBranch;
						stack.Push(branch.Child1);
						stack.Push(branch.Child2);
					}
				}
			}
		}
		#endregion

		#region 公開メソッド
		/// <summary>
		/// ツリーに指定境界ボリュームの葉ノードを追加する
		/// </summary>
		/// <param name="volume">境界ボリューム</param>
		/// <param name="data">葉ノードに保持するデータ</param>
		/// <returns>葉ノード</returns>
		public Node Add(volume volume, T data) {
			var leaf = new LeafNode(null, volume, data);
			InsertLeaf(_Root, leaf);
			_Count++;
			return leaf;
		}

		/// <summary>
		/// 指定の葉ノードをツリーから取り除く
		/// </summary>
		/// <param name="leaf">葉ノード</param>
		public void Remove(Node leaf) {
			// TODO: leaf がツリー内に無ければ return
			RemoveLeaf(leaf);
			_Count--;
		}

		/// <summary>
		/// ツリーをクリアする
		/// </summary>
		public void Clear() {
			_Root = null;
			_Count = 0;
		}

		/// <summary>
		/// 指定境界ボリュームと接触する葉ノードデータを反復処理する列挙子の取得
		/// </summary>
		/// <param name="volume">境界ボリューム</param>
		/// <returns>列挙子</returns>
		public IEnumerable<T> Query(volume volume) {
			Stack<Node> stack = new Stack<Node>(256);
			stack.Push(_Root);
			while (stack.Count != 0) {
				var node = stack.Pop();
				if (node == null)
					continue;

				if (volume.Intersects(node.Volume)) {
					var leaf = node.AsLeaf;
					if (leaf != null) {
						yield return leaf.Value;
					} else {
						var branch = node.AsBranch;
						stack.Push(branch.Child1);
						stack.Push(branch.Child2);
					}
				}
			}
		}

		/// <summary>
		/// 指定境界ボリュームと接触する葉ノードを反復処理する列挙子の取得
		/// </summary>
		/// <param name="volume">境界ボリューム</param>
		/// <returns>列挙子</returns>
		public IEnumerable<LeafNode> QueryLeaves(volume volume) {
			Stack<Node> stack = new Stack<Node>(256);
			stack.Push(_Root);
			while (stack.Count != 0) {
				var node = stack.Pop();
				if (node == null)
					continue;

				if (volume.Intersects(node.Volume)) {
					var leaf = node.AsLeaf;
					if (leaf != null) {
						yield return leaf;
					} else {
						var branch = node.AsBranch;
						stack.Push(branch.Child1);
						stack.Push(branch.Child2);
					}
				}
			}
		}

		/// <summary>
		/// 葉ノードからツリーを最適化する、葉ノード組み合わせ総当りで調べるので数が多くなるととても遅い
		/// </summary>
		public void OptimizeBottomUp() {
			if (_Count == 0)
				return;
			var leaves = new List<Node>(this.Leaves);
			Bottomup(leaves);
			_Root = leaves[0];
		}

		/// <summary>
		/// 根ノードからツリーを最適化する、葉ノード数がしきい値以下なら OptimizeBottomUp() と同じ処理を行う
		/// </summary>
		/// <param name="treshold">[in] 葉ノード数しきい値</param>
		public void OptimizeTopDown(int treshold = 128) {
			if (_Count == 0)
				return;
			_Root = Topdown(new List<Node>(this.Leaves));
		}
		#endregion

		#region 非公開メソッド
		/// <summary>
		/// 指定ノードに葉ノードを追加する
		/// </summary>
		/// <param name="root">追加先ノード</param>
		/// <param name="leaf">追加する葉ノード</param>
		private void InsertLeaf(Node root, LeafNode leaf) {
			if (_Root == null) {
				// ツリーが空なら追加ノードをルートとする
				_Root = leaf;
				leaf.Parent = null;
			} else {
				// 兄弟となる葉ノードを探す、境界ボリュームの距離が近い方へ辿っていく
				LeafNode sibling = root.AsLeaf;
				BranchNode branch = root.AsBranch;
				if (branch != null) {
					var volume = leaf.Volume;
					while(true) {
						var c = branch.GetChild(Select(volume, branch.Child1.Volume, branch.Child2.Volume));
						sibling = c.AsLeaf;
						if (sibling != null)
							break;
						branch = c.AsBranch;
					}
				}

				// 追加ノードと兄弟ノードの親となるノードを作成
				var prev = sibling.Parent;
				branch = new BranchNode(prev, sibling, leaf);

				if (prev != null) {
					// 見つかった兄弟ノードに親があるなら、親ノードの子をすり替えて根に向かって境界ボリュームを計算し直す
					prev.SetChild(sibling.Index, branch);
					sibling.Parent = branch;
					leaf.Parent = branch;
					do {
						if (prev.Volume.Contains(branch.Volume))
							break;
						prev.UpdateVolume();
						branch = prev;
					} while (null != (prev = branch.Parent));
				} else {
					// 見つかった兄弟ノードに親が無いなら、作成した親ノードをルートとする
					sibling.Parent = branch;
					leaf.Parent = branch;
					_Root = branch;
				}
			}
		}

		/// <summary>
		/// 指定の葉ノードを取り除く
		/// </summary>
		/// <param name="leaf">葉ノード</param>
		/// <returns>ノード取り除きにより影響があった最も親ノード</returns>
		private Node RemoveLeaf(Node leaf) {
			if (leaf == _Root) {
				// 指定のノードがルートならツリーを空にする
				_Root = null;
				return null;
			} else {
				var parent = leaf.Parent;
				var prev = parent.Parent;
				var sibling = parent.GetChild(1 - leaf.Index);
				if (prev != null) {
					// 親の親ノードが存在するなら
					// 取り除く葉ノードの兄弟だったものを親の親ノードにセットする
					prev.SetChild(parent.Index, sibling);
					sibling.Parent = prev;
					while (prev != null) {
						var pb = prev.Volume;
						prev.UpdateVolume();
						if (pb == prev.Volume)
							break;
						prev = prev.Parent;
					}
					return prev != null ? prev : _Root;
				} else {
					// 親の親ノードが存在しないなら
					// 取り除く葉ノードの兄弟だったものをルートノードとする
					_Root = sibling;
					sibling.Parent = null;
					return _Root;
				}
			}
		}

		/// <summary>
		/// 葉ノードからツリーを最適化する、葉ノード組み合わせ総当りで調べるので数が多くなるととても遅い
		/// </summary>
		/// <param name="leaves">[in,out] 葉ノードリスト</param>
		private void Bottomup(List<Node> leaves) {
			var nleaves = leaves.Count;

			while (nleaves > 1) {
				// マージした境界ボリュームの体積＋辺長さが最小になる組み合わせを探す
				var minsize = element.MaxValue;
				var minidx1 = -1;
				var minidx2 = -1;
				for (int i = 0; i < nleaves; ++i) {
					var vol = leaves[i].Volume;
					for (int j = i + 1; j < nleaves; ++j) {
						var sz = vol.Merge(leaves[j].Volume).VolumeAndEdgesLength;
						if (sz < minsize) {
							minsize = sz;
							minidx1 = i;
							minidx2 = j;
						}
					}
				}

				// 見つかった組み合わせを子にもつ枝を作成し leaves に追加し、
				// 見つかった組み合わせを leaves から削除する
				var n1 = leaves[minidx1];
				var n2 = leaves[minidx2];
				var p = new BranchNode(null, n1, n2);
				p.Child1 = n1;
				p.Child2 = n2;
				n1.Parent = p;
				n2.Parent = p;
				leaves[minidx1] = p;
				leaves[minidx2] = leaves[nleaves - 1];
				leaves.RemoveAt(nleaves - 1);
				nleaves--;
			}
		}

		/// <summary>
		/// 葉の数がしきい値より大きいならベストな軸を探し出し＋側と－側に分けることでバランスをとる、しきい値以下なら葉ノード組み合わせ総当りでバランスをとる
		/// </summary>
		/// <param name="leaves">[in] 葉ノード列</param>
		/// <param name="treshold">[in] 葉ノード数しきい値</param>
		/// <returns>根となるノード</returns>
		private Node Topdown(List<Node> leaves, int treshold = 128) {
			var axes = Axes;
			var naxes = axes.Length;
			var nleaves = leaves.Count;

			if (nleaves > 1) {
				if (nleaves > treshold) {
					// 葉の数がしきい値より大きい場合にはベストな軸を探し出し
					// その軸に対して＋側と－側に分けることでバランスをとる

					var vol = Bounds(leaves);
					var org = vol.Center;
					var sets = new List<Node>[] { new List<Node>(), new List<Node>() };
					int bestaxis = -1;
					int bestmidp = nleaves;
					var splitcount = new int[naxes, 2];

					// leaves 全体を包含する境界矩形の中心を原点とし
					// 各教会矩形中心が軸の＋－どちら側に存在するかカウントしていく
					for (int i = 0; i < nleaves; ++i) {
						var c = leaves[i].Volume.Center - org;
						for (int j = 0; j < naxes; ++j) {
							++splitcount[j, c.Dot(axes[j]) > 0 ? 1 : 0];
						}
					}

					// ＋－両側にバランス良く散らばってる軸を探す
					for (int i = 0; i < naxes; ++i) {
						if ((splitcount[i, 0] > 0) && (splitcount[i, 1] > 0)) {
							int midp = (int)Math.Abs(splitcount[i, 0] - splitcount[i, 1]);
							if (midp < bestmidp) {
								bestaxis = i;
								bestmidp = midp;
							}
						}
					}

					// 軸が見つかったらその軸に対して＋と－側に分ける
					// 見つからなかったら奇数と偶数で分ける
					if (bestaxis >= 0) {
						Split(leaves, sets[0], sets[1], org, axes[bestaxis]);
					} else {
						for (int i = 0; i < nleaves; ++i) {
							sets[i & 1].Add(leaves[i]);
						}
					}

					// 枝ノードを作成し分けた配列に対して再帰的に親子関係を作っていく
					var node = new BranchNode(null, vol);
					node.Child1 = Topdown(sets[0], treshold);
					node.Child2 = Topdown(sets[1], treshold);
					node.Child1.Parent = node;
					node.Child2.Parent = node;
					return node;
				} else {
					// 葉の数がしきい値以下なら葉ノード組み合わせ総当りでバランスをとる
					Bottomup(leaves);
					return leaves[0];
				}
			}

			return leaves[0];
		}

		/// <summary>
		/// 指定されたノード列の全境界ボリュームを包含する境界ボリュームを計算する
		/// </summary>
		/// <param name="leaves">[in] ノード列</param>
		/// <returns>境界ボリューム</returns>
		static volume Bounds(List<Node> leaves) {
			var volume = leaves[0].Volume;
			for (int i = 1, n = leaves.Count; i < n; i++) {
				volume = volume.Merge(leaves[i].Volume);
			}
			return volume;
		}

		/// <summary>
		/// 指定されたノード列の境界ボリューム中心座標により配列を２つに分ける
		/// </summary>
		/// <param name="leaves">[in] 元のノード列</param>
		/// <param name="left">[out] org を原点としてノードの境界ボリューム中心座標が axis 軸方向に－側のものが追加される</param>
		/// <param name="right">[out] org を原点としてノードの境界ボリューム中心座標が axis 軸方向に＋側のものが追加される</param>
		/// <param name="org">[in] 原点とする座標</param>
		/// <param name="axis">[in] 軸のベクトル</param>
		static void Split(List<Node> leaves, List<Node> left, List<Node> right, vector org, vector axis) {
			left.Clear();
			right.Clear();
			for (int i = 0, n = leaves.Count; i < n; ++i) {
				if (axis.Dot(leaves[i].Volume.Center - org) <= 0)
					left.Add(leaves[i]);
				else
					right.Add(leaves[i]);
			}
		}

		static int Select(volume o, volume a, volume b) {
			return Proximity(o, a) < Proximity(o, b) ? 0 : 1;
		}

		static element Proximity(volume a, volume b) {
			var d = (a.Min + a.Max) - (b.Min + b.Max);
			return d.Sum();
		}
		#endregion

		#region コレクション関係
		/// <summary>
		/// コレクションを反復処理する列挙子の取得
		/// </summary>
		/// <returns>列挙子</returns>
		public IEnumerator<T> GetEnumerator() {
			Stack<Node> stack = new Stack<Node>(256);
			stack.Push(_Root);
			while (stack.Count != 0) {
				var node = stack.Pop();
				if (node == null)
					continue;

				var leaf = node.AsLeaf;
				if (leaf != null) {
					yield return leaf.Value;
				} else {
					var branch = node.AsBranch;
					stack.Push(branch.Child1);
					stack.Push(branch.Child2);
				}
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			Stack<Node> stack = new Stack<Node>(256);
			stack.Push(_Root);
			while (stack.Count != 0) {
				var node = stack.Pop();
				if (node == null)
					continue;

				var leaf = node.AsLeaf;
				if (leaf != null) {
					yield return leaf.Value;
				} else {
					var branch = node.AsBranch;
					stack.Push(branch.Child1);
					stack.Push(branch.Child2);
				}
			}
		}
		#endregion
	}
}
