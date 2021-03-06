// MsiTest.cpp : アプリケーションのエントリ ポイントを定義します。
//

#include "stdafx.h"
#include <Windows.h>
#include <Msi.h>
#include <MsiQuery.h>
#include <iostream>
#include <string>

#pragma comment(lib, "Msi.lib")

int main()
{
	std::wstring buf;
	DWORD buflen;
	buf.resize(512);

	MSIHANDLE hMsiDb;
	auto r = ::MsiOpenDatabaseW(LR"(G:\work\installers\LibreOffice_6.0.4_Win_x64.msi)", MSIDBOPEN_READONLY, &hMsiDb);
	if (r == 0) {
		// クエリのビュー作成
		MSIHANDLE hMsiView;
		r = ::MsiDatabaseOpenViewW(hMsiDb, L"SELECT Property, Value FROM Property WHERE Property='ProductName' OR Property='ProductCode' OR Property='ProductVersion'", &hMsiView);
		if (r == 0) {
			MSIHANDLE hMsiRecord;
			r = ::MsiViewGetColumnInfo(hMsiView, MSICOLINFO_NAMES, &hMsiRecord);
			if (r == 0) {
				// 列名表示
				auto n = MsiRecordGetFieldCount(hMsiRecord);
				for (int i = 1; i <= n; i++) {
					if (i != 1) {
						std::wcout << "\t";
					}
					buf.resize(0);
					buf.resize(512);
					buflen = buf.size();
					r = ::MsiRecordGetStringW(hMsiRecord, i, &buf[0], &buflen);
					if (r == 0) {
						buf.resize(buflen);
						std::wcout << buf;
					}
				}
				std::wcout << std::endl;

				// クエリ実行してレコード表示
				r = ::MsiViewExecute(hMsiView, 0);
				if (r == 0) {
					MSIHANDLE hMsiRecordRow;
					while (::MsiViewFetch(hMsiView, &hMsiRecordRow) == 0) {
						for (int i = 1; i <= 2; i++) {
							if (i != 1) {
								std::wcout << "\t";
							}
							buf.resize(0);
							buf.resize(512);
							buflen = buf.size();
							r = ::MsiRecordGetStringW(hMsiRecordRow, i, &buf[0], &buflen);
							if (r == 0) {
								buf.resize(buflen);
								std::wcout << buf;
							}
						}
						std::wcout << std::endl;
						::MsiCloseHandle(hMsiRecordRow);
					}
					// ::MsiViewClose(hMsiView); // これは全行フェッチせずに次の MsiViewExecute が呼ぶパターンで必要になる
					::MsiCloseHandle(hMsiView);
				}
				::MsiCloseHandle(hMsiRecord);
			}
			::MsiCloseHandle(hMsiView);
		}
		::MsiCloseHandle(hMsiDb);
	}
    return 0;
}

