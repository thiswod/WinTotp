## WinTotp v{{VERSION}}

Windows 本机 Authenticator TOTP 登录（Credential Provider）。

### 客户端包怎么选

| 文件后缀 | 含义 |
|---------|------|
| `*-fd.zip` | **依赖 .NET 8**：体积小，需先装 [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) |
| `*-sc.zip` | **独立运行**：自带运行时，解压即可用（体积更大） |

| RID | 适用机器 |
|-----|---------|
| `win-x64` | 常见 64 位 Windows（推荐） |
| `win-x86` | 32 位 Windows |
| `win-arm64` | Windows on ARM |

### 资源列表

**依赖 .NET（fd）**
- `WinTotp-v{{VERSION}}-win-x64-fd.zip`
- `WinTotp-v{{VERSION}}-win-x86-fd.zip`
- `WinTotp-v{{VERSION}}-win-arm64-fd.zip`

**独立运行（sc）**
- `WinTotp-v{{VERSION}}-win-x64-sc.zip`
- `WinTotp-v{{VERSION}}-win-x86-sc.zip`
- `WinTotp-v{{VERSION}}-win-arm64-sc.zip`

**仅 Credential Provider DLL**（已编译：{{CP_ARCHS}}）
{{CP_ASSETS}}

> ARM64 本机登录组件需 ARM64 工具链；当前构建机未装 ARM64 MSVC，故 `win-arm64` 客户端包可跑配置 GUI，启用登录需后续补 ARM64 DLL。

### 使用
1. 按架构与是否已装 .NET 选包，解压后**管理员**运行 `WinTotp.WindowsClient.exe`
2. 「验证器」扫码绑定 → 「登录组件」点 **启用登录**（当前 Windows 密码；无密码留空）
3. 注销 → 登录选项 → **TR** → 输入 6 位码
4. 登录组件 DLL 须与系统架构一致（x64 系统用 x64 DLL）

### 注意
- 用于本机 Windows 登录；首次试用建议先在测试机验证
- 配置目录：`%ProgramData%\WinTotp\`
