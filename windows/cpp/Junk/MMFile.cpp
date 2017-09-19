#include "MMFile.h"
#include "Error.h"
#include <new>

#if defined __GNUC__
#include <unistd.h>
#include <fcntl.h>
#include <sys/mman.h>
#include <sys/stat.h>
#elif defined  _WIN32
#include <io.h>
#include <fcntl.h>
#include <sys/stat.h>
#endif

#define DEFAULT_FLAGS O_NOATIME


_JUNK_BEGIN


static intptr_t s_PageSize = 0;


//==============================================================================
//		メモリマップトファイル

//! コンストラクタ
MMFile::MMFile() {
	if (s_PageSize == 0) {
#if defined __GNUC__
		s_PageSize = getpagesize();
#elif defined  _WIN32
		SYSTEM_INFO si;
		::GetSystemInfo(&si);
		s_PageSize = si.dwAllocationGranularity;
#endif
	}

	m_Flags = 0;
	m_Size = 0;
	m_hFile = IHV();
#if defined  _WIN32
	m_hMapping = NULL;
#endif
}

//! デストラクタ
MMFile::~MMFile() {
	Close();
}

//! サイズを指定してファイルを作成する、ファイルは読み書き込みモードで開かれる
ibool MMFile::Create(
	const char* pszFile, //!< [in] ファイル名
	int64_t size, //!< [in] ファイルサイズ(bytes)
	uint32_t createFlags //!< [in] ファイル作成動作フラグ、 0 又は MMFile::CreateEnum フラグの組み合わせ
) {
	if(m_hFile != IHV())
		Close();

#if defined __GNUC__
	// ファイル作成
	int mode = O_RDWR | O_CREAT | O_TRUNC;
	if (createFlags & CreateNew)
		mode |= O_EXCL;
	m_hFile = open(pszFile, mode | DEFAULT_FLAGS, S_IRWXU | S_IWGRP | S_IROTH);
	if (m_hFile == IHV()) {
		Error::SetLastErrorFromErrno();
		return false;
	}
	m_Flags = FlagsCreated | FlagsRead | FlagsWrite;

	// ファイルサイズ設定
#ifdef __x86_64__
	if (ftruncate64(m_hFile, (off_t)size) != 0) {
		Error::SetLastErrorFromErrno();
		close(m_hFile);
		m_hFile = IHV();
		return false;
	}
	m_Size = size;
#else
	if (ftruncate(m_hFile, (off_t)size) != 0) {
		Error::SetLastErrorFromErrno();
		close(m_hFile);
		m_hFile = IHV();
		return false;
	}
	m_Size = size;
#endif
#elif defined  _WIN32
	// ファイル作成
	DWORD creationFlags = 0;
	if (createFlags & CreateNew)
		creationFlags |= CREATE_NEW;
	else
		creationFlags |= CREATE_ALWAYS;
	m_hFile = ::CreateFileA(
		pszFile,
		GENERIC_READ | GENERIC_WRITE,
		FILE_SHARE_READ | FILE_SHARE_WRITE,
		NULL,
		creationFlags,
		FILE_ATTRIBUTE_NORMAL,
		NULL);
	if (m_hFile == IHV()) {
		Error::SetLastErrorFromWinErr();
		return false;
	}
	m_Flags = FlagsCreated | FlagsRead | FlagsWrite;

	// ファイルサイズ設定
	//	指定されたサイズの位置にファイルポインタ移動してファイル終端を設定する
	LARGE_INTEGER newPos;
	if (!::SetFilePointerEx(m_hFile, (LARGE_INTEGER&)size, &newPos, FILE_BEGIN) || !::SetEndOfFile(m_hFile)) {
		Error::SetLastErrorFromWinErr();
		::CloseHandle(m_hFile);
		m_hFile = IHV();
		return false;
	}
	m_Size = size;

	// ファイルマッピングオブジェクトの作成
	m_hMapping = CreateMapping();
	if (m_hMapping == NULL) {
		::CloseHandle(m_hFile);
		m_hFile = IHV();
		return false;
	}
#endif

	return true;
}

