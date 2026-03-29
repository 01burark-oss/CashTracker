using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using System.Reflection;
using CashTracker.App;
using CashTracker.App.Services;
using CashTracker.Core.Models;
using CashTracker.Core.Utilities;
using CashTracker.Tests.Support;
using Xunit;

namespace CashTracker.Tests
{
    public sealed class LicenseServiceTests
    {
        [Fact]
        public async Task ActivateAsync_ValidSignedLicense_PersistsAndLoads()
        {
            var appDataPath = Path.Combine(Path.GetTempPath(), $"cashtracker_license_{Guid.NewGuid():N}");
            Directory.CreateDirectory(appDataPath);

            try
            {
                using var rsa = RSA.Create(2048);
                var installIdentity = new FakeInstallIdentityService
                {
                    InstallCode = "CTI-TEST-CODE",
                    InstallCodeHash = "hash-123"
                };
                var runtimeStore = new FakeLicenseRuntimeStateStore();

                var service = new LicenseService(
                    new AppRuntimeOptions { AppDataPath = appDataPath },
                    installIdentity,
                    runtimeStore,
                    rsa.ToXmlString(false));

                var payload = new LicensePayload
                {
                    LicenseId = "LIC-001",
                    CustomerName = "Demo User",
                    InstallCodeHash = installIdentity.InstallCodeHash,
                    IssuedAtUtc = DateTime.UtcNow,
                    Edition = "pro"
                };

                var licenseKey = CreateSignedLicenseKey(payload, rsa);
                var activation = await service.ActivateAsync(licenseKey);

                Assert.True(activation.IsValid);

                var current = await service.GetCurrentStatusAsync();
                Assert.True(current.IsValid);
                Assert.NotNull(current.Payload);
                Assert.Equal("Demo User", current.Payload!.CustomerName);
                Assert.Equal("pro", current.Payload.Edition);
            }
            finally
            {
                if (Directory.Exists(appDataPath))
                    Directory.Delete(appDataPath, true);
            }
        }

        [Fact]
        public async Task ActivateAsync_InstallCodeMismatch_ReturnsFailure()
        {
            using var rsa = RSA.Create(2048);
            var installIdentity = new FakeInstallIdentityService { InstallCodeHash = "expected-hash" };
            var service = new LicenseService(
                new AppRuntimeOptions { AppDataPath = Path.GetTempPath() },
                installIdentity,
                new FakeLicenseRuntimeStateStore(),
                rsa.ToXmlString(false));

            var payload = new LicensePayload
            {
                LicenseId = "LIC-002",
                CustomerName = "Wrong Install",
                InstallCodeHash = "other-hash",
                IssuedAtUtc = DateTime.UtcNow,
                Edition = "pro"
            };

            var result = await service.ActivateAsync(CreateSignedLicenseKey(payload, rsa));

            Assert.False(result.IsValid);
            Assert.Equal(LicenseValidationError.InstallCodeMismatch, result.Error);
        }

