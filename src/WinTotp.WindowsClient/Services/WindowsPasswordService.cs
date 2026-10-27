using System.DirectoryServices.AccountManagement;
using System.Runtime.Versioning;
using Microsoft.Extensions.Options;
using WinTotp.WindowsClient.Options;

namespace WinTotp.WindowsClient.Services;

public enum PasswordApplyStatus
{
    AlreadyApplied,
    Applied,
    Failed,
    Blocked,
    DryRun
}

public readonly record struct PasswordApplyResult(PasswordApplyStatus Status, string? Error);

[SupportedOSPlatform("windows")]
public sealed class WindowsPasswordService
{
    private readonly IOptionsMonitor<WinTotpOptions> _options;
    private readonly ILogger<WindowsPasswordService> _logger;

    public WindowsPasswordService(IOptionsMonitor<WinTotpOptions> options, ILogger<WindowsPasswordService> logger)
    {
        _options = options;
        _logger = logger;
    }

    private WinTotpOptions Opt => _options.CurrentValue;

    public string ResolveTargetUser()
    {
        var o = Opt;
        if (!string.IsNullOrWhiteSpace(o.TargetUser))
            return o.TargetUser.Trim();

        // Default: account running this process (usually the interactive user).
        return Environment.UserName;
    }

    public string ResolveTargetUserSafe()
    {
        try { return ResolveTargetUser(); }
        catch
        {
            var t = Opt.TargetUser;
            return string.IsNullOrWhiteSpace(t) ? "(unset)" : t.Trim();
        }
    }

    public void LogSafetyStatus()
    {
        var user = ResolveTargetUserSafe();
        if (CredentialProviderInstaller.IsRegistered() && CpConfigStore.Exists())
        {
            _logger.LogInformation(
                "CP mode — password rotation disabled. TargetUser={User}. Login uses Authenticator 6-digit code.",
                user);
            AgentStatusFile.Write(dryRun: false, lastError: null, targetUser: user);
            return;
        }

        _logger.LogWarning(
            "Credential Provider not enabled. TargetUser={User}. Install CP from the GUI to use TOTP login without rotating Windows password.",
            user);
        AgentStatusFile.Write(
            dryRun: true,
            lastError: "未启用 Credential Provider（不会轮询改密）",
            targetUser: user);
    }

    /// <summary>
    /// One-time SetPassword used when enabling Credential Provider (hidden static password).
    /// </summary>
    public PasswordApplyResult SetPasswordOnce(string user, string password)
        => SetPasswordOnceStatic(user, password, _logger);

    public static PasswordApplyResult SetPasswordOnceStatic(
        string user,
        string password,
        ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
            return new PasswordApplyResult(PasswordApplyStatus.Failed, "user/password empty");

        try
        {
            using var ctx = new PrincipalContext(ContextType.Machine);
            using var principal = UserPrincipal.FindByIdentity(ctx, IdentityType.SamAccountName, user.Trim())
                ?? throw new InvalidOperationException($"Local user '{user}' not found.");

            if (principal.Enabled == false)
                throw new InvalidOperationException($"Local user '{user}' is disabled.");

            principal.SetPassword(password);
            principal.Save();
            logger?.LogWarning("One-time static password set for {User} (Credential Provider mode).", user);
            return new PasswordApplyResult(PasswordApplyStatus.Applied, null);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "FAILED one-time SetPassword for {User}", user);
            return new PasswordApplyResult(PasswordApplyStatus.Failed, ex.Message);
        }
    }
}
