#pragma once

#include <windows.h>
#include <wincred.h>
#include <credentialprovider.h>
#include <ntsecapi.h>
#include <strsafe.h>
#include <string>
#include <cstring>

#ifndef NT_SUCCESS
#define NT_SUCCESS(Status) (((NTSTATUS)(Status)) >= 0)
#endif

#pragma comment(lib, "Shlwapi.lib")
#pragma comment(lib, "Secur32.lib")
#pragma comment(lib, "Credui.lib")
#pragma comment(lib, "bcrypt.lib")
#pragma comment(lib, "Crypt32.lib")
#pragma comment(lib, "Advapi32.lib")
#pragma comment(lib, "Ole32.lib")
#pragma comment(lib, "OleAut32.lib")

enum TOTP_FIELD_ID
{
    TFI_TILEIMAGE = 0,
    TFI_LABEL = 1,
    TFI_OTP = 2,
    TFI_SUBMIT = 3,
    TFI_NUM_FIELDS = 4,
};

inline void SecureClearWString(std::wstring& s)
{
    if (!s.empty())
    {
        SecureZeroMemory(s.data(), s.size() * sizeof(wchar_t));
        s.clear();
    }
}
