using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using CashTracker.App.Controls;
using CashTracker.App.UI;
using CashTracker.Core.Services;

namespace CashTracker.App.Forms
{
    internal sealed class PinSetupForm : Form
    {
        private readonly IAppSecurityService _appSecurityService;
        private readonly PinCodeInputControl _txtPin;
        private readonly PinCodeInputControl _txtConfirm;
        private readonly Label _lblStatus;
        private readonly Button _btnSave;

        public PinSetupForm(IAppSecurityService appSecurityService, bool isFirstRun)
        {
            _appSecurityService = appSecurityService;

            Text = isFirstRun ? "CashTracker Guvenlik Ayari" : "PIN Degistir";
            Width = 560;
            Height = 520;
            MinimumSize = new Size(560, 520);
            UiMetrics.ApplyFormDefaults(this);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
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
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 404));
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

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 8,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (var i = 0; i < 8; i++)
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            card.Controls.Add(layout);

            var headingFont = BrandTheme.CreateHeadingFont(16f, FontStyle.Bold);
            layout.Controls.Add(new Label
            {
                Text = isFirstRun ? "PIN olusturun" : "Yeni PIN girin",
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = UiMetrics.GetTextLineHeight(headingFont) + 4,
                Font = headingFont,
                ForeColor = BrandTheme.Heading,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 0, 0, 8)
            }, 0, 0);

            layout.Controls.Add(new Label
            {
                Text = "4 haneli sayisal bir PIN belirleyin.",
                AutoSize = true,
                Font = BrandTheme.CreateFont(9.4f),
                ForeColor = BrandTheme.MutedText,
                MaximumSize = new Size(320, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.None,
                Margin = new Padding(10, 0, 10, 18)
            }, 0, 1);

            var lblPin = CreateFieldLabel("Yeni PIN");
            lblPin.Margin = new Padding(32, 0, 32, 6);
            layout.Controls.Add(lblPin, 0, 2);
            _txtPin = CreatePinBox();
            layout.Controls.Add(CreateCenteredRow(_txtPin), 0, 3);

            var lblConfirm = CreateFieldLabel("Yeni PIN (Tekrar)");
            lblConfirm.Margin = new Padding(32, 6, 32, 6);
            layout.Controls.Add(lblConfirm, 0, 4);
            _txtConfirm = CreatePinBox();
            layout.Controls.Add(CreateCenteredRow(_txtConfirm), 0, 5);

            _lblStatus = new Label
            {
                AutoSize = true,
                ForeColor = BrandTheme.MutedText,
                Font = BrandTheme.CreateFont(9.2f),
                MaximumSize = new Size(320, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.None,
                Margin = new Padding(8, 14, 8, 14)
            };
            layout.Controls.Add(_lblStatus, 0, 6);

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Anchor = AnchorStyles.None,
                Margin = new Padding(0, 4, 0, 0)
            };
            var btnCancel = CreateButton("Vazgec", Color.FromArgb(100, 112, 126));
            _btnSave = CreateButton("Kaydet", BrandTheme.Navy);
            btnCancel.Click += (_, __) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };
            _btnSave.Click += async (_, __) => await SaveAsync();
            actions.Controls.Add(_btnSave);
            actions.Controls.Add(btnCancel);
            layout.Controls.Add(actions, 0, 7);

            AcceptButton = _btnSave;
            CancelButton = btnCancel;
        }

        private async Task SaveAsync()
        {
            var pin = (_txtPin.Text ?? string.Empty).Trim();
            var confirm = (_txtConfirm.Text ?? string.Empty).Trim();

            if (pin.Length != 4 || !int.TryParse(pin, out _))
            {
                SetStatus("PIN 4 haneli sayisal olmalidir.", true);
                return;
            }

            if (!string.Equals(pin, confirm))
            {
                SetStatus("PIN alanlari ayni olmalidir.", true);
                return;
            }

            _btnSave.Enabled = false;
            try
            {
                await _appSecurityService.SetPinAsync(pin);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (System.Exception ex)
            {
                SetStatus(ex.Message, true);
            }
            finally
            {
                _btnSave.Enabled = true;
            }
        }

        private void SetStatus(string text, bool isError)
        {
            _lblStatus.Text = text;
            _lblStatus.ForeColor = isError
                ? Color.FromArgb(173, 59, 56)
                : Color.FromArgb(17, 121, 85);
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

        private static PinCodeInputControl CreatePinBox()
        {
            return new PinCodeInputControl
            {
                Dock = DockStyle.Fill,
                Font = BrandTheme.CreateHeadingFont(16f, FontStyle.Bold),
                Height = 52,
                MinimumSize = new Size(260, 52),
                Width = 260,
                PinLength = 4,
                UsePasswordMask = true
            };
        }

        private static TableLayoutPanel CreateCenteredRow(Control control)
        {
            var row = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 3,
                RowCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0)
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, control.Width));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            row.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            row.Controls.Add(control, 1, 0);
            return row;
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
