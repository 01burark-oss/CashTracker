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
                "Bot ayarları kaydedildi. Değişiklikler için uygulama yeniden başlatılacak.",
                "Ayarlar Kaydedildi",
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
                    "Güncelleme ayarları eksik. appsettings.json içine Update:RepoOwner ve Update:RepoName gir.",
                    "Güncelleme Ayarı Eksik",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var originalText = triggerButton.Text;
            triggerButton.Enabled = false;
            triggerButton.Text = "Denetleniyor...";

            try
            {
                var currentVersion = Application.ProductVersion;
                var result = await _updateService.CheckAsync(_updateSettings, currentVersion);

                if (!result.HasUpdate)
                {
                    MessageBox.Show(
                        $"Uygulama güncel.\nSürüm: {currentVersion}",
                        "Güncelleme",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                if (string.IsNullOrWhiteSpace(result.AssetDownloadUrl))
                {
                    var askOpenRelease = MessageBox.Show(
                        $"Yeni sürüm bulundu: {result.LatestTag}\nFakat indirilebilir dosya bulunamadı. Release sayfası açılsın mı?",
                        "Güncelleme Hazır",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (askOpenRelease == DialogResult.Yes && !string.IsNullOrWhiteSpace(result.ReleasePageUrl))
                    {
                        Process.Start(new ProcessStartInfo(result.ReleasePageUrl) { UseShellExecute = true });
                    }

                    return;
                }

                var confirm = MessageBox.Show(
                    $"Yeni sürüm bulundu: {result.LatestTag}\n\nGüncelleme paketi indirilsin mi?",
                    "Güncelleme Hazır",
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
                        "Güvenli doğrulama dosyası (.sha256) bulunamadı. Güncelleme iptal edildi.",
                        "Güncelleme Doğrulanamadı",
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

                Process.Start(new ProcessStartInfo(downloadedPath) { UseShellExecute = true });
                MessageBox.Show(
                    "Güncelleme paketi çalıştırıldı. Kurulumdan sonra uygulamayı yeniden aç.",
                    "Güncelleme Başlatıldı",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Güncelleme denetleme hatası: " + ex.Message,
                    "Güncelleme Hatası",
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
                throw new FileNotFoundException("Güncelleme paketi bulunamadı.", zipPath);

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
                    $"Güncelleme içinde '{currentExeName}' bulunamadı. Asset içeriğini kontrol et.");
            }

            Process.Start(new ProcessStartInfo(launchPath)
            {
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(launchPath) ?? targetDir
            });

            MessageBox.Show(
                "Güncelleme yüklendi. Yeni sürüm başlatıldı.",
                "Güncelleme Tamamlandı",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            Application.Exit();
        }

        private static void EnsureChecksumMatches(string downloadedPath, string checksumText)
        {
            var fileName = Path.GetFileName(downloadedPath);
            if (!TryReadExpectedSha256(checksumText, fileName, out var expectedHash))
            {
                throw new InvalidOperationException("Checksum dosyası geçersiz veya hedef dosya hash'i bulunamadı.");
            }

            var actualHash = ComputeSha256(downloadedPath);
            if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "İndirilen dosyanın checksum doğrulaması başarısız oldu. Güncelleme güvenlik nedeniyle durduruldu.");
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

