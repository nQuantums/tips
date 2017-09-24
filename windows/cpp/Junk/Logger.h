#pragma once
#ifndef __JUNK_LOGGER_H__
#define __JUNK_LOGGER_H__

#include <Windows.h>
#include <sstream>
#include "JunkConfig.h"

#if defined(_MSC_VER)
#if defined(_JUNK_LOGGER_EXPORTS)
#define JUNKLOGGERAPI extern "C" __declspec(dllexport)
#define JUNKLOGGERCALL __stdcall
#elif defined(_JUNK_LOGGER_IMPORTS)
#define JUNKLOGGERAPI extern "C" __declspec(dllimport)
#define JUNKLOGGERCALL __stdcall
#else
#define JUNKLOGGERAPI
#define JUNKLOGGERCALL __stdcall
#endif
#endif

#pragma pack(push, 1)
struct jk_Logger_Frame {
	int64_t EnterTime; //!< フレーム開始時間
	void* pData; //!< データ
	int FrameNameLen; //!< フレーム名長

	_FINLINE jk_Logger_Frame(const wchar_t* pszFrameName, const wchar_t* pszArgs = NULL);
	_FINLINE ~jk_Logger_Frame();
};
#pragma pack(pop)

JUNKLOGGERAPI void JUNKLOGGERCALL jk_Logger_Startup(const wchar_t* pszHost, int port);
JUNKLOGGERAPI void JUNKLOGGERCALL jk_Logger_FrameStart(jk_Logger_Frame* pFrame, const wchar_t* pszFrameName, const wchar_t* pszArgs = NULL);
JUNKLOGGERAPI void JUNKLOGGERCALL jk_Logger_FrameEnd(jk_Logger_Frame* pFrame);

jk_Logger_Frame::jk_Logger_Frame(const wchar_t* pszFrameName, const wchar_t* pszArgs) {
	jk_Logger_FrameStart(this, pszFrameName, pszArgs);
}
jk_Logger_Frame::~jk_Logger_Frame() {
	jk_Logger_FrameEnd(this);
}


class jk_Logger_Stream : public std::wstringstream {
public:
	std::wstring buf;
};

inline jk_Logger_Stream& operator<<(jk_Logger_Stream& stream, const wchar_t* value) {
	if (!value) {
		return (jk_Logger_Stream&)((std::wstringstream&)stream << L"null");
	} else {
		return (jk_Logger_Stream&)((std::wstringstream&)stream << value);
	}
}

inline jk_Logger_Stream& operator<<(jk_Logger_Stream& stream, const char* value) {
	if (!value) {
		return (jk_Logger_Stream&)((std::wstringstream&)stream << L"null");
	} else if (!*value) {
		return (jk_Logger_Stream&)((std::wstringstream&)stream << L"");
	} else {
		size_t len = strlen(value);
		size_t size = (len + 1) * 4;
		if (stream.buf.size() < size)
			stream.buf.resize(size);
		len = (size_t)::MultiByteToWideChar(CP_ACP, 0, value, (int)len, &stream.buf[0], (int)size);
		stream.buf.resize(len + 1);
		stream.buf[len] = 0;
		return (jk_Logger_Stream&)((std::wstringstream&)stream << stream.buf);
	}
}

inline jk_Logger_Stream& operator<<(jk_Logger_Stream& stream, const std::string& value) {
	if (value.empty()) {
		return (jk_Logger_Stream&)((std::wstringstream&)stream << L"");
	} else {
		size_t len = value.size();
		size_t size = (len + 1) * 4;
		if (stream.buf.size() < size)
			stream.buf.resize(size);
		len = (size_t)::MultiByteToWideChar(CP_ACP, 0, value.c_str(), (int)len, &stream.buf[0], (int)size);
		stream.buf.resize(len + 1);
		stream.buf[len] = 0;
		return (jk_Logger_Stream&)((std::wstringstream&)stream << stream.buf);
	}
}

inline std::wstring jk_ExeFileName() {
	wchar_t path[MAX_PATH + 1];
	::GetModuleFileNameW(NULL, path, MAX_PATH);
	return path;
}

#define JUNK_LOG_FUNC_ARGSVAR __jk_log_func_args__
#define JUNK_LOG_FUNC_BEGIN jk_Logger_Stream JUNK_LOG_FUNC_ARGSVAR
#define JUNK_LOG_FUNC_ARGS_BEGIN(arg) jk_Logger_Stream JUNK_LOG_FUNC_ARGSVAR; JUNK_LOG_FUNC_ARGSVAR << L#arg L" = " << (arg)
#define JUNK_LOG_FUNC_ARGS(arg) << L", " L#arg L" = " << (arg)
#define JUNK_LOG_FUNC_COMMIT ; jk_Logger_Frame __jk_log_func__(__FUNCTIONW__, JUNK_LOG_FUNC_ARGSVAR.str().c_str())

