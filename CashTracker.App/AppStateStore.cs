using System;
using System.IO;
using System.Text.Json;

namespace CashTracker.App
{
    internal sealed class AppState
    {
        public string LastShortcutPromptVersion { get; set; } = string.Empty;
    }

    internal static class AppStateStore
    {
        private const string FileName = "app-state.json";

        public static AppState Load(string appDataPath)
        {
            var path = GetPath(appDataPath);
            if (!File.Exists(path))
                return new AppState();

            try
            {
                var json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                    return new AppState();

                return JsonSerializer.Deserialize<AppState>(json) ?? new AppState();
            }
            catch
            {
                return new AppState();
            }
        }

        public static void Save(string appDataPath, AppState state)
        {
            if (state is null)
                throw new ArgumentNullException(nameof(state));

            Directory.CreateDirectory(appDataPath);
            var json = JsonSerializer.Serialize(
                state,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(GetPath(appDataPath), json);
        }

        private static string GetPath(string appDataPath)
        {
            return Path.Combine(appDataPath, FileName);
        }
    }
}
