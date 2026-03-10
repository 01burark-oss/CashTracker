using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using CashTracker.App;
using CashTracker.App.Controls;
using CashTracker.App.UI;
using CashTracker.Core.Services;

namespace CashTracker.App.Forms
{
    internal sealed class PinLoginForm : Form
    {
        private readonly IAppSecurityService _appSecurityService;
        private readonly PinCodeInputControl _txtPin;
        private readonly Label _lblError;
        private readonly Button _btnLogin;
        private readonly Button _btnCancel;
        private readonly Button _btnForgotPin;
        private bool _isProcessing;

        public PinLoginForm(IAppSecurityService appSecurityService)
        {
            _appSecurityService = appSecurityService;

            Text = AppLocalization.T("pin.title");
            Width = 540;
            Height = 460;
            MinimumSize = new Size(540, 460);
            UiMetrics.ApplyFormDefaults(this);
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            BackColor = BrandTheme.AppBackground;
            Font = BrandTheme.CreateFont(10f);
            if (AppIconProvider.Current is Icon appIcon)
                Icon = appIcon;

            var shell = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24),
                BackColor = BrandTheme.AppBackground,
                ColumnCount = 3,
                RowCount = 3
            };
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 372));
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            shell.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            Controls.Add(shell);

            var card = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.White,
                Padding = new Padding(28, 24, 28, 24),
                Margin = Padding.Empty
            };
            card.Paint += (_, e) => ControlPaint.DrawBorder(
                e.Graphics,
                card.ClientRectangle,
                Color.FromArgb(211, 221, 234),
                ButtonBorderStyle.Solid);
            shell.Controls.Add(card, 1, 1);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 5,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            card.Controls.Add(root);

            var headingFont = BrandTheme.CreateHeadingFont(16f, FontStyle.Bold);
            var lblTitle = new Label
            {
                Text = AppLocalization.T("pin.header"),
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = UiMetrics.GetTextLineHeight(headingFont) + 4,
                Font = headingFont,
                ForeColor = BrandTheme.Heading,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 0, 0, 8)
            };
            root.Controls.Add(lblTitle, 0, 0);

            var lblInfo = new Label
            {
                Text = AppLocalization.T("pin.info"),
                AutoSize = true,
                Font = BrandTheme.CreateFont(9.4f),
                ForeColor = BrandTheme.MutedText,
                MaximumSize = new Size(296, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.None,
                Margin = new Padding(10, 0, 10, 18)
            };
            root.Controls.Add(lblInfo, 0, 1);

            var pinRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 3,
                RowCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 0, 10)
            };
            pinRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            pinRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 248));
            pinRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            pinRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.Controls.Add(pinRow, 0, 2);

            var pinFont = BrandTheme.CreateHeadingFont(18f, FontStyle.Bold);
            _txtPin = new PinCodeInputControl
            {
                Dock = DockStyle.Fill,
                Width = 248,
                Height = 54,
                Font = pinFont,
                Margin = new Padding(0),
                PinLength = 4,
                UsePasswordMask = true
            };
            pinRow.Controls.Add(_txtPin, 1, 0);

            _lblError = new Label
            {
                Text = string.Empty,
                AutoSize = true,
                ForeColor = Color.FromArgb(173, 59, 56),
                Font = BrandTheme.CreateFont(9f),
                MaximumSize = new Size(296, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.None,
                Margin = new Padding(8, 12, 8, 12)
            };
            root.Controls.Add(_lblError, 0, 3);
            _txtPin.TextChanged += (_, __) => _lblError.Text = string.Empty;

            var actionLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 4, 0, 0)
            };
            actionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            actionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            actionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.Controls.Add(actionLayout, 0, 4);

            _btnLogin = CreateButton(AppLocalization.T("pin.button.login"), BrandTheme.Navy);
            _btnCancel = CreateButton(AppLocalization.T("pin.button.exit"), Color.FromArgb(102, 114, 128));
            _btnForgotPin = CreateButton(AppLocalization.T("pin.button.forgot"), BrandTheme.Teal);
            _btnForgotPin.AutoSize = true;
            _btnForgotPin.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _btnForgotPin.MinimumSize = UiMetrics.GetButtonMinimumSize(_btnForgotPin.Font, 0);
            _btnForgotPin.Anchor = AnchorStyles.None;
            _btnForgotPin.Margin = new Padding(0, 0, 0, 10);
            actionLayout.Controls.Add(_btnForgotPin, 0, 0);

            var primaryButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Anchor = AnchorStyles.None,
                Margin = new Padding(0)
            };
            primaryButtons.Controls.Add(_btnLogin);
            primaryButtons.Controls.Add(_btnCancel);
            actionLayout.Controls.Add(primaryButtons, 0, 1);

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

        private Task SendPinReminderAsync()
        {
            if (_isProcessing)
                return Task.CompletedTask;

            MessageBox.Show(
                "Guvenlik nedeniyle mevcut PIN gosterilemez. Yeni bir lisans/kurulum talebi icin satici ile iletisime gecin.",
                "PIN Yardimi",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return Task.CompletedTask;
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
                if (!await _appSecurityService.VerifyPinAsync(enteredPin))
                {
                    _lblError.Text = AppLocalization.T("pin.error.wrong");
                    _txtPin.ClearPin();
                    _txtPin.Focus();
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
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                Math.Max(backColor.R - 14, 0),
                Math.Max(backColor.G - 14, 0),
                Math.Max(backColor.B - 14, 0));
            return button;
        }
    }
}