#define JUNK_LOG_FRAME_ARGSVAR(name) __jk_log_frame_ ## name ## _args__
#define JUNK_LOG_FRAME_BEGIN(name) jk_Logger_Stream JUNK_LOG_FRAME_ARGSVAR(name)
#define JUNK_LOG_FRAME_ARGS_BEGIN(name, arg) jk_Logger_Stream JUNK_LOG_FRAME_ARGSVAR(name); JUNK_LOG_FRAME_ARGSVAR(name) << L#arg L" = " << (arg)
#define JUNK_LOG_FRAME_ARGS(arg) << L", " L#arg L" = " << (arg)
#define JUNK_LOG_FRAME_COMMIT(name) ; jk_Logger_Frame __jk_log_frame_ ## name ## __(L#name, JUNK_LOG_FRAME_ARGSVAR(name).str().c_str())

#define JUNK_LOG_FUNC() JUNK_LOG_FUNC_BEGIN JUNK_LOG_FUNC_COMMIT
#define JUNK_LOG_FUNC1(arg1) JUNK_LOG_FUNC_ARGS_BEGIN(arg1) JUNK_LOG_FUNC_COMMIT
#define JUNK_LOG_FUNC2(arg1, arg2) JUNK_LOG_FUNC_ARGS_BEGIN(arg1) JUNK_LOG_FUNC_ARGS(arg2) JUNK_LOG_FUNC_COMMIT
#define JUNK_LOG_FUNC3(arg1, arg2, arg3) JUNK_LOG_FUNC_ARGS_BEGIN(arg1) JUNK_LOG_FUNC_ARGS(arg2) JUNK_LOG_FUNC_ARGS(arg3) JUNK_LOG_FUNC_COMMIT
#define JUNK_LOG_FUNC4(arg1, arg2, arg3, arg4) JUNK_LOG_FUNC_ARGS_BEGIN(arg1) JUNK_LOG_FUNC_ARGS(arg2) JUNK_LOG_FUNC_ARGS(arg3) JUNK_LOG_FUNC_ARGS(arg4) JUNK_LOG_FUNC_COMMIT
#define JUNK_LOG_FUNC5(arg1, arg2, arg3, arg4, arg5) JUNK_LOG_FUNC_ARGS_BEGIN(arg1) JUNK_LOG_FUNC_ARGS(arg2) JUNK_LOG_FUNC_ARGS(arg3) JUNK_LOG_FUNC_ARGS(arg4) JUNK_LOG_FUNC_ARGS(arg5) JUNK_LOG_FUNC_COMMIT
#define JUNK_LOG_FUNC6(arg1, arg2, arg3, arg4, arg5, arg6) JUNK_LOG_FUNC_ARGS_BEGIN(arg1) JUNK_LOG_FUNC_ARGS(arg2) JUNK_LOG_FUNC_ARGS(arg3) JUNK_LOG_FUNC_ARGS(arg4) JUNK_LOG_FUNC_ARGS(arg5) JUNK_LOG_FUNC_ARGS(arg6) JUNK_LOG_FUNC_COMMIT
#define JUNK_LOG_FUNC7(arg1, arg2, arg3, arg4, arg5, arg6, arg7) JUNK_LOG_FUNC_ARGS_BEGIN(arg1) JUNK_LOG_FUNC_ARGS(arg2) JUNK_LOG_FUNC_ARGS(arg3) JUNK_LOG_FUNC_ARGS(arg4) JUNK_LOG_FUNC_ARGS(arg5) JUNK_LOG_FUNC_ARGS(arg6) JUNK_LOG_FUNC_ARGS(arg7) JUNK_LOG_FUNC_COMMIT
#define JUNK_LOG_FUNC8(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8) JUNK_LOG_FUNC_ARGS_BEGIN(arg1) JUNK_LOG_FUNC_ARGS(arg2) JUNK_LOG_FUNC_ARGS(arg3) JUNK_LOG_FUNC_ARGS(arg4) JUNK_LOG_FUNC_ARGS(arg5) JUNK_LOG_FUNC_ARGS(arg6) JUNK_LOG_FUNC_ARGS(arg7) JUNK_LOG_FUNC_ARGS(arg8) JUNK_LOG_FUNC_COMMIT
#define JUNK_LOG_FUNC9(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9) JUNK_LOG_FUNC_ARGS_BEGIN(arg1) JUNK_LOG_FUNC_ARGS(arg2) JUNK_LOG_FUNC_ARGS(arg3) JUNK_LOG_FUNC_ARGS(arg4) JUNK_LOG_FUNC_ARGS(arg5) JUNK_LOG_FUNC_ARGS(arg6) JUNK_LOG_FUNC_ARGS(arg7) JUNK_LOG_FUNC_ARGS(arg8) JUNK_LOG_FUNC_ARGS(arg9) JUNK_LOG_FUNC_COMMIT
#define JUNK_LOG_FUNC10(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10) JUNK_LOG_FUNC_ARGS_BEGIN(arg1) JUNK_LOG_FUNC_ARGS(arg2) JUNK_LOG_FUNC_ARGS(arg3) JUNK_LOG_FUNC_ARGS(arg4) JUNK_LOG_FUNC_ARGS(arg5) JUNK_LOG_FUNC_ARGS(arg6) JUNK_LOG_FUNC_ARGS(arg7) JUNK_LOG_FUNC_ARGS(arg8) JUNK_LOG_FUNC_ARGS(arg9) JUNK_LOG_FUNC_ARGS(arg10) JUNK_LOG_FUNC_COMMIT

