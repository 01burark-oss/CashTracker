using System.Drawing;
using System.Windows.Forms;
using CashTracker.App.UI;
using CashTracker.Core.Services;

namespace CashTracker.App.Forms
{
    public sealed partial class KasaForm : Form
    {
        private readonly IKasaService _kasaService;
        private readonly IIsletmeService _isletmeService;
        private readonly IKalemTanimiService _kalemTanimiService;

        private DataGridView _grid = null!;
        private ComboBox _cmbTip = null!;
        private DateTimePicker _dtTarih = null!;
        private NumericUpDown _numTutar = null!;
        private ComboBox _cmbKalem = null!;
        private Button _btnOdemeNakit = null!;
        private Button _btnOdemeKrediKarti = null!;
        private Button _btnOdemeHavale = null!;
        private Label _lblKalemEmptyHint = null!;
        private Button _btnKalemSettings = null!;
        private TextBox _txtAciklama = null!;
        private Button _btnSave = null!;
        private Button _btnNew = null!;
        private Button _btnDelete = null!;
        private Button _btnRefresh = null!;
        private TableLayoutPanel _rootLayout = null!;
        private Panel _leftPanel = null!;
        private Panel _rightPanel = null!;
        private Label _lblActiveBusiness = null!;

        private int _selectedId;
        private bool _isLoadingKalemler;
        private string _selectedOdemeYontemi = "Nakit";

        public KasaForm(
            IKasaService kasaService,
            IIsletmeService isletmeService,
            IKalemTanimiService kalemTanimiService)
        {
            _kasaService = kasaService;
            _isletmeService = isletmeService;
            _kalemTanimiService = kalemTanimiService;

            Text = "Gelir / Gider";
            Width = 1080;
            Height = 700;
            MinimumSize = new Size(1120, 700);
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
