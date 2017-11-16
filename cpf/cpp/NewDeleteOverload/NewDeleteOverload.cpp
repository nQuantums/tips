// NewDeleteOverload.cpp : コンソール アプリケーションのエントリ ポイントを定義します。
//

#include "stdafx.h"
#include "FileManager.h"

void* operator new(std::size_t size) {
	return malloc(size);
}
void operator delete(void* p) noexcept {
	free(p);
}


int main() {
	//std::unordered_set<ConstStringA::UniquePtr> texts;

	//texts.insert(ConstStringA::New("afefe"));
	//texts.insert(ConstStringA::New("gufefe"));
	//texts.insert(ConstStringA::New("gufefe"));
	//for (auto& s : texts) {
	//	std::cout << s << std::endl;
	//}

	//{
	//	ConstStringA cs("gufefe");
	//	ConstStringA::UniquePtr up(&cs);
	//	auto iter = texts.find(up);
	//	if (iter != texts.end()) {
	//		std::cout << *iter << std::endl;
	//	}
	//}

	//ConstStringA::SharedPtr spsave;
	//{
	//	auto sp = ConstStringA::NewShared("afefe");
	//	spsave = sp;
	//}
	//std::cout << spsave << std::endl;
	//spsave = nullptr;


	FileManager fm;
	auto file1 = fm.GetFile("c:\\work\\test1.txt");
	auto file1_loc1 = file1->GetLocation(Position(1, 2));
	auto file1_loc2 = file1->GetLocation(Position(2, 3));
	auto file2 = fm.GetFile("c:\\work\\test2.txt");
	auto file2_loc1 = file2->GetLocation(Position(1, 2));
	auto file2_loc2 = file2->GetLocation(Position(2, 3));

	return 0;
}
