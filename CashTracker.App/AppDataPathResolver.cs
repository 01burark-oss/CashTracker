using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CashTracker.App;

internal static class AppDataPathResolver
{
    private static readonly string[] MigratedFiles =
    {
        "cashtracker.db",
        "app-state.json",
        "telegram-setup.json",
        "install-identity.json",
        "license.json",
        "license-runtime.json",
        "license-public-key.xml"
    };

    public static string Resolve()
    {
        var explicitPath = Environment.GetEnvironmentVariable("CASHTRACKER_APPDATA");
        if (!string.IsNullOrWhiteSpace(explicitPath))
            return EnsureWritableOrThrow(explicitPath.Trim());

        var primaryPath = GetPrimaryPath();
        if (!string.IsNullOrWhiteSpace(primaryPath) && TryEnsureWritable(primaryPath))
        {
            TryMigrateLegacyData(primaryPath, Path.Combine(AppContext.BaseDirectory, "AppData"));
            return primaryPath;
        }

        foreach (var candidate in GetFallbackCandidates())
        {
            if (TryEnsureWritable(candidate))
                return candidate;
        }

        throw new InvalidOperationException("CashTracker veri klasoru olusturulamadi.");
    }

    public static IReadOnlyList<string> GetMirrorRoots(string selectedPath)
    {
        var normalizedSelected = string.IsNullOrWhiteSpace(selectedPath)
            ? string.Empty
            : Path.GetFullPath(selectedPath.Trim());

        return GetFallbackCandidates()
            .Append(GetPrimaryPath())
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => Path.GetFullPath(path))
            .Where(path => !string.Equals(path, normalizedSelected, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IEnumerable<string> GetFallbackCandidates()
    {
        var envLocalAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        if (!string.IsNullOrWhiteSpace(envLocalAppData))
            yield return Path.Combine(envLocalAppData.Trim(), "CashTracker");

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(userProfile))
            yield return Path.Combine(userProfile, "AppData", "Local", "CashTracker");

        yield return Path.Combine(AppContext.BaseDirectory, "AppData");
    }

    private static bool TryEnsureWritable(string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            return false;

        try
        {
            var normalizedPath = Path.GetFullPath(candidate.Trim());
            Directory.CreateDirectory(normalizedPath);

            var probeFile = Path.Combine(normalizedPath, $".write-test-{Guid.NewGuid():N}.tmp");
            File.WriteAllText(probeFile, "ok");
            File.Delete(probeFile);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string GetPrimaryPath()
    {
        var specialFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(specialFolderPath))
            return Path.Combine(specialFolderPath, "CashTracker");

        var envLocalAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        if (!string.IsNullOrWhiteSpace(envLocalAppData))
            return Path.Combine(envLocalAppData.Trim(), "CashTracker");

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(userProfile))
            return Path.Combine(userProfile, "AppData", "Local", "CashTracker");

        return string.Empty;
    }

    private static string EnsureWritableOrThrow(string candidate)
    {
        if (TryEnsureWritable(candidate))
            return Path.GetFullPath(candidate);

        throw new InvalidOperationException($"CashTracker veri klasoru kullanilamadi: {candidate}");
    }

    private static void TryMigrateLegacyData(string targetPath, string legacyPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(targetPath) || string.IsNullOrWhiteSpace(legacyPath))
                return;

            var normalizedTarget = Path.GetFullPath(targetPath);
            var normalizedLegacy = Path.GetFullPath(legacyPath);
            if (string.Equals(normalizedTarget, normalizedLegacy, StringComparison.OrdinalIgnoreCase))
                return;

            if (!Directory.Exists(normalizedLegacy))
                return;

            Directory.CreateDirectory(normalizedTarget);
            foreach (var fileName in MigratedFiles)
            {
                var sourceFile = Path.Combine(normalizedLegacy, fileName);
                if (!File.Exists(sourceFile))
                    continue;

                var targetFile = Path.Combine(normalizedTarget, fileName);
                if (!ShouldCopy(sourceFile, targetFile))
                    continue;

                File.Copy(sourceFile, targetFile, overwrite: true);
            }
        }
        catch
        {
            // Best-effort migration. Failure should not block startup.
        }
    }

    private static bool ShouldCopy(string sourceFile, string targetFile)
    {
        if (!File.Exists(sourceFile))
            return false;

        if (!File.Exists(targetFile))
            return true;

        var sourceInfo = new FileInfo(sourceFile);
        var targetInfo = new FileInfo(targetFile);
        return sourceInfo.LastWriteTimeUtc > targetInfo.LastWriteTimeUtc.AddSeconds(2);
    }
}
