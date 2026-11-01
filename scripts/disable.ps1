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
    if (Test-Path -LiteralPath $item.Path) {
        if ([Reflection.AssemblyName]::GetAssemblyName($item.Path).Name -ne $item.Name) { throw "Unrecognized assembly: $($item.Path)" }
        if (Test-Path -LiteralPath ($item.Path + '.disabled')) { throw "Disabled copy already exists; no files changed: $($item.Path)" }
    }
}
$done = @()
try {
    foreach ($item in $files) {
        if (Test-Path -LiteralPath $item.Path) {
            Move-Item -LiteralPath $item.Path -Destination ($item.Path + '.disabled')
            $done += $item
        }
    }
} catch {
    foreach ($item in $done) { Move-Item -LiteralPath ($item.Path + '.disabled') -Destination $item.Path }
    throw
}
Write-Output 'Both presence plugins disabled. Settings preserved. Steam and game binaries were not changed.'
