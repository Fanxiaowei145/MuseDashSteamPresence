#requires -Version 7.2
param([string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$version = [IO.File]::ReadAllText((Join-Path $root 'VERSION')).Trim()
if (!$OutputDirectory) { $OutputDirectory = Join-Path $root 'dist' }
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
$name = 'MuseDashSteamPresence-' + $version + '-win-x64'
$stage = Join-Path $OutputDirectory $name
$zip = Join-Path $OutputDirectory ($name + '.zip')
if ((Test-Path -LiteralPath $stage) -or (Test-Path -LiteralPath $zip)) { throw 'Output exists; select a new output directory. Existing releases are never overwritten.' }
$binaries = @('MuseDashSteamPresence.dll','MuseDashSteamPresence.Bootstrap.dll','PresenceController.exe','MuseDashPresenceSettings.exe')
foreach ($file in $binaries) { if (!(Test-Path -LiteralPath (Join-Path $root ('artifacts\bin\' + $file)))) { throw "Build first; missing $file" } }
New-Item -ItemType Directory -Path $stage -Force | Out-Null
$support = Join-Path $stage 'UserData\MuseDashSteamPresence'
New-Item -ItemType Directory -Path $support,(Join-Path $stage 'Mods'),(Join-Path $stage 'Plugins') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $root 'artifacts\bin\MuseDashSteamPresence.dll') -Destination (Join-Path $stage 'Mods')
Copy-Item -LiteralPath (Join-Path $root 'artifacts\bin\MuseDashSteamPresence.Bootstrap.dll') -Destination (Join-Path $stage 'Plugins')
foreach ($file in @('PresenceController.exe','MuseDashPresenceSettings.exe')) { Copy-Item -LiteralPath (Join-Path $root ('artifacts\bin\' + $file)) -Destination $support }
foreach ($file in @('disable.ps1','enable.ps1','GameDirectory.ps1','Disable.cmd','Enable.cmd')) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot $file) -Destination $support }
foreach ($file in @('MuseDash-Settings.cmd','MuseDash-Disable.cmd','MuseDash-Enable.cmd')) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot $file) -Destination $stage }
foreach ($file in @('VERSION','README.md','LICENSE','NOTICE.md','CHANGELOG.md')) { Copy-Item -LiteralPath (Join-Path $root $file) -Destination $support }
Copy-Item -LiteralPath (Join-Path $root 'docs') -Destination (Join-Path $support 'docs') -Recurse
Copy-Item -LiteralPath (Join-Path $root 'docs\ROOT-README.md') -Destination (Join-Path $stage 'MuseDashPresence-README.md')
$plain = [IO.File]::ReadAllText((Join-Path $root 'docs\QUICKSTART.txt')).Replace("`r`n","`n").Replace("`n","`r`n")
[IO.File]::WriteAllText((Join-Path $stage 'MuseDashPresence-ReadMe.txt'), $plain, [Text.UTF8Encoding]::new($true))
$lines = Get-ChildItem -LiteralPath $stage -File -Recurse | Sort-Object FullName | ForEach-Object {
    $relative = [IO.Path]::GetRelativePath($stage, $_.FullName).Replace('\','/')
    (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + $relative
}
[IO.File]::WriteAllLines((Join-Path $stage 'MuseDashPresence-SHA256SUMS.txt'), [string[]]$lines, [Text.UTF8Encoding]::new($false))
$entries = Get-ChildItem -LiteralPath $stage -Force | ForEach-Object FullName
Compress-Archive -LiteralPath $entries -DestinationPath $zip
$hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText(($zip + '.sha256'), $hash + '  ' + [IO.Path]::GetFileName($zip) + "`n", [Text.UTF8Encoding]::new($false))
Write-Output $zip
Write-Output ('SHA256: ' + $hash)
