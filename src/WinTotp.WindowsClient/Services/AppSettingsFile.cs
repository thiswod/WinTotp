using System.Text.Json;
using System.Text.Json.Nodes;
using WinTotp.WindowsClient.Options;

namespace WinTotp.WindowsClient.Services;

public static class AppSettingsFile
{
    private static readonly JsonSerializerOptions JsonWrite = new() { WriteIndented = true };

    public static string GetPath()
        => Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    public static WinTotpOptions Load()
    {
        var path = GetPath();
        if (!File.Exists(path))
            return new WinTotpOptions();

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (!doc.RootElement.TryGetProperty(WinTotpOptions.SectionName, out var section))
                return new WinTotpOptions();

            return JsonSerializer.Deserialize<WinTotpOptions>(section.GetRawText())
                   ?? new WinTotpOptions();
        }
        catch
        {
            return new WinTotpOptions();
        }
    }

    public static void Save(WinTotpOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.TargetUser))
            options.TargetUser = Environment.UserName;

        var path = GetPath();
        JsonObject root;
        if (File.Exists(path))
        {
            root = JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? new JsonObject();
        }
        else
        {
            root = new JsonObject
            {
                ["Logging"] = new JsonObject
                {
                    ["LogLevel"] = new JsonObject
                    {
                        ["Default"] = "Information",
                        ["Microsoft.Hosting.Lifetime"] = "Information"
                    }
                }
            };
        }

        root[WinTotpOptions.SectionName] = JsonSerializer.SerializeToNode(options);
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, root.ToJsonString(JsonWrite));
        File.Move(tmp, path, overwrite: true);
    }
}
