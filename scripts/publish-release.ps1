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

$existing = gh release view $Tag --repo $Repo 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "Updating existing release $Tag ..."
    gh release edit $Tag --repo $Repo --notes-file $NotesPath --title "WinTotp $Tag"
    # Remove legacy ambiguous asset if present
    gh release delete-asset $Tag "WinTotp-$Tag-win-x64.zip" --repo $Repo --yes 2>$null
    gh release upload $Tag ($zips.FullName) --repo $Repo --clobber
} else {
    Write-Host "Creating release $Tag ..."
    gh release create $Tag ($zips.FullName) --repo $Repo --title "WinTotp $Tag" --notes-file $NotesPath --latest
}

Write-Host "Done: https://github.com/$Repo/releases/tag/$Tag"
