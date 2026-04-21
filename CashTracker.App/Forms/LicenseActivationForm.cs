using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using CashTracker.App.Services;
using CashTracker.App.UI;
using CashTracker.Core.Models;

namespace CashTracker.App.Forms
{
    internal sealed class LicenseActivationForm : Form
    {
        private readonly ILicenseService _licenseService;
        private readonly ReceiptOcrSettings _receiptOcrSettings;
        private readonly TextBox _txtInstallCode;
        private readonly TextBox _txtLicenseKey;
        private readonly Label _lblStatus;
        private readonly Button _btnActivate;

        public LicenseActivationForm(ILicenseService licenseService, ReceiptOcrSettings receiptOcrSettings)
        {
            _licenseService = licenseService;
            _receiptOcrSettings = receiptOcrSettings;

            Text = "CashTracker Lisans Aktivasyonu";
            Width = 780;
            Height = 620;
            MinimumSize = new Size(780, 620);
            UiMetrics.ApplyFullscreenDialogDefaults(this, FormStartPosition.CenterScreen);
            BackColor = BrandTheme.AppBackground;
            Font = BrandTheme.CreateFont(10f);
            if (AppIconProvider.Current is Icon appIcon)
                Icon = appIcon;

            var shell = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24),
                BackColor = BrandTheme.AppBackground
            };
            Controls.Add(shell);

            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(24)
            };
            shell.Controls.Add(card);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            card.Controls.Add(root);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 9,
                Margin = Padding.Empty
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (var i = 0; i < 9; i++)
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.Controls.Add(layout, 0, 0);

            var subtitle = new Label
            {
                Text = "Asagidaki kurulum kodunu iletin ve size verilen tek kullanimlik lisans anahtarini yapistirin.",
                AutoSize = true,
                Font = BrandTheme.CreateFont(9.4f),
                ForeColor = BrandTheme.MutedText,
                MaximumSize = new Size(620, 0),
                Margin = new Padding(0, 0, 0, 14)
            };

            var activationHintLabel = new Label
            {
                AutoSize = true,
                Font = BrandTheme.CreateFont(9f),
                ForeColor = BrandTheme.MutedText,
                MaximumSize = new Size(620, 0),
                Margin = new Padding(2, 0, 2, 10),
                Text = "Bu ekranda etkinlestirilen lisans anahtari kurulum koduna ozel uretilir. OCR paketi tanimliysa fis OCR ozelligi aktivasyondan sonra otomatik acilir."
            };

            var contactLabel = new Label
            {
                AutoSize = true,
                Font = BrandTheme.CreateFont(8.9f),
                ForeColor = BrandTheme.MutedText,
                MaximumSize = new Size(620, 0),
                Margin = new Padding(2, 2, 2, 4),
                Text = "Lisans ve OCR aktivasyonu icin iletisim: Instagram @_6uwak | E-posta: 01burark@gmail.com"
            };

            layout.Controls.Add(new Label
            {
                Text = "Lisans aktivasyonu gerekli",
                AutoSize = true,
                Font = BrandTheme.CreateHeadingFont(16f, FontStyle.Bold),
                ForeColor = BrandTheme.Heading,
                Margin = new Padding(0, 0, 0, 10)
            }, 0, 0);
            layout.Controls.Add(subtitle, 0, 1);
            layout.Controls.Add(activationHintLabel, 0, 2);

            layout.Controls.Add(CreateFieldLabel("Kurulum Kodu"), 0, 3);

            var inputFont = BrandTheme.CreateFont(10f);
            var deviceRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 0, 10)
            };
            deviceRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            deviceRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            _txtInstallCode = new TextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoSize = false,
                BorderStyle = BorderStyle.FixedSingle,
                Font = inputFont,
                Height = UiMetrics.GetInputHeight(inputFont),
                MinimumSize = new Size(0, UiMetrics.GetInputHeight(inputFont)),
                Text = _licenseService.GetInstallCode()
            };
            var btnCopy = CreateButton("Kopyala", BrandTheme.Teal);
            btnCopy.Click += (_, __) => Clipboard.SetText(_txtInstallCode.Text);
            deviceRow.Controls.Add(_txtInstallCode, 0, 0);
            deviceRow.Controls.Add(btnCopy, 1, 0);
            layout.Controls.Add(deviceRow, 0, 4);

            layout.Controls.Add(CreateFieldLabel("Lisans Anahtari"), 0, 5);

            var licenseKeyHeight = UiMetrics.GetNoteBoxHeight(BrandTheme.CreateFont(9.5f), 4, 18);
            _txtLicenseKey = new TextBox
            {
                Dock = DockStyle.Top,
                Multiline = true,
                Height = licenseKeyHeight,
                MinimumSize = new Size(0, licenseKeyHeight),
                BorderStyle = BorderStyle.FixedSingle,
                ScrollBars = ScrollBars.Vertical,
                Font = BrandTheme.CreateFont(9.5f),
                Margin = new Padding(0, 0, 0, 8)
            };
            layout.Controls.Add(_txtLicenseKey, 0, 6);

            _lblStatus = new Label
            {
                AutoSize = true,
                ForeColor = BrandTheme.MutedText,
                Font = BrandTheme.CreateFont(9.2f),
                MaximumSize = new Size(620, 0),
                Margin = new Padding(2, 8, 2, 8)
            };
            layout.Controls.Add(_lblStatus, 0, 7);

            layout.Controls.Add(contactLabel, 0, 8);

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0, 12, 0, 0)
            };
            var btnCancel = CreateButton("Kapat", Color.FromArgb(100, 112, 126));
            _btnActivate = CreateButton("Aktive Et", BrandTheme.Navy);
            btnCancel.Click += (_, __) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };
            _btnActivate.Click += async (_, __) => await ActivateAsync();
            actions.Controls.Add(_btnActivate);
            actions.Controls.Add(btnCancel);
            root.Controls.Add(actions, 0, 1);

            void ApplyWrapWidths()
            {
                var contentWidth = Math.Max(420, card.ClientSize.Width - 64);
                subtitle.MaximumSize = new Size(contentWidth, 0);
                activationHintLabel.MaximumSize = new Size(contentWidth, 0);
                _lblStatus.MaximumSize = new Size(contentWidth, 0);
                contactLabel.MaximumSize = new Size(contentWidth, 0);
            }

            card.Resize += (_, __) => ApplyWrapWidths();
            ApplyWrapWidths();

            Shown += async (_, __) => await LoadCurrentLicenseAsync();
        }

        private async Task LoadCurrentLicenseAsync()
        {
            var result = await _licenseService.GetCurrentStatusAsync();
            if (!result.IsValid || result.Payload is null)
            {
                _lblStatus.Text = result.Message;
                _lblStatus.ForeColor = Color.FromArgb(173, 59, 56);
                return;
            }

            _txtLicenseKey.Text = result.LicenseKey;
            _lblStatus.Text = $"Aktif lisans: {result.Payload.CustomerName} | {result.Payload.Edition}";
            _lblStatus.ForeColor = Color.FromArgb(17, 121, 85);
        }

        private async Task ActivateAsync()
        {
            _btnActivate.Enabled = false;

            try
            {
                var result = await _licenseService.ActivateAsync(_txtLicenseKey.Text);
                _lblStatus.Text = result.Message;
                _lblStatus.ForeColor = result.IsValid
                    ? Color.FromArgb(17, 121, 85)
                    : Color.FromArgb(173, 59, 56);

                if (!result.IsValid)
                    return;

                await _licenseService.ApplyReceiptOcrSettingsAsync(_receiptOcrSettings);
                DialogResult = DialogResult.OK;
                Close();
            }
            finally
            {
                _btnActivate.Enabled = true;
            }
        }

        private static Label CreateFieldLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                ForeColor = BrandTheme.Heading,
                Font = BrandTheme.CreateHeadingFont(9.5f, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 6)
            };
        }

        private static Button CreateButton(string text, Color backColor)
        {
            var font = BrandTheme.CreateHeadingFont(9.2f, FontStyle.Bold);
            var button = new Button
            {
                Text = text,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = UiMetrics.GetButtonMinimumSize(font, 118),
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = font,
                Padding = UiMetrics.ButtonPadding,
                Margin = new Padding(8, 0, 0, 0)
            };
            button.FlatAppearance.BorderColor = Color.FromArgb(21, 38, 61);
            button.FlatAppearance.BorderSize = 1;
            return button;
        }
    }
}
