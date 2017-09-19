#pragma once
#ifndef __JUNK_JRPC_H__
#define __JUNK_JRPC_H__

#include "Socket.h"
#include <exception>
#include <string>
#include <vector>

_JUNK_BEGIN

//! RPCヘルパークラス
struct JRpc {
#pragma pack(push, 1)
	//! 送信コマンドパケットイメージ構造体
	struct Pkt {
		int32_t Size; //!< 以降に続くパケット内データサイズ、PKT_SIZE_MIN～PKT_SIZE_MAX 範囲外の値が指定されるとパケットとはみなされず応答パケットも戻りません
		int32_t CmdId; //!< 先頭の4バイトはコマンド種類ID
		uint8_t Data[1]; //!< パケットデータプレースホルダ、nSize 分のデータが続く
	};

	//! コマンドレスポンスパケットイメージ構造体
	struct PktRes {
		int32_t Size; //!< 以降に続くパケット内データサイズ、4～PKT_SIZE_MAX
		int32_t Result; //!< 結果コード、Rc* の値
		uint8_t Data[1]; //!< パケットデータプレースホルダ、nSize 分のデータが続く
	};
#pragma pack(pop)

	//! 内部で使用する結果コード
	enum Rc {
		RcOK = 0, //!< 正常終了

		// IO関係
		RcIoPktReadFailed = -1000, //!< パケット読み込み中にエラーが発生したかデータが終了した
		RcIoPktResReadFailed = -1001, //!< 応答パケットの読み込みに失敗した
		RcIoPktWriteFailed = -1002, //!< パケット書き込み中にエラーが発生した
		RcIoPktResWriteFailed = -1003, //!< 応答パケット書き込み中にエラーが発生した

		// シリアライズ関係
		RcSlUnknownCmdId = -2000, //!< 不明なコマンドを受け取った
		RcSlFmtUnknown = -2001, //!< 引数フォーマットに不明なタイプが含まれている
		RcSlPktSizeNotEnough = -2002, //!< 引数フォーマットに対してパケットのサイズが小さい
		RcSlPktResSizeNotEnough = -2003, //!< 戻り値フォーマットに対して応答パケットのサイズが小さい
		RcSlPktSizeMismatch = -2004, //!< 引数フォーマットで指定されたサイズとパケットサイズが異なる
		RcSlPktResSizeMismatch = -2005, //!< 戻り値フォーマットで指定されたサイズと応答パケットサイズが異なる
		RcSlPktSizeInvalid = -2006, //!< パケット内のデータサイズ部分(Pkt::Size)が大きすぎるか小さすぎる
		RcSlPktResSizeInvalid = -2007, //!< 応答パケット内のデータサイズ部分(PktRes::Size)が大きすぎるか小さすぎる
		RcSlPktBufSizeNotEnough = -2008, //!< 送信するパケットに引数をマーシャリング中にバッファサイズが足りなくなった
		RcSlPktResBufSizeNotEnough = -2009, //!< 送信する応答パケットに戻り値をマーシャリング中にバッファサイズが足りなくなった

		// サーバー内部処理関係
		RcSvError = -3000, //!< サーバー内部でのエラー
	};

	//enum {
	//	ArrayLenMax = 0x100000, //!< RPCでやりとりする配列要素数最大値
	//};

	//! コマンド情報
	struct Cmd {
		const char* pszRetFmt; //!< コマンド戻り値フォーマット
		const char* pszArgFmt; //!< コマンド引数フォーマット
	};

	//! RPC例外クラス
	struct Exception : public std::exception {
		Rc Code; //!< 結果コード

		//! コンストラクタ、結果コードを指定して初期化する
		Exception(Rc code) {
			this->Code = code;
		}
	};

	//! バッファやりとり用構造体
	template<class T>
	struct Buf {
		T* pBuffer; //!< バッファへのポインタ
		int32_t Capacity; //!< pBuffer が指すバッファの容量
		int32_t Length; //!< pBuffer に入っている有効要素数

		//! デフォルトコンストラクタ
		Buf() {
		}

		//! コンストラクタ、バッファポインタと容量を指定して初期化する
		Buf(T* pbuffer, int32_t capacity) {
			this->pBuffer = pbuffer;
			this->Capacity = capacity;
			this->Length = 0;
		}
	};

	static Rc ReadPkt(SocketRef sk, Pkt* pPkt, intptr_t pktSizeMin, intptr_t pktSizeMax); //!< パケット読み込み
	static Rc ReadPktRes(SocketRef sk, PktRes* pPktRes, intptr_t pktSizeMin, intptr_t pktSizeMax); //!< 応答パケット読み込み
	static Rc WritePkt(SocketRef sk, const Pkt* pPkt); //!< パケットを書き込み
	static Rc WritePktRes(SocketRef sk, const PktRes* pPktRes); //!< 応答パケットを書き込み
	static Rc WritePktReadPktRes(SocketRef sk, const Pkt* pPkt, PktRes* pPktRes, intptr_t pktResSizeMin, intptr_t pktResSizeMax); //!< パケットを書き込み応答パケットを読み込む

