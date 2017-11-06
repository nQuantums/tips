// NewDeleteOverload.cpp : コンソール アプリケーションのエントリ ポイントを定義します。
//

#include "stdafx.h"
#include <exception>
#include <memory>
#include <string.h>
#include <iostream>
#include <unordered_set>


template<class T>
std::size_t HashFromNullTerminatedArray(const T* nullTerminatedArray) {
#if defined(_WIN64)
	static_assert(sizeof(size_t) == 8, "This code is for 64-bit size_t.");
	const size_t _FNV_offset_basis = 14695981039346656037ULL;
	const size_t _FNV_prime = 1099511628211ULL;
#else /* defined(_WIN64) */
	static_assert(sizeof(size_t) == 4, "This code is for 32-bit size_t.");
	const size_t _FNV_offset_basis = 2166136261U;
	const size_t _FNV_prime = 16777619U;
#endif /* defined(_WIN64) */
	auto p = nullTerminatedArray;
	if (p) {
		auto _Val = _FNV_offset_basis;
		for (; *p; ++p) {
			_Val ^= size_t(*p);
			_Val *= _FNV_prime;
		}
		return (_Val);
	} else {
		return 0;
	}
}


void* operator new(std::size_t size) {
	return malloc(size);
}
void operator delete(void* p) noexcept {
	free(p);
}

#pragma pack(push, 1)
// 不変文字列クラス
// １回のメモリ確保でメンバ変数と文字列領域を同時に確保する
template<class T> struct ConstString {
	using Char = T;
	using Self = ConstString<Char>;
	using UniquePtr = std::unique_ptr<Self>;
	using SharedPtr = std::shared_ptr<Self>;

	struct UniquePtrHasher {
		using argument_type = UniquePtr;
		using result_type = size_t;
		size_t operator()(const argument_type& _Keyval) const {
			return HashFromNullTerminatedArray(_Keyval->String());
		}
	};
	struct SharedPtrHasher {
		using argument_type = SharedPtr;
		using result_type = size_t;
		size_t operator()(const argument_type& _Keyval) const {
			return HashFromNullTerminatedArray(_Keyval->String());
		}
	};

	struct UniquePtrEquals {
		using argument_type = UniquePtr;
		using result_type = bool;
		bool operator()(const argument_type& _Left, const argument_type& _Right) const {
			return _Left->Equals(*_Right);
		}
	};
	struct SharedPtrEquals {
		using argument_type = SharedPtr;
		using result_type = bool;
		bool operator()(const argument_type& _Left, const argument_type& _Right) const {
			return _Left->Equals(*Right);
		}
	};

	// 文字列の std::unique_ptr を作成
	static std::unique_ptr<ConstString> New(const Char* string) {
		auto stringSize = GetLength(string) + 1;
		auto p = operator new(sizeof(ConstString) + stringSize);
		return std::unique_ptr<ConstString>(new (p) ConstString(stringSize, string)); // TODO: ここで例外発生したらメモリリークしてしまうのでなんとかする
	}

	// 文字列の std::shared_ptr を作成
	static std::shared_ptr<ConstString> NewShared(const Char* string) {
		auto stringSize = GetLength(string) + 1;
		auto p = operator new(sizeof(ConstString) + stringSize);
		return std::shared_ptr<ConstString>(new (p) ConstString(stringSize, string)); // TODO: ここで例外発生したらメモリリークしてしまうのでなんとかする
	}

	// 既に確保済みの文字列を指す一時的な変数を作成
	static ConstString Reference(const Char* string) {
		return ConstString(string);
	}

	ConstString() {
		_string = nullptr;
		_length = 0;
		_hash = 0;
		_owned = false;
	}
	//ConstString(const ConstString& c) {
	//	_string = c._string;
	//	_owned = false;
	//}

	//ConstString& operator=(const ConstString& c) {
	//	_string = c._string;
	//	_owned = false;
	//}

	Char operator[](intptr_t index) const {
		return _string[index];
	}

	operator const Char*() const noexcept {
		return _string;
	}
	const Char* String() const noexcept {
		return _string;
	}
	size_t Length() const noexcept {
		return _length;
	}
	size_t Hash() const noexcept {
		return _hash;
	}
	bool Equals(const Self& c) const {
		auto isNull1 = !_string;
		auto isNull2 = !c._string;
		if (isNull1 && isNull2) {
			return true;
		} else if (isNull1 != isNull2) {
			return false;
		}
		if (_length != c._length) {
			return false;
		}
		return memcmp(_string, c._string, sizeof(Char) * _length) == 0;
	}

	bool operator==(const Self& c) const {
		return Equals(c);
	}

	void operator delete(void* p) {
		if (!p) {
			return;
		}
		auto pcs = reinterpret_cast<ConstString*>(p);
		if (!pcs->_owned) {
			return;
		}
		::operator delete(p);
	}

private:
	static size_t GetLength(const Char* string) {
		if (!string) {
			return 0;
		}
		auto end = string;
		while (*end) {
			end++;
		}
		return end - string;
	}

	ConstString(const Char* string) {
		_string = string;
		_length = GetLength(string);
		_hash = HashFromNullTerminatedArray(string);
		_owned = false;
	}
	ConstString(size_t stringSize, const Char* string) noexcept {
		auto payload = reinterpret_cast<Char*>(this + 1);
		memcpy(payload, string, stringSize);
		_string = payload;
		_length = stringSize - 1;
		_hash = HashFromNullTerminatedArray(string);
		_owned = true;;
	}

	//void* operator new(std::size_t size) {
	//	return malloc(size);
	//}
	//void* operator new(std::size_t size, void* p) noexcept {
	//	return p;
	//}

	const Char* _string;
	size_t _length;
	size_t _hash;
	bool _owned;
};
#pragma pack(pop)

