#pragma once
#ifndef __JUNK_FIXED_H__
#define __JUNK_FIXED_H__

#include "JunkConfig.h"

_JUNK_BEGIN

//! 固定小数点数クラス

//! 除算は double に変換しているので遅い(但し精度はいい)です、あまり使わないように注意
template<
	class T, //!< 値を保持するデータ型
	int _Dp //!< 小数部分のビット数
>
struct Fixed {
	enum {
		Dp = _Dp, //!< 小数部分のビット数
		DpHalf = _Dp / 2, //!< 小数部分のビット数の半分
	};

	typedef T ValueType; //!< 値の型
	typedef Fixed<T, _Dp> Self; //!< 自分自身の型

	T Value; //!< 値

	_FINLINE Fixed() {
	}
	_FINLINE Fixed(int8_t v) {
		Value = (T)v << Dp;
	}
	_FINLINE Fixed(uint8_t v) {
		Value = (T)v << Dp;
	}
	_FINLINE Fixed(int16_t v) {
		Value = (T)v << Dp;
	}
	_FINLINE Fixed(uint16_t v) {
		Value = (T)v << Dp;
	}
	_FINLINE Fixed(int32_t v) {
		Value = (T)v << Dp;
	}
	_FINLINE Fixed(uint32_t v) {
		Value = (T)v << Dp;
	}
	_FINLINE Fixed(float v) {
		Value = (T)((float)One() * v);
	}
	_FINLINE Fixed(double v) {
		Value = (T)((double)One() * v);
	}

	static _FINLINE T One() {
		return (T)1 << Dp;
	}
	static _FINLINE T DpMask() {
		return ((T)1 << Dp) - 1;
	}
	_FINLINE T DpValue() const {
		return Value & DpMask();
	}
	_FINLINE bool HaveDp() const {
		return Value & DpMask() ? true : false;
	}
	_FINLINE Fixed Floor() const {
		return Cast(Value & ~DpMask());
	}
	_FINLINE Fixed Ceil() const {
		return Cast(Value + (One() - (Value & DpMask())));
	}

	_FINLINE Fixed& operator=(int8_t v) {
		Value = (T)v << Dp;
		return *this;
	}
	_FINLINE Fixed& operator=(uint8_t v) {
		Value = (T)v << Dp;
		return *this;
	}
	_FINLINE Fixed& operator=(int16_t v) {
		Value = (T)v << Dp;
		return *this;
	}
	_FINLINE Fixed& operator=(uint16_t v) {
		Value = (T)v << Dp;
		return *this;
	}
	_FINLINE Fixed& operator=(int32_t v) {
		Value = (T)v << Dp;
		return *this;
	}
	_FINLINE Fixed& operator=(uint32_t v) {
		Value = (T)v << Dp;
		return *this;
	}
	_FINLINE Fixed& operator=(float v) {
		Value = (T)((float)One() * v);
		return *this;
	}
	_FINLINE Fixed& operator=(double v) {
		Value = (T)((double)One() * v);
		return *this;
	}

	_FINLINE Fixed operator+() const {
		return *this;
	}
	_FINLINE Fixed operator-() const {
		return Cast(-Value);
	}
	_FINLINE Fixed operator+(Fixed v) const {
		return Cast(Value + v.Value);
	}
	_FINLINE Fixed operator+(int v) const {
		return Cast(Value + ((T)v << Dp));
	}
	_FINLINE friend Fixed operator+(int v1, Fixed v2) {
		return Fixed::Cast(((T)v1 << v2.Dp) + v2.Value);
	}
	_FINLINE Fixed operator-(Fixed v) const {
		return Cast(Value - v.Value);
	}
	_FINLINE Fixed operator-(int v) const {
		return Cast(Value - ((T)v << Dp));
	}
	_FINLINE friend Fixed operator-(int v1, Fixed v2) {
		return Fixed::Cast(((T)v1 << v2.Dp) - v2.Value);
	}
	_FINLINE Fixed operator*(Fixed v) const {
		return Cast((Value >> DpHalf) * (v.Value >> DpHalf));
	}
	_FINLINE Fixed operator*(int v) const {
		return Cast(Value * v);
	}
	_FINLINE friend Fixed operator*(int v1, Fixed v2) {
		return Fixed::Cast(v1 * v2.Value);
	}
	_FINLINE Fixed operator/(Fixed v) const {
		return Cast((T)((double)One() * (double)Value / (double)v.Value));
	}
	_FINLINE Fixed operator/(int v) const {
		return Cast(Value / v);
	}
	_FINLINE Fixed operator<<(int n) const {
		return Cast(Value << n);
	}
	_FINLINE Fixed operator >> (int n) const {
		return Cast(Value << n);
	}
	_FINLINE Fixed& operator+=(Fixed v) {
		Value += v.Value;
		return *this;
	}
	_FINLINE Fixed operator-=(Fixed v) {
		Value -= v.Value;
		return *this;
	}
	_FINLINE Fixed operator*=(Fixed v) {
		Value = (Value >> DpHalf) * (v.Value >> DpHalf);
		return *this;
	}
	_FINLINE Fixed operator/=(Fixed v) {
		Value = (T)((double)One() * (double)Value / (double)v.Value);
		return *this;
	}
	_FINLINE Fixed operator<<=(int n) const {
		Value <<= n;
		return *this;
	}
	_FINLINE Fixed operator>>=(int n) const {
		Value >>= n;
		return *this;
	}

