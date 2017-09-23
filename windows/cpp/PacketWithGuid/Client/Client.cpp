// Client.cpp : コンソール アプリケーションのエントリ ポイントを定義します。
//

#include "stdafx.h"
#include <iostream>
#include "../Commands.h"
#include "../CancelablePipe.h"

using namespace Packets;

struct Session {
	HANDLE cancel_event_;
	CancelablePipe pipe_;
	ByteBuffer recvbuf_;
	ByteBuffer sendbuf_;

	Session() {
		cancel_event_ = ::CreateEventW(NULL, TRUE, FALSE, NULL);
		pipe_ = CancelablePipe(cancel_event_);
	}
	~Session() {
		Close();
		::CloseHandle(cancel_event_);
	}

	void Open(const wchar_t* pipe_name) {
		HANDLE hPipe = ::CreateFileW(
			pipe_name,   // pipe name 
			GENERIC_READ |  // read and write access 
			GENERIC_WRITE,
			0,              // no sharing 
			NULL,           // default security attributes
			OPEN_EXISTING,  // opens existing pipe 
			0,              // default attributes 
			NULL);          // no template file 

		pipe_.hPipe = hPipe;
		pipe_.hEvent = ::CreateEventW(NULL, TRUE, FALSE, NULL);

		recvbuf_.reserve(4096);
		sendbuf_.reserve(4096);
	}
	void Close() {
		::SetEvent(cancel_event_);
		pipe_.Destroy();
	}

	void AddText(const wchar_t* text) {
		sendbuf_.resize(0);
		AddTextCmd::Write(sendbuf_, text);
		pipe_.WriteToBytes(&sendbuf_[0], sendbuf_.size());

		// まずパケットサイズを読み込む
		pktsize_t packetSize;
		pipe_.ReadToBytes(&packetSize, sizeof(packetSize));

		// パケットサイズが無茶な値なら攻撃かもしれない
		Unpacker::VeryfySize(packetSize);

		// １パケット分データ受信
		recvbuf_.resize(0);
		ToBuffer(recvbuf_, packetSize);
		pipe_.ReadToBytes(recvbuf_, packetSize);
	}

	std::vector<std::wstring> GetAllTexts() {
		sendbuf_.resize(0);
		GetAllTextsCmd::Write(sendbuf_);
		pipe_.WriteToBytes(&sendbuf_[0], sendbuf_.size());

		// まずパケットサイズを読み込む
		pktsize_t packetSize;
		pipe_.ReadToBytes(&packetSize, sizeof(packetSize));

		// パケットサイズが無茶な値なら攻撃かもしれない
		Unpacker::VeryfySize(packetSize);

		// １パケット分データ受信
		recvbuf_.resize(0);
		ToBuffer(recvbuf_, packetSize);
		pipe_.ReadToBytes(recvbuf_, packetSize);

		position_t position = 0;
		Unpacker up(recvbuf_, position);
		GetAllTextsRes res(up);
		return std::move(res.texts);
	}
};

void InteractiveTest() {
	Session s;
	s.Open(L"\\\\.\\pipe\\{CF2D7C03-766F-4922-A926-E4BFA6EFE51A}");

	try {
		for (;;) {
			std::wstring line;
			std::getline(std::wcin, line);
			if (line == L"quit") {
				break;
			} else if (line == L"add") {
				std::wcout << "Input text to send:" << std::endl;
				std::getline(std::wcin, line);
				s.AddText(line.c_str());
			} else if (line == L"get") {
				std::vector<std::wstring> texts = s.GetAllTexts();
				std::wcout << "Retrieved texts:" << std::endl;
				for (intptr_t i = 0; i < (intptr_t)texts.size(); i++) {
					std::wcout << texts[i] << std::endl;
				}
			}
		}
	} catch (std::exception& ex) {
		std::cout << ex.what() << std::endl;
	}
}

void SessionLifeCycleTest() {
	try {
		for (int i = 0; i < 10000000; i++) {
			Session s;
			s.Open(L"\\\\.\\pipe\\{CF2D7C03-766F-4922-A926-E4BFA6EFE51A}");
			s.AddText(L"safsdfsf");
			s.GetAllTexts();
			if (i % 1000 == 0)
				std::wcout << i << std::endl;
			//::Sleep(0);
		}
	} catch (Exception& ex) {
		std::cout << ex.what() << "\n" << ex.MessageFromHresultA() << std::endl;
	} catch (std::exception& stdex) {
		std::cout << stdex.what() << std::endl;
	}
}


int main() {
	//InteractiveTest();
	SessionLifeCycleTest();
	return 0;
}

