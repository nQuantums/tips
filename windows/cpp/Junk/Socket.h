#pragma once
#ifndef __JUNK_SOCKET_H__
#define __JUNK_SOCKET_H__

#include "JunkConfig.h"

#include <string.h>
#include <time.h>

#if defined __GNUC__
#include <unistd.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <sys/ioctl.h>
#include <netinet/in.h>
#include <netinet/tcp.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#include <netdb.h>
#include <errno.h>
#elif defined  _MSC_VER
#include <WinSock2.h>
#include <WS2tcpip.h>
#endif

#include <vector>
#include <string>

_JUNK_BEGIN

//! ソケットクラス、デストラクタではソケットクローズしない
struct JUNKAPICLASS SocketRef {
#if defined __GNUC__
	typedef intptr_t Handle; // ハンドル型
#elif defined  _MSC_VER
	typedef SOCKET Handle; // ハンドル型
#endif

	//! Shutdown() メソッドに渡す列挙値
#if _MSC_VER <= 1500
	enum Sd {
#else
	enum class Sd : intptr_t {
#endif
		Read = 0, //!< 読み込みをシャットダウン
		RWrite = 1, //!< 書き込みをシャットダウン
		Both = 2, //!< 読み書き両方をシャットダウン
	};

	//! ソケットタイプ
#if _MSC_VER <= 1500
	enum St {
#else
	enum class St : intptr_t {
#endif
		Stream = SOCK_STREAM, //!< ストリームソケット
		Dgram = SOCK_DGRAM, //!< データグラムソケット
		Raw = SOCK_RAW, //!< 生プロトコルインターフェース
		Rdm = SOCK_RDM, //!< reliably-delivered message
		SeqPacket = SOCK_SEQPACKET, //!< sequenced packet stream
	};

	//! アドレスファミリ
#if _MSC_VER <= 1500
	enum Af {
#else
	enum class Af : intptr_t {
#endif
		IPv4 = AF_INET, //!< IPv4
		IPv6 = AF_INET6, //!< IPv6
	};

	//! エンドポイント情報
	struct JUNKAPICLASS Endpoint {
		addrinfo* pRoot;
		std::vector<addrinfo*> AddrInfos;

		Endpoint() {
			this->pRoot = NULL;
		}
		~Endpoint() {
			Delete();
		}

		//! 指定ホスト名、サービス名、ヒントからアドレス情報を取得し保持する
		bool Create(const wchar_t* pszHost, const wchar_t* pszService, const addrinfo* pHint);

		//! 指定ホスト名、サービス名、ヒントからアドレス情報を取得し保持する
		bool Create(const char* pszHost, const char* pszService, const addrinfo* pHint);

		//! 確保したメモリを破棄する
		void Delete();

		//! ホスト名とサービス名を std::vector に取得する
		bool GetNames(std::vector<std::wstring>& hosts, std::vector<std::wstring>& services);

		//! ホスト名とサービス名を std::vector に取得する
		bool GetNames(std::vector<std::string>& hosts, std::vector<std::string>& services);

		//! 指定ホスト名、サービス名、ヒントからアドレス情報を取得し保持する
		bool Create(
			const wchar_t* pszHost, //!< [in,optional] ホスト名、IPv4とIPv6の文字列もいける、サーバーの場合には NULL を指定すると INADDR_ANY (0.0.0.0), IN6ADDR_ANY_INIT (::) 扱いになる、ちなみにUDPのブロードキャストは 192.168.1.255 みたいな感じ
			const wchar_t* pszService, //!< [in,optional] サービス名(httpなど)またはポート番号
			St sockType, //!< [in] ソケットタイプ
			Af family, //!< [in] アドレスファミリ
			bool toBind = true //!< [in] bind() する際に true を指定する
		) {
			struct addrinfo hints;
			memset(&hints, 0, sizeof(hints));
			hints.ai_socktype = (int)sockType;
			hints.ai_family = (int)family;
			hints.ai_flags = toBind ? AI_PASSIVE : 0;
			return Create(pszHost, pszService, &hints);
		}

