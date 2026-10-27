#include "TotpProvider.h"
#include "helpers.h"
#include "guid.h"
#include "diag.h"
#include <Shlwapi.h>
#include <propkey.h>
#include <new>

extern void DllAddRef();
extern void DllRelease();

namespace
{
    // GUID values from credentialprovider.h (avoid INITGUID link issues).
    // V2 LogonUI shows providers under "Sign-in options" ONLY when TILE_IMAGE uses
    // CPFG_CREDENTIAL_PROVIDER_LOGO and SMALL_TEXT uses CPFG_CREDENTIAL_PROVIDER_LABEL.
    static const GUID kCpfgLogo =
        { 0x2d837775, 0xf6cd, 0x464e, { 0xa7, 0x45, 0x48, 0x2f, 0xd0, 0xb4, 0x74, 0x93 } };
    static const GUID kCpfgLabel =
        { 0x286bbff3, 0xbad4, 0x464f, { 0xad, 0x0e, 0x93, 0x4b, 0xaa, 0xc8, 0x24, 0xaa } };
    static const GUID kCpfgPassword =
        { 0x606da536, 0x9609, 0x4d62, { 0xb1, 0x53, 0xae, 0x6a, 0xc9, 0x4c, 0x5c, 0x5c } };

    static CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR s_fields[] =
    {
        { TFI_TILEIMAGE, CPFT_TILE_IMAGE, (LPWSTR)L"Image", kCpfgLogo },
        { TFI_LABEL, CPFT_SMALL_TEXT, (LPWSTR)L"WinTotp", kCpfgLabel },
        { TFI_OTP, CPFT_PASSWORD_TEXT, (LPWSTR)L"Authenticator code", kCpfgPassword },
        { TFI_SUBMIT, CPFT_SUBMIT_BUTTON, (LPWSTR)L"Sign in", GUID_NULL },
    };
}

TotpProvider::TotpProvider()
{
    DllAddRef();
}

TotpProvider::~TotpProvider()
{
    if (_credential) { _credential->Release(); _credential = nullptr; }
    if (_users) { _users->Release(); _users = nullptr; }
    ClearTotpCpConfig(_cfg);
    DllRelease();
}

IFACEMETHODIMP_(ULONG) TotpProvider::AddRef()
{
    return (ULONG)InterlockedIncrement(&_refs);
}

IFACEMETHODIMP_(ULONG) TotpProvider::Release()
{
    LONG c = InterlockedDecrement(&_refs);
    if (!c) delete this;
    return (ULONG)c;
}

IFACEMETHODIMP TotpProvider::QueryInterface(REFIID riid, void** ppv)
{
    static const QITAB qit[] =
    {
        QITABENT(TotpProvider, ICredentialProvider),
        QITABENT(TotpProvider, ICredentialProviderSetUserArray),
        { 0 },
    };
    return QISearch(this, qit, riid, ppv);
}

HRESULT TotpProvider_CreateInstance(REFIID riid, void** ppv)
{
    TotpProvider* p = new (std::nothrow) TotpProvider();
    if (!p) return E_OUTOFMEMORY;
    HRESULT hr = p->QueryInterface(riid, ppv);
    p->Release();
    return hr;
}

IFACEMETHODIMP TotpProvider::SetUsageScenario(CREDENTIAL_PROVIDER_USAGE_SCENARIO cpus, DWORD)
{
    switch (cpus)
    {
    case CPUS_LOGON:
    case CPUS_UNLOCK_WORKSTATION:
        _cpus = cpus;
        ClearTotpCpConfig(_cfg);
        _configOk = LoadTotpCpConfig(_cfg);
        if (_configOk)
            CpLog(L"SetUsageScenario: config loaded OK.");
        else
            CpLog(L"SetUsageScenario: FAILED to load ProgramData\\WinTotp\\cp-config.dpapi (CP tile hidden).");
        return S_OK;
    default:
        return E_NOTIMPL;
    }
}

