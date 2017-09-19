#pragma once
#ifndef __JUNK_FILEPATH_H__
#define __JUNK_FILEPATH_H__

#include "JunkConfig.h"
#include <string>

_JUNK_BEGIN

//! ファイルパス操作
class JUNKAPICLASS FilePath {
public:
	static ibool Combine(std::string& result, const char* pszPath, const char* pszFile); //!< 指定されたパス名とファイル名を連結する
	static ibool StripPath(std::string& result, const char* pszPath); //!< パス名を取り除きファイル名部分のみ取得する

	static std::wstring Combine(const std::wstring& path1, const std::wstring& path2); //!< 2つの文字列を1つのパスに結合する
	static std::string Combine(const std::string& path1, const std::string& path2); //!< 2つの文字列を1つのパスに結合する
	static std::wstring GetDirectoryName(const std::wstring& path); //!< 指定されたパス名のディレクトリ名部分を取得する
	static std::string GetDirectoryName(const std::string& path); //!< 指定されたパス名のディレクトリ名部分を取得する
	static std::wstring GetFileName(const std::wstring& path); //!< 指定したパス文字列のファイル名と拡張子を取得する
	static std::string GetFileName(const std::string& path); //!< 指定したパス文字列のファイル名と拡張子を取得する
	static std::wstring GetExtension(const std::wstring& path); //!< 指定したパス文字列の拡張子を取得する
	static std::string GetExtension(const std::string& path); //!< 指定したパス文字列の拡張子を取得する
};

_JUNK_END

#endif
