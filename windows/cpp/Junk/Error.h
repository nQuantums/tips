#pragma once
#ifndef __JUNK_ERROR_H__
#define __JUNK_ERROR_H__

#include "JunkConfig.h"
#include <errno.h>
#include <stdarg.h>
#include <string>
#if defined  _WIN32
#include <Windows.h>
#endif

_JUNK_BEGIN

//! エラー情報関係クラス
class JUNKAPICLASS Error {
public:
	static void SetLastError(const char* pszFmt, ...); //!< スレッド毎の最終エラー文字列を設定する
	static void SetLastErrorFromErrno(int en); //!< errno から対応する文字列を取得してスレッド毎のエラー文字列として設定する
	static void SetLastErrorFromErrno(); //!< errno から対応する文字列を取得してスレッド毎のエラー文字列として設定する
#if defined  _WIN32
	static void SetLastErrorFromWinErr(DWORD lastError); //!< Windowsエラーコードから対応する文字列を取得してスレッド毎のエラー文字列として設定する
	static void SetLastErrorFromWinErr(); //!< ::GetLastError() から対応する文字列を取得してスレッド毎のエラー文字列として設定する
#endif
#if defined __GNUC__
	//! LinuxだとSetLastErrorFromErrno()、WindowsだとSetLastErrorFromWinErr()が呼び出される 
	_FINLINE static void SetOsLastError() {
		SetLastErrorFromErrno();
	}
#elif defined  _WIN32
	//! LinuxだとSetLastErrorFromErrno()、WindowsだとSetLastErrorFromWinErr()が呼び出される 
	_FINLINE static void SetOsLastError() {
		SetLastErrorFromWinErr();
	}
#endif



	static const char* GetLastErrorString(); //!< スレッド毎の最終エラー文字列を取得する

	//! Android+NDK だと __thread が使えないため自分で作成
	struct LocalStorage {
		char LastErrorStringBuffer[256]; //!< Error の最終エラー文字列用バッファ
	};
};

//! エラー情報
struct JUNKAPICLASS ErrorInfo {
	ibool _Empty; //!< エラーが空かどうか
	std::string Message; //!< エラーメッセージ

	ErrorInfo() {
		_Empty = true;
	}

	void ClsV(const char* pszClassName, const char* pszFuncName, const char* pszInternalError, const char* pszMessage, va_list args); //!< クラス名、メソッド名、内部エラー情報、メッセージを設定する
	void Cls(const char* pszClassName, const char* pszFuncName, const char* pszInternalError, const char* pszMessage, ...); //!< クラス名、メソッド名、内部エラー情報、メッセージを設定する
	ibool IsEmpty() const; //!< エラー情報が空かどうかの取得
};

#ifdef JNI_H_

#define JUNK_EI_DECL(pre) \
protected: jk::ErrorInfo m_ErrorInfo ## pre; \
	void Ei ## pre ## FncV(const char* pszFuncName, const char* pszInternalError, const char* pszMessage, va_list args); \
	void Ei ## pre ## Fnc(const char* pszFuncName, const char* pszInternalError, const char* pszMessage, ...); \
	void Ei ## pre ## FncLeV(const char* pszFuncName, const char* pszMessage, va_list args); \
	void Ei ## pre ## FncLe(const char* pszFuncName, const char* pszMessage, ...); \
public: \
	_FINLINE jk::ErrorInfo Ei ## pre() { return m_ErrorInfo ## pre; } \
	_FINLINE const std::string& Ei ## pre ## Str() const { return m_ErrorInfo ## pre.Message; } \
	jk::ibool Ei ## pre ## IsEmpty(); \
	jstring Ei ## pre ## Utf(JNIEnv*);

#define JUNK_EI_IMPL(cls, pre) \
	void cls::Ei ## pre ## FncV(const char* pszFuncName, const char* pszInternalError, const char* pszMessage, va_list args) { m_ErrorInfo ## pre.ClsV(#cls, pszFuncName, pszInternalError, pszMessage, args); } \
	void cls::Ei ## pre ## Fnc(const char* pszFuncName, const char* pszInternalError, const char* pszMessage, ...) { va_list args; va_start(args, pszMessage); Ei ## pre ## FncV(pszFuncName, pszInternalError, pszMessage, args); va_end(args); } \
	void cls::Ei ## pre ## FncLeV(const char* pszFuncName, const char* pszMessage, va_list args) { Ei ## pre ## FncV(pszFuncName, jk::Error::GetLastError(), pszMessage, args); } \
	void cls::Ei ## pre ## FncLe(const char* pszFuncName, const char* pszMessage, ...) { va_list args; va_start(args, pszMessage); Ei ## pre ## FncLeV(pszFuncName, pszMessage, args); va_end(args); } \
	jk::ibool cls::Ei ## pre ## IsEmpty() { return m_ErrorInfo ## pre.IsEmpty(); } \
	jstring cls::Ei ## pre ## Utf(JNIEnv* env) { return env->NewStringUTF(m_ErrorInfo ## pre.Message.c_str()); }

#else

#define JUNK_EI_DECL(pre) \
protected: jk::ErrorInfo m_ErrorInfo ## pre; \
	void Ei ## pre ## FncV(const char* pszFuncName, const char* pszInternalError, const char* pszMessage, va_list args); \
	void Ei ## pre ## Fnc(const char* pszFuncName, const char* pszInternalError, const char* pszMessage, ...); \
	void Ei ## pre ## FncLeV(const char* pszFuncName, const char* pszMessage, va_list args); \
	void Ei ## pre ## FncLe(const char* pszFuncName, const char* pszMessage, ...); \
public: \
	_FINLINE jk::ErrorInfo Ei ## pre() { return m_ErrorInfo ## pre; } \
	_FINLINE const std::string& Ei ## pre ## Str() const { return m_ErrorInfo ## pre.Message; } \
	jk::ibool Ei ## pre ## IsEmpty();

#define JUNK_EI_IMPL(cls, pre) \
	void cls::Ei ## pre ## FncV(const char* pszFuncName, const char* pszInternalError, const char* pszMessage, va_list args) { m_ErrorInfo ## pre.ClsV(#cls, pszFuncName, pszInternalError, pszMessage, args); } \
	void cls::Ei ## pre ## Fnc(const char* pszFuncName, const char* pszInternalError, const char* pszMessage, ...) { va_list args; va_start(args, pszMessage); Ei ## pre ## FncV(pszFuncName, pszInternalError, pszMessage, args); va_end(args); } \
	void cls::Ei ## pre ## FncLeV(const char* pszFuncName, const char* pszMessage, va_list args) { Ei ## pre ## FncV(pszFuncName, jk::Error::GetLastError(), pszMessage, args); } \
	void cls::Ei ## pre ## FncLe(const char* pszFuncName, const char* pszMessage, ...) { va_list args; va_start(args, pszMessage); Ei ## pre ## FncLeV(pszFuncName, pszMessage, args); va_end(args); } \
	jk::ibool cls::Ei ## pre ## IsEmpty() { return m_ErrorInfo ## pre.IsEmpty(); }

#endif


_JUNK_END

#endif
