#pragma once
#ifndef __JUNK_FILE_H__
#define __JUNK_FILE_H__

#include "JunkConfig.h"
#include <vector>
#include <string>

_JUNK_BEGIN

//! ファイル
class JUNKAPICLASS File {
public:
	//! アクセス指定フラグ
	enum AccessEnum {
		AccessRead = 1 << 0, //!< 読み込み
		AccessWrite = 1 << 1, //!< 書き込み
	};

	//! ファイルオープン指定フラグ
	enum OpenEnum {
		OpenCreate = 1 << 0, //!< ファイルを新規作成する
		OpenNew = 1 << 1, //!< OpenCreate と一緒に指定し、既存ファイルがある場合には失敗するようになる
		OpenAppend = 1 << 2, //!< 追加書込モードでファイルを開く
	};

	struct Info {
		std::string FileName;
		uint32_t Mode;
		uint64_t Size;
		uint64_t CreateTime;
		uint64_t UpdateTime;
		uint64_t AccessTime;
	};

#if defined __GNUC__
	typedef int Handle; //!< ファイルハンドル型
#else
	typedef int Handle; //!< ファイルハンドル型
#endif

	static ibool Delete(const wchar_t* pszFile); //!< 指定されたファイルを削除する
	static ibool Delete(const char* pszFile); //!< 指定されたファイルを削除する
	static ibool Access(const wchar_t* pszFile, uint32_t accessFlags); //!< 指定されたファイルに指定されたアクセス可能か調べる
	static ibool Access(const char* pszFile, uint32_t accessFlags); //!< 指定されたファイルに指定されたアクセス可能か調べる
	static ibool Exists(const wchar_t* pszFile); //!< 指定されたファイルが存在しているか調べる
	static ibool Exists(const char* pszFile); //!< 指定されたファイルが存在しているか調べる
	static ibool Move(const wchar_t* sourceFileName, const wchar_t* destFileName); //!< 指定されたファイルを新しい場所に移動します
	static ibool Move(const char* sourceFileName, const char* destFileName); //!< 指定されたファイルを新しい場所に移動します

#if defined __GNUC__
	static ibool AddFileInfos(const char* pszDir, std::vector<Info>& infos);
#endif

	//! 有効なファイルハンドルかどうか判定する
	static _FINLINE ibool IsValidFileHandle(Handle hFile) {
		return hFile != InvalidHandle();
	}

	//! 無効なファイルハンドル値の取得
	static _FINLINE Handle InvalidHandle() {
		return -1;
	}


	File(); //! コンストラクタ
	~File(); //! デストラクタ

	ibool Open(const wchar_t* pszFile, uint32_t accessFlags, uint32_t openFlags); //!< ファイルを開く
	ibool Open(const char* pszFile, uint32_t accessFlags, uint32_t openFlags); //!< ファイルを開く
	ibool Close(); //!< ファイルを閉じる
	intptr_t Write(const void* pBuf, size_t sizeBytes); //!< ファイルへ書き込む
	intptr_t Write(const std::string& str); //!< ファイルへ書き込む
	intptr_t Read(void* pBuf, size_t sizeBytes); //!< ファイルから読み込む
	ibool Flush(); //!< メモリー上にあるファイルの内容をストレージデバイス上のものと同期させる

	//! ファイルハンドルの取得
	Handle GetHandle() {
		return m_hFile;
	}

	//! 自分に無効ハンドルが設定されているかどうか
	_FINLINE bool IsInvalidHandle() const {
		return m_hFile == InvalidHandle();
	}

protected:
	Handle m_hFile; //!< ファイルハンドル
};

_JUNK_END

#endif
