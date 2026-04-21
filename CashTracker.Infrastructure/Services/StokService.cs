using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Services
{
    public sealed class StokService : IStokService
    {
        private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;
        private readonly IIsletmeService _isletmeService;

        public StokService(
            IDbContextFactory<CashTrackerDbContext> dbFactory,
            IIsletmeService isletmeService)
        {
            _dbFactory = dbFactory;
            _isletmeService = isletmeService;
        }

        public async Task<decimal> GetCurrentStockAsync(int urunHizmetId, CancellationToken ct = default)
        {
            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            return await db.StokHareketleri
                .AsNoTracking()
                .Where(x => x.IsletmeId == activeIsletmeId && x.UrunHizmetId == urunHizmetId)
                .SumAsync(x => x.Miktar, ct);
        }

        public async Task<StokHareketResult> CreateMovementAsync(
            StokHareketCreateRequest request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.UrunHizmetId <= 0)
                throw new ArgumentException("Urun secilmelidir.", nameof(request));

            if (request.Miktar == 0)
                throw new ArgumentException("Stok miktari sifir olamaz.", nameof(request));

            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var productExists = await db.UrunHizmetleri.AnyAsync(x =>
                x.Id == request.UrunHizmetId &&
                x.IsletmeId == activeIsletmeId &&
                x.Aktif,
                ct);

            if (!productExists)
                throw new InvalidOperationException("Urun aktif isletmede bulunamadi.");

            var movement = new StokHareket
            {
                IsletmeId = activeIsletmeId,
                UrunHizmetId = request.UrunHizmetId,
                Tarih = request.Tarih ?? DateTime.Now,
                Miktar = request.Miktar,
                HareketTipi = request.Miktar > 0 ? "Giris" : "Cikis",
                Kaynak = string.IsNullOrWhiteSpace(request.Kaynak) ? "Manuel" : request.Kaynak.Trim(),
                Aciklama = string.IsNullOrWhiteSpace(request.Aciklama) ? null : request.Aciklama.Trim(),
                CreatedAt = DateTime.Now
            };

            db.StokHareketleri.Add(movement);
            await db.SaveChangesAsync(ct);

            var current = await db.StokHareketleri
                .AsNoTracking()
                .Where(x => x.IsletmeId == activeIsletmeId && x.UrunHizmetId == request.UrunHizmetId)
                .SumAsync(x => x.Miktar, ct);

            return new StokHareketResult
            {
                Hareket = movement,
                MevcutStok = current
            };
        }
    }
}
