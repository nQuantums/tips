#pragma once
#ifndef __JUNK_CLOCK_H__
#define __JUNK_CLOCK_H__

#include "JunkConfig.h"

_JUNK_BEGIN

//! 時間取得
struct JUNKAPICLASS Clock {
	static int64_t SysNS(); //!< システム時間の取得(nsec)
	static double SysS(); //!< システム時間の取得(sec)
};

_JUNK_END

#endif