		//! 指定ホスト名、サービス名、ヒントからアドレス情報を取得し保持する
		bool Create(
			const char* pszHost, //!< [in,optional] ホスト名、IPv4とIPv6の文字列もいける、サーバーの場合には NULL を指定すると INADDR_ANY (0.0.0.0), IN6ADDR_ANY_INIT (::) 扱いになる、ちなみにUDPのブロードキャストは 192.168.1.255 みたいな感じ
			const char* pszService, //!< [in,optional] サービス名(httpなど)またはポート番号
			St sockType, //!< [in] ソケットタイプ
			Af family, //!< [in] アドレスファミリ
			bool toBind = true //!< [in] bind() する際に true を指定する
		) {
			struct addrinfo hints;
			memset(&hints, 0, sizeof(hints));
			hints.ai_socktype = (int)sockType;
			hints.ai_family = (int)family;
			hints.ai_flags = toBind ? AI_PASSIVE : 0;
			return Create(pszHost, pszService, &hints);
		}

		//! すでに getaddrinfo() で作成されているアドレス情報列にアタッチする、デストラクタ時にメモリが破棄される
		void Attach(
			addrinfo* pRoot //!< [in] getaddrinfo() で作成したアドレス情報
		) {
			Delete();
			this->pRoot = pRoot;
			this->AddrInfos.resize(0);
			for (struct addrinfo* adrinf = this->pRoot; adrinf != NULL; adrinf = adrinf->ai_next) {
				this->AddrInfos.push_back(adrinf);
			}
		}

		//! 保持している getaddrinfo() で作成されたアドレス情報列をでタッチする、デタッチ後はメモリ破棄されない
		addrinfo* Detach() {
			addrinfo* p = this->pRoot;
			this->pRoot = NULL;
			return p;
		}

	private:
		// 以下実装してはいけないメソッド
		Endpoint(const Endpoint& c);
		Endpoint& operator=(const Endpoint& c);
	};

#pragma pack(push, 1)
	//! IPv4のアドレスとポートを示す構造体
	struct JUNKAPICLASS IPv4AddrPort {
		in_addr Addr; //!< アドレス
		uint32_t Port; //!< ポート

		_FINLINE IPv4AddrPort() {
		}
		_FINLINE IPv4AddrPort(const in_addr& addr, uint32_t port) {
			this->Addr = addr;
			this->Port = port;
		}
		_FINLINE IPv4AddrPort(const sockaddr_storage& addrst) {
			sockaddr_in& si = (sockaddr_in&)addrst;
			this->Addr = si.sin_addr;
			this->Port = htons(si.sin_port);
		}

		_FINLINE bool operator==(const IPv4AddrPort& ap) const {
			return memcmp(this, &ap, sizeof(*this)) == 0;
		}
		_FINLINE bool operator!=(const IPv4AddrPort& ap) const {
			return memcmp(this, &ap, sizeof(*this)) != 0;
		}
		_FINLINE bool operator<(const IPv4AddrPort& ap) const {
			return memcmp(this, &ap, sizeof(*this)) < 0;
		}
		_FINLINE bool operator<=(const IPv4AddrPort& ap) const {
			return memcmp(this, &ap, sizeof(*this)) <= 0;
		}
		_FINLINE bool operator>(const IPv4AddrPort& ap) const {
			return memcmp(this, &ap, sizeof(*this)) > 0;
		}
		_FINLINE bool operator>=(const IPv4AddrPort& ap) const {
			return memcmp(this, &ap, sizeof(*this)) >= 0;
		}
	};

	//! IPv6のアドレスとポートを示す構造体
	struct JUNKAPICLASS IPv6AddrPort {
		in6_addr Addr; //!< 
		uint32_t Port; //!< ポート

