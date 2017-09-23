#pragma once
#include "Packets.h"


namespace Packets {
	// テキスト引数
	struct PKTID("2AD87975-EACB-4E94-A919-B3ACCB9A60D5") TextArg : ValuePacket<TextArg, std::wstring> {};
	// ファイル名引数
	struct PKTID("4D844928-3409-4b99-90C6-D144B3734C90") FileNameArg : ValuePacket<FileNameArg, std::wstring> {};
	// ハッシュ値引数
	struct PKTID("7CB273A6-50F2-4343-8AF4-7F6200BE8FAD") FileHashArg : ValuePacket<FileHashArg, std::wstring> {};
	// フォルダパス名引数
	struct PKTID("6E382AA0-A1B0-4816-B0A6-A6A9BDE26BBF") FolderPathArg : ValuePacket<FolderPathArg, std::wstring> {};
	// BOOL 値引数
	struct PKTID("3508B202-B300-4DD2-9BC3-0275C2448645") BoolArg : ValuePacket<BoolArg, BOOL> {};
	// HRESULT 値引数
	struct PKTID("7496B65F-51BF-44e8-A741-D4D576301018") HresultArg : ValuePacket<HresultArg, HRESULT> {};


	// テキスト追加コマンド
	struct PKTID("{96FABA44-DB79-40CC-8FE2-5749D37BC27A}") AddTextCmd : UniquePacket<AddTextCmd> {
		std::wstring text; // テキスト

		AddTextCmd(Unpacker& unpacker) {
			this->ReadHeader(unpacker);
			this->text = TextArg::Value(unpacker);
		}

		static void Write(ByteBuffer& buffer, const wchar_t* text) {
			MyPacker p(buffer);
			TextArg::Write(buffer, text);
		}
	};
	// テキスト追加コマンドの応答
	struct PKTID("F82162F6-1FB5-4E19-8A42-F396EC88503F") AddTextRes : UniquePacket<AddTextRes> {
		HRESULT hr; // チェック途中で発生したエラー

		AddTextRes(Unpacker& unpacker) {
			this->ReadHeader(unpacker);
			this->hr = HresultArg::Value(unpacker);
		}

		static void Write(ByteBuffer& buffer, HRESULT hr) {
			MyPacker p(buffer);
			HresultArg::Write(buffer, hr);
		}
	};


	// 全テキスト取得コマンド
	struct PKTID("135F3A96-B2D9-44B4-8DDF-158EE6B2E993") GetAllTextsCmd : UniquePacket<GetAllTextsCmd> {
		GetAllTextsCmd(Unpacker& unpacker) {
			this->ReadHeader(unpacker);
		}

		static void Write(ByteBuffer& buffer) {
			MyPacker p(buffer);
		}
	};
	// テキスト追加コマンドの応答
	struct PKTID("65C3EC0A-93F8-47FC-82E8-160F72ABF239") GetAllTextsRes : UniquePacket<GetAllTextsRes> {
		std::vector<std::wstring> texts; // テキスト配列

		GetAllTextsRes(Unpacker& unpacker) {
			this->ReadHeader(unpacker);
			TextArg::Array(unpacker, this->texts);
		}

		static void Write(ByteBuffer& buffer, const std::vector<std::wstring>& texts) {
			MyPacker p(buffer);
			for (intptr_t i = 0, n = texts.size(); i < n; i++) {
				TextArg::Write(buffer, texts[i]);
			}
		}
	};
}
