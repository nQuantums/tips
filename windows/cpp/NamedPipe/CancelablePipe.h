#pragma once
#include <Windows.h>
#include <iostream>

//! 待機キャンセル機能付きのパイプ
struct CancelablePipe : OVERLAPPED {
	HANDLE hUnownedCancelEvent; //!< 読み書き待ちのキャンセルイベント、PipeOverlapped はこのハンドルの所有権を持たない、使用者側が管理する必要がある
	HANDLE hPipe; //!< パイプ

	__forceinline CancelablePipe() {}
	__forceinline CancelablePipe(HANDLE hCancel) {
		::ZeroMemory(this, sizeof(*this));
		this->hUnownedCancelEvent = hCancel;
		this->hPipe = INVALID_HANDLE_VALUE;
	}

	//! オブジェクトを破棄
	void Destroy() {
		if (this->hEvent)
			::CloseHandle(this->hEvent);
		if (this->hPipe != INVALID_HANDLE_VALUE)
			::CloseHandle(this->hPipe);
	}

	//! パイプハンドル作成
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

	//! 接続待ち受け
	bool Accept() {
		if (::ConnectNamedPipe(this->hPipe, this))
			return true;
		if (::GetLastError() != ERROR_IO_PENDING)
			return false;
		if (::WaitForMultipleObjects(2, this->Handles(), FALSE, INFINITE) == WAIT_OBJECT_0)
			return true;
		return false;
	}

	//! 完了とキャンセルイベントハンドル配列の取得
	__forceinline const HANDLE* Handles() const {
		return &this->hEvent;
	}

	//! 指定バイト数きっちり読み込む
	bool CancelablePipe::ReadToBytes(void* pBuf, intptr_t size) {
		intptr_t len = 0;
		while (size) {
			DWORD n = 0;
			if (!::ReadFile(this->hPipe, (char*)pBuf + len, (DWORD)size, &n, this)) {
				if (::GetLastError() != ERROR_IO_PENDING) {
					std::wcout << L"Failed to ReadFile." << std::endl;
					return false;
				}

				DWORD r = ::WaitForMultipleObjects(2, this->Handles(), FALSE, INFINITE);
				if (r == WAIT_OBJECT_0) {
					if (!::GetOverlappedResult(this->hPipe, this, &n, FALSE)) {
						std::wcout << L"Failed to GetOverlappedResult." << std::endl;
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

	//! 指定バイト数きっちり書き込む
	bool CancelablePipe::WriteToBytes(const void* pBuf, intptr_t size) {
		intptr_t len = 0;
		while (size) {
			DWORD n = 0;
			if (!::WriteFile(this->hPipe, (char*)pBuf + len, (DWORD)size, &n, this)) {
				if (::GetLastError() != ERROR_IO_PENDING) {
					std::wcout << L"Failed to WriteFile." << std::endl;
					return false;
				}

				DWORD r = ::WaitForMultipleObjects(2, this->Handles(), FALSE, INFINITE);
				if (r == WAIT_OBJECT_0) {
					if (!::GetOverlappedResult(this->hPipe, this, &n, FALSE)) {
						std::wcout << L"Failed to GetOverlappedResult." << std::endl;
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
};
