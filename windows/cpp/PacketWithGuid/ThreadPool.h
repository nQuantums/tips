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
		// タスクの破棄処理、Finalize() 直前のクリティカルセクション外から呼び出される、重い処理してもいい
		virtual void Dispose() {}
		// タスクオブジェクト破棄処理、必要ならメモリ解放のため delete this を呼び出す、クリティカルセクション内から呼び出されるため重い処理はやらないで欲しい
		virtual void Finalize() {}
		// タスク実行処理
		virtual void DoTask() {}
		// タスクの停止要求を行う、DoTask()、OnTaskEnd()、OnDestroy() 実行中に呼び出され得る
		virtual void RequestStop() {}
	};

	ThreadPool();
	~ThreadPool();

	// 指定数のワーカースレッドを持つスレッドプールを作成する(スレッドアンセーフ)
	bool Create(size_t thread_count);
	// スレッドプールを破棄する(スレッドアンセーフ)、実行中のタスクには終了要求を行い、キュー内の未実行タスクは直接破棄する、
	void Destroy();
	// 指定されたタスクをキューへ登録する(スレッドセーフ)、登録されたタスクは不要になった際に delete が呼び出される
	bool QueueTask(Task* pTask);

private:
	struct WorkerThreadArg {
		ThreadPool* thread_pool_;
		intptr_t index_;
	};

	static UINT __stdcall ThreadProc(void* pData);
	UINT WorkerThread(intptr_t index);

private:
	struct Worker {
		HANDLE thread_;
		volatile Task* task_;
		CriticalSection cs_;

		Worker(HANDLE thread) {
			thread_ = thread;
			task_ = NULL;
		}
	};

	std::vector<Worker*> workers_;
	CriticalSection lock_;
	ThreadQueue<Task*> task_queue_;
};
