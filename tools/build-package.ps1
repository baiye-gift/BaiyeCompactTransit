param([string]$GameManaged = 'D:\Steam\steamapps\common\OxygenNotIncluded\OxygenNotIncluded_Data\Managed')
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$versionMatch = [regex]::Match([IO.File]::ReadAllText((Join-Path $root 'mod_info.yaml')), '(?m)^version:\s*([0-9]+\.[0-9]+\.[0-9]+)\s*$')
if (-not $versionMatch.Success) { throw 'Missing semantic package version in mod_info.yaml.' }
$version = $versionMatch.Groups[1].Value
$dist = Join-Path $root 'dist'
$stage = Join-Path $dist 'package/BaiyeCompactTransit'
$zip = Join-Path $dist "BaiyeCompactTransit-$version.zip"
& dotnet build (Join-Path $root 'CompactTransit.csproj') -c Release "-p:GameManaged=$GameManaged" -v:q -clp:ErrorsOnly
if ($LASTEXITCODE -ne 0) { throw 'Release compilation failed.' }
# Delete only our fixed staging folder, after verifying its absolute boundary.
$resolvedStage = [IO.Path]::GetFullPath($stage)
if (-not $resolvedStage.StartsWith($root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe staging directory.' }
if (Test-Path -LiteralPath $resolvedStage) { Remove-Item -LiteralPath $resolvedStage -Recurse -Force }
New-Item -ItemType Directory -Force $stage | Out-Null
Copy-Item -LiteralPath (Join-Path $root 'bin/Release/netstandard2.1/BaiyeCompactTransit.dll') -Destination $stage
foreach ($name in @('mod.yaml','mod_info.yaml','README.md')) { Copy-Item -LiteralPath (Join-Path $root $name) -Destination $stage }
Copy-Item -LiteralPath (Join-Path $root 'anim') -Destination $stage -Recurse
Copy-Item -LiteralPath (Join-Path $root 'docs/playtest.md') -Destination (Join-Path $stage 'PLAYTEST.md')
Copy-Item -LiteralPath (Join-Path $root 'docs/connection-layout.png') -Destination (Join-Path $stage 'CONNECTIONS.png')
Copy-Item -LiteralPath (Join-Path $root 'CHANGELOG.md') -Destination $stage
$dlls = @(Get-ChildItem -LiteralPath $stage -Recurse -Filter '*.dll')
if ($dlls.Count -ne 1 -or $dlls[0].Name -ne 'BaiyeCompactTransit.dll') { throw 'Unexpected dependency DLL in package.' }
if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }
Compress-Archive -LiteralPath $stage -DestinationPath $zip
$hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText($zip + '.sha256', "$hash  $([IO.Path]::GetFileName($zip))`n", [Text.UTF8Encoding]::new($false))
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($zip)
try {
    $required = @('BaiyeCompactTransit.dll','mod.yaml','mod_info.yaml','README.md','PLAYTEST.md','CONNECTIONS.png','CHANGELOG.md',
        'anim/assets/baiye_compact_transit/baiye_compact_transit_anim.bytes',
        'anim/assets/baiye_compact_transit/baiye_compact_transit_build.bytes')
    $entries = @($archive.Entries | ForEach-Object { $_.FullName.Replace('\','/') })
    foreach ($file in $required) { if ($entries -notcontains "BaiyeCompactTransit/$file") { throw "ZIP missing $file" } }
    foreach ($entry in $archive.Entries) {
        if ($entry.FullName.EndsWith('/')) { continue }
        $relative = $entry.FullName.Replace('\','/').Substring('BaiyeCompactTransit/'.Length)
        $diskHash = (Get-FileHash -LiteralPath (Join-Path $stage $relative) -Algorithm SHA256).Hash
        $stream = $entry.Open()
        $sha = [Security.Cryptography.SHA256]::Create()
        try { $zipHash = [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-','') }
        finally { $stream.Dispose(); $sha.Dispose() }
        if ($diskHash -ne $zipHash) { throw "ZIP checksum mismatch: $relative" }
    }
    Write-Output "PASS: package contents and every ZIP entry SHA-256 match ($($archive.Entries.Count) entries)."
} finally { $archive.Dispose() }
Write-Output "Package: $zip"
Write-Output "SHA256: $hash"
