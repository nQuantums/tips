#pragma once
#ifndef __JUNK_STR_H__
#define __JUNK_STR_H__

#include "JunkConfig.h"
#include <stdarg.h>
#include <string>

_JUNK_BEGIN

//! 文字列関係ヘルパ
struct JUNKAPICLASS Str {
	//static void FV(std::wstring& s, const wchar_t* pszFmt, va_list args); //!< 指定の文字列へ書式化した文字を代入する
	//static void AFV(std::wstring& s, const wchar_t* pszFmt, va_list args); //!< 指定の文字列へ書式化した文字を追加する
	//static void F(std::wstring& s, const wchar_t* pszFmt, ...); //!< 指定の文字列へ書式化した文字を代入する
	//static void AF(std::wstring& s, const wchar_t* pszFmt, ...); //!< 指定の文字列へ書式化した文字を追加する
	//static void A(std::wstring& s, const wchar_t* value); //!< 指定の文字列へ指定値を文字列化して追加する
	//static void A(std::wstring& s, int value); //!< 指定の文字列へ指定値を文字列化して追加する
	//static void A(std::wstring& s, uint32_t value); //!< 指定の文字列へ指定値を文字列化して追加する
	//static void A(std::wstring& s, unsigned long value); //!< 指定の文字列へ指定値を文字列化して追加する
	//static void A(std::wstring& s, int64_t value); //!< 指定の文字列へ指定値を文字列化して追加する
	//static std::wstring FV(const wchar_t* pszFmt, va_list args); //!< 書式化した文字を取得する
	//static std::wstring F(const wchar_t* pszFmt, ...); //!< 書式化した文字を取得する
	//static void Replace(std::wstring& s, const wchar_t* pszBefore, const wchar_t* pszAfter); //!< 文字列を置き換える

	static void FV(std::string& s, const char* pszFmt, va_list args); //!< 指定の文字列へ書式化した文字を代入する
	static void AFV(std::string& s, const char* pszFmt, va_list args); //!< 指定の文字列へ書式化した文字を追加する
	static void F(std::string& s, const char* pszFmt, ...); //!< 指定の文字列へ書式化した文字を代入する
	static void AF(std::string& s, const char* pszFmt, ...); //!< 指定の文字列へ書式化した文字を追加する
	static std::string FV(const char* pszFmt, va_list args); //!< 書式化した文字を取得する
	static std::string F(const char* pszFmt, ...); //!< 書式化した文字を取得する
	static void Replace(std::string& s, const char* pszBefore, const char* pszAfter); //!< 文字列を置き換える
};

_JUNK_END

#endif
