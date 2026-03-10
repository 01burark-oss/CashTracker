using System;
using System.Diagnostics;
using System.IO;

namespace CashTracker.App.Services
{
    internal static class InstallerLaunchService
    {
        public static bool TryScheduleInstall(string installerPath, int waitForProcessId)
        {
            if (string.IsNullOrWhiteSpace(installerPath) || !File.Exists(installerPath))
                return false;

            var escapedInstaller = installerPath.Replace("'", "''");
            var script =
                $"$installer='{escapedInstaller}'; " +
                $"$pidToWait={waitForProcessId}; " +
                "if($pidToWait -gt 0){ try{ Wait-Process -Id $pidToWait -Timeout 300 -ErrorAction Stop } catch{ } } " +
                "Start-Process -FilePath $installer -ArgumentList '/CURRENTUSER /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /TASKS=desktopicon' -WindowStyle Hidden";

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
