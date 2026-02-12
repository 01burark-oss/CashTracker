using System;
using System.IO;
using System.Threading.Tasks;
using CashTracker.Core.Models;
using Microsoft.Data.Sqlite;

namespace CashTracker.Infrastructure.Services
{
    public sealed class DatabaseBackupService
    {
        private readonly DatabasePaths _paths;

        public DatabaseBackupService(DatabasePaths paths)
        {
            _paths = paths;
        }

        public async Task<string> CreateBackupAsync()
        {
            if (string.IsNullOrWhiteSpace(_paths.DbPath))
                throw new InvalidOperationException("DbPath is empty.");

            var dir = Path.GetDirectoryName(_paths.DbPath)!;
            var backupDir = Path.Combine(dir, "backups");
            Directory.CreateDirectory(backupDir);

            var backupPath = Path.Combine(
                backupDir,
                $"cashtracker_{DateTime.Now:yyyyMMdd_HHmmss_fff}.db");

            const int maxAttempts = 3;
            SqliteException? lastSqliteError = null;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    if (File.Exists(backupPath))
                        File.Delete(backupPath);

                    await using var source = new SqliteConnection($"Data Source={_paths.DbPath};Mode=ReadOnly;Cache=Shared");
                    await using var destination = new SqliteConnection($"Data Source={backupPath}");

                    await source.OpenAsync();
                    await destination.OpenAsync();

                    source.BackupDatabase(destination);
                    return backupPath;
                }
                catch (SqliteException ex) when ((ex.SqliteErrorCode == 5 || ex.SqliteErrorCode == 6) && attempt < maxAttempts)
                {
                    lastSqliteError = ex;
                    await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt));
                }
            }

            throw new InvalidOperationException("Backup could not be completed due to database lock.", lastSqliteError);
        }
    }
}
