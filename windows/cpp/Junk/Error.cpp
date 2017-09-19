#include "Error.h"
#include "Str.h"
#include "ThreadLocalStorage.h"
#include <stdio.h>
#include <string.h>

_JUNK_BEGIN

static JUNK_TLS(Error::LocalStorage) s_tls;

//==============================================================================
//		エラー情報関係クラス

//! スレッド毎の最終エラー文字列を設定する
void Error::SetLastError(
	const char* pszFmt, //!< [in] 書式
	...
) {
	va_list args;
	va_start(args, pszFmt);
	Error::LocalStorage& tls = s_tls.Get();
	size_t bufSize = sizeof(tls.LastErrorStringBuffer);
#if defined __GNUC__
	vsnprintf(tls.LastErrorStringBuffer, bufSize - 1, pszFmt, args);
#else
	vsnprintf_s(tls.LastErrorStringBuffer, bufSize, bufSize - 1, pszFmt, args);
#endif
	va_end(args);
}

//! errno から対応する文字列を取得してスレッド毎のエラー文字列として設定する
void Error::SetLastErrorFromErrno(
	int en //!< [in] errno と同じ形式のエラーコード
) {
	Error::LocalStorage& tls = s_tls.Get();
	char* p = tls.LastErrorStringBuffer;
#ifdef _MSC_VER
	strerror_s(p, sizeof(tls.LastErrorStringBuffer) - 1, en);
#else
	auto p2 = strerror_r(en, p, sizeof(tls.LastErrorStringBuffer) - 1);
	if (p2 != p) {
		strncpy(p, p2, sizeof(tls.LastErrorStringBuffer) - 1);
	}
#endif
}

//! errno から対応する文字列を取得してスレッド毎のエラー文字列として設定する
void Error::SetLastErrorFromErrno() {
	SetLastErrorFromErrno(errno);
}

#if defined  _WIN32
//! Windowsエラーコードから対応する文字列を取得してスレッド毎のエラー文字列として設定する
void Error::SetLastErrorFromWinErr(
	DWORD lastError //!< [in] ::GetLastError() と同じ形式のエラーコード
) {
	Error::LocalStorage& tls = s_tls.Get();
	DWORD n = ::FormatMessageA(
		FORMAT_MESSAGE_IGNORE_INSERTS | FORMAT_MESSAGE_FROM_SYSTEM,
		NULL,
		lastError,
		MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
		tls.LastErrorStringBuffer,
		sizeof(tls.LastErrorStringBuffer) - 1,
		NULL);
	if (n != 0) {
		tls.LastErrorStringBuffer[n] = 0;
	}
}

//! ::GetLastError() から対応する文字列を取得してスレッド毎のエラー文字列として設定する
void Error::SetLastErrorFromWinErr() {
	SetLastErrorFromWinErr(::GetLastError());
}
#endif

//! スレッド毎の最終エラー文字列を取得する
const char* Error::GetLastErrorString() {
	return s_tls.Get().LastErrorStringBuffer;
}


//==============================================================================
//		エラー情報

//! クラス名、メソッド名、内部エラー情報、メッセージを設定する
void ErrorInfo::ClsV(
	const char* pszClassName, //!< [in] クラス名又はNULL
	const char* pszFuncName, //!< [in] 関数名又はNULL
	const char* pszInternalError, //!< [in] 内部エラー文字列又はNULL
	const char* pszMessage, //!< [in] メッセージ又はNULL、書式
	va_list args //!< [in] クラス名又はNULL
) {
	if (!_Empty)
		return;
	_Empty = false;

	Message.resize(0);
	if (pszClassName != NULL && strlen(pszClassName) != 0) {
		Message += pszClassName;
		Message += ".";
	}
	if (pszFuncName != NULL && strlen(pszFuncName) != 0)
		Message += pszFuncName;
	if (!Message.empty())
		Message += " : ";
	if (pszMessage != NULL && strlen(pszMessage) != 0)
		Str::AFV(Message, pszMessage, args);
	if (pszInternalError != NULL && strlen(pszInternalError) != 0)
		Str::AF(Message, " (%s)", pszInternalError);
}

//! クラス名、メソッド名、内部エラー情報、メッセージを設定する
void ErrorInfo::Cls(
	const char* pszClassName, //!< [in] クラス名又はNULL
	const char* pszFuncName, //!< [in] 関数名又はNULL
	const char* pszInternalError, //!< [in] 内部エラー文字列又はNULL
	const char* pszMessage, //!< [in] メッセージ又はNULL、書式
	...
) {
	if (!_Empty)
		return;
	_Empty = false;
	va_list args;
	va_start(args, pszMessage);
	ClsV(pszClassName, pszFuncName, pszInternalError, pszMessage, args);
	va_end(args);
}

//! エラー情報が空かどうかの取得
ibool ErrorInfo::IsEmpty() const {
	return _Empty;
}

_JUNK_END
