using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CashTracker.Core.Utilities;
using Microsoft.Data.Sqlite;

namespace CashTracker.LicenseAdmin;

internal sealed class IssuedLicenseRecord
{
    public int Id { get; set; }
    public string LicenseId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string InstallCode { get; set; } = string.Empty;
    public string InstallCodeHash { get; set; } = string.Empty;
    public string Edition { get; set; } = string.Empty;
    public string LicenseKey { get; set; } = string.Empty;
    public string LicenseKeyFingerprint { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Status { get; set; } = "Issued";
    public DateTime IssuedAtUtc { get; set; }
}

internal sealed class LicensePayload
{
    public string LicenseId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string InstallCodeHash { get; set; } = string.Empty;
    public DateTime IssuedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public string Edition { get; set; } = string.Empty;
    public string ReceiptOcrProvider { get; set; } = string.Empty;
    public string ReceiptOcrModel { get; set; } = string.Empty;
    public string EncryptedReceiptOcrApiKey { get; set; } = string.Empty;
}

internal sealed class LicenseLedgerService
{
    private readonly string _dbPath;

    public LicenseLedgerService(string appDataPath)
    {
        Directory.CreateDirectory(appDataPath);
        _dbPath = Path.Combine(appDataPath, "license-admin.db");
        EnsureDatabase();
    }

    public IssuedLicenseRecord? FindByInstallCodeHash(string installCodeHash)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, LicenseId, CustomerName, InstallCode, InstallCodeHash, Edition, LicenseKey, LicenseKeyFingerprint, Notes, Status, IssuedAtUtc
            FROM IssuedLicenses
            WHERE InstallCodeHash = $installCodeHash
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$installCodeHash", installCodeHash);

        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadRecord(reader) : null;
    }

    public List<IssuedLicenseRecord> GetAll()
    {
        var results = new List<IssuedLicenseRecord>();
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, LicenseId, CustomerName, InstallCode, InstallCodeHash, Edition, LicenseKey, LicenseKeyFingerprint, Notes, Status, IssuedAtUtc
            FROM IssuedLicenses
            ORDER BY IssuedAtUtc DESC;
            """;

        using var reader = command.ExecuteReader();
        while (reader.Read())
            results.Add(ReadRecord(reader));

        return results;
    }

    public void Save(IssuedLicenseRecord record)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO IssuedLicenses
                (LicenseId, CustomerName, InstallCode, InstallCodeHash, Edition, LicenseKey, LicenseKeyFingerprint, Notes, Status, IssuedAtUtc)
            VALUES
                ($licenseId, $customerName, $installCode, $installCodeHash, $edition, $licenseKey, $licenseKeyFingerprint, $notes, $status, $issuedAtUtc);
            """;
        command.Parameters.AddWithValue("$licenseId", record.LicenseId);
        command.Parameters.AddWithValue("$customerName", record.CustomerName);
        command.Parameters.AddWithValue("$installCode", record.InstallCode);
        command.Parameters.AddWithValue("$installCodeHash", record.InstallCodeHash);
        command.Parameters.AddWithValue("$edition", record.Edition);
        command.Parameters.AddWithValue("$licenseKey", record.LicenseKey);
        command.Parameters.AddWithValue("$licenseKeyFingerprint", record.LicenseKeyFingerprint);
        command.Parameters.AddWithValue("$notes", record.Notes);
        command.Parameters.AddWithValue("$status", record.Status);
        command.Parameters.AddWithValue("$issuedAtUtc", record.IssuedAtUtc.ToString("O"));
        command.ExecuteNonQuery();
    }

    private void EnsureDatabase()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS IssuedLicenses (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                LicenseId TEXT NOT NULL,
                CustomerName TEXT NOT NULL,
                InstallCode TEXT NOT NULL,
                InstallCodeHash TEXT NOT NULL UNIQUE,
                Edition TEXT NOT NULL,
                LicenseKey TEXT NOT NULL,
                LicenseKeyFingerprint TEXT NOT NULL,
                Notes TEXT NOT NULL DEFAULT '',
                Status TEXT NOT NULL DEFAULT 'Issued',
                IssuedAtUtc TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();
        return connection;
    }

    private static IssuedLicenseRecord ReadRecord(SqliteDataReader reader)
    {
        return new IssuedLicenseRecord
        {
            Id = reader.GetInt32(0),
            LicenseId = reader.GetString(1),
            CustomerName = reader.GetString(2),
            InstallCode = reader.GetString(3),
            InstallCodeHash = reader.GetString(4),
            Edition = reader.GetString(5),
            LicenseKey = reader.GetString(6),
            LicenseKeyFingerprint = reader.GetString(7),
            Notes = reader.GetString(8),
            Status = reader.GetString(9),
            IssuedAtUtc = DateTime.Parse(reader.GetString(10)).ToUniversalTime()
        };
    }
}

