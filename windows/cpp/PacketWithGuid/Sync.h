#pragma once
#include <stddef.h>
#include <assert.h>
#include <Windows.h>

// クリティカルセクション
class CriticalSection {
public:
	CriticalSection() {
		Initialize();
	}
	~CriticalSection() {
		Destroy();
	}
	void Lock() {
		::EnterCriticalSection(&cs_);
	}
	void Unlock() {
		::LeaveCriticalSection(&cs_);
	}
	void Initialize() {
		::InitializeCriticalSection(&cs_);
	}
	void Destroy() {
		::DeleteCriticalSection(&cs_);
	}

protected:
	CRITICAL_SECTION cs_;
};

// セマフォ
class Semaphore {
public:
	Semaphore(long initia_lcount, long max_count) {
		Initialize(initia_lcount, max_count);
	}
	~Semaphore() {
		Destroy();
	}
	void Lock() {
		::WaitForSingleObject(handle_, INFINITE);
	}
	long Unlock(long release_count = 1) {
		long prev_count;
		::ReleaseSemaphore(handle_, release_count, &prev_count);
		return prev_count;
	}
	void Initialize(long initia_lcount, long max_count) {
		handle_ = ::CreateSemaphoreW(NULL, initia_lcount, max_count, NULL);
	}
	void Destroy() {
		::CloseHandle(handle_);
	}

protected:
	HANDLE handle_;
};


// 同期用オブジェクトロック＆アンロックヘルパ
template<
	class _Sync // 同期用オブジェクト、Lock() と Unlock() メソッドを実装している必要がある
>
class LockGuard {
public:
	// コンストラクタ、指定された同期オブジェクトをロックする
	LockGuard(_Sync* p) {
		assert(p);
		sync_ = p;
		sync_->Lock();
	}

	// デストラクタ、コンストラクタでロックされたオブジェクトをアンロックする
	~LockGuard() {
		if (sync_ != NULL)
			sync_->Unlock();
	}

	// コンストラクタで指定された同期オブジェクトを切り離す、デストラクタでアンロックされなくなる
	void Detach(bool unlock = false) {
		if (sync_ != NULL) {
			if (unlock)
				sync_->Unlock();
			sync_ = NULL;
		}
	}

protected:
	_Sync* sync_; // 同期用オブジェクト、Lock() と Unlock() メソッドを実装している必要がある
};

