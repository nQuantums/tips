#include "Thread.h"
#include "Error.h"
#include <new>
#include <map>


#if defined __GNUC__

#include <unistd.h>

#else

#include <process.h>

#endif


_JUNK_BEGIN

//==============================================================================
//		Thread クラス

//! コンストラクタ
Thread::Thread() {
	m_hThread = 0;
	m_UserStartRoutine = 0;
	m_pUserArgs = 0;
}

//! デストラクタ
Thread::~Thread() {
	Close();
}

//! スレッドを開始する
ibool Thread::Start(
	ProcPtr startRoutine, //!< [in] スレッド開始ルーチン
	void* pArg //!< [in] スレッド開始ルーチンに渡す引数
) {
	assert(m_hThread == 0);

	m_UserStartRoutine = startRoutine;
	m_pUserArgs = pArg;

#if defined __GNUC__
	int en = pthread_create(&m_hThread, NULL, StartRoutine, this);
	if(en != 0) {
		Error::SetLastErrorFromErrno(en);
		m_hThread = 0;
		return false;
	}
	return true;
#else
	DWORD tid;
	m_hThread = _beginthreadex(0, 0, StartRoutine, this, 0, (unsigned int*)&tid);
	if (m_hThread == 0) {
		Error::SetLastErrorFromErrno();
		return false;
	}
	return true;
#endif
}

//! スレッドの終了を待ち、リソースを開放する
void Thread::Join() {
#if defined __GNUC__
	if (m_hThread != 0) {
		pthread_join(m_hThread, NULL);
		m_hThread = 0;
	}
#else
	if (m_hThread != 0) {
		::WaitForSingleObject((HANDLE)m_hThread, INFINITE);
		::CloseHandle((HANDLE)m_hThread);
		m_hThread = 0;
	}
#endif
}

//! スレッドの終了を待たずに、リソースを開放する
void Thread::Close() {
#if defined __GNUC__
	if (m_hThread != 0) {
		pthread_detach(m_hThread);
		m_hThread = 0;
	}
#else
	if (m_hThread != 0) {
		::CloseHandle((HANDLE)m_hThread);
		m_hThread = 0;
	}
#endif
}

//! 指定された時間現在のスレッドを停止する
void Thread::Sleep(
	uint32_t msec //!< [in] スリープ時間(msec)
) {
#if defined __GNUC__
	usleep(msec * 1000);
#else
	::Sleep(msec);
#endif
}

#if defined __GNUC__
//! スレッド初期化、終了処理用
void* Thread::StartRoutine(
	void* pThis //! [in] Thread オブジェクトポインタ
) {
	// ユーザー関数呼び出し
	return (void*)((Thread*)pThis)->m_UserStartRoutine(((Thread*)pThis)->m_pUserArgs);
}
#else
unsigned int WINAPI Thread::StartRoutine(
	void* pThis //! [in] Thread オブジェクトポインタ
) {
	// ユーザー関数呼び出し
	return (unsigned int)((Thread*)pThis)->m_UserStartRoutine(((Thread*)pThis)->m_pUserArgs);
}
#endif

//==============================================================================
//		Mutex クラス

//! コンストラクタ
Mutex::Mutex() {
#if defined __GNUC__
	pthread_mutexattr_t attr;
	pthread_mutexattr_init(&attr);
	pthread_mutexattr_settype(&attr, PTHREAD_MUTEX_RECURSIVE);
	pthread_mutex_init(&m_mutex, &attr);
#else
	m_hMutex = ::CreateMutexA(NULL, false, NULL);
#endif
}
//! デストラクタ
Mutex::~Mutex() {
#if defined __GNUC__
	pthread_mutex_destroy(&m_mutex);
#else
	::CloseHandle(m_hMutex);
#endif
}

//! ロックする
void Mutex::Lock() {
#if defined __GNUC__
	pthread_mutex_lock(&m_mutex);
#else
	::WaitForSingleObject(m_hMutex, INFINITE);
#endif
}

