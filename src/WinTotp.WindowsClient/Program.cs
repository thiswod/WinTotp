using Microsoft.Extensions.Hosting.WindowsServices;
using WinTotp.WindowsClient.Gui;
using WinTotp.WindowsClient.Options;
using WinTotp.WindowsClient.Services;

// Windows 服务（同步 CP 密钥）：WinTotp.WindowsClient.exe --service
// 默认启动图形界面（绑定验证器、安装登录组件）
if (ShouldRunHeadless(args))
{
    await RunHeadlessAsync(args);
    return;
}

ApplicationConfiguration.Initialize();
Application.Run(new MainForm());

static bool ShouldRunHeadless(string[] args)
{
    if (args.Any(a => string.Equals(a, "--service", StringComparison.OrdinalIgnoreCase)
                      || string.Equals(a, "--headless", StringComparison.OrdinalIgnoreCase)))
        return true;

    try
    {
        return WindowsServiceHelpers.IsWindowsService();
    }
    catch
    {
        return false;
    }
}

static async Task RunHeadlessAsync(string[] args)
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "WinTotp Windows Agent";
    });

    // 服务进程工作目录可能是 System32，强制从 exe 目录读配置。
    builder.Configuration.Sources.Clear();
    builder.Configuration
        .AddJsonFile(AppSettingsFile.GetPath(), optional: false, reloadOnChange: true)
        .AddEnvironmentVariables();

    builder.Services.Configure<WinTotpOptions>(
        builder.Configuration.GetSection(WinTotpOptions.SectionName));
    builder.Services.AddSingleton<WindowsPasswordService>();
    builder.Services.AddHostedService<AgentWorker>();

    await builder.Build().RunAsync();
}
