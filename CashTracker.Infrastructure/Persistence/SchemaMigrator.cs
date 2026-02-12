using System;
using System.Data.Common;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Persistence
{
    public static class SchemaMigrator
    {
        public static void EnsureKasaSchema(CashTrackerDbContext db)
        {
            var conn = db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                conn.Open();

            if (!TableExists(conn, "Kasa"))
            {
                db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS Kasa (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Tarih TEXT NOT NULL,
    Tip TEXT NOT NULL,
    Tutar NUMERIC NOT NULL DEFAULT 0,
    GiderTuru TEXT,
    Aciklama TEXT,
    CreatedAt TEXT NOT NULL
);");
                return;
            }

            if (!ColumnExists(conn, "Kasa", "GiderTuru"))
            {
                db.Database.ExecuteSqlRaw("ALTER TABLE Kasa ADD COLUMN GiderTuru TEXT");
            }
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
    }
}
