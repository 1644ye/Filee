# Assemble a self-contained Windows distribution from a verified publish/base directory.
# The explicit allowlist also supports a verified installed 2.1.0 runtime as an offline build baseline.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$BaseDir,
    [Parameter(Mandatory)][string]$AppBuildDir,
    [Parameter(Mandatory)][string]$CoreBuildDir,
    [Parameter(Mandatory)][string]$EnginesBuildDir,
    [string]$Version = '2.1.0-svg.1',
    [string]$OutputDir = (Join-Path $PSScriptRoot '../Releases')
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
if ($Version -notmatch '^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$') { throw 'Invalid version.' }
$BaseDir = (Resolve-Path -LiteralPath $BaseDir).Path
foreach ($name in 'Filee.exe', 'coreclr.dll', 'hostfxr.dll', 'Filee.deps.json', 'Filee.runtimeconfig.json', 'vcruntime140.dll') {
    if (-not (Test-Path -LiteralPath (Join-Path $BaseDir $name))) { throw "Self-contained runtime missing: $name" }
}
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
$OutputDir = (Resolve-Path -LiteralPath $OutputDir).Path
$name = "Filee-$Version-win-x64-with-extension"
$stage = Join-Path $OutputDir $name
$archive = "$stage.zip"
if ((Test-Path -LiteralPath $stage) -or (Test-Path -LiteralPath $archive)) { throw "Output already exists: $name" }
New-Item -ItemType Directory -Path $stage | Out-Null
Get-ChildItem -LiteralPath $BaseDir -File | Where-Object {
    $_.Extension -in '.dll', '.exe', '.msix' -and $_.Name -notmatch '^(uninstall|unins\d+)\.exe$' -or
    $_.Name -in 'Filee.deps.json', 'Filee.runtimeconfig.json', 'filee-cli.deps.json', 'filee-cli.runtimeconfig.json'
} | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $stage }
foreach ($directory in 'cli', 'da', 'de', 'es', 'fr', 'nl', 'pt', 'pt-BR', 'zh-CN', 'zh-TW') {
    $source = Join-Path $BaseDir $directory
    if (Test-Path -LiteralPath $source) { Copy-Item -LiteralPath $source -Destination $stage -Recurse }
}
New-Item -ItemType Directory -Path (Join-Path $stage 'engines') | Out-Null
foreach ($engine in '7zip', 'rhwp') {
    Copy-Item -LiteralPath (Join-Path $BaseDir "engines/$engine") -Destination (Join-Path $stage 'engines') -Recurse
}
foreach ($entry in @(@($AppBuildDir, 'Filee'), @($CoreBuildDir, 'Filee.Core'), @($EnginesBuildDir, 'Filee.Engines'))) {
    Copy-Item -LiteralPath (Join-Path $entry[0] ($entry[1] + '.dll')) -Destination $stage -Force
}
Copy-Item -LiteralPath (Join-Path $root 'browser-extension') -Destination $stage -Recurse
foreach ($notice in 'LICENSE', 'THIRD-PARTY-NOTICES.md') {
    Copy-Item -LiteralPath (Join-Path $root $notice) -Destination $stage
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'svg-release/START-HERE.html') -Destination $stage
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'svg-release/Install-Chrome-Extension.cmd') -Destination $stage
$revision = (& git -C $root rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Cannot resolve source revision.' }
@{
    version = $Version; repository = 'https://github.com/1644ye/Filee'; sourceCommit = $revision
    runtime = 'win-x64'; extensionVersion = (Get-Content (Join-Path $stage 'browser-extension/manifest.json') -Raw | ConvertFrom-Json).version
    build = 'App/Core/Engines built from source; matching verified 2.1.0 self-contained runtime and third-party libraries reused.'
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $stage 'BUILD-INFO.json') -Encoding UTF8
$manifest = Get-ChildItem -LiteralPath $stage -Recurse -File | ForEach-Object {
    $relative = [IO.Path]::GetRelativePath($stage, $_.FullName).Replace('\', '/')
    '{0}  {1}' -f (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant(), $relative
}
$manifest | Set-Content -LiteralPath (Join-Path $stage 'FILES-SHA256SUMS.txt') -Encoding ascii
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($stage, $archive, [IO.Compression.CompressionLevel]::Optimal, $true)
$extensionZip = Join-Path $OutputDir "Filee-SVG-Drag-$Version-Chrome.zip"
[IO.Compression.ZipFile]::CreateFromDirectory((Join-Path $stage 'browser-extension'), $extensionZip)
@($archive, $extensionZip) | ForEach-Object {
    '{0}  {1}' -f (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant(), (Split-Path $_ -Leaf)
} | Set-Content -LiteralPath (Join-Path $OutputDir "Filee-$Version-SHA256SUMS.txt") -Encoding ascii
Get-Item -LiteralPath $archive, $extensionZip | Select-Object Name, Length
