#pragma once
#include "Config.h"
#include <Windows.h>
#include <vector>

namespace NetClientApi {
#pragma pack(push, 1)

	typedef std::vector<uint8_t> PacketBuffer;
	typedef uint32_t PacketSize;


	// 指定バッファへ指定値を追加しバッファ内でのオフセットを返す
	template<class T> __forceinline size_t ToBuffer(PacketBuffer& buffer, const T& value) {
		size_t start_offset = buffer.size();
		buffer.insert(buffer.end(), reinterpret_cast<uint8_t*>(&value), reinterpret_cast<uint8_t*>(&value) + sizeof(value));
		return start_offset;
	}


	// バッファにパケット作成するのを補助する
	// ※パケットサイズはパケット内容書き込み完了時まで不明なので、コンストラクタでパケット作成開始しデストラクタのタイミングではサイズ確定しているのでそれを利用する
	struct PacketWriter {
		PacketBuffer& buffer; // 書き込み先バッファ
		size_t size_offset; // バッファ内でのパケットサイズの位置

		PacketWriter(PacketBuffer& buf) : buffer(buf) {
			size_offset = buf.size();
		}
		~PacketWriter() {
			(PacketSize&)buffer[size_offset] = buffer.size() - size_offset - sizeof(PacketSize);
		}

		// 指定値を追加しバッファ内でのオフセットを返す
		template<class T> __forceinline size_t Write(const T& value) {
			return ToBuffer(value);
		}
	};

	// パケット基本構造体、これを継承してコマンド又は引数パケットを作成する
	struct PacketBase {
		uint32_t size; // 以降に続く(guid含む)パケット内データサイズ(bytes)
		GUID guid; // コマンド又は引数識別子

		__forceinline void* DataPtr() {
			return &(&this->guid)[1];
		}

		// TODO: バッファにパケット作成するのを補助する
		// ※パケットサイズはパケット内容書き込み完了時まで不明なので、コンストラクタでパケット作成開始しデストラクタのタイミングではサイズ確定しているのでそれを利用する
		struct Writer {
			PacketBuffer& buffer; // 書き込み先バッファ
			size_t size_offset; // バッファ内でのパケットサイズの位置

			Writer(PacketBuffer& buf) : buffer(buf) {
				size_offset = buf.size();
			}
			~Writer() {
				(PacketSize&)buffer[size_offset] = buffer.size() - size_offset - sizeof(PacketSize);
			}

			// 指定値を追加しバッファ内でのオフセットを返す
			template<class T> __forceinline size_t Write(const T& value) {
				return ToBuffer(value);
			}
		};


		// 指定バッファへパケットを追加する
		static PacketBase* ToBuffer(PacketBuffer& buffer, uint32_t size, const GUID& guid) {
			PacketCreator pc(buffer);
			pc.Write(
		}

	};

	// ファイル名引数
	struct __declspec(uuid("4D844928-3409-4b99-90C6-D144B3734C90")) ArgFilePath : PacketBase {
		wchar_t file_path[1]; // ファイルパス名

		static void ToBuffer(PacketBuffer& buffer, const wchar_t* file_path) {
			size_t offset = buffer.size();
			buffer.resize(offset + sizeof(this->size));

			uint32_t* size_ptr = reinterpret_cast<uint32_t*>(&buffer[offset]);


			uint32_t size

			PacketBuffer
		}
	};

	// ハッシュ値引数
	struct __declspec(uuid("7CB273A6-50F2-4343-8AF4-7F6200BE8FAD")) ArgHashValue : PacketBase {
		wchar_t hash_value[1]; // ハッシュ値文字列
	};


	// 指定ファイル存在確認コマンド
	struct __declspec(uuid("C8B436AC-B8BB-4939-8447-576814E0F973")) CmdCheckFileExists : PacketBase {

	};

	// int32_t 値応答
	struct __declspec(uuid("7496B65F-51BF-44e8-A741-D4D576301018")) RspInt32 : PacketBase {
	};


#pragma pack(pop)
}
