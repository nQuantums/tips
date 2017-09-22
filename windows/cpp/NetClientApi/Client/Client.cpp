// Client.cpp : �R���\�[�� �A�v���P�[�V�����̃G���g�� �|�C���g���`���܂��B
//

#include "stdafx.h"
#include <iostream>
#include "../NetClientApi/Packets.h"

using namespace NetClientApi;

int _tmain(int argc, _TCHAR* argv[])
{
	PacketBuffer buffer;

	// �Ƃ肠�����o�b�t�@�ɃR�}���h�\�z
	IsFileExistsCmd::Write(buffer, L"testfile.zip", L"1asfsfasfsaffeerr");
	CopyFileIfExistsCmd::Write(buffer, L"testfile.zip", L"1asfsfasfsaffeerr", L"c:\\work");

	// �o�b�t�@����R�}���h�ǂݍ���
	IsFileExistsCmd cmd1;
	CopyFileIfExistsCmd cmd2;

	position_t position = 0;
	while (Unpacker::IsUnpackable(buffer, position)) {
		Unpacker unpacker(buffer, position);

		if (cmd1.Read(unpacker)) {
			std::wcout << L"�w��t�@�C�����݊m�F�R�}���h, " << cmd1.file_name << ", " << cmd1.file_hash << std::endl;
		} else if (cmd2.Read(unpacker)) {
			std::wcout << L"�w��t�@�C�������݂�����w��t�H���_�փR�s�[�R�}���h, " << cmd2.file_name << ", " << cmd2.file_hash << ", " << cmd2.folder_path << std::endl;
		}
	}

	return 0;
}

