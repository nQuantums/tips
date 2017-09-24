// Client.cpp : コンソール アプリケーションのエントリ ポイントを定義します。
//

#include "stdafx.h"
#include <iostream>
#include <iomanip>
#include "../Commands.h"
#include "../CancelablePipe.h"
#include "../ThreadPool.h"


using namespace Packets;

#define PIPE_NAME L"\\\\.\\pipe\\{CF2D7C03-766F-4922-A926-E4BFA6EFE51A}"


struct Session {
	HANDLE cancel_event_;
	CancelablePipe pipe_;
	ByteBuffer recvbuf_;
	ByteBuffer sendbuf_;
	CriticalSection critical_section_; // 破棄されたパイプハンドルへアクセスさせない為に使用

	Session() {
		cancel_event_ = ::CreateEventW(NULL, TRUE, FALSE, NULL);
		pipe_ = CancelablePipe(cancel_event_);
	}
	~Session() {
		Close();
		::CloseHandle(cancel_event_);
	}

	void Open(const wchar_t* pipe_name) {
		::ResetEvent(cancel_event_);
		pipe_.Open(pipe_name);
		recvbuf_.reserve(4096);
		sendbuf_.reserve(4096);
		recvbuf_.resize(0);
		sendbuf_.resize(0);
	}
	void Close() {
		// とりあえずパイプ処理をキャンセルさせる
		::SetEvent(cancel_event_);

		// キャンセル完了を待ってからハンドルを破棄する
		LockGuard<CriticalSection> scope_guard(&critical_section_);
		pipe_.Destroy();
	}

	void Recv() {
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

	HRESULT AddText(const wchar_t* text) {
		LockGuard<CriticalSection> scope_guard(&critical_section_);
		if (pipe_.hPipe == INVALID_HANDLE_VALUE) {
			throw Exception(__FUNCTION__, E_ABORT);
		}

		sendbuf_.resize(0);
		AddTextCmd::Write(sendbuf_, text);
		pipe_.WriteToBytes(&sendbuf_[0], sendbuf_.size());

		Recv();

		position_t position = 0;
		Unpacker up(recvbuf_, position);
		AddTextRes res(up);

		return res.hr;
	}

	std::vector<std::wstring> GetAllTexts() {
		LockGuard<CriticalSection> scope_guard(&critical_section_);
		if (pipe_.hPipe == INVALID_HANDLE_VALUE) {
			throw Exception(__FUNCTION__, E_ABORT);
		}

		sendbuf_.resize(0);
		GetAllTextsCmd::Write(sendbuf_);
		pipe_.WriteToBytes(&sendbuf_[0], sendbuf_.size());

		Recv();

		position_t position = 0;
		Unpacker up(recvbuf_, position);
		GetAllTextsRes res(up);

		return std::move(res.texts);
	}
};

void InteractiveTest() {
	Session s;
	s.Open(PIPE_NAME);

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
			s.Open(PIPE_NAME);
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

void SessionAbortTest() {
	struct Task : public ThreadPool::Task {
		Session* session_;

		Task(Session& session) {
			session_ = &session;
		}
		void Finalize() {
			delete this;
			std::wcout << __FUNCTIONW__ << std::endl;
		}
		void DoTask() {
			try {
				for (int i = 0; i < 100000; i++) {
					session_->GetAllTexts();
					if (i % 1000 == 0)
						std::wcout << i << std::endl;
				}
			} catch (Exception& ex) {
				std::cout << ex.what() << "\n" << std::hex << std::setw(8) << std::setfill('0') << std::hex << ex.Hresult() << ": " << ex.MessageFromHresultA() << std::endl;
			} catch (std::exception& stdex) {
				std::cout << stdex.what() << std::endl;
			}
		}
	};

	try {
		Session s;
		s.Open(PIPE_NAME);
		{
			ThreadPool tp;
			intptr_t thread_count = 25;
			tp.Create(thread_count / 5);
			for (intptr_t i = 0; i < thread_count; i++) {
				tp.QueueTask(new Task(s));
			}
			for (;;) {
				std::wstring line;
				std::getline(std::wcin, line);
				if (line == L"quit") {
					s.Close();
					break;
				}
			}
		}
	} catch (std::exception& ex) {
		std::cout << ex.what() << std::endl;
	}
}


int main() {
	//CancelablePipe pipe;
	//pipe.Open(PIPE_NAME);
	//std::wcout << pipe.hPipe << std::endl;
	//pipe.Destroy();
	//pipe.Open(PIPE_NAME);
	//std::wcout << pipe.hPipe << std::endl;
	//pipe.Destroy();

	//InteractiveTest();
	//SessionLifeCycleTest();
	SessionAbortTest();
	return 0;
}

