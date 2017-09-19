#include "stdafx.h"
#include <iostream>
#include <exception>
#include "PipeServer.h"

template<typename Functor>
struct finally_guard {
	finally_guard(Functor f)
		: functor(std::move(f))
		, active(true) {
	}

	finally_guard(finally_guard&& other)
		: functor(std::move(other.functor))
		, active(other.active) {
		other.active = false;
	}

	finally_guard& operator=(finally_guard&&) = delete;

	~finally_guard() {
		if (active)
			functor();
	}

	Functor functor;
	bool active;
};

template<typename F> finally_guard<typename std::decay<F>::type> finally(F&& f) {
	return { std::forward<F>(f) };
}

//==============================================================================
//		パイプによるサーバー

PipeServer::PipeServer() {
	m_RequestStop = false;
	m_hRequestStopEvent = ::CreateEventW(NULL, TRUE, FALSE, NULL);
	m_SendBufferSize = 0;
	m_RecvBufferSize = 0;
	m_DefaultTimeout = 0;
}

PipeServer::~PipeServer() {
	Stop();

	// TODO: 全クライアント用スレッドが終了するまで待つ

	::CloseHandle(m_hRequestStopEvent);
}

void PipeServer::Start(const wchar_t* pszPipeName, DWORD sendBufferSize, DWORD recvBufferSize, DWORD defaultTimeout) {
	Stop();

	m_PipeName = pszPipeName;
	m_SendBufferSize = sendBufferSize;
	m_RecvBufferSize = recvBufferSize;
	m_DefaultTimeout = defaultTimeout;

	m_RequestStop = false;
	::ResetEvent(m_hRequestStopEvent);
	m_AcceptanceThread = std::thread([this] { this->ThreadProc(); });
}

//! サーバー処理スレッドを停止する、スレッドアンセーフ
void PipeServer::Stop() {
	// 現在管理下のクライアント通信を取得する
	std::vector<std::shared_ptr<ClientContext>> clients;
	{
		std::lock_guard<std::mutex> lock(m_ClientsCs);
		clients = m_Clients;
	}

	// まずサーバーの接続受付を停止させる
	m_RequestStop = true;
	::SetEvent(m_hRequestStopEvent);
	if (m_AcceptanceThread.joinable())
		m_AcceptanceThread.join();

	// クライアント通信スレッドの停止を待つ
	for (auto& c : clients) {
		c.get()->Stop();
	}
}

//! 接続受付スレッド処理
void PipeServer::ThreadProc() {
	HANDLE hCancelEvent = m_hRequestStopEvent;
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
	std::lock_guard<std::mutex> lock(m_ClientsCs);
	m_Clients.push_back(std::shared_ptr<ClientContext>(pClient));
}

//! 指定クライアントを管理下から除外する
bool PipeServer::RemoveClient(ClientContext* pClient) {
	auto& sync = m_ClientsCs;
	sync.lock();
	auto guard = finally([&sync] { sync.unlock(); });

	for (size_t i = 0; i < m_Clients.size(); i++) {
		if (m_Clients[i].get() == pClient) {
			m_Clients.erase(m_Clients.begin() + i);
			sync.unlock();
			guard.active = false;
			return true;
		}
	}
	return false;
}


//==============================================================================
//		クライアントとの通信処理

//! クライアント用通信スレッド処理
void PipeServer::ClientContext::ThreadProc(std::shared_ptr<ClientContext>&& clientContext) {
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

	// 自分を管理対象から取り除く
	auto owner = m_pOwner;
	if (owner)
		owner->RemoveClient(this);
}
