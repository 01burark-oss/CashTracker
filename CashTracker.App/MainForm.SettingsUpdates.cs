using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;
using CashTracker.App.Forms;
using CashTracker.App.Services;

namespace CashTracker.App
{
    internal sealed partial class MainForm
    {
        private void OpenBotSettings()
        {
            using var form = new InitialSetupForm(_telegramSettings.BotToken, _telegramSettings.ChatId, true);
            if (form.ShowDialog(this) != DialogResult.OK)
                return;

            UserTelegramSetupStore.Save(_runtimeOptions.AppDataPath, new UserTelegramSetup
            {
                BotToken = form.BotToken,
                UserId = form.UserId
            });

            MessageBox.Show(
                AppLocalization.T("main.bot.savedBody"),
                AppLocalization.T("main.bot.savedTitle"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            Application.Restart();
            Close();
        }

        private async Task CheckForUpdatesAsync(Button triggerButton)
        {
            if (!_updateSettings.IsConfigured)
            {
                MessageBox.Show(
                    AppLocalization.T("main.update.missingConfigBody"),
                    AppLocalization.T("main.update.missingConfigTitle"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var originalText = triggerButton.Text;
            triggerButton.Enabled = false;
            triggerButton.Text = AppLocalization.T("main.update.checkingButton");

            try
            {
                var currentVersion = Application.ProductVersion;
                var result = await _updateService.CheckAsync(_updateSettings, currentVersion);

                if (!result.HasUpdate)
                {
                    MessageBox.Show(
                        AppLocalization.F("main.update.upToDateBody", currentVersion),
                        AppLocalization.T("main.update.upToDateTitle"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                if (string.IsNullOrWhiteSpace(result.AssetDownloadUrl))
                {
                    var askOpenRelease = MessageBox.Show(
                        AppLocalization.F("main.update.assetMissingBody", result.LatestTag),
                        AppLocalization.T("main.update.availableTitle"),
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (askOpenRelease == DialogResult.Yes && !string.IsNullOrWhiteSpace(result.ReleasePageUrl))
                    {
                        Process.Start(new ProcessStartInfo(result.ReleasePageUrl) { UseShellExecute = true });
                    }

                    return;
                }

                var confirm = MessageBox.Show(
                    AppLocalization.F("main.update.confirmDownloadBody", result.LatestTag),
                    AppLocalization.T("main.update.availableTitle"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes)
                    return;

                var downloadedPath = await _updateService.DownloadAssetAsync(
                    result.AssetDownloadUrl,
                    string.IsNullOrWhiteSpace(result.AssetName) ? "cashtracker-update.bin" : result.AssetName,
                    _runtimeOptions.AppDataPath);

                if (string.IsNullOrWhiteSpace(result.ChecksumAssetDownloadUrl))
                {
                    MessageBox.Show(
                        AppLocalization.T("main.update.checksumMissingBody"),
                        AppLocalization.T("main.update.checksumMissingTitle"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                var checksumText = await _updateService.DownloadTextAsync(result.ChecksumAssetDownloadUrl);
                EnsureChecksumMatches(downloadedPath, checksumText);

                var ext = Path.GetExtension(downloadedPath);
                if (string.Equals(ext, ".zip", StringComparison.OrdinalIgnoreCase))
                {
                    ApplyZipUpdateAndRestart(downloadedPath, result.LatestTag);
                    return;
                }

                if (string.Equals(ext, ".exe", StringComparison.OrdinalIgnoreCase))
                {
                    DesktopExeReplaceService.TryScheduleReplace(
                        downloadedPath,
                        Path.GetFileName(Application.ExecutablePath),
                        Process.GetCurrentProcess().Id);
                }

                Process.Start(new ProcessStartInfo(downloadedPath) { UseShellExecute = true });
                MessageBox.Show(
                    AppLocalization.T("main.update.startedBody"),
                    AppLocalization.T("main.update.startedTitle"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    AppLocalization.F("main.update.errorBody", ex.Message),
                    AppLocalization.T("main.update.errorTitle"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                triggerButton.Enabled = true;
                triggerButton.Text = originalText;
            }
        }

        private void ApplyZipUpdateAndRestart(string zipPath, string latestTag)
        {
            if (!File.Exists(zipPath))
                throw new FileNotFoundException(AppLocalization.T("main.update.packageMissing"), zipPath);

            var cleanedTag = new string((latestTag ?? string.Empty)
                .Where(ch => char.IsLetterOrDigit(ch) || ch == '-' || ch == '_')
                .ToArray());
            if (string.IsNullOrWhiteSpace(cleanedTag))
                cleanedTag = DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);

            var installRoot = Path.Combine(_runtimeOptions.AppDataPath, "installed");
            Directory.CreateDirectory(installRoot);

            var targetDir = Path.Combine(installRoot, cleanedTag);
            if (Directory.Exists(targetDir))
                Directory.Delete(targetDir, true);

            ZipFile.ExtractToDirectory(zipPath, targetDir, true);

            var currentExeName = Path.GetFileName(Application.ExecutablePath);
            var launchPath = Path.Combine(targetDir, currentExeName);
            if (!File.Exists(launchPath))
            {
                launchPath = Directory.GetFiles(targetDir, currentExeName, SearchOption.AllDirectories)
                    .FirstOrDefault() ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(launchPath) || !File.Exists(launchPath))
            {
                throw new InvalidOperationException(
                    AppLocalization.F("main.update.assetMissingInZip", currentExeName));
            }

            Process.Start(new ProcessStartInfo(launchPath)
            {
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(launchPath) ?? targetDir
            });

            var desktopReplaceScheduled = DesktopExeReplaceService.TryScheduleReplace(
                launchPath,
                Path.GetFileName(Application.ExecutablePath),
                Process.GetCurrentProcess().Id);

            MessageBox.Show(
                desktopReplaceScheduled
                    ? AppLocalization.T("main.update.completedBodyWithDesktop")
                    : AppLocalization.T("main.update.completedBody"),
                AppLocalization.T("main.update.completedTitle"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            Application.Exit();
        }

        private static void EnsureChecksumMatches(string downloadedPath, string checksumText)
        {
            var fileName = Path.GetFileName(downloadedPath);
            if (!TryReadExpectedSha256(checksumText, fileName, out var expectedHash))
            {
                throw new InvalidOperationException(AppLocalization.T("main.update.invalidChecksumFile"));
            }

            var actualHash = ComputeSha256(downloadedPath);
            if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    AppLocalization.T("main.update.checksumMismatch"));
            }
        }

        private static string ComputeSha256(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            var hash = SHA256.HashData(stream);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static bool TryReadExpectedSha256(string checksumText, string fileName, out string hash)
        {
            hash = string.Empty;
            if (string.IsNullOrWhiteSpace(checksumText))
                return false;

            var lines = checksumText
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .ToArray();

            foreach (var line in lines)
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                    continue;

                var candidateHash = parts[0].Trim();
                if (!IsSha256Hex(candidateHash))
                    continue;

                if (parts.Length == 1)
                {
                    hash = candidateHash;
                    return true;
                }

                var candidateFile = parts[^1].TrimStart('*');
                if (string.Equals(candidateFile, fileName, StringComparison.OrdinalIgnoreCase))
                {
                    hash = candidateHash;
                    return true;
                }
            }

            return false;
        }

        private static bool IsSha256Hex(string value)
        {
            if (value.Length != 64)
                return false;

            for (var i = 0; i < value.Length; i++)
            {
                var ch = value[i];
                var isHex = (ch >= '0' && ch <= '9') ||
                            (ch >= 'a' && ch <= 'f') ||
                            (ch >= 'A' && ch <= 'F');
                if (!isHex)
                    return false;
            }

            return true;
        }
    }
}

