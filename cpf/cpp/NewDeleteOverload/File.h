#pragma once
#include <unordered_map>
#include "ConstString.h"
#include "Position.h"

class File;
class Location;

class File {
public:
	File(ConstStringA* path) noexcept {
		_Path = path;
	}

	ConstStringA* Path() const noexcept {
		return _Path;
	}
	Location* GetLocation(const Position& pos);

private:
	ConstStringA* _Path;
	std::unordered_map<Position, std::unique_ptr<Location>> _Locations;
};
