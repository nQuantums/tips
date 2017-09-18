// dllmain.h : モジュール クラスの宣言です。

class CNativeAddInModule : public ATL::CAtlDllModuleT< CNativeAddInModule >
{
public :
	DECLARE_LIBID(LIBID_NativeAddInLib)
	DECLARE_REGISTRY_APPID_RESOURCEID(IDR_NATIVEADDIN, "{5ce2978f-238f-4c52-aeb6-f9545da50eb5}")
};

extern class CNativeAddInModule _AtlModule;
