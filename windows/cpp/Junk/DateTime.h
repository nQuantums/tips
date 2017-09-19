#pragma once
#ifndef __JUNK_DATETIME_H__
#define __JUNK_DATETIME_H__

#include "JunkConfig.h"
#include "JunkDef.h"

#if defined _MSC_VER
#include <Windows.h>
#else
#error gcc version is not implemented.
#endif

_JUNK_BEGIN

//! 年月日時分秒とミリ秒
struct JUNKAPICLASS DateTimeValue {
	uint16_t Year; //!< 年
	uint16_t Month; //!< 月、1～12
	uint16_t DayOfWeek; //!< 曜日、0:日曜日～6:土曜日
	uint16_t Day; //!< 日、1～31
	uint16_t Hour; //!< 時、0～23
	uint16_t Minute; //!< 分、0～59
	uint16_t Second; //!< 秒、0～59
	uint16_t Milliseconds; //!< ミリ秒、0～999

	DateTimeValue() {
	}

	DateTimeValue(uint16_t year, uint16_t month = 0, uint16_t day = 0, uint16_t hour = 0, uint16_t minute = 0, uint16_t second = 0, uint16_t msecond = 0) {
		this->Year = year;
		this->Month = month;
		this->Day = day;
		this->Hour = hour;
		this->Minute = minute;
		this->Second = second;
		this->Milliseconds = msecond;
	}
};

//! 日時
struct JUNKAPICLASS DateTime {
#if defined _MSC_VER
	uint64_t Tick; //!< 規定日時からの経過時間値

	//! ローカル時間での現在日時の取得
	static DateTime Now() {
		FILETIME ft;
		DateTime dt;
		::GetSystemTimeAsFileTime(&ft);
		::FileTimeToLocalFileTime(&ft, (FILETIME*)&dt.Tick);
		return dt;
	}

	//! UTC時間での現在日時の取得
	static DateTime NowUtc() {
		DateTime dt;
		::GetSystemTimeAsFileTime((FILETIME*)&dt.Tick);
		return dt;
	}

	//! UTC時間からローカル時間へ変換
	static DateTime UtcToLocal(const DateTime& dtutc) {
		DateTime dtlocal;
		::FileTimeToLocalFileTime((FILETIME*)&dtutc.Tick, (FILETIME*)&dtlocal.Tick);
		return dtlocal;
	}

	//! ローカル時間からUTC時間へ変換
	static DateTime LocalToUtc(const DateTime& dtlocal) {
		DateTime dtutc;
		::LocalFileTimeToFileTime((FILETIME*)&dtlocal.Tick, (FILETIME*)&dtutc.Tick);
		return dtlocal;
	}

	//! ミリ秒単位のUNIX時間から DateTime を取得
	static DateTime FromUnixTimeMs(uint64_t unixTimeMs) {
		return DateTime((unixTimeMs * 10000ULL) + 11644473600000ULL * 10000ULL);
	}

	DateTime() {
	}

	DateTime(uint64_t tick) {
		this->Tick = tick;
	}

	DateTime(const DateTimeValue& dtv) {
		SYSTEMTIME st;
		st.wYear = dtv.Year;
		st.wMonth = dtv.Month;
		st.wDay = dtv.Day;
		st.wHour = dtv.Hour;
		st.wMinute = dtv.Minute;
		st.wSecond = dtv.Second;
		st.wMilliseconds = dtv.Milliseconds;
		::SystemTimeToFileTime(&st, (FILETIME*)&this->Tick);
	}

	//! 年月日時分秒とミリ秒を取得する
	DateTimeValue Value() const {
		SYSTEMTIME st;
		DateTimeValue dtv;
		::FileTimeToSystemTime((FILETIME*)&this->Tick, &st);

		dtv.Year = st.wYear;
		dtv.Month =  st.wMonth;
		dtv.DayOfWeek = st.wDayOfWeek;
		dtv.Day = st.wDay;
		dtv.Hour = st.wHour;
		dtv.Minute = st.wMinute;
		dtv.Second = st.wSecond;
		dtv.Milliseconds = st.wMilliseconds;

		return dtv;
	}

	//! ミリ秒単位でのUNIX時間を取得する
	uint64_t UnixTimeMs() const {
		return (this->Tick - 11644473600000ULL * 10000ULL) / 10000ULL;
	}

	bool operator==(const DateTime& dt) const {
		return this->Tick == dt.Tick;
	}
	bool operator<(const DateTime& dt) const {
		return this->Tick < dt.Tick;
	}
	bool operator<=(const DateTime& dt) const {
		return this->Tick <= dt.Tick;
	}
	bool operator>(const DateTime& dt) const {
		return this->Tick > dt.Tick;
	}
	bool operator>=(const DateTime& dt) const {
		return this->Tick >= dt.Tick;
	}
#else
#error gcc version is not implemented.
#endif
};

_JUNK_END

#endif