		_FINLINE IPv6AddrPort() {
		}
		_FINLINE IPv6AddrPort(const in6_addr& addr, uint32_t port) {
			this->Addr = addr;
			this->Port = port;
		}
		_FINLINE IPv6AddrPort(const sockaddr_storage& addrst) {
			sockaddr_in6& si = (sockaddr_in6&)addrst;
			this->Addr = si.sin6_addr;
			this->Port = htons(si.sin6_port);
		}

		_FINLINE bool operator==(const IPv6AddrPort& ap) const {
			return memcmp(this, &ap, sizeof(*this)) == 0;
		}
		_FINLINE bool operator!=(const IPv6AddrPort& ap) const {
			return memcmp(this, &ap, sizeof(*this)) != 0;
		}
		_FINLINE bool operator<(const IPv6AddrPort& ap) const {
			return memcmp(this, &ap, sizeof(*this)) < 0;
		}
		_FINLINE bool operator<=(const IPv6AddrPort& ap) const {
			return memcmp(this, &ap, sizeof(*this)) <= 0;
		}
		_FINLINE bool operator>(const IPv6AddrPort& ap) const {
			return memcmp(this, &ap, sizeof(*this)) > 0;
		}
		_FINLINE bool operator>=(const IPv6AddrPort& ap) const {
			return memcmp(this, &ap, sizeof(*this)) >= 0;
		}
	};
#pragma pack(pop)

	Handle m_hSock; //!< ソケットハンドル

	//! 無効ソケットハンドルの取得
	static _FINLINE Handle InvalidHandle() {
		return -1;
	}

	//! 指定されたハンドルが無効かどうか調べる
	static _FINLINE bool IsInvalidHandle(Handle handle) {
		return handle == InvalidHandle();
	}

	//! 時間を ms 単位から timeval に変換
	static timeval MsToTimeval(int64_t ms);

	//! 時間を timeval から ms 単位に変換
	static int64_t TimevalToMs(timeval tv);

	//! SocketRef クラスを使用するプログラムの開始時に一度だけ呼び出す
	static bool Startup();

	//! SocketRef クラスの使用が完全に終わったら呼び出す
	static bool Cleanup();

	//! 127.0.0.1 の様なIPv4アドレス文字列をバイナリに変換する
	static uint32_t IPv4StrToBin(const char* pszIPv4);

	//! 127.0.0.1 の様なIPv4アドレス文字列とポート番号からアドレス構造体を取得する
	static sockaddr_in IPv4StrToAddress(const char* pszIPv4, int port);

	//! リモート名の取得
	static std::string GetRemoteName(const sockaddr_storage& addrst);

	//! リモート名の取得
	static std::string GetRemoteName(Handle hSock);

	//! リモート名の取得
	static std::wstring GetRemoteNameW(Handle hSock);

	//! ローカルホスト名を取得
	static std::string GetHostName();

	//! ローカルIPアドレスを取得
	static std::string GetLocalIPAddress(Af af = Af::IPv4);

	//! ローカルIPアドレス列を取得
	static void GetLocalIPAddress(std::vector<std::wstring>& addresses, Af af = Af::IPv4);

	//! ローカルIPアドレス列を取得
	static void GetLocalIPAddress(std::vector<std::string>& addresses, Af af = Af::IPv4);


	//! コンストラクタ
	SocketRef() {
		m_hSock = InvalidHandle();
	}
	//! コンストラクタ、ソケットハンドル指定して初期化
	SocketRef(Handle handle) {
		m_hSock = handle;
	}


	//! 自分に無効ソケットハンドルが設定されているかどうか
	_FINLINE bool IsInvalidHandle() {
		return m_hSock == InvalidHandle();
	}

	//! ハンドルをアタッチする、以前のハンドルが返る
	Handle Attach(Handle newHandle) {
		Handle handle = m_hSock;
		m_hSock = newHandle;
		return handle;
	}

	//! ソケットハンドルを切り離し、所有者を呼び出し元に変更する
	Handle Detach() {
		Handle handle = m_hSock;
		m_hSock = InvalidHandle();
		return handle;
	}

	//! ソケットを作成
	bool Create() {
		m_hSock = socket(AF_INET, SOCK_STREAM, 0);
		return m_hSock != InvalidHandle();
	}

