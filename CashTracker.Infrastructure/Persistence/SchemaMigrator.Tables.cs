using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Persistence
{
    public static partial class SchemaMigrator
    {
        private static partial void EnsureKasaTable(CashTrackerDbContext db, DbConnection conn)
        {
            if (TableExists(conn, "Kasa"))
                return;

            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS Kasa (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL DEFAULT 1,
    Tarih TEXT NOT NULL,
    Tip TEXT NOT NULL,
    Tutar NUMERIC NOT NULL DEFAULT 0,
    OdemeYontemi TEXT NOT NULL DEFAULT 'Nakit',
    Kalem TEXT,
    GiderTuru TEXT,
    Aciklama TEXT,
    CreatedAt TEXT NOT NULL
);");
        }

        private static partial void EnsureIsletmeTable(CashTrackerDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS Isletme (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Ad TEXT NOT NULL,
    IsAktif INTEGER NOT NULL DEFAULT 0,
    CreatedAt TEXT NOT NULL
);");
        }

        private static partial void EnsureKalemTanimiTable(CashTrackerDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS KalemTanimi (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    Tip TEXT NOT NULL,
    Ad TEXT NOT NULL,
    CreatedAt TEXT NOT NULL
);");
        }

        private static partial void EnsureAppSettingTable(CashTrackerDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS AppSetting (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Key TEXT NOT NULL,
    Value TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);");
        }

        private static partial void EnsureCariKartTable(CashTrackerDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS CariKart (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    Tip TEXT NOT NULL DEFAULT 'Musteri',
    Unvan TEXT NOT NULL,
    Telefon TEXT NOT NULL DEFAULT '',
    Eposta TEXT NOT NULL DEFAULT '',
    Adres TEXT NOT NULL DEFAULT '',
    VergiNoTc TEXT NOT NULL DEFAULT '',
    VergiDairesi TEXT NOT NULL DEFAULT '',
    Aktif INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);");
        }

        private static partial void EnsureCariHareketTable(CashTrackerDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS CariHareket (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    CariKartId INTEGER NOT NULL,
    Tarih TEXT NOT NULL,
    HareketTipi TEXT NOT NULL,
    Tutar NUMERIC NOT NULL DEFAULT 0,
    Kaynak TEXT NOT NULL DEFAULT 'Manuel',
    Aciklama TEXT,
    CreatedAt TEXT NOT NULL
);");
        }

        private static partial void EnsureUrunHizmetTable(CashTrackerDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS UrunHizmet (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    Tip TEXT NOT NULL DEFAULT 'Urun',
    Ad TEXT NOT NULL,
    Barkod TEXT NOT NULL DEFAULT '',
    Birim TEXT NOT NULL DEFAULT 'Adet',
    KdvOrani NUMERIC NOT NULL DEFAULT 20,
    AlisFiyati NUMERIC NOT NULL DEFAULT 0,
    SatisFiyati NUMERIC NOT NULL DEFAULT 0,
    KritikStok NUMERIC NOT NULL DEFAULT 0,
    Aktif INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);");
        }

        private static partial void EnsureStokHareketTable(CashTrackerDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS StokHareket (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    UrunHizmetId INTEGER NOT NULL,
    Tarih TEXT NOT NULL,
    Miktar NUMERIC NOT NULL DEFAULT 0,
    HareketTipi TEXT NOT NULL,
    Kaynak TEXT NOT NULL DEFAULT 'Manuel',
    Aciklama TEXT,
    CreatedAt TEXT NOT NULL
);");
        }

        private static partial void EnsureFaturaTable(CashTrackerDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS Fatura (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    CariKartId INTEGER NOT NULL,
    Tarih TEXT NOT NULL,
    VadeTarihi TEXT,
    FaturaTipi TEXT NOT NULL,
    Durum TEXT NOT NULL,
    YerelFaturaNo TEXT NOT NULL DEFAULT '',
    PortalBelgeNo TEXT NOT NULL DEFAULT '',
    PortalUuid TEXT NOT NULL DEFAULT '',
    AraToplam NUMERIC NOT NULL DEFAULT 0,
    IskontoToplam NUMERIC NOT NULL DEFAULT 0,
    KdvToplam NUMERIC NOT NULL DEFAULT 0,
    GenelToplam NUMERIC NOT NULL DEFAULT 0,
    OdenenTutar NUMERIC NOT NULL DEFAULT 0,
    OdemeYontemi TEXT NOT NULL DEFAULT 'Nakit',
    Aciklama TEXT,
    KesildiAt TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);");
        }

        private static partial void EnsureFaturaSatirTable(CashTrackerDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS FaturaSatir (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    FaturaId INTEGER NOT NULL,
    UrunHizmetId INTEGER,
    Aciklama TEXT NOT NULL,
    Birim TEXT NOT NULL DEFAULT 'Adet',
    Miktar NUMERIC NOT NULL DEFAULT 0,
    BirimFiyat NUMERIC NOT NULL DEFAULT 0,
    IskontoOrani NUMERIC NOT NULL DEFAULT 0,
    IskontoTutar NUMERIC NOT NULL DEFAULT 0,
    KdvOrani NUMERIC NOT NULL DEFAULT 20,
    KdvTutar NUMERIC NOT NULL DEFAULT 0,
    SatirNetTutar NUMERIC NOT NULL DEFAULT 0,
    SatirToplam NUMERIC NOT NULL DEFAULT 0,
    StokEtkilesin INTEGER NOT NULL DEFAULT 1
);");
        }

        private static partial void EnsureTahsilatOdemeTable(CashTrackerDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS TahsilatOdeme (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    FaturaId INTEGER NOT NULL,
    CariKartId INTEGER NOT NULL,
    Tarih TEXT NOT NULL,
    Tip TEXT NOT NULL,
    Tutar NUMERIC NOT NULL DEFAULT 0,
    OdemeYontemi TEXT NOT NULL DEFAULT 'Nakit',
    KasaId INTEGER,
    CariHareketId INTEGER,
    Aciklama TEXT,
    CreatedAt TEXT NOT NULL
);");
        }

        private static partial void EnsureBelgeDosyaTable(CashTrackerDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS BelgeDosya (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    FaturaId INTEGER NOT NULL,
    BelgeTipi TEXT NOT NULL DEFAULT 'PDF',
    DosyaYolu TEXT NOT NULL,
    Kaynak TEXT NOT NULL DEFAULT 'Yerel',
    CreatedAt TEXT NOT NULL
);");
        }

        private static partial void EnsureGibPortalAyarTable(CashTrackerDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS GibPortalAyar (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    KullaniciKodu TEXT NOT NULL DEFAULT '',
    SifreCipherText TEXT NOT NULL DEFAULT '',
    TestModu INTEGER NOT NULL DEFAULT 0,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);");
        }

        private static partial void EnsureGibPortalIslemLogTable(CashTrackerDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS GibPortalIslemLog (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    FaturaId INTEGER,
    Tarih TEXT NOT NULL,
    Islem TEXT NOT NULL,
    Basarili INTEGER NOT NULL DEFAULT 0,
    Mesaj TEXT NOT NULL DEFAULT ''
);");
        }

        private static partial void EnsureKasaColumns(CashTrackerDbContext db, DbConnection conn)
        {
            if (!ColumnExists(conn, "Kasa", "GiderTuru"))
                db.Database.ExecuteSqlRaw("ALTER TABLE Kasa ADD COLUMN GiderTuru TEXT");

            if (!ColumnExists(conn, "Kasa", "IsletmeId"))
                db.Database.ExecuteSqlRaw("ALTER TABLE Kasa ADD COLUMN IsletmeId INTEGER NOT NULL DEFAULT 1");

            if (!ColumnExists(conn, "Kasa", "Kalem"))
                db.Database.ExecuteSqlRaw("ALTER TABLE Kasa ADD COLUMN Kalem TEXT");

            if (!ColumnExists(conn, "Kasa", "OdemeYontemi"))
                db.Database.ExecuteSqlRaw("ALTER TABLE Kasa ADD COLUMN OdemeYontemi TEXT NOT NULL DEFAULT 'Nakit'");
        }

        private static partial void EnsureIndexes(CashTrackerDbContext db)
        {
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Kasa_IsletmeId ON Kasa(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Kasa_IsletmeId_Tarih ON Kasa(IsletmeId, Tarih);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Isletme_IsAktif ON Isletme(IsAktif);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_KalemTanimi_IsletmeId ON KalemTanimi(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_KalemTanimi_IsletmeId_Tip_Ad ON KalemTanimi(IsletmeId, Tip, Ad);");
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_AppSetting_Key ON AppSetting(Key);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_CariKart_IsletmeId ON CariKart(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_CariKart_IsletmeId_Unvan ON CariKart(IsletmeId, Unvan);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_CariKart_IsletmeId_VergiNoTc ON CariKart(IsletmeId, VergiNoTc);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_CariHareket_IsletmeId ON CariHareket(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_CariHareket_IsletmeId_CariKartId_Tarih ON CariHareket(IsletmeId, CariKartId, Tarih);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_UrunHizmet_IsletmeId ON UrunHizmet(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_UrunHizmet_IsletmeId_Barkod ON UrunHizmet(IsletmeId, Barkod);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_StokHareket_IsletmeId ON StokHareket(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_StokHareket_IsletmeId_UrunHizmetId_Tarih ON StokHareket(IsletmeId, UrunHizmetId, Tarih);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Fatura_IsletmeId ON Fatura(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Fatura_IsletmeId_Tarih ON Fatura(IsletmeId, Tarih);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Fatura_IsletmeId_CariKartId ON Fatura(IsletmeId, CariKartId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_FaturaSatir_IsletmeId ON FaturaSatir(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_FaturaSatir_IsletmeId_FaturaId ON FaturaSatir(IsletmeId, FaturaId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_TahsilatOdeme_IsletmeId ON TahsilatOdeme(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_TahsilatOdeme_IsletmeId_FaturaId ON TahsilatOdeme(IsletmeId, FaturaId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_TahsilatOdeme_IsletmeId_CariKartId_Tarih ON TahsilatOdeme(IsletmeId, CariKartId, Tarih);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_BelgeDosya_IsletmeId ON BelgeDosya(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_BelgeDosya_IsletmeId_FaturaId ON BelgeDosya(IsletmeId, FaturaId);");
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_GibPortalAyar_IsletmeId ON GibPortalAyar(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_GibPortalIslemLog_IsletmeId ON GibPortalIslemLog(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_GibPortalIslemLog_IsletmeId_FaturaId_Tarih ON GibPortalIslemLog(IsletmeId, FaturaId, Tarih);");
        }
    }
}
