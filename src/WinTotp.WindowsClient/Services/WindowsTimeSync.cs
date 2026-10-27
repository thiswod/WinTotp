using System.Diagnostics;
using System.ServiceProcess;
using Microsoft.Win32;

namespace WinTotp.WindowsClient.Services;

/// <summary>
/// Ensures Windows clock auto-sync is on. TOTP codes diverge from phone Authenticator
/// when the PC clock drifts more than ~30 seconds.
/// </summary>
public static class WindowsTimeSync
{
    private const string ServiceName = "W32Time";
    private const string ParametersKey = @"SYSTEM\CurrentControlSet\Services\W32Time\Parameters";

    public static bool IsAutomaticSyncReady()
    {
        try
        {
            return IsSyncType(ReadType()) && IsServiceRunning();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Turns on automatic time sync, starts W32Time if needed, and forces a resync.
    /// Requires elevation (same as service install).
    /// </summary>
    public static string EnsureAutomaticSync()
    {
        var notes = new List<string>();

        EnsureServiceAutoStartAndRunning(notes);

        using (var key = Registry.LocalMachine.OpenSubKey(ParametersKey, writable: true)
               ?? throw new InvalidOperationException("无法打开时间同步注册表（需要管理员权限）。"))
        {
            var type = key.GetValue("Type") as string;
            if (!IsSyncType(type))
            {
                key.SetValue("Type", "NTP", RegistryValueKind.String);
                notes.Add("已开启自动同步时间");

                var ntpServer = key.GetValue("NtpServer") as string;
                if (string.IsNullOrWhiteSpace(ntpServer))
                {
                    key.SetValue("NtpServer", "time.windows.com,0x9", RegistryValueKind.String);
                    notes.Add("已设置 NTP 为 time.windows.com");
                }
            }
        }

        try
        {
            RunW32tm("/config /update");
        }
        catch (Exception ex)
        {
            notes.Add("应用时间配置失败: " + ex.Message);
        }

        try
        {
            RunW32tm("/resync /force");
            notes.Add("已立即校时");
        }
        catch (Exception ex)
        {
            // Offline / firewall: sync flag is still on; warn but don't fail install.
            notes.Add("立即校时未成功（可稍后在「日期和时间」里同步）: " + Truncate(ex.Message, 80));
        }

        if (notes.Count == 0)
            return "Windows 时间已是自动同步，时间服务正常。";

        return string.Join("；", notes) + "。";
    }

    private static string? ReadType()
    {
        using var key = Registry.LocalMachine.OpenSubKey(ParametersKey);
        return key?.GetValue("Type") as string;
    }

    private static bool IsSyncType(string? type)
        => type is "NTP" or "NT5DS" or "AllSync";

    private static bool IsServiceRunning()
    {
        try
        {
            using var sc = new ServiceController(ServiceName);
            sc.Refresh();
            return sc.Status is ServiceControllerStatus.Running or ServiceControllerStatus.StartPending;
        }
        catch
        {
            return false;
        }
    }

    private static void EnsureServiceAutoStartAndRunning(List<string> notes)
    {
        using (var sc = new ServiceController(ServiceName))
        {
            sc.Refresh();
            // Automatic (incl. delayed-auto reported as Automatic on some builds) is fine;
            // only force when Manual / Disabled.
            if (sc.StartType is ServiceStartMode.Manual or ServiceStartMode.Disabled)
            {
                RunSc("config", $"{ServiceName} start= auto");
                notes.Add("已将 Windows Time 设为自动启动");
            }
        }

        using (var sc = new ServiceController(ServiceName))
        {
            sc.Refresh();
            if (sc.Status is ServiceControllerStatus.Running or ServiceControllerStatus.StartPending)
                return;

            sc.Start();
            sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(20));
            notes.Add("已启动 Windows Time 服务");
        }
    }

    private static void RunW32tm(string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.SystemDirectory, "w32tm.exe"),
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var p = Process.Start(psi) ?? throw new InvalidOperationException("无法启动 w32tm.exe");
        var stdout = p.StandardOutput.ReadToEnd();
        var stderr = p.StandardError.ReadToEnd();
        p.WaitForExit(20000);

        if (p.ExitCode != 0)
            throw new InvalidOperationException($"w32tm {args} 失败: {(stdout + " " + stderr).Trim()}");
    }

    private static void RunSc(string command, string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.SystemDirectory, "sc.exe"),
            Arguments = $"{command} {args}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var p = Process.Start(psi) ?? throw new InvalidOperationException("无法启动 sc.exe");
        var stdout = p.StandardOutput.ReadToEnd();
        var stderr = p.StandardError.ReadToEnd();
        p.WaitForExit(15000);

        if (p.ExitCode != 0)
            throw new InvalidOperationException($"sc {command} 失败: {(stdout + " " + stderr).Trim()}");
    }

    private static string Truncate(string text, int max)
        => text.Length <= max ? text : text[..max] + "…";
}
