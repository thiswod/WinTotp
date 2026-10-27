#Requires -RunAsAdministrator
# Unregister WinTotp Credential Provider (escape hatch while still logged in).
$ErrorActionPreference = 'Stop'
$clsid = '{7F3E9A2B-4C5D-4E8F-9A1B-2C3D4E5F6A7B}'
$dll = Join-Path $env:ProgramData 'WinTotp\WinTotp.CredentialProvider.dll'

if (Test-Path $dll) {
    & "$env:SystemRoot\System32\regsvr32.exe" /s /u $dll
    Write-Host "regsvr32 /u => $LASTEXITCODE"
}

Remove-Item -LiteralPath "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\Credential Providers\$clsid" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath "HKLM:\SOFTWARE\Classes\CLSID\$clsid" -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "Credential Provider unregistered."
Write-Host "Optional: remove config with: Remove-Item '$env:ProgramData\WinTotp\cp-config.dpapi'"
Write-Host "Reboot recommended if LogonUI still holds the DLL."
