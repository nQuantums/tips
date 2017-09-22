// Client.cpp : コンソール アプリケーションのエントリ ポイントを定義します。
//

#include "stdafx.h"
#include <iostream>
#include "../NetClientApi/Packets.h"

using namespace NetClientApi;

int _tmain(int argc, _TCHAR* argv[])
{
	PacketBuffer buffer;

	// とりあえずバッファにコマンド構築
	IsFileExistsCmd::Write(buffer, L"testfile.zip", L"1asfsfasfsaffeerr");
	CopyFileIfExistsCmd::Write(buffer, L"testfile.zip", L"1asfsfasfsaffeerr", L"c:\\work");

	// バッファからコマンド読み込み
	IsFileExistsCmd cmd1;
	CopyFileIfExistsCmd cmd2;

	position_t position = 0;
	while (Unpacker::IsUnpackable(buffer, position)) {
		Unpacker unpacker(buffer, position);

		if (cmd1.Read(unpacker)) {
			std::wcout << L"指定ファイル存在確認コマンド, " << cmd1.file_name << ", " << cmd1.file_hash << std::endl;
		} else if (cmd2.Read(unpacker)) {
			std::wcout << L"指定ファイルが存在したら指定フォルダへコピーコマンド, " << cmd2.file_name << ", " << cmd2.file_hash << ", " << cmd2.folder_path << std::endl;
		}
	}

	return 0;
}

