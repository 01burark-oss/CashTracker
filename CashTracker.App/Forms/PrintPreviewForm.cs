using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CashTracker.App.Printing;
using CashTracker.App.UI;
using CashTracker.Core.Services;

namespace CashTracker.App.Forms
{
    public sealed partial class PrintPreviewForm : Form
    {
        private readonly IKasaService _kasaService;
        private readonly ISummaryService _summaryService;
        private readonly IIsletmeService _isletmeService;
        private readonly System.Windows.Forms.Timer _refreshTimer;

        private PrintPreviewControl _previewControl = null!;
        private ComboBox _cmbTemplate = null!;
        private ComboBox _cmbRange = null!;
        private DateTimePicker _dtFrom = null!;
        private DateTimePicker _dtTo = null!;
        private TextBox _txtNote = null!;
        private Button _btnApplyNote = null!;
        private Button _btnPrint = null!;
        private Button _btnSavePdf = null!;
        private Button _btnExport = null!;
        private Label _lblStatus = null!;

        private int _refreshVersion;
        private bool _isInitializing;
        private string _appliedNote = string.Empty;
        private PrintDocument? _currentPreviewDocument;
        private PrintReportData? _currentPreviewReport;

        public PrintPreviewForm(
            IKasaService kasaService,
            ISummaryService summaryService,
            IIsletmeService isletmeService)
        {
            _kasaService = kasaService;
            _summaryService = summaryService;
            _isletmeService = isletmeService;

            Text = AppLocalization.T("print.title");
            Width = 1360;
            Height = 860;
            MinimumSize = new Size(1180, 760);
            UiMetrics.ApplyFormDefaults(this);
            StartPosition = FormStartPosition.CenterParent;
            WindowState = FormWindowState.Maximized;
            BackColor = Color.FromArgb(238, 239, 241);
            Font = BrandTheme.CreateFont(10f);
            if (AppIconProvider.Current is Icon appIcon)
                Icon = appIcon;

            _refreshTimer = new System.Windows.Forms.Timer { Interval = 240 };
            _refreshTimer.Tick += async (_, __) =>
            {
                _refreshTimer.Stop();
                await RefreshPreviewAsync();
            };

            BuildUi();
            Load += async (_, __) => await InitializeAsync();
            FormClosed += (_, __) => _currentPreviewDocument?.Dispose();
        }

        private void BuildUi()
        {
            SuspendLayout();
            Controls.Clear();

            var shell = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(18, 16, 18, 16),
                BackColor = BackColor
            };
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(shell);

            var previewHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(226, 228, 231),
                Padding = new Padding(26),
                Margin = new Padding(0, 0, 14, 0)
            };
            previewHost.Paint += (_, e) =>
            {
                using var pen = new Pen(Color.FromArgb(190, 193, 198), 1f);
                e.Graphics.DrawRectangle(pen, 0, 0, previewHost.Width - 1, previewHost.Height - 1);
            };
            shell.Controls.Add(previewHost, 0, 0);

            _previewControl = new PrintPreviewControl
            {
                Dock = DockStyle.Fill,
                AutoZoom = false,
                Zoom = 0.92d,
                Rows = 1,
                Columns = 1,
                UseAntiAlias = true,
                BackColor = Color.FromArgb(214, 217, 221)
            };
            previewHost.Controls.Add(_previewControl);

