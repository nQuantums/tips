#include "stdafx.h"
#include <iostream>
#include <exception>
#include "../Commands.h"
#include "PipeServer.h"


using namespace Packets;


//==============================================================================
//		パイプによるサーバー

PipeServer::PipeServer() {
	security_attributes_ = NULL;
	request_stop_event_ = ::CreateEventW(NULL, TRUE, FALSE, NULL);
	send_buffer_size_ = 0;
	recv_buffer_size_ = 0;
	default_timeout_ = 0;
}

PipeServer::~PipeServer() {
	Stop();

	// TODO: 全クライアント用スレッドが終了するまで待つ

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

	// 指定数のスレッドを作成し名前付きパイプでクライアントとやり取りを行う
	// このスレッド数が同時処理できるクライアント数となる
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

//! サーバー処理スレッドを停止する、スレッドアンセーフ
void PipeServer::Stop() {
	// まずサーバーの接続受付を停止させる
	::SetEvent(request_stop_event_);
	//::WaitForSingleObject(accept_end_event_, INFINITE);

	// クライアント通信スレッドの停止を待つ
	thread_pool_.Destroy();
}


//==============================================================================
//		クライアントとの通信処理

void PipeServer::TaskForClient::DoTask() {
	HANDLE hCancelEvent = owner_->request_stop_event_;
	CancelablePipe pipe(hCancelEvent);
	ByteBuffer recvbuf;
	ByteBuffer sendbuf;
	recvbuf.reserve(4096);
	sendbuf.reserve(4096);

	try {
		// 所定の名前でパイプ作成
		pipe.Create(
			owner_->pipe_name_.c_str(),
			PIPE_ACCESS_DUPLEX | FILE_FLAG_OVERLAPPED, PIPE_TYPE_BYTE, PIPE_UNLIMITED_INSTANCES,
			owner_->send_buffer_size_,
			owner_->recv_buffer_size_,
			owner_->default_timeout_,
			owner_->security_attributes_);

		for (;;) {
			try {
				// 接続待機
				pipe.Accept();

				// クライアントとやりとり
				std::vector<std::wstring> texts;
				recvbuf.resize(0);
				sendbuf.resize(0);
				for (;;) {
					// まずパケットサイズを読み込む
					pktsize_t packetSize;
					pipe.ReadToBytes(&packetSize, sizeof(packetSize));

					// パケットサイズが無茶な値なら攻撃かもしれない
					Unpacker::VeryfySize(packetSize);

					// １パケット分データ受信
					recvbuf.resize(0);
					ToBuffer(recvbuf, packetSize);
					pipe.ReadToBytes(recvbuf, packetSize);

					position_t position = 0;
					Unpacker up(recvbuf, position);

					// パケット種類毎の処理を行う
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

					// 応答を返す
					if (!sendbuf.empty())
						pipe.WriteToBytes(&sendbuf[0], sendbuf.size());
				}
			} catch (PipeException&) {
			} catch (UnpackingException&) {
			}

			// 切断して再度接続待機できるようにする
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
