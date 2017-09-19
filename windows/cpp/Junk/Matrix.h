#pragma once
#ifndef __JUNK_MATRIX_H__
#define __JUNK_MATRIX_H__

#include "Vector.h"
#include <algorithm>

_JUNK_BEGIN

#undef far
#undef near

#pragma pack(push,1)

//! D3D用のMatrixMxNの行と列のセレクタ
template<
	intptr_t R, //!< 行数
	intptr_t C //!< 列数
> struct RowColSel {
	enum {
		RightHand = 0 //!< 右手系かどうか、D3Dは左手系なので0とする
	};

	//! 行番号と列番号を指定して配列インデックス番号を取得する
	_FINLINE static intptr_t Index(intptr_t row, intptr_t col) {
		return row * C + col;
	}
	//! 平行移動要素のインデックス番号を取得する
	_FINLINE static intptr_t TranslateIndex(intptr_t index) {
		return (R - 1) * C + index;
	}
};

//! OpenGL用のMatrixMxNの行と列のセレクタ
template<
	intptr_t R, //!< 行数
	intptr_t C //!< 列数
> struct ColRowSel {
	enum {
		RightHand = 1 //!< 右手系かどうか、OpenGLは右手系なので1とする
	};

	//! 行番号と列番号を指定して配列インデックス番号を取得する
	_FINLINE static intptr_t Index(intptr_t row, intptr_t col) {
		return col * R + row;
	}
	//! 平行移動要素のインデックス番号を取得する
	_FINLINE static intptr_t TranslateIndex(intptr_t index) {
		return (C - 1) * R + index;
	}
};

//! 固定サイズMxN行列クラステンプレート
template<
	class T, //!< 要素型
	intptr_t R, //!< 行数
	intptr_t C, //!< 列数
	class SEL = RowColSel<R, C>, //!< 行と列から要素インデックスを取得するクラス
	class Math = DefaultMath<T> //!< ベクトル用算術関数群
