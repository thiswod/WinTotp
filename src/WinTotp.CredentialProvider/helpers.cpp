#include "helpers.h"
#include <Shlwapi.h>

HRESULT FieldDescriptorCoAllocCopy(
    const CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR& rcpfd,
    CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR** ppcpfd)
{
    HRESULT hr;
    CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR* pcpfd =
        (CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR*)CoTaskMemAlloc(sizeof(*pcpfd));
    if (pcpfd)
    {
        pcpfd->dwFieldID = rcpfd.dwFieldID;
        pcpfd->cpft = rcpfd.cpft;
        pcpfd->guidFieldType = rcpfd.guidFieldType;
        if (rcpfd.pszLabel)
        {
            hr = SHStrDupW(rcpfd.pszLabel, &pcpfd->pszLabel);
        }
        else
        {
            pcpfd->pszLabel = nullptr;
            hr = S_OK;
        }
        if (SUCCEEDED(hr))
            *ppcpfd = pcpfd;
        else
            CoTaskMemFree(pcpfd);
    }
    else
    {
        hr = E_OUTOFMEMORY;
    }
    return hr;
}

HRESULT UnicodeStringInitWithString(PWSTR pwz, UNICODE_STRING* pus)
{
    HRESULT hr;
    if (pwz)
    {
        size_t len = 0;
        hr = StringCchLengthW(pwz, 65536, &len);
        if (SUCCEEDED(hr))
        {
            USHORT us = (USHORT)(len * sizeof(wchar_t));
            pus->Length = us;
            pus->MaximumLength = us + sizeof(wchar_t);
            pus->Buffer = pwz;
            hr = S_OK;
        }
    }
    else
    {
        hr = E_INVALIDARG;
    }
    return hr;
}

HRESULT KerbInteractiveUnlockLogonInit(
    PWSTR domain,
    PWSTR username,
    PWSTR password,
    CREDENTIAL_PROVIDER_USAGE_SCENARIO cpus,
    KERB_INTERACTIVE_UNLOCK_LOGON* pkiul)
{
    KERB_INTERACTIVE_UNLOCK_LOGON kiul{};
    KERB_INTERACTIVE_LOGON* pkil = &kiul.Logon;

    pkil->MessageType = (cpus == CPUS_UNLOCK_WORKSTATION)
        ? KerbWorkstationUnlockLogon
        : KerbInteractiveLogon;

    HRESULT hr = UnicodeStringInitWithString(domain, &pkil->LogonDomainName);
    if (SUCCEEDED(hr))
        hr = UnicodeStringInitWithString(username, &pkil->UserName);
    if (SUCCEEDED(hr))
        hr = UnicodeStringInitWithString(password, &pkil->Password);
    if (SUCCEEDED(hr))
        *pkiul = kiul;
    return hr;
}

HRESULT KerbInteractiveUnlockLogonPack(
    const KERB_INTERACTIVE_UNLOCK_LOGON& rkiulIn,
    BYTE** prgb,
    DWORD* pcb)
{
    const KERB_INTERACTIVE_LOGON* pkilIn = &rkiulIn.Logon;

    const DWORD cb = (DWORD)(
        sizeof(rkiulIn) +
        pkilIn->LogonDomainName.Length +
        pkilIn->UserName.Length +
        pkilIn->Password.Length);

    KERB_INTERACTIVE_UNLOCK_LOGON* pkiulOut =
        (KERB_INTERACTIVE_UNLOCK_LOGON*)CoTaskMemAlloc(cb);
    if (!pkiulOut)
        return E_OUTOFMEMORY;

    ZeroMemory(pkiulOut, cb);
    KERB_INTERACTIVE_LOGON* pkilOut = &pkiulOut->Logon;
    pkilOut->MessageType = pkilIn->MessageType;

    BYTE* pbBuffer = (BYTE*)(pkilOut + 1);

    auto pack = [&](const UNICODE_STRING& src, UNICODE_STRING* dst) {
        dst->Length = src.Length;
        dst->MaximumLength = src.Length;
        dst->Buffer = (PWSTR)(pbBuffer - (BYTE*)pkiulOut);
        CopyMemory(pbBuffer, src.Buffer, src.Length);
        pbBuffer += src.Length;
    };

    pack(pkilIn->LogonDomainName, &pkilOut->LogonDomainName);
    pack(pkilIn->UserName, &pkilOut->UserName);
    pack(pkilIn->Password, &pkilOut->Password);

    *prgb = (BYTE*)pkiulOut;
    *pcb = cb;
    return S_OK;
}

HRESULT ProtectIfNecessaryAndCopyPassword(
    PCWSTR pwzPassword,
    CREDENTIAL_PROVIDER_USAGE_SCENARIO cpus,
    PWSTR* ppwzProtectedPassword)
{
    *ppwzProtectedPassword = nullptr;
    if (!pwzPassword)
        return E_INVALIDARG;

    // Blank local passwords: never CredProtect (empty buffer quirks); pack raw empty string.
    if (pwzPassword[0] == L'\0')
        return SHStrDupW(L"", ppwzProtectedPassword);

    // Local interactive logon: prefer CredProtect when it works; otherwise raw password.
    // Some VM / SYSTEM LogonUI paths reject CredProtect'd local passwords.
    if (cpus == CPUS_LOGON || cpus == CPUS_UNLOCK_WORKSTATION || cpus == CPUS_CREDUI)
    {
        BOOL alreadyProtected = FALSE;
        CRED_PROTECTION_TYPE protType = CredUnprotected;
        if (CredIsProtectedW((LPWSTR)pwzPassword, &protType) && protType != CredUnprotected)
            alreadyProtected = TRUE;

        if (!alreadyProtected)
        {
            WCHAR szProtected[CREDUI_MAX_PASSWORD_LENGTH + 1] = {};
            DWORD cch = CREDUI_MAX_PASSWORD_LENGTH;
            if (CredProtectW(FALSE, (LPWSTR)pwzPassword, (DWORD)wcslen(pwzPassword) + 1, szProtected, &cch, nullptr))
                return SHStrDupW(szProtected, ppwzProtectedPassword);
        }
    }
    return SHStrDupW(pwzPassword, ppwzProtectedPassword);
}

HRESULT RetrieveNegotiateAuthPackage(ULONG* pulAuthPackage)
{
    HANDLE hLsa = nullptr;
    NTSTATUS status = LsaConnectUntrusted(&hLsa);
    if (status != 0)
        return HRESULT_FROM_NT(status);

    LSA_STRING name{};
    name.Buffer = (PCHAR)"Negotiate";
    name.Length = (USHORT)strlen(name.Buffer);
    name.MaximumLength = name.Length + 1;

    status = LsaLookupAuthenticationPackage(hLsa, &name, pulAuthPackage);
    LsaDeregisterLogonProcess(hLsa);
    return HRESULT_FROM_NT(status);
}
