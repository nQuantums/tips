#pragma once
#include <unordered_map>
#include "File.h"

class FileManager {
public:

	File* GetFile(const char* filePath) {
		ConstStringA key(filePath);

		auto iter = _Files.find(std::unique_ptr<ConstStringA>(&key));
		if (iter != _Files.end()) {
			return iter->second.get();
		}

		auto path = ConstStringA::New(filePath);
		auto file = new File(path.get());
		_Files[std::move(path)] = std::unique_ptr<File>(file);
		return file;
	}

private:
	std::unordered_map<std::unique_ptr<ConstStringA>, std::unique_ptr<File>> _Files;
};

#include "FileImpl.h"
#include "LocationImpl.h"
