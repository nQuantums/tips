#include "File.h"
#include "Error.h"

#if defined __GNUC__

#include <sys/stat.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <stdio.h>
#include <unistd.h>
#include <fcntl.h>
#include <dirent.h>
#include <string.h>

#define DEFAULT_FLAGS O_NOATIME

#elif defined _MSC_VER

#include <io.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <sys/types.h>
#include <share.h>

#pragma warning(disable:4267)

#endif


_JUNK_BEGIN

//! 指定されたファイルを削除する
ibool File::Delete(
	const wchar_t* pszFile //!< [in] ファイル名
) {
#if defined _MSC_VER
	if (_wremove(pszFile) != 0) {
		Error::SetLastErrorFromErrno();
		return false;
	}
#else
#error gcc version is not implemented.
#endif
	return true;
}

//! 指定されたファイルを削除する
ibool File::Delete(
	const char* pszFile //!< [in] ファイル名
) {
	if (remove(pszFile) != 0) {
		Error::SetLastErrorFromErrno();
		return false;
	}
	return true;
}

//! 指定されたファイルに指定されたアクセス可能か調べる
ibool File::Access(
	const wchar_t* pszFile, //!< [in] ファイルパス名
	uint32_t accessFlags //!< [in] File::Access* のフラグの組み合わせ
) {
#if defined _MSC_VER
	int f = 0;
	if (accessFlags & AccessRead) f |= 4;
	if (accessFlags & AccessWrite) f |= 2;
	return _waccess_s(pszFile, f) == 0;
#else
#error gcc version is not implemented.
#endif
}

//! 指定されたファイルに指定されたアクセス可能か調べる
ibool File::Access(
	const char* pszFile, //!< [in] ファイルパス名
	uint32_t accessFlags //!< [in] File::Access* のフラグの組み合わせ
) {
#if defined __GNUC__
	int f = 0;
	if (accessFlags & AccessRead)
		f |= R_OK;
	if (accessFlags & AccessWrite)
		f |= W_OK;
	return access(pszFile, f) == 0;
#else
	int f = 0;
	if (accessFlags & AccessRead) f |= 4;
	if (accessFlags & AccessWrite) f |= 2;
	return _access_s(pszFile, f) == 0;
#endif
}

//! 指定されたファイルが存在しているか調べる
ibool File::Exists(
	const wchar_t* pszFile //!< [in] ファイル名
) {
#if defined _MSC_VER
	struct _stat64 st;
	return _wstat64(pszFile, &st) == 0;
#else
#error gcc version is not implemented.
#endif
}

//! 指定されたファイルが存在しているか調べる
ibool File::Exists(
	const char* pszFile //!< [in] ファイル名
) {
#if defined __GNUC__
	// ファイルサイズ取得
	struct stat st;
	return stat(pszFile, &st) == 0;
#else
	struct _stat64 st;
	return _stat64(pszFile, &st) == 0;
#endif
}

//! 指定されたファイルを新しい場所に移動します
ibool File::Move(
	const wchar_t* sourceFileName, //!< [in] 移動するファイルの名前
	const wchar_t* destFileName //!< [in] ファイルの新しいパスおよび名前
) {
#if defined _MSC_VER
	if(_wrename(sourceFileName, destFileName) != 0) {
		Error::SetLastErrorFromErrno();
		return false;
	}
#else
#error gcc version is not implemented.
#endif
	return true;
}

//! 指定されたファイルを新しい場所に移動します
ibool File::Move(const char* sourceFileName, const char* destFileName) {
	if(rename(sourceFileName, destFileName) != 0) {
		Error::SetLastErrorFromErrno();
		return false;
	}
	return true;
}

#if defined __GNUC__
ibool File::AddFileInfos(const char* pszDir, std::vector<Info>& infos) {
#if defined __GNUC__
	DIR* dir = opendir(pszDir);
	if (dir == NULL) {
		Error::SetLastErrorFromErrno();
		return false;
	}
	
	size_t dirLen = strlen(pszDir);
	std::string full(pszDir);

	for (struct dirent* dp = readdir(dir); dp != NULL; dp = readdir(dir)) {
		const char* p = dp->d_name;
		if (p[0] == '.')
			continue;

		full.resize(dirLen);
		full += "/";
		full += p;

		struct stat64 s;
		if (lstat64(full.c_str(), &s) < 0) {
			continue;
		}

		Info info;
		info.FileName = p;
		info.Mode = s.st_mode;
		info.Size = s.st_size;
#if defined __USE_MISC || defined __USE_XOPEN2K8
		info.CreateTime = s.st_ctim.tv_sec;
		info.UpdateTime = s.st_mtim.tv_sec;
		info.AccessTime = s.st_atim.tv_sec;
# else
		info.CreateTime = s.st_ctime;
		info.UpdateTime = s.st_mtime;
		info.AccessTime = s.st_atime;
# endif

		infos.push_back(info);
	}
	closedir(dir);
#elif defined  _MSC_VER
#endif

	return true;
}
#endif

//! コンストラクタ
File::File() {
	m_hFile = InvalidHandle();
}

//! デストラクタ
File::~File() {
	Close();
}

