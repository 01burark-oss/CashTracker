using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CashTracker.Core.Entities;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Services
{
    public sealed class AppSecurityService : IAppSecurityService
    {
        private const string PinKey = "AppPin";
        private const string DefaultPin = "0000";
        private const string EncryptedPrefix = "enc:";

        private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;

        public AppSecurityService(IDbContextFactory<CashTrackerDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<string> GetPinAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var row = await db.AppSettings
                .FirstOrDefaultAsync(x => x.Key == PinKey);

            if (row == null)
            {
                var storedDefaultPin = ProtectPin(DefaultPin);
                db.AppSettings.Add(new AppSetting
                {
                    Key = PinKey,
                    Value = storedDefaultPin,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                });
                await db.SaveChangesAsync();
                return DefaultPin;
            }

            if (TryReadStoredPin(row.Value, out var resolvedPin) && IsValidPin(resolvedPin))
            {
                if (!IsEncrypted(row.Value))
                {
                    row.Value = ProtectPin(resolvedPin);
                    row.UpdatedAt = DateTime.Now;
                    await db.SaveChangesAsync();
                }

                return resolvedPin;
            }

            var fallback = ProtectPin(DefaultPin);
            if (!string.Equals(row.Value, fallback, StringComparison.Ordinal))
            {
                row.Value = fallback;
                row.UpdatedAt = DateTime.Now;
                await db.SaveChangesAsync();
            }

            return DefaultPin;
        }

        public async Task SetPinAsync(string pin)
        {
            var normalizedPin = NormalizePin(pin);

            await using var db = await _dbFactory.CreateDbContextAsync();
            var row = await db.AppSettings
                .FirstOrDefaultAsync(x => x.Key == PinKey);

            if (row == null)
            {
                db.AppSettings.Add(new AppSetting
                {
                    Key = PinKey,
                    Value = ProtectPin(normalizedPin),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                });
            }
            else
            {
                row.Value = ProtectPin(normalizedPin);
                row.UpdatedAt = DateTime.Now;
            }

            await db.SaveChangesAsync();
        }

        private static bool IsEncrypted(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.StartsWith(EncryptedPrefix, StringComparison.Ordinal);
        }

        private static bool TryReadStoredPin(string? storedValue, out string pin)
        {
            pin = string.Empty;
            var raw = (storedValue ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            if (!IsEncrypted(raw))
            {
                pin = raw;
                return true;
            }

            try
            {
                if (!OperatingSystem.IsWindows())
                    return false;

                var payload = raw[EncryptedPrefix.Length..];
                var cipher = Convert.FromBase64String(payload);
                var clear = ProtectedData.Unprotect(cipher, null, DataProtectionScope.CurrentUser);
                pin = Encoding.UTF8.GetString(clear).Trim();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string ProtectPin(string pin)
        {
            if (string.IsNullOrWhiteSpace(pin))
                return string.Empty;

            try
            {
                if (!OperatingSystem.IsWindows())
                    return pin.Trim();

                var clearBytes = Encoding.UTF8.GetBytes(pin.Trim());
                var cipherBytes = ProtectedData.Protect(clearBytes, null, DataProtectionScope.CurrentUser);
                return EncryptedPrefix + Convert.ToBase64String(cipherBytes);
            }
            catch
            {
                return pin.Trim();
            }
        }

        private static string NormalizePin(string? value)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (!IsValidPin(normalized))
                throw new ArgumentException("Sifre 4 haneli sayisal olmalidir.", nameof(value));
            return normalized;
        }

        private static bool IsValidPin(string? value)
        {
            var pin = value ?? string.Empty;
            return pin.Length == 4 && pin.All(char.IsDigit);
        }
    }
}
