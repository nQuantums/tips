using System;
using System.Collections.Generic;

using element = System.Single;
using vector = Jk.Vector2f;

namespace Jk {
	/// <summary>
	/// 三角形分割クラス、Carveライブラリから三角形分割部分のみ移植
	/// </summary>
	public class TriangulationF {
		#region 内部クラス
		#region 汎用
		public struct TriIdx {
			public int A, B, C;

			public int this[int index] {
				get {
					switch (index) {
					case 0: return A;
					case 1: return B;
					case 2: return C;
					default: throw new NotImplementedException();
					}
				}
				set {
					switch (index) {
					case 0: A = value; break;
					case 1: B = value; break;
					case 2: C = value; break;
					default: throw new NotImplementedException();
					}
				}
			}

			public TriIdx(int _a, int _b, int _c) {
				this.A = _a;
				this.B = _b;
				this.C = _c;
			}
		}

		public delegate vector Project<T>(T vertex);

		struct Iterator<T> {
			public FList<T> list;
			public int index;

			public T value {
				get {
					return list[index];
				}
				set {
					list[index] = value;
				}
			}

			public Iterator(FList<T> list, int index) {
				this.list = list;
				this.index = index;
			}
		}

		struct LineSegment2 {
			public vector v1;
			public vector v2;
			public vector midpoint;
			public vector half_length;

			public void update() {
				midpoint = (v2 + v1) / 2;
				half_length = (v2 - v1) / 2;
			}

			public bool OK() {
				return !half_length.IsZero;
			}

			public void flip() {
				var t = v1;
				v1 = v2;
				v2 = t;
				half_length = (v2 - v1) / 2;
			}

			//public aabb<ndim> getAABB() {
			//}

			public LineSegment2(vector _v1, vector _v2) {
				v1 = _v1;
				v2 = _v2;
				midpoint = (v2 + v1) / 2;
				half_length = (v2 - v1) / 2;
			}
		}

		class EarQueue {
			PriorityQueue<vertex_info> queue = new PriorityQueue<vertex_info>(new vertex_info_ordering());

			void checkheap() {
			}

			public EarQueue() {
			}

			public int size() {
				return queue.Count;
			}

			public void push(vertex_info v) {
				queue.Push(v);
			}

			public vertex_info pop() {
				return queue.Pop();
			}

			public void remove(vertex_info v) {
				var score = v.score;
				if (v != queue.List[0]) {
					v.score = queue.List[0].score + 1;
					queue.UpdateHeap();
				}
				queue.Pop();
				v.score = score;
			}

			public void changeScore(vertex_info v, element score) {
				if (v.score != score) {
					v.score = score;
					queue.UpdateHeap();
				}
			}

			// 39% of execution time
			public void updateVertex(vertex_info v) {
				var spre = v.score;
				var qpre = v.isCandidate();
				v.recompute();
				var qpost = v.isCandidate();
				var spost = v.score;

				v.score = spre;

				if (qpre) {
					if (qpost) {
						if (v.score != spre) {
							changeScore(v, spost);
						}
					} else {
						remove(v);
					}
				} else {
					if (qpost) {
						push(v);
					}
				}
			}
		}

		/// <summary>
		/// Maintains a linked list of untriangulated vertices during a triangulation operation.
		/// </summary>
		public class vertex_info {
			public vertex_info prev;
			public vertex_info next;
			public vector p;
			public int idx;
			public element score;
			public bool convex;
			public bool failed;

			public vertex_info(vector _p, int _idx) {
				this.p = _p;
				this.idx = _idx;
			}

			public static element triScore(vertex_info p, vertex_info v, vertex_info n) {
				// range: 0 - 1
				element a, b, c;

				bool convex = isLeft(p, v, n);
				if (!convex) return -1e-5f;

				a = (n.p - v.p).Length;
				b = (p.p - n.p).Length;
				c = (v.p - p.p).Length;

				if (a < 1e-10 || b < 1e-10 || c < 1e-10) return 0;

				return Math.Max(Math.Min((a + b) / c, Math.Min((a + c) / b, (b + c) / a)) - 1, 0);
			}

