using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CashTracker.App.Services
{
    internal interface ILicenseService
    {
        string GetInstallCode();
        string GetInstallCodeHash();
        Task<LicenseValidationResult> GetCurrentStatusAsync();
        Task<LicenseAccessResult> EvaluateAccessAsync(LicenseStartupContext? startupContext = null);
        Task<LicenseAccessResult> RecordSuccessfulUseAsync();
        Task<LicenseRuntimeState> GetRuntimeStateAsync();
        Task<LicenseValidationResult> ActivateAsync(string licenseKey);
        Task ClearAsync();
    }

    internal sealed class LicenseService : ILicenseService
    {
        internal const int TrialDays = 15;
        private const string FileName = "license.json";
        private const string ProtectedPrefix = "enc:";
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        private readonly AppRuntimeOptions _runtimeOptions;
        private readonly IInstallIdentityService _installIdentityService;
        private readonly ILicenseRuntimeStateStore _runtimeStateStore;
        private readonly string _publicKeyXml;

        private sealed class StoredLicense
        {
            public string LicenseKey { get; set; } = string.Empty;
            public DateTime SavedAtUtc { get; set; }
        }

        public LicenseService(
            AppRuntimeOptions runtimeOptions,
            IInstallIdentityService installIdentityService,
            ILicenseRuntimeStateStore runtimeStateStore,
            string? publicKeyXml = null)
        {
            _runtimeOptions = runtimeOptions;
            _installIdentityService = installIdentityService;
            _runtimeStateStore = runtimeStateStore;
            _publicKeyXml = string.IsNullOrWhiteSpace(publicKeyXml)
                ? AppSigningKeys.GetLicensePublicKeyXml(runtimeOptions.AppDataPath)
                : publicKeyXml.Trim();
        }

        public string GetInstallCode() => _installIdentityService.GetInstallCode();
        public string GetInstallCodeHash() => _installIdentityService.GetInstallCodeHash();

        public Task<LicenseRuntimeState> GetRuntimeStateAsync()
        {
            return Task.FromResult(EnsureRuntimeState(null));
        }

        public async Task<LicenseAccessResult> EvaluateAccessAsync(LicenseStartupContext? startupContext = null)
        {
            var state = EnsureRuntimeState(startupContext);
            var now = DateTime.UtcNow;

            if (state.TamperLocked)
            {
                return CreateAccessResult(
                    LicenseAccessMode.Blocked,
                    state,
                    "Lisans dogrulamasi icin saat veya trial verisi supheli bulundu. Devam etmek icin lisans anahtari girin.");
            }

            if (IsClockTampered(state, now))
            {
                state.TamperLocked = true;
                SaveRuntimeState(state);
                return CreateAccessResult(
                    LicenseAccessMode.Blocked,
                    state,
                    "Sistem saati geri alinmis gorunuyor. Devam etmek icin lisans anahtari girin.");
            }

            var validation = await GetCurrentStatusAsync();
            if (validation.IsValid)
            {
                if (validation.Payload is not null)
                {
                    state.ActivatedAtUtc ??= DateTime.UtcNow;
                    state.ActivatedLicenseId = validation.Payload.LicenseId;
                    state.TamperLocked = false;
                    SaveRuntimeState(state);
                }

                return CreateAccessResult(
                    LicenseAccessMode.Active,
                    state,
                    $"Aktif lisans: {validation.Payload?.CustomerName}",
                    validation);
            }

            if (state.LegacyExempt)
            {
                return CreateAccessResult(
                    LicenseAccessMode.LegacyExempt,
                    state,
                    "Bu kurulum lisans gecisinden muaf.",
                    validation);
            }

            if (ShouldHardBlockForLicenseValidation(validation))
            {
                return CreateAccessResult(
                    LicenseAccessMode.Blocked,
                    state,
                    validation.Message,
                    validation);
            }

            var daysRemaining = GetDaysRemaining(state, now);
            if (state.TrialStartedAtUtc.HasValue && daysRemaining <= 0)
            {
                return CreateAccessResult(
                    LicenseAccessMode.Blocked,
                    state,
                    "Deneme suresi doldu. Devam etmek icin lisans anahtari girin.",
                    validation);
            }

            var message = daysRemaining == TrialDays
                ? $"Lisans girilmedi. Ilk kullanimla birlikte {TrialDays} gunluk sure baslayacak."
                : $"Lisans girilmedi. {daysRemaining} gunluk kullanim hakki kaldi.";

            return CreateAccessResult(
                LicenseAccessMode.Trial,
                state,
                message,
                validation,
                daysRemaining,
                showBanner: true);
        }

        public async Task<LicenseAccessResult> RecordSuccessfulUseAsync()
        {
            var state = EnsureRuntimeState(null);
            var now = DateTime.UtcNow;
            if (IsClockTampered(state, now))
            {
                state.TamperLocked = true;
                SaveRuntimeState(state);
                return await EvaluateAccessAsync();
            }

            var validation = await GetCurrentStatusAsync();
            if (validation.IsValid || state.LegacyExempt)
            {
                state.LastSeenAtUtc = now;
                SaveRuntimeState(state);
                return await EvaluateAccessAsync();
            }

            if (!state.TrialStartedAtUtc.HasValue)
                state.TrialStartedAtUtc = now;

            state.LastSeenAtUtc = now;
            SaveRuntimeState(state);

            return await EvaluateAccessAsync();
        }

        public async Task<LicenseValidationResult> GetCurrentStatusAsync()
        {
            try
            {
                var path = GetPath();
                if (!File.Exists(path))
                {
                    return LicenseValidationResult.Failure(
                        LicenseValidationError.Missing,
                        "Lisans bulunamadi.");
                }

                var json = await File.ReadAllTextAsync(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return LicenseValidationResult.Failure(
                        LicenseValidationError.Missing,
                        "Lisans dosyasi bos.");
                }

                var stored = JsonSerializer.Deserialize<StoredLicense>(json);
                if (stored is null)
                {
                    return LicenseValidationResult.Failure(
                        LicenseValidationError.StorageError,
                        "Lisans dosyasi okunamadi.");
                }

                var clearLicense = Unprotect(stored.LicenseKey);
                return ValidateLicenseKey(clearLicense);
            }
            catch (Exception ex)
            {
                return LicenseValidationResult.Failure(
                    LicenseValidationError.StorageError,
                    $"Lisans okunamadi: {ex.Message}");
            }
        }

        public async Task<LicenseValidationResult> ActivateAsync(string licenseKey)
        {
            var result = ValidateLicenseKey(licenseKey);
            if (!result.IsValid)
                return result;

            Directory.CreateDirectory(_runtimeOptions.AppDataPath);
            var stored = new StoredLicense
            {
                LicenseKey = Protect(licenseKey.Trim()),
                SavedAtUtc = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(stored, JsonOptions);
            await File.WriteAllTextAsync(GetPath(), json);

            if (result.Payload is not null)
            {
                var state = EnsureRuntimeState(null);
                state.ActivatedAtUtc = DateTime.UtcNow;
                state.ActivatedLicenseId = result.Payload.LicenseId;
                state.TamperLocked = false;
                state.LastSeenAtUtc = DateTime.UtcNow;
                SaveRuntimeState(state);
            }

            return result;
        }

        public Task ClearAsync()
        {
            var path = GetPath();
            if (File.Exists(path))
                File.Delete(path);

            var state = EnsureRuntimeState(null);
            state.ActivatedAtUtc = null;
            state.ActivatedLicenseId = string.Empty;
            SaveRuntimeState(state);
            return Task.CompletedTask;
        }

        private LicenseRuntimeState EnsureRuntimeState(LicenseStartupContext? startupContext)
        {
            var state = _runtimeStateStore.Load();
            var changed = false;
            var installCode = GetInstallCode();

            if (!string.Equals(state.InstallCode, installCode, StringComparison.Ordinal))
            {
                state.InstallCode = installCode;
                changed = true;
            }

            if (IsUninitializedState(state) &&
                startupContext is not null &&
                (startupContext.HadExistingAppState || startupContext.HadExistingDatabase))
            {
                state.LegacyExempt = true;
                changed = true;
            }

            if (changed)
                SaveRuntimeState(state);

            return state;
        }

        private void SaveRuntimeState(LicenseRuntimeState state)
        {
            _runtimeStateStore.Save(state);
        }

        private LicenseValidationResult ValidateLicenseKey(string? licenseKey)
        {
            var raw = licenseKey?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return LicenseValidationResult.Failure(
                    LicenseValidationError.Missing,
                    "Lisans anahtari gerekli.");
            }

            var parts = raw.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3 || !string.Equals(parts[0], "CT1", StringComparison.Ordinal))
            {
                return LicenseValidationResult.Failure(
                    LicenseValidationError.InvalidFormat,
                    "Lisans anahtari bicimi gecersiz.");
            }

            try
            {
                var payloadBytes = Base64Url.Decode(parts[1]);
                var signatureBytes = Base64Url.Decode(parts[2]);
                var payload = JsonSerializer.Deserialize<LicensePayload>(payloadBytes);
                if (payload is null)
                {
                    return LicenseValidationResult.Failure(
                        LicenseValidationError.InvalidFormat,
                        "Lisans icerigi okunamadi.");
                }

                if (!VerifySignature(payloadBytes, signatureBytes, _publicKeyXml))
                {
                    return LicenseValidationResult.Failure(
                        LicenseValidationError.InvalidSignature,
                        "Lisans imzasi dogrulanamadi.");
                }

                if (!string.Equals(
                        payload.InstallCodeHash?.Trim(),
                        GetInstallCodeHash(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    return LicenseValidationResult.Failure(
                        LicenseValidationError.InstallCodeMismatch,
                        "Bu lisans bu kurulum icin olusturulmamis.");
                }

                if (payload.ExpiresAtUtc.HasValue && payload.ExpiresAtUtc.Value < DateTime.UtcNow)
                {
                    return LicenseValidationResult.Failure(
                        LicenseValidationError.Expired,
                        "Lisans suresi dolmus.");
                }

                return LicenseValidationResult.Success(payload, raw);
            }
            catch (Exception ex)
            {
                return LicenseValidationResult.Failure(
                    LicenseValidationError.InvalidFormat,
                    $"Lisans anahtari okunamadi: {ex.Message}");
            }
        }

        private static bool VerifySignature(byte[] payloadBytes, byte[] signatureBytes, string publicKeyXml)
        {
            using var rsa = RSA.Create();
            rsa.FromXmlString(publicKeyXml);
            return rsa.VerifyData(payloadBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        private static bool IsClockTampered(LicenseRuntimeState state, DateTime nowUtc)
        {
            return state.LastSeenAtUtc.HasValue &&
                   state.LastSeenAtUtc.Value > nowUtc.AddHours(12);
        }

        private static int GetDaysRemaining(LicenseRuntimeState state, DateTime nowUtc)
        {
            if (!state.TrialStartedAtUtc.HasValue)
                return TrialDays;

            var elapsed = nowUtc - state.TrialStartedAtUtc.Value;
            if (elapsed < TimeSpan.Zero)
                return TrialDays;

            var elapsedDays = (int)Math.Floor(elapsed.TotalDays);
            return Math.Max(0, TrialDays - elapsedDays);
        }

        private static bool IsUninitializedState(LicenseRuntimeState state)
        {
            return !state.LegacyExempt &&
                   !state.TamperLocked &&
                   !state.TrialStartedAtUtc.HasValue &&
                   !state.LastSeenAtUtc.HasValue &&
                   !state.ActivatedAtUtc.HasValue &&
                   string.IsNullOrWhiteSpace(state.ActivatedLicenseId);
        }

        private static bool ShouldHardBlockForLicenseValidation(LicenseValidationResult validation)
        {
            return validation.Error is LicenseValidationError.InvalidFormat or
                LicenseValidationError.InvalidSignature or
                LicenseValidationError.InstallCodeMismatch or
                LicenseValidationError.Expired;
        }

        private static LicenseAccessResult CreateAccessResult(
            LicenseAccessMode mode,
            LicenseRuntimeState state,
            string message,
            LicenseValidationResult? validation = null,
            int daysRemaining = 0,
            bool showBanner = false)
        {
            return new LicenseAccessResult
            {
                Mode = mode,
                DaysRemaining = daysRemaining,
                Message = message,
                ShowBanner = showBanner,
                Validation = validation,
                RuntimeState = state,
                InstallCode = state.InstallCode
            };
        }

        private string GetPath() => Path.Combine(_runtimeOptions.AppDataPath, FileName);

        private static string Protect(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            try
            {
                var clearBytes = Encoding.UTF8.GetBytes(value.Trim());
                var cipherBytes = ProtectedData.Protect(clearBytes, null, DataProtectionScope.CurrentUser);
                return ProtectedPrefix + Convert.ToBase64String(cipherBytes);
            }
            catch
            {
                return value.Trim();
            }
        }

        private static string Unprotect(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            if (!value.StartsWith(ProtectedPrefix, StringComparison.Ordinal))
                return value.Trim();

            try
            {
                var cipherBytes = Convert.FromBase64String(value[ProtectedPrefix.Length..]);
                var clearBytes = ProtectedData.Unprotect(cipherBytes, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(clearBytes).Trim();
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
