// Client.cpp : �R���\�[�� �A�v���P�[�V�����̃G���g�� �|�C���g���`���܂��B
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

