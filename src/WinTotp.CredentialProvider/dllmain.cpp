#include "common.h"
#define INITGUID
#include <initguid.h>
#include "guid.h"
#include "diag.h"
#include <new>
#include <Shlwapi.h>

HRESULT TotpProvider_CreateInstance(REFIID riid, void** ppv);

static LONG g_cRef = 0;
HINSTANCE g_hinst = nullptr;

void DllAddRef() { InterlockedIncrement(&g_cRef); }
void DllRelease() { InterlockedDecrement(&g_cRef); }

class CClassFactory : public IClassFactory
{
public:
    CClassFactory() : _refs(1) { DllAddRef(); }
    ~CClassFactory() { DllRelease(); }

    IFACEMETHODIMP_(ULONG) AddRef() override { return (ULONG)InterlockedIncrement(&_refs); }
    IFACEMETHODIMP_(ULONG) Release() override
    {
        LONG c = InterlockedDecrement(&_refs);
        if (!c) delete this;
        return (ULONG)c;
    }
    IFACEMETHODIMP QueryInterface(REFIID riid, void** ppv) override
    {
        static const QITAB qit[] = { QITABENT(CClassFactory, IClassFactory), { 0 } };
        return QISearch(this, qit, riid, ppv);
    }
    IFACEMETHODIMP CreateInstance(IUnknown* pUnkOuter, REFIID riid, void** ppv) override
    {
        if (pUnkOuter) return CLASS_E_NOAGGREGATION;
        return TotpProvider_CreateInstance(riid, ppv);
    }
    IFACEMETHODIMP LockServer(BOOL fLock) override
    {
        if (fLock) DllAddRef(); else DllRelease();
        return S_OK;
    }
private:
    LONG _refs;
};

STDAPI DllCanUnloadNow()
{
    return (g_cRef > 0) ? S_FALSE : S_OK;
}

STDAPI DllGetClassObject(REFCLSID rclsid, REFIID riid, void** ppv)
{
    if (rclsid != CLSID_WinTotpProvider)
        return CLASS_E_CLASSNOTAVAILABLE;
    CpLog(L"DllGetClassObject: LogonUI/COM is loading WinTotp CP.");
    CClassFactory* f = new (std::nothrow) CClassFactory();
    if (!f) return E_OUTOFMEMORY;
    HRESULT hr = f->QueryInterface(riid, ppv);
    f->Release();
    return hr;
}

