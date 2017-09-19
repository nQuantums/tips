#include "Encoding.h"

_JUNK_BEGIN


//! バイト配列から文字列の取得
void Encoding::GetString(const char* bytes, size_t size, std::wstring& str) const {
	size_t len = ::MultiByteToWideChar((UINT)this->CodePage, (DWORD)this->Flags, bytes, (int)size, NULL, 0);
	str.resize(len);
	if(len == 0)
		return;
	len = (size_t)::MultiByteToWideChar((UINT)this->CodePage, (DWORD)this->Flags, bytes, (int)size, &str[0], (int)len);
	str.resize(len);
}

//! バイト配列から文字列の取得
void Encoding::GetString(const char* bytes, std::wstring& str) const {
	GetString(bytes, strlen(bytes), str);
}

//! バイト配列から文字列の取得
void Encoding::GetString(const std::string& bytes, std::wstring& str) const {
	if(bytes.empty())
		return;
	GetString(&bytes[0], bytes.size(), str);
}

//! バイト配列から文字列の取得
std::wstring Encoding::GetString(const char* bytes, size_t size) const {
	std::wstring str;
	GetString(bytes, size, str);
#if 1700 <= _MSC_VER
	return std::move(str);
#else
	return str;
#endif
}

//! バイト配列から文字列の取得
std::wstring Encoding::GetString(const char* bytes) const {
	std::wstring str;
	GetString(bytes, strlen(bytes), str);
#if 1700 <= _MSC_VER
	return std::move(str);
#else
	return str;
#endif
}

//! バイト配列から文字列の取得
std::wstring Encoding::GetString(const std::string& bytes) const {
	std::wstring str;
	GetString(&bytes[0], bytes.size(), str);
#if 1700 <= _MSC_VER
	return std::move(str);
#else
	return str;
#endif
}

//! 文字列からバイト配列の取得
void Encoding::GetBytes(const wchar_t* str, size_t strLen, std::string& bytes) const {
	size_t len = ::WideCharToMultiByte((UINT)this->CodePage, (DWORD)this->Flags, str, (int)strLen, NULL, 0, NULL, NULL);
	bytes.resize(len);
	if(len == 0)
		return;
	len = (size_t)::WideCharToMultiByte((UINT)this->CodePage, (DWORD)this->Flags, str, (int)strLen, &bytes[0], (int)len, NULL, NULL);
	bytes.resize(len);
}

//! 文字列からバイト配列の取得
void Encoding::GetBytes(const wchar_t* str, std::string& bytes) const {
	GetBytes(str, ::wcslen(str), bytes);
}

//! 文字列からバイト配列の取得
void Encoding::GetBytes(const std::wstring& str, std::string& bytes) const {
	if(str.empty())
		return;
	GetBytes(str.c_str(), str.size(), bytes);
}

//! 文字列からバイト配列の取得
std::string Encoding::GetBytes(const wchar_t* str, size_t strLen) const {
	std::string bytes;
	GetBytes(str, strLen, bytes);
#if 1700 <= _MSC_VER
	return std::move(bytes);
#else
	return bytes;
#endif
}

//! 文字列からバイト配列の取得
std::string Encoding::GetBytes(const wchar_t* str) const {
	std::string bytes;
	GetBytes(str, ::wcslen(str), bytes);
#if 1700 <= _MSC_VER
	return std::move(bytes);
#else
	return bytes;
#endif
}

//! 文字列からバイト配列の取得
std::string Encoding::GetBytes(const std::wstring& str) const {
	if(str.empty())
		return std::string();
	std::string bytes;
	GetBytes(&str[0], str.size(), bytes);
#if 1700 <= _MSC_VER
	return std::move(bytes);
#else
	return bytes;
#endif
}


_JUNK_END
