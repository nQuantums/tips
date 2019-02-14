// RegSnapshot.cpp : このファイルには 'main' 関数が含まれています。プログラム実行の開始と終了がそこで行われます。
//

#include "pch.h"
#include <Windows.h>
#include <iostream>
#include <vector>
#include <string>

std::wstring subkeyName;
std::wstring valueName;
std::vector<uint8_t> valueData;

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

		if (valueName.size() < cchMaxValueName) {
			valueName.resize(cchMaxValueName);
		}

		if (valueData.size() < cbMaxValueData) {
			valueData.resize(cbMaxValueData);
		}

		for (DWORD i = 0; ; i++) {
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

			valueName[cchValueName] = 0;
	
			std::wcout << valueName << std::endl;
		}
	}

	if (0 < cSubKeys) {
		if (0 < cchMaxSubKeyName) {
			cchMaxSubKeyName++;
		}

		if (subkeyName.size() < cchMaxSubKeyName) {
			subkeyName.resize(cchMaxSubKeyName);
		}

		for (DWORD i = 0; ; i++) {
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
			subkeyName[cchSubKeyName] = 0;

			std::wcout << subkeyName << std::endl;
		
			HKEY hRegSubKey;
			nErrNo = ::RegOpenKeyExW(hKey, subkeyName.data(), 0, KEY_QUERY_VALUE | KEY_ENUMERATE_SUB_KEYS, &hRegSubKey);
			if (ERROR_SUCCESS != nErrNo) {
				continue;
			}
			EnumerateRegKeys(hRegSubKey);
			::RegCloseKey(hRegSubKey);
		}
	}
}


int main() {
	EnumerateRegKeys(HKEY_LOCAL_MACHINE);
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
