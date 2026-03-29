using System;

namespace CashTracker.App.Services
{
    internal sealed class LicensePayload
    {
        public string LicenseId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string InstallCodeHash { get; set; } = string.Empty;
        public DateTime IssuedAtUtc { get; set; }
        public DateTime? ExpiresAtUtc { get; set; }
        public string Edition { get; set; } = string.Empty;
        public string ReceiptOcrProvider { get; set; } = string.Empty;
        public string ReceiptOcrModel { get; set; } = string.Empty;
        public string EncryptedReceiptOcrApiKey { get; set; } = string.Empty;
    }

    internal enum LicenseValidationError
    {
        None,
        Missing,
        InvalidFormat,
        InvalidSignature,
        InstallCodeMismatch,
        Expired,
        StorageError
    }

    internal sealed class LicenseValidationResult
    {
        public static LicenseValidationResult Success(LicensePayload payload, string licenseKey)
        {
            return new LicenseValidationResult
            {
                IsValid = true,
                Payload = payload,
                LicenseKey = licenseKey,
                Error = LicenseValidationError.None
            };
        }

        public static LicenseValidationResult Failure(LicenseValidationError error, string message)
        {
            return new LicenseValidationResult
            {
                IsValid = false,
                Error = error,
                Message = message
            };
        }

        public bool IsValid { get; set; }
        public LicensePayload? Payload { get; set; }
        public string LicenseKey { get; set; } = string.Empty;
        public LicenseValidationError Error { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    internal enum LicenseAccessMode
    {
        Active,
        Trial,
        Blocked,
        LegacyExempt
    }

    internal sealed class LicenseRuntimeState
    {
        public string InstallCode { get; set; } = string.Empty;
        public DateTime? TrialStartedAtUtc { get; set; }
        public DateTime? LastSeenAtUtc { get; set; }
        public bool LegacyExempt { get; set; }
        public bool TamperLocked { get; set; }
        public DateTime? ActivatedAtUtc { get; set; }
        public string ActivatedLicenseId { get; set; } = string.Empty;
        public DateTime UpdatedAtUtc { get; set; }
    }

    internal sealed class LicenseStartupContext
    {
        public bool HadExistingAppState { get; init; }
        public bool HadExistingDatabase { get; init; }
    }

    internal sealed class LicenseAccessResult
    {
        public LicenseAccessMode Mode { get; set; }
        public int DaysRemaining { get; set; }
        public string Message { get; set; } = string.Empty;
        public string InstallCode { get; set; } = string.Empty;
        public bool ShowBanner { get; set; }
        public LicenseValidationResult? Validation { get; set; }
        public LicenseRuntimeState RuntimeState { get; set; } = new();
    }
}
