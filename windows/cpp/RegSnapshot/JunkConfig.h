#pragma once
#ifndef __JUNK_JUNKCONFIG_H__
#define __JUNK_JUNKCONFIG_H__

#include <assert.h>
#include <ctype.h>
#include <stddef.h>
#if defined(_MSC_VER) && _MSC_VER <= 1500
typedef signed char        int8_t;
typedef short              int16_t;
typedef int                int32_t;
typedef long long          int64_t;
typedef unsigned char      uint8_t;
typedef unsigned short     uint16_t;
typedef unsigned int       uint32_t;
typedef unsigned long long uint64_t;

typedef signed char        int_least8_t;
typedef short              int_least16_t;
typedef int                int_least32_t;
typedef long long          int_least64_t;
typedef unsigned char      uint_least8_t;
typedef unsigned short     uint_least16_t;
typedef unsigned int       uint_least32_t;
typedef unsigned long long uint_least64_t;

typedef signed char        int_fast8_t;
typedef int                int_fast16_t;
typedef int                int_fast32_t;
typedef long long          int_fast64_t;
typedef unsigned char      uint_fast8_t;
typedef unsigned int       uint_fast16_t;
typedef unsigned int       uint_fast32_t;
typedef unsigned long long uint_fast64_t;
#else
#include <stdint.h>
#endif

// ネームスペース用マクロ
#define _JUNK_BEGIN namespace jk {
#define _JUNK_END }
#define _JUNK_USING using namespace jk;

// DLLエクスポート、インポート設定用マクロ
//  _JUNK_EXPORTS が定義されている場合はDLLエクスポート用コンパイル
//  _JUNK_IMPORTS が定義されている場合はDLLインポート用コンパイル
// になります
#if defined(_MSC_VER)
#if defined(_JUNK_EXPORTS)
#define JUNKAPI extern "C" __declspec(dllexport)
#define JUNKAPICLASS __declspec(dllexport)
#define JUNKCALL __stdcall
#elif defined(_JUNK_IMPORTS)
#define JUNKAPI extern "C" __declspec(dllimport)
#define JUNKAPICLASS __declspec(dllimport)
#define JUNKCALL __stdcall
#else
#define JUNKAPI
#define JUNKAPICLASS
#define JUNKCALL __stdcall
#endif
#endif

// 強制インライン展開マクロ
#if defined  _MSC_VER
#define _FINLINE inline __forceinline
#else
#define _FINLINE inline __attribute__((always_inline))
#endif


_JUNK_BEGIN
//! bool 型を使うと遅いことがあるのでアーキテクチャのbit数に合わせたもの
typedef intptr_t ibool;
_JUNK_END

#endif
