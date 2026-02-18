using System;
using System.Diagnostics;
using System.IO;

namespace CashTracker.App.Services
{
    internal static class DesktopExeReplaceService
    {
        public static bool TryScheduleReplace(string sourcePath, string? targetFileName = null)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
                return false;

            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (string.IsNullOrWhiteSpace(desktop) || !Directory.Exists(desktop))
                return false;

            var fileName = string.IsNullOrWhiteSpace(targetFileName)
                ? Path.GetFileName(sourcePath)
                : targetFileName;

            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            var destPath = Path.Combine(desktop, fileName);
            if (string.Equals(sourcePath, destPath, StringComparison.OrdinalIgnoreCase))
                return true;

            var escapedSrc = sourcePath.Replace("'", "''");
            var escapedDst = destPath.Replace("'", "''");

            var script =
                $"$src='{escapedSrc}'; " +
                $"$dst='{escapedDst}'; " +
                "for($i=0;$i -lt 180;$i++){ " +
                "try{ Copy-Item -LiteralPath $src -Destination $dst -Force; exit 0 } " +
                "catch{ Start-Sleep -Seconds 1 } } " +
                "exit 1";

            try
            {
                var psi = new ProcessStartInfo("powershell")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                psi.ArgumentList.Add("-NoProfile");
                psi.ArgumentList.Add("-ExecutionPolicy");
                psi.ArgumentList.Add("Bypass");
                psi.ArgumentList.Add("-Command");
                psi.ArgumentList.Add(script);
                Process.Start(psi);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