> struct MatrixMxN {
	enum {
		ROW = R,
		COLUMN = C,
		LENGTH = R * C
	};

	typedef T ValueType;
	typedef SEL Selector;
	typedef MatrixMxN<T, R, C, SEL, Math> Self;

	T e[LENGTH];

	_FINLINE MatrixMxN() {}

	MatrixMxN(const T* p) {
		Order<TmSet, LENGTH>::BinaryAssign(e, p);
	}

	template<class S> MatrixMxN(const MatrixMxN<S, R, C, SEL, Math>& c) {
		Order<TmSet, LENGTH>::BinaryAssign(e, c.e);
	}

	template<class S> MatrixMxN& operator=(const MatrixMxN<S, R, C, SEL, Math>& c) {
		Order<TmSet, LENGTH>::BinaryAssign(e, c.e);
		return *this;
	}

	_FINLINE intptr_t Size() const {
		return LENGTH;
	}
	_FINLINE intptr_t RowCount() const {
		return ROW;
	}
	_FINLINE intptr_t ColumnCount() const {
		return COLUMN;
	}

	_FINLINE const T& operator()(intptr_t i) const {
		return e[i];
	}
	_FINLINE T& operator()(intptr_t i) {
		return e[i];
	}

	_FINLINE const T& operator()(intptr_t r, intptr_t c) const {
		return e[SEL::Index(r, c)];
	}
	_FINLINE T& operator()(intptr_t r, intptr_t c) {
		return e[SEL::Index(r, c)];
	}

	Self operator*(T s) const {
		Self m;
		Order<TmMul, LENGTH>::Binary(m.e, e, s);
		return m;
	}
	Self operator/(T s) const {
		Self m;
		Order<TmDiv, LENGTH>::Binary(m.e, e, s);
		return m;
	}
	Self& operator*=(T s) {
		Order<TmMul, LENGTH>::BinaryAssignScalar(e, s);
		return *this;
	}
	Self& operator/(T s) {
		Order<TmDiv, LENGTH>::BinaryAssignScalar(e, s);
		return *this;
	}

	Self operator+(const Self& m) const {
		Self nm;
		Order<TmAdd, LENGTH>::Binary(nm.e, e, m.e);
		return nm;
	}
	Self operator-(const Self& m) const {
		Self nm;
		Order<TmSub, LENGTH>::Binary(nm.e, e, m.e);
		return nm;
	}
	Self& operator+(const Self& m) {
		Order<TmAdd, LENGTH>::BinaryAssign(e, m.e);
		return *this;
	}
	Self& operator-(const Self& m) {
		Order<TmSub, LENGTH>::BinaryAssign(e, m.e);
		return *this;
	}

	template<intptr_t L> Self& Mul(const MatrixMxN<T, R, L, SEL, Math>& m1, const MatrixMxN<T, L, C, SEL, Math>& m2) {
		for (intptr_t i = 0; i < R; i++) {
			for (intptr_t j = 0; j < C; j++) {
				T t = m1(i, 0) * m2(0, j);
				for (intptr_t k = 1; k < L; k++)
					t += m1(i, k) * m2(k, j);
				(*this)(i, j) = t;
			}
		}
		return *this;
	}

	template<intptr_t L> _FINLINE Self& MulL(const MatrixMxN<T, R, L, SEL, Math>& m) {
		Self t;
		t.Mul(m, *this);
		*this = t;
		return *this;
	}

	template<intptr_t L> _FINLINE Self& MulR(const MatrixMxN<T, R, L, SEL, Math>& m) {
		Self t;
		t.Mul(*this, m);
		*this = t;
		return *this;
	}

	template<intptr_t L> _FINLINE Self& Mul(const MatrixMxN<T, R, L, SEL, Math>& m) {
		Self t;
		return *this = SEL::RightHand ? t.Mul(m, *this) : t.Mul(*this, m);
	}

	//! LU分解、行列の内容は改変される、正方行列でない場合は実行できない
	//! @return true=成功、false=失敗
	intptr_t Lu(
		int* ip, //!< [out] 行交換の情報が返る
		T& rdet, //!< [out] 行列式が返る
		T eps //! [in] 計算機イプシロン(絶対値がこの値以下の場合はゼロと判定される)
	) {
		if (ROW == COLUMN) {
			enum { N = ROW };
			int i, j, k, ii, ik;
			T t, u, det;
			T weight[N];

			for (k = 0; k < N; k++) {  // 各行について
				ip[k] = k;             // 行交換情報の初期値
				u = T(0);              // その行の絶対値最大の要素を求める
				for (j = 0; j < N; j++) {
					t = Math::Abs((*this)(k, j));
					if (t > u) u = t;
				}
				if (Math::Abs(u) <= eps) return false; // 0 なら行列はLU分解できない
				weight[k] = T(1) / u;   // 最大絶対値の逆数
			}
			det = T(1);                   // 行列式の初期値
			for (k = 0; k < N; k++) {  // 各行について
				u = T(-1);
				for (i = k; i < N; i++) {  // より下の各行について
					ii = ip[i];            // 重み×絶対値 が最大の行を見つける
					t = Math::Abs((*this)(ii, k)) * weight[ii];
					if (t > u) {
						u = t;
						j = i;
					}
				}
				ik = ip[j];
				if (j != k) {
					ip[j] = ip[k];
					ip[k] = ik;  // 行番号を交換
					det = -det;  // 行を交換すれば行列式の符号が変わる
				}
				u = (*this)(ik, k);
				det *= u;  // 対角成分
				if (Math::Abs(u) <= eps) return false;    // 0 なら行列はLU分解できない
				for (i = k + 1; i < N; i++) {  // Gauss消去法
					ii = ip[i];
					t = ((*this)(ii, k) /= u);
					for (j = k + 1; j < N; j++)
						(*this)(ii, j) -= t * (*this)(ik, j);
				}
			}
			rdet = det;
			return true;
		} else {
			assert("Not Implemented!" && 0);
			return false;
		}
	}

	//! ax=b を解く、a には (*this) が入る
	//! @return true=成功、false=失敗
	template<class V>
	intptr_t Solve(
		const V& b, //!< [in] ベクトル b
		const int *ip, //!< [in] Lu() で取得した行交換情報
		V& x, //!< [out] x の値が返る
		T eps //! [in] 計算機イプシロン(絶対値がこの値以下の場合はゼロと判定される)
	) const {
		if (ROW == COLUMN && ROW <= b.N) {
			enum { N = ROW };
			int i, j, ii;
			T t;

			for (i = 0; i < N; i++) {       // Gauss消去法の残り
				ii = ip[i];
				t = b(ii);
				for (j = 0; j < i; j++)
					t -= (*this)(ii, j) * x(j);
				x(i) = t;
			}
			for (i = N - 1; i >= 0; i--) {  // 後退代入
				t = x(i);
				ii = ip[i];
				for (j = i + 1; j < N; j++)
					t -= (*this)(ii, j) * x(j);
				T buf = (*this)(ii, i);
				if (Math::Abs(buf) <= eps)
					return false;
				x(i) = t / buf;
			}

			return true;
		} else {
			assert("Not Implemented!" && 0);
			return false;
		}
	}

	//! LU分解による逆行列計算、正方行列でない場合は実行できない
	//! @return true=成功、false=失敗
	intptr_t InverseLu(
		const Self& m, //!< [in] 入力行列
		T& rdet, //!< [out] 行列式が返る
		T eps //! [in] 計算機イプシロン(絶対値がこの値以下の場合はゼロと判定される)
	) {
		if (ROW == COLUMN) {
			enum { N = ROW };
			int i, j, k, ii;
			T t;
			int ip[N];   // 行交換の情報
			Self m2 = m;

			if (!m2.Lu(ip, rdet, eps) || Math::Abs(rdet) <= eps)
				return false;

			for (k = 0; k < N; k++) {
				for (i = 0; i < N; i++) {
					ii = ip[i];
					t = (ii == k);
					for (j = 0; j < i; j++)
						t -= m2(ii, j) * (*this)(j, k);
					(*this)(i, k) = t;
				}
				for (i = N - 1; i >= 0; i--) {
					t = (*this)(i, k);
					ii = ip[i];
					for (j = i + 1; j < N; j++)
						t -= m2(ii, j) * (*this)(j, k);
					(*this)(i, k) = t / m2(ii, i);
				}
			}

			return true;
		} else {
			assert("Not Implemented!" && 0);
			return 0;
		}
	}
};

