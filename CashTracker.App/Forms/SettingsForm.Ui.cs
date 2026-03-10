using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using CashTracker.App.UI;
using CashTracker.Core.Services;

namespace CashTracker.App.Forms
{
    internal sealed partial class SettingsForm
    {
        private enum HintTone
        {
            Neutral,
            Success,
            Warning,
            Error
        }

        private static Panel CreateSurfacePanel()
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                Padding = UiMetrics.SurfacePadding,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        private static Panel CreateSectionHeader(string title, string subtitle)
        {
            var titleFont = BrandTheme.CreateHeadingFont(13f, FontStyle.Bold);
            var subtitleFont = BrandTheme.CreateFont(9.3f);
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(0, UiMetrics.GetHeaderHeight(titleFont, subtitleFont, 20, 3))
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.Controls.Add(layout);

            var titleLabel = new Label
            {
                Text = title,
                Font = titleFont,
                ForeColor = BrandTheme.Heading,
                AutoSize = true,
                Margin = new Padding(0)
            };
            layout.Controls.Add(titleLabel, 0, 0);

            var subtitleLabel = new Label
            {
                Text = subtitle,
                Font = subtitleFont,
                ForeColor = BrandTheme.MutedText,
                AutoSize = true,
                Margin = new Padding(0, 3, 0, 0)
            };
            layout.Controls.Add(subtitleLabel, 0, 1);

            return panel;
        }

        private static Label CreateFieldLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                ForeColor = Color.FromArgb(61, 71, 85),
                Font = BrandTheme.CreateHeadingFont(9.4f, FontStyle.Bold),
                Margin = new Padding(2, 4, 2, 4)
            };
        }

        private static TextBox CreateInputBox()
        {
            var font = BrandTheme.CreateFont(10f);
            return new TextBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                AutoSize = false,
                BorderStyle = BorderStyle.FixedSingle,
                Font = font,
                Height = UiMetrics.GetInputHeight(font),
                Margin = new Padding(0, 3, 0, 3),
                MinimumSize = new Size(0, UiMetrics.GetInputHeight(font))
            };
        }

        private static ComboBox CreateFlatComboBox()
        {
            var font = BrandTheme.CreateFont(10f);
            var height = UiMetrics.GetInputHeight(font);
            return new ComboBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                IntegralHeight = false,
                Font = font,
                Height = height,
                Margin = new Padding(0, 3, 0, 3),
                MinimumSize = new Size(0, height)
            };
        }

        private static TableLayoutPanel CreateTwoColumnRow(float leftPercent, int rightWidth)
        {
            var row = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 0, 10)
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, leftPercent));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, rightWidth));
            row.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            return row;
        }

        private static Button CreateActionButton(string text, Color back, Color fore)
        {
            var font = BrandTheme.CreateHeadingFont(9.2f, FontStyle.Bold);
            var button = new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                BackColor = back,
                ForeColor = fore,
                Font = font,
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(8, 0, 0, 0),
                MinimumSize = new Size(0, UiMetrics.GetButtonHeight(font)),
                Padding = UiMetrics.ButtonPadding
            };

            button.FlatAppearance.BorderColor = Color.FromArgb(20, 40, 64);
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                Math.Max(back.R - 12, 0),
                Math.Max(back.G - 12, 0),
                Math.Max(back.B - 12, 0));

            return button;
        }

        private static string NormalizeText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private void SetBusinessHint(string text, HintTone tone = HintTone.Neutral)
        {
            if (_lblBusinessHint is null)
                return;

            _lblBusinessHint.Text = text;
            _lblBusinessHint.ForeColor = ResolveHintColor(tone);
        }

        private void SetCategoryHint(string text, HintTone tone = HintTone.Neutral)
        {
            if (_lblCategoryHint is null)
                return;

            _lblCategoryHint.Text = text;
            _lblCategoryHint.ForeColor = ResolveHintColor(tone);
        }

        private async Task<bool> RequireTelegramApprovalAsync(
            string title,
            string details,
            System.Action<string, HintTone> setHint)
        {
            if (_telegramApprovalService is null)
                return false;

            setHint(AppLocalization.T("settings.approval.wait"), HintTone.Neutral);

            var result = await _telegramApprovalService.RequestApprovalAsync(
                new TelegramApprovalRequest(title, details, TimeSpan.FromMinutes(2)));

            switch (result.Status)
            {
                case TelegramApprovalStatus.Approved:
                    return true;
                case TelegramApprovalStatus.Rejected:
                    setHint(AppLocalization.T("settings.approval.rejected"), HintTone.Warning);
                    return false;
                case TelegramApprovalStatus.TimedOut:
                    setHint(AppLocalization.T("settings.approval.timeout"), HintTone.Warning);
                    return false;
                case TelegramApprovalStatus.NotConfigured:
                    var localConfirm = MessageBox.Show(
                        "Telegram onayi aktif degil. Yerel onay ile devam edilsin mi?",
                        AppLocalization.T("settings.title"),
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);
                    if (localConfirm == DialogResult.Yes)
                        return true;

                    setHint(AppLocalization.T("settings.approval.notConfiguredHint"), HintTone.Warning);
                    return false;
                case TelegramApprovalStatus.Failed:
                    MessageBox.Show(
                        AppLocalization.F("settings.approval.failedBody", result.Message ?? AppLocalization.T("settings.approval.failedDefault")),
                        AppLocalization.T("settings.title"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    setHint(AppLocalization.T("settings.approval.failedHint"), HintTone.Error);
                    return false;
                default:
                    setHint(AppLocalization.T("settings.approval.failedHint"), HintTone.Error);
                    return false;
            }
        }

        private static Color ResolveHintColor(HintTone tone)
        {
            return tone switch
            {
                HintTone.Success => Color.FromArgb(17, 121, 85),
                HintTone.Warning => Color.FromArgb(173, 111, 24),
                HintTone.Error => Color.FromArgb(173, 59, 56),
                _ => BrandTheme.MutedText
            };
        }
    }
}
