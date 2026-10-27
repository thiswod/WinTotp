using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;

namespace WinTotp.WindowsClient.Services;

/// <summary>
/// Shared ProgramData store for Credential Provider + agent.
/// File is DPAPI LocalMachine encrypted and ACL'd to SYSTEM + Administrators.
/// </summary>
[SupportedOSPlatform("windows")]
public static class CpConfigStore
{
    public const string RelativeDir = "WinTotp";
    public const string FileName = "cp-config.dpapi";

    public static string GetDirectory()
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            RelativeDir);

    public static string GetPath()
        => Path.Combine(GetDirectory(), FileName);

    public sealed class CpConfig
    {
        public int Version { get; set; } = 1;
        public string TargetUser { get; set; } = "";
        public string TotpSecret { get; set; } = "";
        public int TotpStepSeconds { get; set; } = 30;
        public string StaticPassword { get; set; } = "";
    }

    public static bool Exists() => File.Exists(GetPath());

    public static CpConfig? TryLoad()
    {
        var path = GetPath();
        if (!File.Exists(path))
            return null;

        try
        {
            var enc = File.ReadAllBytes(path);
            var json = ProtectedData.Unprotect(enc, optionalEntropy: null, DataProtectionScope.LocalMachine);
            var cfg = JsonSerializer.Deserialize<CpConfig>(json);
            CryptographicOperations.ZeroMemory(json);
            return cfg;
        }
        catch
        {
            return null;
        }
    }

    public static void Save(CpConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (string.IsNullOrWhiteSpace(config.TargetUser))
            throw new InvalidOperationException("TargetUser 不能为空。");
        if (string.IsNullOrWhiteSpace(config.TotpSecret))
            throw new InvalidOperationException("TotpSecret 不能为空。");
        // StaticPassword may be empty for local accounts that have no Windows password.

        var dir = GetDirectory();
        Directory.CreateDirectory(dir);
        HardenDirectoryAcl(dir);

        var json = JsonSerializer.SerializeToUtf8Bytes(config, new JsonSerializerOptions { WriteIndented = false });
        try
        {
            var enc = ProtectedData.Protect(json, optionalEntropy: null, DataProtectionScope.LocalMachine);
            var path = GetPath();
            var tmp = path + ".tmp";
            File.WriteAllBytes(tmp, enc);
            File.Move(tmp, path, overwrite: true);
            HardenFileAcl(path);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(json);
        }
    }

    public static string GenerateStaticPassword(int length = 32)
    {
        const string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*-_=+";
        var bytes = RandomNumberGenerator.GetBytes(length);
        var chars = new char[length];
        for (var i = 0; i < length; i++)
            chars[i] = alphabet[bytes[i] % alphabet.Length];
        CryptographicOperations.ZeroMemory(bytes);
        return new string(chars);
    }

    public static void Delete()
    {
        var path = GetPath();
        if (File.Exists(path))
            File.Delete(path);
    }

    private static void HardenDirectoryAcl(string dir)
    {
        var di = new DirectoryInfo(dir);
        var sec = di.GetAccessControl();
        sec.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        var rules = sec.GetAccessRules(true, true, typeof(SecurityIdentifier));
        foreach (FileSystemAccessRule rule in rules)
            sec.RemoveAccessRule(rule);

        var system = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
        var admins = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
        sec.AddAccessRule(new FileSystemAccessRule(system, FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
        sec.AddAccessRule(new FileSystemAccessRule(admins, FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
        di.SetAccessControl(sec);
    }

    private static void HardenFileAcl(string path)
    {
        var fi = new FileInfo(path);
        var sec = fi.GetAccessControl();
        sec.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        var rules = sec.GetAccessRules(true, true, typeof(SecurityIdentifier));
        foreach (FileSystemAccessRule rule in rules)
            sec.RemoveAccessRule(rule);

        var system = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
        var admins = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
        sec.AddAccessRule(new FileSystemAccessRule(system, FileSystemRights.FullControl, AccessControlType.Allow));
        sec.AddAccessRule(new FileSystemAccessRule(admins, FileSystemRights.FullControl, AccessControlType.Allow));
        fi.SetAccessControl(sec);
    }
}
