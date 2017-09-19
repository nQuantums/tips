#pragma once
#ifndef __JUNK_GEO_H__
#define __JUNK_GEO_H__

#include "JunkConfig.h"
#include "Vector.h"

_JUNK_BEGIN

namespace Geo {

	//! pを開始点としvを方向ベクトルとする線分と点cとの最近点の線分パラメータを計算する
	//! @return 線分のパラメータ
	template<
		class PT, //!< VectorN 形式のクラス
		class VT, //!< VectorN 形式のクラス
		class CT //!< VectorN 形式のクラス
	> _FINLINE typename CT::ValueType LinePointNearestParam(
		const PT& p, //!< [in] 線分の開始点
		const VT& v, //!< [in] 線分の方向ベクトル
		const CT& c //!< [in] 最近点を調べる点c
	) {
		return (c - p).Dot(v) / v.LengthSquare();
	}

	//! ２次元線分同士の交点パラメータを計算する
	//! @return 計算できたら0以外が返る
	template<
		class P1T, //!< VectorN 形式のクラス
		class V1T, //!< VectorN 形式のクラス
		class P2T, //!< VectorN 形式のクラス
		class V2T, //!< VectorN 形式のクラス
		class CT //!< 戻りパラメータの型
	> _FINLINE ibool Line2IntersectParam(
		const P1T& p1, //!< [in] 線分１の開始点
		const V1T& v1, //!< [in] 線分１の方向ベクトル
		const P2T& p2, //!< [in] 線分２の開始点
		const V2T& v2, //!< [in] 線分２の方向ベクトル
		CT& t1, //!< [out] 線分１のパラメータが返る
		CT& t2 //!< [out] 線分２のパラメータが返る
	) {
		auto d = v1(0) * v2(1) - v1(1) * v2(0);
		if (d == decltype(d)(0))
			return false;
		auto pv = p2 - p1;
		t1 = (pv(0) * v2(1) - pv(1) * v2(0)) / d;
		t2 = (pv(0) * v1(1) - pv(1) * v1(0)) / d;
		return true;
	}

	//! ２次元線分同士の交点パラメータを計算する、その際に線分範囲外が判明し次第計算を打ち切る
	//! @return 交差するなら0以外が返る
	template<
		class P1T, //!< VectorN 形式のクラス
		class V1T, //!< VectorN 形式のクラス
		class P2T, //!< VectorN 形式のクラス
		class V2T, //!< VectorN 形式のクラス
		class CT //!< 戻りパラメータの型
	> _FINLINE ibool Line2IntersectParamCheckRange(
		const P1T& p1, //!< [in] 線分１の開始点
		const V1T& v1, //!< [in] 線分１の方向ベクトル
		const P2T& p2, //!< [in] 線分２の開始点
		const V2T& v2, //!< [in] 線分２の方向ベクトル
		CT tolerance, //!< [in] 線分パラメータ範囲内判定用許容誤差値、許容誤差内なら0～1の範囲を超えていても交差していることにする
		CT& t1, //!< [out] 線分１のパラメータが返る
		CT& t2 //!< [out] 線分２のパラメータが返る
	) {
		auto d = v1(0) * v2(1) - v1(1) * v2(0);
		if (d == decltype(d)(0))
			return false;
		auto pv = p2 - p1;
		t2 = (pv(0) * v1(1) - pv(1) * v1(0)) / d;
		if (t2 < -tolerance || tolerance < t2 - decltype(t2)(1))
			return false;
		t1 = (pv(0) * v2(1) - pv(1) * v2(0)) / d;
		return -tolerance <= t1 && t1 - decltype(t1)(1) <= tolerance;
	}

