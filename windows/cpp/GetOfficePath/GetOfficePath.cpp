// GetOfficePath.cpp : コンソール アプリケーションのエントリ ポイントを定義します。
//

#include "stdafx.h"
#include <Windows.h>
#include <iostream>
#include <string>
#include <sstream>
#include <algorithm>
#include <cctype>
#include <clocale>
#include <Shlwapi.h>

#pragma comment(lib, "shlwapi.lib")


struct RegHandle {
	HKEY hk_;

	RegHandle() {
		hk_ = NULL;
	}
	RegHandle(HKEY hk) {
		hk_ = hk;
	}
	~RegHandle() {
		if(hk_ == NULL)
			return;
		::RegCloseKey(hk_);
	}

	HKEY* operator&() {
		return &hk_;
	}
	operator HKEY() const {
		return hk_;
	}

	bool OpenW(HKEY hKey, LPCWSTR subKey) {
		Close();
		if(::RegOpenKeyW(hKey, subKey, &hk_) != ERROR_SUCCESS)
			return false;
		//if(::RegOpenKeyExW(hKey, subKey, 0, KEY_ALL_ACCESS | KEY_WOW64_64KEY, &hk_) != ERROR_SUCCESS)
		//	return false;
		return true;
	}

	void Close() {
		if(hk_ == NULL)
			return;
		::RegCloseKey(hk_);
		hk_ = NULL;
	}

	bool GetString(const wchar_t* value_name, std::wstring& value) const {
		DWORD size, type;
		if(::RegQueryValueExW(hk_, value_name, NULL, &type, NULL, &size) != ERROR_SUCCESS)
			return false;
		if(type != REG_SZ)
			return false;
		value.resize(((size - 1) / sizeof(wchar_t)));
		if(::RegQueryValueExW(hk_, value_name, NULL, &type, (LPBYTE)&value[0], &size) != ERROR_SUCCESS)
			return false;
		return true;
	}

	bool GetString(std::wstring& value) const {
		DWORD size, type;
		if(::RegQueryValueExW(hk_, NULL, NULL, &type, NULL, &size) != ERROR_SUCCESS)
			return false;
		if(type != REG_SZ)
			return false;
		value.resize(((size - 1) / sizeof(wchar_t)));
		if(::RegQueryValueExW(hk_, NULL, NULL, &type, (LPBYTE)&value[0], &size) != ERROR_SUCCESS)
			return false;
		return true;
	}
};

// 既知のOfficeバージョンの新しい方から最初に見つかった有効なインストールパスを取得する
static bool GetOfficeApplicationPath(const wchar_t* app_name, const wchar_t* exe_name, std::wstring& path) {
	const wchar_t* versions[] = {
		L"7.0", // Office 97
		L"8.0", // Office 98
		L"9.0", // Office 2000
		L"10.0", // Office XP
		L"11.0", // Office 2003
		L"12.0", // Office 2007
		L"14.0", // Office 2010
		L"15.0", // Office 2013
		L"16.0", // Office 2016
	};

	wchar_t buf[MAX_PATH];
	std::wstringstream wss;

	for(int i = (sizeof(versions) / sizeof(versions[0])) - 1; i != -1; i--) {
		RegHandle keyPath;

		wss.str(L"");
		wss.clear(std::stringstream::goodbit);
		wss << L"SOFTWARE\\Microsoft\\Office\\" << versions[i] << L"\\" << app_name << L"\\InstallRoot";
		if(keyPath.OpenW(HKEY_LOCAL_MACHINE, wss.str().c_str())) {
			if(keyPath.GetString(L"Path", path)) {
				::PathCombineW(buf, path.c_str(), exe_name);
				if(::PathFileExistsW(buf)) {
					path = buf;
					return true;
				}
			}
		}

		wss.str(L"");
		wss.clear(std::stringstream::goodbit);
		wss << L"SOFTWARE\\WOW6432Node\\Microsoft\\Office\\" << versions[i] << L"\\" << app_name << L"\\InstallRoot";
		if(keyPath.OpenW(HKEY_LOCAL_MACHINE, wss.str().c_str())) {
			if(keyPath.GetString(L"Path", path)) {
				::PathCombineW(buf, path.c_str(), exe_name);
				if(::PathFileExistsW(buf)) {
					path = buf;
					return true;
				}
			}
		}
	}

	return false;
}


