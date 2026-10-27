# WinTotp

Windows 本机 **Authenticator TOTP** 登录（Credential Provider）。无远端服务器、无远程关机。

> 磁盘根目录仍为 `WinTotpRemote`（方便 Cursor 保留历史对话）；产品名与工程名为 **WinTotp**。

## 架构

```
WinTotp.WindowsClient（配置 GUI + 可选开机服务）
        │ 写入 ProgramData 配置 / 注册 DLL
        ▼
WinTotp.CredentialProvider（登录磁贴 TR）
        │ 校验验证器 6 位
        └ 使用账户当前 Windows 密码完成真正登录（不改密）
```

- 登录只输验证器 **6 位**；系统「密码」磁贴保留作逃生入口  
- 绑定用户**默认当前 Windows 账户**  
- 开机服务仅同步 `appsettings` 里的密钥到 `%ProgramData%\WinTotp\`  

详见：[docs/credential-provider.md](docs/credential-provider.md)

## 项目结构

| 路径 | 说明 |
|------|------|
| `WinTotp.slnx` | 解决方案 |
| `src/WinTotp.Shared` | TOTP 算法 |
| `src/WinTotp.WindowsClient` | 配置 GUI（验证器 / 登录组件 / 日志）+ 开机服务 |
| `src/WinTotp.CredentialProvider` | 原生登录组件（C++ COM DLL） |

## 快速开始

### 1. 编译 Credential Provider

需要 VS/Build Tools（MSVC + Windows SDK）。

```bat
cd src\WinTotp.CredentialProvider
_build_now.bat
```

产物：`publish\credential-provider\WinTotp.CredentialProvider.dll`（静态 CRT，无需再装 VC++ 运行库）

### 2. Windows 客户端

```powershell
cd src\WinTotp.WindowsClient
dotnet run
```

1. **管理员**运行；在「验证器」页扫码绑定（用户默认当前账户）  
2. 「登录组件」页点 **启用登录**（填写当前 Windows 密码；无密码留空）  
3. 注销 → **登录选项** → 蓝色 **TR** → 输入 6 位码  
4. **先做虚拟机快照**；Hyper-V 请用**基本会话**测登录界面  

服务模式：`WinTotp.WindowsClient.exe --service`

## 安全提示

- 推荐开启 **BitLocker（TPM+PIN）**  
- 手机/密钥丢失时用快照或恢复介质；不要依赖未知的静态密码  
- 已启用后若要换 DLL：先「卸载」再「启用登录」  
