using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using CashTracker.App.UI;
using CashTracker.Core.Services;

namespace CashTracker.App.Forms
{
    internal sealed class PinLoginForm : Form
    {
        private readonly IAppSecurityService _appSecurityService;
        private readonly TextBox _txtPin;
        private readonly Label _lblError;
        private readonly Button _btnLogin;
        private readonly Button _btnCancel;
        private bool _isProcessing;

        public PinLoginForm(IAppSecurityService appSecurityService)
        {
            _appSecurityService = appSecurityService;

            Text = "Sifre Girisi";
            Width = 420;
            Height = 270;
            MinimumSize = new Size(420, 270);
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            BackColor = BrandTheme.AppBackground;
            Font = BrandTheme.CreateFont(10f);
            if (AppIconProvider.Current is Icon appIcon)
                Icon = appIcon;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(18, 16, 18, 16)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(root);

            var lblTitle = new Label
            {
                Text = "CashTracker Giris",
                AutoSize = true,
                Font = BrandTheme.CreateHeadingFont(14f, FontStyle.Bold),
                ForeColor = BrandTheme.Heading,
                Margin = new Padding(0, 0, 0, 6)
            };
            root.Controls.Add(lblTitle, 0, 0);

            var lblInfo = new Label
            {
                Text = "4 haneli sifrenizi girin. Varsayilan sifre: 0000",
                AutoSize = true,
                Font = BrandTheme.CreateFont(9.4f),
                ForeColor = BrandTheme.MutedText,
                Margin = new Padding(0, 0, 0, 12)
            };
            root.Controls.Add(lblInfo, 0, 1);

            _txtPin = new TextBox
            {
                Width = 180,
                MaxLength = 4,
                UseSystemPasswordChar = true,
                Margin = new Padding(0, 0, 0, 8)
            };
            _txtPin.KeyPress += (_, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                    e.Handled = true;
            };
            root.Controls.Add(_txtPin, 0, 2);

            _lblError = new Label
            {
                Text = string.Empty,
                AutoSize = true,
                ForeColor = Color.FromArgb(173, 59, 56),
                Font = BrandTheme.CreateFont(9f),
                Margin = new Padding(0, 0, 0, 10)
            };
            root.Controls.Add(_lblError, 0, 3);
            _txtPin.TextChanged += (_, __) => _lblError.Text = string.Empty;

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                Margin = new Padding(0)
            };
            root.Controls.Add(buttons, 0, 4);

            _btnLogin = CreateButton("Giris", BrandTheme.Navy);
            _btnCancel = CreateButton("Cikis", Color.FromArgb(102, 114, 128));
            buttons.Controls.Add(_btnLogin);
            buttons.Controls.Add(_btnCancel);

            _btnLogin.Click += async (_, __) => await AuthenticateAsync();
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
                _lblError.Text = "Sifre altyapisi okunamadi.";
            }

            _txtPin.Focus();
        }

        private async Task AuthenticateAsync()
        {
            if (_isProcessing)
                return;

            var enteredPin = (_txtPin.Text ?? string.Empty).Trim();
            if (!IsValidPin(enteredPin))
            {
                _lblError.Text = "Sifre 4 haneli sayisal olmalidir.";
                return;
            }

            _isProcessing = true;
            _btnLogin.Enabled = false;

            try
            {
                var currentPin = await _appSecurityService.GetPinAsync();
                if (!string.Equals(enteredPin, currentPin, StringComparison.Ordinal))
                {
                    _lblError.Text = "Sifre hatali.";
                    _txtPin.SelectAll();
                    return;
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                _lblError.Text = "Sifre dogrulanamadi: " + ex.Message;
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
            var button = new Button
            {
                Text = text,
                Width = 108,
                Height = 34,
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = BrandTheme.CreateHeadingFont(9.2f, FontStyle.Bold),
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
