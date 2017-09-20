// NamedPipeClient.cpp : コンソール アプリケーションのエントリ ポイントを定義します。
//

#include "stdafx.h"
#include <Windows.h>
#include <iostream>
#include <string>
#include <thread>
#include <vector>
#include "../CancelablePipe.h"

int main() {
	HANDLE hPipe = ::CreateFileW(
		L"\\\\.\\pipe\\{CF2D7C03-766F-4922-A926-E4BFA6EFE51A}",   // pipe name 
		GENERIC_READ |  // read and write access 
		GENERIC_WRITE,
		0,              // no sharing 
		NULL,           // default security attributes
		OPEN_EXISTING,  // opens existing pipe 
		0,              // default attributes 
		NULL);          // no template file 

	CancelablePipe pipe(NULL);
	pipe.hPipe = hPipe;

	std::vector<uint8_t> recvbuf;
	std::vector<uint8_t> sendbuf;

	recvbuf.reserve(4096);
	sendbuf.reserve(4096);

	for (;;) {
		std::string line;
		std::getline(std::cin, line);
		if (line == "quit") {
			break;
		}

		// コマンドを送信する
		int32_t packetSize = (uint32_t)line.size();
		sendbuf.resize(0);
		sendbuf.insert(sendbuf.end(), (char*)&packetSize, (char*)&packetSize + sizeof(packetSize));
		sendbuf.insert(sendbuf.end(), line.begin(), line.end());
		if (!pipe.WriteToBytes(&sendbuf[0], sendbuf.size()))
			break;

		// 応答を受信する
		// まずパケットサイズを読み込む
		if (!pipe.ReadToBytes(&packetSize, sizeof(packetSize)))
			break;

		// パケットサイズが無茶な値なら攻撃かもしれない
		if (packetSize < 0 || 100 < packetSize) {
			std::wcout << L"Invalid packet size received.: " << packetSize << std::endl;
			break;
		}

		// パケット内容を読み込む
		recvbuf.resize(0);
		if (!pipe.ReadToBytes(recvbuf, packetSize))
			break;
		recvbuf.push_back(0);

		std::cout << "Received:" << std::endl;
		std::cout << "\t" << &recvbuf[0] << std::endl;
	}

	return 0;
}