	//! アドレス情報でソケットを作成
	bool Create(const addrinfo* pAddrInfo) {
		m_hSock = socket(pAddrInfo->ai_family, pAddrInfo->ai_socktype, 0);
		return m_hSock != InvalidHandle();
	}

	//! Endpoint でソケットを作成
	bool Create(const Endpoint& endpoint, int index = 0) {
		addrinfo* ai = endpoint.AddrInfos[index];
		m_hSock = socket(ai->ai_family, ai->ai_socktype, 0);
		return m_hSock != InvalidHandle();
	}

	//! ソケットを閉じる
	void Close() {
		if (m_hSock == InvalidHandle())
			return;
#ifdef __GNUC__
		close(m_hSock);
#elif defined _MSC_VER
		closesocket(m_hSock);
#endif
		m_hSock = InvalidHandle();
	}

	//! 接続
	bool Connect(const sockaddr_in* pAddr) {
		return connect(m_hSock, (const sockaddr*)pAddr, sizeof(sockaddr_in)) == 0;
	}

#if defined _MSC_VER
#pragma warning(push)
#pragma warning(disable: 4267)
#endif

	//! 接続
	bool Connect(const addrinfo* pAddrInfo) {
		return connect(m_hSock, pAddrInfo->ai_addr, pAddrInfo->ai_addrlen) == 0;
	}

	//! 指定の Endpoint に接続する
	bool Connect(const Endpoint& endpoint, int index = 0) {
		addrinfo* ai = endpoint.AddrInfos[index];
		return connect(m_hSock, ai->ai_addr, ai->ai_addrlen) == 0;
	}

	//! 指定の Endpoint に接続する
	bool Connect(const Endpoint& endpoint, int index, int64_t timeoutMs) {
		unsigned long iMode = 1;
		int iResult = ioctlsocket(m_hSock, FIONBIO, &iMode);
		if (iResult != NO_ERROR)
			return false;

		if (!Connect(endpoint, index))
			return false;

		iMode = 0;
		iResult = ioctlsocket(m_hSock, FIONBIO, &iMode);
		if (iResult != NO_ERROR)
			return false;
 
		fd_set Write, Err;
		FD_ZERO(&Write);
		FD_ZERO(&Err);
		FD_SET(m_hSock, &Write);
		FD_SET(m_hSock, &Err);

		timeval tv = MsToTimeval(timeoutMs);
		select(0, NULL, &Write, &Err, &tv);			
		return FD_ISSET(m_hSock, &Write) != 0;
	}

	//! ソケットを指定ポートにバインドする
	bool Bind(int port) {
		sockaddr_in addr;
		socklen_t addrsize = sizeof(addr);

		memset(&addr, 0, sizeof(addr));
		addr.sin_port = htons((uint16_t)port);
		addr.sin_family = AF_INET;
		addr.sin_addr.s_addr = htonl(INADDR_ANY);

		return bind(m_hSock, (sockaddr*)&addr, addrsize) == 0;
	}

	//! ソケットを指定アドレス情報にバインドする
	bool Bind(const addrinfo* pAddrInfo) {
		return bind(m_hSock, pAddrInfo->ai_addr, pAddrInfo->ai_addrlen) == 0;
	}

	//! ソケットを指定の Endpoint にバインドする
	bool Bind(const Endpoint& endpoint, int index = 0) {
		addrinfo* ai = endpoint.AddrInfos[index];
		return bind(m_hSock, ai->ai_addr, ai->ai_addrlen) == 0;
	}

	//! リッスン開始
	bool Listen(int backlog) {
		return listen(m_hSock, backlog) == 0;
	}

	//! 接続受付
	Handle Accept(sockaddr_in* pAddr) {
		socklen_t addrsize = sizeof(sockaddr_in);
		return accept(m_hSock, (sockaddr*)pAddr, &addrsize);
	}

