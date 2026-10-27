#include "totp.h"
#include <windows.h>
#include <bcrypt.h>
#include <vector>
#include <ctime>
#include <cstring>

namespace
{
    int Base32Value(char c)
    {
        if (c >= 'A' && c <= 'Z') return c - 'A';
        if (c >= 'a' && c <= 'z') return c - 'a';
        if (c >= '2' && c <= '7') return c - '2' + 26;
        return -1;
    }

    bool Base32Decode(const std::string& in, std::vector<BYTE>& out)
    {
        out.clear();
        int buffer = 0;
        int bitsLeft = 0;
        for (char ch : in)
        {
            if (ch == ' ' || ch == '=' || ch == '-') continue;
            int v = Base32Value(ch);
            if (v < 0) return false;
            buffer = (buffer << 5) | v;
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                bitsLeft -= 8;
                out.push_back(static_cast<BYTE>((buffer >> bitsLeft) & 0xFF));
            }
        }
        return !out.empty();
    }

    bool HmacSha1(const BYTE* key, ULONG keyLen, const BYTE* data, ULONG dataLen, BYTE out[20])
    {
        BCRYPT_ALG_HANDLE alg = nullptr;
        BCRYPT_HASH_HANDLE hash = nullptr;
        DWORD objLen = 0, dataOut = 0;
        PBYTE obj = nullptr;
        NTSTATUS st = BCryptOpenAlgorithmProvider(&alg, BCRYPT_SHA1_ALGORITHM, nullptr, BCRYPT_ALG_HANDLE_HMAC_FLAG);
        if (st < 0) return false;
        st = BCryptGetProperty(alg, BCRYPT_OBJECT_LENGTH, (PUCHAR)&objLen, sizeof(objLen), &dataOut, 0);
        if (st < 0) { BCryptCloseAlgorithmProvider(alg, 0); return false; }
        obj = (PBYTE)HeapAlloc(GetProcessHeap(), 0, objLen);
        if (!obj) { BCryptCloseAlgorithmProvider(alg, 0); return false; }
        st = BCryptCreateHash(alg, &hash, obj, objLen, (PUCHAR)key, keyLen, 0);
        if (st < 0) { HeapFree(GetProcessHeap(), 0, obj); BCryptCloseAlgorithmProvider(alg, 0); return false; }
        st = BCryptHashData(hash, (PUCHAR)data, dataLen, 0);
        if (st >= 0)
            st = BCryptFinishHash(hash, out, 20, 0);
        BCryptDestroyHash(hash);
        HeapFree(GetProcessHeap(), 0, obj);
        BCryptCloseAlgorithmProvider(alg, 0);
        return st >= 0;
    }

    std::string ComputeTotp(const std::vector<BYTE>& key, long long counter, int digits)
    {
        BYTE msg[8];
        for (int i = 7; i >= 0; --i)
        {
            msg[i] = static_cast<BYTE>(counter & 0xFF);
            counter >>= 8;
        }
        BYTE mac[20];
        if (!HmacSha1(key.data(), (ULONG)key.size(), msg, 8, mac))
            return {};
        int offset = mac[19] & 0x0F;
        int bin =
            ((mac[offset] & 0x7F) << 24) |
            ((mac[offset + 1] & 0xFF) << 16) |
            ((mac[offset + 2] & 0xFF) << 8) |
            (mac[offset + 3] & 0xFF);
        int mod = 1;
        for (int i = 0; i < digits; ++i) mod *= 10;
        int otp = bin % mod;
        char buf[16];
        sprintf_s(buf, "%0*d", digits, otp);
        return buf;
    }

    std::string NarrowCode(const std::wstring& code)
    {
        std::string s;
        for (wchar_t c : code)
        {
            if (c >= L'0' && c <= L'9')
                s.push_back(static_cast<char>(c));
        }
        return s;
    }
}

bool TotpVerifyCode(const std::string& secretBase32, const std::wstring& code, int stepSeconds)
{
    if (stepSeconds <= 0) stepSeconds = 30;
    auto normalized = NarrowCode(code);
    if (normalized.size() < 6 || normalized.size() > 8)
        return false;

    std::vector<BYTE> key;
    if (!Base32Decode(secretBase32, key))
        return false;

    const long long now = static_cast<long long>(_time64(nullptr));
    const long long timestep = now / stepSeconds;

    // ±2 windows (~±60s) — VMs often have larger clock drift than phones.
    for (long long delta = -2; delta <= 2; ++delta)
    {
        auto expect = ComputeTotp(key, timestep + delta, 6);
        if (!expect.empty() && expect == normalized.substr(0, 6))
            return true;
        if (normalized.size() > 6)
        {
            // allow longer numeric paste; still compare first 6 of expected against full? already 6-digit expect
        }
    }
    return false;
}
