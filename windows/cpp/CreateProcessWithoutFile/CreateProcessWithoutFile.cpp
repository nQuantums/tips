// CreateProcessWithoutFile.cpp : コンソール アプリケーションのエントリ ポイントを定義します。
//

#include "stdafx.h"
#include <windows.h>
#include <vector>
#include <iostream>
#include <fstream>
#include <iterator>
#include <algorithm>
#include <memory>



#pragma pack(push)
#pragma pack(1)
template <class T>
struct LIST_ENTRY_T {
	T Flink;
	T Blink;
};

template <class T>
struct UNICODE_STRING_T {
	union {
		struct {
			WORD Length;
			WORD MaximumLength;
		};
		T dummy;
	};
	T _Buffer;
};

template <class T, class NGF, int A>
struct _PEB_T {
	union {
		struct {
			BYTE InheritedAddressSpace;
			BYTE ReadImageFileExecOptions;
			BYTE BeingDebugged;
			BYTE _SYSTEM_DEPENDENT_01;
		};
		T dummy01;
	};
	T Mutant;
	T ImageBaseAddress;
	T Ldr;
	T ProcessParameters;
	T SubSystemData;
	T ProcessHeap;
	T FastPebLock;
	T _SYSTEM_DEPENDENT_02;
	T _SYSTEM_DEPENDENT_03;
	T _SYSTEM_DEPENDENT_04;
	union {
		T KernelCallbackTable;
		T UserSharedInfoPtr;
	};
	DWORD SystemReserved;
	DWORD _SYSTEM_DEPENDENT_05;
	T _SYSTEM_DEPENDENT_06;
	T TlsExpansionCounter;
	T TlsBitmap;
	DWORD TlsBitmapBits[2];
	T ReadOnlySharedMemoryBase;
	T _SYSTEM_DEPENDENT_07;
	T ReadOnlyStaticServerData;
	T AnsiCodePageData;
	T OemCodePageData;
	T UnicodeCaseTableData;
	DWORD NumberOfProcessors;
	union {
		DWORD NtGlobalFlag;
		NGF dummy02;
	};
	LARGE_INTEGER CriticalSectionTimeout;
	T HeapSegmentReserve;
	T HeapSegmentCommit;
	T HeapDeCommitTotalFreeThreshold;
	T HeapDeCommitFreeBlockThreshold;
	DWORD NumberOfHeaps;
	DWORD MaximumNumberOfHeaps;
	T ProcessHeaps;
	T GdiSharedHandleTable;
	T ProcessStarterHelper;
	T GdiDCAttributeList;
	T LoaderLock;
	DWORD OSMajorVersion;
	DWORD OSMinorVersion;
	WORD OSBuildNumber;
	WORD OSCSDVersion;
	DWORD OSPlatformId;
	DWORD ImageSubsystem;
	DWORD ImageSubsystemMajorVersion;
	T ImageSubsystemMinorVersion;
	union {
		T ImageProcessAffinityMask;
		T ActiveProcessAffinityMask;
	};
	T GdiHandleBuffer[A];
	T PostProcessInitRoutine;
	T TlsExpansionBitmap;
	DWORD TlsExpansionBitmapBits[32];
	T SessionId;
	ULARGE_INTEGER AppCompatFlags;
	ULARGE_INTEGER AppCompatFlagsUser;
	T pShimData;
	T AppCompatInfo;
	UNICODE_STRING_T<T> CSDVersion;
	T ActivationContextData;
	T ProcessAssemblyStorageMap;
	T SystemDefaultActivationContextData;
	T SystemAssemblyStorageMap;
	T MinimumStackCommit;
};

typedef _PEB_T<DWORD, DWORD64, 34> PEB32;
typedef _PEB_T<DWORD64, DWORD, 30> PEB64;
#pragma pack(pop)



typedef LONG(NTAPI *FuncZwUnmapViewOfSection)(HANDLE, PVOID);


ULONG protect(ULONG characteristics) {
	static const ULONG mapping[]
		= { PAGE_NOACCESS, PAGE_EXECUTE, PAGE_READONLY, PAGE_EXECUTE_READ,
		PAGE_READWRITE, PAGE_EXECUTE_READWRITE, PAGE_READWRITE, PAGE_EXECUTE_READWRITE };

	return mapping[characteristics >> 29];
}

