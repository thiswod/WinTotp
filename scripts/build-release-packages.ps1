#Requires -Version 5.1
# Build WinTotp release zips: win-x64/x86/arm64 x framework-dependent / self-contained
$ErrorActionPreference = 'Stop'
$Root = Split-Path $PSScriptRoot -Parent
if (-not (Test-Path (Join-Path $Root 'WinTotp.slnx'))) { $Root = 'f:\CSharp\WinTotpRemote' }

$Version = '1.0.1'
$ClientProj = Join-Path $Root 'src\WinTotp.WindowsClient\WinTotp.WindowsClient.csproj'
$CpSrc = Join-Path $Root 'src\WinTotp.CredentialProvider'
$OutRoot = Join-Path $Root 'publish\release'
$CpOutRoot = Join-Path $Root 'publish\credential-provider'
$Staging = Join-Path $OutRoot '_staging'
$Utf8 = New-Object System.Text.UTF8Encoding $false

New-Item -ItemType Directory -Force -Path $OutRoot | Out-Null
New-Item -ItemType Directory -Force -Path $CpOutRoot | Out-Null
if (Test-Path $Staging) { Remove-Item $Staging -Recurse -Force }
New-Item -ItemType Directory -Force -Path $Staging | Out-Null

function Find-VcVars([string]$Name) {
    $candidates = @(
        "C:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools\VC\Auxiliary\Build\$Name"
        "D:\Program Files\Microsoft Visual Studio\18\Community\VC\Auxiliary\Build\$Name"
        "C:\Program Files\Microsoft Visual Studio\18\Community\VC\Auxiliary\Build\$Name"
    )
    foreach ($c in $candidates) { if (Test-Path $c) { return $c } }
    return $null
}

function Build-CredentialProvider([string]$Arch) {
    $vcvarsMap = @{
        'x64'   = @('vcvars64.bat', 'vcvarsall.bat|x64')
        'x86'   = @('vcvars32.bat', 'vcvarsamd64_x86.bat', 'vcvarsall.bat|x86')
        'arm64' = @('vcvarsarm64.bat', 'vcvarsamd64_arm64.bat', 'vcvarsall.bat|arm64')
    }
    $archDir = Join-Path $CpOutRoot $Arch
    New-Item -ItemType Directory -Force -Path $archDir | Out-Null
    $dll = Join-Path $archDir 'WinTotp.CredentialProvider.dll'
    $objDir = Join-Path $CpSrc "obj\$Arch"
    New-Item -ItemType Directory -Force -Path $objDir | Out-Null

    $vcvars = $null
    $vcargs = ''
    foreach ($entry in $vcvarsMap[$Arch]) {
        $parts = $entry -split '\|', 2
        $path = Find-VcVars $parts[0]
        if ($path) {
            $vcvars = $path
            if ($parts.Count -gt 1) { $vcargs = $parts[1] }
            break
        }
    }
    if (-not $vcvars) {
        Write-Warning "No VC toolchain for $Arch - skip Credential Provider"
        return $null
    }

    $clCmd = @(
        'cl /nologo /O2 /W3 /EHsc /std:c++17 /DUNICODE /D_UNICODE /DWIN32 /D_WINDOWS /D_USRDLL /DNDEBUG /MT'
        'dllmain.cpp TotpProvider.cpp TotpCredential.cpp helpers.cpp config.cpp totp.cpp diag.cpp'
        "/Fo`"$objDir\\`" /link /nologo /DLL /DEF:WinTotp.CredentialProvider.def"
        "/OUT:`"$dll`""
        'credui.lib bcrypt.lib crypt32.lib advapi32.lib ole32.lib oleaut32.lib shlwapi.lib secur32.lib gdi32.lib user32.lib'
    ) -join ' '

    $bat = Join-Path $env:TEMP "wintotp-build-cp-$Arch.bat"
    if ($vcargs) {
        $content = "@echo off`r`ncall `"$vcvars`" $vcargs || exit /b 1`r`ncd /d `"$CpSrc`"`r`n$clCmd`r`nexit /b %ERRORLEVEL%`r`n"
    } else {
        $content = "@echo off`r`ncall `"$vcvars`" || exit /b 1`r`ncd /d `"$CpSrc`"`r`n$clCmd`r`nexit /b %ERRORLEVEL%`r`n"
    }
    [System.IO.File]::WriteAllText($bat, $content, [System.Text.Encoding]::ASCII)

    Write-Host "=== Building Credential Provider ($Arch) ==="
    & cmd /c $bat
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path $dll)) {
        Write-Warning "Credential Provider $Arch build failed"
        return $null
    }
    if ($Arch -eq 'x64') {
        Copy-Item $dll (Join-Path $CpOutRoot 'WinTotp.CredentialProvider.dll') -Force
    }
    Write-Host "OK: $dll"
    return $dll
}

