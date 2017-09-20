#pragma once
#include <Windows.h>
#include <iostream>
#include <string>
#include <vector>
#include <thread>
#include <mutex>
#include <memory>
#include "../CancelablePipe.h"

class PipeClient {
public:
	PipeClient();
	~PipeClient();
};

