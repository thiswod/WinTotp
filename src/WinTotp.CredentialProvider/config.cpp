#include "config.h"
#include "common.h"
#include <wincrypt.h>
#include <fstream>
#include <vector>
#include <algorithm>

namespace
{
    std::wstring ProgramDataConfigPath()
    {
        wchar_t path[MAX_PATH] = {};
        if (!GetEnvironmentVariableW(L"PROGRAMDATA", path, MAX_PATH))
            StringCchCopyW(path, MAX_PATH, L"C:\\ProgramData");
        std::wstring full = path;
        full += L"\\WinTotp\\cp-config.dpapi";
        return full;
    }

    bool ReadFileBytes(const std::wstring& path, std::vector<BYTE>& bytes)
    {
        HANDLE h = CreateFileW(path.c_str(), GENERIC_READ, FILE_SHARE_READ, nullptr, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, nullptr);
        if (h == INVALID_HANDLE_VALUE) return false;
        LARGE_INTEGER size{};
        if (!GetFileSizeEx(h, &size) || size.QuadPart <= 0 || size.QuadPart > 1024 * 1024)
        {
            CloseHandle(h);
            return false;
        }
        bytes.resize(static_cast<size_t>(size.QuadPart));
        DWORD read = 0;
        BOOL ok = ReadFile(h, bytes.data(), (DWORD)bytes.size(), &read, nullptr);
        CloseHandle(h);
        return ok && read == bytes.size();
    }

    // Minimal JSON string extractor: "key" : "value"
    bool JsonGetString(const std::string& json, const char* key, std::string& value)
    {
        std::string pattern = std::string("\"") + key + "\"";
        auto pos = json.find(pattern);
        if (pos == std::string::npos) return false;
        pos = json.find(':', pos + pattern.size());
        if (pos == std::string::npos) return false;
        pos = json.find('"', pos + 1);
        if (pos == std::string::npos) return false;
        auto end = pos + 1;
        std::string out;
        for (; end < json.size(); ++end)
        {
            char c = json[end];
            if (c == '\\' && end + 1 < json.size())
            {
                out.push_back(json[++end]);
                continue;
            }
            if (c == '"') break;
            out.push_back(c);
        }
        value = out;
        return true;
    }

    bool JsonGetInt(const std::string& json, const char* key, int& value)
    {
        std::string pattern = std::string("\"") + key + "\"";
        auto pos = json.find(pattern);
        if (pos == std::string::npos) return false;
        pos = json.find(':', pos + pattern.size());
        if (pos == std::string::npos) return false;
        while (pos < json.size() && (json[pos] == ':' || json[pos] == ' ' || json[pos] == '\t'))
            ++pos;
        value = atoi(json.c_str() + pos);
        return true;
    }

    std::wstring Utf8ToWide(const std::string& s)
    {
        if (s.empty()) return {};
        int n = MultiByteToWideChar(CP_UTF8, 0, s.data(), (int)s.size(), nullptr, 0);
        std::wstring w(n, L'\0');
        MultiByteToWideChar(CP_UTF8, 0, s.data(), (int)s.size(), w.data(), n);
        return w;
    }
}

bool LoadTotpCpConfig(TotpCpConfig& out)
{
    ClearTotpCpConfig(out);
    std::vector<BYTE> enc;
    if (!ReadFileBytes(ProgramDataConfigPath(), enc))
        return false;

    DATA_BLOB in{ (DWORD)enc.size(), enc.data() };
    DATA_BLOB plain{};
    if (!CryptUnprotectData(&in, nullptr, nullptr, nullptr, nullptr, 0, &plain))
        return false;

    std::string json(reinterpret_cast<char*>(plain.pbData), plain.cbData);
    SecureZeroMemory(plain.pbData, plain.cbData);
    LocalFree(plain.pbData);

    std::string user, secret, password;
    int step = 30;
    if (!JsonGetString(json, "TargetUser", user) ||
        !JsonGetString(json, "TotpSecret", secret) ||
        !JsonGetString(json, "StaticPassword", password))
    {
        SecureZeroMemory(json.data(), json.size());
        return false;
    }
    JsonGetInt(json, "TotpStepSeconds", step);
    SecureZeroMemory(json.data(), json.size());

    out.targetUser = Utf8ToWide(user);
    out.totpSecret = secret;
    out.totpStepSeconds = step > 0 ? step : 30;
    out.staticPassword = Utf8ToWide(password); // may be empty (blank Windows password accounts)
    SecureZeroMemory(password.data(), password.size());
    return !out.targetUser.empty() && !out.totpSecret.empty();
}

void ClearTotpCpConfig(TotpCpConfig& cfg)
{
    SecureClearWString(cfg.targetUser);
    SecureClearWString(cfg.staticPassword);
    if (!cfg.totpSecret.empty())
    {
        SecureZeroMemory(cfg.totpSecret.data(), cfg.totpSecret.size());
        cfg.totpSecret.clear();
    }
    cfg.totpStepSeconds = 30;
}
