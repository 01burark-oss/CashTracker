using System.Drawing;
using System.Windows.Forms;
using CashTracker.App.UI;

namespace CashTracker.App.Forms
{
    internal sealed class OptionalIntegrationsForm : Form
    {
        public bool ShouldConfigureTelegram { get; private set; }

        public OptionalIntegrationsForm()
        {
            Text = "Opsiyonel Entegrasyonlar";
            Width = 600;
            Height = 340;
            UiMetrics.ApplyFormDefaults(this);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = BrandTheme.AppBackground;
            Font = BrandTheme.CreateFont(10f);
            if (AppIconProvider.Current is Icon appIcon)
                Icon = appIcon;

            var shell = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24),
                BackColor = BrandTheme.AppBackground
            };
            Controls.Add(shell);

            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(24)
            };
            shell.Controls.Add(card);

            var title = new Label
            {
                Text = "Isterseniz Telegram entegrasyonunu simdi kurabilirsiniz.",
                Dock = DockStyle.Top,
                AutoSize = true,
                Font = BrandTheme.CreateHeadingFont(15f, FontStyle.Bold),
                ForeColor = BrandTheme.Heading,
                Margin = new Padding(0, 0, 0, 10)
            };
            card.Controls.Add(title);

            var body = new Label
            {
                Text = "Telegram zorunlu degil. Atlarsaniz uygulama normal sekilde calismaya devam eder; daha sonra Ayarlar veya sol menu uzerinden baglayabilirsiniz.",
                Dock = DockStyle.Top,
                AutoSize = true,
                Font = BrandTheme.CreateFont(9.5f),
                ForeColor = BrandTheme.MutedText,
                Margin = new Padding(0, 40, 0, 24)
            };
            card.Controls.Add(body);

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            var btnSkip = CreateButton("Simdilik Atla", Color.FromArgb(100, 112, 126));
            var btnConfigure = CreateButton("Telegram Kur", BrandTheme.Teal);
            btnSkip.Click += (_, __) =>
            {
                ShouldConfigureTelegram = false;
                DialogResult = DialogResult.OK;
                Close();
            };
            btnConfigure.Click += (_, __) =>
            {
                ShouldConfigureTelegram = true;
                DialogResult = DialogResult.OK;
                Close();
            };
            actions.Controls.Add(btnConfigure);
            actions.Controls.Add(btnSkip);
            card.Controls.Add(actions);
        }

        private static Button CreateButton(string text, Color backColor)
        {
            var font = BrandTheme.CreateHeadingFont(9.2f, FontStyle.Bold);
            var button = new Button
            {
                Text = text,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = UiMetrics.GetButtonMinimumSize(font, 132),
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
