Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$workspaceRoot = Split-Path -Parent $PSScriptRoot
if ($workspaceRoot.StartsWith("\\?\", [System.StringComparison]::Ordinal)) {
    $workspaceRoot = $workspaceRoot.Substring(4)
}

$exePath = Join-Path $workspaceRoot "CashTracker.LicenseAdmin\bin\Debug\net8.0-windows\CashTracker.LicenseAdmin.exe"
if (-not (Test-Path $exePath)) {
    throw "CashTracker License Admin bulunamadi: $exePath"
}

Start-Process -FilePath $exePath
