using System;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace CashTracker.App.Services
{
    internal interface ILicenseRuntimeStateStore
    {
        LicenseRuntimeState Load();
        void Save(LicenseRuntimeState state);
    }

    internal sealed class LicenseRuntimeStateStore : ILicenseRuntimeStateStore
    {
        private const string FileName = "license-runtime.json";
        private const string DefaultRegistryPath = @"Software\CashTracker\Licensing";
        private const string RegistryValueName = "RuntimeState";
        private const string RegistryPathOverrideEnvironmentVariable = "CASHTRACKER_LICENSE_REGISTRY_PATH";

        private readonly AppRuntimeOptions _runtimeOptions;

        public LicenseRuntimeStateStore(AppRuntimeOptions runtimeOptions)
        {
            _runtimeOptions = runtimeOptions;
        }

        public LicenseRuntimeState Load()
        {
            var fromFile = TryLoadFromFile();
            var fromRegistry = TryLoadFromRegistry();

            if (fromFile is null && fromRegistry is null)
                return new LicenseRuntimeState();

            if (fromFile is null)
                return fromRegistry!;

            if (fromRegistry is null)
                return fromFile;

            return fromFile.UpdatedAtUtc >= fromRegistry.UpdatedAtUtc
                ? fromFile
                : fromRegistry;
        }

        public void Save(LicenseRuntimeState state)
        {
            if (state is null)
                throw new ArgumentNullException(nameof(state));

            state.UpdatedAtUtc = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(
                state,
                new JsonSerializerOptions { WriteIndented = true });

            try
            {
                Directory.CreateDirectory(_runtimeOptions.AppDataPath);
                File.WriteAllText(Path.Combine(_runtimeOptions.AppDataPath, FileName), json);
            }
            catch
            {
                // Best-effort file persistence.
            }

            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(GetRegistryPath());
                key?.SetValue(RegistryValueName, json, RegistryValueKind.String);
            }
            catch
            {
                // Best-effort registry persistence.
            }
        }

        private LicenseRuntimeState? TryLoadFromFile()
        {
            try
            {
                var path = Path.Combine(_runtimeOptions.AppDataPath, FileName);
                if (!File.Exists(path))
                    return null;

                var json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                    return null;

                return JsonSerializer.Deserialize<LicenseRuntimeState>(json);
            }
            catch
            {
                return null;
            }
        }

        private static LicenseRuntimeState? TryLoadFromRegistry()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(GetRegistryPath());
                var json = key?.GetValue(RegistryValueName)?.ToString();
                if (string.IsNullOrWhiteSpace(json))
                    return null;

                return JsonSerializer.Deserialize<LicenseRuntimeState>(json);
            }
            catch
            {
                return null;
            }
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
