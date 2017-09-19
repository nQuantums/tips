#pragma once
#include "Sync.h"
#include <deque>

// スレッド間でアイテムを流すためのキュー
template<typename T>
class ThreadQueue {
public:
	// コンストラクタ、最大要素数を指定して初期化する
	ThreadQueue() : semaphore_(0, 0x7fffffff) {
	}

	size_t Count() {
		return queue_.size();
	}

	const T& operator[](int index) {
		return queue_[index];
	}

	// キューにアイテムを追加する、既に最大要素数に達しているなら失敗する
	bool Push(const T& value) {
		{
			LockGuard<CriticalSection> lock(&critical_section_);
			if(queue_.size() == 0x3fffffff)
				return false;
			queue_.push_back(value);
		}

		semaphore_.Unlock(1); // アイテムを１つ追加したのでアンロック
		return true;
	}

	// キューからアイテムを１つ取り出す、キューにアイテムが追加されるまでブロックされる、処理を終了する際はブロックが解除され false が返る
	bool Pop(T& value) {
		semaphore_.Lock(); // アイテムを１つ消費するのでロック

		LockGuard<CriticalSection> lock(&critical_section_);
		if(queue_.empty())
			return false;

		value = queue_.front();
		queue_.pop_front();
		return true;
	}

	// キューをクリアする
	void Clear() {
		LockGuard<CriticalSection> lock(&critical_section_);
		queue_.clear();
	}

	// 終了のため全ての Pop() でのブロックを解除する
	void Quit() {
		semaphore_.Unlock(0x3fffffff); // アイテムを１つ追加したのでアンロック
	}

	// キューへの排他処理開始
	void Lock() {
		critical_section_.Lock();
	}

	// キューへの排他処理終了
	void Unlock() {
		critical_section_.Unlock();
	}

	void Initialize() {
		new(this) ThreadQueue();
	}
	void Destroy() {
		this->~ThreadQueue();
	}

protected:
	std::deque<T> queue_;
	Semaphore semaphore_;
	CriticalSection critical_section_;
};
