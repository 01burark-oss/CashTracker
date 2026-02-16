using System;
using System.Linq;
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
                db.AppSettings.Add(new AppSetting
                {
                    Key = PinKey,
                    Value = DefaultPin,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                });
                await db.SaveChangesAsync();
                return DefaultPin;
            }

            if (!IsValidPin(row.Value))
            {
                row.Value = DefaultPin;
                row.UpdatedAt = DateTime.Now;
                await db.SaveChangesAsync();
                return DefaultPin;
            }

            return row.Value;
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
                    Value = normalizedPin,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                });
            }
            else
            {
                row.Value = normalizedPin;
                row.UpdatedAt = DateTime.Now;
            }

            await db.SaveChangesAsync();
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
