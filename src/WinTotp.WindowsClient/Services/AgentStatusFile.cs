using System.Text.Json;

namespace WinTotp.WindowsClient.Services;

/// <summary>
/// Small status file next to the exe so the GUI can show why live password sync is blocked
/// even when the Windows service itself is "Running".
/// </summary>
public static class AgentStatusFile
{
    private static readonly JsonSerializerOptions JsonWrite = new() { WriteIndented = true };
    private static readonly object Gate = new();

    public static string GetPath()
        => Path.Combine(AppContext.BaseDirectory, "agent-status.json");

    public static void Write(
        bool dryRun,
        string? lastError,
        string? targetUser = null,
        DateTimeOffset? lastAppliedUtc = null)
    {
        try
        {
            var dto = new AgentStatusDto
            {
                UpdatedUtc = DateTimeOffset.UtcNow,
                DryRun = dryRun,
                TargetUser = targetUser ?? "",
                LastError = string.IsNullOrWhiteSpace(lastError) ? null : lastError.Trim(),
                LastAppliedUtc = lastAppliedUtc,
                Ok = string.IsNullOrWhiteSpace(lastError) && !dryRun
            };

            var path = GetPath();
            var tmp = path + ".tmp";
            lock (Gate)
            {
                File.WriteAllText(tmp, JsonSerializer.Serialize(dto, JsonWrite));
                File.Move(tmp, path, overwrite: true);
            }
        }
        catch
        {
            // Status is best-effort; never break password rotation for logging.
        }
    }

    public static AgentStatusDto? Read()
    {
        try
        {
            var path = GetPath();
            if (!File.Exists(path))
                return null;
            return JsonSerializer.Deserialize<AgentStatusDto>(File.ReadAllText(path));
        }
        catch
        {
            return null;
        }
    }
}

public sealed class AgentStatusDto
{
    public DateTimeOffset UpdatedUtc { get; set; }
    public bool DryRun { get; set; }
    public bool Ok { get; set; }
    public string TargetUser { get; set; } = "";
    public string? LastError { get; set; }
    public DateTimeOffset? LastAppliedUtc { get; set; }
}
