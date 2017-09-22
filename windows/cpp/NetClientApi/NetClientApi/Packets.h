#pragma once
#include "Config.h"
#include <vector>
#include <sstream>
#include <strstream>
#include <string.h>


// パケット構造体にGUIDを付与
#define PKTID(guid) __declspec(uuid(guid))
// パケットデータ領域最小サイズ
#define MIN_PACKET_SIZE sizeof(GUID)
// パケットデータ領域最大サイズ
#define MAX_PACKET_SIZE 4096


namespace NetClientApi {
	typedef std::vector<uint8_t> PacketBuffer;
	typedef intptr_t position_t;
	typedef int32_t pktsize_t;
	struct PacketWriter;


	// アンパッキング中の例外
	class UnpackingException : Exception {
	public:
		UnpackingException(char const* const _Message) : Exception(_Message) {}
		UnpackingException(char const* const _Message, HRESULT hr) : Exception(_Message, hr) {}
	};

	// 必要な引数が見つからないというアンパッキング中の例外
	class NoArgUnpackingException : UnpackingException {
	public:
		NoArgUnpackingException(char const* const _Message) : UnpackingException(_Message) {}
		NoArgUnpackingException(char const* const _Message, HRESULT hr) : UnpackingException(_Message, hr) {}
	};


	inline void GuidToString(const GUID& guid, char buf[40]) {
		snprintf(buf, sizeof(buf), "{%08X-%04hX-%04hX-%02X%02X-%02X%02X%02X%02X%02X%02X}", guid.Data1, guid.Data2, guid.Data3, guid.Data4[0], guid.Data4[1], guid.Data4[2], guid.Data4[3], guid.Data4[4], guid.Data4[5], guid.Data4[6], guid.Data4[7]);
	}

	inline std::string GuidToString(const GUID& guid) {
		char buf[40];
		GuidToString(guid, buf);
		return buf;
	}


	// バッファへパケットをパッキングするヘルパ
	// Write() メソッドでどんどん書き込んでいき、デストラクタでサイズが計算されパケットが完成する
	struct Packer {
		PacketBuffer* buffer_ptr; // 書き込み先バッファ
		position_t start; // バッファ内でのパケット開始位置(bytes)

		__forceinline Packer(PacketBuffer& buf, const GUID& guid) {
			this->buffer_ptr = &buf;
			this->start = buf.size();

			position_t initial_size = sizeof(guid);
			buf.insert(buf.end(), reinterpret_cast<const uint8_t*>(&initial_size), reinterpret_cast<const uint8_t*>(&initial_size) + sizeof(initial_size));
			buf.insert(buf.end(), reinterpret_cast<const uint8_t*>(&guid), reinterpret_cast<const uint8_t*>(&guid) + sizeof(guid));
		}

		__forceinline ~Packer() {
			reinterpret_cast<pktsize_t&>(buffer_ptr->at(this->start)) = this->buffer_ptr->size() - this->start - sizeof(pktsize_t);
		}

		__forceinline PacketBuffer& Buffer() const {
			return *this->buffer_ptr;
		}

		// 指定値を追加しバッファ内でのオフセットを返す
		template<class T> __forceinline position_t Write(const T& value) {
			PacketBuffer& buffer = *this->buffer_ptr;
			position_t offset = buffer.size();
			buffer.insert(buffer.end(), reinterpret_cast<const uint8_t*>(&value), reinterpret_cast<const uint8_t*>(&value) + sizeof(value));
			return offset;
		}

		// 指定バッファへ文字列を追加しバッファ内でのオフセットを返す
		__forceinline position_t Write(const wchar_t* value, size_t len) {
			PacketBuffer& buffer = *this->buffer_ptr;
			position_t offset = buffer.size();
			buffer.insert(buffer.end(), reinterpret_cast<const uint8_t*>(value), reinterpret_cast<const uint8_t*>(value + len));
			return offset;
		}

		// 指定バッファへ文字列を追加しバッファ内でのオフセットを返す
		__forceinline position_t Write(const wchar_t* value) {
			return Write(value, wcslen(value));
		}

		// 指定バッファへ文字列を追加しバッファ内でのオフセットを返す
		__forceinline position_t Write(const std::wstring& value) {
			return Write(value.c_str(), value.size());
		}

		operator PacketBuffer&() {
			return *this->buffer_ptr;
		}
	};

	// バッファからパケットをアンパッキングするヘルパ
	// Read() メソッドでどんどん読み込んでいき、デストラクタで次パケットの開始位置へ読み込み位置が移動する
	// 自パケット外にアクセス発生する様な不正なバッファなら UnpackingException 例外が発生する
	struct Unpacker {
		PacketBuffer* buffer_ptr; // 書き込み先バッファ
		position_t* position_ptr; // 現在の読み込み位置(bytes)
		position_t start; // バッファ内でのパケット開始位置(bytes)
		position_t size; // GUIDとデータ部の合計サイズ(bytes)
		pktsize_t data_size; // データ部のみのサイズ(bytes)
		GUID guid; // パケット種類GUID
		position_t end; // バッファ内でのパケット終了位置+1(bytes)

