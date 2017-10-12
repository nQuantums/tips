// Server.cpp : コンソール アプリケーションのエントリ ポイントを定義します。
//

#include "stdafx.h"
#include "PipeServer.h"
#include "../Security.h"
#include <iostream>


int main() {
	Sid everyone(Sid::Authority::WorldSid, 1, SECURITY_WORLD_RID);
	Sid networkUser(Sid::StringKind::Sid, L"S-1-5-2");
	//Sid networkUser(Sid::StringKind::AccountName, L"NT AUTHORITY\\NETWORK");
	Acl acl({ networkUser , everyone });
	acl.AddAccessDeniedAce(STANDARD_RIGHTS_ALL | SPECIFIC_RIGHTS_ALL, networkUser); // 他PCからの接続を拒否、これを先に AddAccessDeniedAce しておく必要がある
	acl.AddAccessAllowedAce(STANDARD_RIGHTS_ALL | SPECIFIC_RIGHTS_ALL, everyone);

	SecurityDescriptor sd;
	sd.SetDacl(acl);

	SecurityAttributes sa(sd);

	PipeServer ps;
	ps.Start(L"\\\\.\\pipe\\{CF2D7C03-766F-4922-A926-E4BFA6EFE51A}", 4096, 4096, 1000, &sa);

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

