// Client.cpp : コンソール アプリケーションのエントリ ポイントを定義します。
//

#include "stdafx.h"
#include "../NetClientApi/Packets.h"

using namespace NetClientApi;

int _tmain(int argc, _TCHAR* argv[])
{
	PacketBuffer buffer;

	IsFileExistsCmd::Write(buffer, L"testfile.zip", L"1asfsfasfsaffeerr");


	return 0;
}