		Unpacker(PacketBuffer& buf, position_t& position, const Unpacker* parent = NULL) {
			this->buffer_ptr = &buf;
			this->position_ptr = &position;

			position_t start = position;
			if (start < 0 || buf.size() < start + sizeof(pktsize_t)) {
				std::stringstream ss;
				ss << "Buffer size not enough to read packet size.\nStart position: " << start << "\nBuffer size: " << buf.size();
				throw UnpackingException(ss.str().c_str());
			}
			this->start = start;

			pktsize_t size = *reinterpret_cast<pktsize_t*>(&buf[start]);
			if (size < MIN_PACKET_SIZE || MAX_PACKET_SIZE < size) {
				std::stringstream ss;
				ss << "Invalid packet size.\nPacket size: " << size;
				throw UnpackingException(ss.str().c_str());
			}
			this->size = size;
			this->data_size = size - sizeof(GUID);
			this->guid = *reinterpret_cast<const GUID*>(&buf[start + sizeof(pktsize_t)]);

			position_t end = start + sizeof(pktsize_t) + size;
			if ((position_t)buf.size() < end) {
				std::stringstream ss;
				ss << "Buffer size not enough for packet.\nPacket GUID: " << GuidToString(this->guid) << "\nPacket end position: " << end << "\nBuffer size: " << buf.size();
				throw UnpackingException(ss.str().c_str());
			}
			if (parent && parent->end < end) {
				std::stringstream ss;
				ss << "Packet protrudes the parent packet.\nPacket GUID: " << GuidToString(this->guid) << "\nPacket end position: " << end << "\nParent packet end position: " << end;
				throw UnpackingException(ss.str().c_str());
			}
			this->end = end;

			*this->position_ptr = start + sizeof(pktsize_t) + sizeof(GUID);
		}

		__forceinline ~Unpacker() {
			*this->position_ptr = this->end;
		}

		__forceinline PacketBuffer& Buffer() const {
			return *this->buffer_ptr;
		}

		__forceinline position_t Position() const {
			return *this->position_ptr;
		}

		static __forceinline bool IsUnpackable(PacketBuffer& buf, position_t position) {
			position_t start = position;
			if (start < 0 || buf.size() < start + sizeof(pktsize_t)) {
				return false;
			}

			pktsize_t size = *reinterpret_cast<pktsize_t*>(&buf[start]);
			if (size < MIN_PACKET_SIZE || MAX_PACKET_SIZE < size) {
				return false;
			}

			position_t end = start + sizeof(pktsize_t) + size;
			if ((position_t)buf.size() < end) {
				return false;
			}

			return true;
		}

		__forceinline bool IsEndOfPacket(position_t position) const {
			return position == this->end;
		}

		__forceinline bool IsEndOfPacket() const {
			return *this->position_ptr == this->end;
		}

		// バッファ内の現在位置から指定値を取得し次の値を指すよう現在位置をずらす
		template<class T> void Read(T& value) {
			PacketBuffer& buffer = *this->buffer_ptr;
			position_t value_start = *this->position_ptr;
			position_t value_end = value_start + sizeof(value);
			if (this->end < value_end) {
				std::stringstream ss;
				ss << "Packet size not enough to read value.\nPacket GUID: " << GuidToString(this->guid) << "\nPacket end position: " << this->end << "\nValue end position: " << value_end;
				throw UnpackingException(ss.str().c_str());
			}
			*this->position_ptr = value_end;
			value = *reinterpret_cast<const T*>(&buffer[value_start]);
		}

		// バッファ内の現在位置から文字列を取得し次の値を指すよう現在位置をずらす
		void Read(size_t size, std::wstring& value) {
			PacketBuffer& buffer = *this->buffer_ptr;
			position_t value_start = *this->position_ptr;
			position_t value_end = value_start + size;
			if (this->end < value_end) {
				std::stringstream ss;
				ss << "Packet size not enough to read string.\nPacket GUID: " << GuidToString(this->guid) << "\nPacket end position: " << this->end << "\nString end position: " << value_end;
				throw UnpackingException(ss.str().c_str());
			}
			*this->position_ptr = value_end;
			value.assign(reinterpret_cast<const wchar_t*>(&buffer[value_start]), size);
		}

		// バッファ内の現在位置から文字列を取得し次の値を指すよう現在位置をずらす
		__forceinline void Read(std::wstring& value) {
			Read(this->data_size / sizeof(wchar_t), value);
		}