//! 指定されたファイルを指定されたモードで開く
ibool MMFile::Open(
	const char* pszFile, //!< [in] ファイル名
	uint32_t openFlags //!< [in] モード、 MMFile::OpenEnum フラグの組み合わせ
) {
	if(m_hFile != IHV())
		Close();

#ifdef __GNUC__
	// ファイル開く
	int mode = 0;
	if ((openFlags & OpenRW) == OpenRW)
		mode |= O_RDWR;
	else if(openFlags & OpenRead)
		mode |= O_RDONLY;
	else if(openFlags & OpenWrite)
		mode |= O_WRONLY;
	m_hFile = open(pszFile, mode | DEFAULT_FLAGS);
	if(m_hFile == IHV()) {
		Error::SetLastErrorFromErrno();
		return false;
	}
	m_Flags = FlagsOpened;
	if(openFlags & OpenRead)
		m_Flags |= FlagsRead;
	if(openFlags & OpenWrite)
		m_Flags |= FlagsWrite;

	// ファイルサイズ取得
	struct stat64 st;
	if(fstat64(m_hFile, &st) != 0) {
		Error::SetLastErrorFromErrno();
		close(m_hFile);
		m_hFile = IHV();
		return false;
	}
	m_Size = st.st_size;
#elif defined  _WIN32
	// ファイル開く
	DWORD access = 0;
	if (openFlags & OpenRead)
		access |= GENERIC_READ;
	if (openFlags & OpenWrite)
		access |= GENERIC_WRITE;
	m_hFile = ::CreateFileA(
		pszFile,
		access,
		FILE_SHARE_READ | FILE_SHARE_WRITE,
		NULL,
		OPEN_EXISTING,
		FILE_ATTRIBUTE_NORMAL,
		NULL);
	if (m_hFile == IHV()) {
		Error::SetLastErrorFromWinErr();
		return false;
	}
	m_Flags = FlagsOpened;
	if (openFlags & OpenRead)
		m_Flags |= FlagsRead;
	if (openFlags & OpenWrite)
		m_Flags |= FlagsWrite;

	// ファイルサイズ取得
	if (!::GetFileSizeEx(m_hFile, (LARGE_INTEGER*)&m_Size)) {
		Error::SetLastErrorFromWinErr();
		::CloseHandle(m_hFile);
		m_hFile = IHV();
		return false;
	}

	// ファイルマッピングオブジェクトの作成
	m_hMapping = CreateMapping();
	if (m_hMapping == NULL) {
		::CloseHandle(m_hFile);
		m_hFile = IHV();
		return false;
	}
#endif

	return true;
}

//! ファイルを閉じる
ibool MMFile::Close() {
	if(m_hFile != IHV()) {
#ifdef __GNUC__
		if (close(m_hFile) != 0) {
			Error::SetLastErrorFromErrno();
			return false;
		}
		m_hFile = IHV();
#elif defined  _WIN32
		if (m_hMapping != NULL) {
			if (!::CloseHandle(m_hMapping)) {
				Error::SetLastErrorFromWinErr();
				return false;
			}
			m_hMapping = NULL;
		}

		if (!::CloseHandle(m_hFile)) {
			Error::SetLastErrorFromWinErr();
			return false;
		}
		m_hFile = IHV();
#endif
		return false;
	}
	m_Size = 0;
	m_Flags = 0;
	return true;
}

//! ハンドルなど内部データを指定されたメモリマップトファイルオブジェクトから奪い取る、奪い取られた方はコンストラクト直後の状態になる
void MMFile::Rob(
	MMFile* pMMFile //!< [in,out] 中身を奪い取られるオブジェクト
) {
	if(m_hFile != IHV())
		Close();
	*this = *pMMFile;
	new(pMMFile) MMFile();
}

#if defined  _WIN32
//! ファイルマッピングオブジェクトの作成
HANDLE MMFile::CreateMapping() {
	// ファイルマッピングオブジェクトを作成
	DWORD flProtect, accessMode;
	if (m_Flags & FlagsWrite) {
		flProtect = PAGE_READWRITE;
		accessMode = FILE_MAP_WRITE;
	} else {
		flProtect = PAGE_READONLY;
		accessMode = FILE_MAP_READ;
	}

	HANDLE hMapping = ::CreateFileMapping(m_hFile, NULL, flProtect, 0, 0, NULL);
	if (hMapping == NULL) {
		Error::SetLastErrorFromWinErr();
		return NULL;
	}
	return hMapping;
}
#endif