static HRESULT RegisterServer()
{
    wchar_t module[MAX_PATH] = {};
    if (!GetModuleFileNameW(g_hinst, module, MAX_PATH))
        return HRESULT_FROM_WIN32(GetLastError());

    wchar_t clsidKey[128];
    StringCchPrintfW(clsidKey, 128, L"Software\\Classes\\CLSID\\{%08X-%04X-%04X-%02X%02X-%02X%02X%02X%02X%02X%02X}",
        CLSID_WinTotpProvider.Data1, CLSID_WinTotpProvider.Data2, CLSID_WinTotpProvider.Data3,
        CLSID_WinTotpProvider.Data4[0], CLSID_WinTotpProvider.Data4[1],
        CLSID_WinTotpProvider.Data4[2], CLSID_WinTotpProvider.Data4[3],
        CLSID_WinTotpProvider.Data4[4], CLSID_WinTotpProvider.Data4[5],
        CLSID_WinTotpProvider.Data4[6], CLSID_WinTotpProvider.Data4[7]);

    HKEY hKey = nullptr;
    LONG err = RegCreateKeyExW(HKEY_LOCAL_MACHINE, clsidKey, 0, nullptr, 0, KEY_WRITE, nullptr, &hKey, nullptr);
    if (err != ERROR_SUCCESS) return HRESULT_FROM_WIN32(err);
    RegSetValueExW(hKey, nullptr, 0, REG_SZ, (const BYTE*)L"WinTotp Credential Provider",
        (DWORD)((wcslen(L"WinTotp Credential Provider") + 1) * sizeof(wchar_t)));
    RegCloseKey(hKey);

    wchar_t inproc[160];
    StringCchPrintfW(inproc, 160, L"%s\\InprocServer32", clsidKey);
    err = RegCreateKeyExW(HKEY_LOCAL_MACHINE, inproc, 0, nullptr, 0, KEY_WRITE, nullptr, &hKey, nullptr);
    if (err != ERROR_SUCCESS) return HRESULT_FROM_WIN32(err);
    RegSetValueExW(hKey, nullptr, 0, REG_SZ, (const BYTE*)module, (DWORD)((wcslen(module) + 1) * sizeof(wchar_t)));
    RegSetValueExW(hKey, L"ThreadingModel", 0, REG_SZ, (const BYTE*)L"Apartment",
        (DWORD)((wcslen(L"Apartment") + 1) * sizeof(wchar_t)));
    RegCloseKey(hKey);

    wchar_t cpKey[200];
    StringCchPrintfW(cpKey, 200,
        L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Authentication\\Credential Providers\\{%08X-%04X-%04X-%02X%02X-%02X%02X%02X%02X%02X%02X}",
        CLSID_WinTotpProvider.Data1, CLSID_WinTotpProvider.Data2, CLSID_WinTotpProvider.Data3,
        CLSID_WinTotpProvider.Data4[0], CLSID_WinTotpProvider.Data4[1],
        CLSID_WinTotpProvider.Data4[2], CLSID_WinTotpProvider.Data4[3],
        CLSID_WinTotpProvider.Data4[4], CLSID_WinTotpProvider.Data4[5],
        CLSID_WinTotpProvider.Data4[6], CLSID_WinTotpProvider.Data4[7]);
    err = RegCreateKeyExW(HKEY_LOCAL_MACHINE, cpKey, 0, nullptr, 0, KEY_WRITE, nullptr, &hKey, nullptr);
    if (err != ERROR_SUCCESS) return HRESULT_FROM_WIN32(err);
    RegSetValueExW(hKey, nullptr, 0, REG_SZ, (const BYTE*)L"WinTotpCredentialProvider",
        (DWORD)((wcslen(L"WinTotpCredentialProvider") + 1) * sizeof(wchar_t)));
    RegCloseKey(hKey);

    return S_OK;
}

static HRESULT UnregisterServer()
{
    wchar_t clsidKey[128];
    StringCchPrintfW(clsidKey, 128, L"Software\\Classes\\CLSID\\{%08X-%04X-%04X-%02X%02X-%02X%02X%02X%02X%02X%02X}",
        CLSID_WinTotpProvider.Data1, CLSID_WinTotpProvider.Data2, CLSID_WinTotpProvider.Data3,
        CLSID_WinTotpProvider.Data4[0], CLSID_WinTotpProvider.Data4[1],
        CLSID_WinTotpProvider.Data4[2], CLSID_WinTotpProvider.Data4[3],
        CLSID_WinTotpProvider.Data4[4], CLSID_WinTotpProvider.Data4[5],
        CLSID_WinTotpProvider.Data4[6], CLSID_WinTotpProvider.Data4[7]);
    RegDeleteTreeW(HKEY_LOCAL_MACHINE, clsidKey);

    wchar_t cpKey[200];
    StringCchPrintfW(cpKey, 200,
        L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Authentication\\Credential Providers\\{%08X-%04X-%04X-%02X%02X-%02X%02X%02X%02X%02X%02X}",
        CLSID_WinTotpProvider.Data1, CLSID_WinTotpProvider.Data2, CLSID_WinTotpProvider.Data3,
        CLSID_WinTotpProvider.Data4[0], CLSID_WinTotpProvider.Data4[1],
        CLSID_WinTotpProvider.Data4[2], CLSID_WinTotpProvider.Data4[3],
        CLSID_WinTotpProvider.Data4[4], CLSID_WinTotpProvider.Data4[5],
        CLSID_WinTotpProvider.Data4[6], CLSID_WinTotpProvider.Data4[7]);
    RegDeleteTreeW(HKEY_LOCAL_MACHINE, cpKey);
    return S_OK;
}

STDAPI DllRegisterServer()
{
    return RegisterServer();
}

STDAPI DllUnregisterServer()
{
    return UnregisterServer();
}

BOOL WINAPI DllMain(HINSTANCE hinst, DWORD dwReason, LPVOID)
{
    if (dwReason == DLL_PROCESS_ATTACH)
    {
        g_hinst = hinst;
        DisableThreadLibraryCalls(hinst);
    }
    return TRUE;
}
