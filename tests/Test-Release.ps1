#requires -Version 7.2
param([Parameter(Mandatory)][string]$ZipPath)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$ZipPath = [IO.Path]::GetFullPath($ZipPath)
function Assert-Release([bool]$Condition, [string]$Message) { if (!$Condition) { throw $Message } }
$fixtures = Join-Path $root '.local'
$fixture = Join-Path $fixtures ('release-test-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fixture -Force | Out-Null
# This directory is a fake game installation. No actual game or Steam APIs run.
[IO.File]::WriteAllText((Join-Path $fixture 'musedash.exe'), 'existing-game-sentinel')
New-Item -ItemType Directory -Path (Join-Path $fixture 'Mods'),(Join-Path $fixture 'UserData\MuseDashSteamPresence') -Force | Out-Null
[IO.File]::WriteAllText((Join-Path $fixture 'Mods\Euterpe.dll'), 'existing-other-mod-sentinel')
[IO.File]::WriteAllText((Join-Path $fixture 'UserData\MuseDashSteamPresence\settings.xml'), 'existing-user-config-sentinel')
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($ZipPath)
try {
    $names = @($archive.Entries | Where-Object { $_.Name.Length -gt 0 } | ForEach-Object { $_.FullName.Replace('\','/') })
    Assert-Release ($names -contains 'Mods/MuseDashSteamPresence.dll') 'ZIP has an extra containing folder or is missing the mod.'
    Assert-Release ($names -contains 'Plugins/MuseDashSteamPresence.Bootstrap.dll') 'Early plugin is missing.'
    Assert-Release ($names -contains 'UserData/MuseDashSteamPresence/PresenceController.exe') 'Controller is missing.'
    Assert-Release ($names -contains 'UserData/MuseDashSteamPresence/MuseDashPresenceSettings.exe') 'Settings software is missing.'
    Assert-Release ($names -contains 'MuseDash-Settings.cmd') 'Root settings entry is missing.'
    Assert-Release ($names -contains 'MuseDashPresence-ReadMe.txt') 'Plain text quick start is missing.'
    foreach ($name in $names) {
        Assert-Release (!$name.StartsWith('/') -and $name -notmatch '(^|/)\.\.(/|$)|:') "Unsafe ZIP path: $name"
        Assert-Release ($name -notmatch '(^|/)(settings\.xml|state|Euterpe\.dll|GameAssembly\.dll|steamclient64\.dll|musedash\.exe)(/|$)') "Local/third-party file included: $name"
    }
    $binaries = @($names | Where-Object { $_ -match '\.(dll|exe)$' })
    Assert-Release ($binaries.Count -eq 4) 'Unexpected binary bundled with release.'
} finally { $archive.Dispose() }
Expand-Archive -LiteralPath $ZipPath -DestinationPath $fixture -Force
$manifest = Join-Path $fixture 'MuseDashPresence-SHA256SUMS.txt'
Assert-Release (Test-Path -LiteralPath $manifest) 'SHA256 manifest is missing.'
$checked = @{}
foreach ($line in [IO.File]::ReadAllLines($manifest)) {
    Assert-Release ($line -match '^([a-fA-F0-9]{64})  (.+)$') 'Invalid manifest entry.'
    $expected = $Matches[1]; $relative = $Matches[2]
    Assert-Release (!$checked.ContainsKey($relative)) "Duplicate checksum: $relative"
    $checked[$relative] = $true
    Assert-Release ($names -contains $relative) "Checksum references an absent file: $relative"
    Assert-Release ((Get-FileHash -LiteralPath (Join-Path $fixture $relative) -Algorithm SHA256).Hash -eq $expected) "Checksum mismatch: $relative"
}
Assert-Release ($checked.Count -eq $names.Count - 1) 'Manifest does not cover every payload file.'
$sidecar = $ZipPath + '.sha256'
Assert-Release (Test-Path -LiteralPath $sidecar) 'ZIP sidecar hash is missing.'
$zipHash = (Get-FileHash -LiteralPath $ZipPath -Algorithm SHA256).Hash
Assert-Release ([IO.File]::ReadAllText($sidecar).StartsWith($zipHash, [StringComparison]::OrdinalIgnoreCase)) 'ZIP sidecar hash mismatch.'
$support = Join-Path $fixture 'UserData\MuseDashSteamPresence'
$mod = Join-Path $fixture 'Mods\MuseDashSteamPresence.dll'
$early = Join-Path $fixture 'Plugins\MuseDashSteamPresence.Bootstrap.dll'
# Windows PowerShell 5.1 is used by the end-user CMD entry points.
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $support 'disable.ps1') -GamePath $fixture
Assert-Release ($LASTEXITCODE -eq 0) 'Disable failed under Windows PowerShell.'
Assert-Release ((Test-Path -LiteralPath ($mod + '.disabled')) -and (Test-Path -LiteralPath ($early + '.disabled')) -and !(Test-Path -LiteralPath $mod) -and !(Test-Path -LiteralPath $early)) 'Both plugins were not disabled.'
# If any enabled counterpart exists, enable must leave ALL files unchanged.
[IO.File]::WriteAllText($mod, 'collision-sentinel')
$collision = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $support 'enable.ps1') -GamePath $fixture 2>&1
Assert-Release ($LASTEXITCODE -ne 0) 'Conflicting enable should fail.'
Assert-Release ((Test-Path -LiteralPath ($early + '.disabled')) -and !(Test-Path -LiteralPath $early)) 'Enable changed the second plugin after a preflight conflict.'
[IO.File]::Delete($mod)
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $support 'enable.ps1') -GamePath $fixture
Assert-Release ($LASTEXITCODE -eq 0) 'Enable failed under Windows PowerShell.'
Assert-Release ((Test-Path -LiteralPath $mod) -and (Test-Path -LiteralPath $early)) 'Both plugins were not restored.'
Assert-Release ([IO.File]::ReadAllText((Join-Path $fixture 'musedash.exe')) -eq 'existing-game-sentinel') 'Existing game file changed.'
Assert-Release ([IO.File]::ReadAllText((Join-Path $fixture 'Mods\Euterpe.dll')) -eq 'existing-other-mod-sentinel') 'Existing third-party mod changed.'
Assert-Release ([IO.File]::ReadAllText((Join-Path $support 'settings.xml')) -eq 'existing-user-config-sentinel') 'User settings were overwritten.'
Write-Output 'PASS: ZIP root layout, four owned binaries, full SHA256 manifests, extraction preserves game/mod/settings, both-plugin disable/enable and conflict preflight; no live Steam calls.'
Write-Output "Isolated test files retained in: $fixture"
