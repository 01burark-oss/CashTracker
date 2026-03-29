param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "CashTracker.FabescoUpdater\CashTracker.FabescoUpdater.csproj"
$artifactRoot = Join-Path $repoRoot "artifacts\fabesco-updater"
$publishDir = Join-Path $artifactRoot "publish"
$outputExe = Join-Path $artifactRoot "CashTracker-Fabesco-Updater.exe"

New-Item -ItemType Directory -Force -Path $artifactRoot | Out-Null
if (Test-Path $publishDir) {
    Remove-Item -Recurse -Force $publishDir
}

$tempRoot = Join-Path $repoRoot ".tmp"
New-Item -ItemType Directory -Force -Path (Join-Path $repoRoot ".local\\dotnet") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $repoRoot ".local\\appdata\\Roaming") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $repoRoot ".local\\appdata\\Local") | Out-Null
New-Item -ItemType Directory -Force -Path $tempRoot | Out-Null
$env:DOTNET_CLI_HOME = (Resolve-Path (Join-Path $repoRoot ".local\\dotnet")).Path
$env:APPDATA = (Resolve-Path (Join-Path $repoRoot ".local\\appdata\\Roaming")).Path
$env:LOCALAPPDATA = (Resolve-Path (Join-Path $repoRoot ".local\\appdata\\Local")).Path
$env:TEMP = (Resolve-Path $tempRoot).Path
$env:TMP = $env:TEMP
$env:TMPDIR = $env:TEMP
$env:HOME = (Resolve-Path $repoRoot).Path

dotnet publish $projectPath `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $publishDir

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

$builtExe = Join-Path $publishDir "CashTrackerFabescoUpdater.exe"
if (-not (Test-Path $builtExe)) {
    throw "Expected updater exe not found: $builtExe"
}

Copy-Item -Force $builtExe $outputExe

$sha = (Get-FileHash -Path $outputExe -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -Path "$outputExe.sha256" -Value "$sha *$(Split-Path -Leaf $outputExe)" -NoNewline

Write-Host "Updater hazir:"
Write-Host " - EXE: $outputExe"
Write-Host " - SHA: $outputExe.sha256"