#define JUNK_LOG_FRAME(name) JUNK_LOG_FRAME_BEGIN(name) JUNK_LOG_FRAME_COMMIT(name)
#define JUNK_LOG_FRAME1(name, arg1) JUNK_LOG_FRAME_ARGS_BEGIN(name, arg1) JUNK_LOG_FRAME_COMMIT(name)
#define JUNK_LOG_FRAME2(name, arg1, arg2) JUNK_LOG_FRAME_ARGS_BEGIN(name, arg1) JUNK_LOG_FRAME_ARGS(arg2) JUNK_LOG_FRAME_COMMIT(name)
#define JUNK_LOG_FRAME3(name, arg1, arg2, arg3) JUNK_LOG_FRAME_ARGS_BEGIN(name, arg1) JUNK_LOG_FRAME_ARGS(arg2) JUNK_LOG_FRAME_ARGS(arg3) JUNK_LOG_FRAME_COMMIT(name)
#define JUNK_LOG_FRAME4(name, arg1, arg2, arg3, arg4) JUNK_LOG_FRAME_ARGS_BEGIN(name, arg1) JUNK_LOG_FRAME_ARGS(arg2) JUNK_LOG_FRAME_ARGS(arg3) JUNK_LOG_FRAME_ARGS(arg4) JUNK_LOG_FRAME_COMMIT(name)
#define JUNK_LOG_FRAME5(name, arg1, arg2, arg3, arg4, arg5) JUNK_LOG_FRAME_ARGS_BEGIN(name, arg1) JUNK_LOG_FRAME_ARGS(arg2) JUNK_LOG_FRAME_ARGS(arg3) JUNK_LOG_FRAME_ARGS(arg4) JUNK_LOG_FRAME_ARGS(arg5) JUNK_LOG_FRAME_COMMIT(name)
#define JUNK_LOG_FRAME6(name, arg1, arg2, arg3, arg4, arg5, arg6) JUNK_LOG_FRAME_ARGS_BEGIN(name, arg1) JUNK_LOG_FRAME_ARGS(arg2) JUNK_LOG_FRAME_ARGS(arg3) JUNK_LOG_FRAME_ARGS(arg4) JUNK_LOG_FRAME_ARGS(arg5) JUNK_LOG_FRAME_ARGS(arg6) JUNK_LOG_FRAME_COMMIT(name)
#define JUNK_LOG_FRAME7(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7) JUNK_LOG_FRAME_ARGS_BEGIN(name, arg1) JUNK_LOG_FRAME_ARGS(arg2) JUNK_LOG_FRAME_ARGS(arg3) JUNK_LOG_FRAME_ARGS(arg4) JUNK_LOG_FRAME_ARGS(arg5) JUNK_LOG_FRAME_ARGS(arg6) JUNK_LOG_FRAME_ARGS(arg7) JUNK_LOG_FRAME_COMMIT(name)
#define JUNK_LOG_FRAME8(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8) JUNK_LOG_FRAME_ARGS_BEGIN(name, arg1) JUNK_LOG_FRAME_ARGS(arg2) JUNK_LOG_FRAME_ARGS(arg3) JUNK_LOG_FRAME_ARGS(arg4) JUNK_LOG_FRAME_ARGS(arg5) JUNK_LOG_FRAME_ARGS(arg6) JUNK_LOG_FRAME_ARGS(arg7) JUNK_LOG_FRAME_ARGS(arg8) JUNK_LOG_FRAME_COMMIT(name)
#define JUNK_LOG_FRAME9(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9) JUNK_LOG_FRAME_ARGS_BEGIN(name, arg1) JUNK_LOG_FRAME_ARGS(arg2) JUNK_LOG_FRAME_ARGS(arg3) JUNK_LOG_FRAME_ARGS(arg4) JUNK_LOG_FRAME_ARGS(arg5) JUNK_LOG_FRAME_ARGS(arg6) JUNK_LOG_FRAME_ARGS(arg7) JUNK_LOG_FRAME_ARGS(arg8) JUNK_LOG_FRAME_ARGS(arg9) JUNK_LOG_FRAME_COMMIT(name)
#define JUNK_LOG_FRAME10(name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10) JUNK_LOG_FRAME_ARGS_BEGIN(name, arg1) JUNK_LOG_FRAME_ARGS(arg2) JUNK_LOG_FRAME_ARGS(arg3) JUNK_LOG_FRAME_ARGS(arg4) JUNK_LOG_FRAME_ARGS(arg5) JUNK_LOG_FRAME_ARGS(arg6) JUNK_LOG_FRAME_ARGS(arg7) JUNK_LOG_FRAME_ARGS(arg8) JUNK_LOG_FRAME_ARGS(arg9) JUNK_LOG_FRAME_ARGS(arg10) JUNK_LOG_FRAME_COMMIT(name)

#endif
