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

namespace NetClientApi {
	// NetClientApi —p—áŠO
	class Exception : std::exception {
	public:
		Exception(char const* const _Message) : std::exception(_Message) {
			hr_ = S_OK;
		}
		Exception(char const* const _Message, HRESULT hr) : std::exception(_Message) {
			hr_ = hr;
		}

		HRESULT GetHresult() const {
			return hr_;
		}

	protected:
		HRESULT hr_;
	};
}