// 指定ProgIDからCOMサーバー起動コマンドを取得する
static bool GetComServerCommandByProgID(const wchar_t* progId, std::wstring& command) {
	RegHandle keyClsid, keyLocalServer32;
	if(!keyClsid.OpenW(HKEY_CLASSES_ROOT, (progId + std::wstring(L"\\CLSID")).c_str()))
		return false;

	std::wstring strClsid;
	if(!keyClsid.GetString(strClsid))
		return false;

	std::wstring subkey;
	subkey = L"CLSID\\";
	subkey += strClsid;
	subkey += L"\\LocalServer32";
	if(!keyLocalServer32.OpenW(HKEY_CLASSES_ROOT, subkey.c_str())) {
		subkey = L"WOW6432Node\\" + subkey;
		if(!keyLocalServer32.OpenW(HKEY_CLASSES_ROOT, subkey.c_str()))
			return false;
	}

	if(!keyLocalServer32.GetString(command))
		return false;

	for(std::wstring::iterator it = command.begin(); it != command.end(); ++it)
		*it = std::tolower(*it);

	return true;
}

#define OFFICE_PROGID_NUMBER_MAX 20 // 適当に決めたバージョン番号最大値
#define OFFICE_PROGID_NUMBER_MIN 1

// 指定ProgIDに降順でバージョン番号を付与し最初にCOMサーバー起動コマンドを取得できたものを返す
static bool GetComServerCommandByProgIDWithVersion(const wchar_t* progId, std::wstring& command) {
	std::wstringstream wss;

	// 新しい方から使えるものを調べる
	int i;
	for(i = OFFICE_PROGID_NUMBER_MAX; OFFICE_PROGID_NUMBER_MIN <= i; i--) {
		wss.str(L"");
		wss.clear(std::stringstream::goodbit);
		wss << progId << L"." << i;
		if(GetComServerCommandByProgID(wss.str().c_str(), command))
			break;
	}
	// 見つからなかったらだめもとで最後にインストールされたものを試す
	if(i < OFFICE_PROGID_NUMBER_MIN) {
		if(!GetComServerCommandByProgID(progId, command))
			return false;
	}
	std::wcout << wss.str() << std::endl;

	return true;
}

// Excelインストール先パス名を取得する
static bool GetExcelInstalledPath(std::wstring& path) {
	if(!GetComServerCommandByProgIDWithVersion(L"Excel.Application", path))
		return false;

	std::wstring::size_type pos = path.rfind(L"excel.exe");
	if(pos == std::wstring::npos)
		return false;

	path.resize(pos + 9);

	return true;
}

// Wordインストール先パス名を取得する
bool GetWordInstalledPath(std::wstring& path) {
	if(!GetComServerCommandByProgIDWithVersion(L"Word.Application", path))
		return false;

	std::wstring::size_type pos = path.rfind(L"winword.exe");
	if(pos == std::wstring::npos)
		return false;

	path.resize(pos + 11);

	return true;
}

// PowerPointインストール先パス名を取得する
bool GetPowerPointInstalledPath(std::wstring& path) {
	if(!GetComServerCommandByProgIDWithVersion(L"Powerpoint.Application", path))
		return false;

	std::wstring::size_type pos = path.rfind(L"powerpnt.exe");
	if(pos == std::wstring::npos)
		return false;

	path.resize(pos + 12);

	return true;
}


int _tmain(int argc, _TCHAR* argv[])
{
	std::wstring path;

	if(GetOfficeApplicationPath(L"Excel", L"excel.exe", path)) {
		std::wcout << path << std::endl;
	}
	if(GetOfficeApplicationPath(L"Word", L"winword.exe", path)) {
		std::wcout << path << std::endl;
	}
	if(GetOfficeApplicationPath(L"PowerPoint", L"powerpnt.exe", path)) {
		std::wcout << path << std::endl;
	}

	//if(GetExcelInstalledPath(path))
	//	std::wcout << path << std::endl;

	//if(GetWordInstalledPath(path))
	//	std::wcout << path << std::endl;

	//if(GetPowerPointInstalledPath(path))
	//	std::wcout << path << std::endl;

	return 0;
}