//! 固定サイズ2x2行列クラステンプレート
template<
	class T, //!< 要素型
	class SEL = RowColSel<2, 2>, //!< 行と列から要素インデックスを取得するクラス
	class Math = DefaultMath<T> //!< ベクトル用算術関数群
> struct Matrix2x2 : public MatrixMxN<T, 2, 2, SEL, Math> {
	typedef T ValueType;
	typedef SEL Selector;
	typedef MatrixMxN<T, 2, 2, SEL, Math> Base;
	typedef Matrix2x2<T, SEL, Math> Self;
	enum {
		ROW = Base::ROW,
		COLUMN = Base::COLUMN,
		LENGTH = Base::LENGTH
	};

	_FINLINE Matrix2x2() {}
	Matrix2x2(const T* p) : Base(p) {}
	template<class S> Matrix2x2(const MatrixMxN<S, ROW, COLUMN, SEL, Math>& c) : Base(c) {}

	template<class S> Self& operator=(const MatrixMxN<S, ROW, COLUMN, SEL, Math>& c) {
		Base::operator=(c);
		return *this;
	}
};

//! 固定サイズ3x3行列クラステンプレート
template<
	class T, //!< 要素型
	class SEL = RowColSel<3, 3>, //!< 行と列から要素インデックスを取得するクラス
	class Math = DefaultMath<T> //!< ベクトル用算術関数群
> struct Matrix3x3 : public MatrixMxN<T, 3, 3, SEL, Math> {
	typedef T ValueType;
	typedef SEL Selector;
	typedef MatrixMxN<T, 3, 3, SEL, Math> Base;
	typedef Matrix3x3<T, SEL, Math> Self;
	enum {
		ROW = Base::ROW,
		COLUMN = Base::COLUMN,
		LENGTH = Base::LENGTH
	};

	Matrix3x3() {}
	Matrix3x3(const T* p) : Base(p) {}
	template<class S> Matrix3x3(const MatrixMxN<S, ROW, COLUMN, SEL, Math>& c) : Base(c) {}

	template<class S> Matrix3x3& operator=(const MatrixMxN<S, ROW, COLUMN, SEL, Math>& c) {
		Base::operator=(c);
		return *this;
	}
};


//! 固定サイズ4x4行列クラステンプレート
template<
	class T, //!< 要素型
	class SEL = RowColSel<4, 4>, //!< 行と列から要素インデックスを取得するクラス
	class Math = DefaultMath<T> //!< ベクトル用算術関数群
