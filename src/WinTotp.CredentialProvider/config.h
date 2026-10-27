#pragma once
#include <string>

struct TotpCpConfig
{
    std::wstring targetUser;
    std::string totpSecret;      // UTF-8 / ASCII Base32
    int totpStepSeconds = 30;
    std::wstring staticPassword;
};

// Reads %ProgramData%\WinTotp\cp-config.dpapi (DPAPI LocalMachine blob wrapping UTF-8 JSON).
bool LoadTotpCpConfig(TotpCpConfig& out);
void ClearTotpCpConfig(TotpCpConfig& cfg);