			public element calcScore() {
				var this_tri = triScore(prev, this, next);
				var next_tri = triScore(prev, next, next.next);
				var prev_tri = triScore(prev.prev, prev, next);

				return this_tri + Math.Max(next_tri, prev_tri) * .2f;
			}

			public void recompute() {
				score = calcScore();
				convex = isLeft(prev, this, next);
				failed = false;
			}

			public bool isCandidate() {
				return convex && !failed;
			}

			public void remove() {
				next.prev = prev;
				prev.next = next;
			}

			public bool isClipable() {
				for (vertex_info v_test = next.next; v_test != prev; v_test = v_test.next) {
					if (v_test.convex) {
						continue;
					}

					if (v_test.p == prev.p ||
						v_test.p == next.p) {
						continue;
					}

					if (v_test.p == p) {
						if (v_test.next.p == prev.p &&
							v_test.prev.p == next.p) {
							return false;
						}
						if (v_test.next.p == prev.p ||
							v_test.prev.p == next.p) {
							continue;
						}
					}

					if (pointInTriangle(prev, this, next, v_test)) {
						return false;
					}
				}
				return true;
			}

			public vertex_info Clone() {
				return this.MemberwiseClone() as vertex_info;
			}
		}
		#endregion

		#region 比較オブジェクト
		/// <summary>
		/// Provides an ordering of hole loops based upon a single projected axis.
		/// </summary>
		/// <typeparam name="T">vertex</typeparam>
		class order_h_loops<T> : IComparer<T> {
			Project<T> project;
			int axis;

			/**
			 * 
			 * @param _project The projection functor.
			 * @param _axis The axis of the 2d projection upon which hole
			 *              loops are ordered.
			 */
			public order_h_loops(Project<T> _project, int _axis) {
				project = _project;
				axis = _axis;
			}

			public int Compare(T a, T b) {
				return axisOrdering(project(a), project(b), axis);
			}
		}

		/// <summary>
		/// Provides an ordering of hole loops based upon a single projected axis.
		/// </summary>
		/// <typeparam name="T">vertex</typeparam>
		class order_h_loops_iterator<T> : IComparer<Iterator<T>> {
			Project<T> project;
			int axis;

			/**
			 * 
			 * @param _project The projection functor.
			 * @param _axis The axis of the 2d projection upon which hole
			 *              loops are ordered.
			 */
			public order_h_loops_iterator(Project<T> _project, int _axis) {
				project = _project;
				axis = _axis;
			}

			public int Compare(Iterator<T> a, Iterator<T> b) {
				return axisOrdering(project(a.value), project(b.value), axis);
			}
		}

		/// <summary>
		/// Provides an ordering of vertex indicies in a polygon loop according to proximity to a vertex.
		/// </summary>
		/// <typeparam name="T">vertex</typeparam>
		class heap_ordering<T> : IComparer<int> {
			Project<T> project;
			FList<T> loop;
			vector p;
			int axis;

			/** 
			 * 
			 * @param _project A functor which converts vertices to a 2d
			 *                 projection.
			 * @param _loop The polygon loop which indices address.
			 * @param _vert The vertex from which distance is measured.
			 * 
			 */
			public heap_ordering(Project<T> _project, FList<T> _loop, T _vert, int _axis) {
				project = _project;
				loop = _loop;
				p = _project(_vert);
				axis = _axis;
			}

			public int Compare(int a, int b) {
				var pa = project(loop[a]);
				var pb = project(loop[b]);
				var da = (p - pa).LengthSquare;
				var db = (p - pb).LengthSquare;
				if (da > db) return -1;
				if (da < db) return 1;
				return axisOrdering(pa, pb, axis);
			}
		}

		class vertex_info_ordering : IComparer<vertex_info> {
			public int Compare(vertex_info a, vertex_info b) {
				return Math.Sign(a.score - b.score);
			}
		}

		class vertex_info_l2norm_inc_ordering : IComparer<vertex_info> {
			vertex_info v;

			public vertex_info_l2norm_inc_ordering(vertex_info _v) {
				v = _v;
			}

			public int Compare(vertex_info a, vertex_info b) {
				return Math.Sign((v.p - b.p).LengthSquare - (v.p - a.p).LengthSquare);
			}
		}
		#endregion
		#endregion

