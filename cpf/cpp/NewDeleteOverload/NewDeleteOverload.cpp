// NewDeleteOverload.cpp : コンソール アプリケーションのエントリ ポイントを定義します。
//

#include "stdafx.h"
#include <exception>
#include <memory>
#include <string.h>
#include <iostream>
#include <unordered_set>


void* operator new(std::size_t size) {
	return malloc(size);
}
void operator delete(void* p) noexcept {
	free(p);
}


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

#pragma pack(push, 1)
// 不変文字列クラス
// １回のメモリ確保でメンバ変数と文字列領域を同時に確保する
template<class T> class ConstString {
public:
	template<class _Ty> friend class ConstStringAllocator;

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

	// 文字列の std::unique_ptr を作成
	static std::unique_ptr<Self> New(const Char* string) {
		auto stringSize = GetLength(string) + 1;
		auto p = operator new(sizeof(Self) + stringSize);
		return std::unique_ptr<Self>(new (p) Self(stringSize, string)); // TODO: ここで例外発生したらメモリリークしてしまうのでなんとかする
	}

	// 文字列の std::shared_ptr を作成
	static std::shared_ptr<Self> NewShared(const Char* string) {
		auto stringSize = GetLength(string) + 1;
		struct Bridge : public Self {
			Bridge(size_t stringSize, const Char* string) noexcept : Self(stringSize, string) {}
			void operator delete(void* p) {
				if (!p) {
					return;
				}
				auto pcs = reinterpret_cast<Self*>(p);
				if (!pcs->_owned) {
					return;
				}
				::operator delete(p);
			}
		};
		return std::allocate_shared<Bridge>(ConstStringAllocator<Bridge>(stringSize), stringSize, string);
	}


	ConstString() {
		_string = nullptr;
		_length = 0;
		_hash = 0;
		_owned = false;
	}
	ConstString(const Char* string) {
		_string = string;
		_length = GetLength(string);
		_hash = HashFromNullTerminatedArray(string);
		_owned = false;
	}
	ConstString(const ConstString& c) = delete;
	ConstString& operator=(const ConstString& c) = delete;

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

	// ConstString(const Char* string) コンストラクタで生成されたものはメモリ解放の必要が無いため delete をオーバーロードして対処する
	void operator delete(void* p) {
		if (!p) {
			return;
		}
		auto pcs = reinterpret_cast<Self*>(p);
		if (!pcs->_owned) {
			return;
		}
		::operator delete(p);
	}

private:
	ConstString(size_t stringSize, const Char* string) noexcept {
		auto payload = reinterpret_cast<Char*>(this + 1);
		memcpy(payload, string, stringSize);
		_string = payload;
		_length = stringSize - 1;
		_hash = HashFromNullTerminatedArray(string);
		_owned = true;
	}

	const Char* _string;
	size_t _length;
	size_t _hash;
	bool _owned;
};
#pragma pack(pop)


template<class _Ty> class ConstStringAllocator {
public:
	using _Not_user_specialized = void;

	using value_type = _Ty;

	using pointer = value_type *;
	using const_pointer = const value_type *;

	using reference = value_type&;
	using const_reference = const value_type&;

	using size_type = size_t;
	using difference_type = ptrdiff_t;

	using propagate_on_container_move_assignment = std::true_type;
	using is_always_equal = std::true_type;

	template<class _Other> struct rebind {	// convert this type to PayloadAllocator<_Other>
		using other = ConstStringAllocator<_Other>;
	};

	pointer address(reference _Val) const _NOEXCEPT {	// return address of mutable _Val
		return (_STD addressof(_Val));
	}

	const_pointer address(const_reference _Val) const _NOEXCEPT {	// return address of nonmutable _Val
		return (_STD addressof(_Val));
	}

	size_t stringSize;

	ConstStringAllocator() _NOEXCEPT {	// construct default PayloadAllocator (do nothing)
		this->stringSize = 0;
	}
	ConstStringAllocator(size_t payloadSize) _NOEXCEPT {	// construct default PayloadAllocator (do nothing)
		this->stringSize = payloadSize;
	}

	ConstStringAllocator(const ConstStringAllocator&) _NOEXCEPT = default;
	template<class _Other> ConstStringAllocator(const ConstStringAllocator<_Other>& a) _NOEXCEPT {	// construct from a related PayloadAllocator (do nothing)
		this->stringSize = a.stringSize;
	}

	void deallocate(const pointer _Ptr, const size_type _Count) {	// deallocate object at _Ptr
		operator delete(_Ptr);
	}

	_DECLSPEC_ALLOCATOR pointer allocate(_CRT_GUARDOVERFLOW const size_type _Count) {	// allocate array of _Count elements
		return static_cast<pointer>(operator new(_Count * (sizeof(_Ty) + this->stringSize)));
	}

	_DECLSPEC_ALLOCATOR pointer allocate(_CRT_GUARDOVERFLOW const size_type _Count, const void *) {	// allocate array of _Count elements, ignore hint
		return allocate(_Count);
	}

	template<class _Objty, class... _Types> void construct(_Objty * const _Ptr, _Types&&... _Args) {	// construct _Objty(_Types...) at _Ptr
		new (const_cast<void *>(static_cast<const volatile void *>(_Ptr))) _Objty(_STD forward<_Types>(_Args)...);
	}

	template<class _Uty> void destroy(_Uty * const _Ptr) {	// destroy object at _Ptr
		_Ptr->~_Uty();
	}

	size_t max_size() const _NOEXCEPT {	// estimate maximum array size
		return (static_cast<size_t>(-1) / sizeof(_Ty));
	}
};

template<class _Ty, class _Other> inline bool operator==(const ConstStringAllocator<_Ty>&, const ConstStringAllocator<_Other>&) _NOEXCEPT {
	return (true);
}

template<class _Ty, class _Other> inline bool operator!=(const ConstStringAllocator<_Ty>&, const ConstStringAllocator<_Other>&) _NOEXCEPT {
	return (false);
}


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


int main() {
	std::unordered_set<ConstStringA::UniquePtr> texts;

	texts.insert(ConstStringA::New("afefe"));
	texts.insert(ConstStringA::New("gufefe"));
	texts.insert(ConstStringA::New("gufefe"));
	for (auto& s : texts) {
		std::cout << s << std::endl;
	}

	{
		ConstStringA cs("gufefe");
		ConstStringA::UniquePtr up(&cs);
		auto iter = texts.find(up);
		if (iter != texts.end()) {
			std::cout << *iter << std::endl;
		}
	}

	ConstStringA::SharedPtr spsave;
	{
		auto sp = ConstStringA::NewShared("afefe");
		spsave = sp;
	}
	std::cout << spsave << std::endl;
	spsave = nullptr;

	return 0;
}