            var controlPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(250, 250, 250),
                Margin = new Padding(14, 0, 0, 0)
            };
            controlPanel.Paint += (_, e) =>
            {
                using var pen = new Pen(Color.FromArgb(36, 36, 36), 1f);
                e.Graphics.DrawRectangle(pen, 0, 0, controlPanel.Width - 1, controlPanel.Height - 1);
            };
            shell.Controls.Add(controlPanel, 1, 0);

            var actionsHost = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 206,
                BackColor = Color.FromArgb(246, 246, 246),
                Padding = new Padding(18, 8, 18, 16)
            };
            actionsHost.Paint += (_, e) =>
            {
                using var pen = new Pen(Color.FromArgb(188, 188, 188), 1f);
                e.Graphics.DrawLine(pen, 0, 0, actionsHost.Width, 0);
            };
            controlPanel.Controls.Add(actionsHost);

            var scrollHost = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = controlPanel.BackColor,
                Padding = new Padding(18, 18, 18, 12)
            };
            controlPanel.Controls.Add(scrollHost);

            var controlsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 9
            };
            controlsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            controlsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            controlsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            controlsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            controlsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            controlsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            controlsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            controlsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            controlsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            controlsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            scrollHost.Controls.Add(controlsLayout);

            var titlePanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = UiMetrics.GetHeaderHeight(
                    BrandTheme.CreateHeadingFont(14f, FontStyle.Bold),
                    BrandTheme.CreateFont(8.9f),
                    28,
                    4),
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 14),
                Padding = new Padding(14, 12, 14, 10)
            };
            titlePanel.Paint += (_, e) =>
            {
                using var pen = new Pen(Color.FromArgb(206, 206, 206), 1f);
                e.Graphics.DrawRectangle(pen, 0, 0, titlePanel.Width - 1, titlePanel.Height - 1);
                using var accentBrush = new SolidBrush(Color.FromArgb(30, 30, 30));
                e.Graphics.FillRectangle(accentBrush, 0, 0, 7, titlePanel.Height);
            };
            controlsLayout.Controls.Add(titlePanel, 0, 0);

            var title = new Label
            {
                Text = AppLocalization.T("print.panel.title").ToUpperInvariant(),
                AutoSize = true,
                Font = BrandTheme.CreateHeadingFont(14f, FontStyle.Bold),
                ForeColor = Color.Black,
                Margin = new Padding(0)
            };
            titlePanel.Controls.Add(title);

            var subtitle = new Label
            {
                Text = AppLocalization.T("print.panel.subtitle"),
                AutoSize = true,
                Font = BrandTheme.CreateFont(8.9f),
                ForeColor = BrandTheme.MutedText,
                Margin = new Padding(0),
                Location = new Point(14, 14 + UiMetrics.GetTextLineHeight(title.Font) + 4)
            };
            titlePanel.Controls.Add(subtitle);

            controlsLayout.Controls.Add(CreateSectionLabel(AppLocalization.T("print.label.template")), 0, 1);

            _cmbTemplate = CreateComboBox();
            controlsLayout.Controls.Add(CreateInputFrame(_cmbTemplate, bottomMargin: 14), 0, 2);

            controlsLayout.Controls.Add(CreateSectionLabel(AppLocalization.T("print.label.range")), 0, 3);
            _cmbRange = CreateComboBox();
            controlsLayout.Controls.Add(CreateInputFrame(_cmbRange, bottomMargin: 12), 0, 4);

            var dateRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 2,
                Margin = new Padding(0),
                AutoSize = true
            };
            dateRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            dateRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            dateRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            dateRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            controlsLayout.Controls.Add(CreateInputFrame(dateRow, bottomMargin: 14, padding: new Padding(12, 8, 12, 10)), 0, 5);

            dateRow.Controls.Add(CreateFieldLabel(AppLocalization.T("print.label.from")), 0, 0);
            dateRow.Controls.Add(CreateFieldLabel(AppLocalization.T("print.label.to")), 1, 0);

            _dtFrom = CreateDatePicker();
            _dtTo = CreateDatePicker();
            dateRow.Controls.Add(_dtFrom, 0, 1);
            dateRow.Controls.Add(_dtTo, 1, 1);

            controlsLayout.Controls.Add(CreateSectionLabel(AppLocalization.T("print.label.note")), 0, 6);

            var notePanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 2,
                AutoSize = true,
                Margin = new Padding(0)
            };
            notePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            notePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            notePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var noteHeight = UiMetrics.GetNoteBoxHeight(BrandTheme.CreateFont(9.5f), 3, 12);
            _txtNote = new TextBox
            {
                Dock = DockStyle.Top,
                Height = noteHeight,
                Multiline = true,
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                ForeColor = Color.Black,
                Font = BrandTheme.CreateFont(9.5f),
                Margin = new Padding(0),
                MinimumSize = new Size(0, noteHeight),
                ScrollBars = ScrollBars.Vertical
            };
            notePanel.Controls.Add(CreateInputFrame(_txtNote, bottomMargin: 8, padding: new Padding(12, 10, 12, 10)), 0, 0);

            _btnApplyNote = CreateInlineActionButton(AppLocalization.T("print.button.applyNote"));
            notePanel.Controls.Add(_btnApplyNote, 0, 1);
            controlsLayout.Controls.Add(notePanel, 0, 7);

            var hint = new Label
            {
                Text = AppLocalization.T("print.panel.hint"),
                AutoSize = true,
                Font = BrandTheme.CreateFont(8.6f),
                ForeColor = Color.FromArgb(88, 88, 88),
                Margin = new Padding(2, 10, 2, 0)
            };
            controlsLayout.Controls.Add(hint, 0, 8);

            var actions = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Margin = new Padding(0, 0, 0, 0),
                Padding = new Padding(0)
            };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            actions.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            actions.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            actions.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            actions.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            actions.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            actionsHost.Controls.Add(actions);

            var actionsTitle = new Label
            {
                Text = AppLocalization.T("print.panel.actions").ToUpperInvariant(),
                Dock = DockStyle.Top,
                AutoSize = true,
                Font = BrandTheme.CreateFont(8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 80, 80),
                Margin = new Padding(0, 0, 0, 8)
            };
            actions.Controls.Add(actionsTitle, 0, 0);

            _btnPrint = CreateActionButton(AppLocalization.T("print.button.print"), filled: true);
            _btnSavePdf = CreateActionButton(AppLocalization.T("print.button.savePdf"), filled: false);
            _btnExport = CreateActionButton(AppLocalization.T("print.button.export"), filled: false);
            _lblStatus = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Font = BrandTheme.CreateFont(8.8f),
                ForeColor = BrandTheme.MutedText,
                Margin = new Padding(0, 8, 0, 0)
            };

            actions.Controls.Add(_btnPrint, 0, 1);
            actions.Controls.Add(_btnSavePdf, 0, 2);
            actions.Controls.Add(_btnExport, 0, 3);
            actions.Controls.Add(_lblStatus, 0, 4);

            _cmbTemplate.SelectedIndexChanged += (_, __) => SchedulePreviewRefresh();
            _cmbRange.SelectedIndexChanged += (_, __) =>
            {
                ApplyRangeSelectionState();
                SchedulePreviewRefresh();
            };
            _dtFrom.ValueChanged += (_, __) => SchedulePreviewRefresh();
            _dtTo.ValueChanged += (_, __) => SchedulePreviewRefresh();
            _btnApplyNote.Click += (_, __) => ApplyNoteAndRefresh();
            _btnPrint.Click += async (_, __) => await PrintAsync();
            _btnSavePdf.Click += async (_, __) => await SavePdfAsync();
            _btnExport.Click += async (_, __) => await ExportHtmlAsync();

            ResumeLayout(true);
        }

        private async Task InitializeAsync()
        {
            _isInitializing = true;
            try
            {
                _cmbTemplate.DataSource = PrintReportComposer.CreateTemplateOptions().ToList();
                _cmbTemplate.DisplayMember = nameof(PrintTemplateOption.Display);
                _cmbTemplate.ValueMember = nameof(PrintTemplateOption.Template);
                _cmbTemplate.SelectedValue = PrintReportTemplate.ExecutiveSummary;

                _cmbRange.DataSource = PrintRangeCatalog.CreateLocalizedOptions(DateTime.Today).ToList();
                _cmbRange.DisplayMember = nameof(PrintRangeOption.Display);
                _cmbRange.ValueMember = nameof(PrintRangeOption.Code);
                _cmbRange.SelectedValue = SummaryRangeCatalog.Last30Days;

                _dtFrom.Value = DateTime.Today.AddDays(-29);
                _dtTo.Value = DateTime.Today;
                _txtNote.Text = _appliedNote;
                ApplyRangeSelectionState();
            }
            finally
            {
                _isInitializing = false;
            }

            await RefreshPreviewAsync();
        }

        private void ApplyRangeSelectionState()
        {
            var isCustom = string.Equals(_cmbRange.SelectedValue as string, PrintRangeCatalog.Custom, StringComparison.OrdinalIgnoreCase);
            _dtFrom.Enabled = isCustom;
            _dtTo.Enabled = isCustom;

            if (!isCustom && PrintRangeCatalog.TryGetRange(_cmbRange.SelectedValue as string, DateTime.Today, out var from, out var to))
            {
                _dtFrom.Value = from;
                _dtTo.Value = to;
            }
        }

        private void SchedulePreviewRefresh()
        {
            if (_isInitializing)
                return;

            _refreshTimer.Stop();
            _refreshTimer.Start();
        }

        private void ApplyNoteAndRefresh()
        {
            _appliedNote = _txtNote.Text;
            SchedulePreviewRefresh();
        }

        private async Task RefreshPreviewAsync()
        {
            var previewRequest = TryCreateRequest(recordLimit: 10, isPreview: true, out var validationMessage);
            if (previewRequest is null)
            {
                _lblStatus.Text = validationMessage;
                _lblStatus.ForeColor = Color.FromArgb(173, 59, 56);
                return;
            }

            var version = ++_refreshVersion;
            SetActionState(false);
            _lblStatus.Text = AppLocalization.T("print.status.loading");
            _lblStatus.ForeColor = BrandTheme.MutedText;

            try
            {
                var report = await BuildReportAsync(previewRequest);
                if (version != _refreshVersion)
                    return;

                _currentPreviewReport = report;
                _currentPreviewDocument?.Dispose();
                _currentPreviewDocument = PrintReportDocumentFactory.Create(report);
                _previewControl.Document = _currentPreviewDocument;
                _lblStatus.Text = AppLocalization.F("print.status.ready", report.VisibleRecordCount);
                _lblStatus.ForeColor = Color.FromArgb(55, 55, 55);
            }
            catch (Exception ex)
            {
                _lblStatus.Text = AppLocalization.F("print.status.error", ex.Message);
                _lblStatus.ForeColor = Color.FromArgb(173, 59, 56);
            }
            finally
            {
                SetActionState(true);
            }
        }

        private async Task PrintAsync()
        {
            var recordLimit = await ResolveRecordLimitAsync(forPrint: true);
            if (recordLimit == int.MinValue)
                return;

            var request = TryCreateRequest(recordLimit < 0 ? null : recordLimit, isPreview: false, out var validationMessage);
            if (request is null)
            {
                MessageBox.Show(validationMessage, AppLocalization.T("print.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var report = await BuildReportAsync(request);
            using var document = PrintReportDocumentFactory.Create(report);
            using var dialog = new PrintDialog
            {
                Document = document,
                AllowSomePages = false,
                UseEXDialog = true
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            document.Print();
        }

        private async Task SavePdfAsync()
        {
            var request = TryCreateRequest(recordLimit: null, isPreview: false, out var validationMessage);
            if (request is null)
            {
                MessageBox.Show(validationMessage, AppLocalization.T("print.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var dialog = new SaveFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                FileName = BuildExportFileName("pdf")
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            var printerName = PrinterSettings.InstalledPrinters
                .Cast<string>()
                .FirstOrDefault(x => string.Equals(x, "Microsoft Print to PDF", StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrWhiteSpace(printerName))
            {
                MessageBox.Show(
                    AppLocalization.T("print.error.pdfPrinterMissing"),
                    AppLocalization.T("print.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var report = await BuildReportAsync(request);
            using var document = PrintReportDocumentFactory.Create(report);
            document.PrintController = new StandardPrintController();
            document.PrinterSettings.PrinterName = printerName;
            document.PrinterSettings.PrintToFile = true;
            document.PrinterSettings.PrintFileName = dialog.FileName;
            document.Print();

            MessageBox.Show(
                AppLocalization.F("print.export.saved", dialog.FileName),
                AppLocalization.T("print.title"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private async Task ExportHtmlAsync()
        {
            var request = TryCreateRequest(recordLimit: null, isPreview: false, out var validationMessage);
            if (request is null)
            {
                MessageBox.Show(validationMessage, AppLocalization.T("print.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var dialog = new SaveFileDialog
            {
                Filter = "HTML Files (*.html)|*.html",
                FileName = BuildExportFileName("html")
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            var report = await BuildReportAsync(request);
            var html = PrintReportHtmlExporter.Generate(report);
            File.WriteAllText(dialog.FileName, html, Encoding.UTF8);

            MessageBox.Show(
                AppLocalization.F("print.export.saved", dialog.FileName),
                AppLocalization.T("print.title"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private Task<int> ResolveRecordLimitAsync(bool forPrint)
        {
            if ((_cmbTemplate.SelectedValue is not PrintReportTemplate template) || template != PrintReportTemplate.AccountingReport)
                return Task.FromResult(-1);

            if (!forPrint)
                return Task.FromResult(-1);

            using var dialog = new PrintRecordScopeDialog();
            return Task.FromResult(dialog.ShowDialog(this) == DialogResult.OK
                ? dialog.SelectedLimit ?? -1
                : int.MinValue);
        }

        private async Task<PrintReportData> BuildReportAsync(PrintReportRequest request)
        {
            var summary = await _summaryService.GetSummaryAsync(request.From, request.To);
            var records = await _kasaService.GetAllAsync(request.From, request.To);
            var activeBusiness = await _isletmeService.GetActiveAsync();
            return PrintReportComposer.Compose(request, activeBusiness.Ad, summary, records);
        }

        private PrintReportRequest? TryCreateRequest(int? recordLimit, bool isPreview, out string validationMessage)
        {
            validationMessage = string.Empty;

            if (_cmbTemplate.SelectedValue is not PrintReportTemplate template)
            {
                validationMessage = AppLocalization.T("print.validation.template");
                return null;
            }

            var rangeCode = PrintRangeCatalog.NormalizeCode(_cmbRange.SelectedValue as string, SummaryRangeCatalog.Last30Days);
            DateTime from;
            DateTime to;
            string rangeDisplay;

            if (string.Equals(rangeCode, PrintRangeCatalog.Custom, StringComparison.OrdinalIgnoreCase))
            {
                from = _dtFrom.Value.Date;
                to = _dtTo.Value.Date;
                if (from > to)
                {
                    validationMessage = AppLocalization.T("print.validation.dateRange");
                    return null;
                }

                rangeDisplay = AppLocalization.F("print.range.between", from, to);
            }
            else
            {
                (from, to) = SummaryRangeCatalog.GetRange(rangeCode, DateTime.Today);
                rangeDisplay = AppLocalization.F(
                    "print.range.named",
                    PrintRangeCatalog.GetDisplay(rangeCode, DateTime.Today),
                    from,
                    to);
            }

            return new PrintReportRequest
            {
                Template = template,
                From = from,
                To = to,
                RangeDisplay = rangeDisplay,
                Note = _appliedNote,
                GeneratedAt = DateTime.Now,
                RecordLimit = recordLimit,
                IsPreview = isPreview
            };
        }

        private void SetActionState(bool enabled)
        {
            _btnPrint.Enabled = enabled;
            _btnSavePdf.Enabled = enabled;
            _btnExport.Enabled = enabled;
        }

        private string BuildExportFileName(string extension)
        {
            var templateName = _cmbTemplate.SelectedValue is PrintReportTemplate.AccountingReport
                ? "muhasebe-raporu"
                : "yonetici-ozeti";
            return $"cashtracker-{templateName}-{DateTime.Now:yyyyMMdd-HHmm}.{extension}";
        }

        private static Label CreateFieldLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = BrandTheme.CreateFont(8.2f, FontStyle.Bold),
                ForeColor = Color.FromArgb(86, 86, 86),
                Margin = new Padding(0, 0, 0, 6)
            };
        }

        private static Label CreateSectionLabel(string text)
        {
            return new Label
            {
                Text = text.ToUpperInvariant(),
                AutoSize = true,
                Font = BrandTheme.CreateFont(8.3f, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 52, 52),
                Margin = new Padding(2, 0, 2, 6)
            };
        }

        private static Panel CreateInputFrame(Control child, int bottomMargin, Padding? padding = null)
        {
            return FormFactory.CreateInputFrame(child, bottomMargin, padding);
        }

        private static ComboBox CreateComboBox()
        {
            var font = BrandTheme.CreateFont(9.5f);
            return new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                IntegralHeight = false,
                BackColor = Color.White,
                ForeColor = Color.Black,
                Font = font,
                Height = UiMetrics.GetInputHeight(font),
                MinimumSize = new Size(0, UiMetrics.GetInputHeight(font)),
                Margin = new Padding(0)
            };
        }

        private static DateTimePicker CreateDatePicker()
        {
            var font = BrandTheme.CreateFont(9.3f);
            return new DateTimePicker
            {
                Dock = DockStyle.Top,
                Format = DateTimePickerFormat.Short,
                CalendarForeColor = Color.Black,
                CalendarMonthBackground = Color.White,
                Font = font,
                MinimumSize = new Size(0, UiMetrics.GetInputHeight(font)),
                Margin = new Padding(0, 0, 10, 0)
            };
        }

        private static Button CreateActionButton(string text, bool filled)
        {
            var font = BrandTheme.CreateFont(9.8f, FontStyle.Bold);
            var button = new Button
            {
                Text = text,
                Dock = DockStyle.Top,
                MinimumSize = new Size(0, UiMetrics.GetButtonHeight(font, 42, 16)),
                BackColor = filled ? Color.Black : Color.White,
                ForeColor = filled ? Color.White : Color.Black,
                FlatStyle = FlatStyle.Flat,
                Font = font,
                Padding = UiMetrics.ButtonPadding,
                Margin = new Padding(0, 0, 0, 10)
            };

            button.FlatAppearance.BorderColor = Color.Black;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = filled ? Color.FromArgb(35, 35, 35) : Color.FromArgb(242, 242, 242);
            return button;
        }

        private static Button CreateInlineActionButton(string text)
        {
            var font = BrandTheme.CreateFont(8.9f, FontStyle.Bold);
            var button = new Button
            {
                Text = text,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Left,
                BackColor = Color.White,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Font = font,
                MinimumSize = new Size(0, UiMetrics.GetButtonHeight(font)),
                Padding = UiMetrics.ButtonPadding,
                Margin = new Padding(0)
            };

            button.FlatAppearance.BorderColor = Color.FromArgb(112, 112, 112);
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(244, 244, 244);
            return button;
        }
    }
}
