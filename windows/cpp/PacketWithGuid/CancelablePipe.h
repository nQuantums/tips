#pragma once
#include <Windows.h>
#include "Config.h"


// �p�C�v�֌W��O��{�N���X
class PipeException : public Exception {
public:
	PipeException(char const* const _Message) : Exception(_Message) {}
	PipeException(char const* const _Message, HRESULT hr) : Exception(_Message, hr) {}
};

// �p�C�v�쐬���s��O
class PipeCreateException : public PipeException {
public:
	PipeCreateException(char const* const _Message) : PipeException(_Message) {}
	PipeCreateException(char const* const _Message, HRESULT hr) : PipeException(_Message, hr) {}
};

// ConnectNamedPipe ���s��O
class PipeConnectException : public PipeException {
public:
	PipeConnectException(char const* const _Message) : PipeException(_Message) {}
	PipeConnectException(char const* const _Message, HRESULT hr) : PipeException(_Message, hr) {}
};

// �p�C�v�������ݎ��s��O
class PipeWriteException : public PipeException {
public:
	PipeWriteException(char const* const _Message) : PipeException(_Message) {}
	PipeWriteException(char const* const _Message, HRESULT hr) : PipeException(_Message, hr) {}
};

// �p�C�v�ǂݍ��ݗ�O
class PipeReadException : public PipeException {
public:
	PipeReadException(char const* const _Message) : PipeException(_Message) {}
	PipeReadException(char const* const _Message, HRESULT hr) : PipeException(_Message, hr) {}
};


//! �ҋ@�L�����Z���@�\�t���̃p�C�v
struct CancelablePipe : OVERLAPPED {
	HANDLE hUnownedCancelEvent; //!< �ǂݏ����҂��̃L�����Z���C�x���g�APipeOverlapped �͂��̃n���h���̏��L���������Ȃ��A�g�p�ґ����Ǘ�����K�v������
	HANDLE hPipe; //!< �p�C�v

	__forceinline CancelablePipe() {}
	__forceinline CancelablePipe(HANDLE hCancel) {
		::ZeroMemory(this, sizeof(*this));
		this->hUnownedCancelEvent = hCancel;
		this->hPipe = INVALID_HANDLE_VALUE;
	}

	//! �I�u�W�F�N�g��j��
	void Destroy() {
		if (this->hEvent) {
			::CloseHandle(this->hEvent);
			this->hEvent = NULL;
		}
		if (this->hPipe != INVALID_HANDLE_VALUE) {
			::CloseHandle(this->hPipe);
			this->hPipe = INVALID_HANDLE_VALUE;
		}
	}

	//! �p�C�v�n���h���쐬
	void Create(LPCWSTR lpName, DWORD dwOpenMode, DWORD dwPipeMode, DWORD nMaxInstances, DWORD nOutBufferSize, DWORD nInBufferSize, DWORD nDefaultTimeOut, LPSECURITY_ATTRIBUTES lpSecurityAttributes) {
		HANDLE h = ::CreateNamedPipeW(lpName, dwOpenMode, dwPipeMode, nMaxInstances, nOutBufferSize, nInBufferSize, nDefaultTimeOut, lpSecurityAttributes);
		if (h == INVALID_HANDLE_VALUE)
			throw PipeCreateException("Failed to CreateNamedPipe.");
		this->hPipe = h;
		h = ::CreateEvent(NULL, FALSE, FALSE, NULL);
		if (h == NULL)
			throw Exception("Failed to CreateEvent.");
		this->hEvent = h;
	}

	//! �ڑ��҂���
	void Accept() {
		if (::ConnectNamedPipe(this->hPipe, this))
			return;
		if (::GetLastError() != ERROR_IO_PENDING)
			throw PipeConnectException("Failed to ConnectNamedPipe.");
		switch (::WaitForMultipleObjects(this->HandlesCount(), this->Handles(), FALSE, INFINITE)) {
		case WAIT_OBJECT_0:
			return;
		case WAIT_OBJECT_0 + 1:
			throw Exception("Accept cancelled.");
		default:
			throw Exception("Failed to WaitForMultipleObjects.");
		}
	}

	//! �ڑ���ؒf�A����ɂ��ēx Accept() �Őڑ��Ҏ�\�ɂȂ�
	void Disconnect() {
		::DisconnectNamedPipe(this->hPipe);
	}

	//! �����ƃL�����Z���C�x���g�n���h���z��̎擾
	__forceinline const HANDLE* Handles() const {
		return &this->hEvent;
	}

	//! Handles() ���w���n���h����
	__forceinline const DWORD HandlesCount() const {
		return 2;
	}

	//! �w��o�C�g����������ǂݍ���
	void CancelablePipe::ReadToBytes(void* pBuf, intptr_t size) {
		intptr_t len = 0;
		while (size) {
			DWORD n = 0;
			if (!::ReadFile(this->hPipe, (char*)pBuf + len, (DWORD)size, &n, this)) {
				if (::GetLastError() != ERROR_IO_PENDING) {
					throw PipeReadException("Failed to ReadFile.");
				}

				switch (::WaitForMultipleObjects(this->HandlesCount(), this->Handles(), FALSE, INFINITE)) {
				case WAIT_OBJECT_0:
					if (!::GetOverlappedResult(this->hPipe, this, &n, FALSE)) {
						throw PipeReadException("Failed to GetOverlappedResult.");
					}
					break;
				case WAIT_OBJECT_0 + 1:
					throw PipeReadException("ReadToBytes cancelled.");
				default:
					throw PipeReadException("Failed to WaitForMultipleObjects.");
				}
			}

			size -= n;
			len += n;
		}
	}

	//! �w��o�C�g���������菑������
	void CancelablePipe::WriteToBytes(const void* pBuf, intptr_t size) {
		intptr_t len = 0;
		while (size) {
			DWORD n = 0;
			if (!::WriteFile(this->hPipe, (char*)pBuf + len, (DWORD)size, &n, this)) {
				if (::GetLastError() != ERROR_IO_PENDING) {
					throw PipeReadException("Failed to WriteFile.");
				}

				switch (::WaitForMultipleObjects(this->HandlesCount(), this->Handles(), FALSE, INFINITE)) {
				case WAIT_OBJECT_0:
					if (!::GetOverlappedResult(this->hPipe, this, &n, FALSE)) {
						throw PipeReadException("Failed to GetOverlappedResult.");
					}
					break;
				case WAIT_OBJECT_0 + 1:
					throw PipeReadException("WriteToBytes cancelled.");
				default:
					throw PipeReadException("Failed to WaitForMultipleObjects.");
				}
			}

			size -= n;
			len += n;
		}
	}

	//! �w��o�b�t�@�̏I�[�ȍ~�Ɏw��o�C�g����������ǂݍ���
	void CancelablePipe::ReadToBytes(ByteBuffer& buf, intptr_t size) {
#ifdef _DEBUG
		if (!size)
			return;
#endif
		auto prevSize = buf.size();
		buf.resize(buf.size() + size);
		ReadToBytes(&buf[prevSize], size);
	}

	//! �w��o�b�t�@�S�̂��������菑������
	void CancelablePipe::WriteToBytes(const ByteBuffer& buf, intptr_t size) {
#ifdef _DEBUG
		if (buf.empty())
			return;
#endif
		WriteToBytes(&*buf.begin(), size);
	}
};
