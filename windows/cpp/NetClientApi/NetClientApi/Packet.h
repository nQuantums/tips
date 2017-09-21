#pragma once
#include "Config.h"
#include <Windows.h>
#include <vector>

namespace NetClientApi {
#pragma pack(push, 1)

	typedef std::vector<uint8_t> PacketBuffer;
	typedef uint32_t PacketSize;


	// �w��o�b�t�@�֎w��l��ǉ����o�b�t�@���ł̃I�t�Z�b�g��Ԃ�
	template<class T> __forceinline size_t ToBuffer(PacketBuffer& buffer, const T& value) {
		size_t start_offset = buffer.size();
		buffer.insert(buffer.end(), reinterpret_cast<uint8_t*>(&value), reinterpret_cast<uint8_t*>(&value) + sizeof(value));
		return start_offset;
	}


	// �o�b�t�@�Ƀp�P�b�g�쐬����̂�⏕����
	// ���p�P�b�g�T�C�Y�̓p�P�b�g���e�������݊������܂ŕs���Ȃ̂ŁA�R���X�g���N�^�Ńp�P�b�g�쐬�J�n���f�X�g���N�^�̃^�C�~���O�ł̓T�C�Y�m�肵�Ă���̂ł���𗘗p����
	struct PacketWriter {
		PacketBuffer& buffer; // �������ݐ�o�b�t�@
		size_t size_offset; // �o�b�t�@���ł̃p�P�b�g�T�C�Y�̈ʒu

		PacketWriter(PacketBuffer& buf) : buffer(buf) {
			size_offset = buf.size();
		}
		~PacketWriter() {
			(PacketSize&)buffer[size_offset] = buffer.size() - size_offset - sizeof(PacketSize);
		}

		// �w��l��ǉ����o�b�t�@���ł̃I�t�Z�b�g��Ԃ�
		template<class T> __forceinline size_t Write(const T& value) {
			return ToBuffer(value);
		}
	};

	// �p�P�b�g��{�\���́A������p�����ăR�}���h���͈����p�P�b�g���쐬����
	struct PacketBase {
		uint32_t size; // �ȍ~�ɑ���(guid�܂�)�p�P�b�g���f�[�^�T�C�Y(bytes)
		GUID guid; // �R�}���h���͈������ʎq

		__forceinline void* DataPtr() {
			return &(&this->guid)[1];
		}

		// TODO: �o�b�t�@�Ƀp�P�b�g�쐬����̂�⏕����
		// ���p�P�b�g�T�C�Y�̓p�P�b�g���e�������݊������܂ŕs���Ȃ̂ŁA�R���X�g���N�^�Ńp�P�b�g�쐬�J�n���f�X�g���N�^�̃^�C�~���O�ł̓T�C�Y�m�肵�Ă���̂ł���𗘗p����
		struct Writer {
			PacketBuffer& buffer; // �������ݐ�o�b�t�@
			size_t size_offset; // �o�b�t�@���ł̃p�P�b�g�T�C�Y�̈ʒu

			Writer(PacketBuffer& buf) : buffer(buf) {
				size_offset = buf.size();
			}
			~Writer() {
				(PacketSize&)buffer[size_offset] = buffer.size() - size_offset - sizeof(PacketSize);
			}

			// �w��l��ǉ����o�b�t�@���ł̃I�t�Z�b�g��Ԃ�
			template<class T> __forceinline size_t Write(const T& value) {
				return ToBuffer(value);
			}
		};


		// �w��o�b�t�@�փp�P�b�g��ǉ�����
		static PacketBase* ToBuffer(PacketBuffer& buffer, uint32_t size, const GUID& guid) {
			PacketCreator pc(buffer);
			pc.Write(
		}

	};

	// �t�@�C��������
	struct __declspec(uuid("4D844928-3409-4b99-90C6-D144B3734C90")) ArgFilePath : PacketBase {
		wchar_t file_path[1]; // �t�@�C���p�X��

		static void ToBuffer(PacketBuffer& buffer, const wchar_t* file_path) {
			size_t offset = buffer.size();
			buffer.resize(offset + sizeof(this->size));

			uint32_t* size_ptr = reinterpret_cast<uint32_t*>(&buffer[offset]);


			uint32_t size

			PacketBuffer
		}
	};

	// �n�b�V���l����
	struct __declspec(uuid("7CB273A6-50F2-4343-8AF4-7F6200BE8FAD")) ArgHashValue : PacketBase {
		wchar_t hash_value[1]; // �n�b�V���l������
	};


	// �w��t�@�C�����݊m�F�R�}���h
	struct __declspec(uuid("C8B436AC-B8BB-4939-8447-576814E0F973")) CmdCheckFileExists : PacketBase {

	};

	// int32_t �l����
	struct __declspec(uuid("7496B65F-51BF-44e8-A741-D4D576301018")) RspInt32 : PacketBase {
	};


#pragma pack(pop)
}
