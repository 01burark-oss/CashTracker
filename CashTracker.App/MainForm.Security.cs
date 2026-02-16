using System;
using System.Threading.Tasks;
using CashTracker.App.Forms;

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
        }
    }
}
