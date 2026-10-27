#include "diag.h"
#include <strsafe.h>

void CpLog(PCWSTR message)
{
    wchar_t path[MAX_PATH] = {};
    if (!GetEnvironmentVariableW(L"PROGRAMDATA", path, MAX_PATH))
        StringCchCopyW(path, MAX_PATH, L"C:\\ProgramData");
    StringCchCatW(path, MAX_PATH, L"\\WinTotp\\cp-debug.log");

    CreateDirectoryW(L"C:\\ProgramData\\WinTotp", nullptr);

    HANDLE h = CreateFileW(path, FILE_APPEND_DATA, FILE_SHARE_READ, nullptr, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr);
    if (h == INVALID_HANDLE_VALUE)
        return;

    SYSTEMTIME st{};
    GetLocalTime(&st);
    wchar_t line[512];
    StringCchPrintfW(line, 512, L"%04u-%02u-%02u %02u:%02u:%02u  %s\r\n",
        st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond,
        message ? message : L"(null)");

    DWORD bytes = 0;
    WriteFile(h, line, (DWORD)(wcslen(line) * sizeof(wchar_t)), &bytes, nullptr);
    CloseHandle(h);
}
