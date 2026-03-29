using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Services
{
    public sealed class TelegramReceiptSessionStore : ITelegramReceiptSessionStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;
        private readonly ReceiptOcrSettings _settings;

        public TelegramReceiptSessionStore(
            IDbContextFactory<CashTrackerDbContext> dbFactory,
            ReceiptOcrSettings settings)
        {
            _dbFactory = dbFactory;
            _settings = settings;
        }

        public async Task<TelegramReceiptSessionState?> GetAsync(
            long chatId,
            long userId,
            CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var row = await db.AppSettings.FirstOrDefaultAsync(x => x.Key == BuildKey(chatId, userId), ct);
            if (row == null || string.IsNullOrWhiteSpace(row.Value))
                return null;

            TelegramReceiptSessionState? state;
            try
            {
                state = JsonSerializer.Deserialize<TelegramReceiptSessionState>(row.Value, JsonOptions);
            }
            catch
            {
                db.AppSettings.Remove(row);
                await db.SaveChangesAsync(ct);
                return null;
            }

            if (state == null)
            {
                db.AppSettings.Remove(row);
                await db.SaveChangesAsync(ct);
                return null;
            }

            var cutoff = DateTime.UtcNow - _settings.GetSessionTimeout();
            if (state.UpdatedAtUtc < cutoff)
            {
                CleanupTempFile(state.TempFilePath);
                db.AppSettings.Remove(row);
                await db.SaveChangesAsync(ct);
                return null;
            }

            return state;
        }

        public async Task SaveAsync(
            TelegramReceiptSessionState state,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(state);

            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var key = BuildKey(state.ChatId, state.UserId);
            var row = await db.AppSettings.FirstOrDefaultAsync(x => x.Key == key, ct);
            var now = DateTime.UtcNow;

            if (state.CreatedAtUtc == default)
                state.CreatedAtUtc = now;
            state.UpdatedAtUtc = now;

            var value = JsonSerializer.Serialize(state, JsonOptions);
            if (row == null)
            {
                db.AppSettings.Add(new AppSetting
                {
                    Key = key,
                    Value = value,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                });
            }
            else
            {
                row.Value = value;
                row.UpdatedAt = DateTime.Now;
            }

            await db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(
            long chatId,
            long userId,
            CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var row = await db.AppSettings.FirstOrDefaultAsync(x => x.Key == BuildKey(chatId, userId), ct);
            if (row == null)
                return;

            try
            {
                var state = JsonSerializer.Deserialize<TelegramReceiptSessionState>(row.Value, JsonOptions);
                if (state != null)
                    CleanupTempFile(state.TempFilePath);
            }
            catch
            {
            }

            db.AppSettings.Remove(row);
            await db.SaveChangesAsync(ct);
        }

        private static string BuildKey(long chatId, long userId)
        {
            return $"TelegramReceiptSession:{chatId}:{userId}";
        }

        private static void CleanupTempFile(string? path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return;

            try
            {
                File.Delete(path);
            }
            catch
            {
            }
        }
    }
}