	_FINLINE operator int8_t() const {
		return (int8_t)(Value >> Dp);
	}
	_FINLINE operator uint8_t() const {
		return (uint8_t)(Value >> Dp);
	}
	_FINLINE operator int16_t() const {
		return (int16_t)(Value >> Dp);
	}
	_FINLINE operator uint16_t() const {
		return (uint16_t)(Value >> Dp);
	}
	_FINLINE operator int32_t() const {
		return (int32_t)(Value >> Dp);
	}
	_FINLINE operator uint32_t() const {
		return (uint32_t)(Value >> Dp);
	}
	_FINLINE operator int64_t() const {
		return (int64_t)(Value >> Dp);
	}
	_FINLINE operator uint64_t() const {
		return (uint64_t)(Value >> Dp);
	}
	_FINLINE operator float() const {
		return (float)Value / (float)One();
	}
	_FINLINE operator double() const {
		return (double)Value / (double)One();
	}

	_FINLINE bool operator<(Fixed v) const {
		return Value < v.Value;
	}
	_FINLINE bool operator<(int v) const {
		return Value < ((T)v << Dp);
	}
	_FINLINE friend bool operator<(int v1, Fixed v2) {
		return ((T)v1 << Dp) < v2.Value;
	}
	_FINLINE bool operator>(Fixed v) const {
		return Value > v.Value;
	}
	_FINLINE bool operator>(int v) const {
		return Value > ((T)v << Dp);
	}
	_FINLINE friend bool operator>(int v1, Fixed v2) {
		return ((T)v1 << Dp) > v2.Value;
	}
	_FINLINE bool operator<=(Fixed v) const {
		return Value <= v.Value;
	}
	_FINLINE bool operator<=(int v) const {
		return Value <= ((T)v << Dp);
	}
	_FINLINE friend bool operator<=(int v1, Fixed v2) {
		return ((T)v1 << Dp) <= v2.Value;
	}
	_FINLINE bool operator>=(Fixed v) const {
		return Value >= v.Value;
	}
	_FINLINE bool operator>=(int v) const {
		return Value >= ((T)v << Dp);
	}
	_FINLINE friend bool operator>=(int v1, Fixed v2) {
		return ((T)v1 << Dp) >= v2.Value;
	}
	_FINLINE bool operator==(Fixed v) const {
		return Value == v.Value;
	}
	_FINLINE bool operator==(int v) const {
		return Value == ((T)v << Dp);
	}
	_FINLINE friend bool operator==(int v1, Fixed v2) {
		return ((T)v1 << Dp) == v2.Value;
	}
	_FINLINE bool operator!=(Fixed v) const {
		return Value != v.Value;
	}
	_FINLINE bool operator!=(int v) const {
		return Value != ((T)v << Dp);
	}
	_FINLINE friend bool operator!=(int v1, Fixed v2) {
		return ((T)v1 << Dp) != v2.Value;
	}

protected:
	_FINLINE static Fixed Cast(T v) {
		Fixed f;
		f.Value = v;
		return f;
	}
};

_JUNK_END

#endif
