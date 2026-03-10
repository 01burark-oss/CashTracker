using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CashTracker.Core.Utilities;
using Microsoft.Win32;

namespace CashTracker.App.Services
{
    internal interface IInstallIdentityService
    {
        string GetInstallCode();
        string GetInstallCodeHash();
    }

    internal sealed class InstallIdentityService : IInstallIdentityService
    {
        private const string FileName = "install-identity.json";
        private const string DefaultRegistryPath = @"Software\CashTracker\Licensing";
        private const string RegistryValueName = "InstallCode";
        private const string RegistryPathOverrideEnvironmentVariable = "CASHTRACKER_LICENSE_REGISTRY_PATH";

        private readonly AppRuntimeOptions _runtimeOptions;
        private string? _cachedInstallCode;
        private string? _cachedInstallCodeHash;

        private sealed class StoredInstallIdentity
        {
            public string InstallCode { get; set; } = string.Empty;
            public DateTime UpdatedAtUtc { get; set; }
        }

        public InstallIdentityService(AppRuntimeOptions runtimeOptions)
        {
            _runtimeOptions = runtimeOptions;
        }

        public string GetInstallCode()
        {
            _cachedInstallCode ??= LoadOrCreateInstallCode();
            return _cachedInstallCode;
        }

        public string GetInstallCodeHash()
        {
            _cachedInstallCodeHash ??= ComputeSha256(GetInstallCode());
            return _cachedInstallCodeHash;
        }

        private string LoadOrCreateInstallCode()
        {
            var fromFile = TryLoadFromFile();
            var fromRegistry = TryLoadFromRegistry();
            var selected = SelectValidInstallCode(fromFile?.InstallCode, fromRegistry?.InstallCode);

            if (string.IsNullOrWhiteSpace(selected))
                selected = GenerateInstallCode();

            Persist(selected);
            return selected;
        }

        private StoredInstallIdentity? TryLoadFromFile()
        {
            try
            {
                var path = Path.Combine(_runtimeOptions.AppDataPath, FileName);
                if (!File.Exists(path))
                    return null;

                var json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                    return null;

                return JsonSerializer.Deserialize<StoredInstallIdentity>(json);
            }
            catch
            {
                return null;
            }
        }

        private StoredInstallIdentity? TryLoadFromRegistry()
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(GetRegistryPath());
                var installCode = key?.GetValue(RegistryValueName)?.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(installCode))
                    return null;

                return new StoredInstallIdentity
                {
                    InstallCode = installCode
                };
            }
            catch
            {
                return null;
            }
        }

        private static string SelectValidInstallCode(params string?[] values)
        {
            foreach (var value in values)
            {
                if (InstallCodeFormat.TryNormalize(value, out var normalized))
                    return normalized;
            }

            return string.Empty;
        }

        private void Persist(string installCode)
        {
            var trimmed = installCode.Trim();
            var stored = new StoredInstallIdentity
            {
                InstallCode = trimmed,
                UpdatedAtUtc = DateTime.UtcNow
            };

            try
            {
                Directory.CreateDirectory(_runtimeOptions.AppDataPath);
                var path = Path.Combine(_runtimeOptions.AppDataPath, FileName);
                var json = JsonSerializer.Serialize(
                    stored,
                    new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch
            {
                // Best-effort persistence. Registry write below may still succeed.
            }

            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(GetRegistryPath());
                key?.SetValue(RegistryValueName, trimmed, RegistryValueKind.String);
            }
            catch
            {
                // Registry is secondary storage. Failure should not block startup.
            }
        }

        private static string FirstNonEmpty(params string?[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }

            return string.Empty;
        }

        private static string GenerateInstallCode()
        {
            var raw = Guid.NewGuid().ToString("N").ToUpperInvariant();
            return $"CTI-{raw[..8]}-{raw[8..16]}-{raw[16..24]}";
        }

        private static string ComputeSha256(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static string GetRegistryPath()
        {
            var overridePath = Environment.GetEnvironmentVariable(RegistryPathOverrideEnvironmentVariable);
            return string.IsNullOrWhiteSpace(overridePath)
                ? DefaultRegistryPath
                : overridePath.Trim();
        }
    }
}
