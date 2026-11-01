param([string]$GamePath)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'GameDirectory.ps1')
$game = Resolve-MuseDashDirectory -GamePath $GamePath -Pick
Assert-MuseDashClosed -GamePath $game
$files = @(
    @{ Path = (Join-Path $game 'Mods\MuseDashSteamPresence.dll'); Name = 'MuseDashSteamPresence' }
    @{ Path = (Join-Path $game 'Plugins\MuseDashSteamPresence.Bootstrap.dll'); Name = 'MuseDashSteamPresence.Bootstrap' }
)
foreach ($item in $files) {
    if (Test-Path -LiteralPath ($item.Path + '.disabled')) {
        if (Test-Path -LiteralPath $item.Path) { throw "Enabled copy already exists; no files changed: $($item.Path)" }
        if ([Reflection.AssemblyName]::GetAssemblyName($item.Path + '.disabled').Name -ne $item.Name) { throw "Unrecognized assembly: $($item.Path).disabled" }
    }
}
$done = @()
try {
    foreach ($item in $files) {
        if (Test-Path -LiteralPath ($item.Path + '.disabled')) {
            Move-Item -LiteralPath ($item.Path + '.disabled') -Destination $item.Path
            $done += $item
        }
    }
} catch {
    foreach ($item in $done) { Move-Item -LiteralPath $item.Path -Destination ($item.Path + '.disabled') }
    throw
}
Write-Output 'Both presence plugins enabled. Start Muse Dash normally from Steam.'
