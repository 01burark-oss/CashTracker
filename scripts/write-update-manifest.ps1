param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    [Parameter(Mandatory = $true)]
    [string]$PackageUrl,
    [Parameter(Mandatory = $true)]
    [string]$PackagePath,
    [Parameter(Mandatory = $true)]
    [string]$OutputPath,
    [string]$MinSupportedVersion = "",
    [string]$ReleaseNotes = "",
    [switch]$Mandatory,
    [string]$PrivateKeyXml = ""
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $PackagePath)) {
    throw "Package not found: $PackagePath"
}

if ([string]::IsNullOrWhiteSpace($MinSupportedVersion)) {
    $MinSupportedVersion = $Version
}

if ([string]::IsNullOrWhiteSpace($PrivateKeyXml)) {
    $PrivateKeyXml = $env:CASHTRACKER_UPDATE_SIGNING_PRIVATE_KEY_XML
}

if ([string]::IsNullOrWhiteSpace($PrivateKeyXml)) {
    throw "Update signing private key is required."
}

function Convert-ToBase64Url([byte[]]$bytes) {
    [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}

$sha256 = (Get-FileHash -Path $PackagePath -Algorithm SHA256).Hash.ToLowerInvariant()
$payload = [ordered]@{
    latestVersion = $Version
    minSupportedVersion = $MinSupportedVersion
    packageUrl = $PackageUrl
    sha256 = $sha256
    releaseNotes = $ReleaseNotes
    isMandatory = [bool]$Mandatory
}

$payloadJson = $payload | ConvertTo-Json -Compress -Depth 4
$payloadBytes = [System.Text.Encoding]::UTF8.GetBytes($payloadJson)
$rsa = [System.Security.Cryptography.RSA]::Create()
$rsa.FromXmlString($PrivateKeyXml)
$signatureBytes = $rsa.SignData(
    $payloadBytes,
    [System.Security.Cryptography.HashAlgorithmName]::SHA256,
    [System.Security.Cryptography.RSASignaturePadding]::Pkcs1)

$manifest = [ordered]@{
    latestVersion = $Version
    minSupportedVersion = $MinSupportedVersion
    packageUrl = $PackageUrl
    sha256 = $sha256
    releaseNotes = $ReleaseNotes
    isMandatory = [bool]$Mandatory
    signature = (Convert-ToBase64Url $signatureBytes)
}

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutputPath) | Out-Null
$manifest | ConvertTo-Json -Depth 4 | Set-Content -Path $OutputPath -Encoding UTF8
