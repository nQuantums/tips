using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using element = System.Double;
using vector2 = Jk.Vector2d;

namespace Jk {
	/// <summary>
	/// 三角形分割処理クラス
	/// </summary>
	public static class TriangulationD {
		class Node {
			public vector2 Vec2;
			public int Index;
			public element Distance2;
			public Node Prev;
			public Node Next;
		}

		static int Side(vector2 v1, vector2 v2) {
			var a = v1.X * v2.Y - v1.Y * v2.X;
			if (a < 0)
				return -1;
			if (0 < a)
				return 1;
			return 0;
		}

		static int SideOfNormal(vector2 input1, vector2 input2, vector2 input3) {
			return Side(input1 - input2, input1 - input3);
		}

		static bool PointInTriangle(vector2 p1, vector2 p2, vector2 p3, vector2 check) {
			var v1 = check - p1;
			var v2 = check - p2;
			var v3 = check - p3;
			var result1 = Side(v1, v2);
			var result2 = Side(v2, v3);
			var result3 = Side(v3, v1);
			return result1 == result2 && result2 == result3;
		}

		static bool OtherPointsInside(Node node) {
			var p = node.Next.Next;
			var p1 = node.Vec2;
			var p2 = node.Next.Vec2;
			var p3 = node.Prev.Vec2;
			while (true) {
				if (PointInTriangle(p1, p2, p3, p.Vec2))
					return true;
				p = p.Next;
				if (p == node.Prev)
					break;
			}
			return false;
		}

		static Node GetFarthestPoint(Node head) {
			var distance = (element)0;
			var p = head;
			var max = head;
			while (true) {
				var abs = p.Distance2;
				if (abs > distance) {
					distance = abs;
					max = p;
				}
				p = p.Next;
				if (p == head)
					break;
			}
			return max;
		}

		/// <summary>
		/// 三角形分割する
		/// </summary>
		/// <param name="vertices">頂点座標配列</param>
		/// <returns>三角形を構成する頂点インデックス配列</returns>
		public static List<int> Do(vector2[] vertices) {
			var vlen = vertices.Length;
			if (vlen < 3)
				return null;

			var nodes = new Node[vlen];
			for (var i = vlen - 1; i != -1; i--) {
				nodes[i] = new Node();
			}

			var last = nodes[0];
			for (var i = vlen - 1; i != -1; i--) {
				var node = nodes[i];
				node.Vec2 = vertices[i];
				node.Index = i;
				node.Distance2 = node.Vec2.LengthSquare;
				node.Next = last;
				last.Prev = node;
				last = node;
			}
			var head = last;
			var counter = vlen;
			var triangleIndices = new List<int>();

			while (counter > 2) {
				var start = GetFarthestPoint(head);
				var p = head;
				var way = SideOfNormal(start.Vec2, start.Prev.Vec2, start.Next.Vec2);
				p = start;
				var ok = false;
				while (true) {
					if (way == SideOfNormal(p.Vec2, p.Prev.Vec2, p.Next.Vec2)) {
						if (!OtherPointsInside(p)) {
							var tp = p;
							counter--;
							if (p.Prev == p || p.Next == p) {
								head = null;
							} else {
								if (p == head)
									head = p.Next;
								p.Next.Prev = p.Prev;
								p.Prev.Next = p.Next;
							}
							triangleIndices.Add(tp.Index);
							triangleIndices.Add(tp.Next.Index);
							triangleIndices.Add(tp.Prev.Index);
							ok = true;
							break;
						}
					}
					p = p.Next;
					if (p == start)
						break;
				}
				if (!ok)
					break;
			}

			return triangleIndices;
		}
	}
}
