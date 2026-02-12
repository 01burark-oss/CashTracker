using System.Drawing;
using System.Windows.Forms;
using CashTracker.App.UI;

namespace CashTracker.App.Forms
{
    public sealed partial class KasaForm
    {
        private static TableLayoutPanel CreateEditorForm()
        {
            var form = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                AutoSize = true
            };

            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
            return form;
        }

        private void AddTypeRow(TableLayoutPanel form)
        {
            var label = new Label { Text = "Tip", AutoSize = true, Anchor = AnchorStyles.Left };
            _cmbTip = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White,
                Margin = new Padding(0, 8, 0, 8),
                FlatStyle = FlatStyle.Flat
            };
            label.Font = BrandTheme.CreateHeadingFont(9.4f, FontStyle.Bold);
            label.Margin = new Padding(0, 8, 10, 8);

            _cmbTip.Items.AddRange(new object[] { "Gelir", "Gider" });
            _cmbTip.SelectedIndex = 0;
            _cmbTip.SelectedIndexChanged += (_, __) => ToggleGiderTuru();
            form.Controls.Add(label);
            form.Controls.Add(_cmbTip);
        }

        private void AddAmountRow(TableLayoutPanel form)
        {
            var label = new Label { Text = "Tutar", AutoSize = true, Anchor = AnchorStyles.Left };
            _numTutar = new NumericUpDown
            {
                DecimalPlaces = 2,
                Maximum = 100000000,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(0, 8, 0, 8),
                ThousandsSeparator = true
            };
            label.Font = BrandTheme.CreateHeadingFont(9.4f, FontStyle.Bold);
            label.Margin = new Padding(0, 8, 10, 8);

            form.Controls.Add(label);
            form.Controls.Add(_numTutar);
        }
    }
}
