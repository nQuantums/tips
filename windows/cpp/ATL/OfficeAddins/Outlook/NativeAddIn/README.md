# メモ
- クラス作成ウィザードにバグがある。
	- ProgIDは何故か無視される、自分で .rgs ファイルに記述する必要がある。
		- [Connect.rgs](./Connect.rgs)の NativeAddin.Connect の部分は自分で記述した。
	- 作成される .idl ファイルにバグがある。
		- [NativeAddIn.idl](./NativeAddIn.idl)の uuid(eb3a0b1c-8c27-4219-b7d9-02d26f966e5c) の部分は最初 uuid(eb3a0b1c-8c27-4219-b7d9-02d26f966e5c]) だった。
- インターフェース実装ウィザードにバグがある。
	- [Connect.h](./Connect.h)の &__uuidof(_IDTExtensibility2) の部分は最初 &LIBID_IDTExtensibility2 だったがどこにも定義されていなかった。
