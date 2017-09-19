#include "stdafx.h"
#include <iostream>
#include <exception>
#include "PipeServer.h"

//==============================================================================
//		パイプによるサーバー

PipeServer::PipeServer() {
	m_RequestStop = false;
	m_SendBufferSize = 0;
	m_RecvBufferSize = 0;
	m_DefaultTimeout = 0;
}

PipeServer::~PipeServer() {
	Stop();

	// TODO: 全クライアント用スレッドが終了するまで待つ
}

bool PipeServer::Start(const wchar_t* pszPipeName, DWORD sendBufferSize, DWORD recvBufferSize, DWORD defaultTimeout) {
	Stop();

	m_PipeName = pszPipeName;
	m_SendBufferSize = sendBufferSize;
	m_RecvBufferSize = recvBufferSize;
	m_DefaultTimeout = defaultTimeout;

	m_RequestStop = false;
	m_RequestStopEvent.Reset();
	return m_AcceptanceThread.Start(&ThreadStart, this);
}

//! サーバー処理スレッドを停止する、スレッドアンセーフ
void PipeServer::Stop() {
	// まずサーバーの接続受付を停止させる
	m_RequestStop = true;
	m_RequestStopEvent.Set();
	m_AcceptanceThread.Join();

	// 全クライアントの通信スレッドを停止する
	std::vector<ClientContext*> clients;

	{
		jk::CriticalSectionLock lock(&m_ClientsCs);
		clients = m_Clients;
	}

	for (size_t i = 0; i < clients.size(); i++) {
		RemoveClient(clients[i], true);
	}
}

//! 接続受付スレッド開始アドレス
intptr_t PipeServer::ThreadStart(void* pObj) {
	((PipeServer*)pObj)->ThreadProc();
	return 0;
}

//! 接続受付スレッド処理
void PipeServer::ThreadProc() {
	HANDLE hCancelEvent = m_RequestStopEvent.m_hEvent;
	CancelablePipe pipe(hCancelEvent);

	for (;;) {
		// 所定の名前でパイプ作成
		if (!pipe.Create(m_PipeName.c_str(), PIPE_ACCESS_DUPLEX | FILE_FLAG_OVERLAPPED, PIPE_TYPE_BYTE, PIPE_UNLIMITED_INSTANCES, m_SendBufferSize, m_RecvBufferSize, m_DefaultTimeout, NULL)) {
			std::wcout << L"Failed to create pipe." << std::endl;
			break;
		}

		// 接続待ち受け
		if (!pipe.Accept()) {
			std::wcout << L"Failed to accept pipe." << std::endl;
			break;
		}

		// 接続されたのでクライアント接続情報追加
		AddClient(new ClientContext(this, pipe));

		// ハンドルの所有権が移ったのでクリア
		pipe = CancelablePipe(hCancelEvent);
	}

	// 不要なハンドルを破棄
	pipe.Destroy();
}

//! 指定クライアントを管理下へ追加する
void PipeServer::AddClient(ClientContext* pClient) {
	jk::CriticalSectionLock lock(&m_ClientsCs);
	m_Clients.push_back(pClient);
}

//! 指定クライアントを管理下から除外する
bool PipeServer::RemoveClient(ClientContext* pClient, bool wait) {
	jk::CriticalSectionLock lock(&m_ClientsCs);
	for (size_t i = 0; i < m_Clients.size(); i++) {
		if (m_Clients[i] == pClient) {
			m_Clients.erase(m_Clients.begin() + i);
			lock.Detach(true);
			pClient->Stop(wait);
			return true;
		}
	}
	return false;
}


//==============================================================================
//		クライアントとの通信処理

intptr_t PipeServer::ClientContext::ThreadStart(void* pObj) {
	((PipeServer::ClientContext*)pObj)->ThreadProc();
	return 0;
}

//! クライアント用通信スレッド処理
void PipeServer::ClientContext::ThreadProc() {
	CancelablePipe pipe = m_Pipe;
	std::vector<char> buf(4096);
	std::vector<uint8_t> tempBuf;

	for (;;) {
		// まずパケットサイズを読み込む
		int packetSize;
		if (!pipe.ReadToBytes(&packetSize, 4))
			break;

		// パケットサイズが無茶な値なら攻撃かもしれない
		if (packetSize < 0 || 100 < packetSize) {
			std::wcout << L"Invalid packet size received.: " << packetSize << std::endl;
			break;
		}

		// パケット内容を読み込む
		if ((intptr_t)buf.size() < packetSize + 1)
			buf.resize(packetSize + 1);
		if (!pipe.ReadToBytes(&buf[0], packetSize))
			break;

		buf[packetSize + 1] = '\0';

		std::cout << "Received:" << std::endl;
		std::cout << &buf[0] << std::endl;
	}

	// 自分自身を破棄
	delete this;
}
