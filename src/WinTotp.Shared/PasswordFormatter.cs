namespace WinTotp.Shared;

/// <summary>
/// Maps a 6-digit TOTP into a Windows login password.
/// Prefix/suffix are optional (empty = password is just the 6 digits).
/// Default: Tp#{code}!Aa
/// </summary>
public static class PasswordFormatter
{
    public const string DefaultPrefix = "Tp#";
    public const string DefaultSuffix = "!Aa";

    public static string FromTotp(string totpCode, string prefix = DefaultPrefix, string suffix = DefaultSuffix)
    {
        if (string.IsNullOrWhiteSpace(totpCode) || totpCode.Length != 6 || !totpCode.All(char.IsDigit))
            throw new ArgumentException("TOTP code must be 6 digits.", nameof(totpCode));

        prefix ??= "";
        suffix ??= "";
        return prefix + totpCode + suffix;
    }

    public static string Describe(string? prefix = null, string? suffix = null)
    {
        prefix ??= DefaultPrefix;
        suffix ??= DefaultSuffix;
        if (prefix.Length == 0 && suffix.Length == 0)
            return "仅 6 位验证器数字";
        if (prefix.Length == 0)
            return "6位验证码 + " + suffix;
        if (suffix.Length == 0)
            return prefix + " + 6位验证码";
        return prefix + " + 6位验证码 + " + suffix;
    }

    public static bool TryExtractTotp(
        string password,
        out string? totpCode,
        string prefix = DefaultPrefix,
        string suffix = DefaultSuffix)
    {
        totpCode = null;
        if (string.IsNullOrEmpty(password))
            return false;

        prefix ??= "";
        suffix ??= "";

        if (prefix.Length > 0 && !password.StartsWith(prefix, StringComparison.Ordinal))
            return false;
        if (suffix.Length > 0 && !password.EndsWith(suffix, StringComparison.Ordinal))
            return false;

        var midLen = password.Length - prefix.Length - suffix.Length;
        if (midLen != 6)
            return false;

        var mid = password.Substring(prefix.Length, 6);
        if (!mid.All(char.IsDigit))
            return false;

        totpCode = mid;
        return true;
    }
}
