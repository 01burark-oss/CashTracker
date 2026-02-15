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
    public sealed class IsletmeService : IIsletmeService
    {
        private const string VarsayilanIsletmeAdi = "Mevcut Isletme";
        private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;

        public IsletmeService(IDbContextFactory<CashTrackerDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<List<Isletme>> GetAllAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            await EnsureActiveIsletmeAsync(db);
            return await db.Isletmeler
                .AsNoTracking()
                .OrderByDescending(x => x.IsAktif)
                .ThenBy(x => x.Ad)
                .ThenBy(x => x.Id)
                .ToListAsync();
        }

        public async Task<Isletme?> GetByIdAsync(int id)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Isletmeler.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Isletme> GetActiveAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var isletme = await EnsureActiveIsletmeAsync(db);
            await EnsureDefaultKalemlerAsync(db, isletme.Id);
            return isletme;
        }

        public async Task<int> GetActiveIdAsync()
        {
            var isletme = await GetActiveAsync();
            return isletme.Id;
        }

        public async Task<int> CreateAsync(string ad, bool makeActive = false)
        {
            var normalizedName = NormalizeBusinessName(ad);

            await using var db = await _dbFactory.CreateDbContextAsync();
            var hasActive = await db.Isletmeler.AnyAsync(x => x.IsAktif);

            if (makeActive)
            {
                var activeRows = await db.Isletmeler.Where(x => x.IsAktif).ToListAsync();
                foreach (var row in activeRows)
                    row.IsAktif = false;
            }

            var entity = new Isletme
            {
                Ad = normalizedName,
                IsAktif = makeActive || !hasActive,
                CreatedAt = DateTime.Now
            };

            db.Isletmeler.Add(entity);
            await db.SaveChangesAsync();
            await EnsureDefaultKalemlerAsync(db, entity.Id);

            return entity.Id;
        }

        public async Task RenameAsync(int id, string ad)
        {
            var normalizedName = NormalizeBusinessName(ad);

            await using var db = await _dbFactory.CreateDbContextAsync();
            var isletme = await db.Isletmeler.FirstOrDefaultAsync(x => x.Id == id);
            if (isletme == null)
                return;

            isletme.Ad = normalizedName;
            await db.SaveChangesAsync();
        }

        public async Task SetActiveAsync(int id)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var target = await db.Isletmeler.FirstOrDefaultAsync(x => x.Id == id);
            if (target == null)
                return;

            var activeRows = await db.Isletmeler.Where(x => x.IsAktif && x.Id != id).ToListAsync();
            foreach (var row in activeRows)
                row.IsAktif = false;

            target.IsAktif = true;
            await db.SaveChangesAsync();
            await EnsureDefaultKalemlerAsync(db, id);
        }

        private static string NormalizeBusinessName(string value)
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized))
                throw new ArgumentException("Isletme adi bos olamaz.", nameof(value));
            return normalized;
        }

        private static async Task<Isletme> EnsureActiveIsletmeAsync(CashTrackerDbContext db)
        {
            var active = await db.Isletmeler.FirstOrDefaultAsync(x => x.IsAktif);
            if (active != null)
                return active;

            var first = await db.Isletmeler.OrderBy(x => x.Id).FirstOrDefaultAsync();
            if (first != null)
            {
                first.IsAktif = true;
                await db.SaveChangesAsync();
                return first;
            }

            var created = new Isletme
            {
                Ad = VarsayilanIsletmeAdi,
                IsAktif = true,
                CreatedAt = DateTime.Now
            };

            db.Isletmeler.Add(created);
            await db.SaveChangesAsync();
            return created;
        }

        private static async Task EnsureDefaultKalemlerAsync(CashTrackerDbContext db, int isletmeId)
        {
            var gelirVar = await db.KalemTanimlari.AnyAsync(x => x.IsletmeId == isletmeId && x.Tip == "Gelir");
            var giderVar = await db.KalemTanimlari.AnyAsync(x => x.IsletmeId == isletmeId && x.Tip == "Gider");

            if (!gelirVar)
            {
                db.KalemTanimlari.Add(new KalemTanimi
                {
                    IsletmeId = isletmeId,
                    Tip = "Gelir",
                    Ad = "Genel Gelir",
                    CreatedAt = DateTime.Now
                });
            }

            if (!giderVar)
            {
                db.KalemTanimlari.Add(new KalemTanimi
                {
                    IsletmeId = isletmeId,
                    Tip = "Gider",
                    Ad = "Genel Gider",
                    CreatedAt = DateTime.Now
                });
            }

            if (!gelirVar || !giderVar)
                await db.SaveChangesAsync();
        }
    }
}
