#pragma once
#ifndef __JUNK_JUNKDEF_H__
#define __JUNK_JUNKDEF_H__

#include "JunkConfig.h"
#include <limits.h>
#include <float.h>
#include <math.h>

_JUNK_BEGIN

// 数学定数
#define	JUNK_PI       3.141592653589793238462643383279502884197169399375 // π
#define	JUNK_PI_DIV_2 1.57079632679489661923132169163975                 // π/2
#define	JUNK_2_PI     6.28318530717958647692528676655901                 // 2π
#define	JUNK_INV_PI   0.318309886183790671537767526745029                // 1/π
#define	JUNK_RADTODEG 57.2957795130823208767981548141052                 // 180/π
#define	JUNK_DEGTORAD 0.0174532925199432957692369076848861               // π/180
#define	JUNK_EXP      2.71828182845904523536                             // ε
#define	JUNK_ILOG2    3.32192809488736234787                             // 1/log10(2)
#define	JUNK_INV3     0.33333333333333333333                             // 1/3
#define	JUNK_INV6     0.16666666666666666666                             // 1/6
#define	JUNK_INV7     0.14285714285714285714                             // 1/9
#define	JUNK_INV9     0.11111111111111111111                             // 1/9
#define	JUNK_INV255   0.00392156862745098039215686274509804              // 1/255
#define	JUNK_SQRT2    1.4142135623730950488016887242097                  // √2

// 型別の最小値、最大取得関数
template<class T> _FINLINE T MinVal();
template<class T> _FINLINE T MaxVal();
template<class T> _FINLINE T Epsilon();

template<> _FINLINE int8_t MinVal<int8_t>() {
	return SCHAR_MIN;
}
template<> _FINLINE int8_t MaxVal<int8_t>() {
	return SCHAR_MAX;
}
template<> _FINLINE uint8_t MinVal<uint8_t>() {
	return 0;
}
template<> _FINLINE uint8_t MaxVal<uint8_t>() {
	return UCHAR_MAX;
}

template<> _FINLINE int16_t MinVal<int16_t>() {
	return SHRT_MIN;
}
template<> _FINLINE int16_t MaxVal<int16_t>() {
	return SHRT_MAX;
}
template<> _FINLINE uint16_t MinVal<uint16_t>() {
	return 0;
}
template<> _FINLINE uint16_t MaxVal<uint16_t>() {
	return USHRT_MAX;
}

template<> _FINLINE int32_t MinVal<int32_t>() {
	return INT_MIN;
}
template<> _FINLINE int32_t MaxVal<int32_t>() {
	return INT_MAX;
}
template<> _FINLINE uint32_t MinVal<uint32_t>() {
	return 0;
}
template<> _FINLINE uint32_t MaxVal<uint32_t>() {
	return UINT_MAX;
}

template<> _FINLINE int64_t MinVal<int64_t>() {
	return LLONG_MIN;
}
template<> _FINLINE int64_t MaxVal<int64_t>() {
	return LLONG_MAX;
}
template<> _FINLINE uint64_t MinVal<uint64_t>() {
	return 0;
}
template<> _FINLINE uint64_t MaxVal<uint64_t>() {
	return ULLONG_MAX;
}

template<> _FINLINE long MinVal<long>() {
	return LONG_MIN;
}
template<> _FINLINE long MaxVal<long>() {
	return LONG_MAX;
}
template<> _FINLINE unsigned long MinVal<unsigned long>() {
	return 0;
}
template<> _FINLINE unsigned long MaxVal<unsigned long>() {
	return ULONG_MAX;
}

template<> _FINLINE float MinVal<float>() {
	return -FLT_MAX;
}
template<> _FINLINE float MaxVal<float>() {
	return FLT_MAX;
}

template<> _FINLINE double MinVal<double>() {
	return -DBL_MAX;
}
template<> _FINLINE double MaxVal<double>() {
	return DBL_MAX;
}

template<> _FINLINE float Epsilon<float>() {
	return FLT_EPSILON;
}
template<> _FINLINE double Epsilon<double>() {
	return DBL_EPSILON;
}

//! 四捨五入
template<
	class R, //!< 出力正数型
	class T //!< 入力実数型
> _FINLINE R Nint(T s) {
	return s < T(0.5) ? R(s - T(0.5)) : R(s + T(0.5));
}

_JUNK_END

#endif
