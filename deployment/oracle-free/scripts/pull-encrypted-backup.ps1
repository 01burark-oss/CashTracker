[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$IdentityFile,
    [string]$Destination = (Join-Path $env:LOCALAPPDATA 'Systemcel\Backups'),
    [ValidatePattern('^[a-zA-Z0-9@.-]+$')][string]$Remote = 'ubuntu@89.168.102.124'
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Security
$stamp = (& ssh -i $IdentityFile -o BatchMode=yes -o ConnectTimeout=15 $Remote 'find /opt/systemcel/backups -maxdepth 1 -type f -name "systemcel-????????T??????Z.sha256" -printf "%f\n" | sort | tail -1').Trim()
if ($LASTEXITCODE -ne 0 -or $stamp -notmatch '^systemcel-(\d{8}T\d{6}Z)\.sha256$') {
    throw 'Tamamlanmış uzak yedek bulunamadı.'
}
$timestamp = $Matches[1]
$names = @("systemcel-db-$timestamp.dump", "systemcel-appdata-$timestamp.tar.gz", $stamp)
$temporary = Join-Path ([IO.Path]::GetTempPath()) ('systemcel-backup-' + [guid]::NewGuid())
$null = New-Item -ItemType Directory -Path $temporary
$null = New-Item -ItemType Directory -Force -Path $Destination
try {
    foreach ($name in $names) {
        & scp -q -i $IdentityFile -o BatchMode=yes -o ConnectTimeout=15 "${Remote}:/opt/systemcel/backups/$name" (Join-Path $temporary $name)
        if ($LASTEXITCODE -ne 0) { throw 'Yedek indirme başarısız.' }
    }
    $manifest = Get-Content -LiteralPath (Join-Path $temporary $stamp)
    if ($manifest.Count -ne 2) { throw 'Beklenmeyen manifest yapısı.' }
    foreach ($index in 0..1) {
        $name = $names[$index]
        $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $temporary $name)).Hash.ToLowerInvariant()
        if ($manifest[$index] -cne "$hash  $name") { throw 'Yedek checksum eşleşmedi.' }
    }
    foreach ($name in $names) {
        $target = Join-Path $Destination ($name + '.dpapi')
        if (Test-Path -LiteralPath $target) { throw "Hedef zaten var: $target" }
        $plain = [IO.File]::ReadAllBytes((Join-Path $temporary $name))
        $encrypted = [Security.Cryptography.ProtectedData]::Protect($plain, $null, [Security.Cryptography.DataProtectionScope]::CurrentUser)
        [IO.File]::WriteAllBytes($target, $encrypted)
        $roundTrip = [Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes($target), $null, [Security.Cryptography.DataProtectionScope]::CurrentUser)
        $sha = [Security.Cryptography.SHA256]::Create()
        try {
            if ([Convert]::ToBase64String($sha.ComputeHash($plain)) -ne [Convert]::ToBase64String($sha.ComputeHash($roundTrip))) { throw 'Şifre çözme doğrulaması başarısız.' }
        } finally { $sha.Dispose() }
    }
    Write-Output "İndirildi, checksum ve şifre çözme doğrulandı: $timestamp"
    Write-Output "Konum: $Destination"
} finally {
    # Only the three explicitly named downloads in this run's unique temp folder.
    foreach ($name in $names) {
        $download = Join-Path $temporary $name
        if (Test-Path -LiteralPath $download) { Remove-Item -LiteralPath $download }
    }
    Remove-Item -LiteralPath $temporary
}