		#region 公開メソッド
		public static FList<T> incorporateHolesIntoPolygon<T>(Project<T> project, FList<FList<T>> loops) {
			if (loops.Count <= 1)
				return loops[0];
			var holes = new FList<FList<T>>(loops);
			holes.RemoveAt(0);
			return incorporateHolesIntoPolygon(project, loops[0], holes);
		}


		/** 
		 * \brief Merge a set of holes into a polygon. (templated)
		 *
		 * Take a polygon loop and a collection of hole loops, and patch
		 * the hole loops into the polygon loop, returning a vector of
		 * vertices from the polygon and holes, which describes a new
		 * polygon boundary with no holes. The new polygon boundary is
		 * constructed via the addition of edges * joining the polygon
		 * loop to the holes.
		 * 
		 * This may be applied to arbitrary vertex data (generally
		 * carve::geom3d::Vertex pointers), but a projection function must
		 * be supplied to convert vertices to coordinates in 2-space, in
		 * which the work is performed.
		 *
		 * @tparam project_t A functor which converts vertices to a 2d
		 *                   projection.
		 * @tparam vert_t    The vertex type.
		 * @param project The projection functor.
		 * @param f_loop The polygon loop into which holes are to be
		 *               incorporated.
		 * @param h_loops The set of hole loops to be incorporated.
		 * 
		 * @return A vector of vertex pointers.
		 */
		public static FList<T> incorporateHolesIntoPolygon<T>(Project<T> project, FList<T> f_loop, FList<FList<T>> h_loops) {
			var N = f_loop.Count;

			// work out how much space to reserve for the patched in holes.
			for(int i = h_loops.Count - 1; i != -1; i--) {
				N += 2 + h_loops[i].Count;
			}

			// this is the vector that we will build the result in.
			var current_f_loop = new FList<T>(N);
			current_f_loop.AddRange(f_loop);

			var h_loop_min_vertex = new FList<Iterator<T>>(h_loops.Count);

			// find the major axis for the holes - this is the axis that we
			// will sort on for finding vertices on the polygon to join
			// holes up to.
			//
			// it might also be nice to also look for whether it is better
			// to sort ascending or descending.
			// 
			// another trick that could be used is to modify the projection
			// by 90 degree rotations or flipping about an axis. just as
			// long as we keep the carve::geom3d::Vector pointers for the
			// real data in sync, everything should be ok. then we wouldn't
			// need to accomodate axes or sort order in the main loop.

			// find the bounding box of all the holes.
			bool first = true;
			element min_x = element.MaxValue, min_y = element.MaxValue, max_x = element.MinValue, max_y = element.MinValue;
			for (int i = h_loops.Count - 1; i != -1; i--) {
				var hole = h_loops[i];
				for (int j = hole.Count - 1; j != -1; j--) {
					var curr = project(hole[j]);
					if (first) {
						min_x = max_x = curr.X;
						min_y = max_y = curr.Y;
						first = false;
					} else {
						if (curr.X < min_x) min_x = curr.X;
						if (curr.Y < min_y) min_y = curr.Y;
						if (max_x < curr.X) max_x = curr.X;
						if (max_y < curr.Y) max_y = curr.Y;
					}
				}
			}

			// choose the axis for which the bbox is largest.
			int axis = (max_x - min_x) > (max_y - min_y) ? 0 : 1;

			// for each hole, find the minimum vertex in the chosen axis.
			for (int i = 0, n = h_loops.Count; i < n; i++) {
				var hole = h_loops[i];
				var best_i = MinElementIndex(hole, new order_h_loops<T>(project, axis));
				h_loop_min_vertex.Add(new Iterator<T>(hole, best_i));
			}

			// sort the holes by the minimum vertex.
			h_loop_min_vertex.Sort(new order_h_loops_iterator<T>(project, axis));

			// now, for each hole, find a vertex in the current polygon loop that it can be joined to.
			for (int i = 0; i < h_loop_min_vertex.Count; ++i) {
				var N_f_loop = current_f_loop.Count;

				// the index of the vertex in the hole to connect.
				var h_loop_connect = h_loop_min_vertex[i].value;

				var hole_min = project(h_loop_connect);

				// we order polygon loop vertices that may be able to be connected
				// to the hole vertex by their distance to the hole vertex
				var f_loop_heap = new PriorityQueue<int>(new heap_ordering<T>(project, current_f_loop, h_loop_connect, axis), N);

				for (int j = 0; j < N_f_loop; ++j) {
					// it is guaranteed that there exists a polygon vertex with
					// coord < the min hole coord chosen, which can be joined to
					// the min hole coord without crossing the polygon
					// boundary. also, because we merge holes in ascending
					// order, it is also true that this join can never cross
					// another hole (and that doesn't need to be tested for).
					if (project(current_f_loop[j])[axis] <= hole_min[axis]) {
						f_loop_heap.Push(j);
					}
				}

				// we are going to test each potential (according to the
				// previous test) polygon vertex as a candidate join. we order
				// by closeness to the hole vertex, so that the join we make
				// is as small as possible. to test, we need to check the
				// joining line segment does not cross any other line segment
				// in the current polygon loop (excluding those that have the
				// vertex that we are attempting to join with as an endpoint).
				var attachment_point = current_f_loop.Count;

				while (f_loop_heap.Count != 0) {
					var curr = f_loop_heap.Pop();
					// test the candidate join from current_f_loop[curr] to hole_min

					if (!testCandidateAttachment(project, current_f_loop, curr, hole_min)) {
						continue;
					}

					attachment_point = curr;
					break;
				}

				if (attachment_point == current_f_loop.Count) {
					throw new ApplicationException("didn't manage to link up hole!");
				}

				patchHoleIntoPolygon(current_f_loop, attachment_point, h_loop_min_vertex[i]);
			}

			return current_f_loop;
		}

