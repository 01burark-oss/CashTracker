Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$workspaceRoot = Split-Path -Parent $PSScriptRoot
if ($workspaceRoot.StartsWith("\\?\", [System.StringComparison]::Ordinal)) {
    $workspaceRoot = $workspaceRoot.Substring(4)
}

$appProjectPath = Join-Path $workspaceRoot "CashTracker.App\CashTracker.App.csproj"
$appExePath = Join-Path $workspaceRoot "CashTracker.App\bin\Debug\net8.0-windows\CashTracker.exe"
$safeDotnetPath = Join-Path $workspaceRoot "scripts\invoke-safe-dotnet.ps1"
$restoreConfigPath = Join-Path $workspaceRoot "NuGet.Config"
$testRootPath = Join-Path $workspaceRoot ".local\trial-lock-test"
$testRegistryPath = "Software\CashTracker\Licensing\TrialLockTest"
$runtimeStatePath = Join-Path $testRootPath "license-runtime.json"

function Stop-RunningAppInstances {
    param([string]$ExePath)

    $normalizedTarget = [System.IO.Path]::GetFullPath($ExePath)
    $processes = Get-CimInstance Win32_Process -Filter "Name = 'CashTracker.exe'"
    foreach ($process in $processes) {
        $candidatePath = $process.ExecutablePath
        if ([string]::IsNullOrWhiteSpace($candidatePath)) {
            continue
        }

        try {
            $normalizedCandidate = [System.IO.Path]::GetFullPath($candidatePath)
        }
        catch {
            continue
        }

        if ($normalizedCandidate -ieq $normalizedTarget) {
            Stop-Process -Id $process.ProcessId -ErrorAction SilentlyContinue
            Wait-Process -Id $process.ProcessId -Timeout 5 -ErrorAction SilentlyContinue
        }
    }
}

Stop-RunningAppInstances -ExePath $appExePath

& $safeDotnetPath "build" $appProjectPath "--no-restore" "-p:RestoreConfigFile=$restoreConfigPath"
$buildExitCode = $LASTEXITCODE
if ($buildExitCode -ne 0) {
    throw "CashTracker build basarisiz oldu. Cikis kodu: $buildExitCode"
}

if (Test-Path $testRootPath) {
    Remove-Item $testRootPath -Recurse -Force
}

New-Item -ItemType Directory -Path $testRootPath -Force | Out-Null

try {
    Remove-Item "Registry::HKEY_CURRENT_USER\$testRegistryPath" -Recurse -Force -ErrorAction SilentlyContinue
}
catch {
    # Registry cleanup is best-effort for test isolation.
}

$nowUtc = [DateTime]::UtcNow
$expiredRuntimeState = [ordered]@{
    InstallCode = ""
    TrialStartedAtUtc = $nowUtc.AddDays(-16).ToString("o")
    LastSeenAtUtc = $nowUtc.ToString("o")
    LegacyExempt = $false
    TamperLocked = $false
    ActivatedAtUtc = $null
    ActivatedLicenseId = ""
    UpdatedAtUtc = $nowUtc.AddMinutes(5).ToString("o")
}

$runtimeJson = $expiredRuntimeState | ConvertTo-Json
Set-Content -Path $runtimeStatePath -Value $runtimeJson -Encoding UTF8

if (-not (Test-Path $appExePath)) {
    throw "CashTracker.exe bulunamadi: $appExePath"
}

$startInfo = New-Object System.Diagnostics.ProcessStartInfo
$startInfo.FileName = $appExePath
$startInfo.WorkingDirectory = Split-Path -Parent $appExePath
$startInfo.UseShellExecute = $false
$startInfo.EnvironmentVariables["CASHTRACKER_APPDATA"] = $testRootPath
$startInfo.EnvironmentVariables["CASHTRACKER_LICENSE_REGISTRY_PATH"] = $testRegistryPath

[System.Diagnostics.Process]::Start($startInfo) | Out-Null

Write-Output "TEST_APPDATA=$testRootPath"
Write-Output "TEST_REGISTRY=HKEY_CURRENT_USER\\$testRegistryPath"
