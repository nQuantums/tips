#include "stdafx.h"
#include <iostream>
#include <exception>
#include "PipeServer.h"

//==============================================================================
//		�p�C�v�ɂ��T�[�o�[

PipeServer::PipeServer() {
	m_RequestStop = false;
	m_SendBufferSize = 0;
	m_RecvBufferSize = 0;
	m_DefaultTimeout = 0;
}

PipeServer::~PipeServer() {
	Stop();

	// TODO: �S�N���C�A���g�p�X���b�h���I������܂ő҂�
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

//! �T�[�o�[�����X���b�h���~����A�X���b�h�A���Z�[�t
void PipeServer::Stop() {
	// �܂��T�[�o�[�̐ڑ���t���~������
	m_RequestStop = true;
	m_RequestStopEvent.Set();
	m_AcceptanceThread.Join();

	// �S�N���C�A���g�̒ʐM�X���b�h���~����
	std::vector<ClientContext*> clients;

	{
		jk::CriticalSectionLock lock(&m_ClientsCs);
		clients = m_Clients;
	}

	for (size_t i = 0; i < clients.size(); i++) {
		RemoveClient(clients[i], true);
	}
}

//! �ڑ���t�X���b�h�J�n�A�h���X
intptr_t PipeServer::ThreadStart(void* pObj) {
	((PipeServer*)pObj)->ThreadProc();
	return 0;
}

//! �ڑ���t�X���b�h����
void PipeServer::ThreadProc() {
	HANDLE hCancelEvent = m_RequestStopEvent.m_hEvent;
	CancelablePipe pipe(hCancelEvent);

	for (;;) {
		// ����̖��O�Ńp�C�v�쐬
		if (!pipe.Create(m_PipeName.c_str(), PIPE_ACCESS_DUPLEX | FILE_FLAG_OVERLAPPED, PIPE_TYPE_BYTE, PIPE_UNLIMITED_INSTANCES, m_SendBufferSize, m_RecvBufferSize, m_DefaultTimeout, NULL)) {
			std::wcout << L"Failed to create pipe." << std::endl;
			break;
		}

		// �ڑ��҂���
		if (!pipe.Accept()) {
			std::wcout << L"Failed to accept pipe." << std::endl;
			break;
		}

		// �ڑ����ꂽ�̂ŃN���C�A���g�ڑ����ǉ�
		AddClient(new ClientContext(this, pipe));

		// �n���h���̏��L�����ڂ����̂ŃN���A
		pipe = CancelablePipe(hCancelEvent);
	}

	// �s�v�ȃn���h����j��
	pipe.Destroy();
}

//! �w��N���C�A���g���Ǘ����֒ǉ�����
void PipeServer::AddClient(ClientContext* pClient) {
	jk::CriticalSectionLock lock(&m_ClientsCs);
	m_Clients.push_back(pClient);
}

//! �w��N���C�A���g���Ǘ������珜�O����
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
//		�N���C�A���g�Ƃ̒ʐM����

intptr_t PipeServer::ClientContext::ThreadStart(void* pObj) {
	((PipeServer::ClientContext*)pObj)->ThreadProc();
	return 0;
}

//! �N���C�A���g�p�ʐM�X���b�h����
void PipeServer::ClientContext::ThreadProc() {
	CancelablePipe pipe = m_Pipe;
	std::vector<char> buf(4096);
	std::vector<uint8_t> tempBuf;

	for (;;) {
		// �܂��p�P�b�g�T�C�Y��ǂݍ���
		int packetSize;
		if (!pipe.ReadToBytes(&packetSize, 4))
			break;

		// �p�P�b�g�T�C�Y�������Ȓl�Ȃ�U����������Ȃ�
		if (packetSize < 0 || 100 < packetSize) {
			std::wcout << L"Invalid packet size received.: " << packetSize << std::endl;
			break;
		}

		// �p�P�b�g���e��ǂݍ���
		if ((intptr_t)buf.size() < packetSize + 1)
			buf.resize(packetSize + 1);
		if (!pipe.ReadToBytes(&buf[0], packetSize))
			break;

		buf[packetSize + 1] = '\0';

		std::cout << "Received:" << std::endl;
		std::cout << &buf[0] << std::endl;
	}

	// �������g��j��
	delete this;
}
