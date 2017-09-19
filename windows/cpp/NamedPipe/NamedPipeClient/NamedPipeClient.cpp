// NamedPipeClient.cpp : コンソール アプリケーションのエントリ ポイントを定義します。
//

#include "stdafx.h"
#include <Windows.h>
#include <iostream>


int main()
{
	auto hFile = CreateFileW(L"\\\\.\\pipe\\{1151AABD-648D-4BB5-9585-F4106A2F6917}",
		GENERIC_WRITE | GENERIC_READ,
		0,
		NULL,
		OPEN_EXISTING,
		0,
		NULL);
    return 0;
}