	// ２次元線分同士が交差しているか調べる、交点のパラメータは計算しない（整数ベクトル使用可）
	//! @return 交差しているなら0以外が返る
	template<
		class S1T, //!< VectorN 形式のクラス
		class E1T, //!< VectorN 形式のクラス
		class S2T, //!< VectorN 形式のクラス
		class E2T //!< VectorN 形式のクラス
	> _FINLINE ibool Line2Intersect(
		const S1T& s1, //!< [in] 線分１の開始点
		const E1T& e1, //!< [in] 線分１の終了点
		const S2T& s2, //!< [in] 線分２の開始点
		const E2T& e2 //!< [in] 線分２の終了点
	) {
		auto v = s1 - e1;
		auto ox = s2(1) - s1(1);
		auto oy = s1(0) - s2(0);
		if (decltype(v)(0) <= (v(0) * ox + v(1) * oy) * (v(0) * (e2(1) - s1(1)) + v(1) * (s1(0) - e2(0))))
			return false;
		v = s2 - e2;
		if (decltype(v)(0) <= -(v(0) * ox + v(1) * oy) * (v(0) * (e1(1) - s2(1)) + v(1) * (s2(0) - e1(0))))
			return false;
		return true;
	}

	//! ２次元線分同士の交差のラフチェックを行う（整数ベクトル使用可）
	//! @return 交差する可能性があるなら0以外が返る
	template<
		class S1T, //!< VectorN 形式のクラス
		class E1T, //!< VectorN 形式のクラス
		class S2T, //!< VectorN 形式のクラス
		class E2T //!< VectorN 形式のクラス
	> _FINLINE ibool Line2RoughIntersect(
		const S1T& s1, //!< [in] 線分１の開始点
		const E1T& e1, //!< [in] 線分１の終了点
		const S2T& s2, //!< [in] 線分２の開始点
		const E2T& e2 //!< [in] 線分２の終了点
	) {
		if (e1(0) <= s1(0)) {
			if ((s1(0) < s2(0) && s1(0) < e2(0)) || (e1(0) > s2(0) && e1(0) > e2(0)))
				return false;
		} else if ((e1(0) < s2(0) && e1(0) < e2(0)) || (s1(0) > s2(0) && s1(0) > e2(0))) {
			return false;
		}
		if (e1(1) <= s1(1)) {
			if ((s1(1) < s2(1) && s1(1) < e2(1)) || (e1(1) > s2(1) && e1(1) > e2(1)))
				return false;
		} else if ((e1(1) < s2(1) && e1(1) < e2(1)) || (s1(1) > s2(1) && s1(1) > e2(1))) {
			return false;
		}
		return true;
	}

	//! ０を開始点としv1を方向ベクトルとする線分とp2を開始としv2を方向ベクトルとする線分との最近点のパラメータ計算する
	//! @return 最近点が計算できたら0以外が返る
	template<
		class V1T, //!< VectorN 形式のクラス
		class P2T, //!< VectorN 形式のクラス
		class V2T, //!< VectorN 形式のクラス
		class CT //!< 戻りパラメータの型
	> ibool LineNearestParam(
		V1T v1, //!< [in] 線分１の方向ベクトル
		P2T p2, //!< [in] 線分２の開始点
		V2T v2, //!< [in] 線分２の方向ベクトル
		CT& t1, //!< [out] 線分１のパラメータが返る
		CT& t2 //!< [out] 線分２のパラメータが返る
	) {
		auto d1 = p2.Dot(v1);
		auto d2 = p2.Dot(v2);
		auto dv = v1.Dot(v2);
		auto v1sq = v1.LengthSquare();
		auto v2sq = v2.LengthSquare();
		auto den = v1sq * v2sq - dv * dv;
		if (den == decltype(den)(0)) {
			t1 = CT(0);
			t2 = CT(0);
			return false;
		}
		t1 = CT((d1 * v2sq - d2 * dv) / den);
		t2 = CT((d1 * dv - d2 * v1sq) / den);
		return true;
	}

