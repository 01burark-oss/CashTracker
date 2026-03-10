using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
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
        private readonly DatabasePaths _databasePaths;

        public AppSecurityService(IDbContextFactory<CashTrackerDbContext> dbFactory, DatabasePaths databasePaths)
        {
            _dbFactory = dbFactory;
            _databasePaths = databasePaths;
        }

        public async Task<string> GetPinAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var row = await db.AppSettings
                .FirstOrDefaultAsync(x => x.Key == PinKey);

            if (row == null)
            {
                var mirroredPin = await TryGetMirrorPinAsync();
                if (IsValidPin(mirroredPin) && !string.Equals(mirroredPin, DefaultPin, StringComparison.Ordinal))
                {
                    await UpsertPinAsync(db, mirroredPin!);
                    return mirroredPin!;
                }

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

                if (string.Equals(resolvedPin, DefaultPin, StringComparison.Ordinal))
                {
                    var mirroredPin = await TryGetMirrorPinAsync();
                    if (IsValidPin(mirroredPin) && !string.Equals(mirroredPin, DefaultPin, StringComparison.Ordinal))
                    {
                        await UpsertPinAsync(db, mirroredPin!);
                        return mirroredPin!;
                    }
                }

                return resolvedPin;
            }

            var restoredPin = await TryGetMirrorPinAsync();
            if (IsValidPin(restoredPin) && !string.Equals(restoredPin, DefaultPin, StringComparison.Ordinal))
            {
                await UpsertPinAsync(db, restoredPin!);
                return restoredPin!;
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

            await UpsertPinAsync(db, normalizedPin, row);
            await SyncMirrorPinsAsync(normalizedPin);
        }

        public async Task<bool> VerifyPinAsync(string pin)
        {
            var currentPin = await GetPinAsync();
            return string.Equals(
                NormalizePin(pin),
                currentPin,
                StringComparison.Ordinal);
        }

        public async Task<bool> IsDefaultPinAsync()
        {
            var currentPin = await GetPinAsync();
            return string.Equals(currentPin, DefaultPin, StringComparison.Ordinal);
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

        private async Task UpsertPinAsync(CashTrackerDbContext db, string pin, AppSetting? existingRow = null)
        {
            var row = existingRow ?? await db.AppSettings.FirstOrDefaultAsync(x => x.Key == PinKey);
            if (row == null)
            {
                db.AppSettings.Add(new AppSetting
                {
                    Key = PinKey,
                    Value = ProtectPin(pin),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                });
            }
            else
            {
                row.Value = ProtectPin(pin);
                row.UpdatedAt = DateTime.Now;
            }

            await db.SaveChangesAsync();
        }

        private async Task<string?> TryGetMirrorPinAsync()
        {
            foreach (var mirrorPath in EnumerateMirrorDbPaths())
            {
                var pin = await TryReadPinFromDatabaseAsync(mirrorPath);
                if (IsValidPin(pin) && !string.Equals(pin, DefaultPin, StringComparison.Ordinal))
                    return pin;
            }

            return null;
        }

        private async Task SyncMirrorPinsAsync(string pin)
        {
            foreach (var mirrorPath in EnumerateMirrorDbPaths())
            {
                try
                {
                    var dir = Path.GetDirectoryName(mirrorPath);
                    if (string.IsNullOrWhiteSpace(dir))
                        continue;

                    Directory.CreateDirectory(dir);
                    var options = new DbContextOptionsBuilder<CashTrackerDbContext>()
                        .UseSqlite($"Data Source={mirrorPath}")
                        .Options;

                    await using var db = new CashTrackerDbContext(options);
                    await db.Database.EnsureCreatedAsync();
                    SchemaMigrator.EnsureKasaSchema(db);
                    await UpsertPinAsync(db, pin);
                }
                catch
                {
                    // Mirror writes are best-effort and should not block the primary save.
                }
            }
        }

        private async Task<string?> TryReadPinFromDatabaseAsync(string dbPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath))
                    return null;

                var options = new DbContextOptionsBuilder<CashTrackerDbContext>()
                    .UseSqlite($"Data Source={dbPath}")
                    .Options;

                await using var db = new CashTrackerDbContext(options);
                var row = await db.AppSettings.FirstOrDefaultAsync(x => x.Key == PinKey);
                if (row == null)
                    return null;

                return TryReadStoredPin(row.Value, out var pin) ? pin : null;
            }
            catch
            {
                return null;
            }
        }

        private IEnumerable<string> EnumerateMirrorDbPaths()
        {
            return _databasePaths.MirrorDbPaths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Where(path => !string.Equals(path, _databasePaths.DbPath, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }
    }
}
