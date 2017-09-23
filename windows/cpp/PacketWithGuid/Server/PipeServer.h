#pragma once
#include <Windows.h>
#include <iostream>
#include <string>
#include <vector>
#include <memory>
#include "../CancelablePipe.h"
#include "../ThreadPool.h"

//! パイプによるサーバー
class PipeServer {
public:
	PipeServer();
	~PipeServer();

	bool Start(const wchar_t* pszPipeName, DWORD sendBufferSize = 4096, DWORD recvBufferSize = 4096, DWORD defaultTimeout = 1000, LPSECURITY_ATTRIBUTES pSecurityAttributes = NULL);
	void Stop();

protected:
	friend class TaskForClient;

	class TaskForClient : public ThreadPool::Task {
	public:
		TaskForClient(PipeServer* owner) : owner_(owner) {}

		void Finalize() {
			delete this;
		}
		void DoTask();

	protected:
		PipeServer* owner_;
	};

protected:
	std::wstring pipe_name_; //!< 受付パイプ名
	LPSECURITY_ATTRIBUTES security_attributes_; //!< 作成するパイプのセキュリティ記述子
	HANDLE request_stop_event_; //!< サーバー停止要求イベント
	DWORD send_buffer_size_; //!< パイプ送信バッファサイズ
	DWORD recv_buffer_size_; //!< パイプ受信バッファサイズ
	DWORD default_timeout_; //!< パイプ既定タイムアウト(ms)

	ThreadPool thread_pool_; //!< 使用する全スレッドを管理するスレッドプール
};
