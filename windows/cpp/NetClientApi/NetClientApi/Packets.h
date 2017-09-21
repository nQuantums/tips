#pragma once
#include "Config.h"
#include <Windows.h>
#include <vector>
#include <string.h>


// �p�P�b�g�\���̂�GUID��t�^
#define PKTID(guid) __declspec(uuid(guid))


namespace NetClientApi {
#pragma pack(push, 1)

	typedef std::vector<uint8_t> PacketBuffer;
	typedef uint32_t pktsize_t;
	struct PacketWriter;


	// �w��o�b�t�@�֎w��l��ǉ����o�b�t�@���ł̃I�t�Z�b�g��Ԃ�
	template<class T> __forceinline size_t ToBuffer(PacketBuffer& buffer, const T& value) {
		size_t offset = buffer.size();
		buffer.insert(buffer.end(), reinterpret_cast<const uint8_t*>(&value), reinterpret_cast<const uint8_t*>(&value) + sizeof(value));
		return offset;
	}

	// �w��o�b�t�@�֕������ǉ����o�b�t�@���ł̃I�t�Z�b�g��Ԃ�
	__forceinline size_t ToBuffer(PacketBuffer& buffer, const char* value) {
		size_t offset = buffer.size();
		buffer.insert(buffer.end(), reinterpret_cast<const uint8_t*>(value), reinterpret_cast<const uint8_t*>(value + strlen(value)));
		return offset;
	}

	// �w��o�b�t�@�֕������ǉ����o�b�t�@���ł̃I�t�Z�b�g��Ԃ�
	__forceinline size_t ToBuffer(PacketBuffer& buffer, const wchar_t* value) {
		size_t offset = buffer.size();
		buffer.insert(buffer.end(), reinterpret_cast<const uint8_t*>(value), reinterpret_cast<const uint8_t*>(value + wcslen(value)));
		return offset;
	}


	struct PacketWriterInternal {
		PacketBuffer* buffer_ptr; // �������ݐ�o�b�t�@
		size_t offset; // �o�b�t�@���ł̃p�P�b�g�T�C�Y�̈ʒu

		__forceinline PacketWriterInternal(const PacketWriterInternal& tpw) {
			*this = tpw;
		}
		__forceinline PacketWriterInternal(PacketBuffer& buf) {
			this->buffer_ptr = &buf;
			this->offset = buf.size();
			buf.resize(this->offset + sizeof(pktsize_t)); // �Ƃ肠�����p�P�b�g�T�C�Y�p�̗̈�m��
		}

		// �w��l��ǉ����o�b�t�@���ł̃I�t�Z�b�g��Ԃ�
		template<class T> __forceinline size_t Write(const T& value) {
			return ToBuffer(*this->buffer_ptr, value);
		}

		// �w��o�b�t�@�֕������ǉ����o�b�t�@���ł̃I�t�Z�b�g��Ԃ�
		__forceinline size_t Write(const char* value) {
			return ToBuffer(*this->buffer_ptr, value);
		}

		// �w��o�b�t�@�֕������ǉ����o�b�t�@���ł̃I�t�Z�b�g��Ԃ�
		__forceinline size_t Write(const wchar_t* value) {
			return ToBuffer(*this->buffer_ptr, value);
		}


		operator PacketBuffer&() {
			return *this->buffer_ptr;
		}
	};

	// �w��o�b�t�@�ւ̃p�P�b�g�������݂�⏕����
	// ���p�P�b�g�T�C�Y�̓p�P�b�g���e�������݊������܂ŕs���Ȃ̂ŁA�R���X�g���N�^�Ńp�P�b�g�쐬�J�n���f�X�g���N�^�̃^�C�~���O�ł̓T�C�Y�m�肵�Ă���̂ł���𗘗p����
	struct PacketWriter : PacketWriterInternal {
		__forceinline PacketWriter(PacketBuffer& buf) : PacketWriterInternal(buf) {
		}
		__forceinline ~PacketWriter() {
			reinterpret_cast<pktsize_t&>(buffer_ptr->at(this->offset)) = this->buffer_ptr->size() - this->offset - sizeof(pktsize_t);
		}
	};


