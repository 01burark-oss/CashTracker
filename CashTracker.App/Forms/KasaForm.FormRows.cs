using System.Drawing;
using System.Windows.Forms;
using CashTracker.App.UI;

namespace CashTracker.App.Forms
{
    public sealed partial class KasaForm
    {
        private static void AddRow(TableLayoutPanel panel, string label, out TextBox textBox)
        {
            var lbl = new Label
            {
                Text = label,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Font = BrandTheme.CreateHeadingFont(9.4f),
                Margin = new Padding(0, 8, 10, 8)
            };
            textBox = new TextBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 8, 0, 8)
            };
            panel.Controls.Add(lbl);
            panel.Controls.Add(textBox);
        }

        private static void AddRow(TableLayoutPanel panel, string label, out ComboBox comboBox)
        {
            var lbl = new Label
            {
                Text = label,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Font = BrandTheme.CreateHeadingFont(9.4f),
                Margin = new Padding(0, 8, 10, 8)
            };

            comboBox = new ComboBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(0, 8, 0, 8),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White
            };

            panel.Controls.Add(lbl);
            panel.Controls.Add(comboBox);
        }

        private void AddKalemEmptyActionRow(TableLayoutPanel panel)
        {
            var left = new Label
            {
                Text = string.Empty,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8)
            };

            var right = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0, 0, 0, 10)
            };
            right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _lblKalemEmptyHint = new Label
            {
                Text = string.Empty,
                AutoSize = true,
                ForeColor = Color.FromArgb(173, 59, 56),
                Font = BrandTheme.CreateFont(9f),
                Margin = new Padding(0, 0, 0, 6),
                Visible = false
            };

            _btnKalemSettings = CreateButton("Kalemleri Yonet", BrandTheme.Teal, Color.White);
            _btnKalemSettings.Width = 146;
            _btnKalemSettings.Height = 34;
            _btnKalemSettings.Margin = new Padding(0);
            _btnKalemSettings.Anchor = AnchorStyles.Left;
            _btnKalemSettings.Visible = false;

            right.Controls.Add(_lblKalemEmptyHint, 0, 0);
            right.Controls.Add(_btnKalemSettings, 0, 1);

            panel.Controls.Add(left);
            panel.Controls.Add(right);
        }

        private static void AddRow(TableLayoutPanel panel, string label, out DateTimePicker dtp)
        {
            var lbl = new Label
            {
                Text = label,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Font = BrandTheme.CreateHeadingFont(9.4f),
                Margin = new Padding(0, 8, 10, 8)
            };
            dtp = new DateTimePicker
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(0, 8, 0, 8),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd.MM.yyyy HH:mm"
            };
            panel.Controls.Add(lbl);
            panel.Controls.Add(dtp);
        }
    }
}
