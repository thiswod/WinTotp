#pragma once
#include <string>

// RFC 6238 TOTP (HMAC-SHA1, 6 digits). secretBase32 is standard Authenticator Base32.
bool TotpVerifyCode(const std::string& secretBase32, const std::wstring& code, int stepSeconds = 30);