		operator PacketBuffer&() {
			return *this->buffer_ptr;
		}
	};


#pragma pack(push, 1)
	// パケット基本クラス。
	// ・パケット種類を識別するGUIDとデータ領域を持ち、その二つを合わせたサイズ情報持つ。
	// ・データ領域にはパケットを入れ子構造で格納可能とし、パケット以外にも int(4bytes) なども格納可能。
	// ・データ領域のバイナリイメージはパケット種類次第。
	// ・この構造体の値はヘッダの様なもの、但し継承先のメンバ変数がバイナリイメージとなるわけではない。
	// ・異なるバージョン間でもある程度互換性が保てる様にGUIDがキー、Data()の領域が値として json がバイナリになった様なイメージで使う。
	// ・引数として扱う内部のパケットは順不同、json がバイナリになった様なイメージで使うのを想定している。
	struct Packet {
		pktsize_t size; // 以降に続く(guid含む)パケット内データサイズ(bytes)
		GUID guid; // パケットの種類を示すID
	};

	// __uuidof でGUID取得できるクラス(末端の継承先クラス)を明示的に指定したパケット
	template<class _TypeOfId> struct PacketWithId : Packet {
		typedef _TypeOfId TypeOfId;

		struct MyPacker : Packer {
			__forceinline MyPacker(PacketBuffer& buf) : Packer(buf, __uuidof(_TypeOfId)) {}
		};

		__forceinline GUID& Guid() const {
			return __uuidof(_TypeOfId);
		}

		__forceinline std::string GuidString() const {
			char buf[40];
			GuidToString(guid, buf);
			return buf;
		}

		// 指定アンパッカーの現在位置パケットが読み込み可能なものか調べる
		static __forceinline bool IsReadable(Unpacker& unpacker) {
			return memcmp(&unpacker.guid, &__uuidof(_TypeOfId), sizeof(unpacker.guid)) == 0;
		}

		// 指定アンパッカーの現在位置パケットが読み込み可能らヘッダ部分を読み込む
		__forceinline bool ReadHeader(Unpacker& unpacker) {
			if (memcmp(&unpacker.guid, &__uuidof(_TypeOfId), sizeof(unpacker.guid)) != 0)
				return false;
			this->size = unpacker.size;
			this->guid = unpacker.guid;
			return true;
		}

		// 指定アンパッカー内から読み込めるものを探し出して読み込む
		inline bool FindAndRead(Unpacker& unpacker) {
			PacketBuffer& buffer = unpacker.Buffer();
			position_t position = unpacker.Position();
			position_t end = unpacker.end;
			while (position != end) {
				Unpacker tmp_unpacker(buffer, positionm, &unpacker);
				if (reinterpret_cast<_TypeOfId*>(this)->Read(tmp_unpacker)) // 継承先クラスで実装されているされている Read() で読み込む
					return true;
			}
			return false;
		}
	};

	// パケットのデータ領域に値が１つ入ってるパケット
	template<class _TypeOfId, class _ValueType> struct ValuePacket : PacketWithId<_TypeOfId> {
		typedef _ValueType ValueType;

		static __forceinline void Write(PacketBuffer& buffer, const _ValueType& value) {
			MyPacker p(buffer);
			p.Write(value);
		}
		static __forceinline bool Read(Unpacker& unpacker, ValueType& value) {
			if (!this->IsReadable(unpacker))
				return false;
			unpacker.Read(this->value);
			return true;
		}

		// 指定アンパッカー内から読み込めるものを探し出して読み込む
		static bool Value(Unpacker& unpacker, _ValueType& value) {
			PacketBuffer& buffer = unpacker.Buffer();
			position_t position = unpacker.Position();
			position_t end = unpacker.end;
			while (position != end) {
				Unpacker tmp_unpacker(buffer, position, &unpacker);
				if (_TypeOfId::IsReadable(tmp_unpacker)) {
					tmp_unpacker.Read(value);
					return true;
				}
			}
			return false;
		}

		// 指定アンパッカー内から読み込めるものを探し出して返す、見つからなかったら NoArgUnpackingException 例外が発生する
		static _ValueType Value(Unpacker& unpacker) {
			PacketBuffer& buffer = unpacker.Buffer();
			position_t position = unpacker.Position();
			position_t end = unpacker.end;
			while (position != end) {
				Unpacker tmp_unpacker(buffer, position, &unpacker);
				if (_TypeOfId::IsReadable(tmp_unpacker)) {
					_ValueType value;
					tmp_unpacker.Read(value);
					return value;
				}
			}
			throw NoArgUnpackingException(GuidToString(__uuidof(_TypeOfId)).c_str());
		}
	};
#pragma pack(pop)
}
