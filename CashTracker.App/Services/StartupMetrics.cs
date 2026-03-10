using System;
using System.Diagnostics;
using System.IO;

namespace CashTracker.App.Services
{
    internal sealed class StartupMetrics
    {
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private readonly string _logPath;
        private readonly object _sync = new();

        public StartupMetrics(string appDataPath)
        {
            Directory.CreateDirectory(appDataPath);
            _logPath = Path.Combine(appDataPath, "startup.log");
        }

        public void Mark(string stage)
        {
            var line = $"{DateTime.UtcNow:O}|{_stopwatch.ElapsedMilliseconds}|{stage}";
            Debug.WriteLine($"[startup] {line}");

            lock (_sync)
            {
                File.AppendAllText(_logPath, line + Environment.NewLine);
            }
        }
    }
}
