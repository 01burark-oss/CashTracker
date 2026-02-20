using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using CashTracker.App.Forms;
using CashTracker.App.Services;

namespace CashTracker.App
{
    internal sealed partial class MainForm
    {
        private async Task InitializeAfterLoginAsync()
        {
            if (!_isAuthenticated)
            {
                using var loginForm = new PinLoginForm(_appSecurityService);
                var loginResult = loginForm.ShowDialog(this);
                if (loginResult != System.Windows.Forms.DialogResult.OK)
                {
                    BeginInvoke(new Action(Close));
                    return;
                }

                _isAuthenticated = true;
            }

            await RefreshSummariesAsync();
            PromptDesktopShortcutIfNeeded();
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
                "Yeni sürüm açıldı. Masaüstüne kısayol oluşturmak ister misiniz?",
                "Masaüstü Kısayolu",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == System.Windows.Forms.DialogResult.Yes)
            {
                var created = DesktopShortcutService.TryCreateShortcut(
                    Application.ExecutablePath,
                    "CashTracker");

                if (!created)
                {
                    MessageBox.Show(
                        "Kısayol oluşturulamadı. Masaüstü erişimini kontrol edebilirsiniz.",
                        "Kısayol Oluşturma",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }

            state.LastShortcutPromptVersion = currentVersion;
            AppStateStore.Save(_runtimeOptions.AppDataPath, state);
        }
    }
}
