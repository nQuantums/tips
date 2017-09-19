#pragma once
#ifndef __JUNK_TEMPLATEMETA_H__
#define __JUNK_TEMPLATEMETA_H__

#include "JunkDef.h"

_JUNK_BEGIN

// 昔のVCだと static なメソッドはインライン展開してくれないことがあったので強制インライン展開を指定

//! 何もしないオペレータ
struct TmNone {
};

//! 値代入オペレータ
struct TmSet {
	template<class CT, class T1> static _FINLINE void Op(CT& v, const T1& v1) {
		v = v1;
	}
};

//! 値キャストして代入オペレータ
struct TmCastAndSet {
	template<class CT, class T1> static _FINLINE void Op(CT& v, const T1& v1) {
		v = CT(v1);
	}
};

//! 四捨五入して代入オペレータ
struct TmNint {
	template<class CT, class T1> static _FINLINE void Op(CT& v, const T1& v1) {
		v = Nint<CT>(v1);
	}
};

//! +符号オペレータ
struct TmPlus {
	template<class CT, class T1> static _FINLINE void Op(CT& v) {
	}
	template<class CT, class T1> static _FINLINE void Op(CT& v, const T1& v1) {
		v = v1;
	}
};

//! -符号オペレータ
struct TmMinus {
	template<class CT, class T1> static _FINLINE void Op(CT& v) {
		v = -v;
	}
	template<class CT, class T1> static _FINLINE void Op(CT& v, const T1& v1) {
		v = -v1;
	}
};

//! 加算オペレータ
struct TmAdd {
	template<class CT, class T1> static _FINLINE void Start(CT& v, const T1& v1) {
		v = v1;
	}
	template<class CT, class T1> static _FINLINE void Op(CT& v, const T1& v1) {
		v += v1;
	}
	template<class CT, class T1, class T2> static _FINLINE void Op(CT& v, const T1& v1, const T2& v2) {
		v = v1 + v2;
	}
};

//! 減算オペレータ
struct TmSub {
	template<class CT, class T1> static _FINLINE void Start(CT& v, const T1& v1) {
		v = v1;
	}
	template<class CT, class T1> static _FINLINE void Op(CT& v, const T1& v1) {
		v -= v1;
	}
	template<class CT, class T1, class T2> static _FINLINE void Op(CT& v, const T1& v1, const T2& v2) {
		v = v1 - v2;
	}
};

//! 乗算オペレータ
struct TmMul {
	template<class CT, class T1> static _FINLINE void Start(CT& v, const T1& v1) {
		v = v1;
	}
	template<class CT, class T1> static _FINLINE void Op(CT& v, const T1& v1) {
		v *= v1;
	}
	template<class CT, class T1, class T2> static _FINLINE void Op(CT& v, const T1& v1, const T2& v2) {
		v = v1 * v2;
	}
};

//! 除算オペレータオペレータ
struct TmDiv {
	template<class CT, class T1> static _FINLINE void Op(CT& v, const T1& v1) {
		v /= v1;
	}
	template<class CT, class T1, class T2> static _FINLINE void Op(CT& v, const T1& v1, const T2& v2) {
		v = v1 / v2;
	}
};

//! 二乗後加算オペレータ
struct TmSquareAdd {
	template<class CT, class T1> static _FINLINE void Start(CT& v, const T1& v1) {
		v = v1 * v1;
	}
	template<class CT, class T1> static _FINLINE void Op(CT& v, const T1& v1) {
		v += v1 * v1;
	}
};

//! 乗算後加算オペレータ
struct TmMulAdd {
	template<class CT, class T1> static _FINLINE void Start(CT& v, const T1& v1) {
		v = v1;
	}
	template<class CT, class T1> static _FINLINE void Op(CT& v, const T1& v1) {
		v += v * v1;
	}
	template<class CT, class T1, class T2> static _FINLINE void Op(CT& v, const T1& v1, const T2& v2) {
		v += v1 * v2;
	}
};

//! 乗算後減算オペレータ
struct TmMulSub {
	template<class CT, class T1> static _FINLINE void Start(CT& v, const T1& v1) {
		v = v1;
	}
	template<class CT, class T1> static _FINLINE void Op(CT& v, const T1& v1) {
		v -= v * v1;
	}
	template<class CT, class T1, class T2> static _FINLINE void Op(CT& v, const T1& v1, const T2& v2) {
		v -= v1 * v2;
	}
};

