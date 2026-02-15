param(
    [string]$Version = "",
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release",
    [string]$AssetName = "CashTracker.exe"
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

$assetBaseName = [System.IO.Path]::GetFileNameWithoutExtension($AssetName)
if ([string]::IsNullOrWhiteSpace($assetBaseName)) {
    throw "AssetName must include a valid file name, for example 'CashTracker.exe'."
}

$artifactsRoot = Join-Path $repoRoot "artifacts"
$versionDir = Join-Path $artifactsRoot $Version
$publishDir = Join-Path $versionDir "publish"
$publishedExePath = Join-Path $publishDir $AssetName
$releaseExePath = Join-Path $versionDir $AssetName
$shaPath = "$releaseExePath.sha256"

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
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $publishDir

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

if (-not (Test-Path $publishDir)) {
    throw "Publish output directory not found: $publishDir"
}

if (-not (Test-Path $publishedExePath)) {
    $defaultPublishedExe = Join-Path $publishDir "CashTracker.App.exe"
    $fallbackExe = if (Test-Path $defaultPublishedExe) {
        Get-Item -LiteralPath $defaultPublishedExe
    }
    else {
        Get-ChildItem -Path $publishDir -File -Filter "*.exe" | Select-Object -First 1
    }
    if ($null -eq $fallbackExe) {
        throw "Publish output does not contain an .exe file. Check publish settings."
    }

    $publishedExePath = $fallbackExe.FullName
}

if (Test-Path $releaseExePath) {
    Remove-Item $releaseExePath -Force
}

Copy-Item -Path $publishedExePath -Destination $releaseExePath -Force

$hash = (Get-FileHash -Path $releaseExePath -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -Path $shaPath -Value "$hash *$AssetName" -NoNewline

if (Test-Path $publishDir) {
    Remove-Item -Path $publishDir -Recurse -Force
}

Write-Host "Done:"
Write-Host " - Folder: $versionDir"
Write-Host " - Exe:    $releaseExePath"
Write-Host " - Sha256: $shaPath"
