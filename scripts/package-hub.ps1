#requires -Version 7.2
param([string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$version = [IO.File]::ReadAllText((Join-Path $root 'VERSION')).Trim()
if (!$OutputDirectory) { $OutputDirectory = Join-Path $root ('dist\'+$version+'\hub') }
if (Test-Path -LiteralPath $OutputDirectory) { throw 'Hub output exists; select a new folder.' }
$main = Join-Path $root 'artifacts\bin\MuseDashSteamPresence.dll'
$inputStream = [IO.File]::OpenRead($main)
$pe = [Reflection.PortableExecutable.PEReader]::new($inputStream)
$info = $null; $hasPayload = $false
try {
    $md = [Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($pe)
    foreach ($handle in $md.ManifestResources) {
        if ($md.GetString($md.GetManifestResource($handle).Name) -eq 'MuseDashSteamPresence.Payload.zip') { $hasPayload = $true }
    }
    foreach ($handle in $md.CustomAttributes) {
        $attribute = $md.GetCustomAttribute($handle)
        if ($attribute.Constructor.Kind -ne 'MemberReference') { continue }
        $member = $md.GetMemberReference([Reflection.Metadata.MemberReferenceHandle]$attribute.Constructor)
        if ($member.Parent.Kind -ne 'TypeReference') { continue }
        $type = $md.GetTypeReference([Reflection.Metadata.TypeReferenceHandle]$member.Parent)
        if ($md.GetString($type.Name) -ne 'MelonInfoAttribute') { continue }
        $blob = $md.GetBlobReader($attribute.Value)
        if ($blob.ReadUInt16() -ne 1) { throw 'Invalid MelonInfo attribute.' }
        [void]$blob.ReadSerializedString()
        $info = @{ name=$blob.ReadSerializedString(); version=$blob.ReadSerializedString(); author=$blob.ReadSerializedString() }
        break
    }
} finally { $pe.Dispose(); $inputStream.Dispose() }
if (!$hasPayload -or !$info -or $info.name -ne 'Muse Dash Steam Song Presence' -or $info.author -ne '樊小胃') { throw 'Unexpected Hub mod metadata or missing embedded payload. Build the current single-DLL version first.' }
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$destination = Join-Path $OutputDirectory 'MuseDashSteamPresence.dll'
Copy-Item -LiteralPath $main -Destination $destination
$hash = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText(($destination+'.sha256'),$hash+'  MuseDashSteamPresence.dll'+"`n",[Text.UTF8Encoding]::new($false))
$fields = [ordered]@{ name=$info.name; author=$info.author; filename='MuseDashSteamPresence.dll'; releaseVersion=$info.version; distributionVersion=$version; gameVersion='6.7.0'; melonLoaderVersion='0.7.3'; repository=''; configFile=''; modDependencies=@(); libDependencies=@(); incompatibleMods=@(); sha256=$hash; submissionStatus='prepared locally, not submitted or approved' }
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'hub-upload-fields.json'),($fields | ConvertTo-Json -Depth 5),[Text.UTF8Encoding]::new($false))
Copy-Item -LiteralPath (Join-Path $root 'docs\EUTERPE-HUB.md') -Destination $OutputDirectory
Write-Output $destination
Write-Output ($info.name+' / '+$info.version+' / '+$info.author)
Write-Output ('SHA256: '+$hash)
