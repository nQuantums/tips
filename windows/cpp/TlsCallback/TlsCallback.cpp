// http://lallouslab.net/2017/05/30/using-cc-tls-callbacks-in-visual-studio-with-your-32-or-64bits-programs/
//

#include "stdafx.h"
#include <Windows.h>

static int v1 = 0;
static int v2 = 0;
VOID WINAPI tls_callback1(
	PVOID DllHandle,
	DWORD Reason,
	PVOID Reserved) {
	if (Reason == DLL_PROCESS_ATTACH)
		v1 = 1;
}
VOID WINAPI tls_callback2(
	PVOID DllHandle,
	DWORD Reason,
	PVOID Reserved) {
	if (Reason == DLL_PROCESS_ATTACH)
		v2 = 2;
}
//-------------------------------------------------------------------------
// TLS 32/64 bits example by Elias Bachaalany <lallousz-x86@yahoo.com>
#ifdef _M_AMD64
#pragma comment (linker, "/INCLUDE:_tls_used")
#pragma comment (linker, "/INCLUDE:p_tls_callback1")
#pragma const_seg(push)
#pragma const_seg(".CRT$XLAAA")
EXTERN_C const PIMAGE_TLS_CALLBACK p_tls_callback1 = tls_callback1;
#pragma const_seg(".CRT$XLAAB")
EXTERN_C const PIMAGE_TLS_CALLBACK p_tls_callback2 = tls_callback2;
#pragma const_seg(pop)
#endif
#ifdef _M_IX86
#pragma comment (linker, "/INCLUDE:__tls_used")
#pragma comment (linker, "/INCLUDE:_p_tls_callback1")
#pragma data_seg(push)
#pragma data_seg(".CRT$XLAAA")
EXTERN_C PIMAGE_TLS_CALLBACK p_tls_callback1 = tls_callback1;
#pragma data_seg(".CRT$XLAAB")
EXTERN_C PIMAGE_TLS_CALLBACK p_tls_callback2 = tls_callback2;
#pragma data_seg(pop)
#endif
//-------------------------------------------------------------------------
int _tmain(int argc, _TCHAR* argv[]) {
	printf("test values from tls callbacks are: tls1 = %d, tls2 = %d\n", v1, v2);
	return 0;
}
