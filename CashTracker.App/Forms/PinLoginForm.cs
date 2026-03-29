using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using CashTracker.App.Controls;
using CashTracker.App.Services;
using CashTracker.App.UI;
using CashTracker.Core.Services;

namespace CashTracker.App.Forms
{
    internal sealed class PinLoginForm : Form
    {
        private readonly IAppSecurityService _appSecurityService;
        private readonly PinReminderService? _pinReminderService;
        private readonly PinCodeInputControl _txtPin;
        private readonly Label _lblError;
        private readonly Button _btnLogin;
        private readonly Button _btnCancel;
        private readonly Button _btnForgotPin;
        private readonly List<Button> _keypadButtons = new();
        private bool _isProcessing;

        public PinLoginForm(IAppSecurityService appSecurityService, PinReminderService? pinReminderService = null)
        {
            _appSecurityService = appSecurityService;
            _pinReminderService = pinReminderService;

            Text = AppLocalization.T("pin.title");
            Width = 660;
            Height = 760;
            MinimumSize = new Size(660, 760);
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
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 440f));
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 628f));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            Controls.Add(shell);

            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(24),
                Margin = Padding.Empty
            };
            card.Paint += (_, e) =>
            {
                using var pen = new Pen(Color.FromArgb(205, 217, 231), 1.1f);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };
            shell.Controls.Add(card, 1, 1);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 8,
                BackColor = Color.White,
                Margin = Padding.Empty
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 8f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 74f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 266f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62f));
            card.Controls.Add(root);

            var badgeHost = CreateCenterHost();
            root.Controls.Add(badgeHost, 0, 0);

            var badge = new Label
            {
                AutoSize = false,
                Size = new Size(126, 26),
                Text = "GUVENLI GIRIS",
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(231, 246, 242),
                ForeColor = Color.FromArgb(31, 116, 100),
                Font = BrandTheme.CreateHeadingFont(8.2f, FontStyle.Bold)
            };
            badgeHost.Controls.Add(badge);

            var lblTitle = new Label
            {
                Dock = DockStyle.Fill,
                Text = AppLocalization.T("pin.header"),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = BrandTheme.Heading,
                Font = BrandTheme.CreateHeadingFont(18f, FontStyle.Bold),
                Margin = Padding.Empty
            };
            root.Controls.Add(lblTitle, 0, 1);

            var pinHost = CreateCenterHost();
            root.Controls.Add(pinHost, 0, 3);

            _txtPin = new PinCodeInputControl
            {
                Size = new Size(284, 58),
                Font = BrandTheme.CreateHeadingFont(18f, FontStyle.Bold),
                PinLength = 4,
                UsePasswordMask = true
            };
            pinHost.Controls.Add(_txtPin);

            _lblError = new Label
            {
                Dock = DockStyle.Fill,
                Text = string.Empty,
                TextAlign = ContentAlignment.TopCenter,
                ForeColor = Color.FromArgb(173, 59, 56),
                Font = BrandTheme.CreateFont(9f),
                Margin = new Padding(8, 0, 8, 0)
            };
            root.Controls.Add(_lblError, 0, 4);
            _txtPin.TextChanged += async (_, __) =>
            {
                _lblError.Text = string.Empty;
                if (!_isProcessing && (_txtPin.Text?.Length ?? 0) == 4)
                    await AuthenticateAsync();
            };

            var keypadHost = CreateCenterHost();
            root.Controls.Add(keypadHost, 0, 5);

            var keypad = new TableLayoutPanel
            {
                Size = new Size(336, 252),
                ColumnCount = 3,
                RowCount = 4,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            for (var i = 0; i < 3; i++)
                keypad.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112f));
            for (var i = 0; i < 4; i++)
                keypad.RowStyles.Add(new RowStyle(SizeType.Absolute, 63f));
            keypadHost.Controls.Add(keypad);

            AddKeypadButton(keypad, "1", 0, 0, Color.FromArgb(249, 251, 255), BrandTheme.Heading, HandleDigitClick);
            AddKeypadButton(keypad, "2", 1, 0, Color.FromArgb(249, 251, 255), BrandTheme.Heading, HandleDigitClick);
            AddKeypadButton(keypad, "3", 2, 0, Color.FromArgb(249, 251, 255), BrandTheme.Heading, HandleDigitClick);
            AddKeypadButton(keypad, "4", 0, 1, Color.FromArgb(249, 251, 255), BrandTheme.Heading, HandleDigitClick);
            AddKeypadButton(keypad, "5", 1, 1, Color.FromArgb(249, 251, 255), BrandTheme.Heading, HandleDigitClick);
            AddKeypadButton(keypad, "6", 2, 1, Color.FromArgb(249, 251, 255), BrandTheme.Heading, HandleDigitClick);
            AddKeypadButton(keypad, "7", 0, 2, Color.FromArgb(249, 251, 255), BrandTheme.Heading, HandleDigitClick);
            AddKeypadButton(keypad, "8", 1, 2, Color.FromArgb(249, 251, 255), BrandTheme.Heading, HandleDigitClick);
            AddKeypadButton(keypad, "9", 2, 2, Color.FromArgb(249, 251, 255), BrandTheme.Heading, HandleDigitClick);
            AddKeypadButton(keypad, "Temizle", 0, 3, Color.FromArgb(255, 245, 229), Color.FromArgb(160, 102, 27), (_, __) => ClearPin());
            AddKeypadButton(keypad, "0", 1, 3, Color.FromArgb(237, 243, 251), BrandTheme.NavyDeep, HandleDigitClick);
            AddKeypadButton(keypad, "Sil", 2, 3, Color.FromArgb(239, 242, 246), Color.FromArgb(87, 99, 116), (_, __) => RemoveLastDigit());

            var forgotHost = CreateCenterHost();
            root.Controls.Add(forgotHost, 0, 6);

            _btnForgotPin = CreateFlatActionButton(AppLocalization.T("pin.button.forgot"), BrandTheme.Teal, Color.White, new Size(240, 42));
            forgotHost.Controls.Add(_btnForgotPin);

            var actionsHost = CreateCenterHost();
            root.Controls.Add(actionsHost, 0, 7);

            var actions = new TableLayoutPanel
            {
                Size = new Size(336, 52),
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 164f));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 172f));
            actions.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
            actionsHost.Controls.Add(actions);

            _btnCancel = CreateFlatActionButton(AppLocalization.T("pin.button.exit"), Color.FromArgb(235, 239, 244), Color.FromArgb(79, 92, 108), new Size(156, 48));
            _btnCancel.Margin = new Padding(0, 0, 8, 0);
            actions.Controls.Add(_btnCancel, 0, 0);

            _btnLogin = CreateFlatActionButton(AppLocalization.T("pin.button.login"), BrandTheme.Navy, Color.White, new Size(164, 48));
            _btnLogin.Margin = new Padding(8, 0, 0, 0);
            actions.Controls.Add(_btnLogin, 1, 0);

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

            if (_pinReminderService is null)
            {
                MessageBox.Show(
                    "Telegram hatirlatma servisi hazir degil.",
                    "PIN Yardimi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            SetBusyState(true);

            try
            {
                var result = await _pinReminderService.SendCurrentPinAsync();
                var icon = result.Status == PinReminderStatus.Success
                    ? MessageBoxIcon.Information
                    : MessageBoxIcon.Warning;
                MessageBox.Show(result.Message, "PIN Yardimi", MessageBoxButtons.OK, icon);
            }
            finally
            {
                SetBusyState(false);
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

            SetBusyState(true);

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
                SetBusyState(false);
            }
        }

        private void HandleDigitClick(object? sender, EventArgs e)
        {
            if (_isProcessing || sender is not Button button || string.IsNullOrWhiteSpace(button.Text))
                return;

            _txtPin.AppendDigit(button.Text[0]);
            _txtPin.Focus();
        }

        private void ClearPin()
        {
            if (_isProcessing)
                return;

            _txtPin.ClearPin();
            _txtPin.Focus();
        }

        private void RemoveLastDigit()
        {
            if (_isProcessing)
                return;

            _txtPin.RemoveLastDigit();
            _txtPin.Focus();
        }

        private void SetBusyState(bool isBusy)
        {
            _isProcessing = isBusy;
            _btnLogin.Enabled = !isBusy;
            _btnCancel.Enabled = !isBusy;
            _btnForgotPin.Enabled = !isBusy;
            foreach (var button in _keypadButtons)
                button.Enabled = !isBusy;
        }

        private void AddKeypadButton(
            TableLayoutPanel parent,
            string text,
            int column,
            int row,
            Color backColor,
            Color foreColor,
            EventHandler handler)
        {
            var button = new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                BackColor = backColor,
                ForeColor = foreColor,
                FlatStyle = FlatStyle.Flat,
                Font = BrandTheme.CreateHeadingFont(text.Length == 1 ? 18f : 10.2f, FontStyle.Bold),
                Margin = new Padding(6),
                Padding = Padding.Empty
            };
            button.FlatAppearance.BorderColor = Color.FromArgb(202, 214, 228);
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(backColor, 0.04f);
            button.Click += handler;
            parent.Controls.Add(button, column, row);
            _keypadButtons.Add(button);
        }

        private static Panel CreateCenterHost()
        {
            var host = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            void LayoutChildren()
            {
                foreach (Control child in host.Controls)
                {
                    child.Left = Math.Max((host.ClientSize.Width - child.Width) / 2, 0);
                    child.Top = Math.Max((host.ClientSize.Height - child.Height) / 2, 0);
                }
            }

            host.ControlAdded += (_, __) => LayoutChildren();
            host.Resize += (_, __) => LayoutChildren();
            return host;
        }

        private static Button CreateFlatActionButton(string text, Color backColor, Color foreColor, Size size)
        {
            var button = new Button
            {
                Text = text,
                Size = size,
                BackColor = backColor,
                ForeColor = foreColor,
                FlatStyle = FlatStyle.Flat,
                Font = BrandTheme.CreateHeadingFont(9.3f, FontStyle.Bold),
                Padding = new Padding(8, 4, 8, 4),
                TextAlign = ContentAlignment.MiddleCenter,
                UseCompatibleTextRendering = true
            };
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Color.FromArgb(205, 216, 230);
            button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(backColor, 0.04f);
            return button;
        }

        private static bool IsValidPin(string pin)
        {
            return pin.Length == 4 && int.TryParse(pin, out _);
        }
    }
}
