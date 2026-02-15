using System;
using System.Drawing;
using System.Windows.Forms;
using CashTracker.App.UI;

namespace CashTracker.App.Forms
{
    public sealed partial class SettingsForm
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
                Padding = new Padding(16, 14, 16, 16),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        private static Panel CreateSectionHeader(string title, string subtitle)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 66
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
                Font = BrandTheme.CreateHeadingFont(13f, FontStyle.Bold),
                ForeColor = BrandTheme.Heading,
                AutoSize = true,
                Margin = new Padding(0)
            };
            layout.Controls.Add(titleLabel, 0, 0);

            var subtitleLabel = new Label
            {
                Text = subtitle,
                Font = BrandTheme.CreateFont(9.3f),
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
            return new TextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0)
            };
        }

        private static ComboBox CreateFlatComboBox()
        {
            return new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0)
            };
        }

        private static TableLayoutPanel CreateTwoColumnRow(float leftPercent, int rightWidth)
        {
            var row = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 1,
                Height = 36,
                Margin = new Padding(0, 0, 0, 10)
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, leftPercent));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, rightWidth));
            row.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            return row;
        }

        private static Button CreateActionButton(string text, Color back, Color fore)
        {
            var button = new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                Height = 34,
                BackColor = back,
                ForeColor = fore,
                Font = BrandTheme.CreateHeadingFont(9.2f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(8, 0, 0, 0)
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