namespace std {
	template<> struct hash<ConstString<char>::UniquePtr> : ConstString<char>::UniquePtrHasher {};
	template<> struct hash<ConstString<char>::SharedPtr> : ConstString<char>::UniquePtrHasher {};

	template<> struct equal_to<ConstString<char>::UniquePtr> : ConstString<char>::UniquePtrEquals {};
	template<> struct equal_to<ConstString<char>::SharedPtr> : ConstString<char>::SharedPtrEquals {};

	template<class T> std::ostream& operator<<(std::ostream& os, const ConstString<T>& value) {
		return os << value.String();
	}
	template<class T> std::ostream& operator<<(std::ostream& os, const std::unique_ptr<ConstString<T>>& value) {
		return os << value->String();
	}
	template<class T> std::ostream& operator<<(std::ostream& os, const std::shared_ptr<ConstString<T>>& value) {
		return os << value->String();
	}
}

using ConstStringA = ConstString<char>;
using ConstStringW = ConstString<wchar_t>;


int main()
{
	std::unordered_set<ConstStringA::UniquePtr> texts;

	texts.insert(ConstStringA::New("afefe"));
	texts.insert(ConstStringA::New("gufefe"));
	texts.insert(ConstStringA::New("gufefe"));
	for (auto& s : texts) {
		std::cout << s << std::endl;
	}

	{
		auto cs = ConstStringA::Reference("gufefe");
		std::unique_ptr<ConstStringA> up(&cs);
		auto iter = texts.find(up);
		if (iter != texts.end()) {
			std::cout << *iter << std::endl;
		}
	}
	{
		auto cs = ConstStringA::Reference("gufefe");
		std::unique_ptr<ConstStringA> up(&cs);
		auto sp = std::shared_ptr<ConstStringA>(std::move(up));
	}

	std::shared_ptr<ConstStringA> spsave;
	{
		auto sp = ConstStringA::NewShared("afefe");
		spsave = sp;
	}



    return 0;
}

