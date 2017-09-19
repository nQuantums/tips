#include <string>
#include <iostream>
#include <sstream>
#include <strstream>
#include <iomanip>
#include <memory>
#include <string.h>
#include "GlobalSocketLogger.h"
#include "Encoding.h"
#include "FilePath.h"
#include "Directory.h"
#include "DateTime.h"
#include "Str.h"
#include "Clock.h"
#include "ThreadLocalStorage.h"
#include "Logger.h"

#if defined _MSC_VER
#include <Windows.h>
#include <stdlib.h>
#else
#error gcc version is not implemented.
#endif


static JUNK_TLS(bool) s_Recurse;


//==============================================================================
//		エクスポート関数

JUNKLOGGERAPI void JUNKLOGGERCALL jk_Logger_Startup(const wchar_t* pszHost, int port) {
	jk::GlobalSocketLogger::Startup(pszHost, port);
}

JUNKLOGGERAPI void JUNKLOGGERCALL jk_Logger_FrameStart(jk_Logger_Frame* pFrame, const wchar_t* pszFrameName, const wchar_t* pszArgs) {
	// APIフックからも呼ばれるため再入防止
	if(s_Recurse.Get())
		return;
	s_Recurse.Get() = true;

	pFrame->EnterTime = jk::Clock::SysNS();
#if defined _MSC_VER
	jk::GlobalSocketLogger::IncrementDepth();

	// フレーム名と引数追加
	size_t len = wcslen(pszFrameName);
	pFrame->FrameNameLen = (int)len;
	std::wstring& s = *((std::wstring*&)pFrame->pData = new std::wstring(pszFrameName, len));
	if (pszArgs) {
		s += L"(";
		s += pszArgs;
		s += L")";
	} else {
		s += L"()";
	}

	// ログをサーバーへ送る
	jk::GlobalSocketLogger::WriteLog((uint32_t)jk::GlobalSocketLogger::GetDepth(), jk::LogServer::LogTypeEnum::Enter, s.c_str());
#else
#error gcc version is not implemented.
#endif

	s_Recurse.Get() = false;
}

JUNKLOGGERAPI void JUNKLOGGERCALL jk_Logger_FrameEnd(jk_Logger_Frame* pFrame) {
	// APIフックからも呼ばれるため再入防止
	if(s_Recurse.Get())
		return;
	s_Recurse.Get() = true;

#if defined _MSC_VER
	std::wstring& s = *(std::wstring*)pFrame->pData;

	// 所要時間追加
	wchar_t time[32];
	_i64tow_s((jk::Clock::SysNS() - pFrame->EnterTime) / 1000000, time, 32, 10);
	s.resize(pFrame->FrameNameLen);
	s += JUNKLOG_DELIMITER;
	s += time;
	s += L"ms";

	// ログをサーバーへ送る
	jk::GlobalSocketLogger::WriteLog((uint32_t)jk::GlobalSocketLogger::GetDepth(), jk::LogServer::LogTypeEnum::Leave, s.c_str());

	jk::GlobalSocketLogger::DecrementDepth();

	delete &s;
#else
#error gcc version is not implemented.
#endif

	s_Recurse.Get() = false;
}
