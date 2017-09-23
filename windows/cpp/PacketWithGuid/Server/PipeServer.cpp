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

	if (!thread_pool_.Create(32))
		return false;

	if (!thread_pool_.QueueTask(new TaskForAccept(this))) {
		throw Exception("Failed to QueueTask.");
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

//! 接続受付処理
void PipeServer::AcceptProc() {
	HANDLE hCancelEvent = request_stop_event_;
	CancelablePipe pipe(hCancelEvent);

	std::wcout << L"Server started." << std::endl;

	try {
		for (;;) {
			try {
				// TODO: ソケットとは違い accept() してスレッド作るのではなく、予め最大スレッド数作って名前付きパイプ作って待たせておく必要がある、で接続受け付けたスレッドのままクライアントと処理をする

				// 所定の名前でパイプ作成
				pipe.Destroy();
				pipe.Create(pipe_name_.c_str(), PIPE_ACCESS_DUPLEX | FILE_FLAG_OVERLAPPED, PIPE_TYPE_BYTE, PIPE_UNLIMITED_INSTANCES, send_buffer_size_, recv_buffer_size_, default_timeout_, security_attributes_);

				// 接続待ち受け
				pipe.Accept();

				// 接続されたのでサーバークライアント間通信タスク開始
				//std::wcout << L"Connected." << std::endl;
				TaskForClient* task = new TaskForClient(pipe);
				if (!thread_pool_.QueueTask(task)) {
					continue;
				}

				// ハンドルの所有権が移ったのでクリア
				pipe = CancelablePipe(hCancelEvent);
			} catch (PipeException&) {
			}
		}
	} catch (Exception& ex) {
		std::cout << ex.what() << "\n" << ex.MessageFromHresultA() << std::endl;
	} catch (std::exception& stdex) {
		std::cout << stdex.what() << std::endl;
	}

	// 不要なハンドルを破棄
	pipe.Destroy();
}


//==============================================================================
//		クライアント接続受付処理

void PipeServer::TaskForAccept::DoTask() {
	owner_->AcceptProc();
}


//==============================================================================
//		クライアントとの通信処理

void PipeServer::TaskForClient::DoTask() {
	CancelablePipe pipe = pipe_;
	ByteBuffer recvbuf;
	ByteBuffer sendbuf;
	std::vector<std::wstring> texts;

	recvbuf.reserve(4096);
	sendbuf.reserve(4096);

	try {
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

			// パケットをアンパッキングして解析する
			position_t position = 0;
			Unpacker up(recvbuf, position);

			// パケット種類毎の処理を行う
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

			// 応答を返す
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
