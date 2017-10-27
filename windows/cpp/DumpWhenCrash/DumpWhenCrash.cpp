// DumpWhenCrash.cpp : コンソール アプリケーションのエントリ ポイントを定義します。
//

#include "stdafx.h"
#include <Windows.h>
#include <DbgHelp.h>
#include <stdio.h>
#include <strsafe.h>
#include <string.h>
#include <thread>
#include <assert.h>
#include <stdlib.h>


void CreateDump(EXCEPTION_POINTERS *pep, int level) {
	TCHAR szFilePath[1024];
	GetModuleFileName(NULL, szFilePath, sizeof(szFilePath));

	_tcscat_s(szFilePath, _T(".dmp"));

	HANDLE hFile = CreateFile(szFilePath, GENERIC_READ | GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
	if (hFile == INVALID_HANDLE_VALUE) {
		_tprintf(_T("CreateFile failed. Error: %u \n"), GetLastError());
		return;
	}

	MINIDUMP_EXCEPTION_INFORMATION mdei;

	mdei.ThreadId = GetCurrentThreadId();
	mdei.ExceptionPointers = pep;
	mdei.ClientPointers = FALSE;

	MINIDUMP_CALLBACK_INFORMATION mci;

	mci.CallbackRoutine = NULL;
	mci.CallbackParam = 0;

	MINIDUMP_TYPE mdt;

	switch (level) {
	case 0:
		mdt = (MINIDUMP_TYPE)(MiniDumpNormal);
		break;
	case 1:
		mdt = (MINIDUMP_TYPE)(
			MiniDumpWithIndirectlyReferencedMemory |
			MiniDumpScanMemory);
		break;
	case 2:
		mdt = (MINIDUMP_TYPE)(
			MiniDumpWithPrivateReadWriteMemory |
			MiniDumpWithDataSegs |
			MiniDumpWithHandleData |
			MiniDumpWithFullMemoryInfo |
			MiniDumpWithThreadInfo |
			MiniDumpWithUnloadedModules);
		break;
	default:
		mdt = (MINIDUMP_TYPE)(
			MiniDumpWithFullMemory |
			MiniDumpWithFullMemoryInfo |
			MiniDumpWithHandleData |
			MiniDumpWithThreadInfo |
			MiniDumpWithUnloadedModules);
		break;
	}

	typedef BOOL(WINAPI *FuncPtrMiniDumpWriteDump)(
			_In_ HANDLE hProcess,
			_In_ DWORD ProcessId,
			_In_ HANDLE hFile,
			_In_ MINIDUMP_TYPE DumpType,
			_In_opt_ PMINIDUMP_EXCEPTION_INFORMATION ExceptionParam,
			_In_opt_ PMINIDUMP_USER_STREAM_INFORMATION UserStreamParam,
			_In_opt_ PMINIDUMP_CALLBACK_INFORMATION CallbackParam
		);

	HMODULE h = ::LoadLibraryW(L"DbgHelp.dll");
	FuncPtrMiniDumpWriteDump pFn = (FuncPtrMiniDumpWriteDump)::GetProcAddress(h, "MiniDumpWriteDump");

	BOOL rv = pFn(GetCurrentProcess(), GetCurrentProcessId(), hFile, mdt, (pep != NULL) ? &mdei : NULL, NULL, &mci);
	if (rv == FALSE) {
		_tprintf(_T("MiniDumpWriteDump failed. Error: %u \n"), GetLastError());
	}

	CloseHandle(hFile);
	return;
}

LONG WINAPI MyUnhandledExceptionFilter(
	struct _EXCEPTION_POINTERS *ExceptionInfo
) {
	CreateDump(ExceptionInfo, 3);
	return EXCEPTION_CONTINUE_SEARCH; // EXCEPTION_EXECUTE_HANDLER;
}

void Crash() {
	std::thread t([&] {
		//exit(0);
		//assert(0);
		int* p = NULL;
		*p = 32;
	});
	t.join();

	//int* p = NULL;
	//*p = 32;
}

int main()
{
	SetUnhandledExceptionFilter(MyUnhandledExceptionFilter);

	Crash();

	return 0;
}

