#pragma once
#include <Windows.h>
#include <vector>

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
		if (this->hEvent)
			::CloseHandle(this->hEvent);
		if (this->hPipe != INVALID_HANDLE_VALUE)
			::CloseHandle(this->hPipe);
	}

	//! �p�C�v�n���h���쐬
	bool Create(LPCWSTR lpName, DWORD dwOpenMode, DWORD dwPipeMode, DWORD nMaxInstances, DWORD nOutBufferSize, DWORD nInBufferSize, DWORD nDefaultTimeOut, LPSECURITY_ATTRIBUTES lpSecurityAttributes) {
		HANDLE h = ::CreateNamedPipeW(lpName, dwOpenMode, dwPipeMode, nMaxInstances, nOutBufferSize, nInBufferSize, nDefaultTimeOut, lpSecurityAttributes);
		if (h == INVALID_HANDLE_VALUE)
			return false;
		this->hPipe = h;
		h = ::CreateEvent(NULL, FALSE, FALSE, NULL);
		if (h == NULL)
			return false;
		this->hEvent = h;
		return true;
	}

	//! �ڑ��҂���
	bool Accept() {
		if (::ConnectNamedPipe(this->hPipe, this))
			return true;
		if (::GetLastError() != ERROR_IO_PENDING)
			return false;
		if (::WaitForMultipleObjects(this->HandlesCount(), this->Handles(), FALSE, INFINITE) == WAIT_OBJECT_0)
			return true;
		return false;
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
	bool CancelablePipe::ReadToBytes(void* pBuf, intptr_t size) {
		intptr_t len = 0;
		while (size) {
			DWORD n = 0;
			if (!::ReadFile(this->hPipe, (char*)pBuf + len, (DWORD)size, &n, this)) {
				if (::GetLastError() != ERROR_IO_PENDING) {
					return false;
				}

				DWORD r = ::WaitForMultipleObjects(this->HandlesCount(), this->Handles(), FALSE, INFINITE);
				if (r == WAIT_OBJECT_0) {
					if (!::GetOverlappedResult(this->hPipe, this, &n, FALSE)) {
						return false;
					}
				} else {
					return false;
				}
			}

			size -= n;
			len += n;
		}
		return true;
	}

	//! �w��o�C�g���������菑������
	bool CancelablePipe::WriteToBytes(const void* pBuf, intptr_t size) {
		intptr_t len = 0;
		while (size) {
			DWORD n = 0;
			if (!::WriteFile(this->hPipe, (char*)pBuf + len, (DWORD)size, &n, this)) {
				if (::GetLastError() != ERROR_IO_PENDING) {
					return false;
				}

				DWORD r = ::WaitForMultipleObjects(this->HandlesCount(), this->Handles(), FALSE, INFINITE);
				if (r == WAIT_OBJECT_0) {
					if (!::GetOverlappedResult(this->hPipe, this, &n, FALSE)) {
						return false;
					}
				} else {
					return false;
				}
			}

			size -= n;
			len += n;
		}
		return true;
	}

	//! �w��o�b�t�@�̏I�[�ȍ~�Ɏw��o�C�g����������ǂݍ���
	bool CancelablePipe::ReadToBytes(std::vector<uint8_t>& buf, intptr_t size) {
#ifdef _DEBUG
		if (!size)
			return true;
#endif
		auto prevSize = buf.size();
		buf.resize(buf.size() + size);
		return ReadToBytes(&buf[prevSize], size);
	}

	//! �w��o�b�t�@�S�̂��������菑������
	bool CancelablePipe::WriteToBytes(const std::vector<uint8_t>& buf, intptr_t size) {
#ifdef _DEBUG
		if (buf.empty())
			return true;
#endif
		return WriteToBytes(&*buf.begin(), size);
	}
};
