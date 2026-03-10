using System.Threading.Tasks;
using System.Windows.Forms;
using CashTracker.App.Services;

namespace CashTracker.App
{
    internal sealed partial class MainForm
    {
        private async Task InitializeAfterLoginAsync()
        {
            _startupMetrics.Mark("mainform-init-start");
            await _licenseService.RecordSuccessfulUseAsync();
            await RefreshLicenseBannerAsync();
            await RefreshSummariesAsync();
            _startupMetrics.Mark("dashboard-snapshot-ready");
            PromptDesktopShortcutIfNeeded();
            _ = RunDeferredUpdateCheckAsync();
        }

        private async Task RefreshLicenseBannerAsync()
        {
            var access = await _licenseService.EvaluateAccessAsync();
            if (access.Mode != LicenseAccessMode.Trial || !access.ShowBanner)
            {
                _licenseBanner.Visible = false;
                return;
            }

            _lblLicenseBannerTitle.Text = AppLocalization.T("license.banner.title");
            _lblLicenseBannerText.Text = AppLocalization.F("license.banner.remaining", access.DaysRemaining);
            _licenseBanner.Visible = true;
        }

        private void PromptDesktopShortcutIfNeeded()
        {
            var currentVersion = Application.ProductVersion ?? string.Empty;
            if (string.IsNullOrWhiteSpace(currentVersion))
                return;

            var state = AppStateStore.Load(_runtimeOptions.AppDataPath);
            if (string.Equals(state.LastShortcutPromptVersion, currentVersion, StringComparison.OrdinalIgnoreCase))
                return;

            var result = MessageBox.Show(
                AppLocalization.T("main.shortcut.promptBody"),
                AppLocalization.T("main.shortcut.promptTitle"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                var created = DesktopShortcutService.TryCreateShortcut(
                    Application.ExecutablePath,
                    "CashTracker");

                if (!created)
                {
                    MessageBox.Show(
                        AppLocalization.T("main.shortcut.errorBody"),
                        AppLocalization.T("main.shortcut.errorTitle"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }

            state.LastShortcutPromptVersion = currentVersion;
            AppStateStore.Save(_runtimeOptions.AppDataPath, state);
        }
    }
}
