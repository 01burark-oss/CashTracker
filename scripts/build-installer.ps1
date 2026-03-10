param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    [Parameter(Mandatory = $true)]
    [string]$PublishDir,
    [Parameter(Mandatory = $true)]
    [string]$OutputDir,
    [string]$SetupBaseName = "CashTracker-Setup"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$scriptPath = Join-Path $repoRoot "installer\CashTracker.iss"
$iconPath = Join-Path $repoRoot "CashTracker.App\Assets\app.ico"

if (-not (Test-Path $scriptPath)) {
    throw "Installer script not found: $scriptPath"
}

if (-not (Test-Path $PublishDir)) {
    throw "Publish directory not found: $PublishDir"
}

if (-not (Test-Path (Join-Path $PublishDir "CashTracker.exe"))) {
    throw "Expected published exe not found in $PublishDir"
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

$iscc = (Get-Command iscc.exe -ErrorAction SilentlyContinue)?.Source
if (-not $iscc) {
    $candidate = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
    if (Test-Path $candidate) {
        $iscc = $candidate
    }
}

if (-not $iscc) {
    throw "Inno Setup compiler (iscc.exe) not found."
}

$env:CASHTRACKER_APP_VERSION = $Version
$env:CASHTRACKER_PUBLISH_DIR = (Resolve-Path $PublishDir).Path
$env:CASHTRACKER_OUTPUT_DIR = (Resolve-Path $OutputDir).Path
$env:CASHTRACKER_SETUP_BASENAME = $SetupBaseName
$env:CASHTRACKER_ICON_PATH = (Resolve-Path $iconPath).Path

& $iscc $scriptPath

if ($LASTEXITCODE -ne 0) {
    throw "ISCC failed with exit code $LASTEXITCODE."
}
