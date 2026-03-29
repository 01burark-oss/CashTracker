using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using CashTracker.App.Forms;
using CashTracker.App.Services;

namespace CashTracker.App
{
    internal sealed partial class MainForm
    {
        private async Task RunDeferredUpdateCheckAsync()
        {
            if (_hasDeferredUpdateCheckStarted || !_updateSettings.IsConfigured)
                return;

            _hasDeferredUpdateCheckStarted = true;

            try
            {
                var delaySeconds = Math.Max(10, _updateSettings.AutoCheckDelaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                if (IsDisposed || Disposing)
                    return;

                var result = await FetchUpdateResultAsync(forceRefresh: true);
                if (!result.HasUpdate)
                    return;

                MarkUpdateAvailable(result);

                if (result.IsMandatory)
                    await PromptAndInstallUpdateAsync(_btnUpdateNav, result);
            }
            catch
            {
                // Background checks should never block the user.
            }
        }

        private void OpenBotSettings()
        {
            using var form = new InitialSetupForm(_telegramSettings.BotToken, _telegramSettings.ChatId, _telegramSettings.AllowedUserIds, true);
            if (form.ShowDialog(this) != DialogResult.OK)
                return;

            UserTelegramSetupStore.Save(_runtimeOptions.AppDataPath, new UserTelegramSetup
            {
                BotToken = form.BotToken,
                ChatId = form.ChatId,
                AllowedUserIds = form.AllowedUserIds
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
                var result = _cachedUpdateResult is { HasUpdate: true }
                    ? _cachedUpdateResult
                    : await FetchUpdateResultAsync(forceRefresh: true);

                if (!result.HasUpdate)
                {
                    ClearUpdateBadge();
                    MessageBox.Show(
                        AppLocalization.F("main.update.upToDateBody", Application.ProductVersion ?? string.Empty),
                        AppLocalization.T("main.update.upToDateTitle"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                MarkUpdateAvailable(result);
                await PromptAndInstallUpdateAsync(triggerButton, result);
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

        private async Task<ManifestUpdateCheckResult> FetchUpdateResultAsync(bool forceRefresh)
        {
            if (!forceRefresh && _cachedUpdateResult is not null)
                return _cachedUpdateResult;

            var currentVersion = Application.ProductVersion ?? string.Empty;
            _cachedUpdateResult = await _updateService.CheckAsync(_updateSettings, currentVersion);
            return _cachedUpdateResult;
        }

        private void MarkUpdateAvailable(ManifestUpdateCheckResult result)
        {
            _cachedUpdateResult = result;
            if (_lblUpdateBadge is null)
                return;

            _lblUpdateBadge.Text = AppLocalization.T(
                result.IsMandatory
                    ? "main.badge.updateRequired"
                    : "main.badge.updateAvailable");
            _lblUpdateBadge.Visible = result.HasUpdate;
        }

        private void ClearUpdateBadge()
        {
            _cachedUpdateResult = null;
            if (_lblUpdateBadge is not null)
                _lblUpdateBadge.Visible = false;
        }

        private async Task PromptAndInstallUpdateAsync(Button triggerButton, ManifestUpdateCheckResult result)
        {
            var minimumVersionText = string.IsNullOrWhiteSpace(result.MinSupportedVersion)
                ? "-"
                : result.MinSupportedVersion;
            var installPrompt = result.CanInstallInApp
                ? (result.IsMandatory
                    ? "Bu guncelleme zorunlu. Devam etmek icin kuruluma gecilecek."
                    : "Guncelleme paketini indirip kurmak istiyor musunuz?")
                : "Bu surum icin otomatik kurulum paketi yayinlanmamis. Release sayfasini acmak istiyor musunuz?";
            var message =
                $"Yeni surum: {result.LatestVersion}\n" +
                $"Asgari desteklenen surum: {minimumVersionText}\n\n" +
                $"Notlar:\n{(string.IsNullOrWhiteSpace(result.ReleaseNotes) ? "- Not yok -" : result.ReleaseNotes)}\n\n" +
                installPrompt;

            var buttons = result.IsMandatory ? MessageBoxButtons.OKCancel : MessageBoxButtons.YesNo;
            var confirm = MessageBox.Show(
                message,
                AppLocalization.T("main.update.availableTitle"),
                buttons,
                MessageBoxIcon.Information);

            var wantsInstall = result.IsMandatory
                ? confirm == DialogResult.OK
                : confirm == DialogResult.Yes;

            if (!wantsInstall)
            {
                if (result.IsMandatory)
                    Close();

                return;
            }

            if (!result.CanInstallInApp)
            {
                var targetUrl = string.IsNullOrWhiteSpace(result.ReleasePageUrl)
                    ? result.PackageUrl
                    : result.ReleasePageUrl;
                if (string.IsNullOrWhiteSpace(targetUrl))
                    throw new InvalidOperationException("Guncelleme sayfasi bulunamadi.");

                Process.Start(new ProcessStartInfo
                {
                    FileName = targetUrl,
                    UseShellExecute = true
                });
                return;
            }

            var packagePath = await _updateService.DownloadPackageAsync(
                result.PackageUrl,
                string.IsNullOrWhiteSpace(result.PackageFileName) ? "CashTracker-Setup.exe" : result.PackageFileName,
                _runtimeOptions.AppDataPath);

            UpdateManifestService.VerifyPackageHash(packagePath, result.Sha256);

            if (!InstallerLaunchService.TryScheduleInstall(packagePath, Process.GetCurrentProcess().Id))
                throw new InvalidOperationException("Installer baslatilamadi.");

            MessageBox.Show(
                AppLocalization.T("main.update.startedBody"),
                AppLocalization.T("main.update.startedTitle"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            triggerButton.Enabled = false;
            Application.Exit();
        }
    }
}
