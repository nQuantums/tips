#include "FilePath.h"

#include <string.h>

#if defined __GNUC__
#else

#include <Windows.h>
#include <Shlwapi.h>

#pragma comment(lib, "Shlwapi.lib")

#endif



_JUNK_BEGIN

//! 指定されたパス名とファイル名を連結する
intptr_t FilePath::Combine(
	std::string& result, //!< [out] 連結されたパスが返る
	const char* pszPath, //!< [in] パス名
	const char* pszFile //!< [in] ファイル名
) {
#if defined __GNUC__
	// もしファイル名がフルパスならファイル名をそのまま返す
	if(pszFile[0] == '/') {
		result = pszFile;
		return true;
	}

	// パス名で初期化
	result = pszPath;

	// パス名の最後が '/' じゃなかったら追加する
	if(!result.empty() && result[result.size() - 1] != '/')
		result += "/";

	// ファイル名を連結
	result += pszFile;

	return true;
#else
	char buf[MAX_PATH];
	::PathCombineA(buf, pszPath, pszFile);
	result = buf;
	return true;
#endif
}

//! パス名を取り除きファイル名部分のみ取得する
ibool FilePath::StripPath(
	std::string& result, //!< [out] ファイル名部分が返る
	const char* pszPath //!< [in] パス名
) {
#if defined __GNUC__
	// もしファイル名がフルパスならファイル名をそのまま返す
	const char* p = strrchr(pszPath, '/');
	result.resize(0);
	if(p != NULL) {
		p++;
		result.insert(result.begin(), p, p + strlen(p));
	}
	return true;
#else
	result = pszPath;
	if (result.empty())
		return true;
	::PathStripPathA(&result[0]);
	result.resize(strlen(result.c_str()));
	return true;
#endif
}

//! 2つの文字列を1つのパスに結合する
std::wstring FilePath::Combine(const std::wstring& path1, const std::wstring& path2) {
#if defined _MSC_VER
	wchar_t buf[MAX_PATH] = L"";
	::PathCombineW(buf, path1.c_str(), path2.c_str());
	return buf;
#else
#error gcc version is not implemented.
#endif
}

//! 2つの文字列を1つのパスに結合する
std::string FilePath::Combine(const std::string& path1, const std::string& path2) {
#if defined _MSC_VER
	char buf[MAX_PATH] = "";
	::PathCombineA(buf, path1.c_str(), path2.c_str());
	return buf;
#else
#error gcc version is not implemented.
#endif
}

//! 指定されたパス名のディレクトリ名部分を取得する
std::wstring FilePath::GetDirectoryName(const std::wstring& path) {
#if defined _MSC_VER
	wchar_t drive[MAX_PATH] = L"";
	wchar_t dir[MAX_PATH] = L"";
	_wsplitpath_s(path.c_str(), drive, MAX_PATH, dir, MAX_PATH, NULL, 0, NULL, 0);
#if 1700 <= _MSC_VER
	return std::move(std::wstring(drive) + dir);
#else
	return std::wstring(drive) + dir;
#endif
#else
#error gcc version is not implemented.
#endif
}

//! 指定されたパス名のディレクトリ名部分を取得する
std::string FilePath::GetDirectoryName(const std::string& path) {
#if defined _MSC_VER
	char drive[MAX_PATH] = "";
	char dir[MAX_PATH] = "";
	_splitpath_s(path.c_str(), drive, MAX_PATH, dir, MAX_PATH, NULL, 0, NULL, 0);
#if 1700 <= _MSC_VER
	return std::move(std::string(drive) + dir);
#else
	return std::string(drive) + dir;
#endif
#else
#error gcc version is not implemented.
#endif
}

//! 指定したパス文字列のファイル名と拡張子を取得する
std::wstring FilePath::GetFileName(const std::wstring& path) {
#if defined _MSC_VER
	wchar_t filename[MAX_PATH] = L"";
	wchar_t ext[MAX_PATH] = L"";
	_wsplitpath_s(path.c_str(), NULL, 0, NULL, 0, filename, MAX_PATH, ext, MAX_PATH);
#if 1700 <= _MSC_VER
	return std::move(std::wstring(filename) + ext);
#else
	return std::wstring(filename) + ext;
#endif
#else
#error gcc version is not implemented.
#endif
}

//! 指定したパス文字列のファイル名と拡張子を取得する
std::string FilePath::GetFileName(const std::string& path) {
#if defined _MSC_VER
	char filename[MAX_PATH] = "";
	char ext[MAX_PATH] = "";
	_splitpath_s(path.c_str(), NULL, 0, NULL, 0, filename, MAX_PATH, ext, MAX_PATH);
#if 1700 <= _MSC_VER
	return std::move(std::string(filename) + ext);
#else
	return std::string(filename) + ext;
#endif
#else
#error gcc version is not implemented.
#endif
}

//! 指定したパス文字列の拡張子を取得する
std::wstring FilePath::GetExtension(const std::wstring& path) {
#if defined _MSC_VER
	wchar_t ext[MAX_PATH] = L"";
	_wsplitpath_s(path.c_str(), NULL, 0, NULL, 0, NULL, 0, ext, MAX_PATH);
#if 1700 <= _MSC_VER
	return std::move(std::wstring(ext));
#else
	return std::wstring(ext);
#endif
#else
#error gcc version is not implemented.
#endif
}

//! 指定したパス文字列の拡張子を取得する
std::string FilePath::GetExtension(const std::string& path) {
#if defined _MSC_VER
	char ext[MAX_PATH] = "";
	_splitpath_s(path.c_str(), NULL, 0, NULL, 0, NULL, 0, ext, MAX_PATH);
#if 1700 <= _MSC_VER
	return std::move(std::string(ext));
#else
	return std::string(ext);
#endif
#else
#error gcc version is not implemented.
#endif
}


_JUNK_END
