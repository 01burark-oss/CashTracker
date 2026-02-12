using System;
using System.Collections.Generic;
using System.IO;

namespace CashTracker.App
{
    internal static class EnvFileLoader
    {
        public static void Load()
        {
            var envPath = FindEnvFile();
            if (envPath is null || !File.Exists(envPath))
                return;

            foreach (var rawLine in File.ReadAllLines(envPath))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith('#'))
                    continue;

                var eq = line.IndexOf('=');
                if (eq <= 0)
                    continue;

                var key = line[..eq].Trim();
                var value = line[(eq + 1)..].Trim();

                if (value.Length >= 2)
                {
                    var first = value[0];
                    var last = value[^1];
                    if ((first == '"' && last == '"') || (first == '\'' && last == '\''))
                        value = value[1..^1];
                }

                if (string.IsNullOrWhiteSpace(key))
                    continue;

                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                {
                    Environment.SetEnvironmentVariable(key, value, EnvironmentVariableTarget.Process);
                }
            }
        }

        private static string? FindEnvFile()
        {
            foreach (var root in CandidateRoots())
            {
                var current = root;
                while (current is not null)
                {
                    var candidate = Path.Combine(current, ".env");
                    if (File.Exists(candidate))
                        return candidate;

                    current = Directory.GetParent(current)?.FullName;
                }
            }

            return null;
        }

        private static IEnumerable<string> CandidateRoots()
        {
            yield return AppContext.BaseDirectory;

            var cwd = Directory.GetCurrentDirectory();
            if (!string.Equals(cwd, AppContext.BaseDirectory, StringComparison.OrdinalIgnoreCase))
                yield return cwd;
        }
    }
}