function Publish-Client([string]$Rid, [bool]$SelfContained) {
    $mode = if ($SelfContained) { 'sc' } else { 'fd' }
    $label = if ($SelfContained) { 'self-contained' } else { 'framework-dependent' }
    $outDir = Join-Path $Staging "client-$Rid-$mode"
    if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }

    Write-Host "=== Publish client $Rid ($label) ==="
    $dotnetArgs = @(
        'publish', $ClientProj
        '-c', 'Release'
        '-r', $Rid
        '--self-contained', ($(if ($SelfContained) { 'true' } else { 'false' }))
        '-o', $outDir
        '-p:DebugType=None'
        '-p:DebugSymbols=false'
    )
    if ($SelfContained) {
        $dotnetArgs += @(
            '-p:PublishSingleFile=true'
            '-p:IncludeNativeLibrariesForSelfExtract=true'
            '-p:EnableCompressionInSingleFile=true'
        )
    }
    $null = & dotnet @dotnetArgs
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed: $Rid $mode" }

    $arch = switch -Regex ($Rid) {
        'x64'   { 'x64' }
        'x86'   { 'x86' }
        'arm64' { 'arm64' }
        default { $null }
    }
    $cpDll = if ($arch) { Join-Path $CpOutRoot "$arch\WinTotp.CredentialProvider.dll" } else { $null }
    $cpDest = Join-Path $outDir 'credential-provider'
    New-Item -ItemType Directory -Force -Path $cpDest | Out-Null
    if ($cpDll -and (Test-Path $cpDll)) {
        Copy-Item $cpDll $cpDest -Force
    } else {
        $note = "No matching Credential Provider DLL for this RID.`r`nLogin tile requires a native DLL matching the OS architecture.`r`n"
        [System.IO.File]::WriteAllText((Join-Path $cpDest 'README.txt'), $note, $Utf8)
    }

    $zipName = "WinTotp-v$Version-$Rid-$mode.zip"
    $zipPath = Join-Path $OutRoot $zipName
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    Compress-Archive -Path (Join-Path $outDir '*') -DestinationPath $zipPath -CompressionLevel Optimal
    $mb = [math]::Round((Get-Item $zipPath).Length / 1MB, 2)
    Write-Host "OK: $zipPath ($mb MB)"
    return $zipPath
}

function Zip-CpOnly([string]$Arch) {
    $dll = Join-Path $CpOutRoot "$Arch\WinTotp.CredentialProvider.dll"
    if (-not (Test-Path $dll)) { return $null }
    $zipPath = Join-Path $OutRoot "WinTotp.CredentialProvider-v$Version-$Arch.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    Compress-Archive -Path $dll -DestinationPath $zipPath -CompressionLevel Optimal
    Write-Host "OK: $zipPath"
    return $zipPath
}

$cpBuilt = @()
foreach ($a in @('x64', 'x86', 'arm64')) {
    $p = Build-CredentialProvider $a
    if ($p) { $cpBuilt += $a }
}

$zips = @()
foreach ($rid in @('win-x64', 'win-x86', 'win-arm64')) {
    $zips += Publish-Client $rid $false
    $zips += Publish-Client $rid $true
}
foreach ($a in $cpBuilt) {
    $z = Zip-CpOnly $a
    if ($z) { $zips += $z }
}

$legacy = Join-Path $OutRoot "WinTotp-v$Version-win-x64.zip"
if (Test-Path $legacy) { Remove-Item $legacy -Force }

$templatePath = Join-Path $PSScriptRoot 'RELEASE_NOTES.template.md'
$notes = [System.IO.File]::ReadAllText($templatePath, $Utf8)
$cpList = if ($cpBuilt.Count) { $cpBuilt -join ', ' } else { '(none)' }
$cpAssets = ($cpBuilt | ForEach-Object { "- ``WinTotp.CredentialProvider-v$Version-$_.zip``" }) -join "`n"
if (-not $cpAssets) { $cpAssets = '- (none)' }
$notes = $notes.Replace('{{VERSION}}', $Version).Replace('{{CP_ARCHS}}', $cpList).Replace('{{CP_ASSETS}}', $cpAssets)
$notesPath = Join-Path $OutRoot 'RELEASE_NOTES.md'
[System.IO.File]::WriteAllText($notesPath, $notes, $Utf8)

Write-Host ""
Write-Host "Built $($zips.Count) packages under $OutRoot"
Write-Host "Notes: $notesPath"
$zips
