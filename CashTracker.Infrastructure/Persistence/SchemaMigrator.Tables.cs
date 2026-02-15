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

        private static partial void EnsureKasaColumns(CashTrackerDbContext db, DbConnection conn)
        {
            if (!ColumnExists(conn, "Kasa", "GiderTuru"))
                db.Database.ExecuteSqlRaw("ALTER TABLE Kasa ADD COLUMN GiderTuru TEXT");

            if (!ColumnExists(conn, "Kasa", "IsletmeId"))
                db.Database.ExecuteSqlRaw("ALTER TABLE Kasa ADD COLUMN IsletmeId INTEGER NOT NULL DEFAULT 1");

            if (!ColumnExists(conn, "Kasa", "Kalem"))
                db.Database.ExecuteSqlRaw("ALTER TABLE Kasa ADD COLUMN Kalem TEXT");
        }

        private static partial void EnsureIndexes(CashTrackerDbContext db)
        {
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Kasa_IsletmeId ON Kasa(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Kasa_IsletmeId_Tarih ON Kasa(IsletmeId, Tarih);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Isletme_IsAktif ON Isletme(IsAktif);");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_KalemTanimi_IsletmeId ON KalemTanimi(IsletmeId);");
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_KalemTanimi_IsletmeId_Tip_Ad ON KalemTanimi(IsletmeId, Tip, Ad);");
        }
    }
}
