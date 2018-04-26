#include "StdAfx.h"
#include <process.h>
#include "ThreadPool.h"


//
//	スレッド名設定
//	SetThreadName (https://msdn.microsoft.com/ja-jp/library/xcb2z8hs.aspx)
//
const DWORD MS_VC_EXCEPTION = 0x406D1388;  
#pragma pack(push,8)  
typedef struct tagTHREADNAME_INFO  
{  
    DWORD dwType; // Must be 0x1000.  
    LPCSTR szName; // Pointer to name (in user addr space).  
    DWORD dwThreadID; // Thread ID (-1=caller thread).  
    DWORD dwFlags; // Reserved for future use, must be zero.  
 } THREADNAME_INFO;  
#pragma pack(pop)  
inline void SetThreadName(DWORD dwThreadID, const char* threadName) {  
    THREADNAME_INFO info;  
    info.dwType = 0x1000;  
    info.szName = threadName;  
    info.dwThreadID = dwThreadID;  
    info.dwFlags = 0;  
#pragma warning(push)  
#pragma warning(disable: 6320 6322)  
    __try{  
        RaiseException(MS_VC_EXCEPTION, 0, sizeof(info) / sizeof(ULONG_PTR), (ULONG_PTR*)&info);  
    }  
    __except (EXCEPTION_EXECUTE_HANDLER){  
    }  
#pragma warning(pop)  
} 



ThreadPool::ThreadPool() {

}

ThreadPool::~ThreadPool() {
	Destroy();
}

// 指定数のワーカースレッドを持つスレッドプールを作成する(スレッドアンセーフ)
bool ThreadPool::Create(size_t thread_count) {
	if (!workers_.empty())
		return false;

	workers_.resize(thread_count);

	for (size_t i = 0; i < thread_count; i++) {
		WorkerThreadArg* pArg = new WorkerThreadArg();
		pArg->thread_pool_ = this;
		pArg->index_ = i;
		HANDLE handle = reinterpret_cast<HANDLE>(_beginthreadex(NULL, 0, &ThreadPool::ThreadProc, pArg, 0, NULL));
		if (handle == NULL)
			return false;

		workers_[i] = new Worker(handle);
	}

	return true;
}

// スレッドプールを破棄する(スレッドアンセーフ)、実行中のタスクには終了要求を行い、キュー内の未実行タスクは直接破棄する、
void ThreadPool::Destroy() {
	if (workers_.empty())
		return;

	// ※以降 Create() が呼び出されるまで、Run() が呼び出されてはいけない

	// キュー内のタスクを破棄する
	{
		// まだ実行されていないタスクを一旦退避しタスクキューをクリア、スレッドを終了可能にする
		std::deque<Task*> temp;
		task_queue_.Clear(&temp);

		// Pop() によるブロックを全て解除する
		task_queue_.Quit();

		// 未実行タスクを破棄する
		for (size_t i = 0, n = temp.size(); i < n; i++) {
			Task* pTask = temp[i];
			pTask->Dispose();
			pTask->Finalize();
		}
	}

	// 実行中タスクへ終了要求を行う
	size_t worker_count = workers_.size();
	for (size_t i = 0; i < worker_count; i++) {
		Worker* worker = workers_[i];
		LockGuard<CriticalSection> lock(&worker->cs_);
		Task* pTask = reinterpret_cast<Task*>(::InterlockedExchangePointer((PVOID*)&worker->task_, NULL));
		if (pTask) {
			pTask->RequestStop();
		}
	}

	// スレッドを停止
	LockGuard<CriticalSection> scope_guard(&lock_);
	for (size_t i = 0; i < worker_count; i++) {
		Worker* worker = workers_[i];
		::WaitForSingleObject(worker->thread_, INFINITE);
		::CloseHandle(worker->thread_);
	}

	// 変数クリア
	for (size_t i = 0; i < worker_count; i++) {
		delete workers_[i];
	}
	workers_.clear();
	task_queue_.Destroy();
	task_queue_.Initialize();
}

// 指定されたタスクをキューへ登録する(スレッドセーフ)、登録されたタスクは不要になった際に delete される
bool ThreadPool::QueueTask(Task* pTask) {
	return task_queue_.Push(pTask);
}

UINT ThreadPool::ThreadProc(void* pData) {
	WorkerThreadArg* pArg = reinterpret_cast<WorkerThreadArg*>(pData);
	UINT result = pArg->thread_pool_->WorkerThread(pArg->index_);
	delete pArg;
	return result;
}

//
//	ワーカースレッド処理
//
UINT ThreadPool::WorkerThread(intptr_t index) {
	//SetThreadName(::GetCurrentThreadId(), "ThreadPool");

	Worker* worker = workers_[index];
	CriticalSection& cs = worker->cs_;
	LockGuard<CriticalSection> lock(&cs);

	for (;;) {
		// キューからタスクを取得し
		Task* pTask;
		if (!task_queue_.Pop(pTask)) {
			worker->task_ = NULL;
			break;
		}
		worker->task_ = pTask;
		cs.Unlock();

		// 実行する
		pTask->DoTask();
		pTask->Dispose();

		// そして破棄する
		cs.Lock();
		worker->task_ = NULL;
		pTask->Finalize();
	}

	return 0;
}
