using System;
using System.Collections.Generic;
using System.IO;

namespace CashTracker.LicenseAdmin;

internal static class LicenseAdminPathResolver
{
    public static string Resolve()
    {
        var explicitPath = Environment.GetEnvironmentVariable("CASHTRACKER_LICENSE_ADMIN_APPDATA");
        if (!string.IsNullOrWhiteSpace(explicitPath))
            return EnsureWritableOrThrow(explicitPath.Trim());

        foreach (var candidate in GetCandidates())
        {
            if (TryEnsureWritable(candidate))
                return Path.GetFullPath(candidate);
        }

        throw new InvalidOperationException("CashTracker License Admin veri klasoru olusturulamadi.");
    }

    private static IEnumerable<string> GetCandidates()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "AppData");

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(userProfile))
            yield return Path.Combine(userProfile, "AppData", "Local", "CashTracker", "LicenseAdmin");

        var envLocalAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        if (!string.IsNullOrWhiteSpace(envLocalAppData))
            yield return Path.Combine(envLocalAppData.Trim(), "CashTracker", "LicenseAdmin");

        var specialLocalAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(specialLocalAppData))
            yield return Path.Combine(specialLocalAppData, "CashTracker", "LicenseAdmin");

    }

    private static bool TryEnsureWritable(string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            return false;

        try
        {
            var normalized = Path.GetFullPath(candidate.Trim());
            Directory.CreateDirectory(normalized);

            var probe = Path.Combine(normalized, $".write-test-{Guid.NewGuid():N}.tmp");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string EnsureWritableOrThrow(string candidate)
    {
        if (TryEnsureWritable(candidate))
            return Path.GetFullPath(candidate);

        throw new InvalidOperationException($"CashTracker License Admin veri klasoru kullanilamadi: {candidate}");
    }
}
