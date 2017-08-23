// GetOfficePath.cpp : コンソール アプリケーションのエントリ ポイントを定義します。
//

#include "stdafx.h"
#include <Windows.h>
#include <iostream>
#include <string>
#include <algorithm>
#include <cctype>
#include <clocale>

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
		return true;
	}

	void Close() {
		if(hk_ == NULL)
			return;
		::RegCloseKey(hk_);
		hk_ = NULL;
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

// 指定ProgIDからCOMサーバー起動コマンドを取得する
bool GetComServerCommandByProgID(const wchar_t* progId, std::wstring& command) {
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


// Excelインストール先パス名を取得する
bool GetExcelInstalledPath(std::wstring& path) {
	if(!GetComServerCommandByProgID(L"Excel.Application", path))
		return false;

	std::wstring::size_type pos = path.rfind(L"excel.exe");
	if(pos == std::wstring::npos)
		return false;

	path.resize(pos + 9);

	return true;
}

// Wordインストール先パス名を取得する
bool GetWordInstalledPath(std::wstring& path) {
	if(!GetComServerCommandByProgID(L"Word.Application", path))
		return false;

	std::wstring::size_type pos = path.rfind(L"winword.exe");
	if(pos == std::wstring::npos)
		return false;

	path.resize(pos + 11);

	return true;
}


int _tmain(int argc, _TCHAR* argv[])
{
	std::wstring path;

	if(GetExcelInstalledPath(path))
		std::wcout << path << std::endl;

	if(GetWordInstalledPath(path))
		std::wcout << path << std::endl;

	return 0;
}

