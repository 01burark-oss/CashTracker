[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^[0-9a-fA-F]{40}$')]
    [string]$CandidateSha,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$EnvironmentName,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$OutputPath
)

$ErrorActionPreference = "Stop"

function New-Observation([string]$Blocker) {
    [ordered]@{
        status = "pending"
        observedAtUtc = $null
        actualResult = $null
        evidencePath = $null
        blocker = $Blocker
    }
}

function New-Check(
    [string]$Scenario,
    [string]$Role,
    [string]$Expected,
    [string]$Blocker,
    [string]$Device = $null
) {
    $check = [ordered]@{
        scenario = $Scenario
        role = $Role
        actorLabel = $null
        expectedResult = $Expected
        status = "pending"
        observedAtUtc = $null
        actualResult = $null
        evidencePath = $null
        blocker = $Blocker
    }
    if (-not [string]::IsNullOrWhiteSpace($Device)) { $check.device = $Device }
    $check
}

$externalAccount = "Kontrollü gerçek hesap ve sağlayıcı erişimi gerekli."
$physicalDevice = "Fiziksel cihaz ve cihaz sahibi gerekli; WebKit emülasyonu yeterli değildir."
$pilotParticipants = "Pilot katılımcıları, takvim ve destek kanalı gerekli."

$document = [ordered]@{
    schemaVersion = 1
    candidateSha = $CandidateSha.ToLowerInvariant()
    environment = $EnvironmentName
    recordedAtUtc = [DateTimeOffset]::UtcNow.ToString("o")
    release = [ordered]@{
        sourceSha = $CandidateSha.ToLowerInvariant()
        deployedSha = $null
        bundle = New-Observation "SHA-sabitli yayın paketi henüz oluşturulmadı veya doğrulanmadı."
        health = New-Observation "Aday sürümün hedef ortam sağlık kaydı henüz alınmadı."
    }
    gates = @(
        [ordered]@{
            id = "K0"; title = "Sürüm ve kanıt envanteri"; externalRequired = $false; status = "pending"
            checks = @(
                (New-Check "candidate-sha" "developer" "Kaynak, paket ve hedef ortam aynı tam commit SHA ile ilişkilidir." "Paket ve hedef ortam SHA kaydı gerekli."),
                (New-Check "public-health" "developer" "Liveness, readiness ve public smoke aynı SHA için geçer." "Hedef ortam public smoke kaydı gerekli.")
            )
        },
        [ordered]@{
            id = "K1"; title = "Gerçek Clerk kaydı ve kurulum"; externalRequired = $true; status = "pending"
            checks = @(
                (New-Check "email-signup-onboarding" "business-owner" "Kayıt, doğrulama, rol, kurulum ve ana ekran tek kullanıcı/işletme üretir." $externalAccount),
                (New-Check "oauth-resume-idempotency" "accountant" "OAuth dönüşü, yenileme ve tekrar callback mükerrer kayıt üretmez." $externalAccount),
                (New-Check "signout-session-expiry" "business-owner" "Çıkış ve süresi dolmuş oturum sonrası korunan API reddedilir." $externalAccount)
            )
        },
        [ordered]@{
            id = "K2"; title = "Tenant ve üyelik sınırları"; externalRequired = $true; status = "pending"
            checks = @(
                (New-Check "tenant-a-to-b-denial" "owner-staff-accountant" "Liste, kayıt, rapor, dosya ve sohbet erişimleri tenant B verisini sunucuda reddeder." $externalAccount),
                (New-Check "revoked-membership" "staff" "Yetkisi kaldırılan açık sekme, dosya bağlantısı ve sohbet bağlantısı yeniden reddedilir." $externalAccount),
                (New-Check "invite-and-ownership-boundaries" "owner" "Davet, kapasite, rol ve sahiplik kuralları yarış ve tekrar kullanımda korunur." $externalAccount)
            )
        },
        [ordered]@{
            id = "K6"; title = "AI, sohbet dosyası ve kritik arayüz"; externalRequired = $true; status = "pending"
            checks = @(
                (New-Check "ai-provider-and-tenant-context" "business-owner" "Gerçek yanıt/kota/timeout görünür; tenant B verisi yanıta girmez." "AI sağlayıcı erişimi ve iki kontrollü tenant gerekli."),
                (New-Check "chat-file-roundtrip" "business-owner-accountant" "Küçük dosya iki rolde gönderilip indirilir; bozuk/büyük/yetkisiz dosya reddedilir." "Kontrollü hesaplar ve zararsız test dosyası gerekli."),
                (New-Check "critical-ui-keyboard-states" "keyboard-user" "Kritik akışlarda sıra, odak dönüşü, Escape ve yükleme/boş/hata durumları kullanılabilir." "Aday sürümde kontrollü kullanıcı oturumu gerekli.")
            )
        },
        [ordered]@{
            id = "K7"; title = "Fiziksel iPhone ve Safari"; externalRequired = $true; status = "pending"
            checks = @(
                (New-Check "iphone-safari-critical-flow" "business-owner" "Kayıt, klavye, işletme değişimi, dosya/kamera, rapor, modal ve ağ dönüşü geçer." $physicalDevice "MODEL / iOS / Safari sürümünü girin")
            )
        },
        [ordered]@{
            id = "K8"; title = "Tekrarlanabilir yayın ve geri dönüş"; externalRequired = $false; status = "pending"
            checks = @(
                (New-Check "immutable-release-bundle" "developer" "Tam SHA, manifest ve SHA-256 içeren paket yeniden üretilebilir." "Release bundle workflow çıktısı gerekli."),
                (New-Check "schema-compatible-rollback" "developer" "Önceki doğrulanmış sürüme dönüş izole ortamda readiness ve smoke ile geçer." "İzole rollback provası ve önceki doğrulanmış SHA gerekli.")
            )
        },
        [ordered]@{
            id = "K9"; title = "Sınırlı gerçek kullanıcı pilotu"; externalRequired = $true; status = "pending"
            checks = @(
                (New-Check "pilot-roster-and-support" "pilot-coordinator" "Anonim katılımcı etiketleri, cihazlar, veri sınırı ve destek kanalı kayıtlıdır." $pilotParticipants),
                (New-Check "pilot-critical-tasks" "business-and-accountant" "Kritik görevler desteklenen roller/cihazlarda en az bir kez tamamlanır; ham sayılar tutulur." $pilotParticipants),
                (New-Check "pilot-release-decision" "pilot-coordinator" "Kritik açık hata yoktur; son 48 saat, yedek ve alarm durumu değerlendirilir." $pilotParticipants)
            )
        }
    )
}

$resolvedOutput = [System.IO.Path]::GetFullPath($OutputPath)
$parent = Split-Path -Parent $resolvedOutput
if (-not [string]::IsNullOrWhiteSpace($parent)) {
    New-Item -ItemType Directory -Force -Path $parent | Out-Null
}
$document | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $resolvedOutput -Encoding utf8NoBOM
Write-Output "Release evidence template created: $resolvedOutput"
