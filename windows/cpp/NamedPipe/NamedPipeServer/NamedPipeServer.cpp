// NamedPipeServer.cpp : コンソール アプリケーションのエントリ ポイントを定義します。
//

#include "stdafx.h"
#include <Windows.h>
#include <iostream>
#include <string>
#include "../../Junk/Thread.h"
#include "../NamedPipeServer/PipeServer.h"


void main() {
	PipeServer ps;

	ps.Start(L"\\\\.\\pipe\\{CF2D7C03-766F-4922-A926-E4BFA6EFE51A}");

	for (;;) {
		std::string line;
		std::getline(std::cin, line);
		if (line == "quit") {
			break;
		}
	}

	ps.Stop();
}
