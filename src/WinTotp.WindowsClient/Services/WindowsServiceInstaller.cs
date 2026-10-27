using System.Diagnostics;
using System.ServiceProcess;

namespace WinTotp.WindowsClient.Services;

/// <summary>
/// Installs the agent as an auto-start Windows Service to sync TOTP secret
/// into ProgramData for the Credential Provider.
/// </summary>
public static class WindowsServiceInstaller
{
    public const string ServiceName = "WinTotpAgent";
    public const string DisplayName = "WinTotp Windows Agent";

    public static string ExePath
        => Path.Combine(AppContext.BaseDirectory, "WinTotp.WindowsClient.exe");

    public static bool IsInstalled()
    {
        try
        {
            using var sc = new ServiceController(ServiceName);
            _ = sc.Status;
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static ServiceControllerStatus? GetStatus()
    {
        if (!IsInstalled()) return null;
        using var sc = new ServiceController(ServiceName);
        sc.Refresh();
        return sc.Status;
    }

    public static bool IsRunning()
        => GetStatus() is ServiceControllerStatus.Running or ServiceControllerStatus.StartPending;

    public static void Install()
    {
        if (!File.Exists(ExePath))
            throw new FileNotFoundException("找不到客户端程序", ExePath);

        if (IsInstalled())
            throw new InvalidOperationException("开机服务已安装。");

        // sc.exe requires a space after '='. Quote exe when path contains spaces.
        var bin = ExePath.Contains(' ', StringComparison.Ordinal)
            ? $"\\\"{ExePath}\\\" --service"
            : $"{ExePath} --service";
        RunSc("create", $"{ServiceName} binPath= \"{bin}\" start= auto DisplayName= \"{DisplayName}\"");
        RunSc("description", $"{ServiceName} \"Sync WinTotp authenticator secret for Credential Provider.\"");
        RunSc("failure", $"{ServiceName} reset= 86400 actions= restart/5000/restart/5000/restart/5000");
    }

    public static void Uninstall()
    {
        if (!IsInstalled())
            return;

        try { Stop(); } catch { /* ignore */ }
        RunSc("delete", ServiceName);
        // delete is async in SCM — wait briefly
        for (var i = 0; i < 20 && IsInstalled(); i++)
            Thread.Sleep(200);
    }

    public static void Start()
    {
        using var sc = new ServiceController(ServiceName);
        sc.Refresh();
        if (sc.Status is ServiceControllerStatus.Running or ServiceControllerStatus.StartPending)
            return;
        sc.Start();
        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
    }

    public static void Stop()
    {
        if (!IsInstalled()) return;
        using var sc = new ServiceController(ServiceName);
        sc.Refresh();
        if (sc.Status is ServiceControllerStatus.Stopped or ServiceControllerStatus.StopPending)
            return;
        sc.Stop();
        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
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
            throw new InvalidOperationException($"sc {command} 失败 (code={p.ExitCode}): {stdout} {stderr}".Trim());
    }
}
