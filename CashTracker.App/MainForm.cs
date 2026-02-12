using System.Drawing;
using System.Windows.Forms;
using CashTracker.App.Services;
using CashTracker.App.UI;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Services;

namespace CashTracker.App
{
    internal sealed partial class MainForm : Form
    {
        private readonly IKasaService _kasaService;
        private readonly ISummaryService _summaryService;
        private readonly BackupReportService _backupReport;
        private readonly TelegramSettings _telegramSettings;
        private readonly UpdateSettings _updateSettings;
        private readonly AppRuntimeOptions _runtimeOptions;
        private readonly GitHubUpdateService _updateService;

        private SummaryCard _cardDaily = null!;
        private SummaryCard _card30 = null!;
        private SummaryCard _card365 = null!;

        private ComboBox _cmbMonth = null!;
        private Label _lblMonthIncome = null!;
        private Label _lblMonthExpense = null!;
        private Label _lblMonthNet = null!;

        private ComboBox _cmbYear = null!;
        private Label _lblYearIncome = null!;
        private Label _lblYearExpense = null!;
        private Label _lblYearNet = null!;

        private sealed class SummaryCard
        {
            public Panel Root { get; set; } = null!;
            public Label Income { get; set; } = null!;
            public Label Expense { get; set; } = null!;
            public Label Net { get; set; } = null!;
            public Button SendButton { get; set; } = null!;
        }

        private sealed class MonthItem
        {
            public int Year { get; set; }
            public int Month { get; set; }
            public string Display { get; set; } = string.Empty;
        }

        private sealed class YearItem
        {
            public int Year { get; set; }
            public string Display { get; set; } = string.Empty;
        }

        public MainForm(
            IKasaService kasaService,
            ISummaryService summaryService,
            BackupReportService backupReport,
            TelegramSettings telegramSettings,
            UpdateSettings updateSettings,
            AppRuntimeOptions runtimeOptions,
            GitHubUpdateService updateService)
        {
            _kasaService = kasaService;
            _summaryService = summaryService;
            _backupReport = backupReport;
            _telegramSettings = telegramSettings;
            _updateSettings = updateSettings;
            _runtimeOptions = runtimeOptions;
            _updateService = updateService;

            Text = "CashTracker Gösterge Paneli";
            Width = 1320;
            Height = 900;
            MinimumSize = new Size(1180, 760);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = BrandTheme.AppBackground;
            Font = BrandTheme.CreateFont(10f);
            if (AppIconProvider.Current is Icon appIcon)
                Icon = appIcon;

            BuildUi();
            Shown += async (_, __) => await RefreshSummariesAsync();
        }
    }
}

