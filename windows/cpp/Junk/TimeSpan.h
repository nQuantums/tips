#pragma once
#ifndef __JUNK_TIMESPAN_H__
#define __JUNK_TIMESPAN_H__

#include "JunkConfig.h"
#include "JunkDef.h"

#if defined _MSC_VER
#include <Windows.h>
#else
#error gcc version is not implemented.
#endif

_JUNK_BEGIN

//! 時間長
struct TimeSpan {
#if defined _MSC_VER
#else
#error gcc version is not implemented.
#endif
};

_JUNK_END

#endif
