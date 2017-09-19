#pragma once
#ifndef __JUNK_THREAD_H__
#define __JUNK_THREAD_H__

#include "JunkConfig.h"

#if defined __GNUC__

#include <pthread.h>
#include <unistd.h>

#else

#include <Windows.h>

#endif


_JUNK_BEGIN

//! スレッド
class JUNKAPICLASS Thread {
public:
#if defined __GNUC__
	typedef pthread_t Handle; //!< スレッドハンドル型
	typedef pid_t Id; //!< スレッドID型
#else
	typedef uintptr_t Handle; //!< スレッドハンドル型
	typedef DWORD Id; //!< スレッドID型
#endif
	typedef intptr_t(*ProcPtr)(void*); //!< スレッド開始ルーチンポインタ

	Thread(); //!< コンストラクタ
	~Thread(); //!< デストラクタ
	ibool Start(ProcPtr startRoutine, void* pArg); //!< スレッドを開始する
	void Join(); //!< スレッドの終了を待ち、リソースを開放する
	void Close(); //!< スレッドの終了を待たずに、リソースを開放する
	static void Sleep(uint32_t msec); //!< 指定された時間現在のスレッドを停止する

#if defined __GNUC__
									  //! 現在のスレッドIDの取得
	static _FINLINE Id CurrentThreadId() {
		return pthread_self();
	}
#else
									  //! 現在のスレッドIDの取得
	static _FINLINE Id CurrentThreadId() {
		return ::GetCurrentThreadId();
	}
#endif

	//! Start() メソッドでスレッド作成されたかどうかの取得
	_FINLINE ibool IsStarted() {
		return m_hThread != 0;
	}

	//! ハンドルの取得
	_FINLINE Handle GetHandle() {
		return m_hThread;
	}

protected:
#if defined __GNUC__
	static void* StartRoutine(void*); //!< スレッド初期化、終了処理用
#else
	static unsigned int WINAPI StartRoutine(void*); //!< スレッド初期化、終了処理用
#endif

protected:
	Handle m_hThread; //!< スレッドハンドル
	ProcPtr m_UserStartRoutine; //!< Start() メソッドに渡された開始アドレス
	void* m_pUserArgs; //!< Start() メソッドに渡されたコンテキスト
};

//! ミューテックス
class Mutex {
public:
	Mutex(); //!< コンストラクタ
	~Mutex(); // デストラクタ
	void Lock(); //!< ロックする
	void Unlock(); //!< アンロックする
	void Initialize(); //!< コンストラクタと同じように内部オブジェクトを初期化する
	void Destroy(); //!< デストラクタと同じように内部オブジェクトを破棄する

protected:
#if defined __GNUC__
	pthread_mutex_t m_mutex; //!< ミューテックス
#else
	HANDLE m_hMutex; //!< ミューテックスハンドル
#endif
};

//! クリティカルセクション
class JUNKAPICLASS CriticalSection {
public:
	CriticalSection(); //!< コンストラクタ
	~CriticalSection(); // デストラクタ
	void Lock(); //!< ロックする
	void Unlock(); //!< アンロックする
	void Initialize(); //!< コンストラクタと同じように内部オブジェクトを初期化する
	void Destroy(); //!< デストラクタと同じように内部オブジェクトを破棄する

protected:
#if defined __GNUC__
	pthread_mutex_t m_mutex; //!< ミューテックス
#else
	CRITICAL_SECTION m_Cs; //!< クリティカルセクション
#endif
};

//! イベント
class JUNKAPICLASS Event {
public:
#if defined __GNUC__

#else
	typedef HANDLE Handle; //!< ハンドル型
#endif

public:
	Event(); //!< コンストラクタ
	~Event(); // デストラクタ
	void Set(); //!< イベントをシグナル状態にする
	void Reset(); //!< イベントを非シグナル状態にする
	ibool Wait(intptr_t timeoutMs = -1); //!< イベントがシグナル状態になるのを待つ
	void Initialize(); //!< コンストラクタと同じように内部オブジェクトを初期化する
	void Destroy(); //!< デストラクタと同じように内部オブジェクトを破棄する

public:
#if defined __GNUC__
	pthread_cond_t m_ready;
	pthread_mutex_t m_lock;
#else
	HANDLE m_hEvent; //!< イベントハンドル
#endif
};

//! Read-Write Lock パターンのロッククラス、Read は同時に複数実行できる
template<
	class _Sync //!< CriticalSection または Mutex を指定する
>
class ReadWriteLock {
public:
#if defined _MSC_VER
	ReadWriteLock(intptr_t initialCount = 0, intptr_t maxCount = 0x7fffffff) {
		m_nActiveReaders = 0;
		m_nWaitingReaders = 0;
		m_nActiveWriters = 0;
		m_nWaitingWriters = 0;
		m_hBlockedReader = ::CreateSemaphoreW(NULL, (long)initialCount, (long)maxCount, NULL);
		m_hBlockedWriter = ::CreateSemaphoreW(NULL, (long)initialCount, (long)maxCount, NULL);
	}
	~ReadWriteLock() {
		::CloseHandle(m_hBlockedReader);
		::CloseHandle(m_hBlockedWriter);
	}

