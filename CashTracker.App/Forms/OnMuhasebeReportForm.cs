using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using CashTracker.App.UI;
using CashTracker.Core.Services;

namespace CashTracker.App.Forms
{
    internal sealed class OnMuhasebeReportForm : Form
    {
        private readonly IOnMuhasebeReportService _reportService;
        private readonly DateTimePicker _monthPicker = new();
        private readonly TextBox _txtOutput = new();
        private readonly Label _lblStatus = new();

        public OnMuhasebeReportForm(IOnMuhasebeReportService reportService)
        {
            _reportService = reportService;
            Text = "Muhasebeci Raporu";
            Width = 620;
            Height = 300;
            UiMetrics.ApplyFullscreenDialogDefaults(this);
            Font = new Font("Segoe UI", 10f);
            BuildUi();
        }

        private void BuildUi()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(22)
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(root);

            root.Controls.Add(new Label
            {
                Text = "Aylik muhasebeci paketi",
                Font = new Font(Font.FontFamily, 14f, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8)
            }, 0, 0);

            root.Controls.Add(new Label
            {
                Text = "Fatura, cari, stok, gelir/gider ve KDV ozetleri CSV/HTML olarak uretilir ve ZIP paketine alinir.",
                AutoSize = true,
                MaximumSize = new Size(540, 0),
                ForeColor = Color.FromArgb(76, 86, 106),
                Margin = new Padding(0, 0, 0, 14)
            }, 0, 1);

            var form = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 3, AutoSize = true };
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            root.Controls.Add(form, 0, 2);

            _monthPicker.Format = DateTimePickerFormat.Custom;
            _monthPicker.CustomFormat = "yyyy-MM";
            _monthPicker.ShowUpDown = true;
            _txtOutput.ReadOnly = true;
            _txtOutput.Text = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            form.Controls.Add(new Label { Text = "Donem", AutoSize = true, Margin = new Padding(0, 7, 8, 7) }, 0, 0);
            form.Controls.Add(_monthPicker, 1, 0);
            form.Controls.Add(new Label(), 2, 0);
            form.Controls.Add(new Label { Text = "Klasor", AutoSize = true, Margin = new Padding(0, 7, 8, 7) }, 0, 1);
            form.Controls.Add(_txtOutput, 1, 1);
            var btnBrowse = CreateButton("Sec", 78);
            form.Controls.Add(btnBrowse, 2, 1);
            btnBrowse.Click += (_, __) => SelectFolder();

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding(0, 18, 0, 12)
            };
            root.Controls.Add(buttons, 0, 3);
            var btnCreate = CreateButton("Paketi Olustur", 130);
            var btnClose = CreateButton("Kapat", 90);
            buttons.Controls.AddRange([btnCreate, btnClose]);
            btnCreate.Click += async (_, __) => await CreateReportAsync();
            btnClose.Click += (_, __) => Close();

            _lblStatus.AutoSize = true;
            _lblStatus.MaximumSize = new Size(540, 0);
            root.Controls.Add(_lblStatus, 0, 4);
        }

        private void SelectFolder()
        {
            using var dialog = new FolderBrowserDialog { SelectedPath = _txtOutput.Text };
            if (dialog.ShowDialog(this) == DialogResult.OK)
                _txtOutput.Text = dialog.SelectedPath;
        }

        private async Task CreateReportAsync()
        {
            try
            {
                var zipPath = await _reportService.CreateMonthlyExportAsync(_monthPicker.Value, _txtOutput.Text);
                _lblStatus.ForeColor = Color.FromArgb(17, 121, 85);
                _lblStatus.Text = $"Paket olustu: {zipPath}";
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{zipPath}\"") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                _lblStatus.ForeColor = Color.FromArgb(173, 59, 56);
                _lblStatus.Text = ex.Message;
            }
        }

        private static Button CreateButton(string text, int width)
        {
            return new Button { Text = text, Width = width, Height = 34, Margin = new Padding(0, 0, 8, 0) };
        }
    }
}
