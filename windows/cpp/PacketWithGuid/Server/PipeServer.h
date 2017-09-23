#pragma once
#include <Windows.h>
#include <iostream>
#include <string>
#include <vector>
#include <memory>
#include "../CancelablePipe.h"
#include "../ThreadPool.h"

//! �p�C�v�ɂ��T�[�o�[
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
	std::wstring pipe_name_; //!< ��t�p�C�v��
	LPSECURITY_ATTRIBUTES security_attributes_; //!< �쐬����p�C�v�̃Z�L�����e�B�L�q�q
	HANDLE request_stop_event_; //!< �T�[�o�[��~�v���C�x���g
	DWORD send_buffer_size_; //!< �p�C�v���M�o�b�t�@�T�C�Y
	DWORD recv_buffer_size_; //!< �p�C�v��M�o�b�t�@�T�C�Y
	DWORD default_timeout_; //!< �p�C�v����^�C���A�E�g(ms)

	ThreadPool thread_pool_; //!< �g�p����S�X���b�h���Ǘ�����X���b�h�v�[��
};