	//! Pnを開始点としVnを方向ベクトルとする２線分との最近点のパラメータ計算する
	//! @return 最近点が計算できたら0以外が返る
	template<
		class P1T, //!< VectorN 形式のクラス
		class V1T, //!< VectorN 形式のクラス
		class P2T, //!< VectorN 形式のクラス
		class V2T, //!< VectorN 形式のクラス
		class CT //!< 戻りパラメータの型
	> ibool LineNearestParam(
		P1T p1, //!< [in] 線分１の開始点
		V1T v1, //!< [in] 線分１の方向ベクトル
		P2T p2, //!< [in] 線分２の開始点
		V2T v2, //!< [in] 線分２の方向ベクトル
		CT& t1, //!< [out] 線分１のパラメータが返る
		CT& t2 //!< [out] 線分２のパラメータが返る
	) {
		auto pv = p2 - p1;
		auto d1 = pv.Dot(v1);
		auto d2 = pv.Dot(v2);
		auto dv = v1.Dot(v2);
		auto v1sq = v1.LengthSquare();
		auto v2sq = v2.LengthSquare();
		auto den = v1sq * v2sq - dv * dv;
		if (den == decltype(den)(0)) {
			t1 = CT(0);
			t2 = CT(0);
			return false;
		}
		t1 = CT((d1 * v2sq - d2 * dv) / den);
		t2 = CT((d1 * dv - d2 * v1sq) / den);
		return true;
	}

	//! pを開始点としvを方向ベクトルとする太さを持つ線分に点Cが接触しているか調べる
	//! @return 接触しているなら0以外が返る
	template<
		class PT, //!< VectorN 形式のクラス
		class VT, //!< VectorN 形式のクラス
		class CT //!< VectorN 形式のクラス
	> ibool ThicknessLineTouch(
		const PT& p, //!< [in] 線分の開始点
		const VT& v, //!< [in] 線分の方向ベクトル
		const CT& c, //!< [in] 調べる点c
		typename PT::ValueType thic2, //!< [in] 線分の太さの二乗
		typename PT::ValueType* pt = 0, //! [out] 途中で計算される点cに最も近い線分上の点のパラメタを取得したい場合は有効なポインタを、それ以外はnullptrを指定する
		PT* pPos = 0, //!< [out] 途中で計算される点cに最も近い線分上の点を取得したい場合は有効なポインタを、それ以外はnullptrを指定する（パラメタから明らかに線分に接触していない場合は計算されません）
		typename PT::ValueType* pdist2 = 0 //!< [out] 途中で計算される点cに最も近い線分上の点との距離の二乗を取得したい場合有効なポインタを、それ以外はnullptrを指定する（パラメタから明らかに線分に接触していない場合は計算されません）
	) {
		auto t = LinePointNearestParam(p, v, c);
		if (pt) *pt = t;
		if (t < decltype(t)(0) || decltype(t)(1) < t)
			return false;
		auto pos = p + v * t;
		if (pPos) *pPos = pos;
		auto dist2 = (pos - C).LengthSquare();
		if (pdist2) *pdist2 = dist2;
		return dist2 <= thic2;
	}

	//! 点が２次元多角形の内にあるか調べる、辺と点上の座標は接触しているとみなされない
	//! @return 点が多角形内にあるなら0以外が返る
	template<
		class CT, //!< VectorN 形式のクラス
		class VT, //!< VectorN 形式のクラス
		class PRJ = ProjectVector2<Vector2<typename VT::ValueType> > //!< 入力頂点から２次元ベクトルのみを抽出するクラス
	> inline intptr_t PointInPolygon2(
		CT c, //!< [in] 点の座標
		const VT* pvts, //!< [in] 多角形の頂点列ポインタ
		intptr_t nvts, //!< [in] 多角形の頂点数、３以上でなければならない
		ibool close = false, //!< [in] 多角形の始点と終点を閉じて判定をする場合は０以外を指定する
		PRJ prj = ProjectVector2<Vector2<typename VT::ValueType> >() //!< [in] 入力頂点から２次元ベクトルのみを抽出するクラス
	) {
		intptr_t i, count = 0;
		intptr_t n = close ? nvts + 1 : nvts;
		auto p1 = prj(pvts[0]);
		auto x1 = c(0) - p1(0);
		decltype(x1) zero(0);
		for (i = 1; i < n; i++) {
			auto p2 = prj(pvts[close ? i % nvts : i]);
			auto x2 = c(0) - p2(0);
			if ((x1 < zero && zero <= x2) || (zero <= x1 && x2 < zero))
				count += x1 * (p2(1) - p1(1)) < (c(1) - p1(1)) * (p2(0) - p1(0)) ? -1 : 1;
			p1 = p2;
			x1 = x2;
		}
		return count;
	}