		public static void triangulate<T>(Project<T> project, FList<T> poly, FList<TriIdx> result) {
			var N = poly.Count;

			result.Clear();
			if (N < 3) {
				return;
			}

			result.Capacity = poly.Count - 2;

			if (N == 3) {
				result.Add(new TriIdx(0, 1, 2));
				return;
			}

			var vinfo = new vertex_info[N];

			vinfo[0] = new vertex_info(project(poly[0]), 0);
			for (int i = 1; i < N - 1; ++i) {
				vinfo[i] = new vertex_info(project(poly[i]), i);
				vinfo[i].prev = vinfo[i - 1];
				vinfo[i - 1].next = vinfo[i];
			}
			vinfo[N - 1] = new vertex_info(project(poly[N - 1]), N - 1);
			vinfo[N - 1].prev = vinfo[N - 2];
			vinfo[N - 1].next = vinfo[0];
			vinfo[0].prev = vinfo[N - 1];
			vinfo[N - 2].next = vinfo[N - 1];

			for (int i = 0; i < N; ++i) {
				vinfo[i].recompute();
			}

			var begin = vinfo[0];

			removeDegeneracies(ref begin, result);

			doTriangulate(begin, result);
		}
		#endregion

		#region 非公開メソッド
		static int axisOrdering(vector a, vector b, int axis) {
			var a1 = a[axis];
			var b1 = b[axis];
			if (a1 < b1) return -1;
			if (a1 > b1) return 1;
			var a2 = a[1 - axis];
			var b2 = b[1 - axis];
			if (a2 < b2) return -1;
			if (a2 > b2) return 1;
			return 0;
		}

		static element orient2d(vector a, vector b, vector c) {
			var acx = a.X - c.X;
			var bcx = b.X - c.X;
			var acy = a.Y - c.Y;
			var bcy = b.Y - c.Y;
			return acy * bcx - acx * bcy;
		}

		static bool isLeft(vertex_info a, vertex_info b, vertex_info c) {
			if (a.idx < b.idx && b.idx < c.idx) {
				return orient2d(a.p, b.p, c.p) > 0.0;
			} else if (a.idx < c.idx && c.idx < b.idx) {
				return orient2d(a.p, c.p, b.p) < 0.0;
			} else if (b.idx < a.idx && a.idx < c.idx) {
				return orient2d(b.p, a.p, c.p) < 0.0;
			} else if (b.idx < c.idx && c.idx < a.idx) {
				return orient2d(b.p, c.p, a.p) > 0.0;
			} else if (c.idx < a.idx && a.idx < b.idx) {
				return orient2d(c.p, a.p, b.p) > 0.0;
			} else {
				return orient2d(c.p, b.p, a.p) < 0.0;
			}
		}

