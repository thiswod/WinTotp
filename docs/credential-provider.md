# Credential Provider 安装与测试

## 架构（MVP）

- 登录/锁屏：`WinTotp.CredentialProvider.dll` 校验验证器 **6 位**。
- 通过后使用 ProgramData 里保存的**当前 Windows 密码**完成真正登录（**不会** `SetPassword`）。
- 系统「密码」磁贴保留作逃生入口。
- 开机服务：仅同步 TotpSecret 到 CP 配置（无远程关机）。

配置：`%ProgramData%\WinTotp\cp-config.dpapi`  
日志：`%ProgramData%\WinTotp\cp-debug.log`

## 编译 DLL

```bat
cd src\WinTotp.CredentialProvider
_build_now.bat
```

产物（**/MT** 静态 CRT）：`publish\credential-provider\WinTotp.CredentialProvider.dll`

## 测试清单（务必先快照）

1. 管理员运行客户端；绑定验证器（目标用户默认当前账户）。
2. **安装/启用登录组件**：当前 Windows 密码（无密码留空）。
3. Hyper-V 用**基本会话**；注销后点「登录选项」→ 蓝色 **TR** → 输入 6 位。
4. 不要把 6 位填进系统「密码」框。

## 卸载

客户端 → **卸载登录组件**；可选删除 `cp-config.dpapi`。

锁机时：还原检查点，或离线删除 Credential Providers / CLSID 注册表项并重置密码。