//==============================================================================
//		メモリマップトファイルの割り当てられたメモリアドレスを指すビュー

//! コンストラクタ
MMView::MMView() {
	m_Position = 0;
	m_Size = 0;
	m_pPtr = NULL;
	m_MappedSize = 0;
	m_pMappedPtr = NULL;
}

//! デストラクタ
MMView::~MMView() {
	Unmap();
}

//! メモリを割り当ててポインタを返す、Unmap() を呼び出すまで mmfile.Close() を呼び出してはならない
void* MMView::Map(
	MMFile* pMMFile, //!< [in] メモリマップトファイル
	int64_t position, //!< [in] ファイルの先頭からメモリ割り当て位置へのオフセット(bytes)
	intptr_t size //!< [in] メモリ割り当てサイズ(bytes)、ファイルサイズを超える値を指定されたら可能な最大サイズに制限される、割り当て可能なサイズが 0 以下ならエラーとなる
) {
	if(m_pMappedPtr != NULL)
		Unmap();

	// ポジションとサイズチェック
	if(pMMFile->GetSize() < position + size) {
		if(pMMFile->GetSize() < position) {
			Error::SetLastErrorFromErrno(EINVAL);
			return NULL;
		}
		size = (intptr_t)(pMMFile->GetSize() - (int64_t)position);
	}
	if(size <= 0) {
		Error::SetLastErrorFromErrno(EINVAL);
		return NULL;
	}

	// メモリに割り当て
	intptr_t offset = position % s_PageSize;
	int64_t mapPosition = position - offset;
	intptr_t mapSize = size + offset;
	int ff = pMMFile->Flags();

#ifdef __GNUC__
	int f = 0;
	if(ff & MMFile::FlagsRead)
		f |= PROT_READ;
	if(ff & MMFile::FlagsWrite)
		f |= PROT_WRITE;
	m_pMappedPtr = mmap(NULL, mapSize, f, MAP_SHARED, pMMFile->m_hFile, mapPosition);
	if(m_pMappedPtr == MAP_FAILED) {
		Error::SetLastErrorFromErrno();
		m_pMappedPtr = NULL;
		return NULL;
	}
#elif defined  _WIN32
	LARGE_INTEGER li;
	li.QuadPart = mapPosition;

	// 指定された位置をメモリにマッピング
	m_pMappedPtr = ::MapViewOfFile(
		pMMFile->m_hMapping,
		ff & MMFile::FlagsWrite ? FILE_MAP_WRITE : FILE_MAP_READ,
		li.HighPart,
		li.LowPart,
		mapSize);
	if (m_pMappedPtr == NULL) {
		Error::SetLastErrorFromWinErr();
		return NULL;
	}
#endif

	m_MappedSize = mapSize;
	m_pPtr = (void*)((int8_t*)m_pMappedPtr + offset);
	m_Size = size;
	m_Position = position;

	return m_pPtr;
}

//! 割り当てられたメモリを開放する
ibool MMView::Unmap() {
	if(m_pMappedPtr == NULL) {
		Error::SetLastErrorFromErrno(EADDRNOTAVAIL);
		return false;
	}
#ifdef __GNUC__
	if(munmap(m_pMappedPtr, m_MappedSize) != 0) {
		Error::SetLastErrorFromErrno();
		return false;
	}
#elif defined  _WIN32
	if (!::UnmapViewOfFile(m_pMappedPtr)) {
		Error::SetLastErrorFromWinErr();
		return false;
	}
#endif
	m_Position = 0;
	m_Size = 0;
	m_pPtr = NULL;
	m_MappedSize = 0;
	m_pMappedPtr = NULL;
	return true;
}

//! ハンドルなど内部データを指定されたビューオブジェクトから奪い取る、奪い取られた方はコンストラクト直後の状態になる
void MMView::Rob(
	MMView* pMMView //!< [in,out] 中身を奪い取られるオブジェクト
) {
	if(m_pMappedPtr != NULL)
		Unmap();
	*this = *pMMView;
	new(pMMView) MMView();
}

_JUNK_END
