namespace WinTotp.WindowsClient.Options;

public sealed class WinTotpOptions
{
    public const string SectionName = "WinTotp";

    /// <summary>
    /// Local Authenticator secret (Base32). Credential Provider verifies codes from this.
    /// </summary>
    public string TotpSecret { get; set; } = "";

    public int TotpStepSeconds { get; set; } = 30;

    /// <summary>
    /// Local Windows account bound to Credential Provider.
    /// Empty = current interactive user (Environment.UserName) at save/enable time.
    /// </summary>
    public string TargetUser { get; set; } = "";
}
