#include "TotpCredential.h"
#include "helpers.h"
#include "totp.h"
#include "guid.h"
#include "diag.h"
#include <Shlwapi.h>
#include <sddl.h>
#include <new>

#pragma comment(lib, "Advapi32.lib")
#pragma comment(lib, "Gdi32.lib")
#pragma comment(lib, "User32.lib")

namespace
{
    std::wstring LookupLocalUserSid(const std::wstring& sam)
    {
        if (sam.empty()) return {};
        BYTE sid[SECURITY_MAX_SID_SIZE] = {};
        DWORD sidLen = sizeof(sid);
        WCHAR domain[256] = {};
        DWORD domainLen = 256;
        SID_NAME_USE use = SidTypeUnknown;
        if (!LookupAccountNameW(nullptr, sam.c_str(), sid, &sidLen, domain, &domainLen, &use))
            return {};
        LPWSTR sidStr = nullptr;
        if (!ConvertSidToStringSidW(sid, &sidStr) || !sidStr)
            return {};
        std::wstring out = sidStr;
        LocalFree(sidStr);
        return out;
    }
}

TotpCredential::TotpCredential() = default;

TotpCredential::~TotpCredential()
{
    for (auto& s : _rgFieldStrings)
    {
        if (s) { CoTaskMemFree(s); s = nullptr; }
    }
    if (_tileBitmap)
    {
        DeleteObject(_tileBitmap);
        _tileBitmap = nullptr;
    }
    ClearTotpCpConfig(_cfg);
    SecureClearWString(_otp);
    SecureClearWString(_userSid);
    if (_events) { _events->Release(); _events = nullptr; }
}

namespace
{
    HBITMAP CreateTotpLogoBitmap()
    {
        const int size = 128;
        BITMAPINFO bmi{};
        bmi.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
        bmi.bmiHeader.biWidth = size;
        bmi.bmiHeader.biHeight = -size; // top-down
        bmi.bmiHeader.biPlanes = 1;
        bmi.bmiHeader.biBitCount = 32;
        bmi.bmiHeader.biCompression = BI_RGB;

        void* bits = nullptr;
        HDC hdcScreen = GetDC(nullptr);
        HDC hdc = CreateCompatibleDC(hdcScreen);
        HBITMAP hbmp = CreateDIBSection(hdc, &bmi, DIB_RGB_COLORS, &bits, nullptr, 0);
        if (!hbmp || !bits)
        {
            if (hdc) DeleteDC(hdc);
            if (hdcScreen) ReleaseDC(nullptr, hdcScreen);
            return nullptr;
        }

        auto* px = static_cast<DWORD*>(bits);
        for (int i = 0; i < size * size; ++i)
            px[i] = 0xFF0078D4; // BGRA Win11-ish blue

        HGDIOBJ old = SelectObject(hdc, hbmp);
        SetBkMode(hdc, TRANSPARENT);
        SetTextColor(hdc, RGB(255, 255, 255));
        HFONT font = CreateFontW(48, 0, 0, 0, FW_BOLD, FALSE, FALSE, FALSE,
            DEFAULT_CHARSET, OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS,
            CLEARTYPE_QUALITY, DEFAULT_PITCH | FF_SWISS, L"Segoe UI");
        HGDIOBJ oldFont = font ? SelectObject(hdc, font) : nullptr;
        RECT rc{ 0, 0, size, size };
        DrawTextW(hdc, L"TR", -1, &rc, DT_CENTER | DT_VCENTER | DT_SINGLELINE);
        if (oldFont) SelectObject(hdc, oldFont);
        if (font) DeleteObject(font);
        SelectObject(hdc, old);
        DeleteDC(hdc);
        ReleaseDC(nullptr, hdcScreen);
        return hbmp;
    }
}

IFACEMETHODIMP_(ULONG) TotpCredential::AddRef()
{
    return (ULONG)InterlockedIncrement(&_refs);
}

IFACEMETHODIMP_(ULONG) TotpCredential::Release()
{
    LONG c = InterlockedDecrement(&_refs);
    if (!c) delete this;
    return (ULONG)c;
}