//! ファイルを開く
ibool File::Open(
	const wchar_t* pszFile, //!< [in] ファイル名
	uint32_t accessFlags, //!< [in] File::Access* のフラグの組み合わせ
	uint32_t openFlags //!< [in] File::Open* のフラグ組み合わせ
) {
#if defined _MSC_VER
	int oflag = _O_BINARY;
	int shflag = _SH_DENYWR;
	int pmode = _S_IREAD | _S_IWRITE;

	if ((accessFlags & AccessWrite) && (accessFlags & AccessRead)) {
		oflag |= _O_RDWR;
	} else if (accessFlags & AccessWrite) {
		oflag |= _O_WRONLY;
	} else if (accessFlags & AccessRead) {
		oflag |= _O_RDONLY;
	}

	if (openFlags & OpenCreate) oflag |= _O_CREAT | _O_TRUNC;
	if (openFlags & OpenNew) oflag |= _O_EXCL;
	if (openFlags & OpenAppend) oflag |= _O_APPEND;

	if (_wsopen_s(&m_hFile, pszFile, oflag, shflag, pmode) != 0) {
		Error::SetLastErrorFromErrno();
		return false;
	}

	return true;
#else
#error gcc version is not implemented.
#endif
}

//! ファイルを開く
ibool File::Open(
	const char* pszFile, //!< [in] ファイル名
	uint32_t accessFlags, //!< [in] File::Access* のフラグの組み合わせ
	uint32_t openFlags //!< [in] File::Open* のフラグ組み合わせ
) {
#if defined __GNUC__
	int oflag = 0;
	int pmode = S_IRWXU | S_IWGRP | S_IROTH;

	if ((accessFlags & AccessWrite) && (accessFlags & AccessRead)) {
		oflag |= O_RDWR;
	} else if (accessFlags & AccessWrite) {
		oflag |= O_WRONLY;
	} else if (accessFlags & AccessRead) {
		oflag |= O_RDONLY;
	}

	if (openFlags & OpenCreate)
		oflag |= O_CREAT | O_TRUNC;
	if (openFlags & OpenAppend)
		oflag |= O_APPEND;

	m_hFile = open(pszFile, oflag | DEFAULT_FLAGS, pmode);
	if (!IsValidFileHandle(m_hFile)) {
		Error::SetLastErrorFromErrno();
		return false;
	}

	return true;
#else
	int oflag = _O_BINARY;
	int shflag = _SH_DENYWR;
	int pmode = _S_IREAD | _S_IWRITE;

	if ((accessFlags & AccessWrite) && (accessFlags & AccessRead)) {
		oflag |= _O_RDWR;
	} else if (accessFlags & AccessWrite) {
		oflag |= _O_WRONLY;
	} else if (accessFlags & AccessRead) {
		oflag |= _O_RDONLY;
	}

	if (openFlags & OpenCreate) oflag |= _O_CREAT | _O_TRUNC;
	if (openFlags & OpenNew) oflag |= _O_EXCL;
	if (openFlags & OpenAppend) oflag |= _O_APPEND;

	if (_sopen_s(&m_hFile, pszFile, oflag, shflag, pmode) != 0) {
		Error::SetLastErrorFromErrno();
		return false;
	}

	return true;
#endif
}

//! ファイルを閉じる
ibool File::Close() {
#if defined __GNUC__
	if (!File::IsValidFileHandle(m_hFile)) {
		Error::SetLastErrorFromErrno(EBADFD);
		return false;
	}
	if (close(m_hFile) != 0) {
		Error::SetLastErrorFromErrno();
		return false;
	}
	m_hFile = InvalidHandle();
	return true;
#else
	if (!File::IsValidFileHandle(m_hFile)) {
		Error::SetLastErrorFromErrno(EBADF);
		return false;
	}
	if (_close(m_hFile) != 0) {
		Error::SetLastErrorFromErrno();
		return false;
	}
	m_hFile = InvalidHandle();
	return true;
#endif
}

//! ファイルへ書き込む
//! @retval 書き込まれたバイト数が返る、失敗したら負数が返る
intptr_t File::Write(
	const void* pBuf, // [in] 書き込むデータ
	size_t sizeBytes // [in] 書き込むサイズ(bytes)
) {
#if defined __GNUC__
	return write(m_hFile, pBuf, sizeBytes);
#elif defined _MSC_VER
	return _write(m_hFile, pBuf, sizeBytes);
#endif
}

//! ファイルへ書き込む
intptr_t File::Write(
	const std::string& str //!< [in] 書き込むバイト配列
) {
	if(str.empty())
		return 0;
	return Write(&str[0], str.size());
}


//! ファイルから読み込む
//! @retval 書き込まれたバイト数が返る、失敗したら負数が返る
intptr_t File::Read(
	void* pBuf, // [out] 読み込まれたデータが返る
	size_t sizeBytes // [in] 読み込むサイズ(bytes)
) {
#if defined __GNUC__
	return read(m_hFile, pBuf, sizeBytes);
#elif defined _MSC_VER
	return _read(m_hFile, pBuf, sizeBytes);
#endif
}

//!　メモリー上にあるファイルの内容をストレージデバイス上のものと同期させる
ibool File::Flush() {
#if defined _MSC_VER
	if(_commit(m_hFile) != 0) {
		Error::SetLastErrorFromErrno();
		return false;
	}
#else
	if(fsync(m_hFile) != 0) {
		Error::SetLastErrorFromErrno();
		return false;
	}
#endif
	return true;
}

_JUNK_END
