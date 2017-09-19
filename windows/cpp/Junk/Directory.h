#pragma once
#ifndef __JUNK_DIRECTORY_H__
#define __JUNK_DIRECTORY_H__

#include "JunkConfig.h"
#include <vector>
#include <string>

_JUNK_BEGIN

//! ディレクトリ
class JUNKAPICLASS Directory {
public:
	static ibool GetCurrent(std::wstring& curDir); //!< カレントディレクトリを取得する
	static ibool GetCurrent(std::string& curDir); //!< カレントディレクトリを取得する
	static ibool Exists(const wchar_t* pszDir); //!< 指定されたディレクトリが存在しているか調べる
	static ibool Exists(const char* pszDir); //!< 指定されたディレクトリが存在しているか調べる
	static ibool Create(const wchar_t* pszDir); //!< 指定されたディレクトリを作成する
	static ibool Create(const char* pszDir); //!< 指定されたディレクトリを作成する

	static std::wstring GetCurrentW(); //!< カレントディレクトリを取得する
	static std::string GetCurrentA(); //!< カレントディレクトリを取得する
	static std::wstring GetExeDirectoryW(); //!< 実行中EXEの置かれているディレクトリパス名を取得する
	static std::string GetExeDirectoryA(); //!< 実行中EXEの置かれているディレクトリパス名を取得する
};

_JUNK_END

#endif
