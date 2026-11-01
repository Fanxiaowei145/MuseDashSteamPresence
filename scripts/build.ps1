#requires -Version 7.2
param([string]$GamePath, [string]$CompilerDirectory, [string]$RuntimeDirectory, [switch]$TestsOnly)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$bin = Join-Path $root 'artifacts\bin'
$testBin = Join-Path $root 'artifacts\tests'
New-Item -ItemType Directory -Force -Path $bin,$testBin | Out-Null
$shared = Join-Path $root 'src\Shared'
$controller = Join-Path $root 'src\Controller'
$settings = Join-Path $root 'src\Settings'
$tests = Join-Path $root 'tests'
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $csc)) { throw 'Windows x64 with .NET Framework is required.' }
function Compile-Desktop {
    param([string]$Name, [string[]]$Sources, [switch]$Gui, [string]$MainClass)
    $output = if ($Gui) { Join-Path $bin ($Name + '.exe') } else { Join-Path $testBin ($Name + '.exe') }
    $options = @('/nologo', '/platform:x64', "/out:$output")
    $options += if ($Gui) { '/target:winexe' } else { '/target:exe' }
    if ($Name -eq 'MuseDashPresenceSettings' -or $Name -eq 'SettingsEnvironmentTests') { $options += '/reference:System.Windows.Forms.dll','/reference:System.Drawing.dll' }
    if ($Name -eq 'InstallationTests' -or $Name -eq 'SettingsEnvironmentTests') { $options += '/reference:Microsoft.CSharp.dll' }
    if ($Name -eq 'PayloadDeploymentTests') { $options += '/reference:System.IO.Compression.dll' }
    if ($MainClass) { $options += "/main:$MainClass" }
    & $csc @options @Sources
    if ($LASTEXITCODE -ne 0) { throw "Build failed: $Name" }
    if (!$Gui) {
        & $output
        if ($LASTEXITCODE -ne 0) { throw "Tests failed: $Name" }
    }
}
$policy = Join-Path $shared 'PublicationPolicy.cs'
$display = Join-Path $shared 'SongDisplay.cs'
$config = Join-Path $shared 'PresenceSettings.cs'
$steamBuild = Join-Path $shared 'SteamBuild.cs'
$installation = Join-Path $shared 'PluginInstallation.cs'
$deployment = Join-Path $shared 'PayloadDeployment.cs'
$packet = Join-Path $controller 'GamePresencePacket.cs'
$loop = Join-Path $controller 'PresenceController.cs'
Compile-Desktop 'PolicyTests' @($policy, (Join-Path $tests 'PolicyTests.cs'))
Compile-Desktop 'PacketTests' @($packet, (Join-Path $tests 'PacketTests.cs'))
Compile-Desktop 'SongDisplayTests' @($policy,$display,$config,(Join-Path $tests 'SongDisplayTests.cs'))
# This links the real loop to a fake transport, never to SteamNative.cs.
Compile-Desktop 'ControllerRegressionTests' @($policy,$display,$config,$packet,$loop,(Join-Path $tests 'ControllerRegressionTests.cs')) -MainClass 'ControllerRegressionTests'
Compile-Desktop 'PresenceController' @($policy,$display,$config,$steamBuild,$packet,$loop,(Join-Path $controller 'SteamNative.cs')) -Gui
Compile-Desktop 'MuseDashPresenceSettings' @($policy,$display,$config,$steamBuild,$installation,(Join-Path $settings 'GameLocation.cs'),(Join-Path $settings 'SettingsApp.cs')) -Gui
Compile-Desktop 'InstallationTests' @($installation,(Join-Path $tests 'InstallationFixtures.cs'),(Join-Path $tests 'InstallationTests.cs'))
Compile-Desktop 'SettingsEnvironmentTests' @($policy,$display,$config,$installation,(Join-Path $settings 'GameLocation.cs'),(Join-Path $settings 'SettingsApp.cs'),(Join-Path $tests 'InstallationFixtures.cs'),(Join-Path $tests 'SettingsEnvironmentTests.cs')) -MainClass 'SettingsEnvironmentTests'
Compile-Desktop 'PayloadDeploymentTests' @($deployment,(Join-Path $tests 'PayloadDeploymentTests.cs'))
if ($TestsOnly) { Write-Output 'Offline checks passed; native Steam code was compiled but never executed. Game plugins were not built.'; return }

