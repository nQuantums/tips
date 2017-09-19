#pragma once
#ifndef __JUNK_ENCODING_H__
#define __JUNK_ENCODING_H__

#include "JunkConfig.h"
#include "JunkDef.h"

#if defined _MSC_VER
#include <Windows.h>
#else
#error gcc version is not implemented.
#endif

#include <string>


_JUNK_BEGIN

//! 文字コードクラス
class JUNKAPICLASS Encoding {
#if defined _MSC_VER
private:
	uint32_t CodePage; //!< コードページ
	uint32_t Flags; //!< 文字の種類を指定するフラグ

public:
	//!< ASCIIエンコーディングの取得
	static Encoding ASCII() {
		Encoding enc;
		enc.CodePage = CP_ACP;
		enc.Flags = 0;
		return enc;
	}

	//!< UTF8エンコーディングの取得
	static Encoding UTF8() {
		Encoding enc;
		enc.CodePage = CP_UTF8;
		enc.Flags = 0;
		return enc;
	}

	//!< Shift_JISエンコーディングの取得
	static Encoding ShiftJIS() {
		Encoding enc;
		enc.CodePage = 932;
		enc.Flags = 0;
		return enc;
	}

	void GetString(const char* bytes, size_t size, std::wstring& str) const; //!< バイト配列から文字列の取得
	void GetString(const char* bytes, std::wstring& str) const; //!< バイト配列から文字列の取得
	void GetString(const std::string& bytes, std::wstring& str) const; //!< バイト配列から文字列の取得
	std::wstring GetString(const char* bytes, size_t size) const; //!< バイト配列から文字列の取得
	std::wstring GetString(const char* bytes) const; //!< バイト配列から文字列の取得
	std::wstring GetString(const std::string& bytes) const; //!< バイト配列から文字列の取得
	void GetBytes(const wchar_t* str, size_t strLen, std::string& bytes) const; //!< 文字列からバイト配列の取得
	void GetBytes(const wchar_t* str, std::string& bytes) const; //!< 文字列からバイト配列の取得
	void GetBytes(const std::wstring& str, std::string& bytes) const; //!< 文字列からバイト配列の取得
	std::string GetBytes(const wchar_t* str, size_t strLen) const; //!< 文字列からバイト配列の取得
	std::string GetBytes(const wchar_t* str) const; //!< 文字列からバイト配列の取得
	std::string GetBytes(const std::wstring& str) const; //!< 文字列からバイト配列の取得
#else
#error gcc version is not implemented.
#endif
};

_JUNK_END

#endif