	//! 接続受付
	Handle Accept(sockaddr* pFromAddr, socklen_t* pFromLen) {
		return accept(m_hSock, pFromAddr, pFromLen);
	}

	//! 接続受付
	Handle Accept(sockaddr_storage* pFromAddr, socklen_t* pFromLen) {
		return accept(m_hSock, (sockaddr*)pFromAddr, pFromLen);
	}

	//! ソケットアドレスから名前情報を取得する
	static bool GetName(const sockaddr* pAddr, socklen_t addrLen, std::string& host, std::string& service);

	//! リモート名の取得
	std::string GetRemoteName() const;

	//! ソケットから読み込み
	_FINLINE intptr_t Recv(void* pBuf, size_t size) {
#ifdef __GNUC__
		return recv(m_hSock, pBuf, size, 0);
#elif defined _MSC_VER
		return recv(m_hSock, (char*)pBuf, size, 0);
#endif
	}

	//! 指定アドレスからのソケットから読み込み(UDP用)
	_FINLINE intptr_t RecvFrom(void* pBuf, size_t size, sockaddr_storage* pFromAddr, socklen_t* pFromLen) {
#ifdef __GNUC__
		return recvfrom(m_hSock, (char*)pBuf, size, 0, (sockaddr*)pFromAddr, pFromLen);
#elif defined _MSC_VER
		return recvfrom(m_hSock, (char*)pBuf, size, 0, (sockaddr*)pFromAddr, pFromLen);
#endif
	}

	//! RecvFrom() などで取得した sockaddr_storage からIPv4アドレス＆ポートを取得する
	static _FINLINE IPv4AddrPort ToIPv4AddrPort(const sockaddr_storage& addrst) {
		return IPv4AddrPort(addrst);
	}

	//! RecvFrom() などで取得した sockaddr_storage からIPv6アドレス＆ポートを取得する
	static _FINLINE IPv6AddrPort ToIPv6AddrPort(const sockaddr_storage& addrst) {
		return IPv6AddrPort(addrst);
	}

	//! ソケットへ書き込み
	_FINLINE intptr_t Send(const void* pBuf, size_t size) {
#ifdef __GNUC__
		return send(m_hSock, (void*)pBuf, size, 0);
#elif defined _MSC_VER
		return send(m_hSock, (char*)pBuf, size, 0);
#endif
	}

	//! 指定アドレスへ送信(UDP用)
	_FINLINE intptr_t SendTo(const void* pBuf, size_t size, const addrinfo* pAddrInfo) {
#ifdef __GNUC__
		return sendto(m_hSock, (char*)pBuf, size, 0, (sockaddr*)pAddrInfo->ai_addr, pAddrInfo->ai_addrlen);
#elif defined _MSC_VER
		return sendto(m_hSock, (char*)pBuf, size, 0, pAddrInfo->ai_addr, pAddrInfo->ai_addrlen);
#endif
	}

	//! 指定アドレス情報へ送信(UDP用)
	_FINLINE intptr_t SendTo(const void* pBuf, size_t size, const sockaddr_in& addr) {
#ifdef __GNUC__
		return sendto(m_hSock, (char*)pBuf, size, 0, (sockaddr*)&addr, sizeof(addr));
#elif defined _MSC_VER
		return sendto(m_hSock, (char*)pBuf, size, 0, (sockaddr*)&addr, sizeof(addr));
#endif
	}

#if defined _MSC_VER
#pragma warning(pop)
#endif

	//! 指定 Endpoint へ送信(UDP用)
	_FINLINE intptr_t SendTo(const void* pBuf, size_t size, const Endpoint& endpoint, int index = 0) {
		return this->SendTo(pBuf, size, endpoint.AddrInfos[index]);
	}

	//! シャットダウン
	bool Shutdown(Sd how) {
		return shutdown(m_hSock, (int)how) == 0;
	}

	//! 読み込みタイムアウト設定
	bool SetRecvTimeout(int64_t ms) {
		timeval tv = MsToTimeval(ms);
		return setsockopt(m_hSock, SOL_SOCKET, SO_RCVTIMEO, (char*)&tv, sizeof(tv)) == 0;
	}

