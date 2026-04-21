using System.Drawing;
using System.Windows.Forms;
using CashTracker.App.UI;

namespace CashTracker.App.Forms
{
    internal sealed class SmsCodePromptForm : Form
    {
        private readonly TextBox _txtCode = new();

        public SmsCodePromptForm(string message)
        {
            Text = "GIB SMS Onayi";
            Width = 430;
            Height = 230;
            UiMetrics.ApplyFullscreenDialogDefaults(this);
            Font = new Font("Segoe UI", 10f);
            SmsCode = string.Empty;
            BuildUi(message);
        }

        public string SmsCode { get; private set; }

        private void BuildUi(string message)
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(18)
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(root);

            root.Controls.Add(new Label
            {
                Text = message,
                AutoSize = true,
                MaximumSize = new Size(360, 0),
                Margin = new Padding(0, 0, 0, 12)
            }, 0, 0);

            _txtCode.Dock = DockStyle.Top;
            _txtCode.Margin = new Padding(0, 0, 0, 16);
            root.Controls.Add(_txtCode, 0, 1);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft
            };
            root.Controls.Add(buttons, 0, 2);

            var btnOk = new Button { Text = "Onayla", Width = 100, Height = 34 };
            var btnCancel = new Button { Text = "Vazgec", Width = 100, Height = 34 };
            buttons.Controls.AddRange([btnOk, btnCancel]);

            btnOk.Click += (_, __) =>
            {
                SmsCode = _txtCode.Text.Trim();
                DialogResult = DialogResult.OK;
                Close();
            };
            btnCancel.Click += (_, __) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };
        }
    }
}
