param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$DotnetArgs
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$workspaceRoot = Split-Path -Parent $PSScriptRoot
$correctHome = [System.Environment]::GetFolderPath("UserProfile")
$dotnetHome = Join-Path $workspaceRoot ".local\\dotnet"
$roamingAppData = Join-Path $workspaceRoot ".local\\appdata\\Roaming"
$localAppData = Join-Path $workspaceRoot ".local\\appdata\\Local"
$tempPath = Join-Path $workspaceRoot ".tmp"

New-Item -ItemType Directory -Force $dotnetHome | Out-Null
New-Item -ItemType Directory -Force $roamingAppData | Out-Null
New-Item -ItemType Directory -Force $localAppData | Out-Null
New-Item -ItemType Directory -Force $tempPath | Out-Null

$env:USERPROFILE = $correctHome
$env:DOTNET_CLI_HOME = $dotnetHome
$env:APPDATA = $roamingAppData
$env:LOCALAPPDATA = $localAppData
$env:TEMP = $tempPath
$env:TMP = $tempPath

if (-not $DotnetArgs -or $DotnetArgs.Count -eq 0) {
    throw "Kullanim: .\\scripts\\invoke-safe-dotnet.ps1 build CashTracker.sln"
}

& dotnet @DotnetArgs
$exitCode = $LASTEXITCODE
if ($exitCode -ne 0) {
    exit $exitCode
}
