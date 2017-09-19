#include "JRpc.h"
#include <algorithm>
#include <string.h>

_JUNK_BEGIN


//! 指定サイズ分読み込む
static bool RecvToSize(SocketRef sk, uint8_t* pBuf, size_t size) {
	while (size) {
		int n = sk.Recv(pBuf, size);
		if (n <= 0)
			return false;
		pBuf += n;
		size -= n;
	}
	return true;
}

//! 指定サイズ分書き込む
static bool SendToSize(SocketRef sk, uint8_t* pBuf, size_t size) {
	while (size) {
		int n = sk.Send(pBuf, size);
		if (n <= 0)
			return false;
		pBuf += n;
		size -= n;
	}
	return true;
}


//! パケット読み込み
//! @return JRpc::Rc 結果コード
JRpc::Rc JRpc::ReadPkt(
	SocketRef sk, //!< [in] ソケット
	Pkt* pPkt, //!< [out] 読み込み先パケット、pktSizeMax + 4 以上のバッファサイズが必要
	intptr_t pktSizeMin, //!< [in] 最小パケットサイズ、Pkt::Size がこの値未満ならばエラーと判断する
	intptr_t pktSizeMax //!< [in] 最大パケットサイズ、Pkt::Size がこの値を超えたらエラーと判断する
) {
	// パケットサイズ部分を読み込む
	if (!RecvToSize(sk, (uint8_t*)pPkt, 4)) {
		return RcIoPktReadFailed;
	}

	// パケットサイズチェック
	intptr_t size = pPkt->Size;
	if (size < pktSizeMin || pktSizeMax < size) {
		return RcSlPktSizeInvalid;
	}

	// パケットサイズ分読み込む
	if (!RecvToSize(sk, (uint8_t*)pPkt + 4, size)) {
		return RcIoPktReadFailed;
	}

	return RcOK;
}

//! 応答パケット読み込み
//! @return JRpc::Rc 結果コード
JRpc::Rc JRpc::ReadPktRes(
	SocketRef sk, //!< [in] ソケット
	PktRes* pPktRes, //!< [out] 読み込み先応答パケット、pktSizeMax + 4 以上のバッファサイズが必要
	intptr_t pktSizeMin, //!< [in] 最小パケットサイズ、PktRes::Size がこの値未満ならばエラーと判断する
	intptr_t pktSizeMax //!< [in] 最大パケットサイズ、PktRes::Size がこの値を超えたらエラーと判断する
) {
	// パケットサイズ部分を読み込む
	if (!RecvToSize(sk, (uint8_t*)pPktRes, 4)) {
		return RcIoPktResReadFailed;
	}

	// パケットサイズチェック
	intptr_t size = pPktRes->Size;
	if (size < pktSizeMin || pktSizeMax < size) {
		return RcSlPktResSizeInvalid;
	}

	// パケットサイズ分読み込む
	if (!RecvToSize(sk, (uint8_t*)pPktRes + 4, size)) {
		return RcIoPktResReadFailed;
	}

	// 結果コードを判定
	if (pPktRes->Result < 0)
		return (JRpc::Rc)pPktRes->Result;

	return RcOK;
}

//! パケットを書き込み
//! @return JRpc::Rc 結果コード
JRpc::Rc JRpc::WritePkt(
	SocketRef sk, //!< [in] ソケット
	const Pkt* pPkt //!< [in] 書き込むパケット
) {
	if (!SendToSize(sk, (uint8_t*)pPkt, 4 + pPkt->Size)) {
		return RcIoPktWriteFailed;
	}
	return RcOK;
}

//! 応答パケットを書き込み
//! @return JRpc::Rc 結果コード
JRpc::Rc JRpc::WritePktRes(
	SocketRef sk, //!< [in] ソケット
	const PktRes* pPktRes //!< [in] 書き込む応答パケット
) {
	if (!SendToSize(sk, (uint8_t*)pPktRes, 4 + pPktRes->Size)) {
		return RcIoPktResWriteFailed;
	}
	return RcOK;
}

