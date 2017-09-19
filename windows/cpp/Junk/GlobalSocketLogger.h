#pragma once
#ifndef __JUNK_GLOBALSOCKETLOGGER_H__
#define __JUNK_GLOBALSOCKETLOGGER_H__

#include "JunkConfig.h"
#include "JunkDef.h"
#include "Socket.h"
#include "Thread.h"
#include "File.h"
#include <vector>
#include <sstream>
#include <string.h>

_JUNK_BEGIN

//! GlobalSocketLogger からのログを処理するサーバー処理クラス
class JUNKAPICLASS LogServer {
public:
	//! サーバーへのコマンドID
#if (defined _MSC_VER) && (_MSC_VER <= 1500)
	enum CommandEnum {
#else
	enum class CommandEnum : uint32_t {
#endif
		Unknown = 0,
		WriteLog,
		Flush,
		FileClose,
		BinaryLog,
	};

	//! サーバーからの応答ID
#if (defined _MSC_VER) && (_MSC_VER <= 1500)
	enum ResultEnum {
#else
	enum class ResultEnum : uint32_t {
#endif
		Ok = 0,
		Error,
	};

	//! ログの種類
#if (defined _MSC_VER) && (_MSC_VER <= 1500)
	enum LogTypeEnum {
#else
	enum class LogTypeEnum : uint16_t {
#endif
		Enter = 1,
		Leave = 2,
	};

#pragma pack(push, 1)
	//! コマンド作成時の new 抑止用一時バッファ情報
	struct TempBuf {
		void* Ptr;
		size_t Size;

		TempBuf(void* p, size_t size) {
			this->Ptr = p;
			this->Size = size;
		}
	};

	//! サーバーへのコマンド、応答パケット基本クラス
	struct Pkt {
		int32_t Size; // 以降に続くパケットバイト数
		union {
			uint32_t Command; //!< コマンドID
			uint32_t Result; //!< 応答コードID
		};

		//! パケット全体のサイズ(bytes)
		_FINLINE size_t PacketSize() const {
			return sizeof(this->Size) + this->Size;
		}

		static inline Pkt* Allocate(TempBuf tempBuf, size_t size) {
			Pkt* pPkt = (Pkt*)(sizeof(pPkt->Size) + size <= tempBuf.Size ? tempBuf.Ptr : new uint8_t[sizeof(pPkt->Size) + size]);
			pPkt->Size = (int32_t)size;
			return pPkt;
		}

		static inline Pkt* Allocate(TempBuf tempBuf, CommandEnum command, size_t dataSize) {
			Pkt* pPkt = Allocate(tempBuf, sizeof(pPkt->Command) + dataSize);
			pPkt->Command = (uint32_t)command;
			return pPkt;
		}

		static inline Pkt* Allocate(TempBuf tempBuf, CommandEnum command, const void* pData, size_t dataSize) {
			Pkt* pPkt = Allocate(tempBuf, sizeof(pPkt->Command) + dataSize);
			pPkt->Command = (uint32_t)command;
			memcpy(&pPkt[1], pData, dataSize);
			return pPkt;
		}

		static inline void Deallocate(TempBuf tempBuf, Pkt* pPkt) {
			if (pPkt != tempBuf.Ptr && pPkt != NULL)
				delete pPkt;
		}
	};

	//! ログ出力コマンドパケット
	struct PktCommandLogWrite : public Pkt {
		uint32_t Pid; //!< プロセスID
		uint32_t Tid; //!< スレッドID
		uint16_t Depth; //!< 呼び出し階層深度
		uint16_t LogType; //!< ログ種類
		char Text[1]; //!< UTF-8エンコードされた文字列データ

		//! テキストサイズ(bytes)
		_FINLINE size_t TextSize() const {
			return this->Size - sizeof(this->Command) - sizeof(this->Pid) - sizeof(this->Tid) - sizeof(this->Depth) - sizeof(this->LogType);
		}

		static PktCommandLogWrite* Allocate(TempBuf tempBuf, uint32_t pid, uint32_t tid, uint16_t depth, LogTypeEnum logType, const char* pszText, size_t size) {
			PktCommandLogWrite* pPkt = (PktCommandLogWrite*)Pkt::Allocate(tempBuf, CommandEnum::WriteLog, sizeof(pPkt->Pid) + sizeof(pPkt->Tid) + sizeof(pPkt->Depth) + sizeof(pPkt->LogType) + size);
			pPkt->Pid = pid;
			pPkt->Tid = tid;
			pPkt->Depth = depth;
			pPkt->LogType = (uint16_t)logType;
			memcpy(pPkt->Text, pszText, size);
			return pPkt;
		}
	};

	//! バイナリ形式ログ出力設定コマンドパケット
	struct PktCommandBinaryLog : public Pkt {
		int32_t Binary; //!< 0以外ならバイナリ

		static PktCommandBinaryLog* Allocate(TempBuf tempBuf, bool binary) {
			int32_t binaryValue = binary;
			PktCommandBinaryLog* pPkt = (PktCommandBinaryLog*)Pkt::Allocate(tempBuf, CommandEnum::BinaryLog, &binaryValue, sizeof(binaryValue));
			return pPkt;
		}
	};
#pragma pack(pop)

	//! CommandWriteLog 内から呼び出されるハンドラ
	typedef void (*CommandWriteLogHandler)(SocketRef sock, PktCommandLogWrite* pCmd, const char* pszRemoteName);

