#requires -Version 7.2
param([Parameter(Mandatory)][string]$BinDirectory)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$work = Join-Path $root ('artifacts\payload-' + [guid]::NewGuid().ToString('N'))
$support = Join-Path $work 'UserData\MuseDashSteamPresence'
New-Item -ItemType Directory -Path $support,(Join-Path $work 'Plugins') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $BinDirectory 'MuseDashSteamPresence.Bootstrap.dll') -Destination (Join-Path $work 'Plugins')
foreach ($file in @('PresenceController.exe','MuseDashPresenceSettings.exe')) { Copy-Item -LiteralPath (Join-Path $BinDirectory $file) -Destination $support }
foreach ($file in @('GameDirectory.ps1','disable.ps1','enable.ps1','Disable.cmd','Enable.cmd')) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot $file) -Destination $support }
foreach ($file in @('MuseDash-Settings.cmd','MuseDash-Disable.cmd','MuseDash-Enable.cmd')) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot $file) -Destination $work }
foreach ($file in @('VERSION','LICENSE','NOTICE.md','README.md','CHANGELOG.md')) { Copy-Item -LiteralPath (Join-Path $root $file) -Destination $support }
Copy-Item -LiteralPath (Join-Path $root 'docs') -Destination (Join-Path $support 'docs') -Recurse
$plain = [IO.File]::ReadAllText((Join-Path $root 'docs\QUICKSTART.txt')).Replace("`r`n","`n").Replace("`n","`r`n")
[IO.File]::WriteAllText((Join-Path $work 'MuseDashPresence-ReadMe.txt'),$plain,[Text.UTF8Encoding]::new($true))
$files = @(Get-ChildItem -LiteralPath $work -Recurse -File | Sort-Object FullName)
$lines = $files | ForEach-Object { (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()+"`t"+[IO.Path]::GetRelativePath($work,$_.FullName).Replace('\','/') }
[IO.File]::WriteAllLines((Join-Path $work 'payload-manifest.tsv'),[string[]]$lines,[Text.UTF8Encoding]::new($false))
$archive = Join-Path $work 'payload.zip'
Add-Type -AssemblyName System.IO.Compression
$stream = [IO.File]::Create($archive)
$zip = [IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in @(Get-ChildItem -LiteralPath $work -Recurse -File | Where-Object FullName -ne $archive | Sort-Object FullName)) {
        $entry = $zip.CreateEntry([IO.Path]::GetRelativePath($work,$file.FullName).Replace('\','/'))
        $target = $entry.Open()
        try { $bytes = [IO.File]::ReadAllBytes($file.FullName); $target.Write($bytes,0,$bytes.Length) } finally { $target.Dispose() }
    }
} finally { $zip.Dispose(); $stream.Dispose() }
Write-Output $archive
