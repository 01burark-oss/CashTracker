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
                Height = 60
            };

            var titleLabel = new Label
            {
                Text = title,
                Font = BrandTheme.CreateHeadingFont(13.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(42, 50, 61),
                AutoSize = true,
                Location = new Point(0, 0)
            };
            panel.Controls.Add(titleLabel);

            var subtitleLabel = new Label
            {
                Text = subtitle,
                Font = BrandTheme.CreateFont(9.4f, FontStyle.Regular),
                ForeColor = Color.FromArgb(106, 118, 136),
                AutoSize = true,
                Location = new Point(1, 25)
            };
            panel.Controls.Add(subtitleLabel);

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

            button.FlatAppearance.BorderColor = Color.FromArgb(218, 224, 232);
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = back == Color.White
                ? Color.FromArgb(245, 247, 251)
                : back;

            return button;
        }
    }
}
