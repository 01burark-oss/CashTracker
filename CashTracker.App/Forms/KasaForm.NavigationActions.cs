using System.Threading.Tasks;

namespace CashTracker.App.Forms
{
    public sealed partial class KasaForm
    {
        // Frontend butonu eklendiginde bu metod dogrudan cagrilacak.
        private async Task OpenSettingsForKalemManagementAsync()
        {
            using var form = new SettingsForm(_isletmeService, _kalemTanimiService, _telegramApprovalService);
            form.ShowDialog(this);

            await LoadKalemlerForTipAsync();
            await LoadAllAsync();
        }
    }
}
