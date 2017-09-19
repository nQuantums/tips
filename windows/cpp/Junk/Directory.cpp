#include "Directory.h"
#include "Error.h"
#include "FilePath.h"

#if defined __GNUC__

#include <unistd.h>
#include <linux/limits.h>

#elif defined  _WIN32

#include <Windows.h>

#endif


_JUNK_BEGIN

//! カレントディレクトリを取得する
ibool Directory::GetCurrent(
	std::wstring& curDir //<! [out] カレントディレクトリパス名が返る
) {
#if defined _MSC_VER
	wchar_t buf[MAX_PATH];
	if (!::GetCurrentDirectoryW(sizeof(buf), buf)) {
		Error::SetLastErrorFromWinErr();
		return false;
	}
	curDir = buf;
	return true;
#else
#error gcc version is not implemented.
#endif
}

//! カレントディレクトリを取得する
ibool Directory::GetCurrent(
	std::string& curDir //<! [out] カレントディレクトリパス名が返る
) {
#if defined __GNUC__
	char buf[PATH_MAX];
	if (getcwd(buf, PATH_MAX) == NULL) {
		Error::SetLastErrorFromErrno();
		return false;
	}
	curDir = buf;
	return true;
#elif defined  _WIN32
	char buf[MAX_PATH];
	if (!::GetCurrentDirectoryA(sizeof(buf), buf)) {
		Error::SetLastErrorFromWinErr();
		return false;
	}
	curDir = buf;
	return true;
#endif
}

//!< 指定されたディレクトリが存在しているか調べる
ibool Directory::Exists(
	const wchar_t* pszDir // [in] ディレクトリパス名
) {
#if defined __GNUC__
#error gcc version is not implemented.
#elif defined  _WIN32
	DWORD dwAttrib = ::GetFileAttributesW(pszDir);
	if(dwAttrib == INVALID_FILE_ATTRIBUTES) {
		Error::SetLastErrorFromWinErr();
		return false;
	}
	return (dwAttrib != INVALID_FILE_ATTRIBUTES &&  (dwAttrib & FILE_ATTRIBUTE_DIRECTORY));
#endif
}

//!< 指定されたディレクトリが存在しているか調べる
ibool Directory::Exists(
	const char* pszDir // [in] ディレクトリパス名
) {
#if defined __GNUC__
#error gcc version is not implemented.
#elif defined  _WIN32
	DWORD dwAttrib = ::GetFileAttributesA(pszDir);
	if(dwAttrib == INVALID_FILE_ATTRIBUTES) {
		Error::SetLastErrorFromWinErr();
		return false;
	}
	return (dwAttrib != INVALID_FILE_ATTRIBUTES &&  (dwAttrib & FILE_ATTRIBUTE_DIRECTORY));
#endif
}

//! 指定されたディレクトリを作成する
ibool Directory::Create(
	const wchar_t* pszDir // [in] ディレクトリパス名
) {
#if defined __GNUC__
#error gcc version is not implemented.
#elif defined  _WIN32
	if(!::CreateDirectoryW(pszDir, NULL)) {
		Error::SetLastErrorFromWinErr();
		return false;
	}
	return true;
#endif
}


//! 指定されたディレクトリを作成する
ibool Directory::Create(
	const char* pszDir // [in] ディレクトリパス名
) {
#if defined __GNUC__
#error gcc version is not implemented.
#elif defined  _WIN32
	if(!::CreateDirectoryA(pszDir, NULL)) {
		Error::SetLastErrorFromWinErr();
		return false;
	}
	return true;
#endif
}

//! カレントディレクトリを取得する
std::wstring Directory::GetCurrentW() {
	std::wstring s;
	GetCurrent(s);
#if 1700 <= _MSC_VER
	return std::move(s);
#else
	return s;
#endif
}

//! カレントディレクトリを取得する
std::string Directory::GetCurrentA() {
	std::string s;
	GetCurrent(s);
#if 1700 <= _MSC_VER
	return std::move(s);
#else
	return s;
#endif
}

//! 実行中EXEの置かれているディレクトリパス名を取得する
std::wstring Directory::GetExeDirectoryW() {
#if defined _MSC_VER
	wchar_t path[MAX_PATH] = L"";
	::GetModuleFileNameW(NULL, path, MAX_PATH);
#if 1700 <= _MSC_VER
	return std::move(FilePath::GetDirectoryName(path));
#else
	return FilePath::GetDirectoryName(path);
#endif
#else
#error gcc version is not implemented.
#endif
}

//! 実行中EXEの置かれているディレクトリパス名を取得する
std::string Directory::GetExeDirectoryA() {
#if defined _MSC_VER
	char path[MAX_PATH] = "";
	::GetModuleFileNameA(NULL, path, MAX_PATH);
#if 1700 <= _MSC_VER
	return std::move(FilePath::GetDirectoryName(path));
#else
	return FilePath::GetDirectoryName(path);
#endif
#else
#error gcc version is not implemented.
#endif
}

_JUNK_END
