#pragma once
#include "ThreadQueue.h"
#include <vector>
#include <queue>

// スレッドプール、Create() で作って QueueUserWorkItem() でキューに登録し、Destroy() で全スレッドとキューに入っているタスクを破棄する
class ThreadPool {
public:
	// ThreadPool で実行するタスク基本クラス
	struct Task {
		virtual ~Task() {}
		// タスク実行処理
		virtual void DoTask() {}
		// タスク実行完了直後に呼び出される
		virtual void OnTaskEnd() {}
		// タスク delete 直前に呼び出される
		virtual void OnDestroy() {}
		// タスクの停止要求を行う、DoTask()、OnTaskEnd()、OnDestroy() 実行中に呼び出され得る
		virtual void RequestStop() {}
	};

	ThreadPool();
	~ThreadPool();

	// 指定数のワーカースレッドを持つスレッドプールを作成する、スレッドアンセーフ
	bool Create(size_t thread_count);
	// スレッドプールを破棄する、実行中のタスクには終了要求を行い、キュー内の未実行タスクは直接破棄する、スレッドアンセーフ
	void Destroy();
	// 指定されたタスクをキューへ登録する、登録されたタスクは不要になった際に delete される、スレッドセーフ
	void QueueUserWorkItem(Task* pTask);

private:
	struct WorkerThreadArg {
		ThreadPool* thread_pool_;
		int index_;
	};

	static UINT __stdcall ThreadProc(void* pData);
	UINT WorkerThread(int index);

private:
	std::vector<HANDLE> worker_threads_;
	std::vector<Task*> running_tasks_;
	std::vector<CriticalSection> running_tasks_locks_;
	CriticalSection lock_;
	ThreadQueue<Task*> worker_queue_;
};