std::vector<BYTE> ReadFileToVector(const wchar_t* filename) {
	// open the file:
	std::ifstream file(filename, std::ios::binary);

	// Stop eating new lines in binary mode!!!
	file.unsetf(std::ios::skipws);

	// get its size:
	std::streampos fileSize;

	file.seekg(0, std::ios::end);
	fileSize = file.tellg();
	file.seekg(0, std::ios::beg);

	// reserve capacity
	std::vector<BYTE> vec;
	vec.reserve(fileSize);

	// read the data:
	vec.insert(vec.begin(), std::istream_iterator<BYTE>(file), std::istream_iterator<BYTE>());

	return std::move(vec);
}

std::unique_ptr<char> ReadFile(const wchar_t* filename) {
	std::ifstream infile(filename, std::ifstream::binary);

	infile.seekg(0, infile.end);     //N is the total number of doubles
	auto N = infile.tellg();
	infile.seekg(0, infile.beg);

	std::unique_ptr<char> buffer(new char[N]);  // 領域を確保

	infile.read(buffer.get(), N);

	return std::move(buffer);
}


int main() {
	// .libファイル無いので動的に関数取得
	HMODULE hMod = ::GetModuleHandleW(L"ntdll.dll");
	auto ZwUnmapViewOfSection = FuncZwUnmapViewOfSection(::GetProcAddress(hMod, "ZwUnmapViewOfSection"));


	TCHAR cmdline[] = _T("cmd.exe");
	STARTUPINFOW si;
	ZeroMemory(&si, sizeof(si));
	si.cb = sizeof(si);

	PROCESS_INFORMATION pi;
	ZeroMemory(&pi, sizeof(pi));
	if (!::CreateProcessW(NULL, cmdline, NULL, NULL, FALSE, CREATE_SUSPENDED, NULL, NULL, &si, &pi)) {
		std::cout << "CreateProcessW failed." << std::endl;
		return -1;
	}

	CONTEXT context = { CONTEXT_INTEGER };
	::GetThreadContext(pi.hThread, &context);

	PVOID x;
	::ReadProcessMemory(pi.hProcess, PCHAR(context.Rbx) + 8, &x, sizeof x, 0);
	//::ReadProcessMemory(pi.hProcess, PCHAR(context.Ebx) + 8, &x, sizeof x, 0);

	auto status = ZwUnmapViewOfSection(pi.hProcess, x);

	auto exeData = ReadFile(LR"(C:\Windows\notepad.exe)");
	PVOID p = exeData.get();
	//PVOID p = ::LockResource(::LoadResource(0, ::FindResourceW(0, L"Image", L"EXE")));

	PIMAGE_NT_HEADERS nt = PIMAGE_NT_HEADERS(PCHAR(p) + PIMAGE_DOS_HEADER(p)->e_lfanew);

	PVOID q = ::VirtualAllocEx(pi.hProcess,
		PVOID(nt->OptionalHeader.ImageBase),
		nt->OptionalHeader.SizeOfImage,
		MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE);

	::WriteProcessMemory(pi.hProcess, q, p, nt->OptionalHeader.SizeOfHeaders, 0);

	PIMAGE_SECTION_HEADER sect = IMAGE_FIRST_SECTION(nt);

	for (ULONG i = 0; i < nt->FileHeader.NumberOfSections; i++) {

		::WriteProcessMemory(pi.hProcess,
			PCHAR(q) + sect[i].VirtualAddress,
			PCHAR(p) + sect[i].PointerToRawData,
			sect[i].SizeOfRawData, 0);

		ULONG x;

		::VirtualProtectEx(pi.hProcess, PCHAR(q) + sect[i].VirtualAddress, sect[i].Misc.VirtualSize,
			protect(sect[i].Characteristics), &x);
	}

	::WriteProcessMemory(pi.hProcess, PCHAR(context.Ebx) + 8, &q, sizeof q, 0);

	context.Eax = ULONG(q) + nt->OptionalHeader.AddressOfEntryPoint;

	::SetThreadContext(pi.hThread, &context);

	::ResumeThread(pi.hThread);

	::CloseHandle(pi.hProcess);
	::CloseHandle(pi.hThread);

	return 0;
}
