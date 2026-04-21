using System;
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
    public sealed class TelegramStockSessionStore : ITelegramStockSessionStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };

        private static readonly TimeSpan SessionTimeout = TimeSpan.FromMinutes(30);
        private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;

        public TelegramStockSessionStore(IDbContextFactory<CashTrackerDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<TelegramStockSessionState?> GetAsync(long chatId, long userId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var row = await db.AppSettings.FirstOrDefaultAsync(x => x.Key == BuildKey(chatId, userId), ct);
            if (row == null || string.IsNullOrWhiteSpace(row.Value))
                return null;

            TelegramStockSessionState? state;
            try
            {
                state = JsonSerializer.Deserialize<TelegramStockSessionState>(row.Value, JsonOptions);
            }
            catch
            {
                db.AppSettings.Remove(row);
                await db.SaveChangesAsync(ct);
                return null;
            }

            if (state == null || state.UpdatedAtUtc < DateTime.UtcNow - SessionTimeout)
            {
                db.AppSettings.Remove(row);
                await db.SaveChangesAsync(ct);
                return null;
            }

            return state;
        }

        public async Task SaveAsync(TelegramStockSessionState state, CancellationToken ct = default)
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

        public async Task DeleteAsync(long chatId, long userId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var row = await db.AppSettings.FirstOrDefaultAsync(x => x.Key == BuildKey(chatId, userId), ct);
            if (row == null)
                return;

            db.AppSettings.Remove(row);
            await db.SaveChangesAsync(ct);
        }

        private static string BuildKey(long chatId, long userId)
        {
            return $"TelegramStockSession:{chatId}:{userId}";
        }
    }
}
