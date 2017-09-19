#pragma once
#include <Windows.h>
#include <iostream>
#include <string>
#include <vector>
#include <thread>
#include <mutex>
#include <memory>
#include "../CancelablePipe.h"

//! パイプによるサーバー
class PipeServer {
public:
	PipeServer();
	~PipeServer();

	void Start(const wchar_t* pszPipeName, DWORD sendBufferSize = 4096, DWORD recvBufferSize = 4096, DWORD defaultTimeout = 1000);
	void Stop();

protected:
	class ClientContext {
	public:
		ClientContext(PipeServer* pOwner, const CancelablePipe& pipe) {
			m_pOwner = pOwner;
			m_Pipe = pipe;
			m_Thread = std::thread([this] { this->ThreadProc(std::shared_ptr<ClientContext>(this)); });
		}
		~ClientContext() {
			Stop();
			m_Pipe.Destroy();
		}

		void Stop() {
			if (m_Thread.joinable())
				m_Thread.join();
		}

	protected:
		void ThreadProc(std::shared_ptr<ClientContext>&& clientContext);

	protected:
		PipeServer* m_pOwner;
		CancelablePipe m_Pipe;
		std::thread m_Thread;
	};

protected:
	void ThreadProc(); //!< 接続受付スレッド処理
	void AddClient(ClientContext* pClient); //!< 指定クライアントを管理下へ追加する
	bool RemoveClient(ClientContext* pClient); //!< 指定クライアントを管理下から除外する

protected:
	std::wstring m_PipeName; //!< 受付パイプ名
	volatile bool m_RequestStop; //!< サーバー停止要求フラグ
	HANDLE m_hRequestStopEvent; //!< サーバー停止要求イベント
	std::thread m_AcceptanceThread; //!< 接続受付処理スレッド
	DWORD m_SendBufferSize; //!< パイプ送信バッファサイズ
	DWORD m_RecvBufferSize; //!< パイプ受信バッファサイズ
	DWORD m_DefaultTimeout; //!< パイプ既定タイムアウト(ms)

	std::vector<std::shared_ptr<ClientContext>> m_Clients; //!< クライアント処理配列
	std::mutex m_ClientsCs; //!< m_Clients アクセス排他処理用
};