IFACEMETHODIMP TotpCredential::QueryInterface(REFIID riid, void** ppv)
{
    static const QITAB qit[] =
    {
        QITABENT(TotpCredential, ICredentialProviderCredential),
        QITABENT(TotpCredential, ICredentialProviderCredential2),
        { 0 },
    };
    return QISearch(this, qit, riid, ppv);
}

HRESULT TotpCredential::Initialize(
    CREDENTIAL_PROVIDER_USAGE_SCENARIO cpus,
    const TotpCpConfig& cfg,
    ICredentialProviderUser* pUser)
{
    _cpus = cpus;
    _cfg.targetUser = cfg.targetUser;
    _cfg.totpSecret = cfg.totpSecret;
    _cfg.totpStepSeconds = cfg.totpStepSeconds;
    _cfg.staticPassword = cfg.staticPassword;

    HRESULT hr = S_OK;
    if (pUser)
    {
        PWSTR sid = nullptr;
        if (SUCCEEDED(pUser->GetSid(&sid)) && sid)
        {
            _userSid = sid;
            CoTaskMemFree(sid);
        }
    }
    // Always bind to the configured local account so the tile appears under that user.
    if (_userSid.empty())
        _userSid = LookupLocalUserSid(_cfg.targetUser);

    if (_userSid.empty())
        CpLog(L"Initialize: could not resolve user SID — tile may not attach to user.");
    else
        CpLog((L"Initialize: sid=" + _userSid + L" user=" + _cfg.targetUser).c_str());

    auto dup = [](PCWSTR s, PWSTR* out) -> HRESULT {
        return SHStrDupW(s ? s : L"", out);
    };

    _tileBitmap = CreateTotpLogoBitmap();

    if (SUCCEEDED(hr)) hr = dup(L"", &_rgFieldStrings[TFI_TILEIMAGE]);
    if (SUCCEEDED(hr))
        hr = dup(L"WinTotp", &_rgFieldStrings[TFI_LABEL]); // hover text under Sign-in options
    if (SUCCEEDED(hr)) hr = dup(L"", &_rgFieldStrings[TFI_OTP]);
    if (SUCCEEDED(hr)) hr = dup(L"Sign in", &_rgFieldStrings[TFI_SUBMIT]);
    return hr;
}

IFACEMETHODIMP TotpCredential::Advise(ICredentialProviderCredentialEvents* pcpce)
{
    if (_events) { _events->Release(); _events = nullptr; }
    _events = pcpce;
    if (_events) _events->AddRef();
    return S_OK;
}

IFACEMETHODIMP TotpCredential::UnAdvise()
{
    if (_events) { _events->Release(); _events = nullptr; }
    return S_OK;
}

IFACEMETHODIMP TotpCredential::SetSelected(BOOL* pbAutoLogon)
{
    *pbAutoLogon = FALSE;
    return S_OK;
}

IFACEMETHODIMP TotpCredential::SetDeselected()
{
    SecureClearWString(_otp);
    if (_rgFieldStrings[TFI_OTP])
    {
        CoTaskMemFree(_rgFieldStrings[TFI_OTP]);
        _rgFieldStrings[TFI_OTP] = nullptr;
        SHStrDupW(L"", &_rgFieldStrings[TFI_OTP]);
        if (_events)
            _events->SetFieldString(this, TFI_OTP, _rgFieldStrings[TFI_OTP]);
    }
    return S_OK;
}

