#pragma once
#include <Windows.h>
#include <iostream>
#include <string>
#include <vector>
#include "../../Junk/Thread.h"
#include "../CancelablePipe.h"

//! �p�C�v�ɂ��T�[�o�[
class PipeServer {
public:
	PipeServer();
	~PipeServer();
	bool Start(const wchar_t* pszPipeName, DWORD sendBufferSize = 4096, DWORD recvBufferSize = 4096, DWORD defaultTimeout = 1000);
	void Stop();

protected:
	class ClientContext {
	public:
		ClientContext(PipeServer* pOwner, const CancelablePipe& pipe) {
			m_pOwner = pOwner;
			m_Pipe = pipe;
			m_Thread.Start(&ThreadStart, this);
		}
		~ClientContext() {
			m_Pipe.Destroy();
		}

		void Stop(bool wait) {
		}

	protected:
		static intptr_t ThreadStart(void* pObj);
		void ThreadProc();

	protected:
		PipeServer* m_pOwner;
		CancelablePipe m_Pipe;
		jk::Thread m_Thread;
	};

protected:
	static intptr_t ThreadStart(void* pObj); //!< �ڑ���t�X���b�h�J�n�A�h���X
	void ThreadProc(); //!< �ڑ���t�X���b�h����
	void AddClient(ClientContext* pClient); //!< �w��N���C�A���g���Ǘ����֒ǉ�����
	bool RemoveClient(ClientContext* pClient, bool wait = false); //!< �w��N���C�A���g���Ǘ������珜�O����

protected:
	std::wstring m_PipeName; //!< ��t�p�C�v��
	volatile bool m_RequestStop; //!< �T�[�o�[��~�v���t���O
	jk::Event m_RequestStopEvent; //!< �T�[�o�[��~�v���C�x���g
	jk::Thread m_AcceptanceThread; //!< �ڑ���t�����X���b�h
	DWORD m_SendBufferSize; //!< �p�C�v���M�o�b�t�@�T�C�Y
	DWORD m_RecvBufferSize; //!< �p�C�v��M�o�b�t�@�T�C�Y
	DWORD m_DefaultTimeout; //!< �p�C�v����^�C���A�E�g(ms)

	std::vector<ClientContext*> m_Clients; //!< �N���C�A���g�����z��
	jk::CriticalSection m_ClientsCs; //!< m_Clients �A�N�Z�X�r�������p
};
