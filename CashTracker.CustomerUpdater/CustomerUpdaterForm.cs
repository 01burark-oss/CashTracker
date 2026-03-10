using System.Drawing;

namespace CashTracker.CustomerUpdater;

internal sealed class CustomerUpdaterForm : Form
{
    private readonly CustomerUpdaterOptions _options;
    private readonly Label _statusLabel;
    private readonly ProgressBar _progressBar;
    private readonly Button _primaryButton;
    private CustomerUpdaterResult? _result;
    private bool _started;

    public CustomerUpdaterForm(CustomerUpdaterOptions options)
    {
        _options = options;

        Text = "CashTracker Fabesco Guncelleyici";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(560, 280);
        BackColor = Color.FromArgb(248, 247, 242);

        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(28, 24, 28, 20)
        };

        var titleLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 36,
            Font = new Font("Segoe UI Semibold", 15.5f, FontStyle.Bold),
            Text = "CashTracker Fabesco"
        };

        var subtitleLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 44,
            Font = new Font("Segoe UI", 9.75f, FontStyle.Regular),
            ForeColor = Color.FromArgb(92, 92, 92),
            Text = "Bu arac son surumu GitHub'dan indirir, sessizce kurar ve masaustune Cashtracker Fabesco kisayolu olusturur."
        };

        _statusLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 54,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular),
            ForeColor = Color.FromArgb(36, 36, 36),
            Text = "Hazirlaniyor..."
        };

        _progressBar = new ProgressBar
        {
            Dock = DockStyle.Top,
            Height = 18,
            Style = ProgressBarStyle.Continuous,
            Maximum = 100
        };

        var spacer = new Panel
        {
            Dock = DockStyle.Fill
        };

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        _primaryButton = new Button
        {
            Text = "Acmaya Hazir",
            AutoSize = false,
            Width = 180,
            Height = 36,
            Enabled = false,
            BackColor = Color.FromArgb(28, 94, 68),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _primaryButton.FlatAppearance.BorderSize = 0;
        _primaryButton.Click += PrimaryButton_Click;

        var closeButton = new Button
        {
            Text = "Kapat",
            AutoSize = false,
            Width = 120,
            Height = 36,
            BackColor = Color.FromArgb(235, 233, 226),
            ForeColor = Color.FromArgb(48, 48, 48),
            FlatStyle = FlatStyle.Flat
        };
        closeButton.FlatAppearance.BorderSize = 0;
        closeButton.Click += (_, _) => Close();

        footer.Controls.Add(_primaryButton);
        footer.Controls.Add(closeButton);

        card.Controls.Add(spacer);
        card.Controls.Add(footer);
        card.Controls.Add(_statusLabel);
        card.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 16 });
        card.Controls.Add(_progressBar);
        card.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 18 });
        card.Controls.Add(subtitleLabel);
        card.Controls.Add(titleLabel);

        Controls.Add(card);
        Shown += CustomerUpdaterForm_Shown;
    }

    private async void CustomerUpdaterForm_Shown(object? sender, EventArgs e)
    {
        if (_started)
            return;

        _started = true;
        var progress = new Progress<CustomerUpdaterStatus>(status =>
        {
            _progressBar.Value = Math.Max(0, Math.Min(100, status.Percent));
            _statusLabel.Text = status.Message;
        });

        using var httpClient = new HttpClient();
        var service = new CustomerUpdateService(httpClient);
        _result = await service.RunAsync(_options, progress, CancellationToken.None);

        if (_result.Success)
        {
            _progressBar.Value = 100;
            _statusLabel.Text = _result.Message;
            _primaryButton.Enabled = true;
            _primaryButton.Text = _options.CheckOnly ? "Tamam" : "Cashtracker Fabesco'yu Ac";
            return;
        }

        _statusLabel.Text = _result.Message;
        _primaryButton.Text = "Tekrar Dene";
        _primaryButton.Enabled = true;
    }

    private void PrimaryButton_Click(object? sender, EventArgs e)
    {
        if (_result is null)
            return;

        if (!_result.Success)
        {
            _started = false;
            _progressBar.Value = 0;
            _statusLabel.Text = "Tekrar deneniyor...";
            _primaryButton.Enabled = false;
            CustomerUpdaterForm_Shown(this, EventArgs.Empty);
            return;
        }

        if (_options.CheckOnly)
        {
            Close();
            return;
        }

        if (!string.IsNullOrWhiteSpace(_result.InstalledExePath) && File.Exists(_result.InstalledExePath))
        {
            var workingDirectory = Path.GetDirectoryName(_result.InstalledExePath) ?? AppContext.BaseDirectory;
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_result.InstalledExePath)
            {
                UseShellExecute = true,
                WorkingDirectory = workingDirectory
            });
        }

        Close();
    }
}
