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
                Margin = new Padding(0, 8, 0, 8)
            };
            panel.Controls.Add(lbl);
            panel.Controls.Add(dtp);
        }
    }
}
