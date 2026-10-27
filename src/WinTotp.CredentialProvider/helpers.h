#pragma once
#include "common.h"

HRESULT FieldDescriptorCoAllocCopy(
    const CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR& rcpfd,
    CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR** ppcpfd);

HRESULT UnicodeStringInitWithString(PWSTR pwz, UNICODE_STRING* pus);

HRESULT KerbInteractiveUnlockLogonInit(
    PWSTR domain,
    PWSTR username,
    PWSTR password,
    CREDENTIAL_PROVIDER_USAGE_SCENARIO cpus,
    KERB_INTERACTIVE_UNLOCK_LOGON* pkiul);

HRESULT KerbInteractiveUnlockLogonPack(
    const KERB_INTERACTIVE_UNLOCK_LOGON& rkiulIn,
    BYTE** prgb,
    DWORD* pcb);

HRESULT ProtectIfNecessaryAndCopyPassword(
    PCWSTR pwzPassword,
    CREDENTIAL_PROVIDER_USAGE_SCENARIO cpus,
    PWSTR* ppwzProtectedPassword);

HRESULT RetrieveNegotiateAuthPackage(ULONG* pulAuthPackage);
