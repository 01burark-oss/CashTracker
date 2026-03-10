using System.Text.Json;
using CashTracker.Core.Entities;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
{
    Console.Error.WriteLine("Kullanim: CashTracker.DemoSeeder <appDataPath>");
    return 1;
}

var appDataPath = Path.GetFullPath(args[0].Trim());
Directory.CreateDirectory(appDataPath);

var dbPath = Path.Combine(appDataPath, "cashtracker.db");
var options = new DbContextOptionsBuilder<CashTrackerDbContext>()
    .UseSqlite($"Data Source={dbPath}")
    .Options;

await using var db = new CashTrackerDbContext(options);
await db.Database.EnsureDeletedAsync();
await db.Database.EnsureCreatedAsync();
SchemaMigrator.EnsureKasaSchema(db);

var random = new Random(20260310);
var now = DateTime.Now;

var businesses = new[]
{
    new Isletme { Ad = "Limon Kahve", IsAktif = true, CreatedAt = now.AddDays(-210) },
    new Isletme { Ad = "Kose Market", IsAktif = false, CreatedAt = now.AddDays(-180) },
    new Isletme { Ad = "Inci Butik", IsAktif = false, CreatedAt = now.AddDays(-140) }
};

db.Isletmeler.AddRange(businesses);
await db.SaveChangesAsync();

var gelirKalemleri = new[]
{
    "Kahve Satisi",
    "Tatli Satisi",
    "Paket Siparis",
    "Atolye Kaydi",
    "Kurumsal Satis",
    "Hizli Satis"
};

var giderKalemleri = new[]
{
    "Kira",
    "Personel",
    "Tedarik",
    "Elektrik",
    "Su",
    "Kargo",
    "Temizlik",
    "Reklam"
};

var descriptions = new[]
{
    "Gun sonu toplu giris",
    "Saha satisi",
    "Siparis kapandi",
    "Acil ihtiyac alimi",
    "Tedarik odemesi",
    "Hafta sonu yogunlugu",
    "Yuksek sepetli islem",
    "Promosyon kampanyasi"
};

var paymentMethods = new[]
{
    "Nakit",
    "KrediKarti",
    "OnlineOdeme",
    "Havale"
};

foreach (var business in businesses)
{
    db.KalemTanimlari.AddRange(
        gelirKalemleri.Select(name => new KalemTanimi
        {
            IsletmeId = business.Id,
            Tip = "Gelir",
            Ad = name,
            CreatedAt = now.AddDays(-120)
        }));

    db.KalemTanimlari.AddRange(
        giderKalemleri.Select(name => new KalemTanimi
        {
            IsletmeId = business.Id,
            Tip = "Gider",
            Ad = name,
            CreatedAt = now.AddDays(-120)
        }));
}

await db.SaveChangesAsync();

foreach (var business in businesses)
{
    var count = business.IsAktif ? 110 : 70;
    for (var i = 0; i < count; i++)
    {
        var isIncome = random.NextDouble() > 0.38;
        var date = now.Date
            .AddDays(-random.Next(0, 120))
            .AddHours(random.Next(8, 23))
            .AddMinutes(random.Next(0, 60));

        var amount = isIncome
            ? random.Next(90, 2800) + (decimal)(random.NextDouble() * 0.99)
            : random.Next(60, 1900) + (decimal)(random.NextDouble() * 0.99);

        var kalem = isIncome
            ? gelirKalemleri[random.Next(gelirKalemleri.Length)]
            : giderKalemleri[random.Next(giderKalemleri.Length)];

        db.Kasalar.Add(new Kasa
        {
            IsletmeId = business.Id,
            Tarih = date,
            Tip = isIncome ? "Gelir" : "Gider",
            Tutar = decimal.Round(amount, 2),
            OdemeYontemi = paymentMethods[random.Next(paymentMethods.Length)],
            Kalem = kalem,
            GiderTuru = isIncome ? null : kalem,
            Aciklama = descriptions[random.Next(descriptions.Length)],
            CreatedAt = date.AddMinutes(random.Next(1, 120))
        });
    }
}

db.AppSettings.Add(new AppSetting
{
    Key = "AppPin",
    Value = "1234",
    CreatedAt = now,
    UpdatedAt = now
});

await db.SaveChangesAsync();

var appState = new
{
    LastShortcutPromptVersion = string.Empty,
    LanguageCode = "tr",
    SummaryPrimaryRange = "last30days",
    SummarySecondaryRange = "last1year",
    HasCompletedOnboarding = true
};

await File.WriteAllTextAsync(
    Path.Combine(appDataPath, "app-state.json"),
    JsonSerializer.Serialize(appState, new JsonSerializerOptions { WriteIndented = true }));

var licenseRuntime = new
{
    InstallCode = string.Empty,
    TrialStartedAtUtc = (DateTime?)null,
    LastSeenAtUtc = now.ToUniversalTime(),
    LegacyExempt = true,
    TamperLocked = false,
    ActivatedAtUtc = (DateTime?)null,
    ActivatedLicenseId = string.Empty,
    UpdatedAtUtc = now.ToUniversalTime()
};

await File.WriteAllTextAsync(
    Path.Combine(appDataPath, "license-runtime.json"),
    JsonSerializer.Serialize(licenseRuntime, new JsonSerializerOptions { WriteIndented = true }));

var licensePath = Path.Combine(appDataPath, "license.json");
if (File.Exists(licensePath))
    File.Delete(licensePath);

Console.WriteLine($"DEMO_APPDATA={appDataPath}");
Console.WriteLine("DEMO_PIN=1234");
Console.WriteLine($"BUSINESS_COUNT={businesses.Length}");
Console.WriteLine($"TRANSACTION_COUNT={await db.Kasalar.CountAsync()}");
return 0;