IFACEMETHODIMP TotpCredential::GetFieldState(
    DWORD dwFieldID,
    CREDENTIAL_PROVIDER_FIELD_STATE* pcpfs,
    CREDENTIAL_PROVIDER_FIELD_INTERACTIVE_STATE* pcpfis)
{
    if (dwFieldID >= TFI_NUM_FIELDS) return E_INVALIDARG;
    *pcpfis = CPFIS_NONE;
    switch (dwFieldID)
    {
    case TFI_TILEIMAGE:
        // Required for V2: icon under "Sign-in options" / 登录选项.
        *pcpfs = CPFS_DISPLAY_IN_BOTH;
        break;
    case TFI_LABEL:
        // CPFG_CREDENTIAL_PROVIDER_LABEL supplies tooltip; sample keeps this HIDDEN.
        *pcpfs = CPFS_HIDDEN;
        break;
    case TFI_OTP:
        *pcpfs = CPFS_DISPLAY_IN_SELECTED_TILE;
        *pcpfis = CPFIS_FOCUSED;
        break;
    case TFI_SUBMIT:
        *pcpfs = CPFS_DISPLAY_IN_SELECTED_TILE;
        break;
    default:
        *pcpfs = CPFS_HIDDEN;
        break;
    }
    return S_OK;
}

IFACEMETHODIMP TotpCredential::GetStringValue(DWORD dwFieldID, PWSTR* ppwsz)
{
    if (dwFieldID >= TFI_NUM_FIELDS) return E_INVALIDARG;
    return SHStrDupW(_rgFieldStrings[dwFieldID] ? _rgFieldStrings[dwFieldID] : L"", ppwsz);
}

IFACEMETHODIMP TotpCredential::GetBitmapValue(DWORD dwFieldID, HBITMAP* phbmp)
{
    *phbmp = nullptr;
    if (dwFieldID != TFI_TILEIMAGE)
        return E_INVALIDARG;
    if (!_tileBitmap)
        _tileBitmap = CreateTotpLogoBitmap();
    if (!_tileBitmap)
        return E_FAIL;
    // LogonUI does not take ownership; we free in destructor.
    *phbmp = _tileBitmap;
    return S_OK;
}

IFACEMETHODIMP TotpCredential::GetCheckboxValue(DWORD, BOOL*, PWSTR*)
{
    return E_NOTIMPL;
}

IFACEMETHODIMP TotpCredential::GetSubmitButtonValue(DWORD dwFieldID, DWORD* pdwAdjacentTo)
{
    if (dwFieldID != TFI_SUBMIT) return E_INVALIDARG;
    *pdwAdjacentTo = TFI_OTP;
    return S_OK;
}

IFACEMETHODIMP TotpCredential::GetComboBoxValueCount(DWORD, DWORD*, DWORD*)
{
    return E_NOTIMPL;
}

IFACEMETHODIMP TotpCredential::GetComboBoxValueAt(DWORD, DWORD, PWSTR*)
{
    return E_NOTIMPL;
}

IFACEMETHODIMP TotpCredential::SetStringValue(DWORD dwFieldID, PCWSTR pwz)
{
    if (dwFieldID != TFI_OTP) return E_INVALIDARG;
    if (_rgFieldStrings[TFI_OTP])
    {
        CoTaskMemFree(_rgFieldStrings[TFI_OTP]);
        _rgFieldStrings[TFI_OTP] = nullptr;
    }
    HRESULT hr = SHStrDupW(pwz ? pwz : L"", &_rgFieldStrings[TFI_OTP]);
    if (SUCCEEDED(hr))
        _otp = pwz ? pwz : L"";
    return hr;
}

IFACEMETHODIMP TotpCredential::SetCheckboxValue(DWORD, BOOL)
{
    return E_NOTIMPL;
}

IFACEMETHODIMP TotpCredential::SetComboBoxSelectedValue(DWORD, DWORD)
{
    return E_NOTIMPL;
}

IFACEMETHODIMP TotpCredential::CommandLinkClicked(DWORD)
{
    return E_NOTIMPL;
}

IFACEMETHODIMP TotpCredential::GetUserSid(PWSTR* ppszSid)
{
    if (_userSid.empty())
    {
        *ppszSid = nullptr;
        return S_FALSE;
    }
    return SHStrDupW(_userSid.c_str(), ppszSid);
}

