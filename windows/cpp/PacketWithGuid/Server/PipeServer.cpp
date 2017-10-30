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

	// �w�萔�̃X���b�h���쐬�����O�t���p�C�v�ŃN���C�A���g�Ƃ������s��
	// ���̃X���b�h�������������ł���N���C�A���g���ƂȂ�
	size_t thread_count = 32;
	if (!thread_pool_.Create(thread_count))
		return false;
	for (size_t i = 0; i < thread_count; i++) {
		if (!thread_pool_.QueueTask(new TaskForClient(this))) {
			throw Exception("Failed to QueueTask.");
		}
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


//==============================================================================
//		�N���C�A���g�Ƃ̒ʐM����

void PipeServer::TaskForClient::DoTask() {
	HANDLE hCancelEvent = owner_->request_stop_event_;
	CancelablePipe pipe(hCancelEvent);
	ByteBuffer recvbuf;
	ByteBuffer sendbuf;
	recvbuf.reserve(4096);
	sendbuf.reserve(4096);

	try {
		// ����̖��O�Ńp�C�v�쐬
		pipe.Create(
			owner_->pipe_name_.c_str(),
			PIPE_ACCESS_DUPLEX | FILE_FLAG_OVERLAPPED, PIPE_TYPE_BYTE, PIPE_UNLIMITED_INSTANCES,
			owner_->send_buffer_size_,
			owner_->recv_buffer_size_,
			owner_->default_timeout_,
			owner_->security_attributes_);

		for (;;) {
			try {
				// �ڑ��ҋ@
				pipe.Accept();

				// �N���C�A���g�Ƃ��Ƃ�
				std::vector<std::wstring> texts;
				recvbuf.resize(0);
				sendbuf.resize(0);
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

					position_t position = 0;
					Unpacker up(recvbuf, position);

					// �p�P�b�g��ޖ��̏������s��
					sendbuf.resize(0);
					if (AddTextCmd::IsReadable(up)) {
						AddTextCmd cmd(up, false);
						texts.push_back(std::move(cmd.text));
						//::Sleep(1000);
						AddTextRes::Write(sendbuf, S_OK);
					} else if (GetAllTextsCmd::IsReadable(up)) {
						GetAllTextsCmd cmd(up, false);
						::Sleep(5000);
						GetAllTextsRes::Write(sendbuf, texts);
					}

					// ������Ԃ�
					if (!sendbuf.empty())
						pipe.WriteToBytes(&sendbuf[0], sendbuf.size());
				}
			} catch (PipeException&) {
			} catch (UnpackingException&) {
			}

			// �ؒf���čēx�ڑ��ҋ@�ł���悤�ɂ���
			pipe.Disconnect();

			if (::WaitForSingleObject(hCancelEvent, 0) == WAIT_OBJECT_0)
				break;
		}
	} catch (Exception& ex) {
		std::cout << ex.what() << "\n" << ex.MessageFromHresultA() << std::endl;
	} catch (std::exception& stdex) {
		std::cout << stdex.what() << std::endl;
	}

	pipe.Destroy();
}
