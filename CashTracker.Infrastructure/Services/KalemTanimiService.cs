using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CashTracker.Core.Entities;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Services
{
    public sealed class KalemTanimiService : IKalemTanimiService
    {
        private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;
        private readonly IIsletmeService _isletmeService;

        public KalemTanimiService(
            IDbContextFactory<CashTrackerDbContext> dbFactory,
            IIsletmeService isletmeService)
        {
            _dbFactory = dbFactory;
            _isletmeService = isletmeService;
        }

        public async Task<List<KalemTanimi>> GetAllAsync()
        {
            var activeId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync();

            return await db.KalemTanimlari
                .AsNoTracking()
                .Where(x => x.IsletmeId == activeId)
                .OrderBy(x => x.Tip)
                .ThenBy(x => x.Ad)
                .ToListAsync();
        }

        public async Task<List<KalemTanimi>> GetByTipAsync(string tip)
        {
            var normalizedTip = NormalizeTip(tip);
            var activeId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync();

            return await db.KalemTanimlari
                .AsNoTracking()
                .Where(x => x.IsletmeId == activeId && x.Tip == normalizedTip)
                .OrderBy(x => x.Ad)
                .ToListAsync();
        }

        public async Task<int> CreateAsync(string tip, string ad)
        {
            var normalizedTip = NormalizeTip(tip);
            var normalizedAd = NormalizeName(ad);
            var normalizedAdLower = normalizedAd.ToLowerInvariant();
            var activeId = await _isletmeService.GetActiveIdAsync();

            await using var db = await _dbFactory.CreateDbContextAsync();
            var existing = await db.KalemTanimlari
                .Where(x => x.IsletmeId == activeId && x.Tip == normalizedTip)
                .FirstOrDefaultAsync(x => x.Ad.ToLower() == normalizedAdLower);

            if (existing != null)
                return existing.Id;

            var entity = new KalemTanimi
            {
                IsletmeId = activeId,
                Tip = normalizedTip,
                Ad = normalizedAd,
                CreatedAt = DateTime.Now
            };

            db.KalemTanimlari.Add(entity);
            await db.SaveChangesAsync();
            return entity.Id;
        }

        public async Task DeleteAsync(int id)
        {
            var activeId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync();

            var row = await db.KalemTanimlari.FirstOrDefaultAsync(x => x.Id == id && x.IsletmeId == activeId);
            if (row == null)
                return;

            db.KalemTanimlari.Remove(row);
            await db.SaveChangesAsync();
        }

        private static string NormalizeName(string value)
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized))
                throw new ArgumentException("Kalem adi bos olamaz.", nameof(value));
            return normalized;
        }

        private static string NormalizeTip(string value)
        {
            var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
            return normalized switch
            {
                "gelir" => "Gelir",
                "giris" => "Gelir",
                "income" => "Gelir",
                "gider" => "Gider",
                "cikis" => "Gider",
                "expense" => "Gider",
                _ => throw new ArgumentException("Tip sadece gelir veya gider olabilir.", nameof(value))
            };
        }
    }
}
