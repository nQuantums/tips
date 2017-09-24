// Server.cpp : コンソール アプリケーションのエントリ ポイントを定義します。
//

#include "stdafx.h"
#include "PipeServer.h"


int main() {
	PipeServer ps;

	ps.Start(L"\\\\.\\pipe\\{CF2D7C03-766F-4922-A926-E4BFA6EFE51A}", 4096, 4096, 1000, NULL);

	for (;;) {
		std::string line;
		std::getline(std::cin, line);
		if (line == "quit") {
			break;
		}
	}

	ps.Stop();

	return 0;
}

