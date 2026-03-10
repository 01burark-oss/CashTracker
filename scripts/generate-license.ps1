param(
    [Parameter(Mandatory = $true)]
    [string]$CustomerName,
    [Parameter(Mandatory = $true)]
    [string]$InstallCode,
    [Parameter(Mandatory = $true)]
    [string]$LicenseId,
    [string]$Edition = "pro",
    [string]$ExpiresAtUtc = "",
    [string]$PrivateKeyXml = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($PrivateKeyXml)) {
    $PrivateKeyXml = $env:CASHTRACKER_LICENSE_PRIVATE_KEY_XML
}

if ([string]::IsNullOrWhiteSpace($PrivateKeyXml)) {
    throw "License signing private key is required."
}

function Convert-ToBase64Url([byte[]]$bytes) {
    [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}

$sha = [System.Security.Cryptography.SHA256]::Create()
$installCodeHashBytes = $sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($InstallCode))
$installCodeHash = [Convert]::ToHexString($installCodeHashBytes).ToLowerInvariant()

$payload = [ordered]@{
    licenseId = $LicenseId
    customerName = $CustomerName
    installCodeHash = $installCodeHash
    issuedAtUtc = [DateTime]::UtcNow.ToString("O")
    expiresAtUtc = if ([string]::IsNullOrWhiteSpace($ExpiresAtUtc)) { $null } else { [DateTime]::Parse($ExpiresAtUtc).ToUniversalTime().ToString("O") }
    edition = $Edition
}

$payloadJson = $payload | ConvertTo-Json -Compress -Depth 4
$payloadBytes = [System.Text.Encoding]::UTF8.GetBytes($payloadJson)
$rsa = [System.Security.Cryptography.RSA]::Create()
$rsa.FromXmlString($PrivateKeyXml)
$signatureBytes = $rsa.SignData(
    $payloadBytes,
    [System.Security.Cryptography.HashAlgorithmName]::SHA256,
    [System.Security.Cryptography.RSASignaturePadding]::Pkcs1)

$licenseKey = "CT1.$(Convert-ToBase64Url $payloadBytes).$(Convert-ToBase64Url $signatureBytes)"
Write-Output $licenseKey