> struct Matrix4x4 : public MatrixMxN<T, 4, 4, SEL, Math> {
	typedef T ValueType;
	typedef SEL Selector;
	typedef typename Math::AngleType AngleType;
	typedef MatrixMxN<T, 4, 4, SEL, Math> Base;
	typedef Matrix4x4<T, SEL, Math> Self;
	enum {
		ROW = Base::ROW,
		COLUMN = Base::COLUMN,
		LENGTH = Base::LENGTH,
		N = 4,
	};

	_FINLINE Matrix4x4() {}
	Matrix4x4(const T* p) : Base(p) {}
	template<class S> Matrix4x4(const MatrixMxN<S, ROW, COLUMN, SEL, Math>& c) : Base(c) {}

	template<class S> Matrix4x4& operator=(const MatrixMxN<S, ROW, COLUMN, SEL, Math>& c) {
		Base::operator=(c);
		return *this;
	}

	_FINLINE T& TranslateElement(intptr_t index) {
		return this->e[SEL::TranslateIndex(index)];
	}

	_FINLINE const T& TranslateElement(intptr_t index) const {
		return this->e[SEL::TranslateIndex(index)];
	}

	_FINLINE Vector4<T, Math> GetRowVector(intptr_t row) const {
		return Vector4<T, Math>((*this)(row, 0), (*this)(row, 1), (*this)(row, 2), (*this)(row, 3));
	}

	_FINLINE Vector4<T, Math> GetColVector(intptr_t col) const {
		return Vector4<T, Math>((*this)(0, col), (*this)(1, col), (*this)(2, col), (*this)(3, col));
	}

	_FINLINE Vector3<T, Math> GetRowVector3(intptr_t row) const {
		return Vector3<T, Math>((*this)(row, 0), (*this)(row, 1), (*this)(row, 2));
	}

	_FINLINE Vector3<T, Math> GetColVector3(intptr_t col) const {
		return Vector3<T, Math>((*this)(0, col), (*this)(1, col), (*this)(2, col));
	}

	template<class MF> _FINLINE void SetRowVector(intptr_t row, VectorN<T, COLUMN, MF> v) {
		(*this)(row, 0) = v(0);
		(*this)(row, 1) = v(1);
		(*this)(row, 2) = v(2);
		(*this)(row, 3) = v(3);
	}

	template<class MF> _FINLINE void SetRowVector(intptr_t row, VectorN<T, 3, MF> v, T t) {
		(*this)(row, 0) = v(0);
		(*this)(row, 1) = v(1);
		(*this)(row, 2) = v(2);
		(*this)(row, 3) = t;
	}

	_FINLINE void SetRowVector(intptr_t row, T x, T y, T z, T w) {
		(*this)(row, 0) = x;
		(*this)(row, 1) = y;
		(*this)(row, 2) = z;
		(*this)(row, 3) = w;
	}

	_FINLINE void SetRowVector(intptr_t row, T x, T y, T z) {
		(*this)(row, 0) = x;
		(*this)(row, 1) = y;
		(*this)(row, 2) = z;
	}

	template<class MF> _FINLINE void SetColVector(intptr_t col, VectorN<T, ROW, MF> v) {
		(*this)(0, col) = v(0);
		(*this)(1, col) = v(1);
		(*this)(2, col) = v(2);
		(*this)(3, col) = v(3);
	}

	template<class MF> _FINLINE void SetColVector(intptr_t col, VectorN<T, 3, MF> v, T t) {
		(*this)(0, col) = v(0);
		(*this)(1, col) = v(1);
		(*this)(2, col) = v(2);
		(*this)(3, col) = t;
	}

	_FINLINE void SetColVector(intptr_t col, T x, T y, T z, T w) {
		(*this)(0, col) = x;
		(*this)(1, col) = y;
		(*this)(2, col) = z;
		(*this)(3, col) = w;
	}

	_FINLINE void SetColVector(intptr_t col, T x, T y, T z) {
		(*this)(0, col) = x;
		(*this)(1, col) = y;
		(*this)(2, col) = z;
	}

	_FINLINE void Swap(intptr_t r1, intptr_t c1, intptr_t r2, intptr_t c2) {
		T& t1 = (*this)(r1, c1);
		T& t2 = (*this)(r2, c2);
		T t = t1;
		t1 = t2;
		t2 = t;
	}

#define _JMATMUL(r, c) (*this)(r, c) = m1(r, 0) * m2(0, c) + m1(r, 1) * m2(1, c) + m1(r, 2) * m2(2, c) + m1(r, 3) * m2(3, c)
	Self& Mul(const MatrixMxN<T, ROW, COLUMN, SEL, Math>& m1, const MatrixMxN<T, ROW, COLUMN, SEL, Math>& m2) {
		_JMATMUL(0, 0);
		_JMATMUL(1, 0);
		_JMATMUL(2, 0);
		_JMATMUL(3, 0);
		_JMATMUL(0, 1);
		_JMATMUL(1, 1);
		_JMATMUL(2, 1);
		_JMATMUL(3, 1);
		_JMATMUL(0, 2);
		_JMATMUL(1, 2);
		_JMATMUL(2, 2);
		_JMATMUL(3, 2);
		_JMATMUL(0, 3);
		_JMATMUL(1, 3);
		_JMATMUL(2, 3);
		_JMATMUL(3, 3);
		return *this;
	}
#undef _JMATMUL

	_FINLINE Self& MulL(const MatrixMxN<T, ROW, COLUMN, SEL, Math>& m) {
		Self t;
		t.Mul(m, *this);
		*this = t;
		return *this;
	}

	_FINLINE Self& MulR(const MatrixMxN<T, ROW, COLUMN, SEL, Math>& m) {
		Self t;
		t.Mul(*this, m);
		*this = t;
		return *this;
	}

	_FINLINE Self& Mul(const MatrixMxN<T, ROW, COLUMN, SEL, Math>& m) {
		Self t;
		return *this = SEL::RightHand ? t.Mul(m, *this) : t.Mul(*this, m);
	}

	Self& Identity() {
		SetRowVector(0, T(1), T(0), T(0), T(0));
		SetRowVector(1, T(0), T(1), T(0), T(0));
		SetRowVector(2, T(0), T(0), T(1), T(0));
		SetRowVector(3, T(0), T(0), T(0), T(1));
		return *this;
	}

	Self& Scale(T x, T y, T z) {
		SetRowVector(0, x, T(0), T(0), T(0));
		SetRowVector(1, T(0), y, T(0), T(0));
		SetRowVector(2, T(0), T(0), z, T(0));
		SetRowVector(3, T(0), T(0), T(0), T(1));
		return *this;
	}

	Self& Translate(T x, T y, T z) {
		SetRowVector(0, T(1), T(0), T(0), T(0));
		SetRowVector(1, T(0), T(1), T(0), T(0));
		SetRowVector(2, T(0), T(0), T(1), T(0));
		SetRowVector(3, T(0), T(0), T(0), T(1));
		TranslateElement(0) = x;
		TranslateElement(1) = y;
		TranslateElement(2) = z;
		return *this;
	}

	Self& RotateX(T s, T c) {
		SetRowVector(0, T(1), T(0), T(0), T(0));
		SetRowVector(3, T(0), T(0), T(0), T(1));
		if (SEL::RightHand) {
			SetRowVector(1, T(0), c, -s, T(0));
			SetRowVector(2, T(0), s, c, T(0));
		} else {
			SetRowVector(1, T(0), c, s, T(0));
			SetRowVector(2, T(0), -s, c, T(0));
		}
		return *this;
	}

	Self& RotateXRad(AngleType rad) {
		return RotateX(T(Math::SinRad(rad)), T(Math::CosRad(rad)));
	}

	Self& RotateXDeg(AngleType deg) {
		return RotateX(T(Math::SinDeg(deg)), T(Math::CosDeg(deg)));
	}

	Self& RotateY(T s, T c) {
		SetRowVector(1, T(0), T(1), T(0), T(0));
		SetRowVector(3, T(0), T(0), T(0), T(1));
		if (SEL::RightHand) {
			SetRowVector(0, c, T(0), s, T(0));
			SetRowVector(2, -s, T(0), c, T(0));
		} else {
			SetRowVector(0, c, T(0), -s, T(0));
			SetRowVector(2, s, T(0), c, T(0));
		}
		return *this;
	}

	Self& RotateYRad(AngleType rad) {
		return RotateY(T(Math::SinRad(rad)), T(Math::CosRad(rad)));
	}

	Self& RotateYDeg(AngleType deg) {
		return RotateY(T(Math::SinDeg(deg)), T(Math::CosDeg(deg)));
	}

	Self& RotateZ(T s, T c) {
		SetRowVector(2, T(0), T(0), T(1), T(0));
		SetRowVector(3, T(0), T(0), T(0), T(1));
		if (SEL::RightHand) {
			SetRowVector(0, c, -s, T(0), T(0));
			SetRowVector(1, s, c, T(0), T(0));
		} else {
			SetRowVector(0, c, s, T(0), T(0));
			SetRowVector(1, -s, c, T(0), T(0));
		}
		return *this;
	}

	Self& RotateZRad(AngleType rad) {
		return RotateZ(T(Math::SinRad(rad)), T(Math::CosRad(rad)));
	}

	Self& RotateZDeg(AngleType deg) {
		return RotateZ(T(Math::SinDeg(deg)), T(Math::CosDeg(deg)));
	}

	Self& RotateAxis(T x, T y, T z, T s, T c) {
		T ic = T(1) - c;
		T icx = ic*x;
		T icy = ic*y;
		T icz = ic*z;
		T icxy = icx*y;
		T icxz = icx*z;
		T icyz = icy*z;
		if (SEL::RightHand) {
			SetColVector(0, icx*x + c, s*z + icxy, icxz - s*y, T(0));
			SetColVector(1, icxy - s*z, icy*y + c, icyz + s*x, T(0));
			SetColVector(2, icxz + s*y, icyz - s*x, icz*z + c, T(0));
			SetColVector(3, T(0), T(0), T(0), T(T(1)));
		} else {
			SetRowVector(0, icx*x + c, s*z + icxy, icxz - s*y, T(0));
			SetRowVector(1, icxy - s*z, icy*y + c, icyz + s*x, T(0));
			SetRowVector(2, icxz + s*y, icyz - s*x, icz*z + c, T(0));
			SetRowVector(3, T(0), T(0), T(0), T(T(1)));
		}
		return *this;
	}

	Self& RotateAxisRad(T x, T y, T z, AngleType rad) {
		return RotateAxis(x, y, z, T(Math::SinRad(rad)), T(Math::CosRad(rad)));
	}

	Self& RotateAxisDeg(T x, T y, T z, AngleType deg) {
		return RotateAxis(x, y, z, T(Math::SinDeg(deg)), T(Math::CosDeg(deg)));
	}

	Self& Transpose() {
		Swap(0, 1, 1, 0);
		Swap(0, 2, 2, 0);
		Swap(0, 3, 3, 0);
		Swap(1, 2, 2, 1);
		Swap(1, 3, 3, 1);
		Swap(2, 3, 3, 2);
		return *this;
	}

	Self& ProjectionGL(T left, T right, T bottom, T top, T near, T far) { // OpenGL は変換後には奥行き方向が+になる
		T width = right - left;
		T height = top - bottom;
		T depth = near - far;
		SetRowVector(0, T(2) * near / width, T(0), (right + left) / width, T(0));
		SetRowVector(1, T(0), T(2) * near / height, (top + bottom) / height, T(0));
		SetRowVector(2, T(0), T(0), (near + far) / depth, T(2) * far * near / depth); // z を -1 ～ 1 に正規化する、OpenGLだと変換後は奥が+
		SetRowVector(3, T(0), T(0), T(-1), T(0));
		return *this;
	}

	Self& ProjectionGL(T width, T height, T near, T far) { // OpenGL は変換後には奥行き方向が+になる
		T depth = near - far;
		SetRowVector(0, T(2) * near / width, T(0), T(0), T(0));
		SetRowVector(1, T(0), T(2) * near / height, T(0) / height, T(0));
		SetRowVector(2, T(0), T(0), (near + far) / depth, T(2) * far * near / depth); // z を -1 ～ 1 に正規化する、OpenGLだと変換後は奥が+
		SetRowVector(3, T(0), T(0), T(-1), T(0));
		return *this;
	}

	template<class MF> Self& View(VectorN<T, 3, MF> from, VectorN<T, 3, MF> at, VectorN<T, 3, MF> up) {
		VectorN<T, 3, MF> z = SEL::RightHand ? from - at : at - from;
		VectorN<T, 3, MF> x = up.Cross(z);
		VectorN<T, 3, MF> y = z.Cross(x);
		x.NormalizeSelf();
		y.NormalizeSelf();
		z.NormalizeSelf();
		if (SEL::RightHand) {
			SetRowVector(0, x(0), x(1), x(2), -x.Dot(from));
			SetRowVector(1, y(0), y(1), y(2), -y.Dot(from));
			SetRowVector(2, z(0), z(1), z(2), -z.Dot(from));
			SetRowVector(3, T(0), T(0), T(0), T(1));
		} else {
			SetColVector(0, x(0), x(1), x(2), -x.Dot(from));
			SetColVector(1, y(0), y(1), y(2), -y.Dot(from));
			SetColVector(2, z(0), z(1), z(2), -z.Dot(from));
			SetColVector(3, T(0), T(0), T(0), T(1));
		}
		return *this;
	}

	//T Lu(int* ip) { // LU分解
	//	int i, j, k, ii, ik;
	//	T t, u, det;
	//	T weight[N];

	//	for (k = 0; k < N; k++) {  /* 各行について */
	//		ip[k] = k;             /* 行交換情報の初期値 */
	//		u = T(0);              /* その行の絶対値最大の要素を求める */
	//		for (j = 0; j < N; j++) {
	//			t = Math::Abs((*this)(k, j));
	//			if (t > u) u = t;
	//		}
	//		if (u == T(0)) return T(0); /* 0 なら行列はLU分解できない */
	//		weight[k] = T(1) / u;   /* 最大絶対値の逆数 */
	//	}
	//	det = T(1);                   /* 行列式の初期値 */
	//	for (k = 0; k < N; k++) {  /* 各行について */
	//		u = T(-1);
	//		for (i = k; i < N; i++) {  /* より下の各行について */
	//			ii = ip[i];            /* 重み×絶対値 が最大の行を見つける */
	//			t = Math::Abs((*this)(ii, k)) * weight[ii];
	//			if (t > u) {
	//				u = t;
	//				j = i;
	//			}
	//		}
	//		ik = ip[j];
	//		if (j != k) {
	//			ip[j] = ip[k];
	//			ip[k] = ik;  /* 行番号を交換 */
	//			det = -det;  /* 行を交換すれば行列式の符号が変わる */
	//		}
	//		u = (*this)(ik, k);
	//		det *= u;  /* 対角成分 */
	//		if (u == T(0)) return T(0);    /* 0 なら行列はLU分解できない */
	//		for (i = k + 1; i < N; i++) {  /* Gauss消去法 */
	//			ii = ip[i];
	//			t = ((*this)(ii, k) /= u);
	//			for (j = k + 1; j < N; j++)
	//				(*this)(ii, j) -= t * (*this)(ik, j);
	//		}
	//	}
	//	return det;           /* 戻り値は行列式 */
	//}

	//T Inverse(const Self& m) { // 逆行列を計算、行列式が返る(逆行列計算できない場合は0になる)
	//	int i, j, k, ii;
	//	T t, det;
	//	int ip[N];   /* 行交換の情報 */
	//	Self m2 = m;

	//	det = m2.Lu(ip);
	//	if (det != 0)
	//		for (k = 0; k < N; k++) {
	//			for (i = 0; i < N; i++) {
	//				ii = ip[i];
	//				t = (ii == k);
	//				for (j = 0; j < i; j++)
	//					t -= m2(ii, j) * (*this)(j, k);
	//				(*this)(i, k) = t;
	//			}
	//			for (i = N - 1; i >= 0; i--) {
	//				t = (*this)(i, k);
	//				ii = ip[i];
	//				for (j = i + 1; j < N; j++)
	//					t -= m2(ii, j) * (*this)(j, k);
	//				(*this)(i, k) = t / m2(ii, i);
	//			}
	//		}
	//	return det;
	//}

	//int InverseGaussJordan() { // Gauss-Jordan法で逆行列にする、成功したら1が返る、失敗したら0が返り行列内容は壊れる
	//	intptr_t i, j, k;
	//	T t, u;
	//	for (k = 0; k < ROW; k++) {
	//		t = (*this)(k, k);
	//		if(t == T(0))
	//			return 0;
	//		for (i = 0; i < ROW; i++) (*this)(i, k) /= t;
	//		(*this)(k, k) = 1 / t;
	//		for (j = 0; j < COLUMN; j++)
	//			if (j != k) {
	//				u = (*this)(k, j);
	//				for (i = 0; i < ROW; i++)
	//					if (i != k) (*this)(i, j) -= (*this)(i, k) * u;
	//					else        (*this)(i, j) = -u / t;
	//			}
	//	}
	//	return 1;
	//}

	Self& InverseQuasi(const Self& m) { // 回転と平行移動だけを行う直行行列の逆行列を計算する
		SetRowVector(0, m(0, 0), m(1, 0), m(2, 0));
		SetRowVector(1, m(0, 1), m(1, 1), m(2, 1));
		SetRowVector(2, m(0, 2), m(1, 2), m(2, 2));
		(*this)(3, 3) = T(1);
		if (SEL::RightHand) {
			VectorN<T, 3, Math> t = m.GetColVector3(3);
			(*this)(0, 3) = -m(0, 0) * t(0) - m(1, 0) * t(1) - m(2, 0) * t(2);
			(*this)(1, 3) = -m(0, 1) * t(0) - m(1, 1) * t(1) - m(2, 1) * t(2);
			(*this)(2, 3) = -m(0, 2) * t(0) - m(1, 2) * t(1) - m(2, 2) * t(2);
			(*this)(3, 0) = T(0);
			(*this)(3, 1) = T(0);
			(*this)(3, 2) = T(0);
		} else {
			VectorN<T, 3, Math> t = m.GetRowVector3(3);
			(*this)(3, 0) = -m(0, 0) * t(0) - m(0, 1) * t(1) - m(0, 2) * t(2);
			(*this)(3, 1) = -m(1, 0) * t(0) - m(1, 1) * t(1) - m(1, 2) * t(2);
			(*this)(3, 2) = -m(2, 0) * t(0) - m(2, 1) * t(1) - m(2, 2) * t(2);
			(*this)(0, 3) = T(0);
			(*this)(1, 3) = T(0);
			(*this)(2, 3) = T(0);
		}
		return *this;
	}

	static _FINLINE Self NewIdentity() {
		Self m;
		m.Identity();
		return m;
	}

	static _FINLINE Self NewScale(T x, T y, T z) {
		Self m;
		m.Scale(x, y, z);
		return m;
	}

	static _FINLINE Self NewTranslate(T x, T y, T z) {
		Self m;
		m.Translate(x, y, z);
		return m;
	}

	static _FINLINE Self NewRotateXRad(AngleType rad) {
		Self m;
		m.RotateXRad(rad);
		return m;
	}

	static _FINLINE Self NewRotateYRad(AngleType rad) {
		Self m;
		m.RotateYRad(rad);
		return m;
	}

	static _FINLINE Self NewRotateZRad(AngleType rad) {
		Self m;
		m.RotateZRad(rad);
		return m;
	}

	static _FINLINE Self NewRotateAxisRad(T x, T y, T z, AngleType rad) {
		Self m;
		m.RotateAxisRad(x, y, z, rad);
		return m;
	}

	static _FINLINE Self NewRotateXDeg(AngleType deg) {
		Self m;
		m.RotateXDeg(deg);
		return m;
	}

	static _FINLINE Self NewRotateYDeg(AngleType deg) {
		Self m;
		m.RotateYDeg(deg);
		return m;
	}

	static _FINLINE Self NewRotateZDeg(AngleType deg) {
		Self m;
		m.RotateZDeg(deg);
		return m;
	}

	static _FINLINE Self NewRotateAxisDeg(T x, T y, T z, AngleType deg) {
		Self m;
		m.RotateAxisDeg(x, y, z, deg);
		return m;
	}

	Self NewTranspose() {
		Self m;
		m.SetRowVector(0, this->GetColVector(0));
		m.SetRowVector(1, this->GetColVector(1));
		m.SetRowVector(2, this->GetColVector(2));
		m.SetRowVector(3, this->GetColVector(3));
		return m;
	}

	static _FINLINE Self NewProjectionGL(T left, T right, T bottom, T top, T near, T far) {
		Self m;
		m.ProjectionGL(left, right, bottom, top, near, far);
		return m;
	}

	static _FINLINE Self NewProjectionGL(T width, T height, T near, T far) {
		Self m;
		m.ProjectionGL(width, height, near, far);
		return m;
	}

	Self NewInverseQuasi() const { // 回転と平行移動だけを行う直行行列の逆行列を計算する
		Self m;
		m.InverseQuasi(*this);
		return m;
	}
};

#pragma pack(pop)

_JUNK_END

#endif