		static bool pointInTriangle(vertex_info a, vertex_info b, vertex_info c, vertex_info d) {
			return !isLeft(a, c, d) && !isLeft(b, a, d) && !isLeft(c, b, d);
		}

		static int removeDegeneracies(ref vertex_info begin, FList<TriIdx> result) {
			vertex_info v;
			vertex_info n;
			int count = 0;
			int remain = 0;

			v = begin;
			do {
				v = v.next;
				++remain;
			} while (v != begin);

			v = begin;
			do {
				if (remain < 4) break;

				bool remove = false;
				if (v.p == v.next.p) {
					remove = true;
				} else if (v.p == v.next.next.p) {
					if (v.next.p == v.next.next.next.p) {
						// a 'z' in the loop: z (a) b a b c . remove a-b-a . z (a) a b c . remove a-a-b (next loop) . z a b c
						// z --(a)-- b
						//         /
						//        /
						//      a -- b -- d
						remove = true;
					} else {
						// a 'shard' in the loop: z (a) b a c d . remove a-b-a . z (a) a b c d . remove a-a-b (next loop) . z a b c d
						// z --(a)-- b
						//         /
						//        /
						//      a -- c -- d
						// n.b. can only do this if the shard is pointing out of the polygon. i.e. b is outside z-a-c
						remove = !internalToAngle(v.next.next.next, v, v.prev, v.next.p);
					}
				}

				if (remove) {
					result.Add(new TriIdx(v.idx, v.next.idx, v.next.next.idx));
					n = v.next;
					if (n == begin) begin = n.next;
					n.remove();
					count++;
					remain--;
				} else {
					v = v.next;
				}
			} while (v != begin);

			return count;
		}

		static bool splitAndResume(vertex_info begin, FList<TriIdx> result) {
			vertex_info v1, v2;

			if (!findDiagonal(begin, out v1, out v2)) return false;

			vertex_info v1_copy = v1.Clone();
			vertex_info v2_copy = v2.Clone();

			v1.next = v2;
			v2.prev = v1;

			v1_copy.next.prev = v1_copy;
			v2_copy.prev.next = v2_copy;

			v1_copy.prev = v2_copy;
			v2_copy.next = v1_copy;

			bool r1 = doTriangulate(v1, result);
			bool r2 = doTriangulate(v1_copy, result);
			return r1 && r2;
		}

		static bool findDiagonal(vertex_info begin, out vertex_info v1, out vertex_info v2) {
			vertex_info t;
			var heap = new FList<vertex_info>();

			v1 = begin;
			do {
				heap.Clear();

				for (v2 = v1.next.next; v2 != v1.prev; v2 = v2.next) {
					if (!internalToAngle(v1.next, v1, v1.prev, v2.p) ||
						!internalToAngle(v2.next, v2, v2.prev, v1.p)) continue;

					PriorityQueue<vertex_info>.PushHeap(heap, v2, new vertex_info_l2norm_inc_ordering(v1));
				}

				while (heap.Count != 0) {
					v2 = PriorityQueue<vertex_info>.PopHeap(heap, new vertex_info_l2norm_inc_ordering(v1));

					// test whether v1-v2 is a valid diagonal.
					var v_min_x = Math.Min(v1.p.X, v2.p.X);
					var v_max_x = Math.Max(v1.p.X, v2.p.X);

					bool intersected = false;

					for (t = v1.next; !intersected && t != v1.prev; t = t.next) {
						vertex_info u = t.next;
						if (t == v2 || u == v2) continue;

						var l1 = orient2d(v1.p, v2.p, t.p);
						var l2 = orient2d(v1.p, v2.p, u.p);

						if ((l1 > 0.0 && l2 > 0.0) || (l1 < 0.0 && l2 < 0.0)) {
							// both on the same side; no intersection
							continue;
						}

						var dx13 = v1.p.X - t.p.X;
						var dy13 = v1.p.Y - t.p.Y;
						var dx43 = u.p.X - t.p.X;
						var dy43 = u.p.Y - t.p.Y;
						var dx21 = v2.p.X - v1.p.X;
						var dy21 = v2.p.Y - v1.p.Y;
						var ua_n = dx43 * dy13 - dy43 * dx13;
						var ub_n = dx21 * dy13 - dy21 * dx13;
						var u_d = dy43 * dx21 - dx43 * dy21;

						if (Math.Abs(u_d) < element.Epsilon) {
							// parallel
							if (Math.Abs(ua_n) < element.Epsilon) {
								// colinear
								if (Math.Max(t.p.X, u.p.X) >= v_min_x && Math.Min(t.p.X, u.p.X) <= v_max_x) {
									// colinear and intersecting
									intersected = true;
								}
							}
						} else {
							// not parallel
							var ua = ua_n / u_d;
							var ub = ub_n / u_d;

							if (0 <= ua && ua <= 1 && 0 <= ub && ub <= 1) {
								intersected = true;
							}
						}
					}

					if (!intersected) {
						// test whether midpoint winding == 1

						var mid = (v1.p + v2.p) / 2;
						if (windingNumber(begin, mid) == 1) {
							// this diagonal is ok
							return true;
						}
					}
				}

				// couldn't find a diagonal from v1 that was ok.
				v1 = v1.next;
			} while (v1 != begin);
			return false;
		}

