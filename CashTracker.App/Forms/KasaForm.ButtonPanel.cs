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
            _btnNew = CreateButton("Yeni", Color.FromArgb(236, 240, 246), Color.FromArgb(52, 61, 73));
            _btnDelete = CreateButton("Sil", Color.FromArgb(210, 73, 73), Color.White);
            _btnRefresh = CreateButton("Yenile", Color.FromArgb(236, 240, 246), Color.FromArgb(52, 61, 73));

            WireButtons();
            return new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = false,
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