//! 最大値選択
struct TmMax {
	template<class CT, class T1> static _FINLINE void Start(CT& v, const T1& v1) {
		v = v1;
	}
	template<class CT, class T1> static _FINLINE void Op(CT& v, const T1& v1) {
		if (v < v1)
			v = v1;
	}
	template<class CT, class T1, class T2> static _FINLINE void Op(CT& v, const T1& v1, const T2& v2) {
		v = v1 < v2 ? v2 : v1;
	}
};

//! 最小値選択
struct TmMin {
	template<class CT, class T1> static _FINLINE void Start(CT& v, const T1& v1) {
		v = v1;
	}
	template<class CT, class T1> static _FINLINE void Op(CT& v, const T1& v1) {
		if (v > v1)
			v = v1;
	}
	template<class CT, class T1, class T2> static _FINLINE void Op(CT& v, const T1& v1, const T2& v2) {
		v = v1 > v2 ? v2 : v1;
	}
};


//! 指定数要素に指定オペレータを適用するテンプレートメタなクラス、コンパイラによっては for 文展開してくれるかわからないため確実に展開するために使用する
template<
	class Operator, //!< TmAdd などのオペレータクラス
	intptr_t N, //!< 要素数
	intptr_t LIM = 32 //!< 最大要素数制限、この値以上の要素数を処理する場合には for が使用される、コンパイラが展開してくれるかもしれない
