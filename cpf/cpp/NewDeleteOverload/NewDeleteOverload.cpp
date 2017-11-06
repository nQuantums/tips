// NewDeleteOverload.cpp : コンソール アプリケーションのエントリ ポイントを定義します。
//

#include "stdafx.h"
#include <exception>
#include <memory>
#include <string.h>
#include <iostream>


void* operator new(std::size_t size) {
	return malloc(size);
}
void operator delete(void* p) noexcept {
	free(p);
}

struct ConstString {
	ConstString(const char* msg) {
		_string = msg;
	}

	static std::unique_ptr<ConstString> New(const char* string) {
		auto size = sizeof(((ConstString*)0)->_string);
		auto len = std::strlen(string);
		size += len + 1;
		auto p = operator new(size);
		return std::unique_ptr<ConstString>(new (p) ConstString(len, string));
	}

	operator const char*() const {
		return _string;
	}
	const char* String() const {
		return _string;
	}

private:
	ConstString(size_t len, const char* msg) {
		auto p = reinterpret_cast<char*>(&_string + 1);
		memcpy(p, msg, len + 1);
		_string = p;
	}

	const char* _string;
};


int main()
{
	auto p = ConstString::New("ptr");
	auto r = ConstString("reference");
	std::cout << p->String() << std::endl;
	std::cout << r.String() << std::endl;
	return 0;
}

