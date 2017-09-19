#pragma once
#ifndef __JUNK_THREADLOCALSTORAGE_H__
#define __JUNK_THREADLOCALSTORAGE_H__

#include "JunkConfig.h"

#if defined __GNUC__
#include <pthread.h>
#endif

_JUNK_BEGIN

#if defined __GNUC__ && defined __ANDROID__
//! スレッドローカルストレージ
//! AndroidNDK などで thread_local キーワード使えない時に使用する。
template<class T>
struct JUNKAPICLASS ThreadLocalStorage {
	pthread_key_t TlsKey; //!< TLSのキー

	//! コンストラクタ
	ThreadLocalStorage() {
		pthread_key_create(&TlsKey, &ThreadLocalStorage::TlsDestructor);
	}
	
	//! デストラクタ
	~ThreadLocalStorage() {
		pthread_key_delete(TlsKey);
	}

	//! TLSに保存されているオブジェクトの取得
	T& Get() {
		T* p = (T*)pthread_getspecific(TlsKey);
		if (p == NULL) {
			p = new T();
			pthread_setspecific(TlsKey, p);
		}
		return *p;
	}

	//! TLSのオブジェクト削除コールバック関数
	static void TlsDestructor(void* p) {
		delete (T*)p;
	}
};
#define JUNK_TLS(type) jk::ThreadLocalStorage<type>

#else
//! スレッドローカルストレージ
template<class T>
struct JUNKAPICLASS ThreadLocalStorage {
	T Value; //!< スレッド毎にインスタンス化される値

	//! TLSに保存されているオブジェクトの取得
	_FINLINE T& Get() {
		return this->Value;
	}
};

#if _MSC_VER <= 1700
#define JUNK_TLS(type) __declspec(thread) jk::ThreadLocalStorage<type>
#else
#define JUNK_TLS(type) thread_local jk::ThreadLocalStorage<type>
#endif

#endif

_JUNK_END

#endif
