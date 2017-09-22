#pragma once
#include "Config.h"
#include <vector>
#include <sstream>
#include <strstream>
#include <string.h>


// �p�P�b�g�\���̂�GUID��t�^
#define PKTID(guid) __declspec(uuid(guid))
// �p�P�b�g�f�[�^�̈�ŏ��T�C�Y
#define MIN_PACKET_SIZE sizeof(GUID)
// �p�P�b�g�f�[�^�̈�ő�T�C�Y
#define MAX_PACKET_SIZE 4096


namespace NetClientApi {
	typedef std::vector<uint8_t> PacketBuffer;
	typedef intptr_t position_t;
	typedef int32_t pktsize_t;
	struct PacketWriter;


	// �A���p�b�L���O���̗�O
	class UnpackingException : Exception {
	public:
		UnpackingException(char const* const _Message) : Exception(_Message) {}
		UnpackingException(char const* const _Message, HRESULT hr) : Exception(_Message, hr) {}
	};

	// �K�v�Ȉ�����������Ȃ��Ƃ����A���p�b�L���O���̗�O
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


	// �o�b�t�@�փp�P�b�g���p�b�L���O����w���p
	// Write() ���\�b�h�łǂ�ǂ񏑂�����ł����A�f�X�g���N�^�ŃT�C�Y���v�Z����p�P�b�g����������
	struct Packer {
		PacketBuffer* buffer_ptr; // �������ݐ�o�b�t�@
		position_t start; // �o�b�t�@���ł̃p�P�b�g�J�n�ʒu(bytes)

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

		// �w��l��ǉ����o�b�t�@���ł̃I�t�Z�b�g��Ԃ�
		template<class T> __forceinline position_t Write(const T& value) {
			PacketBuffer& buffer = *this->buffer_ptr;
			position_t offset = buffer.size();
			buffer.insert(buffer.end(), reinterpret_cast<const uint8_t*>(&value), reinterpret_cast<const uint8_t*>(&value) + sizeof(value));
			return offset;
		}

		// �w��o�b�t�@�֕������ǉ����o�b�t�@���ł̃I�t�Z�b�g��Ԃ�
		__forceinline position_t Write(const wchar_t* value, size_t len) {
			PacketBuffer& buffer = *this->buffer_ptr;
			position_t offset = buffer.size();
			buffer.insert(buffer.end(), reinterpret_cast<const uint8_t*>(value), reinterpret_cast<const uint8_t*>(value + len));
			return offset;
		}

		// �w��o�b�t�@�֕������ǉ����o�b�t�@���ł̃I�t�Z�b�g��Ԃ�
		__forceinline position_t Write(const wchar_t* value) {
			return Write(value, wcslen(value));
		}

		// �w��o�b�t�@�֕������ǉ����o�b�t�@���ł̃I�t�Z�b�g��Ԃ�
		__forceinline position_t Write(const std::wstring& value) {
			return Write(value.c_str(), value.size());
		}

		operator PacketBuffer&() {
			return *this->buffer_ptr;
		}
	};

	// �o�b�t�@����p�P�b�g���A���p�b�L���O����w���p
	// Read() ���\�b�h�łǂ�ǂ�ǂݍ���ł����A�f�X�g���N�^�Ŏ��p�P�b�g�̊J�n�ʒu�֓ǂݍ��݈ʒu���ړ�����
	// ���p�P�b�g�O�ɃA�N�Z�X��������l�ȕs���ȃo�b�t�@�Ȃ� UnpackingException ��O����������
	struct Unpacker {
		PacketBuffer* buffer_ptr; // �������ݐ�o�b�t�@
		position_t* position_ptr; // ���݂̓ǂݍ��݈ʒu(bytes)
		position_t start; // �o�b�t�@���ł̃p�P�b�g�J�n�ʒu(bytes)
		position_t size; // GUID�ƃf�[�^���̍��v�T�C�Y(bytes)
		pktsize_t data_size; // �f�[�^���݂̂̃T�C�Y(bytes)
		GUID guid; // �p�P�b�g���GUID
		position_t end; // �o�b�t�@���ł̃p�P�b�g�I���ʒu+1(bytes)

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

