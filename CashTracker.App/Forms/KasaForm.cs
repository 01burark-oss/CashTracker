using System.Drawing;
using System.Windows.Forms;
using CashTracker.App.UI;
using CashTracker.Core.Services;

namespace CashTracker.App.Forms
{
    public sealed partial class KasaForm : Form
    {
        private readonly IKasaService _kasaService;

        private DataGridView _grid = null!;
        private ComboBox _cmbTip = null!;
        private DateTimePicker _dtTarih = null!;
        private NumericUpDown _numTutar = null!;
        private TextBox _txtGiderTuru = null!;
        private TextBox _txtAciklama = null!;
        private Button _btnSave = null!;
        private Button _btnNew = null!;
        private Button _btnDelete = null!;
        private Button _btnRefresh = null!;
        private TableLayoutPanel _rootLayout = null!;
        private Panel _leftPanel = null!;
        private Panel _rightPanel = null!;

        private int _selectedId;

        public KasaForm(IKasaService kasaService)
        {
            _kasaService = kasaService;

            Text = "Gelir / Gider";
            Width = 1080;
            Height = 700;
            MinimumSize = new Size(960, 640);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = BrandTheme.AppBackground;
            Font = BrandTheme.CreateFont(10f);
            if (AppIconProvider.Current is Icon appIcon)
                Icon = appIcon;

            BuildUi();
            Load += async (_, __) => await LoadAllAsync();
            Resize += (_, __) => ApplyResponsiveLayout();
        }
    }
}
