using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace CashTracker.App.Services
{
    internal static class UpdateBootstrapService
    {
        private static readonly TimeSpan CloseWaitTimeout = TimeSpan.FromMinutes(5);

        public static bool TryApplyPendingUpdate(string appDataPath)
        {
            try
            {
                var currentExePath = Environment.ProcessPath;
                if (!IsUpdateBootstrapPath(currentExePath, appDataPath))
                    return false;

                currentExePath = Path.GetFullPath(currentExePath!);
                RequestOtherInstancesToClose(Path.GetFileNameWithoutExtension(currentExePath), Process.GetCurrentProcess().Id);

                var versionFolder = CreateVersionFolderName(
                    FileVersionInfo.GetVersionInfo(currentExePath).ProductVersion);
                var installDir = Path.Combine(appDataPath, "installed", versionFolder);
                Directory.CreateDirectory(installDir);

                var targetPath = Path.Combine(installDir, Path.GetFileName(currentExePath));
                File.Copy(currentExePath, targetPath, true);

                DesktopShortcutService.TryCreateShortcut(targetPath, "CashTracker");
                DesktopExeReplaceService.TryScheduleReplace(currentExePath, Path.GetFileName(currentExePath));

                Process.Start(new ProcessStartInfo(targetPath)
                {
                    UseShellExecute = true,
                    WorkingDirectory = installDir
                });

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Guncelleme kurulumu tamamlanamadi: " + ex.Message,
                    "Guncelleme Hatasi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }
        }

        internal static bool IsUpdateBootstrapPath(string? currentExePath, string appDataPath)
        {
            if (string.IsNullOrWhiteSpace(currentExePath) ||
                string.IsNullOrWhiteSpace(appDataPath) ||
                !File.Exists(currentExePath))
            {
                return false;
            }

            var exePath = Path.GetFullPath(currentExePath);
            var updatesDir = EnsureTrailingSeparator(Path.GetFullPath(Path.Combine(appDataPath, "updates")));
            return exePath.StartsWith(updatesDir, StringComparison.OrdinalIgnoreCase);
        }

        internal static string CreateVersionFolderName(string? productVersion)
        {
            var candidate = string.IsNullOrWhiteSpace(productVersion)
                ? string.Empty
                : new string(productVersion
                    .Trim()
                    .Where(ch => char.IsLetterOrDigit(ch) || ch == '.' || ch == '-' || ch == '_')
                    .ToArray());

            return string.IsNullOrWhiteSpace(candidate)
                ? DateTime.Now.ToString("yyyyMMddHHmmss")
                : candidate;
        }

        private static void RequestOtherInstancesToClose(string processName, int currentProcessId)
        {
            var deadline = DateTime.UtcNow + CloseWaitTimeout;

            while (DateTime.UtcNow < deadline)
            {
                var others = GetOtherInstances(processName, currentProcessId).ToArray();
                if (others.Length == 0)
                    return;

                foreach (var process in others)
                {
                    using (process)
                    {
                        TryCloseMainWindow(process);

                        if (HasExited(process))
                            continue;

                        try
                        {
                            process.WaitForExit(1000);
                        }
                        catch
                        {
                        }
                    }
                }

                Thread.Sleep(500);
            }
        }

        private static bool HasExited(Process process)
        {
            try
            {
                return process.HasExited;
            }
            catch
            {
                return true;
            }
        }

        private static IEnumerable<Process> GetOtherInstances(string processName, int currentProcessId)
        {
            Process[] processes;
            try
            {
                processes = Process.GetProcessesByName(processName);
            }
            catch
            {
                yield break;
            }

            foreach (var process in processes)
            {
                if (process.Id == currentProcessId)
                {
                    process.Dispose();
                    continue;
                }

                yield return process;
            }
        }

        private static void TryCloseMainWindow(Process process)
        {
            try
            {
                if (process.CloseMainWindow())
                    return;
            }
            catch
            {
            }

            try
            {
                if (process.MainWindowHandle != IntPtr.Zero)
                    PostClose(process.MainWindowHandle);
            }
            catch
            {
            }
        }

        private static string EnsureTrailingSeparator(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            return path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? path
                : path + Path.DirectorySeparatorChar;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private static void PostClose(IntPtr windowHandle)
        {
            const uint WmClose = 0x0010;
            PostMessage(windowHandle, WmClose, IntPtr.Zero, IntPtr.Zero);
        }
    }
}
