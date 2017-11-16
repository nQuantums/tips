#pragma once
#include "Location.h"

Location::Location(File* file, const Position& pos) noexcept {
	this->pFile = file;
	this->Pos = pos;
}

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
