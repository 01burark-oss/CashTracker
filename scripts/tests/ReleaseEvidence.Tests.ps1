$script:RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$script:Generator = Join-Path $RepoRoot "scripts/New-SystemcelReleaseEvidence.ps1"
$script:Validator = Join-Path $RepoRoot "scripts/Test-SystemcelReleaseEvidence.ps1"
$script:Sha = "48d76c7fc191d433fcbb57d087521fdc529c22ec"

Describe "Systemcel release evidence" {
    It "creates a valid template without claiming external checks passed" {
        $path = Join-Path $TestDrive "evidence.json"

        & $Generator -CandidateSha $Sha -EnvironmentName "staging" -OutputPath $path
        { & $Validator -Path $path } | Should Not Throw

        $evidence = Get-Content -Raw $path | ConvertFrom-Json
        $evidence.candidateSha | Should Be $Sha
        (@($evidence.gates).id -join ",") | Should Be "K0,K1,K2,K6,K7,K8,K9"
        @($evidence.gates | ForEach-Object checks | Where-Object status -eq "passed").Count | Should Be 0
        (@($evidence.gates | Where-Object externalRequired).status -join ",") | Should Not Match "passed"
    }

    It "rejects a passed real-world check without timestamp, result and evidence reference" {
        $path = Join-Path $TestDrive "unsupported-pass.json"
        & $Generator -CandidateSha $Sha -EnvironmentName "production" -OutputPath $path
        $evidence = Get-Content -Raw $path | ConvertFrom-Json
        $evidence.gates[1].checks[0].status = "passed"
        $evidence | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $path -Encoding utf8NoBOM

        $threw = $false
        try { & $Validator -Path $path } catch { $threw = $true }
        $threw | Should Be $true
    }

    It "accepts a physical-device pass only when device and evidence details are recorded" {
        $path = Join-Path $TestDrive "iphone.json"
        & $Generator -CandidateSha $Sha -EnvironmentName "production" -OutputPath $path
        $evidence = Get-Content -Raw $path | ConvertFrom-Json
        $check = $evidence.gates | Where-Object id -eq "K7" | Select-Object -ExpandProperty checks -First 1
        $check.status = "passed"
        $check.observedAtUtc = "2026-09-11T08:30:00Z"
        $check.actualResult = "Kritik akış tamamlandı."
        $check.evidencePath = "private://pilot/K7/iphone-01"
        $check.blocker = $null
        $check.actorLabel = "pilot-owner-01"
        $check.device = "iPhone 15 / iOS 20 / Safari"
        $evidence | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $path -Encoding utf8NoBOM

        { & $Validator -Path $path } | Should Not Throw
    }
}
