param(
    [string]$Version = "",
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "CashTracker.App\CashTracker.App.csproj"

if (-not (Test-Path $projectPath)) {
    throw "Project file not found: $projectPath"
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    [xml]$projectXml = Get-Content $projectPath
    $Version = $projectXml.Project.PropertyGroup.Version
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    throw "Version is required. Set <Version> in CashTracker.App.csproj or pass -Version."
}

$artifactsRoot = Join-Path $repoRoot "artifacts"
$versionDir = Join-Path $artifactsRoot $Version
$zipName = "CashTracker-v$Version.zip"
$zipPath = Join-Path $artifactsRoot $zipName
$shaPath = "$zipPath.sha256"

New-Item -ItemType Directory -Path $artifactsRoot -Force | Out-Null
New-Item -ItemType Directory -Path $versionDir -Force | Out-Null
Get-ChildItem -Path $versionDir -Force | Remove-Item -Recurse -Force

Write-Host "Publishing $Version for $Runtime..."
dotnet publish $projectPath `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $versionDir

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Compress-Archive -Path (Join-Path $versionDir "*") -DestinationPath $zipPath -Force

$hash = (Get-FileHash -Path $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -Path $shaPath -Value "$hash *$zipName" -NoNewline

Write-Host "Done:"
Write-Host " - Folder: $versionDir"
Write-Host " - Zip:    $zipPath"
Write-Host " - Sha256: $shaPath"
