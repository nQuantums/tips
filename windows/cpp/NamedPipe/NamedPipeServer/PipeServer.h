#pragma once
#include <Windows.h>
#include <iostream>
#include <string>
#include <vector>
#include <thread>
#include <mutex>
#include <memory>
#include "../CancelablePipe.h"

//! �p�C�v�ɂ��T�[�o�[
class PipeServer {
public:
	PipeServer();
	~PipeServer();

	void Start(const wchar_t* pszPipeName, DWORD sendBufferSize = 4096, DWORD recvBufferSize = 4096, DWORD defaultTimeout = 1000, LPSECURITY_ATTRIBUTES pSecurityAttributes = NULL);
	void Stop();

protected:
	class TaskForClient {
	public:
		TaskForClient(PipeServer* pOwner, const CancelablePipe& pipe) {
			m_pOwner = pOwner;
			m_Pipe = pipe;
		}
		~TaskForClient() {
			if (m_Thread.joinable()) {
				if (std::this_thread::get_id() == m_Thread.get_id())
					m_Thread.detach();
				else
					m_Thread.join();
			}
			m_Pipe.Destroy();
		}

		void Start(std::shared_ptr<TaskForClient> clientContext) {
			m_Thread = std::thread([&clientContext] { clientContext->ThreadProc(clientContext); });
		}

		void Stop() {
			if (m_Thread.joinable())
				m_Thread.join();
		}

	protected:
		void ThreadProc(std::shared_ptr<TaskForClient> clientContext);

	protected:
		PipeServer* m_pOwner;
		CancelablePipe m_Pipe;
		std::thread m_Thread;
	};

protected:
	void ThreadProc(); //!< �ڑ���t�X���b�h����
	void AddClient(std::shared_ptr<TaskForClient> clientContext); //!< �w��N���C�A���g���Ǘ����֒ǉ�����
	bool RemoveClient(std::shared_ptr<TaskForClient> clientContext); //!< �w��N���C�A���g���Ǘ������珜�O����

protected:
	std::wstring m_PipeName; //!< ��t�p�C�v��
	LPSECURITY_ATTRIBUTES m_pSecurityAttributes; //!< �쐬����p�C�v�̃Z�L�����e�B�L�q�q
	HANDLE m_hRequestStopEvent; //!< �T�[�o�[��~�v���C�x���g
	std::thread m_AcceptanceThread; //!< �ڑ���t�����X���b�h
	DWORD m_SendBufferSize; //!< �p�C�v���M�o�b�t�@�T�C�Y
	DWORD m_RecvBufferSize; //!< �p�C�v��M�o�b�t�@�T�C�Y
	DWORD m_DefaultTimeout; //!< �p�C�v����^�C���A�E�g(ms)

	std::vector<std::shared_ptr<TaskForClient>> m_Clients; //!< �N���C�A���g�����z��
	std::mutex m_ClientsCs; //!< m_Clients �A�N�Z�X�r�������p
};
