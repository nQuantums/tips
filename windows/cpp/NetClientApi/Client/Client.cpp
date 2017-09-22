// Client.cpp : コンソール アプリケーションのエントリ ポイントを定義します。
//

#include "stdafx.h"
#include <iostream>
#include "../NetClientApi/Commands.h"

using namespace NetClientApi;

int _tmain(int argc, _TCHAR* argv[])
{
	PacketBuffer buffer;

	// とりあえずバッファにコマンド構築
	IsFileExistsCmd::Write(buffer, L"testfile.zip", L"1asfsfasfsaffeerr");
	CopyFileCmd::Write(buffer, L"testfile.zip", L"1asfsfasfsaffeerr", L"c:\\work");

	// バッファからコマンド読み込み
	IsFileExistsCmd cmd1;
	CopyFileCmd cmd2;

	position_t position = 0;
	while (Unpacker::IsUnpackable(buffer, position)) {
		Unpacker unpacker(buffer, position);

		if (cmd1.Read(unpacker)) {
			std::wcout << L"IsFileExistsCmd, " << cmd1.file_name << ", " << cmd1.file_hash << std::endl;
		} else if (cmd2.Read(unpacker)) {
			std::wcout << L"CopyFileIfExistsCmd, " << cmd2.file_name << ", " << cmd2.file_hash << ", " << cmd2.folder_path << std::endl;
		}
	}

	return 0;
}