> struct Order {

	//! 単項演算子を適用し元の値を書き換える
	template<class CT> static _FINLINE void UnaryAssign(CT* e) {
		if (N <= LIM) {
			Operator::Op(e[N - 1]);
			Order<Operator, N - 1, LIM>::UnaryAssign(e);
		} else {
			for (intptr_t i = N - 1; i != -1; --i)
				Operator::Op(e[i]);
		}
	}

	//! ２項演算子を適用し元の値を書き換える
	template<class CT, class T1> static _FINLINE void BinaryAssign(CT* e, const T1* e1) {
		if (N <= LIM) {
			Operator::Op(e[N - 1], e1[N - 1]);
			Order<Operator, N - 1, LIM>::BinaryAssign(e, e1);
		} else {
			for (intptr_t i = N - 1; i != -1; --i)
				Operator::Op(e[i], e1[i]);
		}
	}

	//! ２項演算子を適用し元の値を書き換える、対となる項はスカラー
	template<class CT, class T1> static _FINLINE void BinaryAssignScalar(CT* e, const T1& s) {
		if (N <= LIM) {
			Operator::Op(e[N - 1], s);
			Order<Operator, N - 1, LIM>::BinaryAssignScalar(e, s);
		} else {
			for (intptr_t i = N - 1; i != -1; --i)
				Operator::Op(e[i], s);
		}
	}

	//! 指定インデックスの要素とそれ以外で設定する値を切り替える
	template<class CT, class T1> static _FINLINE void BinaryAssignByIndex(CT* e, intptr_t index, const T1& value, const T1& other) {
		if (N <= LIM) {
			Operator::Op(e[N - 1], N - 1 == index ? value : other);
			Order<Operator, N - 1, LIM>::BinaryAssignByIndex(e, index, value, other);
		} else {
			for (intptr_t i = N - 1; i != -1; --i)
				Operator::Op(e[i], i == index ? value : other);
		}
	}

	//! ２項演算子を適用し戻り値用変数へ代入する
	template<class CT, class T1, class T2> static _FINLINE void Binary(CT* r, const T1* e1, const T2* e2) {
		if (N <= LIM) {
			Operator::Op(r[N - 1], e1[N - 1], e2[N - 1]);
			Order<Operator, N - 1, LIM>::Binary(r, e1, e2);
		} else {
			for (intptr_t i = N - 1; i != -1; --i)
				Operator::Op(r[i], e1[i], e2[i]);
		}
	}

	//! ２項演算子を適用し戻り値用変数へ代入する、対となる項はスカラー
	template<class CT, class T1, class T2> static _FINLINE void BinaryScalar(CT* r, const T1* e1, const T2& s) {
		if (N <= LIM) {
			Operator::Op(r[N - 1], e1[N - 1], s);
			Order<Operator, N - 1, LIM>::BinaryScalar(r, e1, s);
		} else {
			for (intptr_t i = N - 1; i != -1; --i)
				Operator::Op(r[i], e1[i], s);
		}
	}

	//! ベクトル e1 の要素とベクトル e2 の要素を２項演算子で演算後その値を用いて e を変化させる
	template<class CT, class T1, class T2> static _FINLINE void BinaryModulate(CT* e, const T1* e1, const T1* e2) {
		if (N <= LIM) {
			Operator::Op(e[N - 1], e1[N - 1], e2[N - 1]);
			Order<Operator, N - 1, LIM>::BinaryModulate(e, e1, e2);
		} else {
			for (intptr_t i = N - 1; i != -1; --i)
				Operator::Op(e[i], e1[i], e2[i]);
		}
	}

	//! ベクトル e1 の要素とスカラー s を２項演算子で演算後その値を用いて e を変化させる
	template<class CT, class T1, class T2> static _FINLINE void BinaryScalarModulate(CT* e, const T1* e1, const T2& s) {
		if (N <= LIM) {
			Operator::Op(e[N - 1], e1[N - 1], s);
			Order<Operator, N - 1, LIM>::BinaryScalarModulate(e, e1, s);
		} else {
			for (intptr_t i = N - 1; i != -1; --i)
				Operator::Op(e[i], e1[i], s);
		}
	}

	//! x+y+z の様に水平に２項演算子を適用し r に代入する
	template<class CT, class T1> static _FINLINE void HorizontalBinary(CT& r, const T1* e) {
		if (N <= LIM) {
			Operator::Start(r, e[N - 1]);
			Order<Operator, N - 1, LIM>::HorizontalBinaryInternal(r, e);
		} else {
			Operator::Start(r, e[N - 1]);
			for (intptr_t i = N - 2; i != -1; --i)
				Operator::Op(r, e[i]);
		}
	}

	//! HorizontalBinary の内部処理
	template<class CT, class T1> static _FINLINE void HorizontalBinaryInternal(CT& r, const T1* e) {
		Operator::Op(r, e[N - 1]);
		Order<Operator, N - 1, LIM>::HorizontalBinaryInternal(r, e);
	}

	//! ２項が同じ値なら true を返す、それ以外は false
	template<class T1, class T2> static _FINLINE bool Equal(const T1* e1, const T2* e2) {
		if (N <= LIM) {
			return e1[N - 1] == e2[N - 1] && Order<Operator, N - 1, LIM>::Equal(e1, e2);
		} else {
			for (intptr_t i = N - 1; i != -1; --i)
				if (e1[i] != e2[i])
					return false;
			return true;
		}
	}

	//! ２項が同じ値なら true を返す、それ以外は false、対となる項はスカラー
	template<class T1, class T2> static _FINLINE bool EqualScalar(const T1* e, const T2& s) {
		if (N <= LIM) {
			return e[N - 1] == s && Order<Operator, N - 1, LIM>::EqualScalar(e, s);
		} else {
			for (intptr_t i = N - 1; i != -1; --i)
				if (e[i] != s)
					return false;
			return true;
		}
	}

	//! ２項が異なる値なら true を返す、それ以外は false
	template<class T1, class T2> static _FINLINE bool NotEqual(const T1* e1, const T2* e2) {
		if (N <= LIM) {
			return e1[N - 1] != e2[N - 1] || Order<Operator, N - 1, LIM>::NotEqual(e1, e2);
		} else {
			for (intptr_t i = N - 1; i != -1; --i)
				if (e1[i] != e2[i])
					return true;
			return false;
		}
	}

	//! ２項が異なる値なら true を返す、それ以外は false、対となる項はスカラー
	template<class T1, class T2> static _FINLINE bool NotEqualScalar(const T1* e, const T2& s) {
		if (N <= LIM) {
			return e[N - 1] != s || Order<Operator, N - 1, LIM>::NotEqualScalar(e, s);
		} else {
			for (intptr_t i = N - 1; i != -1; --i)
				if (e[i] != s)
					return true;
			return false;
		}
	}

	//! p1 の全要素の値が p2 より小さいなら true を返す、それ以外は false
	template<class T1, class T2> static _FINLINE bool LessThan(const T1* p1, const T2* p2) {
		if (N <= LIM) {
			if (p2[N - 1] <= p1[N - 1])
				return false;
			return Order<Operator, N - 1, LIM>::LessThan(p1, p2);
		} else {
			for (intptr_t i = N - 1; i != -1; --i) {
				if (p2[i] <= p1[i])
					return false;
			}
			return true;
		}
	}

	//! p1 の全要素の値が p2 以下なら true を返す、それ以外は false
	template<class T1, class T2> static _FINLINE bool LessThanOrEqual(const T1* p1, const T2* p2) {
		if (N <= LIM) {
			if (p2[N - 1] < p1[N - 1])
				return false;
			return Order<Operator, N - 1, LIM>::LessThanOrEqual(p1, p2);
		} else {
			for (intptr_t i = N - 1; i != -1; --i) {
				if (p2[i] < p1[i])
					return false;
			}
			return true;
		}
	}

	//! p1 と p2 の内積を計算し v へ代入する
	template<class CT, class T1, class T2> static _FINLINE void Dot(CT& v, const T1* p1, const T2* p2) {
		if (N <= LIM) {
			v = p1[N - 1] * p2[N - 1];
			Order<Operator, N - 1, LIM>::DotInternal(v, p1, p2);
		} else {
			v = p1[N - 1] * p2[N - 1];
			for (intptr_t i = N - 2; i != -1; --i)
				v += p1[i] * p2[i];
		}
	}

	//! Dot の内部処理
	template<class CT, class T1, class T2> static _FINLINE void DotInternal(CT& v, const T1* p1, const T2* p2) {
		v += p1[N - 1] * p2[N - 1];
		Order<Operator, N - 1, LIM>::DotInternal(v, p1, p2);
	}

	//! Cross 内部処理用構造体、N を維持したまま N1 をインデックスとして変化させ使用する
	template<intptr_t N1, intptr_t dummy = 0> struct CrossStruct {
		template<class CT, class T1, class T2> static _FINLINE void Cross(CT* p, const T1* p1, const T2* p2) {
			enum { I1 = (N1 - 1 + 1) % N, I2 = (N1 - 1 + 2) % N };
			p[N1 - 1] = p1[I1] * p2[I2] - p1[I2] * p2[I1];
			CrossStruct<N1 - 1>::Cross(p, p1, p2);
		}
	};
	template<intptr_t dummy> struct CrossStruct<0, dummy> {
		template<class CT, class T1, class T2> static _FINLINE void Cross(CT* p, const T1* p1, const T2* p2) {}
	};

	//! p1 と p2 の外積を計算し p へ代入する
	template<class CT, class T1, class T2> static _FINLINE void Cross(CT* p, const T1* p1, const T2* p2) {
		if (N <= LIM) {
			CrossStruct<N>::Cross(p, p1, p2);
		} else {
			for (intptr_t i = 0; i < N; ++i) {
				intptr_t i1 = (i + 1) % N, i2 = (i + 2) % N;
				p[i] = p1[i1] * p2[i2] - p1[i2] * p2[i1];
			}
		}
	}
};