//! パケットを書き込み応答パケットを読み込む
//! @return JRpc::Rc 結果コード
JRpc::Rc JRpc::WritePktReadPktRes(
	SocketRef sk, //!< [in] ソケット
	const Pkt* pPkt, //!< [in] 書き込むパケット
	PktRes* pPktRes, //!< [out] 読み込み先応答パケット、pktResSizeMax + 4 以上のバッファサイズが必要
	intptr_t pktResSizeMin, //!< [in] 最小パケットサイズ、PktRes::Size がこの値未満ならばエラーと判断する
	intptr_t pktResSizeMax //!< [in] 最大パケットサイズ、PktRes::Size がこの値を超えたらエラーと判断する
) {
	// パケットを書き込む
	if (!SendToSize(sk, (uint8_t*)pPkt, 4 + pPkt->Size)) {
		return RcIoPktWriteFailed;
	}

	// 応答パケットサイズ部分を読み込む
	if (!RecvToSize(sk, (uint8_t*)pPktRes, 4)) {
		return RcIoPktResReadFailed;
	}

	// 応答パケットサイズチェック
	intptr_t size = pPktRes->Size;
	if (size < pktResSizeMin || pktResSizeMax < size) {
		return RcSlPktResSizeInvalid;
	}

	// 応答パケットサイズ分読み込む
	if (!RecvToSize(sk, (uint8_t*)pPktRes + 4, size)) {
		return RcIoPktResReadFailed;
	}

	// 応答パケットの結果コードを判定
	if (pPktRes->Result < 0)
		return (JRpc::Rc)pPktRes->Result;

	return RcOK;
}

//! パケットのデータ部分に文字列を追加する
bool JRpc::Put(
	uint8_t*& p, //!< [in,out] 書き込み先ポインタ
	const uint8_t* e, //!< [in] バッファの終端
	const std::string& val //!< [in] 文字列
) {
	int32_t len = (int32_t)val.size();
	uint8_t* p2 = p + sizeof(int32_t) + len;
	if (e < p2) return false;
	Put(p, len);
	if (len != 0) memcpy(p, val.c_str(), len);
	p = p2;
	return true;
}

//! パケットのデータ部分に文字列を追加する
bool JRpc::Put(
	uint8_t*& p, //!< [in,out] 書き込み先ポインタ
	const uint8_t* e, //!< [in] バッファの終端
	const std::vector<std::string>& val //!< [in] 文字列
) {
	int32_t len = (int32_t)val.size();
	if (e < p + sizeof(int32_t)) return false;
	Put(p, len);
	if (len != 0) {
		const std::string* psrc = &val[0];
		const std::string* psrce = psrc + len;
		while (psrc != psrce) {
			if (!Put(p, e, *psrc)) return false;
			psrc++;
		}
	}
	return true;
}

//! 応答パケットのデータ部分から文字列を取得する
bool JRpc::Get(
	const uint8_t*& p, //!< [in,out] 読み込み元ポインタ
	const uint8_t* e, //!< [in] バッファの終端
	std::string& val //!< [out] 文字列が返る
) {
	const uint8_t* p2 = p + sizeof(int32_t);
	if (e < p2) return false;
	int32_t len;
	Get(p, len);
	if (len < 0 || e - p2 < len) return false;
	p2 += len;
	val.assign((char*)p, (char*)p2);
	p = p2;
	return true;
}

//! 応答パケットのデータ部分から文字列を取得する
bool JRpc::Get(
	const uint8_t*& p, //!< [in,out] 読み込み元ポインタ
	const uint8_t* e, //!< [in] バッファの終端
	std::vector<std::string>& val //!< [out] 文字列が返る
) {
	if (e < p + sizeof(int32_t)) return false;
	int32_t len;
	Get(p, len);
	int64_t bytes = (int64_t)len * sizeof(int32_t);
	if (len < 0 || e - p < bytes) return false;
	val.resize(len);
	if (len != 0) {
		std::string* pdst = &val[0];
		std::string* pdste = pdst + len;
		while (pdst != pdste) {
			if (!Get(p, e, *pdst)) return false;
			pdst++;
		}
	}
	return true;
}

_JUNK_END
