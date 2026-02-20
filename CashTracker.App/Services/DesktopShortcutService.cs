using System;
using System.Diagnostics;
using System.IO;

namespace CashTracker.App.Services
{
    internal static class DesktopShortcutService
    {
        public static bool TryCreateShortcut(string targetPath, string shortcutName)
        {
            if (string.IsNullOrWhiteSpace(targetPath) || !File.Exists(targetPath))
                return false;

            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (string.IsNullOrWhiteSpace(desktop) || !Directory.Exists(desktop))
                return false;

            if (string.IsNullOrWhiteSpace(shortcutName))
                return false;

            var shortcutPath = Path.Combine(desktop, $"{shortcutName}.lnk");
            var workingDir = Path.GetDirectoryName(targetPath) ?? desktop;

            var escapedTarget = Escape(targetPath);
            var escapedShortcut = Escape(shortcutPath);
            var escapedWorkingDir = Escape(workingDir);

            var script =
                "$ws = New-Object -ComObject WScript.Shell; " +
                $"$sc = $ws.CreateShortcut('{escapedShortcut}'); " +
                $"$sc.TargetPath = '{escapedTarget}'; " +
                $"$sc.WorkingDirectory = '{escapedWorkingDir}'; " +
                $"$sc.IconLocation = '{escapedTarget}'; " +
                "$sc.Save();";

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

        private static string Escape(string value)
        {
            return value.Replace("'", "''");
        }
    }
}