//! アンロックする
void Mutex::Unlock() {
#if defined __GNUC__
	pthread_mutex_unlock(&m_mutex);
#else
	::ReleaseMutex(m_hMutex);
#endif
}

//! コンストラクタと同じように内部オブジェクトを初期化する
void Mutex::Initialize() {
	new(this) Mutex();
}

//! デストラクタと同じように内部オブジェクトを破棄する
void Mutex::Destroy() {
	this->~Mutex();
}


//==============================================================================
//		CriticalSection クラス

//! コンストラクタ
CriticalSection::CriticalSection() {
#if defined __GNUC__
	pthread_mutexattr_t attr;
	pthread_mutexattr_init(&attr);
	pthread_mutexattr_settype(&attr, PTHREAD_MUTEX_RECURSIVE);
	pthread_mutex_init(&m_mutex, &attr);
#else
	::InitializeCriticalSection(&m_Cs);
#endif
}
//! デストラクタ
CriticalSection::~CriticalSection() {
#if defined __GNUC__
	pthread_mutex_destroy(&m_mutex);
#else
	::DeleteCriticalSection(&m_Cs);
#endif
}

//! ロックする
void CriticalSection::Lock() {
#if defined __GNUC__
	pthread_mutex_lock(&m_mutex);
#else
	::EnterCriticalSection(&m_Cs);
#endif
}

//! アンロックする
void CriticalSection::Unlock() {
#if defined __GNUC__
	pthread_mutex_unlock(&m_mutex);
#else
	::LeaveCriticalSection(&m_Cs);
#endif
}

//! コンストラクタと同じように内部オブジェクトを初期化する
void CriticalSection::Initialize() {
	new(this) CriticalSection();
}

//! デストラクタと同じように内部オブジェクトを破棄する
void CriticalSection::Destroy() {
	this->~CriticalSection();
}


//==============================================================================
//		Event クラス

//! コンストラクタ
Event::Event() {
#if defined __GNUC__
	pthread_mutexattr_t atr;
	pthread_mutexattr_init(&atr);
	pthread_mutex_init(&m_lock, &atr);
	pthread_cond_init(&m_ready, NULL);
#else
	m_hEvent = ::CreateEventA(NULL, FALSE, FALSE, NULL);
#endif
}
//! デストラクタ
Event::~Event() {
#if defined __GNUC__
	pthread_cond_destroy(&m_ready);
	pthread_mutex_destroy(&m_lock);
#else
	::CloseHandle(m_hEvent);
#endif
}

//! イベントをシグナル状態にする
void Event::Set() {
#if defined __GNUC__
	pthread_mutex_lock(&m_lock);
	pthread_mutex_unlock(&m_lock);
	pthread_cond_signal(&m_ready);
#else
	::SetEvent(m_hEvent);
#endif
}

//! イベントを非シグナル状態にする
void Event::Reset() {
#if defined __GNUC__
	pthread_mutex_unlock(&m_lock);
#else
	::ResetEvent(m_hEvent);
#endif
}

//! イベントがシグナル状態になるのを待つ
ibool Event::Wait(intptr_t timeoutMs) {
#if defined __GNUC__
	pthread_mutex_lock(&m_lock);
	int check_value;
	if (timeoutMs == -1) {
		check_value = pthread_cond_wait(&m_ready, &m_lock);
	} else {
		check_value = pthread_cond_timedwait(&m_ready, &m_lock, &t);
	}
	return check_value == 0;
#else
	return ::WaitForSingleObject(m_hEvent, (DWORD)timeoutMs) == WAIT_OBJECT_0;
#endif
}

//! コンストラクタと同じように内部オブジェクトを初期化する
void Event::Initialize() {
	new(this) Event();
}

//! デストラクタと同じように内部オブジェクトを破棄する
void Event::Destroy() {
	this->~Event();
}


_JUNK_END
