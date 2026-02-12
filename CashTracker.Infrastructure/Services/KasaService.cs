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
    public sealed class KasaService : IKasaService
    {
        private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;

        public KasaService(IDbContextFactory<CashTrackerDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<List<Kasa>> GetAllAsync(DateTime? from = null, DateTime? to = null)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var query = db.Kasalar.AsNoTracking();

            if (from.HasValue) query = query.Where(x => x.Tarih >= from.Value.Date);
            if (to.HasValue)
            {
                var endExclusive = to.Value.Date.AddDays(1);
                query = query.Where(x => x.Tarih < endExclusive);
            }

            return await query.OrderByDescending(x => x.Tarih).ToListAsync();
        }

        public async Task<Kasa?> GetByIdAsync(int id)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Kasalar.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<int> CreateAsync(Kasa kasa)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            kasa.CreatedAt = DateTime.Now;
            db.Kasalar.Add(kasa);
            await db.SaveChangesAsync();

            return kasa.Id;
        }

        public async Task UpdateAsync(Kasa kasa)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var existing = await db.Kasalar.FirstOrDefaultAsync(x => x.Id == kasa.Id);
            if (existing == null)
                return;

            // Preserve CreatedAt while updating editable fields.
            existing.Tarih = kasa.Tarih;
            existing.Tip = kasa.Tip;
            existing.Tutar = kasa.Tutar;
            existing.GiderTuru = kasa.GiderTuru;
            existing.Aciklama = kasa.Aciklama;
            await db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var kasa = await db.Kasalar.FirstOrDefaultAsync(x => x.Id == id);
            if (kasa == null) return;

            db.Kasalar.Remove(kasa);
            await db.SaveChangesAsync();
        }
    }
}
