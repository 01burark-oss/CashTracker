using System.Globalization;
using CashTracker.App;
using CashTracker.App.Printing;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;

AppLocalization.SetLanguage("tr");

var repoRoot = ResolveRepoRoot();
var outputDir = Path.Combine(repoRoot, "artifacts", "demo-report");
Directory.CreateDirectory(outputDir);

var records = BuildRecords();
var from = new DateTime(2026, 3, 1);
var to = new DateTime(2026, 3, 31);
var summary = new PeriodSummary
{
    From = from,
    To = to,
    IncomeTotal = records.Where(x => AppLocalization.NormalizeTip(x.Tip) == "Gelir").Sum(x => x.Tutar),
    ExpenseTotal = records.Where(x => AppLocalization.NormalizeTip(x.Tip) == "Gider").Sum(x => x.Tutar),
    IncomeCount = records.Count(x => AppLocalization.NormalizeTip(x.Tip) == "Gelir"),
    ExpenseCount = records.Count(x => AppLocalization.NormalizeTip(x.Tip) == "Gider")
};

var accountingRequest = new PrintReportRequest
{
    Template = PrintReportTemplate.AccountingReport,
    From = from,
    To = to,
    RangeDisplay = "Mart 2026",
    GeneratedAt = new DateTime(2026, 3, 31, 18, 45, 0),
    Note = "Demo muhasebe raporu. Bu dosya yazdirma duzenini yazicisiz kontrol etmek icin olusturuldu.",
    IsPreview = false
};

var executiveRequest = new PrintReportRequest
{
    Template = PrintReportTemplate.ExecutiveSummary,
    From = from,
    To = to,
    RangeDisplay = "Mart 2026",
    GeneratedAt = accountingRequest.GeneratedAt,
    Note = "Aylik satislar guclu, kart ve online odemeler dengeli. Gider tarafinda kira ve personel one cikiyor.",
    IsPreview = false
};

var accountingReport = PrintReportComposer.Compose(accountingRequest, "Fabesco Demo Isletme", summary, records);
var executiveReport = PrintReportComposer.Compose(executiveRequest, "Fabesco Demo Isletme", summary, records);

var accountingPath = Path.Combine(outputDir, "cashtracker-demo-accounting.html");
var executivePath = Path.Combine(outputDir, "cashtracker-demo-executive.html");

File.WriteAllText(accountingPath, PrintReportHtmlExporter.Generate(accountingReport));
File.WriteAllText(executivePath, PrintReportHtmlExporter.Generate(executiveReport));

Console.WriteLine(accountingPath);
Console.WriteLine(executivePath);

static string ResolveRepoRoot()
{
    var current = new DirectoryInfo(AppContext.BaseDirectory);
    while (current is not null)
    {
        if (File.Exists(Path.Combine(current.FullName, "CashTracker.sln")))
        {
            return current.FullName;
        }

        current = current.Parent;
    }

    throw new InvalidOperationException("Repo root bulunamadi.");
}

static List<Kasa> BuildRecords()
{
    return
    [
        new Kasa { Id = 1, Tarih = new DateTime(2026, 3, 1), Tip = "Gelir", Tutar = 2450m, OdemeYontemi = "Nakit", Kalem = "Espresso", Aciklama = "Sabah satislari", CreatedAt = new DateTime(2026, 3, 1, 9, 5, 0) },
        new Kasa { Id = 2, Tarih = new DateTime(2026, 3, 2), Tip = "Gelir", Tutar = 3180m, OdemeYontemi = "KrediKarti", Kalem = "Tatli", Aciklama = "Ofis siparisleri", CreatedAt = new DateTime(2026, 3, 2, 14, 20, 0) },
        new Kasa { Id = 3, Tarih = new DateTime(2026, 3, 3), Tip = "Gider", Tutar = 4200m, OdemeYontemi = "Havale", Kalem = "Kira", Aciklama = "Mart kira odemesi", CreatedAt = new DateTime(2026, 3, 3, 11, 0, 0) },
        new Kasa { Id = 4, Tarih = new DateTime(2026, 3, 4), Tip = "Gelir", Tutar = 1675m, OdemeYontemi = "OnlineOdeme", Kalem = "Paket Servis", Aciklama = "Mobil siparisler", CreatedAt = new DateTime(2026, 3, 4, 20, 15, 0) },
        new Kasa { Id = 5, Tarih = new DateTime(2026, 3, 5), Tip = "Gider", Tutar = 680m, OdemeYontemi = "Nakit", Kalem = "Temizlik", Aciklama = "Sarj malzemeleri", CreatedAt = new DateTime(2026, 3, 5, 10, 30, 0) },
        new Kasa { Id = 6, Tarih = new DateTime(2026, 3, 7), Tip = "Gelir", Tutar = 2890m, OdemeYontemi = "Kredi Karti", Kalem = "Kahvalti", Aciklama = "Hafta sonu yogunlugu", CreatedAt = new DateTime(2026, 3, 7, 13, 10, 0) },
        new Kasa { Id = 7, Tarih = new DateTime(2026, 3, 9), Tip = "Gider", Tutar = 1250m, OdemeYontemi = "Havale", Kalem = "Elektrik", Aciklama = "Elektrik faturasi", CreatedAt = new DateTime(2026, 3, 9, 8, 45, 0) },
        new Kasa { Id = 8, Tarih = new DateTime(2026, 3, 12), Tip = "Gelir", Tutar = 3560m, OdemeYontemi = "Online Odeme", Kalem = "Kurumsal Paket", Aciklama = "Toplu siparis", CreatedAt = new DateTime(2026, 3, 12, 16, 55, 0) },
        new Kasa { Id = 9, Tarih = new DateTime(2026, 3, 14), Tip = "Gider", Tutar = 940m, OdemeYontemi = "KrediKarti", Kalem = "Bakim", Aciklama = "Kahve makinesi servisi", CreatedAt = new DateTime(2026, 3, 14, 12, 5, 0) },
        new Kasa { Id = 10, Tarih = new DateTime(2026, 3, 18), Tip = "Gelir", Tutar = 2140m, OdemeYontemi = "Nakit", Kalem = "Sandvic", Aciklama = "Oglen satislari", CreatedAt = new DateTime(2026, 3, 18, 15, 35, 0) },
        new Kasa { Id = 11, Tarih = new DateTime(2026, 3, 21), Tip = "Gider", Tutar = 2700m, OdemeYontemi = "Havale", Kalem = "Personel", Aciklama = "Avans odemesi", CreatedAt = new DateTime(2026, 3, 21, 18, 10, 0) },
        new Kasa { Id = 12, Tarih = new DateTime(2026, 3, 27), Tip = "Gelir", Tutar = 4025m, OdemeYontemi = "KrediKarti", Kalem = "Brunch", Aciklama = "Pazar gunu satislari", CreatedAt = new DateTime(2026, 3, 27, 17, 25, 0) }
    ];
}
