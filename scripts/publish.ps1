$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$dotnet = "dotnet"
$localDotnet = "C:\tmp\dotnet\dotnet.exe"

if (Test-Path $localDotnet) {
    $dotnet = $localDotnet
}

$env:DOTNET_CLI_HOME = Join-Path $repoRoot ".dotnet-home"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"

& $dotnet publish `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true

$publishDir = Join-Path $repoRoot "bin\Release\net8.0-windows\win-x64\publish"
$releaseDir = Join-Path $repoRoot "Release"
$starterDest = Join-Path $releaseDir "Projects\StarterKit"

New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $starterDest "src") | Out-Null

Copy-Item -LiteralPath (Join-Path $publishDir "CircuitForge.exe") -Destination (Join-Path $releaseDir "CircuitForge.exe") -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "Assets\AppIcon.ico") -Destination (Join-Path $releaseDir "AppIcon.ico") -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "Assets\AppIcon.png") -Destination (Join-Path $releaseDir "AppIcon.png") -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "StarterKit\platformio.ini") -Destination (Join-Path $starterDest "platformio.ini") -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "StarterKit\learning-notes.md") -Destination (Join-Path $starterDest "learning-notes.md") -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "StarterKit\src\main.cpp") -Destination (Join-Path $starterDest "src\main.cpp") -Force

$pdb = Join-Path $publishDir "CircuitForge.pdb"
if (Test-Path $pdb) {
    Remove-Item -LiteralPath $pdb
}

Write-Host "Published app:"
Write-Host (Join-Path $releaseDir "CircuitForge.exe")
