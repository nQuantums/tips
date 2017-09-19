#pragma once
#ifndef __JUNK_MMFILE_H__
#define __JUNK_MMFILE_H__

#include "JunkConfig.h"
#if defined _WIN32
#include <Windows.h>
#endif

_JUNK_BEGIN

//! メモリマップトファイル
class MMFile {
	friend class MMView;
public:
#if defined __GNUC__
	typedef int Handle; //!< ファイルハンドル型

	//! 無効なファイルハンドルの値
	static _FINLINE Handle IHV() {
		return -1;
	}

#elif defined  _WIN32
	typedef HANDLE Handle; //!< ファイルハンドル型

	//! 無効なファイルハンドルの値
	static _FINLINE Handle IHV() {
		return INVALID_HANDLE_VALUE;
	}
#endif

	//! 状態フラグ
	enum FlagsEnum {
		FlagsCreated = 1 << 0, //!< ファイルが作成されている
		FlagsOpened = 1 << 1, //!< ファイルが開かれている
		FlagsRead = 1 << 2, //!< 読み込みモードで開かれている
		FlagsWrite = 1 << 3, //!< 書き込みモードで開かれている
	};

	//! ファイル作成フラグ
	enum CreateEnum {
		CreateNew = 1, //!< 新しいファイルを作成する、同名のファイルが存在していた場合にはエラーが返る
	};

	//! ファイルオープンフラグ
	enum OpenEnum {
		OpenRead = 1 << 0, //!< 読み込みモードで開く
		OpenWrite = 1 << 1, //!< 書き込みモードで開く
		OpenRW = OpenRead | OpenWrite, //!< 読み書きモードで開く
	};


	MMFile(); //!< コンストラクタ
	~MMFile(); //!< デストラクタ

	ibool Create(const char* pszFile, int64_t size, uint32_t createFlags = 0); //!< サイズを指定してファイルを作成する、ファイルは読み書き込みモードで開かれる
	ibool Open(const char* pszFile, uint32_t openFlags); //!< 指定されたファイルを指定されたモードで開く
	ibool Close(); //!< ファイルを閉じる
	void Rob(MMFile* pMMFile); //!< ハンドルなど内部データを指定されたメモリマップトファイルオブジェクトから奪い取る、奪い取られた方はコンストラクト直後の状態になる

	//! 状態フラグの取得
	_FINLINE uint32_t Flags() const {
		return m_Flags;
	}

	//! 確保したファイルサイズ(bytes)の取得
	_FINLINE int64_t GetSize() const {
		return m_Size;
	}

#if defined __GNUC__
	//! ファイルハンドルの取得
	_FINLINE Handle GetHandle() const {
		return m_hFile;
	}
#elif defined  _WIN32
	//! ファイルハンドルの取得
	_FINLINE Handle GetHandle() const {
		return m_hFile;
	}
#endif

protected:
	uint32_t m_Flags; //!< フラグ
	int64_t m_Size; //!< ファイルサイズ(bytes)
	Handle m_hFile; //!< ファイルディスクリプタ、開かれていない場合は IHV() の値になる
#if defined  _WIN32
	HANDLE m_hMapping; //!< メモリマップトファイルオブジェクト

protected:
	HANDLE CreateMapping(); //!< ファイルマッピングオブジェクトの作成
#endif
};

//! メモリマップトファイルの割り当てられたメモリアドレスを指すビュー
class MMView {
public:
	MMView(); //!< コンストラクタ
	~MMView(); // デストラクタ

	void* Map(MMFile* pMMFile, int64_t position, intptr_t size); //!< メモリを割り当ててポインタを返す、Unmap() を呼び出すまで mmfile.Close() を呼び出してはならない
	ibool Unmap(); //!< 割り当てられたメモリを開放する
	void Rob(MMView* pMMView); //!< ハンドルなど内部データを指定されたビューオブジェクトから奪い取る、奪い取られた方はコンストラクト直後の状態になる

	//! ファイルの先頭からメモリ割り当て位置へのオフセット(bytes)の取得
	_FINLINE int64_t Position() const {
		return m_Position;
	}
	//! メモリ割り当てサイズ(bytes)の取得
	_FINLINE intptr_t Size() const {
		return m_Size;
	}
	//! Position() に対応するメモリアドレスの取得
	_FINLINE void* Ptr() const {
		return m_pPtr;
	}
	//! 実際のメモリ割り当てサイズ(bytes)の取得
	_FINLINE intptr_t MappedSize() const {
		return m_MappedSize;
	}
	//! メモリマップ割り当ての先頭アドレスの取得
	_FINLINE void* MappedPtr() const {
		return m_pMappedPtr;
	}

protected:
	int64_t m_Position; //!< ファイルの先頭からメモリ割り当て位置へのオフセット(bytes)
	intptr_t m_Size; //!< メモリ割り当てサイズ(bytes)
	void* m_pPtr; // m_Position に対応するメモリアドレス
	intptr_t m_MappedSize; //!< メモリ割り当てサイズ(bytes)
	void* m_pMappedPtr; //!< メモリマップ割り当ての先頭アドレス
};

_JUNK_END

#endif
