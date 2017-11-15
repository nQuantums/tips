// NewDeleteOverload.cpp : コンソール アプリケーションのエントリ ポイントを定義します。
//

#include "stdafx.h"
#include <exception>
#include <memory>
#include <string.h>
#include <iostream>
#include <unordered_set>
#include <unordered_map>


void* operator new(std::size_t size) {
	return malloc(size);
}
void operator delete(void* p) noexcept {
	free(p);
}


template<class Derived>
std::size_t HashFromNullTerminatedArray(const Derived* nullTerminatedArray) {
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

	template<class _Other> struct rebind {	// convert this type to ConstStringAllocator<_Other>
		using other = ConstStringAllocator<_Other>;
	};

	pointer address(reference _Val) const _NOEXCEPT {	// return address of mutable _Val
		return (_STD addressof(_Val));
	}

	const_pointer address(const_reference _Val) const _NOEXCEPT {	// return address of nonmutable _Val
		return (_STD addressof(_Val));
	}

	size_t stringSize;

	ConstStringAllocator() _NOEXCEPT {	// construct default ConstStringAllocator (do nothing)
		this->stringSize = 0;
	}
	ConstStringAllocator(size_t payloadSize) _NOEXCEPT {	// construct default ConstStringAllocator (do nothing)
		this->stringSize = payloadSize;
	}

	ConstStringAllocator(const ConstStringAllocator&) _NOEXCEPT = default;
	template<class _Other> ConstStringAllocator(const ConstStringAllocator<_Other>& a) _NOEXCEPT {	// construct from a related ConstStringAllocator (do nothing)
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

#pragma pack(push, 1)
struct ConstStringDefaultBase {};

// 不変文字列クラス
// １回のメモリ確保でメンバ変数と文字列領域を同時に確保する
template<class Derived, class Base = ConstStringDefaultBase> class ConstString : public Base {
public:
	using Char = Derived;
	using Self = ConstString<Char>;
	using UniquePtr = std::unique_ptr<Self>;
	using SharedPtr = std::shared_ptr<Self>;

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

	// std::unique_ptr でラップした文字列インスタンスを作成
	template<class... Args> static std::unique_ptr<Self> New(const Char* string, Args... args) {
		return NewDerived<Self>(string, args...);
	}

	// std::shared_ptr でラップした文字列インスタンスを作成
	template<class... Args> static std::shared_ptr<Self> NewShared(const Char* string, Args... args) {
		return NewSharedDerived<Self>(string, args...);
	}


	template<class... Args> ConstString(Args... args) noexcept : Base(args...) {
		_string = nullptr;
		_length = 0;
		_hash = 0;
		_owned = false;
	}
	template<class... Args> ConstString(const Char* string, Args... args) noexcept : Base(args...) {
		_string = string;
		_length = GetLength(string);
		_hash = HashFromNullTerminatedArray(string);
		_owned = false;
	}
	template<class Derived, class... Args> ConstString(Derived* dummy, size_t stringSize, const Char* string, Args... args) noexcept : Base(args...) {
		auto payload = reinterpret_cast<Char*>(reinterpret_cast<Derived*>(this) + 1);
		memcpy(payload, string, stringSize);
		_string = payload;
		_length = stringSize - 1;
		_hash = HashFromNullTerminatedArray(string);
		_owned = true;
	}
	ConstString(const ConstString& c) = delete;
	ConstString& operator=(const ConstString& c) = delete;

	Char operator[](intptr_t index) const {
		return _string[index];
	}

	constexpr operator const Char*() const noexcept {
		return _string;
	}
	constexpr const Char* String() const noexcept {
		return _string;
	}
	constexpr size_t Length() const noexcept {
		return _length;
	}
	constexpr size_t Hash() const noexcept {
		return _hash;
	}
	bool Owned() const  noexcept {
		return _owned;
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

	// メモリ解放の必要が無いパターンもあるため delete をオーバーロードして対処する
	void operator delete(void* p) {
		if (!p) {
			return;
		}
		auto pcs = reinterpret_cast<Self*>(p);
		if (!pcs->Owned()) {
			return;
		}
		::operator delete(p);
	}

protected:
	// std::unique_ptr でラップしたインスタンスを作成
	template<class Derived, class... Args> static std::unique_ptr<Derived> NewDerived(const Char* string, Args... args) {
		auto stringSize = GetLength(string) + 1;
		auto p = operator new(sizeof(Derived) + stringSize);
		return std::unique_ptr<Derived>(new (p) Derived(reinterpret_cast<Derived*>(0), stringSize, string, args...)); // TODO: ここで例外発生したらメモリリークしてしまうのでなんとかする
	}

	// std::shared_ptr でラップしたインスタンスを作成
	template<class Derived, class... Args> static std::shared_ptr<Derived> NewSharedDerived(const Char* string, Args... args) {
		auto stringSize = GetLength(string) + 1;
		return std::allocate_shared<Derived>(ConstStringAllocator<Derived>(stringSize), reinterpret_cast<Derived*>(0), stringSize, string, args...);
	}

	const Char* _string;
	size_t _length;
	size_t _hash;
	bool _owned;
};
#pragma pack(pop)

template<class PtrType>
struct ConstStringPtrHasher {
	using argument_type = PtrType;
	using result_type = size_t;
	size_t operator()(const argument_type& _Keyval) const {
		return _Keyval->Hash();
	}
};

template<class PtrType>
struct ConstStringPtrEquals {
	using argument_type = PtrType;
	using result_type = bool;
	bool operator()(const argument_type& _Left, const argument_type& _Right) const {
		return _Left->Equals(*_Right);
	}
};


namespace std {
	template<class Char, class Base> struct hash<std::unique_ptr<ConstString<Char, Base>>> : ConstStringPtrHasher<std::unique_ptr<ConstString<Char, Base>>> {};
	template<class Char, class Base> struct equal_to<std::unique_ptr<ConstString<Char, Base>>> : ConstStringPtrEquals<std::unique_ptr<ConstString<Char, Base>>> {};
	template<class Char, class Base> struct hash<std::shared_ptr<ConstString<Char, Base>>> : ConstStringPtrHasher<std::shared_ptr<ConstString<Char, Base>>> {};
	template<class Char, class Base> struct equal_to<std::shared_ptr<ConstString<Char, Base>>> : ConstStringPtrEquals<std::shared_ptr<ConstString<Char, Base>>> {};

	template<class Char, class Base> std::ostream& operator<<(std::ostream& os, const ConstString<Char, Base>& value) {
		return os << value.String();
	}
	template<class Char, class Base> std::ostream& operator<<(std::ostream& os, const std::unique_ptr<ConstString<Char, Base>>& value) {
		return os << value->String();
	}
	template<class Char, class Base> std::ostream& operator<<(std::ostream& os, const std::shared_ptr<ConstString<Char, Base>>& value) {
		return os << value->String();
	}
}

using ConstStringA = ConstString<char>;
using ConstStringW = ConstString<wchar_t>;


#pragma pack(push, 1)
struct Position {
	union {
		struct {
			int32_t Line;
			int32_t Column;
		};
		int64_t Value;
	};

	Position() {
		this->Value = 0;
	}
	Position(int32_t line, int32_t column) {
		this->Line = line;
		this->Column = column;
	}

	constexpr size_t Hash() const noexcept {
		return static_cast<size_t>(this->Value);
	}

	bool operator==(const Position& p) const {
		return this->Value == p.Value;
	}
	bool operator!=(const Position& p) const {
		return this->Value == p.Value;
	}
};

struct PositionHasher {
	using argument_type = Position;
	using result_type = size_t;
	size_t operator()(const Position& _Keyval) const {
		return _Keyval.Hash();
	}
};

struct PositionEquals {
	using argument_type = Position;
	using result_type = bool;
	bool operator()(const argument_type& _Left, const argument_type& _Right) const {
		return _Left == _Right;
	}
};

namespace std {
	template<> struct hash<Position> : PositionHasher {};
	template<> struct equal_to<Position> : PositionEquals {};

	template<class T, class Base> std::ostream& operator<<(std::ostream& os, const Position& value) {
		return os << "(" << value.Line << ", " << value.Column << ")";
	}
}
#pragma pack(pop)


class File;
struct Location {
	File* pFile;
	Position Pos;

	Location(File* file, const Position& pos) {
		this->pFile = file;
		this->Pos = pos;
	}
};

template<class PtrType>
struct LocationPtrHasher {
	using argument_type = PtrType;
	using result_type = size_t;
	size_t operator()(const argument_type& _Keyval) const {
		return _Keyval->Pos.Value;
	}
};

template<class PtrType>
struct LocationPtrEquals {
	using argument_type = PtrType;
	using result_type = bool;
	bool operator()(const argument_type& _Left, const argument_type& _Right) const {
		return _Left->Pos == _Right->Pos;
	}
};

namespace std {
	template<> struct hash<std::unique_ptr<Location>> : LocationPtrHasher<std::unique_ptr<Location>> {};
	template<> struct equal_to<std::unique_ptr<Location>> : LocationPtrEquals<std::unique_ptr<Location>> {};
	template<> struct hash<std::shared_ptr<Location>> : LocationPtrHasher<std::shared_ptr<Location>> {};
	template<> struct equal_to<std::shared_ptr<Location>> : LocationPtrEquals<std::shared_ptr<Location>> {};

	//std::ostream& operator<<(std::ostream& os, const std::unique_ptr<Location>& value) {
	//	return os << value->pFile->String() << "<" << value->Pos.Line << ", " << value->Pos.Column << ">";
	//}
}


class File : public ConstString<char> {
public:
	File() noexcept : ConstString<char>() {}
	File(const Char* string) noexcept : ConstString<char>(string) {}
	File(File* dummy, size_t stringSize, const Char* string) noexcept : ConstString<char>(dummy, stringSize, string) {}
	~File() {

	}
	//File(const File& c) = delete;
	//File& operator=(const File& c) = delete;

	static std::unique_ptr<File> New(const Char* string) {
		return NewDerived<File>(string);
	}
	static std::shared_ptr<File> NewShared(const Char* string) {
		return NewSharedDerived<File>(string);
	}

	Location* GetLocation(const Position& pos) {
		auto iter = _Locations.find(pos);
		if (iter != _Locations.end()) {
			return iter->second.get();
		}

		auto loc = new Location(this, pos);
		_Locations[pos].reset(loc);
		//_Locations.insert(std::pair<Position, std::unique_ptr<Location>>(pos, std::make_unique<Location>(loc)));
		return loc;
	}

private:
	std::unordered_map<Position, std::unique_ptr<Location>> _Locations;
};

namespace std {
	// TEMPLATE CLASS default_delete
	template<> struct default_delete<File> {	// default deleter for unique_ptr
		constexpr default_delete() _NOEXCEPT = default;

		template<class _Ty2, class = enable_if_t<is_convertible<_Ty2 *, _Ty *>::value>> default_delete(const default_delete<_Ty2>&) _NOEXCEPT {	// construct from another default_delete
		}

		void operator()(File* _Ptr) const _NOEXCEPT {	// delete a pointer
			if (_Ptr->Owned()) {
				delete _Ptr;
			}
		}
	};

	template<> struct hash<std::unique_ptr<File>> : ConstStringPtrHasher<std::unique_ptr<File>> {};
	template<> struct equal_to<std::unique_ptr<File>> : ConstStringPtrEquals<std::unique_ptr<File>> {};
	template<> struct hash<std::shared_ptr<File>> : ConstStringPtrHasher<std::shared_ptr<File>> {};
	template<> struct equal_to<std::shared_ptr<File>> : ConstStringPtrEquals<std::shared_ptr<File>> {};

	std::ostream& operator<<(std::ostream& os, const File& value) {
		return os << value.String();
	}
	std::ostream& operator<<(std::ostream& os, const std::unique_ptr<File>& value) {
		return os << value->String();
	}
	std::ostream& operator<<(std::ostream& os, const std::shared_ptr<File>& value) {
		return os << value->String();
	}
}


class FileManager {
public:

	File* GetFile(const char* filePath) {
		File tmp(filePath);

		auto iter = _Files.find(std::unique_ptr<File>(&tmp));
		if (iter != _Files.end()) {
			return iter->get();
		}

		auto file = File::New(filePath);
		auto p = file.get();
		_Files.insert(std::move(file));
		return p;
	}

private:
	std::unordered_set<std::unique_ptr<File>> _Files;
};




int main() {
	//std::unordered_set<ConstStringA::UniquePtr> texts;

	//texts.insert(ConstStringA::New("afefe"));
	//texts.insert(ConstStringA::New("gufefe"));
	//texts.insert(ConstStringA::New("gufefe"));
	//for (auto& s : texts) {
	//	std::cout << s << std::endl;
	//}

	//{
	//	ConstStringA cs("gufefe");
	//	ConstStringA::UniquePtr up(&cs);
	//	auto iter = texts.find(up);
	//	if (iter != texts.end()) {
	//		std::cout << *iter << std::endl;
	//	}
	//}

	//ConstStringA::SharedPtr spsave;
	//{
	//	auto sp = ConstStringA::NewShared("afefe");
	//	spsave = sp;
	//}
	//std::cout << spsave << std::endl;
	//spsave = nullptr;



	{
		File tmp("afefe");
		{
			std::unique_ptr<File> upf(&tmp);
		}
	}

	//std::unique_ptr<File, std::default_delete<File>> up;


	//FileManager fm;
	//auto file = fm.GetFile("c:\\work\\test.txt");
	//auto loc = file->GetLocation(Position(1, 2));

	return 0;
}