	//! パケットのデータ部分に指定値を追加する
	template<class T>
	static _FINLINE void Put(uint8_t*& p, const T& val) {
		*(T*)p = val;
		p += sizeof(T);
	}

	//! パケットのデータ部分に指定値を追加する
	template<class T>
	static _FINLINE bool Put(uint8_t*& p, const uint8_t* e, const T& val) {
		uint8_t* p2 = p + sizeof(T);
		if (e < p2) return false;
		*(T*)p = val;
		p = p2;
		return true;
	}

	//! パケットのデータ部分に指定値を追加する
	template<class T>
	static bool Put(uint8_t*& p, const uint8_t* e, const std::vector<T>& val) {
		int32_t len = (int32_t)val.size();
		uint8_t* p2 = p + sizeof(int32_t) + sizeof(T) * len;
		if (e < p2) return false;
		Put(p, len);
		if (len != 0) {
			const T* psrc = &val[0];
			T* pdst = (T*)p;
			while ((uint8_t*)pdst != p2) {
				*pdst = *psrc;
				psrc++;
				pdst++;
			}
			p = p2;
		}
		return true;
	}

	//! パケットのデータ部分に受け取り用バッファを指定する
	template<class T>
	static _FINLINE bool Put(uint8_t*& p, const uint8_t* e, const Buf<T>& val) {
		int32_t len = (int32_t)val.Length;
		uint8_t* p2 = p + 2 * sizeof(int32_t)+sizeof(T)* len;
		if (e < p2) return false;
		Put(p, val.Capacity);
		Put(p, len);
		if (len != 0) {
			const T* psrc = val.pBuffer;
			T* pdst = (T*)p;
			while ((uint8_t*)pdst != p2) {
				*pdst = *psrc;
				psrc++;
				pdst++;
			}
			p = p2;
		}
		return true;
	}

	//! 応答パケットのデータ部分から値を取得する
	template<class T>
	static _FINLINE void Get(const uint8_t*& p, T& val) {
		val = *(T*)p;
		p += sizeof(T);
	}

	//! 応答パケットのデータ部分から値を取得する
	template<class T>
	static _FINLINE bool Get(const uint8_t*& p, const uint8_t* e, T& val) {
		const uint8_t* p2 = p + sizeof(T);
		if (e < p2) return false;
		val = *(T*)p;
		p = p2;
		return true;
	}

	//! 応答パケットのデータ部分から値を取得する
	template<class T>
	static bool Get(const uint8_t*& p, const uint8_t* e, std::vector<T>& val) {
		const uint8_t* p2 = p + sizeof(int32_t);
		if (e < p2) return false;
		int32_t len;
		Get(p, len);
		int64_t bytes = (int64_t)len * sizeof(T);
		if (len < 0 || e - p2 < bytes) return false;
		p2 += (int32_t)bytes;
		val.resize(len);
		if (len != 0) {
			const T* psrc = (T*)p;
			T* pdst = &val[0];
			while ((uint8_t*)psrc != p2) {
				*pdst = *psrc;
				psrc++;
				pdst++;
			}
			p = p2;
		}
		return true;
	}

	//! 応答パケットのデータ部分から受け取り用バッファを取得する
	template<class T>
	static bool Get(const uint8_t*& p, const uint8_t* e, Buf<T>& val) {
		const uint8_t* p2 = p + sizeof(int32_t);
		if (e < p2) return false;
		int32_t len;
		Get(p, len);
		if (val.Capacity < len) return false;
		int64_t bytes = (int64_t)len * sizeof(T);
		if (len < 0 || e - p2 < bytes) return false;
		p2 += (int32_t)bytes;
		val.Length = len;
		if (len != 0) {
			const T* psrc = (T*)p;
			T* pdst = val.pBuffer;
			while ((uint8_t*)psrc != p2) {
				*pdst = *psrc;
				psrc++;
				pdst++;
			}
			p = p2;
		}
		return true;
	}

	static bool Put(uint8_t*& p, const uint8_t* e, const std::string& val); //!< パケットのデータ部分に文字列を追加する
	static bool Put(uint8_t*& p, const uint8_t* e, const std::vector<std::string>& val); //!< パケットのデータ部分に文字列を追加する
	static bool Get(const uint8_t*& p, const uint8_t* e, std::string& val); //!< 応答パケットのデータ部分から文字列を取得する
	static bool Get(const uint8_t*& p, const uint8_t* e, std::vector<std::string>& val); //!< 応答パケットのデータ部分から文字列を取得する
};

_JUNK_END

#endif
