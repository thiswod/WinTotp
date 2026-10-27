using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace WinTotp.WindowsClient.Services;

/// <summary>
/// Installs/registers WinTotp.CredentialProvider.dll.
/// Keeps the built-in Password provider enabled as an escape hatch (Phase 1).
/// Does NOT change the Windows account password — stores the current one for CP to pack after OTP.
/// </summary>
[SupportedOSPlatform("windows")]
public static class CredentialProviderInstaller
{
    public const string Clsid = "{7F3E9A2B-4C5D-4E8F-9A1B-2C3D4E5F6A7B}";
    public const string DllFileName = "WinTotp.CredentialProvider.dll";

    public static string GetBundledDllPath()
        => Path.Combine(AppContext.BaseDirectory, "credential-provider", DllFileName);

    /// <summary>Same folder as the exe (common when packaging flat to Desktop).</summary>
    public static string GetSiblingDllPath()
        => Path.Combine(AppContext.BaseDirectory, DllFileName);

    /// <summary>ProgramData copy (legacy). Prefer System32 for LogonUI reliability.</summary>
    public static string GetProgramDataDllPath()
        => Path.Combine(CpConfigStore.GetDirectory(), DllFileName);

    /// <summary>Canonical install location — matches Microsoft sample guidance.</summary>
    public static string GetInstalledDllPath()
        => Path.Combine(Environment.SystemDirectory, DllFileName);

    /// <summary>Search order: bundled → sibling → System32 → ProgramData.</summary>
    public static string? FindSourceDll()
    {
        foreach (var path in new[]
                 {
                     GetBundledDllPath(), GetSiblingDllPath(),
                     GetInstalledDllPath(), GetProgramDataDllPath()
                 })
        {
            if (File.Exists(path))
                return path;
        }
        return null;
    }

    public static bool IsRegistered()
    {
        using var key = Registry.LocalMachine.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\Credential Providers\" + Clsid);
        return key is not null;
    }

    public static bool IsDllPresent()
        => FindSourceDll() is not null;

    public static void Install(string? sourceDllPath = null)
    {
        var source = sourceDllPath;
        if (string.IsNullOrWhiteSpace(source) || !File.Exists(source))
            source = FindSourceDll();
        // Prefer packaging location over an already-installed System32/ProgramData copy.
        if (source is not null &&
            (string.Equals(source, GetInstalledDllPath(), StringComparison.OrdinalIgnoreCase) ||
             string.Equals(source, GetProgramDataDllPath(), StringComparison.OrdinalIgnoreCase)))
        {
            source = File.Exists(GetBundledDllPath()) ? GetBundledDllPath()
                : File.Exists(GetSiblingDllPath()) ? GetSiblingDllPath()
                : source;
        }

        if (source is null || !File.Exists(source))
            throw new FileNotFoundException(
                "找不到 Credential Provider DLL。请把 WinTotp.CredentialProvider.dll 放在程序同目录，或 credential-provider\\ 子目录。",
                DllFileName);

        Directory.CreateDirectory(CpConfigStore.GetDirectory());
        var dest = GetInstalledDllPath();
        try
        {
            File.Copy(source, dest, overwrite: true);
        }
        catch (IOException ex)
        {
            // LogonUI may lock the previous DLL until reboot — fall back to ProgramData.
            dest = GetProgramDataDllPath();
            File.Copy(source, dest, overwrite: true);
            _ = ex;
        }

        // Prefer self-registration; fall back to manual registry write.
        var reg = RunRegSvr32(dest, uninstall: false);
        if (reg != 0)
            RegisterManually(dest);

        if (!IsRegistered())
            throw new InvalidOperationException("Credential Provider 注册失败。");

        VerifyComLoad();
    }

    /// <summary>
    /// CoCreate the provider in-process. Catches missing CRT / bad exports before logout.
    /// </summary>
    public static void VerifyComLoad()
    {
        try
        {
            var type = Type.GetTypeFromCLSID(new Guid(Clsid), throwOnError: true)
                ?? throw new InvalidOperationException("无法解析 Credential Provider CLSID。");
            var obj = Activator.CreateInstance(type)
                ?? throw new InvalidOperationException("CoCreateInstance 返回空。");
            try
            {
                Marshal.FinalReleaseComObject(obj);
            }
            catch
            {
                // ignore
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Credential Provider DLL 已注册，但无法被 COM 加载（常见原因：缺 VC++ 运行库，或 DLL 架构不对）。" +
                "请使用静态链接(/MT)编译的新 DLL 重新安装。详情: " + ex.Message,
                ex);
        }
    }

    public static void Uninstall()
    {
        foreach (var dest in new[] { GetInstalledDllPath(), GetProgramDataDllPath() })
        {
            if (File.Exists(dest))
                RunRegSvr32(dest, uninstall: true);
        }

        UnregisterManually();

        foreach (var dest in new[] { GetInstalledDllPath(), GetProgramDataDllPath() })
        {
            try
            {
                if (File.Exists(dest))
                    File.Delete(dest);
            }
            catch
            {
                // DLL may be locked by LogonUI until reboot.
            }
        }
    }