. (Join-Path $PSScriptRoot 'GameDirectory.ps1')
$game = Resolve-MuseDashDirectory -GamePath $GamePath
if (!$RuntimeDirectory) {
    $runtimeRoot = Join-Path $game 'dotnet\shared\Microsoft.NETCore.App'
    if (Test-Path -LiteralPath $runtimeRoot) {
        $runtime = Get-ChildItem -LiteralPath $runtimeRoot -Directory | Where-Object Name -like '6.0.*' | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
        if ($runtime) { $RuntimeDirectory = $runtime.FullName }
    }
    if (!$RuntimeDirectory) {
        $runtimeRoot = Join-Path $env:ProgramFiles 'dotnet\shared\Microsoft.NETCore.App'
        if (Test-Path -LiteralPath $runtimeRoot) {
            $runtime = Get-ChildItem -LiteralPath $runtimeRoot -Directory | Where-Object Name -like '6.0.*' | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
            if ($runtime) { $RuntimeDirectory = $runtime.FullName }
        }
    }
}
if (!$RuntimeDirectory -or !(Test-Path -LiteralPath (Join-Path $RuntimeDirectory 'System.Private.CoreLib.dll'))) { throw 'A .NET 6 runtime reference folder is required. Pass -RuntimeDirectory.' }
if (!$CompilerDirectory) {
    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($dotnet) {
        foreach ($line in @(& $dotnet.Source --list-sdks)) {
            if ($line -match '^(\S+)\s+\[(.+)\]$') { $CompilerDirectory = Join-Path (Join-Path $Matches[2] $Matches[1]) 'Roslyn\bincore' }
        }
    }
}
if (!$CompilerDirectory -or !(Test-Path -LiteralPath (Join-Path $CompilerDirectory 'Microsoft.CodeAnalysis.CSharp.dll'))) { throw 'Install a .NET SDK (8 or later), or pass -CompilerDirectory containing the Roslyn assemblies.' }
[void][Reflection.Assembly]::LoadFrom((Join-Path $CompilerDirectory 'Microsoft.CodeAnalysis.dll'))
[void][Reflection.Assembly]::LoadFrom((Join-Path $CompilerDirectory 'Microsoft.CodeAnalysis.CSharp.dll'))
$references = [Collections.Generic.List[Microsoft.CodeAnalysis.MetadataReference]]::new()
$referenceFiles = @(
    Get-ChildItem -LiteralPath $RuntimeDirectory -Filter '*.dll' | ForEach-Object FullName
    (Join-Path $game 'MelonLoader\net6\MelonLoader.dll')
    (Join-Path $game 'MelonLoader\net6\0Harmony.dll')
    (Join-Path $game 'MelonLoader\net6\Il2CppInterop.Runtime.dll')
    (Join-Path $game 'MelonLoader\Il2CppAssemblies\Assembly-CSharp.dll')
    (Join-Path $game 'MelonLoader\Il2CppAssemblies\Il2Cppmscorlib.dll')
    (Join-Path $game 'MelonLoader\Il2CppAssemblies\UnityEngine.CoreModule.dll')
)
foreach ($file in $referenceFiles) {
    if (!(Test-Path -LiteralPath $file)) { throw "Reference missing: $file. Start the game once with MelonLoader to generate interop assemblies." }
    try { [void][Reflection.AssemblyName]::GetAssemblyName($file) } catch { continue }
    $references.Add([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile($file))
}
function Compile-Plugin {
    param([string]$Name, [string[]]$Sources, [string]$PayloadPath, [switch]$Console)
    $trees = [Collections.Generic.List[Microsoft.CodeAnalysis.SyntaxTree]]::new()
    foreach ($file in $Sources) { $trees.Add([Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText([IO.File]::ReadAllText($file))) }
    $kind = if ($Console) { [Microsoft.CodeAnalysis.OutputKind]::ConsoleApplication } else { [Microsoft.CodeAnalysis.OutputKind]::DynamicallyLinkedLibrary }
    $options = [Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions]::new($kind)
    $compilation = [Microsoft.CodeAnalysis.CSharp.CSharpCompilation]::Create($Name,$trees,$references,$options)
    $stream = [IO.MemoryStream]::new()
    try {
        if ($PayloadPath) {
            $payloadBytes = [IO.File]::ReadAllBytes($PayloadPath)
            $factory = { [IO.MemoryStream]::new($payloadBytes,$false) }.GetNewClosure()
            $provider = [Func[IO.Stream]]$factory
            $resources = [Collections.Generic.List[Microsoft.CodeAnalysis.ResourceDescription]]::new()
            $resources.Add([Microsoft.CodeAnalysis.ResourceDescription]::new('MuseDashSteamPresence.Payload.zip',$provider,$true))
            $result = $compilation.Emit($stream,$null,$null,$null,$resources)
        } else { $result = $compilation.Emit($stream) }
        if (!$result.Success) { $result.Diagnostics | Where-Object Severity -eq Error | ForEach-Object ToString; throw "Build failed: $Name" }
        [IO.File]::WriteAllBytes((Join-Path $bin ($Name + '.dll')), $stream.ToArray())
    } finally { $stream.Dispose() }
}
$sessionFiles = Join-Path $shared 'SessionFiles.cs'
$reader = Join-Path $shared 'PayloadArchiveReader.cs'
$runtime = Join-Path $shared 'PayloadRuntime.cs'
Compile-Plugin 'MuseDashSteamPresence.Bootstrap' @($sessionFiles,$installation,$deployment,$reader,$runtime,(Join-Path $root 'src\Bootstrap\Bootstrap.cs'))
$payloadPath = & (Join-Path $PSScriptRoot 'build-payload.ps1') -BinDirectory $bin
Compile-Plugin 'MuseDashSteamPresence' @($policy,$sessionFiles,$deployment,$reader,$runtime,(Join-Path $root 'src\Plugin\Mod.cs'),(Join-Path $root 'src\Plugin\EuterpeSongResolver.cs')) -PayloadPath $payloadPath
Compile-Plugin 'HubDeploymentProbe' @($deployment,$reader,$runtime,$sessionFiles,(Join-Path $tests 'HubDeploymentProbe.cs')) -Console
& $csc /nologo /target:exe /platform:x64 "/out:$testBin\FakeOldController.exe" (Join-Path $tests 'FakeOldController.cs')
if ($LASTEXITCODE -ne 0) { throw 'Own-controller test stub build failed.' }
$runtimeConfig = '{"runtimeOptions":{"tfm":"net6.0","rollForward":"Major","framework":{"name":"Microsoft.NETCore.App","version":"6.0.0"}}}'
[IO.File]::WriteAllText((Join-Path $bin 'HubDeploymentProbe.runtimeconfig.json'),$runtimeConfig)
$hostPath = Join-Path $game 'dotnet\dotnet.exe'
if (!(Test-Path -LiteralPath $hostPath)) { $hostPath = (Get-Command dotnet -ErrorAction Stop).Source }
$probeFixture = Join-Path $root ('.local\hub-deployment-' + [guid]::NewGuid().ToString('N'))
& $hostPath (Join-Path $bin 'HubDeploymentProbe.dll') (Join-Path $bin 'MuseDashSteamPresence.dll') $probeFixture (Join-Path $testBin 'FakeOldController.exe')
if ($LASTEXITCODE -ne 0) { throw 'Actual Hub-DLL deployment probe failed.' }
Write-Output "Built all four components: $bin"
