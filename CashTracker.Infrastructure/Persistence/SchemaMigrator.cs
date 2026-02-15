using System;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Persistence
{
    public static partial class SchemaMigrator
    {
        private const string VarsayilanIsletmeAdi = "Mevcut Isletme";

        public static void EnsureKasaSchema(CashTrackerDbContext db)
        {
            var conn = db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                conn.Open();

            EnsureKasaTable(db, conn);
            EnsureIsletmeTable(db);
            EnsureKalemTanimiTable(db);

            EnsureKasaColumns(db, conn);
            EnsureIndexes(db);

            var activeIsletmeId = EnsureActiveBusiness(db, conn);
            BackfillKasaBusiness(db, activeIsletmeId);
            BackfillKasaKalem(db);
            SeedKalemFromKasa(db);
            EnsureDefaultKalemler(db, activeIsletmeId);
        }

        private static bool TableExists(DbConnection conn, string tableName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name=$name;";
            var p = cmd.CreateParameter();
            p.ParameterName = "$name";
            p.Value = tableName;
            cmd.Parameters.Add(p);
            var result = cmd.ExecuteScalar();
            return result != null && result != DBNull.Value;
        }

        private static bool ColumnExists(DbConnection conn, string tableName, string columnName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info({tableName});";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var name = reader.GetString(1);
                if (string.Equals(name, columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static int ExecuteScalarInt(DbConnection conn, string sql)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            var result = cmd.ExecuteScalar();
            if (result is null || result == DBNull.Value)
                return 0;
            return Convert.ToInt32(result);
        }

        private static partial void EnsureKasaTable(CashTrackerDbContext db, DbConnection conn);
        private static partial void EnsureIsletmeTable(CashTrackerDbContext db);
        private static partial void EnsureKalemTanimiTable(CashTrackerDbContext db);
        private static partial void EnsureKasaColumns(CashTrackerDbContext db, DbConnection conn);
        private static partial void EnsureIndexes(CashTrackerDbContext db);
        private static partial int EnsureActiveBusiness(CashTrackerDbContext db, DbConnection conn);
        private static partial void BackfillKasaBusiness(CashTrackerDbContext db, int activeIsletmeId);
        private static partial void BackfillKasaKalem(CashTrackerDbContext db);
        private static partial void SeedKalemFromKasa(CashTrackerDbContext db);
        private static partial void EnsureDefaultKalemler(CashTrackerDbContext db, int isletmeId);
    }
}
