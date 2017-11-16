#pragma once
#include "Position.h"

class File;

class Location {
public:
	File* pFile;
	Position Pos;

	Location(File* file, const Position& pos) noexcept;
};
