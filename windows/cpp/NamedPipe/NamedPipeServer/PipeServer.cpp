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
//		�p�C�v�ɂ��T�[�o�[

PipeServer::PipeServer() {
	m_RequestStop = false;
	m_hRequestStopEvent = ::CreateEventW(NULL, TRUE, FALSE, NULL);
	m_SendBufferSize = 0;
	m_RecvBufferSize = 0;
	m_DefaultTimeout = 0;
}

PipeServer::~PipeServer() {
	Stop();

	// TODO: �S�N���C�A���g�p�X���b�h���I������܂ő҂�

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

//! �T�[�o�[�����X���b�h���~����A�X���b�h�A���Z�[�t
void PipeServer::Stop() {
	// ���݊Ǘ����̃N���C�A���g�ʐM���擾����
	std::vector<std::shared_ptr<ClientContext>> clients;
	{
		std::lock_guard<std::mutex> lock(m_ClientsCs);
		clients = m_Clients;
	}

	// �܂��T�[�o�[�̐ڑ���t���~������
	m_RequestStop = true;
	::SetEvent(m_hRequestStopEvent);
	if (m_AcceptanceThread.joinable())
		m_AcceptanceThread.join();

	// �N���C�A���g�ʐM�X���b�h�̒�~��҂�
	for (auto& c : clients) {
		c.get()->Stop();
	}
}

//! �ڑ���t�X���b�h����
void PipeServer::ThreadProc() {
	HANDLE hCancelEvent = m_hRequestStopEvent;
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
	std::lock_guard<std::mutex> lock(m_ClientsCs);
	m_Clients.push_back(std::shared_ptr<ClientContext>(pClient));
}

//! �w��N���C�A���g���Ǘ������珜�O����
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
//		�N���C�A���g�Ƃ̒ʐM����

//! �N���C�A���g�p�ʐM�X���b�h����
void PipeServer::ClientContext::ThreadProc(std::shared_ptr<ClientContext>&& clientContext) {
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

	// �������Ǘ��Ώۂ����菜��
	auto owner = m_pOwner;
	if (owner)
		owner->RemoveClient(this);
}
