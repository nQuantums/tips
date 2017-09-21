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

// 指定数のワーカースレッドを持つスレッドプールを作成する
bool ThreadPool::Create(size_t thread_count) {
	if(!worker_threads_.empty())
		return false;

	worker_threads_.resize(thread_count);
	running_tasks_.resize(thread_count);
	running_tasks_locks_.resize(thread_count);

	for(size_t i = 0; i < thread_count; i++) {
		WorkerThreadArg* pArg = new WorkerThreadArg();
		pArg->thread_pool_ = this;
		pArg->index_ = i;
		HANDLE handle = reinterpret_cast<HANDLE>(_beginthreadex(NULL, 0, &ThreadPool::ThreadProc, pArg, 0, NULL));
		if(handle == NULL)
			return false;

		worker_threads_[i] = handle;
	}

	return true;
}

// スレッドプールを破棄する、実行中のタスクには終了要求を行い、キュー内の未実行タスクは直接破棄する、スレッドアンセーフ
void ThreadPool::Destroy() {
	if(worker_threads_.empty())
		return;

	// ※以降 Create() が呼び出されるまで、Run() が呼び出されてはいけない

	// キュー内のタスクを破棄する
	{
		ThreadQueue<Task*>& wq = worker_queue_;

		// Pop() によるブロックを全て解除する
		worker_queue_.Quit();

		// 未実行タスクを破棄する
		LockGuard<ThreadQueue<Task*>> lock(&wq);
		for(size_t i = 0, n = wq.Count(); i < n; i++) {
			Task* pTask = wq[i];
			pTask->OnDestroy();
			delete pTask;
		}
		worker_queue_.Clear();
	}

	// 実行中タスクへ終了要求を行う
	for(size_t i = 0, n = worker_threads_.size(); i < n; i++) {
		LockGuard<CriticalSection> lock(&running_tasks_locks_[i]);

		Task* pTask = running_tasks_[i];
		if(pTask != NULL) {
			pTask->RequestStop();
		}
	}

	// スレッドを停止
	LockGuard<CriticalSection> scope_guard(&lock_);
	::WaitForMultipleObjects(static_cast<DWORD>(worker_threads_.size()), &worker_threads_[0], TRUE, INFINITE);
	for(std::vector<HANDLE>::const_iterator iter = worker_threads_.begin(); iter != worker_threads_.end(); ++iter) {
		::CloseHandle(*iter);
	}
	worker_threads_.clear();

	// 変数クリア
	running_tasks_.clear();
	running_tasks_locks_.clear();
	worker_queue_.Destroy();
	worker_queue_.Initialize();
}

// 指定されたタスクをキューへ登録する、登録されたタスクは不要になった際に delete される、スレッドセーフ
void ThreadPool::QueueUserWorkItem(Task* pTask) {
	worker_queue_.Push(pTask);
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
UINT ThreadPool::WorkerThread(int index) {
	SetThreadName(::GetCurrentThreadId(), "ThreadPool");

	CriticalSection& cs = running_tasks_locks_[index];
	LockGuard<CriticalSection> lock(&cs);

	for(;;) {
		// キューからタスクを取得し
		Task* pTask;
		if(!worker_queue_.Pop(pTask)) {
			running_tasks_[index] = NULL;
			break;
		}
		running_tasks_[index] = pTask;
		cs.Unlock();

		// 実行する
		pTask->DoTask();
		pTask->OnTaskEnd();
		pTask->OnDestroy();

		// もう用済みなので破棄する
		cs.Lock();
		running_tasks_[index] = NULL;
		delete pTask;
	}

	return 0;
}