        [Fact]
        public async Task EvaluateAccessAsync_NewInstall_ReturnsTrial()
        {
            var service = new LicenseService(
                new AppRuntimeOptions { AppDataPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")) },
                new FakeInstallIdentityService(),
                new FakeLicenseRuntimeStateStore());

            var result = await service.EvaluateAccessAsync(new LicenseStartupContext());

            Assert.Equal(LicenseAccessMode.Trial, result.Mode);
            Assert.Equal(LicenseService.TrialDays, result.DaysRemaining);
            Assert.True(result.ShowBanner);
        }

        [Fact]
        public async Task EvaluateAccessAsync_LegacyInstall_ReturnsLegacyExempt()
        {
            var service = new LicenseService(
                new AppRuntimeOptions { AppDataPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")) },
                new FakeInstallIdentityService(),
                new FakeLicenseRuntimeStateStore());

            var result = await service.EvaluateAccessAsync(new LicenseStartupContext
            {
                HadExistingAppState = true
            });

            Assert.Equal(LicenseAccessMode.LegacyExempt, result.Mode);
        }

        [Fact]
        public void GetDaysRemaining_UsesElapsedTimeInsteadOfUtcDate()
        {
            var state = new LicenseRuntimeState
            {
                TrialStartedAtUtc = new DateTime(2026, 3, 1, 23, 59, 0, DateTimeKind.Utc)
            };

            var method = typeof(LicenseService).GetMethod("GetDaysRemaining", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            var remaining = (int)method!.Invoke(null, new object[] { state, new DateTime(2026, 3, 2, 0, 1, 0, DateTimeKind.Utc) })!;

            Assert.Equal(LicenseService.TrialDays, remaining);
        }

        [Fact]
        public async Task RecordSuccessfulUseAsync_ValidLicense_RefreshesLastSeenAtUtc()
        {
            using var rsa = RSA.Create(2048);
            var runtimeStore = new FakeLicenseRuntimeStateStore();
            runtimeStore.Save(new LicenseRuntimeState
            {
                LastSeenAtUtc = DateTime.UtcNow.AddDays(-5)
            });
            var installIdentity = new FakeInstallIdentityService
            {
                InstallCodeHash = "hash-123"
            };
            var service = new LicenseService(
                new AppRuntimeOptions { AppDataPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")) },
                installIdentity,
                runtimeStore,
                rsa.ToXmlString(false));

            var payload = new LicensePayload
            {
                LicenseId = "LIC-REFRESH",
                CustomerName = "Refresh Test",
                InstallCodeHash = installIdentity.InstallCodeHash,
                IssuedAtUtc = DateTime.UtcNow,
                Edition = "pro"
            };

            var activation = await service.ActivateAsync(CreateSignedLicenseKey(payload, rsa));
            Assert.True(activation.IsValid);

            var firstSeen = runtimeStore.State.LastSeenAtUtc;
            await Task.Delay(25);

            var access = await service.RecordSuccessfulUseAsync();

            Assert.Equal(LicenseAccessMode.Active, access.Mode);
            Assert.True(runtimeStore.State.LastSeenAtUtc > firstSeen);
        }

        [Fact]
        public async Task ApplyReceiptOcrSettingsAsync_ValidEncryptedSecret_OverridesSettings()
        {
            using var rsa = RSA.Create(2048);
            var installIdentity = new FakeInstallIdentityService
            {
                InstallCode = "CTI-1234ABCD-5678EF90-1357ACE0",
                InstallCodeHash = "hash-ocr-123"
            };
            var service = new LicenseService(
                new AppRuntimeOptions { AppDataPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")) },
                installIdentity,
                new FakeLicenseRuntimeStateStore(),
                rsa.ToXmlString(false));

            var payload = new LicensePayload
            {
                LicenseId = "LIC-OCR-001",
                CustomerName = "OCR User",
                InstallCodeHash = installIdentity.InstallCodeHash,
                IssuedAtUtc = DateTime.UtcNow,
                Edition = "pro",
                ReceiptOcrProvider = "Gemini",
                ReceiptOcrModel = "gemini-2.5-flash",
                EncryptedReceiptOcrApiKey = InstallScopedSecretProtector.Protect("secret-api-key", installIdentity.InstallCode)
            };

            var activation = await service.ActivateAsync(CreateSignedLicenseKey(payload, rsa));
            Assert.True(activation.IsValid);

            var settings = new ReceiptOcrSettings
            {
                Provider = "Gemini",
                ApiKey = string.Empty,
                Model = "gemini-2.0-flash"
            };

            await service.ApplyReceiptOcrSettingsAsync(settings);

            Assert.Equal("Gemini", settings.EffectiveProvider);
            Assert.Equal("gemini-2.5-flash", settings.EffectiveModel);
            Assert.Equal("secret-api-key", settings.EffectiveApiKey);
            Assert.True(settings.IsConfigured);
        }

        private static string CreateSignedLicenseKey(LicensePayload payload, RSA rsa)
        {
            var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(payload);
            var signatureBytes = rsa.SignData(payloadBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return $"CT1.{Base64Url.Encode(payloadBytes)}.{Base64Url.Encode(signatureBytes)}";
        }
    }
}