    /// <summary>Human-readable health check for the GUI (registry / COM / config / DLL path).</summary>
    public static string Diagnose()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== WinTotp CP 诊断 ===");
        sb.AppendLine("注册表 Credential Providers: " + (IsRegistered() ? "有" : "无"));
        using (var inproc = Registry.LocalMachine.OpenSubKey(@"Software\Classes\CLSID\" + Clsid + @"\InprocServer32"))
        {
            var dll = inproc?.GetValue(null) as string;
            sb.AppendLine("InprocServer32: " + (dll ?? "(缺失)"));
            if (!string.IsNullOrEmpty(dll))
                sb.AppendLine("  文件存在: " + File.Exists(dll) + "  大小: " + (File.Exists(dll) ? new FileInfo(dll).Length + " bytes" : "-"));
        }
        sb.AppendLine("System32 DLL: " + GetInstalledDllPath() + " => " + File.Exists(GetInstalledDllPath()));
        sb.AppendLine("ProgramData DLL: " + GetProgramDataDllPath() + " => " + File.Exists(GetProgramDataDllPath()));
        sb.AppendLine("配置文件: " + CpConfigStore.GetPath() + " => " + CpConfigStore.Exists());
        var cfg = CpConfigStore.TryLoad();
        if (cfg is null)
            sb.AppendLine("配置解密: 失败或为空");
        else
            sb.AppendLine($"配置解密: OK  TargetUser={cfg.TargetUser}  SecretLen={cfg.TotpSecret.Length}  PwdLen={cfg.StaticPassword.Length}  Step={cfg.TotpStepSeconds}");

        try
        {
            VerifyComLoad();
            sb.AppendLine("COM CoCreate: OK");
        }
        catch (Exception ex)
        {
            sb.AppendLine("COM CoCreate: FAIL — " + ex.Message);
        }

        sb.AppendLine();
        sb.AppendLine("登录界面操作：点「登录选项」，应出现蓝色 TR 图标（WinTotp）。");
        sb.AppendLine("若没有图标，看 %ProgramData%\\WinTotp\\cp-debug.log；Hyper-V 请用基本会话。");
        return sb.ToString();
    }

    /// <summary>
    /// Enable CP mode: register DLL, write protected config with the account's CURRENT Windows password
    /// (empty string if the account has no password). Does not call SetPassword — avoids lockout.
    /// </summary>
    /// <param name="currentWindowsPassword">Current local logon password; empty if none.</param>
    public static string Enable(
        string targetUser,
        string totpSecret,
        int totpStepSeconds,
        string? currentWindowsPassword = null)
    {
        if (string.IsNullOrWhiteSpace(targetUser))
            throw new ArgumentException("目标用户不能为空。", nameof(targetUser));
        if (string.IsNullOrWhiteSpace(totpSecret))
            throw new ArgumentException("TOTP 密钥不能为空。", nameof(totpSecret));

        // 1) Install + COM-load test FIRST — never leave the user with a changed password and a broken CP.
        Install();

        var cfg = new CpConfigStore.CpConfig
        {
            TargetUser = targetUser.Trim(),
            TotpSecret = totpSecret.Replace(" ", "", StringComparison.Ordinal).ToUpperInvariant(),
            TotpStepSeconds = totpStepSeconds <= 0 ? 30 : totpStepSeconds,
            StaticPassword = currentWindowsPassword ?? ""
        };
        CpConfigStore.Save(cfg);

        var escape = string.IsNullOrEmpty(cfg.StaticPassword)
            ? "系统「密码」磁贴：直接回车（账户无密码）"
            : "系统「密码」磁贴：输入你刚才填写的当前 Windows 密码";

        return "已启用 Credential Provider（未改 Windows 密码）。登录用验证器 6 位；逃生：" + escape + "。";
    }

    public static void SyncSecretFromAppSettings(string totpSecret, int totpStepSeconds, string? targetUser = null)
    {
        var cfg = CpConfigStore.TryLoad();
        if (cfg is null)
            return;

        cfg.TotpSecret = totpSecret.Replace(" ", "", StringComparison.Ordinal).ToUpperInvariant();
        cfg.TotpStepSeconds = totpStepSeconds <= 0 ? 30 : totpStepSeconds;
        if (!string.IsNullOrWhiteSpace(targetUser))
            cfg.TargetUser = targetUser.Trim();
        CpConfigStore.Save(cfg);
    }

    private static int RunRegSvr32(string dllPath, bool uninstall)
    {
        var args = uninstall
            ? $"/s /u \"{dllPath}\""
            : $"/s \"{dllPath}\"";
        using var p = Process.Start(new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.SystemDirectory, "regsvr32.exe"),
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true
        });
        if (p is null) return -1;
        p.WaitForExit(30000);
        return p.ExitCode;
    }

    private static void RegisterManually(string dllPath)
    {
        using (var clsid = Registry.LocalMachine.CreateSubKey(@"Software\Classes\CLSID\" + Clsid))
        {
            clsid.SetValue(null, "WinTotp Credential Provider");
            using var inproc = clsid.CreateSubKey("InprocServer32");
            inproc.SetValue(null, dllPath);
            inproc.SetValue("ThreadingModel", "Apartment");
        }

        using var cp = Registry.LocalMachine.CreateSubKey(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\Credential Providers\" + Clsid);
        cp.SetValue(null, "WinTotpCredentialProvider");
    }

    private static void UnregisterManually()
    {
        try { Registry.LocalMachine.DeleteSubKeyTree(@"Software\Classes\CLSID\" + Clsid, throwOnMissingSubKey: false); }
        catch { /* ignore */ }
        try
        {
            Registry.LocalMachine.DeleteSubKeyTree(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\Credential Providers\" + Clsid,
                throwOnMissingSubKey: false);
        }
        catch { /* ignore */ }
    }
}
