Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$workspaceRoot = Split-Path -Parent $PSScriptRoot
if ($workspaceRoot.StartsWith("\\?\", [System.StringComparison]::Ordinal)) {
    $workspaceRoot = $workspaceRoot.Substring(4)
}

$appProjectPath = Join-Path $workspaceRoot "CashTracker.App\CashTracker.App.csproj"
$appExePath = Join-Path $workspaceRoot "CashTracker.App\bin\Debug\net8.0-windows\CashTracker.exe"
$restoreConfigPath = Join-Path $workspaceRoot "NuGet.Config"
$safeDotnetPath = Join-Path $workspaceRoot "scripts\invoke-safe-dotnet.ps1"

if (-not (Test-Path $appProjectPath)) {
    throw "CashTracker.App.csproj bulunamadi: $appProjectPath"
}

if (-not (Test-Path $safeDotnetPath)) {
    throw "invoke-safe-dotnet.ps1 bulunamadi: $safeDotnetPath"
}

function Get-LatestSourceWriteTimeUtc {
    param([string]$RootPath)

    $extensions = @(".cs", ".csproj", ".resx", ".json", ".config", ".settings")
    $folders = @(
        "CashTracker.App",
        "CashTracker.Core",
        "CashTracker.Infrastructure"
    )

    $latestUtc = [datetime]::MinValue
    foreach ($folder in $folders) {
        $folderPath = Join-Path $RootPath $folder
        if (-not (Test-Path $folderPath)) {
            continue
        }

        $candidates = Get-ChildItem -Path $folderPath -Recurse -File | Where-Object {
            $extensions -contains $_.Extension
        }

        foreach ($candidate in $candidates) {
            if ($candidate.LastWriteTimeUtc -gt $latestUtc) {
                $latestUtc = $candidate.LastWriteTimeUtc
            }
        }
    }

    return $latestUtc
}

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

$shouldBuild = -not (Test-Path $appExePath)
if (-not $shouldBuild) {
    $latestSourceUtc = Get-LatestSourceWriteTimeUtc -RootPath $workspaceRoot
    $exeWriteTimeUtc = (Get-Item $appExePath).LastWriteTimeUtc
    $shouldBuild = $latestSourceUtc -gt $exeWriteTimeUtc
}

if ($shouldBuild) {
    Stop-RunningAppInstances -ExePath $appExePath

    & $safeDotnetPath "build" $appProjectPath "--no-restore" "-p:RestoreConfigFile=$restoreConfigPath"
    $buildExitCode = $LASTEXITCODE
    if ($buildExitCode -ne 0) {
        throw "CashTracker build basarisiz oldu. Cikis kodu: $buildExitCode"
    }
}

if (-not (Test-Path $appExePath)) {
    throw "CashTracker.exe bulunamadi: $appExePath"
}

Start-Process -FilePath $appExePath