		/** 
		 * \brief Determine whether p is internal to the anticlockwise
		 *        angle abc, where b is the apex of the angle.
		 *
		 * @param[in] a 
		 * @param[in] b 
		 * @param[in] c 
		 * @param[in] p 
		 * 
		 * @return true, if p is contained in the anticlockwise angle from
		 *               b->a to b->c. Reflex angles contain p if p lies
		 *               on b->a or on b->c. Acute angles do not contain p
		 *               if p lies on b->a or on b->c. This is so that
		 *               internalToAngle(a,b,c,p) = !internalToAngle(c,b,a,p)
		 */
		static bool internalToAngle(vector a, vector b, vector c, vector p) {
			bool reflex = a.LessIdThan(c) ? orient2d(b, a, c) <= 0.0 : orient2d(b, c, a) > 0.0;
			var d1 = orient2d(b, a, p);
			var d2 = orient2d(b, c, p);
			if (reflex) {
				return d1 >= 0.0 || d2 <= 0.0;
			} else {
				return d1 > 0.0 && d2 < 0.0;
			}
		}

		/** 
		 * \brief Determine whether p is internal to the anticlockwise
		 *        angle ac, with apex at (0,0).
		 *
		 * @param[in] a 
		 * @param[in] c 
		 * @param[in] p 
		 * 
		 * @return true, if p is contained in a0c.
		 */
		static bool internalToAngle(vector a,
									vector c,
									vector p) {
			return internalToAngle(a, vector.Zero, c, p);
		}

		static bool internalToAngle(vertex_info a, vertex_info b, vertex_info c, vector p) {
			return internalToAngle(a.p, b.p, c.p, p);
		}

		static int windingNumber(vertex_info begin, vector point) {
			int wn = 0;

			vertex_info v = begin;
			do {
				if (v.p.Y <= point.Y) {
					if (v.next.p.Y > point.Y && orient2d(v.p, v.next.p, point) > 0) {
						++wn;
					}
				} else {
					if (v.next.p.Y <= point.Y && orient2d(v.p, v.next.p, point) < 0) {
						--wn;
					}
				}
				v = v.next;
			} while (v != begin);

			return wn;
		}

		static bool doTriangulate(vertex_info begin, FList<TriIdx> result) {
			var vq = new EarQueue();

			var v = begin;
			int remain = 0;
			do {
				if (v.isCandidate()) vq.push(v);
				v = v.next;
				remain++;
			} while (v != begin);

			while (remain > 3 && vq.size() != 0) {
				var v2 = vq.pop();
				if (!v2.isClipable()) {
					v2.failed = true;
					continue;
				}

				continue_clipping:
				var n = v2.next;
				var p = v2.prev;

				result.Add(new TriIdx(v2.prev.idx, v2.idx, v2.next.idx));

				v2.remove();
				if (v2 == begin) begin = v2.next;

				if (--remain == 3) break;

				vq.updateVertex(n);
				vq.updateVertex(p);

				if (n.score < p.score) {
					var t = n;
					n = p;
					p = t;
				}

				if (n.score > 0.25 && n.isCandidate() && n.isClipable()) {
					vq.remove(n);
					v2 = n;
					goto continue_clipping;
				}

				if (p.score > 0.25 && p.isCandidate() && p.isClipable()) {
					vq.remove(p);
					v2 = p;
					goto continue_clipping;
				}

			}


			if (remain > 3) {
				remain -= removeDegeneracies(ref begin, result);

				if (remain > 3) {
					return splitAndResume(begin, result);
				}
			}

			if (remain == 3) {
				result.Add(new TriIdx(begin.idx, begin.next.idx, begin.next.next.idx));
			}

			var d = begin;
			do {
				var n = d.next;
				d = n;
			} while (d != begin);

			return true;
		}

