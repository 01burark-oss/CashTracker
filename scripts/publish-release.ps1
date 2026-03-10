param(
    [string]$Version = "",
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release",
    [string]$SetupBaseName = "CashTracker-Setup"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "CashTracker.App\CashTracker.App.csproj"
$buildInstallerScript = Join-Path $PSScriptRoot "build-installer.ps1"

if (-not (Test-Path $projectPath)) {
    throw "Project file not found: $projectPath"
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    [xml]$projectXml = Get-Content $projectPath
    $versionNode = @($projectXml.Project.PropertyGroup | Where-Object {
        $_.PSObject.Properties.Name -contains "Version"
    })[0]
    if ($null -ne $versionNode) {
        $Version = $versionNode.Version
    }
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    throw "Version is required. Set <Version> in CashTracker.App.csproj or pass -Version."
}

$artifactsRoot = Join-Path $repoRoot "artifacts"
$versionDir = Join-Path $artifactsRoot $Version
$publishDir = Join-Path $versionDir "publish"
$installerPath = Join-Path $versionDir "$SetupBaseName.exe"

New-Item -ItemType Directory -Path $artifactsRoot -Force | Out-Null
New-Item -ItemType Directory -Path $versionDir -Force | Out-Null
Get-ChildItem -Path $versionDir -Force | Remove-Item -Recurse -Force

Write-Host "Publishing $Version for $Runtime..."
dotnet publish $projectPath `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $publishDir

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

if (-not (Test-Path (Join-Path $publishDir "CashTracker.exe"))) {
    throw "Expected published file not found: $(Join-Path $publishDir 'CashTracker.exe')"
}

& $buildInstallerScript `
    -Version $Version `
    -PublishDir $publishDir `
    -OutputDir $versionDir `
    -SetupBaseName $SetupBaseName

if (-not (Test-Path $installerPath)) {
    throw "Installer was not created: $installerPath"
}

$installerSha = (Get-FileHash -Path $installerPath -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -Path "$installerPath.sha256" -Value "$installerSha *$(Split-Path -Leaf $installerPath)" -NoNewline

Write-Host "Done:"
Write-Host " - Folder:     $versionDir"
Write-Host " - PublishDir: $publishDir"
Write-Host " - Installer:  $installerPath"