	//! 点が２次元多角形に接触しているか調べる、辺と点上の座標は接触しているとみなす
	//! @return 点が多角形内にあるなら2、辺または頂点上にあるなら1、それ以外なら0が返る
	template<
		class CT, //!< VectorN 形式のクラス
		class VT, //!< VectorN 形式のクラス
		class PRJ = ProjectVector2<Vector2<typename VT::ValueType> > //!< 入力頂点から２次元ベクトルのみを抽出するクラス
	> inline intptr_t PointTouchPolygon2(
		CT c, //!< [in] 点の座標
		const VT* pvts, //!< [in] 多角形の頂点列ポインタ
		intptr_t nvts, //!< [in] 多角形の頂点数、３以上でなければならない
		ibool close = false, //!< [in] 多角形の始点と終点を閉じて判定をする場合は０以外を指定する
		PRJ prj = ProjectVector2<Vector2<typename VT::ValueType> >() //!< [in] 入力頂点から２次元ベクトルのみを抽出するクラス
	) {
		intptr_t i, count = 0;
		intptr_t n = close ? nvts + 1 : nvts;
		auto p1 = prj(pvts[0]);
		auto y1 = c(1) - p1(1);
		decltype(y1) zero(0);
		for (i = 1; i < n; i++) {
			auto p2 = prj(pvts[close ? i % nvts : i]);
			auto y2 = c(1) - p2(1);
			if ((y1 < zero && zero <= y2) || (zero <= y1 && y2 < zero)) {
				auto rx = c(0) - p1(0);
				auto dy = p2(1) - p1(1);
				auto dx = p2(0) - p1(0);
				auto t1 = y1 * dx;
				auto t2 = rx * dy;
				if (t1 == t2)
					return 1;
				count += t1 < t2 ? -1 : 1;
			} else if (y1 == zero && y2 == zero) {
				auto x1 = c(0) - p1(0);
				auto x2 = c(0) - p2(0);
				if ((x1 <= zero) != (x2 <= zero) || x1 == zero || x2 == zero)
					return 1;
			} else if (c == p1) {
				return 1;
			}
			p1 = p2;
			y1 = y2;
		}
		return count != 0 ? 2 : 0;
	}

	//! 多角形の面積と回り方向を計算する
	//! @return 多角形の面積が返る、ポリゴンが反時計回りなら正数、時計回りなら負数となる
	template<
		class VT, //!< VectorN 形式のクラス
		class PRJ = ProjectVector2<Vector2<typename VT::ValueType> > //!< 入力頂点から２次元ベクトルのみを抽出するクラス
	> inline typename VT::ValueType Polygon2Area(
		const VT* pvts, //!< [in] 多角形の頂点列ポインタ
		intptr_t nvts, //!< [in] 多角形の頂点数、３以上でなければならない
		PRJ prj = ProjectVector2<Vector2<typename VT::ValueType> >() //!< [in] 入力頂点から２次元ベクトルのみを抽出するクラス
	) {
		typedef typename VT::ValueType val;
		intptr_t i;
		val s(0);
		auto p1 = prj(pvts[0]);
		for (i = 1; i <= nvts; i++) {
			auto p2 = prj(pvts[i % nvts]);
			s += (p1(0) - p2(0)) * (p1(1) + p2(1));
			p1 = p2;
		}
		return s / val(2);
	}

}

_JUNK_END

#endif
