param(
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release",
    [string]$OutputBaseName = "CashTracker-Fabesco-Updater"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
if ($repoRoot.StartsWith("\\?\", [System.StringComparison]::Ordinal)) {
    $repoRoot = $repoRoot.Substring(4)
}

$projectPath = Join-Path $repoRoot "CashTracker.CustomerUpdater\CashTracker.CustomerUpdater.csproj"
$appProjectPath = Join-Path $repoRoot "CashTracker.App\CashTracker.App.csproj"
$safeDotnetPath = Join-Path $repoRoot "scripts\invoke-safe-dotnet.ps1"

if (-not (Test-Path $projectPath)) {
    throw "Project file not found: $projectPath"
}

if (-not (Test-Path $appProjectPath)) {
    throw "App project file not found: $appProjectPath"
}

[xml]$appProjectXml = Get-Content $appProjectPath
$appVersionNode = @($appProjectXml.Project.PropertyGroup | Where-Object {
    $_.PSObject.Properties.Name -contains "Version"
})[0]
$appVersion = if ($null -ne $appVersionNode) { $appVersionNode.Version } else { "" }
if ([string]::IsNullOrWhiteSpace($appVersion)) {
    throw "App version could not be read from CashTracker.App.csproj"
}

$artifactsRoot = Join-Path $repoRoot "artifacts"
$versionRoot = Join-Path $artifactsRoot $appVersion
$publishDir = Join-Path $versionRoot "customer-updater-publish"
$outputExePath = Join-Path $versionRoot ($OutputBaseName + ".exe")

if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $versionRoot | Out-Null

& $safeDotnetPath "publish" $projectPath "-c" $Configuration "-r" $Runtime "--self-contained" "true" "-p:PublishSingleFile=true" "-p:EnableCompressionInSingleFile=false" "-p:IncludeNativeLibrariesForSelfExtract=false" "-o" $publishDir "-p:RestoreConfigFile=$(Join-Path $repoRoot 'NuGet.Config')"
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

$publishedExe = Join-Path $publishDir "CashTrackerFabescoUpdater.exe"
if (-not (Test-Path $publishedExe)) {
    throw "Published updater exe not found: $publishedExe"
}

Copy-Item $publishedExe $outputExePath -Force

$sha = (Get-FileHash -Path $outputExePath -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -Path ($outputExePath + ".sha256") -Value "$sha *$(Split-Path -Leaf $outputExePath)" -NoNewline

Write-Host "Done:"
Write-Host " - PublishDir: $publishDir"
Write-Host " - UpdaterExe: $outputExePath"
