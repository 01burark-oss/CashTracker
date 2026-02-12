using System.Drawing;
using System.Windows.Forms;
using CashTracker.App.UI;

namespace CashTracker.App.Forms
{
    public sealed partial class KasaForm
    {
        private FlowLayoutPanel CreateButtonPanel()
        {
            _btnSave = CreateButton("Kaydet", BrandTheme.Navy, Color.White);
            _btnNew = CreateButton("Yeni", BrandTheme.Navy, Color.White);
            _btnDelete = CreateButton("Sil", BrandTheme.Navy, Color.White);
            _btnRefresh = CreateButton("Yenile", BrandTheme.Navy, Color.White);

            WireButtons();
            return new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                Margin = new Padding(0),
                Padding = new Padding(0, 14, 0, 0)
            };
        }

        private void WireButtons()
        {
            _btnSave.Click += async (_, __) => await SaveAsync();
            _btnNew.Click += (_, __) => ClearForm();
            _btnDelete.Click += async (_, __) => await DeleteAsync();
            _btnRefresh.Click += async (_, __) => await LoadAllAsync();
        }
    }
}
