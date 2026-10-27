namespace WinTotp.Shared;

// Shared DTOs for local TOTP snapshots (no remote hub / shutdown protocol).

public sealed class TotpStatusDto
{
    public string Code { get; set; } = "";
    public string Password { get; set; } = "";
    public int SecondsRemaining { get; set; }
    public int StepSeconds { get; set; } = 30;
    public DateTimeOffset ServerUtc { get; set; }
    public long PeriodId { get; set; }
}

/// <summary>Local TOTP window snapshot used by the client preview UI.</summary>
public sealed class PasswordAuthorityDto
{
    public string Password { get; set; } = "";
    public string Code { get; set; } = "";
    public string PreviousPassword { get; set; } = "";
    public int SecondsRemaining { get; set; }
    public int StepSeconds { get; set; } = 30;
    public long PeriodId { get; set; }
    public DateTimeOffset ServerUtc { get; set; }
}