//! 指定数の要素に指定オペレータを適用するテンプレートメタなクラスの終端
template<class Operator, intptr_t LIM> struct Order<Operator, 0, LIM> {
	template<class CT> static _FINLINE void UnaryAssign(CT* p) {}
	template<class CT, class T1> static _FINLINE void BinaryAssign(CT* p, const T1* p1) {}
	template<class CT, class T1> static _FINLINE void BinaryAssignScalar(CT* p, const T1& s) {}
	template<class CT, class T1> static _FINLINE void BinaryAssignByIndex(CT* p, intptr_t index, const T1& value, const T1& other) {}
	template<class CT, class T1, class T2> static _FINLINE void Binary(CT* p, const T1* p1, const T2* p2) {}
	template<class CT, class T1, class T2> static _FINLINE void BinaryScalar(CT* p, const T1* p1, const T2& s) {}
	template<class CT, class T1, class T2> static _FINLINE void BinaryModulate(CT* e, const T1* e1, const T1* e2) {}
	template<class CT, class T1, class T2> static _FINLINE void BinaryScalarModulate(CT* e, const T1* e1, const T2& s) {}
	template<class CT> static _FINLINE void HorizontalBinary(CT& r, const CT* e) {}
	template<class CT, class T1> static _FINLINE void HorizontalBinaryInternal(CT& r, const T1* e) {}
	template<class T1, class T2> static _FINLINE bool Equal(const T1* p1, const T2* p2) {
		return true;
	}
	template<class T1, class T2> static _FINLINE bool EqualScalar(const T1* p1, const T2& v) {
		return true;
	}
	template<class T1, class T2> static _FINLINE bool NotEqual(const T1* p1, const T2* p2) {
		return false;
	}
	template<class T1, class T2> static _FINLINE bool NotEqualScalar(const T1* p1, const T2& v) {
		return false;
	}
	template<class T1, class T2> static _FINLINE bool LessThan(const T1* p1, const T2* p2) {
		return true;
	}
	template<class T1, class T2> static _FINLINE bool LessThanOrEqual(const T1* p1, const T2* p2) {
		return true;
	}
	template<class CT, class T1, class T2> static _FINLINE void DotInternal(CT& v, const T1* p1, const T2* p2) {}
	template<class CT, class T1, class T2> static _FINLINE void Cross(CT* p, const T1* p1, const T2* p2) {}
};

_JUNK_END

#endif
