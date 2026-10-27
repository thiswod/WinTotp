using OtpNet;

namespace WinTotp.Shared;

/// <summary>
/// RFC 6238 TOTP helper for the Windows client / preview UI.
/// </summary>
public sealed class TotpService
{
    private readonly Totp _totp;
    private readonly int _stepSeconds;
    private readonly string _passwordPrefix;
    private readonly string _passwordSuffix;

    public TotpService(
        string base32Secret,
        int stepSeconds = 30,
        int digits = 6,
        string? passwordPrefix = null,
        string? passwordSuffix = null)
    {
        if (string.IsNullOrWhiteSpace(base32Secret))
            throw new ArgumentException("TOTP secret is required.", nameof(base32Secret));

        _stepSeconds = stepSeconds;
        // null = built-in default; empty string = no affix (caller opts out).
        _passwordPrefix = passwordPrefix ?? PasswordFormatter.DefaultPrefix;
        _passwordSuffix = passwordSuffix ?? PasswordFormatter.DefaultSuffix;
        var key = Base32Encoding.ToBytes(base32Secret.Replace(" ", "").ToUpperInvariant());
        _totp = new Totp(key, step: stepSeconds, totpSize: digits);
    }

    public string PasswordPrefix => _passwordPrefix;
    public string PasswordSuffix => _passwordSuffix;

    public string GetCode(DateTime? utc = null)
        => _totp.ComputeTotp(utc ?? DateTime.UtcNow);

    /// <summary>Verify a 6-digit code with ±1 step clock skew tolerance.</summary>
    public bool VerifyCode(string? code, DateTime? utc = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        var normalized = code.Trim().Replace(" ", "", StringComparison.Ordinal);
        if (normalized.Length is < 6 or > 8)
            return false;

        return _totp.VerifyTotp(
            utc ?? DateTime.UtcNow,
            normalized,
            out _,
            new VerificationWindow(previous: 1, future: 1));
    }

    public string GetPassword(DateTime? utc = null)
        => PasswordFormatter.FromTotp(GetCode(utc), _passwordPrefix, _passwordSuffix);

    public string GetPasswordForPeriodOffset(int periodOffset, DateTime? utc = null)
    {
        var now = utc ?? DateTime.UtcNow;
        return GetPassword(now.AddSeconds(periodOffset * _stepSeconds));
    }

    public long GetPeriodId(DateTime? utc = null)
    {
        var unix = new DateTimeOffset(utc ?? DateTime.UtcNow).ToUnixTimeSeconds();
        return unix / _stepSeconds;
    }

    public int SecondsRemaining(DateTime? utc = null)
    {
        var now = utc ?? DateTime.UtcNow;
        var unix = new DateTimeOffset(now).ToUnixTimeSeconds();
        var remaining = _stepSeconds - (int)(unix % _stepSeconds);
        return remaining == 0 ? _stepSeconds : remaining;
    }

    public DateTime PeriodEndUtc(DateTime? utc = null)
    {
        var now = utc ?? DateTime.UtcNow;
        return now.AddSeconds(SecondsRemaining(now));
    }

    public PasswordAuthorityDto CreateAuthoritySnapshot(DateTime? utc = null)
    {
        var now = utc ?? DateTime.UtcNow;
        return new PasswordAuthorityDto
        {
            Code = GetCode(now),
            Password = GetPassword(now),
            PreviousPassword = GetPasswordForPeriodOffset(-1, now),
            SecondsRemaining = SecondsRemaining(now),
            StepSeconds = _stepSeconds,
            PeriodId = GetPeriodId(now),
            ServerUtc = new DateTimeOffset(DateTime.SpecifyKind(now, DateTimeKind.Utc))
        };
    }

    public static string GenerateBase32Secret(int bytes = 20)
    {
        var key = KeyGeneration.GenerateRandomKey(bytes);
        return Base32Encoding.ToString(key);
    }
}