    static void Startup(); //!< ログ出力先など初期化、プログラム起動時一回だけ呼び出す、スレッドアンセーフ
    static void Cleanup(); //!< 終了処理、プログラム終了時一回だけ呼び出す、スレッドアンセーフ

	LogServer();

	ibool Start(const wchar_t* pszLogFolder, int port); //!< 別スレッドでサーバー処理を開始する、スレッドアンセーフ
	void Stop(); //!< サーバー処理スレッドを停止する、スレッドアンセーフ
	void Write(const char* bytes, size_t size); //!< ログファイルへ書き込む
	void CommandBinaryLog(SocketRef sock, PktCommandBinaryLog* pCmd); //!< バイナリ形式でログ出力するかどうか設定する
	void CommandWriteLog(std::vector<uint8_t>& buf, SocketRef sock, PktCommandLogWrite* pCmd, const std::string& remoteName); //!< ログ出力コマンド処理
	void CommandFlush(SocketRef sock, Pkt* pCmd); //!< フラッシュコマンド処理
	void CommandFileClose(SocketRef sock, Pkt* pCmd); //!< 現在のログファイルを閉じる

	void SetCommandWriteLogHandler(CommandWriteLogHandler handler); //!< CommandWriteLog 内から呼び出されるハンドラを設定する
	CommandWriteLogHandler GetCommandWriteLogHandler(); //!< CommandWriteLog 内から呼び出されるハンドラを取得する

private:
	//! クライアント毎の処理
	class Client {
	public:
		Client(LogServer* pOwner, Socket::Handle hClient, const char* pszRemoteName); //!< コンストラクタ、クライアント通信スレッドが開始される

		void Stop(bool wait = false); //!< クライアント通信スレッドを停止する

	private:
		static intptr_t ThreadStart(void* pObj);
		void ThreadProc();

		LogServer* m_pOwner;
		Socket m_Socket;
		std::string m_RemoteName;
		Thread m_Thread;
	};

	static intptr_t ThreadStart(void* pObj); //!< 接続受付スレッド開始アドレス
	void ThreadProc(); //!< 接続受付スレッド処理
	void AddClient(Client* pClient); //!< 指定クライアントを管理下へ追加する
	bool RemoveClient(Client* pClient, bool wait = false); //!< 指定クライアントを管理下から除外する

	std::wstring m_LogFolder; //!< ログの置き場フォルダ
	File m_LogFile; //!< ログファイル
	CriticalSection m_LogFileCs; //!< m_LogFile アクセス排他処理用

	volatile ibool m_RequestStop; //!< サーバー停止要求フラグ
	Event m_RequestStopEvent; //!< サーバー停止要求イベント
	Socket m_AcceptanceSocket; //!< 接続受付ソケット
	Thread m_AcceptanceThread; //!< 接続受付処理スレッド

	std::vector<Client*> m_Clients; //!< クライアント処理配列
	CriticalSection m_ClientsCs; //!< m_Clients アクセス排他処理用

	bool m_BinaryLog; //!< バイナリ形式でログを出力するかどうか

	CommandWriteLogHandler m_CommandWriteLogHandler; //!< CommandWriteLog 内から呼び出されるハンドラ
};

#define JUNKLOG_DELIMITER (L',')

//! ソケットを使いサーバーへログを送るシングルトンクラス
class JUNKAPICLASS GlobalSocketLogger {
public:
	//! インスタンス
	struct Instance;
	
	static Instance* GetInstance(); //!< 通信用ソケットなどの情報を保持するインスタンスを取得する
	static void Startup(const wchar_t* pszHost, int port); //!< ログ出力先など初期化、プログラム起動時一回だけ呼び出す、スレッドセーフ
    static void Startup(const char* pszHost, int port); //!< ログ出力先など初期化、プログラム起動時一回だけ呼び出す、スレッドセーフ
	static void Startup(wchar_t* pszIniFile = L"GlobalSocketLogger.ini"); //!< ログ出力先など初期化、プログラム起動時一回だけ呼び出す、スレッドセーフ
	static void Startup(Instance* pInstance); //!< 他DLLのインスタンスを指定して初期化する
    static void Cleanup(); //!< 終了処理、プログラム終了時一回だけ呼び出す、スレッドアンセーフ
	static LogServer::Pkt* Command(LogServer::TempBuf tempBuf, LogServer::Pkt* pCmd); //!< サーバーへコマンドパケットを送り応答を取得する、スレッドセーフ
	static void BinaryLog(bool binary); //!< サーバーのログ出力形式をバイナリかどうか設定する、スレッドセーフ
	static void WriteLog(uint32_t depth, LogServer::LogTypeEnum logType, const wchar_t* pszText); //!< サーバーへログを送る、スレッドセーフ
	static void Flush(); //!< サーバーへログをファイルへフラッシュ要求、スレッドセーフ
	static void FileClose(); //!< サーバーへ現在のログファイルを閉じる要求、スレッドセーフ
	static intptr_t GetDepth(); //!< 現在のスレッドの呼び出し深度の取得
	static intptr_t IncrementDepth(); //!< 現在のスレッドの呼び出し深度をインクリメント
	static intptr_t DecrementDepth(); //!< 現在のスレッドの呼び出し深度をデクリメント
};

_JUNK_END

#endif
