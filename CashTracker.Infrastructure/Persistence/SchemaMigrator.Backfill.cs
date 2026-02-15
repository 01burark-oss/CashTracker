using System;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Persistence
{
    public static partial class SchemaMigrator
    {
        private static partial int EnsureActiveBusiness(CashTrackerDbContext db, DbConnection conn)
        {
            var isletmeSayisi = ExecuteScalarInt(conn, "SELECT COUNT(1) FROM Isletme;");
            if (isletmeSayisi == 0)
            {
                db.Database.ExecuteSqlRaw(
                    "INSERT INTO Isletme (Ad, IsAktif, CreatedAt) VALUES ({0}, 1, {1});",
                    VarsayilanIsletmeAdi,
                    DateTime.Now);
                return ExecuteScalarInt(conn, "SELECT Id FROM Isletme WHERE IsAktif = 1 ORDER BY Id LIMIT 1;");
            }

            var activeCount = ExecuteScalarInt(conn, "SELECT COUNT(1) FROM Isletme WHERE IsAktif = 1;");
            if (activeCount == 0)
            {
                var firstId = ExecuteScalarInt(conn, "SELECT Id FROM Isletme ORDER BY Id LIMIT 1;");
                db.Database.ExecuteSqlRaw(
                    "UPDATE Isletme SET IsAktif = CASE WHEN Id = {0} THEN 1 ELSE 0 END;",
                    firstId);
                return firstId;
            }

            var keepId = ExecuteScalarInt(conn, "SELECT Id FROM Isletme WHERE IsAktif = 1 ORDER BY Id LIMIT 1;");
            if (activeCount > 1)
            {
                db.Database.ExecuteSqlRaw(
                    "UPDATE Isletme SET IsAktif = CASE WHEN Id = {0} THEN 1 ELSE 0 END;",
                    keepId);
            }

            return keepId;
        }

        private static partial void BackfillKasaBusiness(CashTrackerDbContext db, int activeIsletmeId)
        {
            db.Database.ExecuteSqlRaw(
                "UPDATE Kasa SET IsletmeId = {0} WHERE IsletmeId IS NULL OR IsletmeId = 0;",
                activeIsletmeId);
        }

        private static partial void BackfillKasaKalem(CashTrackerDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
UPDATE Kasa
SET Kalem = TRIM(GiderTuru)
WHERE (Kalem IS NULL OR TRIM(Kalem) = '')
  AND (Tip = 'Gider' OR Tip = 'Cikis')
  AND GiderTuru IS NOT NULL
  AND TRIM(GiderTuru) <> '';");

            db.Database.ExecuteSqlRaw(@"
UPDATE Kasa
SET Kalem = 'Genel Gider'
WHERE (Kalem IS NULL OR TRIM(Kalem) = '')
  AND (Tip = 'Gider' OR Tip = 'Cikis');");

            db.Database.ExecuteSqlRaw(@"
UPDATE Kasa
SET Kalem = 'Genel Gelir'
WHERE (Kalem IS NULL OR TRIM(Kalem) = '')
  AND (Tip = 'Gelir' OR Tip = 'Giris');");

            db.Database.ExecuteSqlRaw(@"
UPDATE Kasa
SET GiderTuru = Kalem
WHERE (Tip = 'Gider' OR Tip = 'Cikis')
  AND (GiderTuru IS NULL OR TRIM(GiderTuru) = '')
  AND Kalem IS NOT NULL
  AND TRIM(Kalem) <> '';");
        }

        private static partial void SeedKalemFromKasa(CashTrackerDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
INSERT INTO KalemTanimi (IsletmeId, Tip, Ad, CreatedAt)
SELECT DISTINCT k.IsletmeId, 'Gelir', TRIM(k.Kalem), CURRENT_TIMESTAMP
FROM Kasa k
WHERE (k.Tip = 'Gelir' OR k.Tip = 'Giris')
  AND k.Kalem IS NOT NULL
  AND TRIM(k.Kalem) <> ''
  AND NOT EXISTS (
      SELECT 1
      FROM KalemTanimi kt
      WHERE kt.IsletmeId = k.IsletmeId
        AND kt.Tip = 'Gelir'
        AND LOWER(kt.Ad) = LOWER(TRIM(k.Kalem))
  );");

            db.Database.ExecuteSqlRaw(@"
INSERT INTO KalemTanimi (IsletmeId, Tip, Ad, CreatedAt)
SELECT DISTINCT k.IsletmeId, 'Gider', TRIM(k.Kalem), CURRENT_TIMESTAMP
FROM Kasa k
WHERE (k.Tip = 'Gider' OR k.Tip = 'Cikis')
  AND k.Kalem IS NOT NULL
  AND TRIM(k.Kalem) <> ''
  AND NOT EXISTS (
      SELECT 1
      FROM KalemTanimi kt
      WHERE kt.IsletmeId = k.IsletmeId
        AND kt.Tip = 'Gider'
        AND LOWER(kt.Ad) = LOWER(TRIM(k.Kalem))
  );");
        }

        private static partial void EnsureDefaultKalemler(CashTrackerDbContext db, int isletmeId)
        {
            db.Database.ExecuteSqlRaw(@"
INSERT INTO KalemTanimi (IsletmeId, Tip, Ad, CreatedAt)
SELECT {0}, 'Gelir', 'Genel Gelir', CURRENT_TIMESTAMP
WHERE NOT EXISTS (
    SELECT 1 FROM KalemTanimi WHERE IsletmeId = {0} AND Tip = 'Gelir'
);", isletmeId);

            db.Database.ExecuteSqlRaw(@"
INSERT INTO KalemTanimi (IsletmeId, Tip, Ad, CreatedAt)
SELECT {0}, 'Gider', 'Genel Gider', CURRENT_TIMESTAMP
WHERE NOT EXISTS (
    SELECT 1 FROM KalemTanimi WHERE IsletmeId = {0} AND Tip = 'Gider'
);", isletmeId);
        }
    }
}
