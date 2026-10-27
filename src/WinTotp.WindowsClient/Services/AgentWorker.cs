using Microsoft.Extensions.Options;
using WinTotp.WindowsClient.Options;

namespace WinTotp.WindowsClient.Services;

/// <summary>
/// Headless agent: keeps ProgramData CP config (TotpSecret / TargetUser) in sync with appsettings.
/// No remote shutdown / SignalR.
/// </summary>
public sealed class AgentWorker : BackgroundService
{
    private readonly IOptionsMonitor<WinTotpOptions> _options;
    private readonly WindowsPasswordService _passwordService;
    private readonly ILogger<AgentWorker> _logger;

    public AgentWorker(
        IOptionsMonitor<WinTotpOptions> options,
        WindowsPasswordService passwordService,
        ILogger<AgentWorker> logger)
    {
        _options = options;
        _passwordService = passwordService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = _options.CurrentValue;
        if (string.IsNullOrWhiteSpace(opts.TotpSecret))
            throw new InvalidOperationException("WinTotp:TotpSecret is required — 请在客户端绑定验证器并保存密钥。");

        SyncCpConfig(opts);
        _options.OnChange(SyncCpConfig);

        var targetUser = _passwordService.ResolveTargetUserSafe();
        _logger.LogInformation(
            "Agent start (CP sync only) user={User} cpRegistered={Cp}",
            targetUser, CredentialProviderInstaller.IsRegistered());
        _passwordService.LogSafetyStatus();

        // Idle until stop — OnChange handles secret updates while the service runs.
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // normal shutdown
        }
    }

    private void SyncCpConfig(WinTotpOptions opts)
    {
        try
        {
            if (!CpConfigStore.Exists())
                return;

            CredentialProviderInstaller.SyncSecretFromAppSettings(
                opts.TotpSecret,
                opts.TotpStepSeconds,
                string.IsNullOrWhiteSpace(opts.TargetUser) ? null : opts.TargetUser);
            _logger.LogInformation("Synced TotpSecret into ProgramData CP config.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to sync CP config from appsettings");
        }
    }
}
