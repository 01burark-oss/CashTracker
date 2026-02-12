using System;
using System.Drawing;
using System.Windows.Forms;
using CashTracker.App.UI;

namespace CashTracker.App.Forms
{
    public sealed partial class KasaForm
    {
        private static Panel CreateSectionHeader(string title, string subtitle)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 62
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
                Font = BrandTheme.CreateHeadingFont(13.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(42, 50, 61),
                AutoSize = true,
                Margin = new Padding(0)
            };
            layout.Controls.Add(titleLabel, 0, 0);

            var subtitleLabel = new Label
            {
                Text = subtitle,
                Font = BrandTheme.CreateFont(9.4f, FontStyle.Regular),
                ForeColor = Color.FromArgb(106, 118, 136),
                AutoSize = true,
                Margin = new Padding(0, 2, 0, 0)
            };
            layout.Controls.Add(subtitleLabel, 0, 1);

            return panel;
        }

        private static Button CreateButton(string text, Color back, Color fore)
        {
            var button = new Button
            {
                Text = text,
                Width = 108,
                Height = 36,
                BackColor = back,
                ForeColor = fore,
                Font = BrandTheme.CreateHeadingFont(9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(8, 0, 0, 0),
                FlatStyle = FlatStyle.Flat
            };

            button.FlatAppearance.BorderColor = Color.FromArgb(21, 38, 61);
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                Math.Max(back.R - 12, 0),
                Math.Max(back.G - 12, 0),
                Math.Max(back.B - 12, 0));

            return button;
        }
    }
}