	void BeginReading() {
		m_Sync.Lock();
		if (m_nActiveWriters > 0 || m_nWaitingWriters > 0) {
			m_nWaitingReaders++;
			m_Sync.Unlock();
			::WaitForSingleObject(m_hBlockedReader, -1);
		} else {
			m_nActiveReaders++;
			m_Sync.Unlock();
		}
	}
	void EndReading() {
		m_Sync.Lock();
		m_nActiveReaders--;
		if (m_nActiveReaders == 0 && m_nWaitingWriters > 0) {
			m_nActiveWriters = 1;
			m_nWaitingWriters--;
			::ReleaseSemaphore(m_hBlockedWriter, 1, 0);
		}
		m_Sync.Unlock();
	}
	void BeginWriting() {
		m_Sync.Lock();
		if (m_nActiveReaders == 0 && m_nActiveWriters == 0) {
			m_nActiveWriters = 1;
			m_Sync.Unlock();
		} else {
			m_nWaitingWriters++;
			m_Sync.Unlock();
			::WaitForSingleObject(m_hBlockedWriter, -1);
		}
	}
	void EndWriting() {
		m_Sync.Lock();
		m_nActiveWriters = 0;
		if (m_nWaitingReaders > 0) {
			while (m_nWaitingReaders > 0) {
				m_nWaitingReaders--;
				m_nActiveReaders++;
				::ReleaseSemaphore(m_hBlockedReader, 1, 0);
			}
		} else if (m_nWaitingWriters > 0) {
			m_nWaitingWriters--;
			::ReleaseSemaphore(m_hBlockedWriter, 1, 0);
		}
		m_Sync.Unlock();
	}

	int GetNumActiveReaders() {
		Lock lb(&m_Sync);
		return m_nActiveReaders;
	}
	int GetNumWaitingReaders() {
		Lock lb(&m_Sync);
		return m_nWaitingReaders;
	}
	int GetNumActiveWriters() {
		Lock lb(&m_Sync);
		return m_nActiveWriters;
	}
	int GetNumWaitingWriters() {
		Lock lb(&m_Sync);
		return m_nWaitingWriters;
	}

protected:
	_Sync m_Sync;
	HANDLE m_hBlockedReader;
	HANDLE m_hBlockedWriter;
	intptr_t m_nActiveReaders;
	intptr_t m_nWaitingReaders;
	intptr_t m_nActiveWriters;
	intptr_t m_nWaitingWriters;
#else
#error gcc version is not implemented.
#endif
};


//! 同期用オブジェクトロック＆アンロックヘルパ
template<
	class _Sync //!< 同期用オブジェクト、Lock() と Unlock() メソッドを実装している必要がある
>
class JUNKAPICLASS Lock {
public:
	//! コンストラクタ、指定された同期オブジェクトをロックする
	Lock(_Sync* p) {
		assert(p);
		pSync = p;
		pSync->Lock();
	}

	//! デストラクタ、コンストラクタでロックされたオブジェクトをアンロックする
	~Lock() {
		if (pSync != NULL)
			pSync->Unlock();
	}

	//! コンストラクタで指定された同期オブジェクトを切り離す、デストラクタでアンロックされなくなる
	void Detach(bool unlock = false) {
		if (pSync != NULL) {
			if (unlock)
				pSync->Unlock();
			pSync = NULL;
		}
	}

protected:
	_Sync* pSync; //!< 同期用オブジェクト、Lock() と Unlock() メソッドを実装している必要がある
};

//! 読み込み処理ロック＆アンロックヘルパ
template<
	class _Sync //!< 同期用オブジェクト、BeginReading() と EndReading() メソッドを実装している必要がある
>
class JUNKAPICLASS LockReading {
public:
	//! コンストラクタ、指定された同期オブジェクトをロックする
	LockReading(_Sync* p) {
		assert(p);
		pSync = p;
		pSync->BeginReading();
	}

	//! デストラクタ、コンストラクタでロックされたオブジェクトをアンロックする
	~LockReading() {
		if (pSync != NULL)
			pSync->EndReading();
	}

	//! コンストラクタで指定された同期オブジェクトを切り離す、デストラクタでアンロックされなくなる
	void Detach(bool unlock = false) {
		if (pSync != NULL) {
			if (unlock)
				pSync->EndReading();
			pSync = NULL;
		}
	}

protected:
	_Sync* pSync; //!< 同期用オブジェクト、BeginReading() と EndReading() メソッドを実装している必要がある
};

//! 書き込み処理ロック＆アンロックヘルパ
template<
	class _Sync //!< 同期用オブジェクト、BeginWriting() と EndWriting() メソッドを実装している必要がある
>
class JUNKAPICLASS LockWriting {
public:
	//! コンストラクタ、指定された同期オブジェクトをロックする
	LockWriting(_Sync* p) {
		assert(p);
		pSync = p;
		pSync->BeginWriting();
	}

	//! デストラクタ、コンストラクタでロックされたオブジェクトをアンロックする
	~LockWriting() {
		if (pSync != NULL)
			pSync->EndWriting();
	}

	//! コンストラクタで指定された同期オブジェクトを切り離す、デストラクタでアンロックされなくなる
	void Detach(bool unlock = false) {
		if (pSync != NULL) {
			if (unlock)
				pSync->EndWriting();
			pSync = NULL;
		}
	}

protected:
	_Sync* pSync; //!< 同期用オブジェクト、BeginWriting() と EndWriting() メソッドを実装している必要がある
};

typedef Lock<Mutex> MutexLock; //!< Mutex 用ロック
typedef Lock<CriticalSection> CriticalSectionLock; //!< Mutex 用ロック

_JUNK_END

#endif
