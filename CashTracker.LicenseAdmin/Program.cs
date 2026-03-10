using System;
using System.Windows.Forms;

namespace CashTracker.LicenseAdmin;

static class Program
{
    [STAThread]
    static void Main()
    {
        try
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            ApplicationConfiguration.Initialize();
            Application.Run(new LicenseAdminForm());
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"CashTracker License Admin baslatilamadi.\n\n{ex.Message}",
                "CashTracker License Admin",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
