using System.Drawing;
using System.Windows.Forms;
using CashTracker.App;
using CashTracker.App.Services;
using CashTracker.App.UI;
using CashTracker.Core.Models;
using CashTracker.Core.Services;

namespace CashTracker.App.Forms
{
    internal sealed partial class SettingsForm : Form
    {
        private readonly IIsletmeService _isletmeService;
        private readonly IKalemTanimiService _kalemTanimiService;
        private readonly ITelegramApprovalService _telegramApprovalService;
        private readonly AppRuntimeOptions _runtimeOptions;
        private readonly IAppSecurityService _appSecurityService;
        private readonly ILicenseService _licenseService;
        private readonly ReceiptOcrSettings _receiptOcrSettings;

        private TableLayoutPanel _rootLayout = null!;
        private Panel _businessPanel = null!;
        private Panel _categoryPanel = null!;
        private TableLayoutPanel _businessSectionLayout = null!;
        private TableLayoutPanel _categorySectionLayout = null!;
        private TableLayoutPanel _rowActiveBusiness = null!;
        private TableLayoutPanel _rowLanguage = null!;
        private TableLayoutPanel _rowRenameBusiness = null!;
        private TableLayoutPanel _rowNewBusiness = null!;
        private TableLayoutPanel _rowEditKalem = null!;
        private TableLayoutPanel _rowAddKalem = null!;

        private ComboBox _cmbBusinesses = null!;
        private ComboBox _cmbLanguage = null!;
        private TextBox _txtRenameBusiness = null!;
        private TextBox _txtNewBusiness = null!;
        private Button _btnSetActiveBusiness = null!;
        private Button _btnApplyLanguage = null!;
        private Button _btnRenameBusiness = null!;
        private Button _btnAddBusiness = null!;
        private Button _btnDeleteBusiness = null!;
        private Button _btnChangePin = null!;
        private Label _lblBusinessHint = null!;
        private TextBox _txtInstallCode = null!;
        private TextBox _txtLicenseKey = null!;
        private Button _btnCopyInstallCode = null!;
        private Button _btnActivateLicense = null!;
        private Label _lblLicenseStatus = null!;

        private ComboBox _cmbKalemTip = null!;
        private ListBox _lstKalemler = null!;
        private TextBox _txtEditKalem = null!;
        private TextBox _txtNewKalem = null!;
        private Button _btnUpdateKalem = null!;
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
            public string Display => IsAktif ? $"{Ad} ({AppLocalization.T("settings.status.active")})" : Ad;
        }

        private sealed class KalemItem
        {
            public int Id { get; init; }
            public string Ad { get; init; } = string.Empty;
        }

        public SettingsForm(
            IIsletmeService isletmeService,
            IKalemTanimiService kalemTanimiService,
            ITelegramApprovalService telegramApprovalService,
            AppRuntimeOptions runtimeOptions,
            IAppSecurityService appSecurityService,
            ILicenseService licenseService,
            ReceiptOcrSettings receiptOcrSettings)
        {
            _isletmeService = isletmeService;
            _kalemTanimiService = kalemTanimiService;
            _telegramApprovalService = telegramApprovalService;
            _runtimeOptions = runtimeOptions;
            _appSecurityService = appSecurityService;
            _licenseService = licenseService;
            _receiptOcrSettings = receiptOcrSettings;

            Text = AppLocalization.T("settings.title");
            Width = 1120;
            Height = 740;
            MinimumSize = new Size(1020, 700);
            UiMetrics.ApplyFormDefaults(this);
            StartPosition = FormStartPosition.CenterParent;
            WindowState = FormWindowState.Maximized;
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
