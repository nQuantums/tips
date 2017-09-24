#pragma once
#include <Windows.h>
#include "Config.h"


// パイプ関係例外基本クラス
class PipeException : public Exception {
public:
	PipeException(char const* const _Message) : Exception(_Message) {}
	PipeException(char const* const _Message, HRESULT hr) : Exception(_Message, hr) {}
};

// パイプ作成失敗例外
class PipeCreateException : public PipeException {
public:
	PipeCreateException(char const* const _Message) : PipeException(_Message) {}
	PipeCreateException(char const* const _Message, HRESULT hr) : PipeException(_Message, hr) {}
};

// ConnectNamedPipe 失敗例外
class PipeConnectException : public PipeException {
public:
	PipeConnectException(char const* const _Message) : PipeException(_Message) {}
	PipeConnectException(char const* const _Message, HRESULT hr) : PipeException(_Message, hr) {}
};

// パイプ読み書き失敗例外
class PipeReadWriteException : public PipeException {
public:
	PipeReadWriteException(char const* const _Message) : PipeException(_Message) {}
	PipeReadWriteException(char const* const _Message, HRESULT hr) : PipeException(_Message, hr) {}
};


//! 待機キャンセル機能付きのパイプ
struct CancelablePipe : OVERLAPPED {
	HANDLE hUnownedCancelEvent; //!< 読み書き待ちのキャンセルイベント、このハンドルの所有権は使用者側にある
	HANDLE hPipe; //!< パイプ

	__forceinline CancelablePipe() {}
	CancelablePipe(HANDLE hCancel) {
		::ZeroMemory(this, sizeof(*this));
		this->hUnownedCancelEvent = hCancel;
		this->hPipe = INVALID_HANDLE_VALUE;
	}

	//! オブジェクトを破棄
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

	//! パイプハンドル作成
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

	//! 既存のパイプ接続
	void Open(LPCWSTR lpName) {
		HANDLE h = ::CreateFileW(
			lpName,               // pipe name 
			GENERIC_READ |        // read and write access 
			GENERIC_WRITE,
			0,                    // no sharing 
			NULL,                 // default security attributes
			OPEN_EXISTING,        // opens existing pipe 
			FILE_FLAG_OVERLAPPED, // 途中キャンセルできるようオーバーラップにする
			NULL);                // no template file 
		if (h == INVALID_HANDLE_VALUE) {
			throw PipeException("Failed to create file.");
		}
		this->hPipe = h;
		h = ::CreateEvent(NULL, FALSE, FALSE, NULL);
		if (h == NULL)
			throw Exception("Failed to CreateEvent.");
		this->hEvent = h;
	}

	//! 接続待ち受け
	void Accept() {
		if (::ConnectNamedPipe(this->hPipe, this))
			return;
		if (::GetLastError() != ERROR_IO_PENDING)
			throw PipeConnectException("Failed to ConnectNamedPipe.");
		switch (::WaitForMultipleObjects(this->HandlesCount(), this->Handles(), FALSE, INFINITE)) {
		case WAIT_OBJECT_0:
			return;
		case WAIT_OBJECT_0 + 1:
			throw PipeException("Accept cancelled.");
		default:
			throw PipeException("Failed to WaitForMultipleObjects.");
		}
	}

	//! 接続を切断、これにより再度 Accept() で接続待受可能になる
	void Disconnect() {
		::DisconnectNamedPipe(this->hPipe);
	}

	//! 完了とキャンセルイベントハンドル配列の取得
	__forceinline const HANDLE* Handles() const {
		return &this->hEvent;
	}

	//! Handles() が指すハンドル数
	__forceinline const DWORD HandlesCount() const {
		return 2;
	}

	//! 指定バイト数きっちり読み込む
	void CancelablePipe::ReadToBytes(void* pBuf, intptr_t size) {
		intptr_t len = 0;
		while (size) {
			DWORD n = 0;
			if (!::ReadFile(this->hPipe, (char*)pBuf + len, (DWORD)size, &n, this)) {
				if (::GetLastError() != ERROR_IO_PENDING) {
					throw PipeReadWriteException("Failed to ReadFile.");
				}

				switch (::WaitForMultipleObjects(this->HandlesCount(), this->Handles(), FALSE, INFINITE)) {
				case WAIT_OBJECT_0:
					if (!::GetOverlappedResult(this->hPipe, this, &n, FALSE)) {
						throw PipeReadWriteException("Failed to GetOverlappedResult.");
					}
					break;
				case WAIT_OBJECT_0 + 1:
					throw PipeReadWriteException("ReadToBytes cancelled.");
				default:
					throw PipeReadWriteException("Failed to WaitForMultipleObjects.");
				}
			}

			size -= n;
			len += n;
		}
	}

	//! 指定バイト数きっちり書き込む
	void CancelablePipe::WriteToBytes(const void* pBuf, intptr_t size) {
		intptr_t len = 0;
		while (size) {
			DWORD n = 0;
			if (!::WriteFile(this->hPipe, (char*)pBuf + len, (DWORD)size, &n, this)) {
				if (::GetLastError() != ERROR_IO_PENDING) {
					throw PipeReadWriteException("Failed to WriteFile.");
				}

				switch (::WaitForMultipleObjects(this->HandlesCount(), this->Handles(), FALSE, INFINITE)) {
				case WAIT_OBJECT_0:
					if (!::GetOverlappedResult(this->hPipe, this, &n, FALSE)) {
						throw PipeReadWriteException("Failed to GetOverlappedResult.");
					}
					break;
				case WAIT_OBJECT_0 + 1:
					throw PipeReadWriteException("WriteToBytes cancelled.");
				default:
					throw PipeReadWriteException("Failed to WaitForMultipleObjects.");
				}
			}

			size -= n;
			len += n;
		}
	}

	//! 指定バッファの終端以降に指定バイト数きっちり読み込む
	void CancelablePipe::ReadToBytes(ByteBuffer& buf, intptr_t size) {
#ifdef _DEBUG
		if (!size)
			return;
#endif
		auto prevSize = buf.size();
		buf.resize(buf.size() + size);
		ReadToBytes(&buf[prevSize], size);
	}

	//! 指定バッファ全体をきっちり書き込む
	void CancelablePipe::WriteToBytes(const ByteBuffer& buf, intptr_t size) {
#ifdef _DEBUG
		if (buf.empty())
			return;
#endif
		WriteToBytes(&*buf.begin(), size);
	}
};
