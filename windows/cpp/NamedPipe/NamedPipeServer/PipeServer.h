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
	void ThreadProc(); //!< �ڑ���t�X���b�h����
	void AddClient(ClientContext* pClient); //!< �w��N���C�A���g���Ǘ����֒ǉ�����
	bool RemoveClient(ClientContext* pClient); //!< �w��N���C�A���g���Ǘ������珜�O����

protected:
	std::wstring m_PipeName; //!< ��t�p�C�v��
	volatile bool m_RequestStop; //!< �T�[�o�[��~�v���t���O
	HANDLE m_hRequestStopEvent; //!< �T�[�o�[��~�v���C�x���g
	std::thread m_AcceptanceThread; //!< �ڑ���t�����X���b�h
	DWORD m_SendBufferSize; //!< �p�C�v���M�o�b�t�@�T�C�Y
	DWORD m_RecvBufferSize; //!< �p�C�v��M�o�b�t�@�T�C�Y
	DWORD m_DefaultTimeout; //!< �p�C�v����^�C���A�E�g(ms)

	std::vector<std::shared_ptr<ClientContext>> m_Clients; //!< �N���C�A���g�����z��
	std::mutex m_ClientsCs; //!< m_Clients �A�N�Z�X�r�������p
};