internal sealed class LicenseKeyIssuer
{
    public string CreateLicenseKey(
        string customerName,
        string installCode,
        string licenseId,
        string edition,
        string privateKeyPath,
        string? receiptOcrApiKey = null,
        string? receiptOcrProvider = null,
        string? receiptOcrModel = null)
    {
        var hasReceiptOcrSecret = !string.IsNullOrWhiteSpace(receiptOcrApiKey);
        var payload = new LicensePayload
        {
            LicenseId = licenseId,
            CustomerName = customerName.Trim(),
            InstallCodeHash = ComputeInstallCodeHash(installCode),
            IssuedAtUtc = DateTime.UtcNow,
            Edition = edition.Trim(),
            ReceiptOcrProvider = hasReceiptOcrSecret ? (receiptOcrProvider?.Trim() ?? "Gemini") : string.Empty,
            ReceiptOcrModel = hasReceiptOcrSecret ? (receiptOcrModel?.Trim() ?? "gemini-2.5-flash") : string.Empty,
            EncryptedReceiptOcrApiKey = hasReceiptOcrSecret
                ? InstallScopedSecretProtector.Protect(receiptOcrApiKey!, installCode)
                : string.Empty
        };

        var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(payload);
        using var rsa = LoadPrivateKey(privateKeyPath);
        var signatureBytes = rsa.SignData(payloadBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return $"CT1.{Base64UrlEncode(payloadBytes)}.{Base64UrlEncode(signatureBytes)}";
    }

    public static string ComputeInstallCodeHash(string installCode)
    {
        var bytes = Encoding.UTF8.GetBytes(InstallCodeFormat.Normalize(installCode));
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string ComputeLicenseFingerprint(string licenseKey)
    {
        var bytes = Encoding.UTF8.GetBytes(licenseKey ?? string.Empty);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant()[..16];
    }

    private static RSA LoadPrivateKey(string path)
    {
        var content = File.ReadAllText(path).Trim();
        var rsa = RSA.Create();
        if (content.StartsWith("<", StringComparison.Ordinal))
        {
            rsa.FromXmlString(content);
            return rsa;
        }

        rsa.ImportFromPem(content);
        return rsa;
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}

internal static class LicenseAdminRuntime
{
    public static string GetAppDataPath()
    {
        return LicenseAdminPathResolver.Resolve();
    }

    public static string CreateLicenseId()
    {
        return $"LIC-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..28].ToUpperInvariant();
    }

    public static string TryResolvePrivateKeyPath()
    {
        var appDataPath = GetAppDataPath();
        var settings = LicenseAdminSettingsStore.Load(appDataPath);
        foreach (var candidate in GetPrivateKeyCandidates(settings.PrivateKeyPath))
        {
            if (!string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate))
                return Path.GetFullPath(candidate);
        }

        return string.Empty;
    }

    public static void RememberPrivateKeyPath(string privateKeyPath)
    {
        if (string.IsNullOrWhiteSpace(privateKeyPath))
            return;

        var appDataPath = GetAppDataPath();
        LicenseAdminSettingsStore.Save(appDataPath, new LicenseAdminSettings
        {
            PrivateKeyPath = privateKeyPath.Trim()
        });
    }

    private static string?[] GetPrivateKeyCandidates(string? rememberedPath)
    {
        var baseDir = AppContext.BaseDirectory;
        var repoRoot = ResolveRepoRoot(baseDir);
        return new[]
        {
            Environment.GetEnvironmentVariable("CASHTRACKER_LICENSE_PRIVATE_KEY_PATH"),
            rememberedPath,
            !string.IsNullOrWhiteSpace(repoRoot) ? Path.Combine(repoRoot, ".local", "license-private-key.xml") : string.Empty,
            !string.IsNullOrWhiteSpace(repoRoot) ? Path.Combine(repoRoot, ".local", "license-private-key.pem") : string.Empty,
            !string.IsNullOrWhiteSpace(repoRoot) ? Path.Combine(repoRoot, "license-private-key.xml") : string.Empty,
            !string.IsNullOrWhiteSpace(repoRoot) ? Path.Combine(repoRoot, "license-private-key.pem") : string.Empty,
            Path.Combine(baseDir, "AppData", "license-private-key.xml"),
            Path.Combine(baseDir, "AppData", "license-private-key.pem"),
            Path.Combine(baseDir, "license-private-key.xml"),
            Path.Combine(baseDir, "license-private-key.pem")
        };
    }

    private static string ResolveRepoRoot(string baseDir)
    {
        try
        {
            var directory = new DirectoryInfo(baseDir);
            for (var i = 0; i < 6 && directory is not null; i++)
            {
                if (File.Exists(Path.Combine(directory.FullName, "CashTracker.sln")))
                    return directory.FullName;

                directory = directory.Parent;
            }
        }
        catch
        {
            // Ignore path probing failures.
        }

        return string.Empty;
    }
}
