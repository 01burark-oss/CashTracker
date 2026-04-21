using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using CashTracker.App.UI;
using CashTracker.Core.Models;
using CashTracker.Core.Services;

namespace CashTracker.App.Forms
{
    internal sealed class GibPortalSettingsForm : Form
    {
        private readonly IGibPortalService _gibPortalService;
        private readonly TextBox _txtKullaniciKodu = new();
        private readonly TextBox _txtSifre = new();
        private readonly CheckBox _chkTestModu = new();
        private readonly Label _lblPasswordInfo = new();
        private readonly Label _lblStatus = new();

        public GibPortalSettingsForm(IGibPortalService gibPortalService)
        {
            _gibPortalService = gibPortalService;
            Text = "GIB Portal Ayarlari";
            Width = 620;
            Height = 380;
            UiMetrics.ApplyFullscreenDialogDefaults(this);
            Font = new Font("Segoe UI", 10f);
            BuildUi();
            Load += async (_, __) => await LoadSettingsAsync();
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
                Text = "GIB e-Arsiv Portal yerel otomasyonu",
                Font = new Font(Font.FontFamily, 14f, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8)
            }, 0, 0);

            root.Controls.Add(new Label
            {
                Text = "Kullanici kodu ve parola sadece bu bilgisayarda Windows DPAPI ile sifreli saklanir. Parola ekranda geri gosterilmez.",
                AutoSize = true,
                MaximumSize = new Size(540, 0),
                ForeColor = Color.FromArgb(76, 86, 106),
                Margin = new Padding(0, 0, 0, 14)
            }, 0, 1);

            var form = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, AutoSize = true };
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.Controls.Add(form, 0, 2);

            _txtSifre.UseSystemPasswordChar = true;
            _chkTestModu.Text = "Test portalini kullan";
            _lblPasswordInfo.AutoSize = true;
            _lblPasswordInfo.ForeColor = Color.FromArgb(76, 86, 106);

            AddRow(form, 0, "Kullanici kodu", _txtKullaniciKodu);
            AddRow(form, 1, "Parola", _txtSifre);
            AddRow(form, 2, "", _lblPasswordInfo);
            AddRow(form, 3, "", _chkTestModu);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding(0, 18, 0, 12)
            };
            root.Controls.Add(buttons, 0, 3);
            var btnSave = CreateButton("Kaydet");
            var btnTest = CreateButton("Baglantiyi Test Et");
            var btnClose = CreateButton("Kapat");
            buttons.Controls.AddRange([btnSave, btnTest, btnClose]);
            btnSave.Click += async (_, __) => await SaveAsync();
            btnTest.Click += async (_, __) => await TestAsync();
            btnClose.Click += (_, __) => Close();

            _lblStatus.AutoSize = true;
            _lblStatus.MaximumSize = new Size(540, 0);
            root.Controls.Add(_lblStatus, 0, 4);
        }

        private async Task LoadSettingsAsync()
        {
            var settings = await _gibPortalService.GetSettingsAsync();
            if (settings == null)
            {
                _lblPasswordInfo.Text = "Parola kayitli degil.";
                return;
            }

            _txtKullaniciKodu.Text = settings.KullaniciKodu;
            _txtSifre.Clear();
            _chkTestModu.Checked = settings.TestModu;
            _lblPasswordInfo.Text = settings.HasPassword
                ? "Kayitli parola var. Degistirmek istemiyorsaniz parola alanini bos birakin."
                : "Parola kayitli degil.";
        }

        private async Task SaveAsync()
        {
            try
            {
                await _gibPortalService.SaveSettingsAsync(new GibPortalSaveSettingsRequest
                {
                    KullaniciKodu = _txtKullaniciKodu.Text,
                    Sifre = string.IsNullOrWhiteSpace(_txtSifre.Text) ? null : _txtSifre.Text,
                    TestModu = _chkTestModu.Checked
                });
                _txtSifre.Clear();
                await LoadSettingsAsync();
                ShowStatus("Ayarlar kaydedildi.", success: true);
            }
            catch (Exception ex)
            {
                ShowStatus(ex.Message, success: false);
            }
        }

        private async Task TestAsync()
        {
            try
            {
                await SaveAsync();
                var result = await _gibPortalService.TestConnectionAsync();
                ShowStatus(result.Message, result.Success);
            }
            catch (Exception ex)
            {
                ShowStatus(ex.Message, success: false);
            }
        }

        private void ShowStatus(string message, bool success)
        {
            _lblStatus.Text = message;
            _lblStatus.ForeColor = success ? Color.FromArgb(17, 121, 85) : Color.FromArgb(173, 59, 56);
        }

        private static void AddRow(TableLayoutPanel table, int row, string label, Control input)
        {
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 7, 8, 7) }, 0, row);
            input.Dock = DockStyle.Fill;
            input.Margin = new Padding(0, 3, 0, 3);
            table.Controls.Add(input, 1, row);
        }

        private static Button CreateButton(string text)
        {
            return new Button { Text = text, Width = text.Length > 12 ? 150 : 100, Height = 34, Margin = new Padding(0, 0, 8, 0) };
        }
    }
}
