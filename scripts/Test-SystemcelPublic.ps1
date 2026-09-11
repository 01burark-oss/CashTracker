[CmdletBinding()]
param(
    [string]$BaseUrl = "https://systemcel.app",
    [string]$AllowedOrigin = "https://systemcel.app",
    [ValidatePattern('^$|^[0-9a-fA-F]{40}$')]
    [string]$CandidateSha = "",
    [string]$EnvironmentName = "production",
    [string]$EvidencePath = ""
)

$ErrorActionPreference = "Stop"
if (-not [string]::IsNullOrWhiteSpace($EvidencePath) -and [string]::IsNullOrWhiteSpace($CandidateSha)) {
    throw "CandidateSha is required when EvidencePath is set."
}
$base = $BaseUrl.TrimEnd("/")
$failures = [System.Collections.Generic.List[string]]::new()
$checks = [System.Collections.Generic.List[object]]::new()

function Add-Failure([string]$Message) {
    $script:failures.Add($Message)
    $script:checks.Add([ordered]@{ status = "failed"; check = $Message })
    Write-Output "[FAIL] $Message"
}

function Add-Pass([string]$Message) {
    $script:checks.Add([ordered]@{ status = "passed"; check = $Message })
    Write-Output "[PASS] $Message"
}

function Get-Response([string]$Path, [string]$Method = "GET", [hashtable]$Headers = @{}) {
    try {
        Invoke-WebRequest -Uri "$base$Path" -Method $Method -Headers $Headers -UseBasicParsing -SkipHttpErrorCheck
    }
    catch {
        Add-Failure "Transport error for $Method $Path"
        [pscustomobject]@{
            StatusCode = 0
            Headers = @{}
            Content = ""
        }
    }
}

function Write-Evidence {
    if ([string]::IsNullOrWhiteSpace($EvidencePath)) { return }

    $resolvedEvidence = [System.IO.Path]::GetFullPath($EvidencePath)
    $evidenceParent = Split-Path -Parent $resolvedEvidence
    if (-not [string]::IsNullOrWhiteSpace($evidenceParent)) {
        New-Item -ItemType Directory -Force -Path $evidenceParent | Out-Null
    }
    [ordered]@{
        schemaVersion = 1
        candidateSha = if ([string]::IsNullOrWhiteSpace($CandidateSha)) { $null } else { $CandidateSha.ToLowerInvariant() }
        environment = $EnvironmentName
        baseUrl = $base
        observedAtUtc = [DateTimeOffset]::UtcNow.ToString("o")
        status = if ($failures.Count -eq 0) { "passed" } else { "failed" }
        checks = @($checks)
    } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $resolvedEvidence -Encoding utf8NoBOM
    Write-Output "Public smoke evidence written: $resolvedEvidence"
}

function Assert-Status([object]$Response, [int]$Expected, [string]$Name) {
    if ([int]$Response.StatusCode -ne $Expected) {
        Add-Failure "$Name status $($Response.StatusCode), expected $Expected"
    }
    else {
        Add-Pass "$Name status $Expected"
    }
}

function Assert-Header([object]$Response, [string]$Name) {
    if ([string]::IsNullOrWhiteSpace([string]$Response.Headers[$Name])) {
        Add-Failure "Missing response header: $Name"
    }
    else {
        Add-Pass "Response header present: $Name"
    }
}

$root = Get-Response "/"
Assert-Status $root 200 "landing"
@(
    "Strict-Transport-Security",
    "X-Content-Type-Options",
    "X-Frame-Options",
    "Referrer-Policy",
    "Permissions-Policy",
    "Content-Security-Policy"
) | ForEach-Object { Assert-Header $root $_ }

$live = Get-Response "/api/health/live"
Assert-Status $live 200 "liveness"
if ([string]$live.Headers["Content-Type"] -notmatch "application/json") {
    Add-Failure "Liveness response is not JSON"
}
else {
    $liveBody = $live.Content | ConvertFrom-Json
    if ($liveBody.durum -eq "canli") { Add-Pass "Liveness payload" } else { Add-Failure "Unexpected liveness payload" }
}

$ready = Get-Response "/api/health/ready"
Assert-Status $ready 200 "readiness"
if ([string]$ready.Headers["Content-Type"] -notmatch "application/json") {
    Add-Failure "Readiness response is not JSON"
}
else {
    $readyBody = $ready.Content | ConvertFrom-Json
    if ($readyBody.durum -eq "hazir" -and $readyBody.veritabani -eq "PostgreSql") {
        Add-Pass "Readiness payload and PostgreSQL connection"
    }
    else {
        Add-Failure "Unexpected readiness payload"
    }
}

$plans = Get-Response "/api/public/planlar"
Assert-Status $plans 200 "public plans"
if ([string]$plans.Headers["Content-Type"] -notmatch "application/json") {
    Add-Failure "Public plans response is not JSON"
}
else {
    $parsedPlans = $plans.Content | ConvertFrom-Json
    $planBody = @($parsedPlans | ForEach-Object { $_ })
    $invalidPrice = @($planBody | Where-Object { [decimal]$_.aylikTutar -le 0 })
    if ($planBody.Count -eq 5 -and $invalidPrice.Count -eq 0) {
        Add-Pass "Five paid monthly plans"
    }
    else {
        Add-Failure "Expected five paid plans with positive monthly prices"
    }
}

$publicConfig = Get-Response "/api/public/config"
Assert-Status $publicConfig 200 "public config"
if ([string]$publicConfig.Headers["Content-Type"] -notmatch "application/json") {
    Add-Failure "Public config response is not JSON"
}
else {
    $parsedConfig = $publicConfig.Content | ConvertFrom-Json
    if ([string]::IsNullOrWhiteSpace([string]$parsedConfig.environmentName) -or $parsedConfig.clerk.enabled -ne $true) {
        Add-Failure "Public environment or Clerk configuration is missing"
    }
    else {
        Add-Pass "Public environment: $($parsedConfig.environmentName); Clerk enabled"
    }
}

$developerApi = Get-Response "/api/v1/business"
Assert-Status $developerApi 401 "developer API canonical unauthenticated endpoint"
if ([string]$developerApi.Headers["Content-Type"] -notmatch "application/problem\+json") {
    Add-Failure "Developer API authentication error is not application/problem+json"
}
else {
    Add-Pass "Developer API canonical URL and authentication boundary"
}

$untrusted = Get-Response "/api/health/live" "OPTIONS" @{
    Origin = "https://untrusted.invalid"
    "Access-Control-Request-Method" = "GET"
}
if ([string]::IsNullOrWhiteSpace([string]$untrusted.Headers["Access-Control-Allow-Origin"])) {
    Add-Pass "Untrusted CORS origin rejected"
}
else {
    Add-Failure "Untrusted CORS origin accepted"
}

$trusted = Get-Response "/api/health/live" "OPTIONS" @{
    Origin = $AllowedOrigin
    "Access-Control-Request-Method" = "GET"
}
if ([string]$trusted.Headers["Access-Control-Allow-Origin"] -eq $AllowedOrigin) {
    Add-Pass "Configured CORS origin accepted"
}
else {
    Add-Failure "Configured CORS origin was not accepted"
}

Write-Evidence

if ($failures.Count -gt 0) {
    throw "Public gate failed with $($failures.Count) error(s)."
}

Write-Output "Public gate passed for $base"
