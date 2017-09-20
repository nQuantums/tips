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
	m_pSecurityAttributes = NULL;
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

void PipeServer::Start(const wchar_t* pszPipeName, DWORD sendBufferSize, DWORD recvBufferSize, DWORD defaultTimeout, LPSECURITY_ATTRIBUTES pSecurityAttributes) {
	Stop();

	m_PipeName = pszPipeName;
	m_SendBufferSize = sendBufferSize;
	m_RecvBufferSize = recvBufferSize;
	m_DefaultTimeout = defaultTimeout;
	m_pSecurityAttributes = pSecurityAttributes;

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

	std::wcout << L"Server started." << std::endl;

	for (;;) {
		// ����̖��O�Ńp�C�v�쐬
		if (!pipe.Create(m_PipeName.c_str(), PIPE_ACCESS_DUPLEX | FILE_FLAG_OVERLAPPED, PIPE_TYPE_BYTE, PIPE_UNLIMITED_INSTANCES, m_SendBufferSize, m_RecvBufferSize, m_DefaultTimeout, m_pSecurityAttributes)) {
			std::wcout << L"Failed to create pipe." << std::endl;
			break;
		}

		// �ڑ��҂���
		if (!pipe.Accept()) {
			std::wcout << L"Failed to accept pipe." << std::endl;
			break;
		}

		// �ڑ����ꂽ�̂ŃN���C�A���g�ڑ����ǉ�
		std::wcout << L"Connected." << std::endl;
		auto clientContext = std::make_shared<ClientContext>(this, pipe);
		AddClient(clientContext);

		// �n���h���̏��L�����ڂ����̂ŃN���A
		pipe = CancelablePipe(hCancelEvent);

		// �N���C�A���g�Ƃ̒ʐM�X���b�h�J�n
		clientContext->Start(clientContext);
	}

	// �s�v�ȃn���h����j��
	pipe.Destroy();
}

//! �w��N���C�A���g���Ǘ����֒ǉ�����
void PipeServer::AddClient(std::shared_ptr<ClientContext> clientContext) {
	std::lock_guard<std::mutex> lock(m_ClientsCs);
	m_Clients.push_back(clientContext);
}

//! �w��N���C�A���g���Ǘ������珜�O����
bool PipeServer::RemoveClient(std::shared_ptr<ClientContext> clientContext) {
	auto& sync = m_ClientsCs;
	sync.lock();
	auto guard = finally([&sync] { sync.unlock(); });
	auto p = clientContext.get();

	for (size_t i = 0; i < m_Clients.size(); i++) {
		if (m_Clients[i].get() == p) {
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
void PipeServer::ClientContext::ThreadProc(std::shared_ptr<ClientContext> clientContext) {
	CancelablePipe pipe = m_Pipe;
	std::vector<uint8_t> recvbuf;
	std::vector<uint8_t> sendbuf;

	recvbuf.reserve(4096);
	sendbuf.reserve(4096);

	for (;;) {
		// �܂��p�P�b�g�T�C�Y��ǂݍ���
		int32_t packetSize;
		if (!pipe.ReadToBytes(&packetSize, sizeof(packetSize)))
			break;

		// �p�P�b�g�T�C�Y�������Ȓl�Ȃ�U����������Ȃ�
		if (packetSize < 0 || 100 < packetSize) {
			std::wcout << L"Invalid packet size received.: " << packetSize << std::endl;
			break;
		}

		// �p�P�b�g���e��ǂݍ���
		recvbuf.resize(0);
		if (!pipe.ReadToBytes(recvbuf, packetSize))
			break;
		recvbuf.push_back(0);

		std::cout << "Received:" << std::endl;
		std::cout << "\t" << &recvbuf[0] << std::endl;

		// ������Ԃ�
		auto response = "OK";
		packetSize = (int)strlen(response);
		sendbuf.resize(0);
		sendbuf.insert(sendbuf.end(), (char*)&packetSize, (char*)&packetSize + sizeof(packetSize));
		sendbuf.insert(sendbuf.end(), response, response + packetSize);
		if (!pipe.WriteToBytes(&sendbuf[0], sendbuf.size()))
			break;
	}

	std::cout << "Disconnected." << std::endl;

	// �������Ǘ��Ώۂ����菜��
	auto owner = m_pOwner;
	if (owner)
		owner->RemoveClient(clientContext);
}
