param(
    [switch]$Reset
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$workspaceRoot = Split-Path -Parent $PSScriptRoot
if ($workspaceRoot.StartsWith("\\?\", [System.StringComparison]::Ordinal)) {
    $workspaceRoot = $workspaceRoot.Substring(4)
}

$appProjectPath = Join-Path $workspaceRoot "CashTracker.App\CashTracker.App.csproj"
$demoSeederProjectPath = Join-Path $workspaceRoot "CashTracker.DemoSeeder\CashTracker.DemoSeeder.csproj"
$appExePath = Join-Path $workspaceRoot "CashTracker.App\bin\Debug\net8.0-windows\CashTracker.exe"
$safeDotnetPath = Join-Path $workspaceRoot "scripts\invoke-safe-dotnet.ps1"
$restoreConfigPath = Join-Path $workspaceRoot "NuGet.Config"
$demoAppDataPath = Join-Path $workspaceRoot ".local\random-demo"
$demoRegistryPath = "Software\CashTracker\Licensing\RandomDemo"

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
if ($LASTEXITCODE -ne 0) {
    throw "CashTracker build basarisiz oldu. Cikis kodu: $LASTEXITCODE"
}

& $safeDotnetPath "restore" $demoSeederProjectPath "-p:RestoreConfigFile=$restoreConfigPath"
if ($LASTEXITCODE -ne 0) {
    throw "CashTracker.DemoSeeder restore basarisiz oldu. Cikis kodu: $LASTEXITCODE"
}

& $safeDotnetPath "build" $demoSeederProjectPath "--no-restore" "-p:RestoreConfigFile=$restoreConfigPath"
if ($LASTEXITCODE -ne 0) {
    throw "CashTracker.DemoSeeder build basarisiz oldu. Cikis kodu: $LASTEXITCODE"
}

if ($Reset -and (Test-Path $demoAppDataPath)) {
    Remove-Item $demoAppDataPath -Recurse -Force
}

if (-not (Test-Path (Join-Path $demoAppDataPath "cashtracker.db"))) {
    try {
        Remove-Item "Registry::HKEY_CURRENT_USER\$demoRegistryPath" -Recurse -Force -ErrorAction SilentlyContinue
    }
    catch {
        # Registry cleanup is best-effort for isolated demo state.
    }

    & $safeDotnetPath "run" "--project" $demoSeederProjectPath "--no-build" "--" $demoAppDataPath
    if ($LASTEXITCODE -ne 0) {
        throw "Demo veri uretimi basarisiz oldu. Cikis kodu: $LASTEXITCODE"
    }
}

if (-not (Test-Path $appExePath)) {
    throw "CashTracker.exe bulunamadi: $appExePath"
}

$startInfo = New-Object System.Diagnostics.ProcessStartInfo
$startInfo.FileName = $appExePath
$startInfo.WorkingDirectory = Split-Path -Parent $appExePath
$startInfo.UseShellExecute = $false
$startInfo.EnvironmentVariables["CASHTRACKER_APPDATA"] = $demoAppDataPath
$startInfo.EnvironmentVariables["CASHTRACKER_LICENSE_REGISTRY_PATH"] = $demoRegistryPath

[System.Diagnostics.Process]::Start($startInfo) | Out-Null

Write-Output "DEMO_APPDATA=$demoAppDataPath"
Write-Output "DEMO_PIN=1234"