IFACEMETHODIMP TotpProvider::SetSerialization(const CREDENTIAL_PROVIDER_CREDENTIAL_SERIALIZATION*)
{
    return E_NOTIMPL;
}

IFACEMETHODIMP TotpProvider::Advise(ICredentialProviderEvents*, UINT_PTR)
{
    return S_OK;
}

IFACEMETHODIMP TotpProvider::UnAdvise()
{
    return S_OK;
}

IFACEMETHODIMP TotpProvider::GetFieldDescriptorCount(DWORD* pdwCount)
{
    *pdwCount = ARRAYSIZE(s_fields);
    return S_OK;
}

IFACEMETHODIMP TotpProvider::GetFieldDescriptorAt(DWORD dwIndex, CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR** ppcpfd)
{
    if (dwIndex >= ARRAYSIZE(s_fields) || !ppcpfd)
        return E_INVALIDARG;
    return FieldDescriptorCoAllocCopy(s_fields[dwIndex], ppcpfd);
}

IFACEMETHODIMP TotpProvider::SetUserArray(ICredentialProviderUserArray* users)
{
    if (_users) { _users->Release(); _users = nullptr; }
    _users = users;
    if (_users) _users->AddRef();
    return S_OK;
}

IFACEMETHODIMP TotpProvider::GetCredentialCount(
    DWORD* pdwCount,
    DWORD* pdwDefault,
    BOOL* pbAutoLogonWithDefault)
{
    *pdwDefault = CREDENTIAL_PROVIDER_NO_DEFAULT;
    *pbAutoLogonWithDefault = FALSE;
    *pdwCount = 0;

    if (!_configOk)
    {
        CpLog(L"GetCredentialCount: no config — returning 0 credentials.");
        return S_OK;
    }

    if (_credential) { _credential->Release(); _credential = nullptr; }

    ICredentialProviderUser* matched = nullptr;
    if (_users)
    {
        DWORD count = 0;
        if (SUCCEEDED(_users->GetCount(&count)))
        {
            for (DWORD i = 0; i < count; ++i)
            {
                ICredentialProviderUser* user = nullptr;
                if (FAILED(_users->GetAt(i, &user)) || !user)
                    continue;
                PWSTR name = nullptr;
                // Prefer SAM-compatible name
                HRESULT hrName = user->GetStringValue(PKEY_Identity_UserName, &name);
                bool isMatch = false;
                if (SUCCEEDED(hrName) && name)
                {
                    // name may be "COMPUTER\user" or "user"
                    std::wstring n(name);
                    auto slash = n.find_last_of(L'\\');
                    std::wstring sam = (slash == std::wstring::npos) ? n : n.substr(slash + 1);
                    if (_wcsicmp(sam.c_str(), _cfg.targetUser.c_str()) == 0)
                        isMatch = true;
                    CoTaskMemFree(name);
                }
                if (isMatch)
                {
                    matched = user; // already AddRef'd by GetAt
                    break;
                }
                user->Release();
            }
        }
    }

    // Even if the user tile list has no match, still offer a CP tile for the configured account.
    TotpCredential* cred = new (std::nothrow) TotpCredential();
    if (!cred)
    {
        if (matched) matched->Release();
        return E_OUTOFMEMORY;
    }

    HRESULT hr = cred->Initialize(_cpus, _cfg, matched);
    if (matched) matched->Release();

    if (FAILED(hr))
    {
        cred->Release();
        return hr;
    }

    _credential = cred;
    *pdwCount = 1;
    // Do not force default: empty submit in broken remote sessions causes instant "wrong password".
    *pdwDefault = CREDENTIAL_PROVIDER_NO_DEFAULT;
    CpLog(L"GetCredentialCount: offering 1 WinTotp credential.");
    return S_OK;
}

IFACEMETHODIMP TotpProvider::GetCredentialAt(DWORD dwIndex, ICredentialProviderCredential** ppcpc)
{
    if (dwIndex != 0 || !_credential || !ppcpc)
        return E_INVALIDARG;
    return _credential->QueryInterface(IID_PPV_ARGS(ppcpc));
}
