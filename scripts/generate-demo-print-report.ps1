param()

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot

powershell -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "invoke-safe-dotnet.ps1") run --project (Join-Path $repoRoot "CashTracker.PrintDemoGenerator\CashTracker.PrintDemoGenerator.csproj")
