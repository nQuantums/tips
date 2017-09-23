#include "stdafx.h"
#include <iostream>
#include <exception>
#include "../Commands.h"
#include "PipeServer.h"


using namespace Packets;


//==============================================================================
//		�p�C�v�ɂ��T�[�o�[

PipeServer::PipeServer() {
	security_attributes_ = NULL;
	request_stop_event_ = ::CreateEventW(NULL, TRUE, FALSE, NULL);
	send_buffer_size_ = 0;
	recv_buffer_size_ = 0;
	default_timeout_ = 0;
}

PipeServer::~PipeServer() {
	Stop();

	// TODO: �S�N���C�A���g�p�X���b�h���I������܂ő҂�

	::CloseHandle(request_stop_event_);
}

bool PipeServer::Start(const wchar_t* pszPipeName, DWORD sendBufferSize, DWORD recvBufferSize, DWORD defaultTimeout, LPSECURITY_ATTRIBUTES pSecurityAttributes) {
	Stop();

	pipe_name_ = pszPipeName;
	send_buffer_size_ = sendBufferSize;
	recv_buffer_size_ = recvBufferSize;
	default_timeout_ = defaultTimeout;
	security_attributes_ = pSecurityAttributes;
	::ResetEvent(request_stop_event_);

	if (!thread_pool_.Create(32))
		return false;

	if (!thread_pool_.QueueTask(new TaskForAccept(this))) {
		throw Exception("Failed to QueueTask.");
	}

	return true;
}

//! �T�[�o�[�����X���b�h���~����A�X���b�h�A���Z�[�t
void PipeServer::Stop() {
	// �܂��T�[�o�[�̐ڑ���t���~������
	::SetEvent(request_stop_event_);
	//::WaitForSingleObject(accept_end_event_, INFINITE);

	// �N���C�A���g�ʐM�X���b�h�̒�~��҂�
	thread_pool_.Destroy();
}

//! �ڑ���t����
void PipeServer::AcceptProc() {
	HANDLE hCancelEvent = request_stop_event_;
	CancelablePipe pipe(hCancelEvent);

	std::wcout << L"Server started." << std::endl;

	try {
		for (;;) {
			try {
				// TODO: �\�P�b�g�Ƃ͈Ⴂ accept() ���ăX���b�h���̂ł͂Ȃ��A�\�ߍő�X���b�h������Ė��O�t���p�C�v����đ҂����Ă����K�v������A�Őڑ��󂯕t�����X���b�h�̂܂܃N���C�A���g�Ə���������

				// ����̖��O�Ńp�C�v�쐬
				pipe.Destroy();
				pipe.Create(pipe_name_.c_str(), PIPE_ACCESS_DUPLEX | FILE_FLAG_OVERLAPPED, PIPE_TYPE_BYTE, PIPE_UNLIMITED_INSTANCES, send_buffer_size_, recv_buffer_size_, default_timeout_, security_attributes_);

				// �ڑ��҂���
				pipe.Accept();

				// �ڑ����ꂽ�̂ŃT�[�o�[�N���C�A���g�ԒʐM�^�X�N�J�n
				//std::wcout << L"Connected." << std::endl;
				TaskForClient* task = new TaskForClient(pipe);
				if (!thread_pool_.QueueTask(task)) {
					continue;
				}

				// �n���h���̏��L�����ڂ����̂ŃN���A
				pipe = CancelablePipe(hCancelEvent);
			} catch (PipeException&) {
			}
		}
	} catch (Exception& ex) {
		std::cout << ex.what() << "\n" << ex.MessageFromHresultA() << std::endl;
	} catch (std::exception& stdex) {
		std::cout << stdex.what() << std::endl;
	}

	// �s�v�ȃn���h����j��
	pipe.Destroy();
}


//==============================================================================
//		�N���C�A���g�ڑ���t����

void PipeServer::TaskForAccept::DoTask() {
	owner_->AcceptProc();
}


//==============================================================================
//		�N���C�A���g�Ƃ̒ʐM����

void PipeServer::TaskForClient::DoTask() {
	CancelablePipe pipe = pipe_;
	ByteBuffer recvbuf;
	ByteBuffer sendbuf;
	std::vector<std::wstring> texts;

	recvbuf.reserve(4096);
	sendbuf.reserve(4096);

	try {
		for (;;) {
			// �܂��p�P�b�g�T�C�Y��ǂݍ���
			pktsize_t packetSize;
			pipe.ReadToBytes(&packetSize, sizeof(packetSize));

			// �p�P�b�g�T�C�Y�������Ȓl�Ȃ�U����������Ȃ�
			Unpacker::VeryfySize(packetSize);

			// �P�p�P�b�g���f�[�^��M
			recvbuf.resize(0);
			ToBuffer(recvbuf, packetSize);
			pipe.ReadToBytes(recvbuf, packetSize);

			// �p�P�b�g���A���p�b�L���O���ĉ�͂���
			position_t position = 0;
			Unpacker up(recvbuf, position);

			// �p�P�b�g��ޖ��̏������s��
			sendbuf.resize(0);
			if (AddTextCmd::IsReadable(up)) {
				AddTextCmd cmd(up);
				texts.push_back(std::move(cmd.text));
				//::Sleep(1000);
				AddTextRes::Write(sendbuf, S_OK);
			} else if (GetAllTextsCmd::IsReadable(up)) {
				GetAllTextsCmd cmd(up);
				//::Sleep(1000);
				GetAllTextsRes::Write(sendbuf, texts);
			}

			// ������Ԃ�
			if (!sendbuf.empty())
				pipe.WriteToBytes(&sendbuf[0], sendbuf.size());
		}

		//std::cout << "Disconnected." << std::endl;
	} catch (Exception& ex) {
		std::cout << ex.what() << "\n" << ex.MessageFromHresultA() << std::endl;
	} catch (std::exception& stdex) {
		std::cout << stdex.what() << std::endl;
	}
}