		static int MinElementIndex<T>(FList<T> list, IComparer<T> comparer) {
			T min = list[0];
			int index = 0;
			for (int i = list.Count - 1; i != 0; i--) {
				var t = list[i];
				if (comparer.Compare(t, min) < 0) {
					min = t;
					index = i;
				}
			}
			return index;
		}

		static bool lineSegmentIntersection_simple(vector s1, vector e1, vector s2, vector e2) {
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

		static bool lineSegmentIntersection_simple(LineSegment2 seg1, LineSegment2 seg2) {
			return lineSegmentIntersection_simple(seg1.v1, seg1.v2, seg2.v1, seg2.v2);
		}

		static bool testCandidateAttachment<T>(Project<T> project, FList<T> current_f_loop, int curr, vector hole_min) {

			var SZ = current_f_loop.Count;

			int prev, next;

			if (curr == 0) {
				prev = SZ - 1; next = 1;
			} else if (curr == SZ - 1) {
				prev = curr - 1; next = 0;
			} else {
				prev = curr - 1; next = curr + 1;
			}

			if (!internalToAngle(project(current_f_loop[next]), project(current_f_loop[curr]), project(current_f_loop[prev]), hole_min)) {
				return false;
			}

			if (hole_min == project(current_f_loop[curr])) {
				return true;
			}

			var test = new LineSegment2(hole_min, project(current_f_loop[curr]));

			var v1 = current_f_loop.Count - 1;
			int v2 = 0;
			var v1_side = orient2d(test.v1, test.v2, project(current_f_loop[v1]));
			element v2_side = 0;

			while (v2 != current_f_loop.Count) {
				v2_side = orient2d(test.v1, test.v2, project(current_f_loop[v2]));

				if (v1_side != v2_side) {
					// XXX: need to test vertices, not indices, because they may
					// be duplicated.
					if (project(current_f_loop[v1]) != project(current_f_loop[curr]) &&

						project(current_f_loop[v2]) != project(current_f_loop[curr])) {
						var test2 = new LineSegment2(project(current_f_loop[v1]), project(current_f_loop[v2]));
						if (lineSegmentIntersection_simple(test, test2)) {
							// intersection; failed.
							return false;
						}
					}
				}

				v1 = v2;
				v1_side = v2_side;
				++v2;
			}
			return true;
		}

		/** 
		 * \brief Given a polygon loop and a hole loop, and attachment
		 * points, insert the hole loop vertices into the polygon loop.
		 * 
		 * @param[in,out] f_loop The polygon loop to incorporate the
		 *                       hole into.
		 * @param f_loop_attach[in] The index of the vertex of the
		 *                          polygon loop that the hole is to be
		 *                          attached to.
		 * @param hole_attach[in] A pair consisting of a pointer to a
		 *                        hole container and an iterator into
		 *                        that container reflecting the point of
		 *                        attachment of the hole.
		 */
		static void patchHoleIntoPolygon<T>(FList<T> f_loop, int f_loop_attach, Iterator<T> hole_attach) {
			// join the vertex curr of the polygon loop to the hole at
			// h_loop_connect
			var hole_temp = new T[hole_attach.list.Count + 2];
			for (int i = 0, n = hole_attach.list.Count; i <= n; i++) {
				hole_temp[i] = hole_attach.list[(hole_attach.index + i) % n];
			}
			hole_temp[hole_temp.Length - 1] = f_loop[f_loop_attach];
			f_loop.InsertRange(f_loop_attach + 1, hole_temp);
		}
		#endregion
	}
}
