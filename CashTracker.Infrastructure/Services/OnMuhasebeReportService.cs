using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Services
{
    public sealed class OnMuhasebeReportService : IOnMuhasebeReportService
    {
        private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;
        private readonly IIsletmeService _isletmeService;

        public OnMuhasebeReportService(
            IDbContextFactory<CashTrackerDbContext> dbFactory,
            IIsletmeService isletmeService)
        {
            _dbFactory = dbFactory;
            _isletmeService = isletmeService;
        }

        public async Task<string> CreateMonthlyExportAsync(DateTime month, string outputDirectory, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(outputDirectory))
                throw new ArgumentException("Cikti klasoru secilmelidir.", nameof(outputDirectory));

            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();
            var activeBusiness = await _isletmeService.GetActiveAsync();
            var start = new DateTime(month.Year, month.Month, 1);
            var end = start.AddMonths(1);
            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            var exportRoot = Path.Combine(outputDirectory, $"CashTracker_OnMuhasebe_{start:yyyy_MM}_{stamp}");
            Directory.CreateDirectory(exportRoot);

            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var faturalar = await db.Faturalar
                .AsNoTracking()
                .Where(x => x.IsletmeId == activeIsletmeId && x.Tarih >= start && x.Tarih < end)
                .OrderBy(x => x.Tarih)
                .ThenBy(x => x.Id)
                .ToListAsync(ct);
            var faturaIds = faturalar.Select(x => x.Id).ToArray();
            var faturaSatirlari = await db.FaturaSatirlari
                .AsNoTracking()
                .Where(x => x.IsletmeId == activeIsletmeId && faturaIds.Contains(x.FaturaId))
                .OrderBy(x => x.FaturaId)
                .ThenBy(x => x.Id)
                .ToListAsync(ct);
            var cariKartlari = await db.CariKartlari
                .AsNoTracking()
                .Where(x => x.IsletmeId == activeIsletmeId)
                .OrderBy(x => x.Unvan)
                .ToListAsync(ct);
            var cariHareketleri = await db.CariHareketleri
                .AsNoTracking()
                .Where(x => x.IsletmeId == activeIsletmeId && x.Tarih >= start && x.Tarih < end)
                .OrderBy(x => x.Tarih)
                .ThenBy(x => x.Id)
                .ToListAsync(ct);
            var urunler = await db.UrunHizmetleri
                .AsNoTracking()
                .Where(x => x.IsletmeId == activeIsletmeId)
                .OrderBy(x => x.Ad)
                .ToListAsync(ct);
            var stokHareketleri = await db.StokHareketleri
                .AsNoTracking()
                .Where(x => x.IsletmeId == activeIsletmeId && x.Tarih >= start && x.Tarih < end)
                .OrderBy(x => x.Tarih)
                .ThenBy(x => x.Id)
                .ToListAsync(ct);
            var kasaKayitlari = await db.Kasalar
                .AsNoTracking()
                .Where(x => x.IsletmeId == activeIsletmeId && x.Tarih >= start && x.Tarih < end)
                .OrderBy(x => x.Tarih)
                .ThenBy(x => x.Id)
                .ToListAsync(ct);
            var belgeDosyalari = await db.BelgeDosyalari
                .AsNoTracking()
                .Where(x => x.IsletmeId == activeIsletmeId && faturaIds.Contains(x.FaturaId))
                .ToListAsync(ct);

            await WriteCsvAsync(
                Path.Combine(exportRoot, "faturalar.csv"),
                ["Id", "Tarih", "Tip", "Durum", "YerelNo", "PortalNo", "Uuid", "CariId", "AraToplam", "Iskonto", "Kdv", "GenelToplam", "Odenen", "OdemeYontemi", "Aciklama"],
                faturalar.Select(x => new[]
                {
                    x.Id.ToString(CultureInfo.InvariantCulture),
                    x.Tarih.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    x.FaturaTipi,
                    x.Durum,
                    x.YerelFaturaNo,
                    x.PortalBelgeNo,
                    x.PortalUuid,
                    x.CariKartId.ToString(CultureInfo.InvariantCulture),
                    Money(x.AraToplam),
                    Money(x.IskontoToplam),
                    Money(x.KdvToplam),
                    Money(x.GenelToplam),
                    Money(x.OdenenTutar),
                    x.OdemeYontemi,
                    x.Aciklama ?? string.Empty
                }),
                ct);

            await WriteCsvAsync(
                Path.Combine(exportRoot, "fatura_satirlari.csv"),
                ["FaturaId", "UrunHizmetId", "Aciklama", "Birim", "Miktar", "BirimFiyat", "IskontoOrani", "KdvOrani", "KdvTutar", "SatirToplam"],
                faturaSatirlari.Select(x => new[]
                {
                    x.FaturaId.ToString(CultureInfo.InvariantCulture),
                    x.UrunHizmetId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                    x.Aciklama,
                    x.Birim,
                    Money(x.Miktar),
                    Money(x.BirimFiyat),
                    Money(x.IskontoOrani),
                    Money(x.KdvOrani),
                    Money(x.KdvTutar),
                    Money(x.SatirToplam)
                }),
                ct);

            await WriteCsvAsync(
                Path.Combine(exportRoot, "cari_hareketler.csv"),
                ["Id", "CariId", "Tarih", "HareketTipi", "Tutar", "Kaynak", "Aciklama"],
                cariHareketleri.Select(x => new[]
                {
                    x.Id.ToString(CultureInfo.InvariantCulture),
                    x.CariKartId.ToString(CultureInfo.InvariantCulture),
                    x.Tarih.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    x.HareketTipi,
                    Money(x.Tutar),
                    x.Kaynak,
                    x.Aciklama ?? string.Empty
                }),
                ct);

            await WriteCsvAsync(
                Path.Combine(exportRoot, "cari_kartlar.csv"),
                ["Id", "Tip", "Unvan", "Telefon", "Eposta", "VergiNoTc", "VergiDairesi", "Aktif"],
                cariKartlari.Select(x => new[]
                {
                    x.Id.ToString(CultureInfo.InvariantCulture),
                    x.Tip,
                    x.Unvan,
                    x.Telefon,
                    x.Eposta,
                    x.VergiNoTc,
                    x.VergiDairesi,
                    x.Aktif ? "Evet" : "Hayir"
                }),
                ct);

            await WriteCsvAsync(
                Path.Combine(exportRoot, "stok_hareketleri.csv"),
                ["Id", "UrunHizmetId", "Tarih", "HareketTipi", "Miktar", "Kaynak", "Aciklama"],
                stokHareketleri.Select(x => new[]
                {
                    x.Id.ToString(CultureInfo.InvariantCulture),
                    x.UrunHizmetId.ToString(CultureInfo.InvariantCulture),
                    x.Tarih.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    x.HareketTipi,
                    Money(x.Miktar),
                    x.Kaynak,
                    x.Aciklama ?? string.Empty
                }),
                ct);

            await WriteCsvAsync(
                Path.Combine(exportRoot, "urun_hizmetler.csv"),
                ["Id", "Tip", "Ad", "Barkod", "Birim", "KdvOrani", "AlisFiyati", "SatisFiyati", "KritikStok", "Aktif"],
                urunler.Select(x => new[]
                {
                    x.Id.ToString(CultureInfo.InvariantCulture),
                    x.Tip,
                    x.Ad,
                    x.Barkod,
                    x.Birim,
                    Money(x.KdvOrani),
                    Money(x.AlisFiyati),
                    Money(x.SatisFiyati),
                    Money(x.KritikStok),
                    x.Aktif ? "Evet" : "Hayir"
                }),
                ct);

            await WriteCsvAsync(
                Path.Combine(exportRoot, "gelir_gider.csv"),
                ["Id", "Tarih", "Tip", "Tutar", "OdemeYontemi", "Kalem", "GiderTuru", "Aciklama"],
                kasaKayitlari.Select(x => new[]
                {
                    x.Id.ToString(CultureInfo.InvariantCulture),
                    x.Tarih.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    x.Tip,
                    Money(x.Tutar),
                    x.OdemeYontemi,
                    x.Kalem ?? string.Empty,
                    x.GiderTuru ?? string.Empty,
                    x.Aciklama ?? string.Empty
                }),
                ct);

            var kdvRows = faturaSatirlari
                .GroupJoin(faturalar, row => row.FaturaId, invoice => invoice.Id, (row, invoice) => new { Row = row, Invoice = invoice.FirstOrDefault() })
                .Where(x => x.Invoice != null)
                .GroupBy(x => new { x.Invoice!.FaturaTipi, x.Row.KdvOrani })
                .OrderBy(x => x.Key.FaturaTipi)
                .ThenBy(x => x.Key.KdvOrani)
                .Select(x => new[]
                {
                    x.Key.FaturaTipi,
                    Money(x.Key.KdvOrani),
                    Money(x.Sum(y => y.Row.SatirNetTutar)),
                    Money(x.Sum(y => y.Row.KdvTutar)),
                    Money(x.Sum(y => y.Row.SatirToplam))
                });
            await WriteCsvAsync(Path.Combine(exportRoot, "kdv_ozeti.csv"), ["Tip", "KdvOrani", "Matrah", "Kdv", "Toplam"], kdvRows, ct);

            var belgeRoot = Path.Combine(exportRoot, "belge_dosyalari");
            Directory.CreateDirectory(belgeRoot);
            foreach (var belge in belgeDosyalari)
            {
                if (string.IsNullOrWhiteSpace(belge.DosyaYolu) || !File.Exists(belge.DosyaYolu))
                    continue;

                var safeName = $"{belge.FaturaId}_{belge.BelgeTipi}_{Path.GetFileName(belge.DosyaYolu)}";
                File.Copy(belge.DosyaYolu, Path.Combine(belgeRoot, safeName), overwrite: true);
            }

            await WriteSummaryHtmlAsync(Path.Combine(exportRoot, "ozet.html"), activeBusiness?.Ad ?? "Isletme", start, faturalar.Count, kasaKayitlari.Count, ct);

            var zipPath = exportRoot + ".zip";
            if (File.Exists(zipPath))
                File.Delete(zipPath);

            ZipFile.CreateFromDirectory(exportRoot, zipPath, CompressionLevel.Fastest, includeBaseDirectory: true);
            return zipPath;
        }

        private static async Task WriteCsvAsync(string path, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows, CancellationToken ct)
        {
            await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
            await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            await writer.WriteLineAsync(string.Join(";", headers.Select(EscapeCsv)).AsMemory(), ct);
            foreach (var row in rows)
                await writer.WriteLineAsync(string.Join(";", row.Select(EscapeCsv)).AsMemory(), ct);
        }

        private static async Task WriteSummaryHtmlAsync(string path, string businessName, DateTime month, int invoiceCount, int cashCount, CancellationToken ct)
        {
            var html = $$"""
<!doctype html>
<html lang="tr">
<head>
<meta charset="utf-8">
<title>CashTracker On Muhasebe Ozeti</title>
<style>
body { font-family: Arial, sans-serif; color: #162033; margin: 32px; }
table { border-collapse: collapse; min-width: 520px; }
td, th { border: 1px solid #ccd5e1; padding: 8px 10px; }
th { background: #eef4fb; text-align: left; }
</style>
</head>
<body>
<h1>CashTracker On Muhasebe Ozeti</h1>
<table>
<tr><th>Isletme</th><td>{{EscapeHtml(businessName)}}</td></tr>
<tr><th>Donem</th><td>{{month:yyyy-MM}}</td></tr>
<tr><th>Fatura Sayisi</th><td>{{invoiceCount}}</td></tr>
<tr><th>Gelir/Gider Kaydi</th><td>{{cashCount}}</td></tr>
</table>
<p>CSV dosyalari Excel ile acilabilir. Varsa GIB PDF/XML belgeleri belge_dosyalari klasorune eklendi.</p>
</body>
</html>
""";
            await File.WriteAllTextAsync(path, html, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), ct);
        }

        private static string EscapeCsv(string value)
        {
            var clean = value.Replace('\r', ' ').Replace('\n', ' ').Trim();
            return clean.Contains(';') || clean.Contains('"')
                ? $"\"{clean.Replace("\"", "\"\"")}\""
                : clean;
        }

        private static string EscapeHtml(string value)
        {
            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
        }

        private static string Money(decimal value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
