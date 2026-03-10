using System;
using System.IO;
using System.Text.Json;

namespace CashTracker.LicenseAdmin;

internal sealed class LicenseAdminSettings
{
    public string PrivateKeyPath { get; set; } = string.Empty;
}

internal static class LicenseAdminSettingsStore
{
    private const string FileName = "admin-settings.json";

    public static LicenseAdminSettings Load(string appDataPath)
    {
        try
        {
            var path = GetPath(appDataPath);
            if (!File.Exists(path))
                return new LicenseAdminSettings();

            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
                return new LicenseAdminSettings();

            return JsonSerializer.Deserialize<LicenseAdminSettings>(json) ?? new LicenseAdminSettings();
        }
        catch
        {
            return new LicenseAdminSettings();
        }
    }

    public static void Save(string appDataPath, LicenseAdminSettings settings)
    {
        try
        {
            Directory.CreateDirectory(appDataPath);
            var path = GetPath(appDataPath);
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch
        {
            // Best-effort preferences store.
        }
    }

    private static string GetPath(string appDataPath)
    {
        return Path.Combine(appDataPath, FileName);
    }
}