	// �p�P�b�g��{�\���́A�������Ƀt�H�[�}�b�g���������邽�߂Ɏg�p����
	struct Packet {
		pktsize_t size; // �ȍ~�ɑ���(guid�܂�)�p�P�b�g���f�[�^�T�C�Y(bytes)
		GUID guid; // �p�P�b�g�t�H�[�}�b�gID

				   // �p�P�b�g�f�[�^�̈�ւ̃|�C���^�擾
		__forceinline const void* Data() const {
			return reinterpret_cast<const uint8_t*>(this) + sizeof(*this);
		}
		// �p�P�b�g�f�[�^�̈�ւ̃|�C���^�擾
		__forceinline void* Data() {
			return reinterpret_cast<uint8_t*>(this) + sizeof(*this);
		}

		template<class PacketType> struct Packer : PacketWriter {
			__forceinline Packer(PacketBuffer& buf) : PacketWriter(buf) {
				Write(__uuidof(PacketType));
			}
		};
	};

	// �l�p�P�b�g
	template<class ValueType> struct ValuePacket : Packet {
		__forceinline const ValueType& Value() const {
			return *reinterpret_cast<const ValueType*>(this->Data());
		}
		__forceinline ValueType& Value() {
			return *reinterpret_cast<ValueType*>(this->Data());
		}

		template<class PacketType> static __forceinline void Write(PacketBuffer& buffer, const ValueType& value) {
			Packet::Packer<PacketType> p(buffer);
			p.Write(value);
		}
	};

	// ������p�P�b�g
	struct StringPacket : Packet {
		__forceinline const wchar_t* Value() const {
			return reinterpret_cast<const wchar_t*>(this->Data());
		}
		__forceinline wchar_t* Value() {
			return reinterpret_cast<wchar_t*>(this->Data());
		}

		template<class PacketType> static __forceinline void Write(PacketBuffer& buffer, const wchar_t* value) {
			Packet::Packer<PacketType> p(buffer);
			p.Write(value);
		}
	};

	// �t�@�C��������
	struct PKTID("4D844928-3409-4b99-90C6-D144B3734C90") FilePathArg : StringPacket {
		static __forceinline void Write(PacketBuffer& buffer, const wchar_t* value) {
			StringPacket::Write<FilePathArg>(buffer, value);
		}
	};

	// �n�b�V���l����
	struct PKTID("7CB273A6-50F2-4343-8AF4-7F6200BE8FAD") HashValueArg : StringPacket {
		static __forceinline void Write(PacketBuffer& buffer, const wchar_t* value) {
			StringPacket::Write<HashValueArg>(buffer, value);
		}
	};

	// BOOL �l����
	struct PKTID("3508B202-B300-4DD2-9BC3-0275C2448645") BoolArg : ValuePacket<BOOL> {
		static __forceinline void Write(PacketBuffer& buffer, BOOL value) {
			ValuePacket<BOOL>::Write<BoolArg>(buffer, value);
		}
	};

	// HRESULT �l����
	struct PKTID("7496B65F-51BF-44e8-A741-D4D576301018") HresultArg : ValuePacket<HRESULT> {
		static __forceinline void Write(PacketBuffer& buffer, HRESULT value) {
			ValuePacket<HRESULT>::Write<HresultArg>(buffer, value);
		}
	};


	// �w��t�@�C�����݊m�F�R�}���h
	struct PKTID("C8B436AC-B8BB-4939-8447-576814E0F973") IsFileExistsCmd : Packet {
		static __forceinline void Write(PacketBuffer& buffer, const wchar_t* file_name, const wchar_t* file_hash) {
			Packet::Packer<IsFileExistsCmd> p(buffer);
			FilePathArg::Write(buffer, file_name);
			HashValueArg::Write(buffer, file_hash);
		}
	};

	// �w��t�@�C�����݊m�F����
	struct PKTID("91BFBE77-39F2-427D-B379-206DF312E064") IsFileExistsRes : Packet {
		static __forceinline void Write(PacketBuffer& buffer, HRESULT hr) {
			Packet::Packer<IsFileExistsRes> p(buffer);
			HresultArg::Write(buffer, hr);
		}
	};
#pragma pack(pop)
}