IFACEMETHODIMP TotpCredential::GetSerialization(
    CREDENTIAL_PROVIDER_GET_SERIALIZATION_RESPONSE* pcpgsr,
    CREDENTIAL_PROVIDER_CREDENTIAL_SERIALIZATION* pcpcs,
    PWSTR* ppwszOptionalStatusText,
    CREDENTIAL_PROVIDER_STATUS_ICON* pcpsiOptionalStatusIcon)
{
    *pcpgsr = CPGSR_NO_CREDENTIAL_NOT_FINISHED;
    *ppwszOptionalStatusText = nullptr;
    *pcpsiOptionalStatusIcon = CPSI_NONE;
    ZeroMemory(pcpcs, sizeof(*pcpcs));

    if (!TotpVerifyCode(_cfg.totpSecret, _otp, _cfg.totpStepSeconds))
    {
        CpLog(L"TOTP verify FAILED (check VM clock vs phone, and TotpSecret).");
        *pcpgsr = CPGSR_NO_CREDENTIAL_FINISHED;
        // Distinct text so user knows this is WinTotp, not the system Password tile.
        SHStrDupW(L"WinTotp: authenticator code invalid or clock skew. Sync VM time.", ppwszOptionalStatusText);
        *pcpsiOptionalStatusIcon = CPSI_ERROR;
        return S_OK;
    }

    CpLog(L"TOTP verify OK — packing local logon.");

    // "." = local account domain (more reliable than NetBIOS name on some VMs).
    wchar_t domain[] = L".";

    PWSTR protectedPassword = nullptr;
    HRESULT hr = ProtectIfNecessaryAndCopyPassword(_cfg.staticPassword.c_str(), _cpus, &protectedPassword);
    if (FAILED(hr))
    {
        CpLog(L"ProtectPassword failed.");
        return hr;
    }

    KERB_INTERACTIVE_UNLOCK_LOGON kiul;
    hr = KerbInteractiveUnlockLogonInit(
        domain,
        (PWSTR)_cfg.targetUser.c_str(),
        protectedPassword,
        _cpus,
        &kiul);

    if (SUCCEEDED(hr))
    {
        hr = KerbInteractiveUnlockLogonPack(kiul, &pcpcs->rgbSerialization, &pcpcs->cbSerialization);
    }

    if (SUCCEEDED(hr))
    {
        ULONG ulAuthPackage = 0;
        hr = RetrieveNegotiateAuthPackage(&ulAuthPackage);
        if (SUCCEEDED(hr))
        {
            pcpcs->ulAuthenticationPackage = ulAuthPackage;
            pcpcs->clsidCredentialProvider = CLSID_WinTotpProvider;
            *pcpgsr = CPGSR_RETURN_CREDENTIAL_FINISHED;
            CpLog(L"Serialization ready.");
        }
        else
        {
            CpLog(L"RetrieveNegotiateAuthPackage failed.");
        }
    }
    else
    {
        CpLog(L"Kerb pack/init failed.");
    }

    if (protectedPassword)
    {
        SecureZeroMemory(protectedPassword, wcslen(protectedPassword) * sizeof(wchar_t));
        CoTaskMemFree(protectedPassword);
    }
    return hr;
}

IFACEMETHODIMP TotpCredential::ReportResult(
    NTSTATUS ntsStatus,
    NTSTATUS ntsSubstatus,
    PWSTR* ppwszOptionalStatusText,
    CREDENTIAL_PROVIDER_STATUS_ICON* pcpsiOptionalStatusIcon)
{
    *ppwszOptionalStatusText = nullptr;
    *pcpsiOptionalStatusIcon = CPSI_NONE;

    wchar_t msg[160];
    StringCchPrintfW(msg, 160, L"ReportResult status=0x%08X sub=0x%08X", (unsigned)ntsStatus, (unsigned)ntsSubstatus);
    CpLog(msg);

    if (!NT_SUCCESS(ntsStatus))
    {
        // STATUS_LOGON_FAILURE etc. — usually hidden static password mismatch.
        SHStrDupW(L"WinTotp: Windows rejected the hidden password. Re-run Install/Enable login component.", ppwszOptionalStatusText);
        *pcpsiOptionalStatusIcon = CPSI_ERROR;
    }
    return S_OK;
}
