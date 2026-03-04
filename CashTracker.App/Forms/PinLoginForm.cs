using System;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using CashTracker.App;
using CashTracker.App.UI;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Services;

namespace CashTracker.App.Forms
{
    internal sealed class PinLoginForm : Form
    {
        private readonly IAppSecurityService _appSecurityService;
        private readonly BackupReportService _backupReport;
        private readonly TelegramSettings _telegramSettings;
        private readonly TextBox _txtPin;
        private readonly Label _lblError;
        private readonly Button _btnLogin;
        private readonly Button _btnCancel;
        private readonly Button _btnForgotPin;
        private bool _isProcessing;

        public PinLoginForm(
            IAppSecurityService appSecurityService,
            BackupReportService backupReport,
            TelegramSettings telegramSettings)
        {
            _appSecurityService = appSecurityService;
            _backupReport = backupReport;
            _telegramSettings = telegramSettings;

            Text = AppLocalization.T("pin.title");
            Width = 620;
            Height = 420;
            MinimumSize = new Size(620, 420);
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
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
                Padding = new Padding(26, 22, 26, 22)
            };
            card.Paint += (_, e) => ControlPaint.DrawBorder(
                e.Graphics,
                card.ClientRectangle,
                Color.FromArgb(211, 221, 234),
                ButtonBorderStyle.Solid);
            shell.Controls.Add(card);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            card.Controls.Add(root);

            var lblTitle = new Label
            {
                Text = AppLocalization.T("pin.header"),
                AutoSize = true,
                Font = BrandTheme.CreateHeadingFont(16f, FontStyle.Bold),
                ForeColor = BrandTheme.Heading,
                Margin = new Padding(0, 0, 0, 8)
            };
            root.Controls.Add(lblTitle, 0, 0);

            var lblInfo = new Label
            {
                Text = AppLocalization.T("pin.info"),
                AutoSize = true,
                Font = BrandTheme.CreateFont(9.4f),
                ForeColor = BrandTheme.MutedText,
                Margin = new Padding(0, 0, 0, 18)
            };
            root.Controls.Add(lblInfo, 0, 1);

            var pinRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 10)
            };
            pinRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            pinRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            pinRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            root.Controls.Add(pinRow, 0, 2);

            _txtPin = new TextBox
            {
                Dock = DockStyle.Fill,
                MaxLength = 4,
                UseSystemPasswordChar = true,
                Font = BrandTheme.CreateHeadingFont(14f, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Center,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0)
            };
            _txtPin.KeyPress += (_, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                    e.Handled = true;
            };
            pinRow.Controls.Add(_txtPin, 1, 0);

            _lblError = new Label
            {
                Text = string.Empty,
                AutoSize = true,
                ForeColor = Color.FromArgb(173, 59, 56),
                Font = BrandTheme.CreateFont(9f),
                Margin = new Padding(2, 0, 2, 10)
            };
            root.Controls.Add(_lblError, 0, 3);
            _txtPin.TextChanged += (_, __) => _lblError.Text = string.Empty;

            var actionGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = true,
                Margin = new Padding(0)
            };
            actionGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
            actionGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
            actionGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.Controls.Add(actionGrid, 0, 4);

            _btnLogin = CreateButton(AppLocalization.T("pin.button.login"), BrandTheme.Navy);
            _btnCancel = CreateButton(AppLocalization.T("pin.button.exit"), Color.FromArgb(102, 114, 128));
            _btnForgotPin = CreateButton(AppLocalization.T("pin.button.forgot"), BrandTheme.Teal);
            _btnForgotPin.Dock = DockStyle.Fill;
            _btnForgotPin.Margin = new Padding(0, 0, 10, 0);
            actionGrid.Controls.Add(_btnForgotPin, 0, 0);

            var primaryButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                Margin = new Padding(0)
            };
            primaryButtons.Controls.Add(_btnLogin);
            primaryButtons.Controls.Add(_btnCancel);
            actionGrid.Controls.Add(primaryButtons, 1, 0);

            _btnLogin.Click += async (_, __) => await AuthenticateAsync();
            _btnForgotPin.Click += async (_, __) => await SendPinReminderAsync();
            _btnCancel.Click += (_, __) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            AcceptButton = _btnLogin;
            CancelButton = _btnCancel;
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            try
            {
                await _appSecurityService.GetPinAsync();
            }
            catch
            {
                _lblError.Text = AppLocalization.T("pin.error.storage");
            }

            _txtPin.Focus();
        }

        private async Task SendPinReminderAsync()
        {
            if (_isProcessing)
                return;

            if (!_telegramSettings.IsEnabled)
            {
                MessageBox.Show(
                    AppLocalization.T("pin.forgot.telegramNotConfigured"),
                    AppLocalization.T("pin.forgot.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                AppLocalization.T("pin.forgot.confirm"),
                AppLocalization.T("pin.forgot.title"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            _btnForgotPin.Enabled = false;

            try
            {
                var currentPin = await _appSecurityService.GetPinAsync();
                var stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                var text = AppLocalization.F("pin.forgot.messageToTelegram", currentPin, stamp);
                await _backupReport.SendTextAsync(text);

                MessageBox.Show(
                    AppLocalization.T("pin.forgot.sent"),
                    AppLocalization.T("pin.forgot.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    AppLocalization.F("pin.forgot.sendError", ex.Message),
                    AppLocalization.T("pin.forgot.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _btnForgotPin.Enabled = true;
            }
        }

        private async Task AuthenticateAsync()
        {
            if (_isProcessing)
                return;

            var enteredPin = (_txtPin.Text ?? string.Empty).Trim();
            if (!IsValidPin(enteredPin))
            {
                _lblError.Text = AppLocalization.T("pin.error.invalid");
                return;
            }

            _isProcessing = true;
            _btnLogin.Enabled = false;

            try
            {
                var currentPin = await _appSecurityService.GetPinAsync();
                if (!string.Equals(enteredPin, currentPin, StringComparison.Ordinal))
                {
                    _lblError.Text = AppLocalization.T("pin.error.wrong");
                    _txtPin.SelectAll();
                    return;
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                _lblError.Text = AppLocalization.F("pin.error.verifyFail", ex.Message);
            }
            finally
            {
                _btnLogin.Enabled = true;
                _isProcessing = false;
            }
        }

        private static bool IsValidPin(string pin)
        {
            return pin.Length == 4 && int.TryParse(pin, out _);
        }

        private static Button CreateButton(string text, Color backColor)
        {
            var font = BrandTheme.CreateHeadingFont(9.2f, FontStyle.Bold);

            var button = new Button
            {
                Text = text,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(118, 38),
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = font,
                Padding = new Padding(16, 0, 16, 0),
                Margin = new Padding(8, 0, 0, 0)
            };
            button.FlatAppearance.BorderColor = Color.FromArgb(21, 38, 61);
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                Math.Max(backColor.R - 14, 0),
                Math.Max(backColor.G - 14, 0),
                Math.Max(backColor.B - 14, 0));
            return button;
        }
    }
}
