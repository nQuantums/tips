#pragma once
#include "Packets.h"


namespace NetClientApi {
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


	// 指定ファイル存在確認コマンド
	struct PKTID("C8B436AC-B8BB-4939-8447-576814E0F973") IsFileExistsCmd : PacketWithId<IsFileExistsCmd> {
		std::wstring file_name; // ファイル名
		std::wstring file_hash; // ファイル内容ハッシュ値

		static void Write(PacketBuffer& buffer, const wchar_t* file_name, const wchar_t* file_hash) {
			MyPacker p(buffer);
			FileNameArg::Write(buffer, file_name);
			FileHashArg::Write(buffer, file_hash);
		}
		bool Read(Unpacker& unpacker) {
			if (!this->ReadHeader(unpacker))
				return false;
			this->file_name = FileNameArg::Value(unpacker);
			this->file_hash = FileHashArg::Value(unpacker);
			return true;
		}
	};
	// 指定ファイル存在確認コマンドの応答
	struct PKTID("91BFBE77-39F2-427D-B379-206DF312E064") IsFileExistsRes : PacketWithId<IsFileExistsCmd> {
		HRESULT hr; // チェック途中で発生したエラー
		BOOL exists; // ファイルの有無

		static void Write(PacketBuffer& buffer, HRESULT hr, BOOL exists) {
			MyPacker p(buffer);
			HresultArg::Write(buffer, hr);
			BoolArg::Write(buffer, exists);
		}
		bool Read(Unpacker& unpacker) {
			if (!this->ReadHeader(unpacker))
				return false;
			this->hr = HresultArg::Value(unpacker);
			this->exists = BoolArg::Value(unpacker);
			return true;
		}
	};


	// ファイルを指定フォルダへコピーコマンド
	struct PKTID("232C49F2-FB4B-4079-898D-227A0BB52601") CopyFileCmd : PacketWithId<CopyFileCmd> {
		std::wstring file_name; // ファイル名
		std::wstring file_hash; // ファイル内容ハッシュ値
		std::wstring folder_path; // コピー先フォルダパス名

		static void Write(PacketBuffer& buffer, const wchar_t* file_name, const wchar_t* file_hash, const wchar_t* folder_path) {
			MyPacker p(buffer);
			FileNameArg::Write(buffer, file_name);
			FileHashArg::Write(buffer, file_hash);
			FolderPathArg::Write(buffer, folder_path);
		}
		bool Read(Unpacker& unpacker) {
			if (!this->ReadHeader(unpacker))
				return false;
			this->file_name = FileNameArg::Value(unpacker);
			this->file_hash = FileHashArg::Value(unpacker);
			this->folder_path = FolderPathArg::Value(unpacker);
			return true;
		}
	};
	// ファイルを指定フォルダへコピーコマンドの応答
	struct PKTID("2E68DE5D-94A9-4ADF-AFEB-851ED3BAF523") CopyFileIfExistsRes : PacketWithId<CopyFileIfExistsRes> {
		HRESULT hr; // チェック途中で発生したエラー
		BOOL exists; // ファイルの有無

		static void Write(PacketBuffer& buffer, HRESULT hr, BOOL exists) {
			MyPacker p(buffer);
			HresultArg::Write(buffer, hr);
			BoolArg::Write(buffer, exists);
		}
		bool Read(Unpacker& unpacker) {
			if (!this->ReadHeader(unpacker))
				return false;
			this->hr = HresultArg::Value(unpacker);
			this->exists = BoolArg::Value(unpacker);
			return true;
		}
	};
}
