using System.Drawing;
using System.Windows.Forms;
using CashTracker.App.UI;

namespace CashTracker.App.Forms
{
    internal sealed class PrintRecordScopeDialog : Form
    {
        public int? SelectedLimit { get; private set; }

        public PrintRecordScopeDialog()
        {
            Text = AppLocalization.T("print.scope.title");
            Width = 460;
            Height = 220;
            MinimumSize = new Size(420, 210);
            UiMetrics.ApplyFullscreenDialogDefaults(this);
            ShowInTaskbar = false;
            BackColor = Color.White;
            Font = BrandTheme.CreateFont(10f);
            if (AppIconProvider.Current is Icon appIcon)
                Icon = appIcon;

            BuildUi();
        }

        private void BuildUi()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(18, 16, 18, 16),
                BackColor = Color.White
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(root);

            var title = new Label
            {
                Text = AppLocalization.T("print.scope.header"),
                AutoSize = true,
                Font = BrandTheme.CreateHeadingFont(13f, FontStyle.Bold),
                ForeColor = BrandTheme.Heading,
                Margin = new Padding(0, 0, 0, 6)
            };
            root.Controls.Add(title, 0, 0);

            var body = new Label
            {
                Text = AppLocalization.T("print.scope.body"),
                AutoSize = true,
                Font = BrandTheme.CreateFont(9.8f),
                ForeColor = BrandTheme.MutedText,
                Margin = new Padding(0, 0, 0, 14)
            };
            root.Controls.Add(body, 0, 1);

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0)
            };
            root.Controls.Add(actions, 0, 2);

            var btnAll = CreateActionButton(AppLocalization.T("print.scope.all"));
            var btn100 = CreateActionButton(AppLocalization.T("print.scope.first100"));
            var btn50 = CreateActionButton(AppLocalization.T("print.scope.first50"));
            var btnCancel = CreateActionButton(AppLocalization.T("print.scope.cancel"), filled: false);

            btnAll.Click += (_, __) =>
            {
                SelectedLimit = null;
                DialogResult = DialogResult.OK;
                Close();
            };
            btn100.Click += (_, __) =>
            {
                SelectedLimit = 100;
                DialogResult = DialogResult.OK;
                Close();
            };
            btn50.Click += (_, __) =>
            {
                SelectedLimit = 50;
                DialogResult = DialogResult.OK;
                Close();
            };
            btnCancel.Click += (_, __) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            actions.Controls.Add(btnAll);
            actions.Controls.Add(btn100);
            actions.Controls.Add(btn50);
            actions.Controls.Add(btnCancel);
        }

        private static Button CreateActionButton(string text, bool filled = true)
        {
            var font = BrandTheme.CreateFont(9.5f, FontStyle.Bold);
            var button = new Button
            {
                Text = text,
                Width = 104,
                MinimumSize = new Size(104, UiMetrics.GetButtonHeight(font)),
                Margin = new Padding(10, 0, 0, 0),
                FlatStyle = FlatStyle.Flat,
                Font = font,
                BackColor = filled ? Color.Black : Color.White,
                ForeColor = filled ? Color.White : Color.Black,
                Padding = UiMetrics.ButtonPadding
            };

            button.FlatAppearance.BorderColor = Color.Black;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = filled ? Color.FromArgb(35, 35, 35) : Color.FromArgb(244, 244, 244);
            return button;
        }
    }
}
