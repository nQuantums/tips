#pragma once
#include "File.h"
#include "LocationImpl.h"

namespace std {
	inline std::ostream& operator<<(std::ostream& os, const File& value) {
		return os << value.Path();
	}
	inline std::ostream& operator<<(std::ostream& os, const File* value) {
		return os << value->Path();
	}
	inline std::ostream& operator<<(std::ostream& os, const std::unique_ptr<File>& value) {
		return os << value->Path();
	}
	inline std::ostream& operator<<(std::ostream& os, const std::shared_ptr<File>& value) {
		return os << value->Path();
	}
}

Location* File::GetLocation(const Position& pos) {
	auto iter = _Locations.find(pos);
	if (iter != _Locations.end()) {
		return iter->second.get();
	}

	auto loc = new Location(this, pos);
	_Locations[pos].reset(loc);
	//_Locations.insert(std::pair<Position, std::unique_ptr<Location>>(pos, std::make_unique<Location>(loc)));
	return loc;
}
