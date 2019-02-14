// RegSnapshot.cpp : このファイルには 'main' 関数が含まれています。プログラム実行の開始と終了がそこで行われます。
//

#include "pch.h"
#include <Windows.h>
#include <iostream>
#include <string>
#include <locale>
#include <codecvt>
#include <fstream>
#include <vector>
#include <string>

std::wstring subkeyName;
std::wstring valueName;
std::vector<uint8_t> valueData;
std::wstring valueSz;
std::ofstream* pOutputFile;

std::string utf16_to_utf8(const std::wstring& str) {
	static_assert(sizeof(wchar_t) == 2, "this function is windows only");
	const int len = ::WideCharToMultiByte(CP_UTF8, 0, str.c_str(), -1, nullptr, 0, nullptr, nullptr);
	std::string re(len * 2, '\0');
	if (!::WideCharToMultiByte(CP_UTF8, 0, str.c_str(), -1, &re[0], len, nullptr, nullptr)) {
		const auto er = ::GetLastError();
		throw std::system_error(std::error_code(er, std::system_category()), "WideCharToMultiByte:(" + std::to_string(er) + ')');
	}
	const std::size_t real_len = std::char_traits<char>::length(re.c_str());
	re.resize(real_len);
	re.shrink_to_fit();
	return re;
}

void EnumerateRegKeys(HKEY hKey) {
	DWORD cSubKeys, cchMaxSubKeyName, cValues, cchMaxValueName, cbMaxValueData;

	auto nErrNo = ::RegQueryInfoKeyW(
		hKey,
		NULL,
		NULL,
		NULL,
		&cSubKeys,
		&cchMaxSubKeyName,  // in TCHARs *not* incl. NULL char
		NULL,
		&cValues,
		&cchMaxValueName,   // in TCHARs *not* incl. NULL char
		&cbMaxValueData,
		NULL,
		NULL
	);
	if (ERROR_SUCCESS != nErrNo) {
		return;
	}

	if (cValues != 0) {
		if (0 < cchMaxValueName) {
			cchMaxValueName++;
		}

		for (DWORD i = 0; ; i++) {
			valueName.resize(cchMaxValueName);
			valueData.resize(cbMaxValueData);

			DWORD cchValueName, nValueType, cbValueData;
			cchValueName = (DWORD)valueName.size();
			cbValueData = (DWORD)valueData.size();
			nErrNo = ::RegEnumValueW(
				hKey,
				i,
				(wchar_t*)valueName.data(),
				&cchValueName,   // in TCHARs; in *including* and out *excluding* NULL char
				NULL,
				&nValueType,
				(uint8_t*)valueData.data(),
				&cbValueData);
			if (ERROR_NO_MORE_ITEMS == nErrNo) {
				break;
			}
			if (ERROR_SUCCESS != nErrNo) {
				// TODO: process/protocol issue in some way, do not silently ignore it (at least in Debug builds)
				continue;
			}

			valueName.resize(cchValueName);

			if (nValueType == REG_SZ && (lstrcmpW(valueName.c_str(), L"DisplayName") == 0 || lstrcmpW(valueName.c_str(), L"DisplayVersion") == 0 || lstrcmpW(valueName.c_str(), L"Publisher") == 0)) {
				auto strSize = (cbValueData + 1) / 2;
				valueSz.resize(0);
				valueSz.resize(strSize);
				memcpy_s((void*)valueSz.data(), cbValueData, valueData.data(), cbValueData);
				*pOutputFile << "	- " << utf16_to_utf8(valueName) << " : " << utf16_to_utf8(valueSz) << std::endl;
			} else if (nValueType == REG_DWORD && lstrcmpW(valueName.c_str(), L"Language") == 0) {
				auto value = *(DWORD*)valueData.data();
				*pOutputFile << "	- " << utf16_to_utf8(valueName) << " : " << value << std::endl;
			}
		}
	}

	if (0 < cSubKeys) {
		if (0 < cchMaxSubKeyName) {
			cchMaxSubKeyName++;
		}

		for (DWORD i = 0; ; i++) {
			subkeyName.resize(cchMaxSubKeyName);

			DWORD cchSubKeyName;
			cchSubKeyName = (DWORD)subkeyName.size();
			nErrNo = ::RegEnumKeyExW(
				hKey, i,
				(wchar_t*)subkeyName.data(),
				&cchSubKeyName,  // in TCHARs; in *including* and out *excluding* NULL char
				NULL,
				NULL,
				NULL,
				NULL);
			if (ERROR_NO_MORE_ITEMS == nErrNo) {
				break;
			}
			if (ERROR_SUCCESS != nErrNo) {
				// TODO: process/protocol issue in some way, do not silently ignore it (at least in Debug builds)
				continue;
			}
			subkeyName.resize(cchSubKeyName);

			*pOutputFile << "# " << utf16_to_utf8(subkeyName) << std::endl;
		
			HKEY hRegSubKey;
			nErrNo = ::RegOpenKeyExW(hKey, subkeyName.data(), 0, KEY_QUERY_VALUE | KEY_ENUMERATE_SUB_KEYS | KEY_WOW64_64KEY, &hRegSubKey);
			if (ERROR_SUCCESS != nErrNo) {
				continue;
			}
			EnumerateRegKeys(hRegSubKey);
			::RegCloseKey(hRegSubKey);
		}
	}
}


int main() {
	//std::locale::global(std::locale(""));
	//std::wcout.imbue(std::locale("Japanese", LC_COLLATE));
	//std::wcout.imbue(std::locale("Japanese", LC_CTYPE));

	std::ofstream ofs("output.md");
	pOutputFile = &ofs;

	HKEY hRegSubKey;
	auto result = ::RegOpenKeyExW(HKEY_LOCAL_MACHINE, L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall", 0, KEY_QUERY_VALUE | KEY_ENUMERATE_SUB_KEYS | KEY_WOW64_64KEY, &hRegSubKey);
	if (result == ERROR_SUCCESS) {
		EnumerateRegKeys(hRegSubKey);
	}

	result = ::RegOpenKeyExW(HKEY_LOCAL_MACHINE, L"SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall", 0, KEY_QUERY_VALUE | KEY_ENUMERATE_SUB_KEYS | KEY_WOW64_64KEY, &hRegSubKey);
	if (result == ERROR_SUCCESS) {
		EnumerateRegKeys(hRegSubKey);
	}

	ofs.close();
}

// プログラムの実行: Ctrl + F5 または [デバッグ] > [デバッグなしで開始] メニュー
// プログラムのデバッグ: F5 または [デバッグ] > [デバッグの開始] メニュー

// 作業を開始するためのヒント: 
//    1. ソリューション エクスプローラー ウィンドウを使用してファイルを追加/管理します 
//   2. チーム エクスプローラー ウィンドウを使用してソース管理に接続します
//   3. 出力ウィンドウを使用して、ビルド出力とその他のメッセージを表示します
//   4. エラー一覧ウィンドウを使用してエラーを表示します
//   5. [プロジェクト] > [新しい項目の追加] と移動して新しいコード ファイルを作成するか、[プロジェクト] > [既存の項目の追加] と移動して既存のコード ファイルをプロジェクトに追加します
//   6. 後ほどこのプロジェクトを再び開く場合、[ファイル] > [開く] > [プロジェクト] と移動して .sln ファイルを選択します