	//! 読み込みタイムアウト取得
	int64_t GetRecvTimeout() {
		timeval tv;
		socklen_t size = sizeof(tv);
		memset(&tv, 0, sizeof(tv));
		if (getsockopt(m_hSock, SOL_SOCKET, SO_RCVTIMEO, (char*)&tv, &size) != 0)
			return -1;
		return TimevalToMs(tv);
	}

	//! 書き込みタイムアウト設定
	bool SetSendTimeout(int64_t ms) {
		timeval tv = MsToTimeval(ms);
		return setsockopt(m_hSock, SOL_SOCKET, SO_SNDTIMEO, (char*)&tv, sizeof(tv)) == 0;
	}

	//! 書き込みタイムアウト取得
	int64_t GetSendTimeout() {
		timeval tv;
		socklen_t size = sizeof(tv);
		memset(&tv, 0, sizeof(tv));
		if (getsockopt(m_hSock, SOL_SOCKET, SO_SNDTIMEO, (char*)&tv, &size) != 0)
			return -1;
		return TimevalToMs(tv);
	}

	//! Nagleアルゴリズムの無効化の設定
	bool SetNoDelay(int flag) {
		return setsockopt(m_hSock, IPPROTO_TCP, TCP_NODELAY, (char*)&flag, sizeof(flag)) == 0;
	}

	//! Nagleアルゴリズムの無効化の取得
	int GetNoDelay() {
		int flag = 0;
		socklen_t size = sizeof(flag);
		if (getsockopt(m_hSock, IPPROTO_TCP, TCP_NODELAY, (char*)&flag, &size) != 0)
			return -1;
		return flag;
	}

	//! ブロッキングモードの設定
	bool SetBlockingMode(bool blocking) {
#ifdef __GNUC__
		int val = blocking ? 0 : 1;
		return ioctl(m_hSock, FIONBIO, &val) == 0;
#elif defined _MSC_VER
		u_long val = blocking ? 0 : 1;
		return ioctlsocket(m_hSock, FIONBIO, &val) == 0;
#endif
	}

	//! ノンブロッキングモードで Recv() または RecvFrom() 呼び出し時に負数が返った場合にエラーでは無くデータ未取得なのかどうか調べる
	_FINLINE bool NoRecvError() {
#ifdef __GNUC__
		return errno == EAGAIN;
#elif defined _MSC_VER
		return ::WSAGetLastError() == WSAEWOULDBLOCK;
#endif
	}

	//! ブロッキングモードで Send() または SendTo() 呼び出し時に負数が返った場合にタイムアウトによりエラーとなったかどうか調べる
	_FINLINE bool TimedOutSendError() {
#ifdef __GNUC__
		return errno == EAGAIN || errno == EWOULDBLOCK;
#elif defined _MSC_VER
		return ::WSAGetLastError() == WSAETIMEDOUT;
#endif
	}

	//! ブロッキングモードで Recv() または RecvFrom() 呼び出し時に負数が返った場合にタイムアウトによりエラーとなったかどうか調べる
	_FINLINE bool TimedOutRecvError() {
#ifdef __GNUC__
		return errno == EAGAIN || errno == EWOULDBLOCK;
#elif defined _MSC_VER
		return ::WSAGetLastError() == WSAETIMEDOUT;
#endif
	}
};

//! ソケットクラス
struct JUNKAPICLASS Socket: public SocketRef {
	//! コンストラクタ
	Socket() {
	}
	//! コンストラクタ、ソケットハンドル指定して初期化
	Socket(Handle handle) :
			SocketRef(handle) {
	}
	//! デストラクタ
	Socket(const Socket& c) :
			SocketRef(c.m_hSock) {
	}
	~Socket() {
		if (m_hSock == InvalidHandle())
			return;
#ifdef __GNUC__
		close(m_hSock);
#elif defined _MSC_VER
		closesocket(m_hSock);
#endif
	}
};

_JUNK_END

#endif
