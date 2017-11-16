#pragma once
#include <functional>
#include <ostream>

#pragma pack(push, 1)
struct Position {
	union {
		struct {
			int32_t Line;
			int32_t Column;
		};
		int64_t Value;
	};

	Position() noexcept {
		this->Value = 0;
	}
	Position(int32_t line, int32_t column) noexcept {
		this->Line = line;
		this->Column = column;
	}

	constexpr size_t Hash() const noexcept {
		return static_cast<size_t>(this->Value);
	}

	bool operator==(const Position& p) const noexcept {
		return this->Value == p.Value;
	}
	bool operator!=(const Position& p) const noexcept {
		return this->Value == p.Value;
	}
};

struct PositionHasher {
	using argument_type = Position;
	using result_type = size_t;
	size_t operator()(const Position& _Keyval) const noexcept {
		return _Keyval.Hash();
	}
};

struct PositionEquals {
	using argument_type = Position;
	using result_type = bool;
	bool operator()(const argument_type& _Left, const argument_type& _Right) const noexcept {
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
