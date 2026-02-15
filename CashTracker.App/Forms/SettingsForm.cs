using System.Drawing;
using System.Windows.Forms;
using CashTracker.App.UI;
using CashTracker.Core.Services;

namespace CashTracker.App.Forms
{
    public sealed partial class SettingsForm : Form
    {
        private readonly IIsletmeService _isletmeService;
        private readonly IKalemTanimiService _kalemTanimiService;

        private TableLayoutPanel _rootLayout = null!;
        private Panel _businessPanel = null!;
        private Panel _categoryPanel = null!;
        private TableLayoutPanel _businessSectionLayout = null!;
        private TableLayoutPanel _categorySectionLayout = null!;
        private TableLayoutPanel _rowActiveBusiness = null!;
        private TableLayoutPanel _rowRenameBusiness = null!;
        private TableLayoutPanel _rowNewBusiness = null!;
        private TableLayoutPanel _rowAddKalem = null!;

        private ComboBox _cmbBusinesses = null!;
        private TextBox _txtRenameBusiness = null!;
        private TextBox _txtNewBusiness = null!;
        private Button _btnSetActiveBusiness = null!;
        private Button _btnRenameBusiness = null!;
        private Button _btnAddBusiness = null!;
        private Label _lblBusinessHint = null!;

        private ComboBox _cmbKalemTip = null!;
        private ListBox _lstKalemler = null!;
        private TextBox _txtNewKalem = null!;
        private Button _btnAddKalem = null!;
        private Button _btnDeleteKalem = null!;
        private Label _lblCategoryHint = null!;

        private bool _isLoadingBusinesses;
        private bool _isLoadingKalemler;

        private sealed class IsletmeItem
        {
            public int Id { get; init; }
            public string Ad { get; init; } = string.Empty;
            public bool IsAktif { get; init; }
            public string Display => IsAktif ? $"{Ad} (Aktif)" : Ad;
        }

        private sealed class KalemItem
        {
            public int Id { get; init; }
            public string Ad { get; init; } = string.Empty;
        }

        public SettingsForm(
            IIsletmeService isletmeService,
            IKalemTanimiService kalemTanimiService)
        {
            _isletmeService = isletmeService;
            _kalemTanimiService = kalemTanimiService;

            Text = "Ayarlar";
            Width = 1120;
            Height = 740;
            MinimumSize = new Size(1020, 700);
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
