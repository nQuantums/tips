#pragma once
#include <Windows.h>
#include <assert.h>
#include <ctype.h>
#include <stddef.h>
#if defined(_MSC_VER) && _MSC_VER <= 1500
typedef signed char        int8_t;
typedef short              int16_t;
typedef int                int32_t;
typedef long long          int64_t;
typedef unsigned char      uint8_t;
typedef unsigned short     uint16_t;
typedef unsigned int       uint32_t;
typedef unsigned long long uint64_t;

typedef signed char        int_least8_t;
typedef short              int_least16_t;
typedef int                int_least32_t;
typedef long long          int_least64_t;
typedef unsigned char      uint_least8_t;
typedef unsigned short     uint_least16_t;
typedef unsigned int       uint_least32_t;
typedef unsigned long long uint_least64_t;

typedef signed char        int_fast8_t;
typedef int                int_fast16_t;
typedef int                int_fast32_t;
typedef long long          int_fast64_t;
typedef unsigned char      uint_fast8_t;
typedef unsigned int       uint_fast16_t;
typedef unsigned int       uint_fast32_t;
typedef unsigned long long uint_fast64_t;
#else
#include <stdint.h>
#endif
#include <exception>
#include <vector>

#pragma pack(push, 1)
// コンストラクタ無しの１バイトデータ
struct ByteItem {
	uint8_t value;

	__forceinline ByteItem() {}
};
#pragma pack(pop)

// バッファ拡張時に0埋めを行わないバッファ
typedef std::vector<ByteItem> ByteBuffer;

// HRESULT を持つ例外
class Exception : public std::exception {
public:
	Exception(char const* const _Message) : std::exception(_Message) {
		hr_ = HRESULT_FROM_WIN32(::GetLastError());
	}
	Exception(char const* const _Message, HRESULT hr) : std::exception(_Message) {
		hr_ = hr;
	}

	HRESULT Hresult() const {
		return hr_;
	}

	std::string MessageFromHresultA() const {
		std::string message;
		LPVOID p;
		::FormatMessageA(
			FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM,
			NULL,
			hr_,
			MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
			(LPSTR)&p,
			0,
			NULL);
		if (p != NULL)
			message = (LPCSTR)p;
		::LocalFree(p);
		return std::move(message);
	}

protected:
	HRESULT hr_;
};

// 処理中止を示す例外
class AbortException : public Exception {
public:
	AbortException(char const* const _Message) : Exception(_Message) {}
	AbortException(char const* const _Message, HRESULT hr) : Exception(_Message, hr) {}
};
