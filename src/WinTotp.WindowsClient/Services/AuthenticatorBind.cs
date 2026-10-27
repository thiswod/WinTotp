using System.Drawing.Imaging;
using QRCoder;
using WinTotp.Shared;

namespace WinTotp.WindowsClient.Services;

/// <summary>Helpers for binding Google Authenticator / iOS Passwords to the local TotpSecret.</summary>
public static class AuthenticatorBind
{
    public static string NormalizeSecret(string? secret)
        => (secret ?? "").Replace(" ", "").Trim().ToUpperInvariant();

    public static string EnsureSecret(string? existing)
    {
        var s = NormalizeSecret(existing);
        return string.IsNullOrWhiteSpace(s) ? TotpService.GenerateBase32Secret() : s;
    }

    public static string BuildOtpAuthUrl(string secret, string accountLabel)
    {
        secret = NormalizeSecret(secret);
        var label = Uri.EscapeDataString("WinTotp:" + accountLabel);
        var issuer = Uri.EscapeDataString("WinTotp");
        return $"otpauth://totp/{label}?secret={secret}&issuer={issuer}&algorithm=SHA1&digits=6&period=30";
    }

    public static Bitmap CreateQrBitmap(string otpauthUrl, int pixelsPerModule = 4)
    {
        var generator = new QRCodeGenerator();
        var data = generator.CreateQrCode(otpauthUrl, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data);
        var bytes = png.GetGraphic(pixelsPerModule);
        using var ms = new MemoryStream(bytes);
        return new Bitmap(ms);
    }

    public static string FormatLoginHint(string code, string? prefix = null, string? suffix = null)
        => PasswordFormatter.FromTotp(
            code,
            prefix ?? PasswordFormatter.DefaultPrefix,
            suffix ?? PasswordFormatter.DefaultSuffix);
}
