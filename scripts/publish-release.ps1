#Requires -Version 5.1
# Build all packages and create/update GitHub Release for thiswod/WinTotp
$ErrorActionPreference = 'Stop'
$Root = Split-Path $PSScriptRoot -Parent
if (-not (Test-Path (Join-Path $Root 'WinTotp.slnx'))) { $Root = 'f:\CSharp\WinTotpRemote' }

$Version = '1.0.1'
$Tag = "v$Version"
$Repo = 'thiswod/WinTotp'
$OutRoot = Join-Path $Root 'publish\release'
$NotesPath = Join-Path $OutRoot 'RELEASE_NOTES.md'

gh auth status
if ($LASTEXITCODE -ne 0) { throw 'Run: gh auth login' }

& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'build-release-packages.ps1')
if ($LASTEXITCODE -ne 0) { throw 'build-release-packages.ps1 failed' }

$zips = Get-ChildItem $OutRoot -Filter '*.zip' | Sort-Object Name
if ($zips.Count -lt 1) { throw 'No zip packages found' }

$prevEap = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
git -C $Root fetch origin master 2>&1 | Out-Null
$ErrorActionPreference = $prevEap

$sha = (git -C $Root rev-parse HEAD).Trim()
if (-not (git -C $Root tag -l $Tag)) {
    git -C $Root tag -a $Tag -m "WinTotp $Tag" $sha
    if ($LASTEXITCODE -ne 0) { throw 'git tag failed' }
    git -C $Root push origin $Tag
    if ($LASTEXITCODE -ne 0) { throw 'git push tag failed' }
}

$prevEap2 = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
$null = gh release view $Tag --repo $Repo 2>&1
$releaseExists = ($LASTEXITCODE -eq 0)
$ErrorActionPreference = $prevEap2

# Prefer freshly built notes (rebuild may have overwritten); patch changelog if missing
$notesText = [System.IO.File]::ReadAllText($NotesPath)
if ($notesText -notmatch '本版更新') {
    $tpl = Join-Path $PSScriptRoot 'RELEASE_NOTES.template.md'
    if (Test-Path $tpl) {
        $Utf8 = New-Object System.Text.UTF8Encoding $false
        $cpBuilt = @()
        foreach ($a in @('x64','x86','arm64')) {
            if (Test-Path (Join-Path $Root "publish\credential-provider\$a\WinTotp.CredentialProvider.dll")) { $cpBuilt += $a }
        }
        $notes = [System.IO.File]::ReadAllText($tpl, $Utf8)
        $cpAssets = ($cpBuilt | ForEach-Object { "- ``WinTotp.CredentialProvider-v$Version-$_.zip``" }) -join "`n"
        if (-not $cpAssets) { $cpAssets = '- (none)' }
        $notes = $notes.Replace('{{VERSION}}', $Version).Replace('{{CP_ARCHS}}', ($(if ($cpBuilt) { $cpBuilt -join ', ' } else { '(none)' }))).Replace('{{CP_ASSETS}}', $cpAssets)
        [System.IO.File]::WriteAllText($NotesPath, $notes, $Utf8)
    }
}

if ($releaseExists) {
    Write-Host "Updating existing release $Tag ..."
    gh release edit $Tag --repo $Repo --notes-file $NotesPath --title "WinTotp $Tag"
    gh release upload $Tag ($zips.FullName) --repo $Repo --clobber
} else {
    Write-Host "Creating release $Tag ..."
    gh release create $Tag ($zips.FullName) --repo $Repo --title "WinTotp $Tag" --notes-file $NotesPath --latest
}

Write-Host "Done: https://github.com/$Repo/releases/tag/$Tag"
