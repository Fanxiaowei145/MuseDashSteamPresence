function Resolve-MuseDashDirectory {
    param([string]$GamePath, [switch]$Pick)
    if ($GamePath) {
        $resolved = [IO.Path]::GetFullPath($GamePath)
        if (!(Test-Path -LiteralPath (Join-Path $resolved 'musedash.exe'))) { throw 'musedash.exe was not found in the selected folder.' }
        return $resolved
    }
    # Scripts shipped inside UserData belong to that exact installation,
    # including portable/moved Steam libraries.
    $localGame = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
    if ((Split-Path $PSScriptRoot -Leaf) -eq 'MuseDashSteamPresence' -and (Test-Path -LiteralPath (Join-Path $localGame 'musedash.exe'))) { return $localGame }
    $steamPath = (Get-ItemProperty 'HKCU:\Software\Valve\Steam' -ErrorAction SilentlyContinue).SteamPath
    $libraries = @($steamPath)
    if ($steamPath) {
        $vdf = Join-Path $steamPath 'steamapps\libraryfolders.vdf'
        if (Test-Path -LiteralPath $vdf) {
            $libraries += [regex]::Matches([IO.File]::ReadAllText($vdf), '"path"\s*"([^"]+)"') | ForEach-Object { $_.Groups[1].Value.Replace('\\','\') }
        }
    }
    foreach ($library in $libraries) {
        if (!$library) { continue }
        $candidate = Join-Path $library 'steamapps\common\Muse Dash'
        if (Test-Path -LiteralPath (Join-Path $candidate 'musedash.exe')) { return [IO.Path]::GetFullPath($candidate) }
    }
    if ($Pick) {
        Add-Type -AssemblyName System.Windows.Forms
        $dialog = New-Object Windows.Forms.FolderBrowserDialog
        try {
            $dialog.Description = 'Select the Muse Dash folder containing musedash.exe'
            if ($dialog.ShowDialog() -eq 'OK') { return Resolve-MuseDashDirectory -GamePath $dialog.SelectedPath }
        } finally { $dialog.Dispose() }
        throw 'Folder selection cancelled; no files changed.'
    }
    throw 'Muse Dash was not found. Pass -GamePath with the game folder.'
}

function Assert-MuseDashClosed {
    param([string]$GamePath)
    $expectedHelper = if ($GamePath) { Join-Path $GamePath 'UserData\MuseDashSteamPresence\PresenceController.exe' } else { '' }
    foreach ($process in @(Get-Process musedash,PresenceController -ErrorAction SilentlyContinue)) {
        $blocked = !$GamePath
        try {
            $path = $process.Path
            if (!$path) { $blocked = $true }
            elseif ($process.ProcessName -ieq 'musedash') { $blocked = !$GamePath -or ([IO.Path]::GetDirectoryName($path) -ieq $GamePath.TrimEnd('\','/')) }
            else { $blocked = !$GamePath -or ($path -ieq $expectedHelper) }
        } catch { $blocked = $true }
        if ($blocked) { throw 'Close this Muse Dash installation and wait for its presence controller to exit first.' }
    }
}