		// �o�b�t�@���̌��݈ʒu����w��l���擾�����̒l���w���悤���݈ʒu�����炷
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

		// �o�b�t�@���̌��݈ʒu���當������擾�����̒l���w���悤���݈ʒu�����炷
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

		// �o�b�t�@���̌��݈ʒu���當������擾�����̒l���w���悤���݈ʒu�����炷
		__forceinline void Read(std::wstring& value) {
			Read(this->data_size / sizeof(wchar_t), value);
		}

		operator PacketBuffer&() {
			return *this->buffer_ptr;
		}
	};


#pragma pack(push, 1)
	// �p�P�b�g��{�N���X�B
	// �E�p�P�b�g��ނ����ʂ���GUID�ƃf�[�^�̈�������A���̓�����킹���T�C�Y��񎝂B
	// �E�f�[�^�̈�ɂ̓p�P�b�g�����q�\���Ŋi�[�\�Ƃ��A�p�P�b�g�ȊO�ɂ� int(4bytes) �Ȃǂ��i�[�\�B
	// �E�f�[�^�̈�̃o�C�i���C���[�W�̓p�P�b�g��ގ���B
	// �E���̍\���̂̒l�̓w�b�_�̗l�Ȃ��́A�A���p����̃����o�ϐ����o�C�i���C���[�W�ƂȂ�킯�ł͂Ȃ��B
	// �E�قȂ�o�[�W�����Ԃł�������x�݊������ۂĂ�l��GUID���L�[�AData()�̗̈悪�l�Ƃ��� json ���o�C�i���ɂȂ����l�ȃC���[�W�Ŏg���B
	// �E�����Ƃ��Ĉ��������̃p�P�b�g�͏��s���Ajson ���o�C�i���ɂȂ����l�ȃC���[�W�Ŏg���̂�z�肵�Ă���B
	struct Packet {
		pktsize_t size; // �ȍ~�ɑ���(guid�܂�)�p�P�b�g���f�[�^�T�C�Y(bytes)
		GUID guid; // �p�P�b�g�̎�ނ�����ID
	};

	// __uuidof ��GUID�擾�ł���N���X(���[�̌p����N���X)�𖾎��I�Ɏw�肵���p�P�b�g
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

		// �w��A���p�b�J�[�̌��݈ʒu�p�P�b�g���ǂݍ��݉\�Ȃ��̂����ׂ�
		static __forceinline bool IsReadable(Unpacker& unpacker) {
			return memcmp(&unpacker.guid, &__uuidof(_TypeOfId), sizeof(unpacker.guid)) == 0;
		}

		// �w��A���p�b�J�[�̌��݈ʒu�p�P�b�g���ǂݍ��݉\��w�b�_������ǂݍ���
		__forceinline bool ReadHeader(Unpacker& unpacker) {
			if (memcmp(&unpacker.guid, &__uuidof(_TypeOfId), sizeof(unpacker.guid)) != 0)
				return false;
			this->size = unpacker.size;
			this->guid = unpacker.guid;
			return true;
		}

		// �w��A���p�b�J�[������ǂݍ��߂���̂�T���o���ēǂݍ���
		inline bool FindAndRead(Unpacker& unpacker) {
			PacketBuffer& buffer = unpacker.Buffer();
			position_t position = unpacker.Position();
			position_t end = unpacker.end;
			while (position != end) {
				Unpacker tmp_unpacker(buffer, positionm, &unpacker);
				if (reinterpret_cast<_TypeOfId*>(this)->Read(tmp_unpacker)) // �p����N���X�Ŏ�������Ă��邳��Ă��� Read() �œǂݍ���
					return true;
			}
			return false;
		}
	};

	// �p�P�b�g�̃f�[�^�̈�ɒl���P�����Ă�p�P�b�g
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

		// �w��A���p�b�J�[������ǂݍ��߂���̂�T���o���ēǂݍ���
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

		// �w��A���p�b�J�[������ǂݍ��߂���̂�T���o���ĕԂ��A������Ȃ������� NoArgUnpackingException ��O����������
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
