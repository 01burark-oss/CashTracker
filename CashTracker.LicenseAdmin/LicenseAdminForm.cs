using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CashTracker.Core.Utilities;

namespace CashTracker.LicenseAdmin;

internal sealed class LicenseAdminForm : Form
{
    private readonly LicenseLedgerService _ledger;
    private readonly LicenseKeyIssuer _issuer;

    private TextBox _txtKeyPath = null!;
    private TextBox _txtCustomerName = null!;
    private TextBox _txtInstallCode = null!;
    private TextBox _txtReceiptOcrApiKey = null!;
    private TextBox _txtReceiptOcrProvider = null!;
    private TextBox _txtReceiptOcrModel = null!;
    private TextBox _txtEdition = null!;
    private TextBox _txtLicenseId = null!;
    private TextBox _txtNotes = null!;
    private TextBox _txtResult = null!;
    private Label _lblStatus = null!;
    private DataGridView _grid = null!;

    public LicenseAdminForm()
    {
        _ledger = new LicenseLedgerService(LicenseAdminRuntime.GetAppDataPath());
        _issuer = new LicenseKeyIssuer();

        Text = "CashTracker License Admin";
        Width = 1360;
        Height = 860;
        MinimumSize = new Size(1240, 760);
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        BackColor = AdminTheme.AppBackground;
        Font = AdminTheme.CreateFont(10f);

        BuildUi();
        Load += (_, __) =>
        {
            LoadLedger();
            InitializeDefaults();
        };
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 16, 18, 18),
            ColumnCount = 2,
            RowCount = 1,
            BackColor = AdminTheme.AppBackground
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 430));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var left = CreateSurfacePanel();
        left.Margin = new Padding(0, 0, 12, 0);
        var right = CreateSurfacePanel();
        root.Controls.Add(left, 0, 0);
        root.Controls.Add(right, 1, 0);

        BuildLeftPanel(left);
        BuildRightPanel(right);
    }

    private void BuildLeftPanel(Control host)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 19,
            Padding = new Padding(22)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 18; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        host.Controls.Add(layout);

        layout.Controls.Add(new Label
        {
            Text = "Tek Kullanimlik Lisans Uretimi",
            Font = AdminTheme.CreateHeadingFont(16f, FontStyle.Bold),
            ForeColor = AdminTheme.Heading,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 6)
        }, 0, 0);

        layout.Controls.Add(new Label
        {
            Text = "Kurulum koduna ozel imzali lisans uretin. Ayni kurulum kodu icin ikinci lisans uretilmez; mevcut kayit gosterilir.",
            Font = AdminTheme.CreateFont(9.2f),
            ForeColor = AdminTheme.MutedText,
            MaximumSize = new Size(350, 0),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 18)
        }, 0, 1);

        layout.Controls.Add(CreateFieldLabel("Private Key Dosyasi"), 0, 2);
        var keyRow = CreateActionRow();
        _txtKeyPath = CreateInputBox();
        var btnBrowse = CreateActionButton("Sec", AdminTheme.Navy);
        btnBrowse.Width = 94;
        btnBrowse.Click += (_, __) => BrowseKeyFile();
        keyRow.Controls.Add(_txtKeyPath, 0, 0);
        keyRow.Controls.Add(btnBrowse, 1, 0);
        layout.Controls.Add(keyRow, 0, 3);

        layout.Controls.Add(CreateFieldLabel("Musteri Adi"), 0, 4);
        _txtCustomerName = CreateInputBox();
        layout.Controls.Add(_txtCustomerName, 0, 5);

        layout.Controls.Add(CreateFieldLabel("Kurulum Kodu"), 0, 6);
        _txtInstallCode = CreateInputBox();
        layout.Controls.Add(_txtInstallCode, 0, 7);

        layout.Controls.Add(CreateFieldLabel("OCR API Key (Opsiyonel)"), 0, 8);
        _txtReceiptOcrApiKey = CreateInputBox();
        _txtReceiptOcrApiKey.UseSystemPasswordChar = true;
        layout.Controls.Add(_txtReceiptOcrApiKey, 0, 9);

        layout.Controls.Add(new Label
        {
            Text = "Bos birakilirsa lisans OCR secret tasimaz. Girilirse install code ile sifrelenir ve sadece bu kurulumda cozulebilir.",
            Font = AdminTheme.CreateFont(9f),
            ForeColor = AdminTheme.MutedText,
            MaximumSize = new Size(350, 0),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 10)
        }, 0, 10);

        var ocrMetaRow = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 0, 0, 0)
        };
        ocrMetaRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        ocrMetaRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        ocrMetaRow.Controls.Add(CreateFieldLabel("OCR Provider"), 0, 0);
        ocrMetaRow.Controls.Add(CreateFieldLabel("OCR Model"), 1, 0);
        _txtReceiptOcrProvider = CreateInputBox();
        _txtReceiptOcrProvider.Text = "Gemini";
        _txtReceiptOcrModel = CreateInputBox();
        _txtReceiptOcrModel.Text = "gemini-2.5-flash";
        ocrMetaRow.Controls.Add(_txtReceiptOcrProvider, 0, 1);
        ocrMetaRow.Controls.Add(_txtReceiptOcrModel, 1, 1);
        layout.Controls.Add(ocrMetaRow, 0, 11);

        var metaRow = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 8, 0, 0)
        };
        metaRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        metaRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.Controls.Add(metaRow, 0, 12);

        metaRow.Controls.Add(CreateFieldLabel("Edition"), 0, 0);
        metaRow.Controls.Add(CreateFieldLabel("License ID"), 1, 0);
        _txtEdition = CreateInputBox();
        _txtEdition.Text = "pro";
        _txtLicenseId = CreateInputBox();
        _txtLicenseId.Text = LicenseAdminRuntime.CreateLicenseId();
        metaRow.Controls.Add(_txtEdition, 0, 1);
        metaRow.Controls.Add(_txtLicenseId, 1, 1);

        layout.Controls.Add(CreateFieldLabel("Notlar"), 0, 13);
        _txtNotes = new TextBox
        {
            Dock = DockStyle.Top,
            Multiline = true,
            Height = 82,
            BorderStyle = BorderStyle.FixedSingle,
            Font = AdminTheme.CreateFont(9.3f),
            Margin = new Padding(0, 2, 0, 10)
        };
        layout.Controls.Add(_txtNotes, 0, 14);

        var actionBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            Margin = new Padding(0, 2, 0, 10)
        };
        var btnGenerate = CreateActionButton("Lisans Uret", AdminTheme.Teal);
        var btnCopy = CreateActionButton("Anahtari Kopyala", AdminTheme.Navy);
        btnGenerate.Click += (_, __) => GenerateLicense();
        btnCopy.Click += (_, __) =>
        {
            if (!string.IsNullOrWhiteSpace(_txtResult.Text))
                Clipboard.SetText(_txtResult.Text);
        };
        actionBar.Controls.Add(btnGenerate);
        actionBar.Controls.Add(btnCopy);
        layout.Controls.Add(actionBar, 0, 15);

        _lblStatus = new Label
        {
            AutoSize = true,
            ForeColor = AdminTheme.MutedText,
            Font = AdminTheme.CreateFont(9.1f),
            Margin = new Padding(2, 0, 2, 10)
        };
        layout.Controls.Add(_lblStatus, 0, 16);

        layout.Controls.Add(CreateFieldLabel("Uretilen Lisans Anahtari"), 0, 17);
        _txtResult = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.FixedSingle,
            Font = AdminTheme.CreateFont(9.1f),
            Margin = new Padding(0, 2, 0, 0)
        };
        layout.Controls.Add(_txtResult, 0, 18);
    }

    private void BuildRightPanel(Control host)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(22)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        host.Controls.Add(layout);

        layout.Controls.Add(new Label
        {
            Text = "Verilen Lisanslar",
            Font = AdminTheme.CreateHeadingFont(15f, FontStyle.Bold),
            ForeColor = AdminTheme.Heading,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 4)
        }, 0, 0);

        var subtitleRow = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 0, 0, 12)
        };
        subtitleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        subtitleRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.Controls.Add(subtitleRow, 0, 1);

        subtitleRow.Controls.Add(new Label
        {
            Text = "Her kurulum kodu icin tek kayit tutulur. Daha once verilmis lisans otomatik bulunur.",
            Font = AdminTheme.CreateFont(9.2f),
            ForeColor = AdminTheme.MutedText,
            AutoSize = true
        }, 0, 0);

        var btnRefresh = CreateActionButton("Yenile", AdminTheme.Navy);
        btnRefresh.Click += (_, __) => LoadLedger();
        subtitleRow.Controls.Add(btnRefresh, 1, 0);

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            AutoGenerateColumns = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "License ID", DataPropertyName = nameof(LedgerGridRow.LicenseId), Width = 150 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Musteri", DataPropertyName = nameof(LedgerGridRow.CustomerName), Width = 180 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Kurulum Kodu", DataPropertyName = nameof(LedgerGridRow.InstallCode), Width = 220 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Edition", DataPropertyName = nameof(LedgerGridRow.Edition), Width = 90 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tarih (UTC)", DataPropertyName = nameof(LedgerGridRow.IssuedAtUtc), Width = 160 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Durum", DataPropertyName = nameof(LedgerGridRow.Status), Width = 90 });
        layout.Controls.Add(_grid, 0, 2);
    }

    private void BrowseKeyFile()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Key files (*.xml;*.pem)|*.xml;*.pem|All files (*.*)|*.*",
            CheckFileExists = true,
            Title = "Private key sec"
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _txtKeyPath.Text = dialog.FileName;
            LicenseAdminRuntime.RememberPrivateKeyPath(dialog.FileName);
        }
    }

    private void GenerateLicense()
    {
        var keyPath = (_txtKeyPath.Text ?? string.Empty).Trim();
        var customerName = (_txtCustomerName.Text ?? string.Empty).Trim();
        var receiptOcrApiKey = (_txtReceiptOcrApiKey.Text ?? string.Empty).Trim();
        var receiptOcrProvider = (_txtReceiptOcrProvider.Text ?? string.Empty).Trim();
        var receiptOcrModel = (_txtReceiptOcrModel.Text ?? string.Empty).Trim();
        var edition = string.IsNullOrWhiteSpace(_txtEdition.Text) ? "pro" : _txtEdition.Text.Trim();
        var licenseId = string.IsNullOrWhiteSpace(_txtLicenseId.Text)
            ? LicenseAdminRuntime.CreateLicenseId()
            : _txtLicenseId.Text.Trim().ToUpperInvariant();

        if (!File.Exists(keyPath))
        {
            SetStatus("Gecerli bir private key dosyasi secin.", AdminTheme.Error);
            return;
        }

        LicenseAdminRuntime.RememberPrivateKeyPath(keyPath);

        if (string.IsNullOrWhiteSpace(customerName))
        {
            SetStatus("Musteri adi zorunlu.", AdminTheme.Error);
            return;
        }

        if (!InstallCodeFormat.TryNormalize(_txtInstallCode.Text, out var installCode))
        {
            SetStatus($"Kurulum kodu gecersiz. Ornek bicim: {InstallCodeFormat.Example}", AdminTheme.Error);
            return;
        }

        _txtInstallCode.Text = installCode;

        var installCodeHash = LicenseKeyIssuer.ComputeInstallCodeHash(installCode);
        var existing = _ledger.FindByInstallCodeHash(installCodeHash);
        if (existing is not null)
        {
            _txtResult.Text = existing.LicenseKey;
            _txtLicenseId.Text = existing.LicenseId;
            SetStatus("Bu kurulum kodu icin daha once lisans uretilmis. Mevcut kayit yansitildi.", AdminTheme.Warning);
            return;
        }

        try
        {
            var licenseKey = _issuer.CreateLicenseKey(
                customerName,
                installCode,
                licenseId,
                edition,
                keyPath,
                receiptOcrApiKey,
                receiptOcrProvider,
                receiptOcrModel);
            _txtResult.Text = licenseKey;
            _txtLicenseId.Text = licenseId;

            _ledger.Save(new IssuedLicenseRecord
            {
                LicenseId = licenseId,
                CustomerName = customerName,
                InstallCode = installCode,
                InstallCodeHash = installCodeHash,
                Edition = edition,
                LicenseKey = licenseKey,
                LicenseKeyFingerprint = LicenseKeyIssuer.ComputeLicenseFingerprint(licenseKey),
                Notes = (_txtNotes.Text ?? string.Empty).Trim(),
                Status = "Issued",
                IssuedAtUtc = DateTime.UtcNow
            });

            LoadLedger();
            SetStatus("Lisans anahtari olusturuldu ve local ledger'a kaydedildi.", AdminTheme.Success);
            _txtLicenseId.Text = LicenseAdminRuntime.CreateLicenseId();
        }
        catch (Exception ex)
        {
            SetStatus($"Lisans uretilemedi: {ex.Message}", AdminTheme.Error);
        }
    }

    private void LoadLedger()
    {
        var rows = _ledger
            .GetAll()
            .Select(x => new LedgerGridRow
            {
                LicenseId = x.LicenseId,
                CustomerName = x.CustomerName,
                InstallCode = x.InstallCode,
                Edition = x.Edition,
                IssuedAtUtc = x.IssuedAtUtc.ToString("yyyy-MM-dd HH:mm"),
                Status = x.Status
            })
            .ToList();

        _grid.DataSource = rows;
    }

    private void SetStatus(string message, Color color)
    {
        _lblStatus.Text = message;
        _lblStatus.ForeColor = color;
    }

    private void InitializeDefaults()
    {
        var keyPath = LicenseAdminRuntime.TryResolvePrivateKeyPath();
        if (!string.IsNullOrWhiteSpace(keyPath))
            _txtKeyPath.Text = keyPath;

        try
        {
            if (string.IsNullOrWhiteSpace(_txtInstallCode.Text) &&
                Clipboard.ContainsText() &&
                InstallCodeFormat.TryNormalize(Clipboard.GetText(), out var installCode))
            {
                _txtInstallCode.Text = installCode;
            }
        }
        catch
        {
            // Clipboard access is best-effort.
        }

        if (!string.IsNullOrWhiteSpace(_txtKeyPath.Text) && !string.IsNullOrWhiteSpace(_txtInstallCode.Text))
        {
            SetStatus("Private key otomatik secildi. Kurulum kodu clipboard'dan alindi; sadece musteri adi girmeniz yeterli.", AdminTheme.Success);
            _txtCustomerName.Focus();
            return;
        }

        if (!string.IsNullOrWhiteSpace(_txtKeyPath.Text))
        {
            SetStatus("Private key otomatik secildi.", AdminTheme.Success);
            _txtCustomerName.Focus();
        }
    }

    private static Panel CreateSurfacePanel()
    {
        return new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AdminTheme.Surface,
            BorderStyle = BorderStyle.FixedSingle
        };
    }

    private static Label CreateFieldLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            ForeColor = AdminTheme.Heading,
            Font = AdminTheme.CreateHeadingFont(9.4f, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 6)
        };
    }

    private static TextBox CreateInputBox()
    {
        var font = AdminTheme.CreateFont(9.5f);
        return new TextBox
        {
            Dock = DockStyle.Top,
            AutoSize = false,
            BorderStyle = BorderStyle.FixedSingle,
            Font = font,
            Height = 38,
            MinimumSize = new Size(0, 38),
            Margin = new Padding(0, 2, 0, 10)
        };
    }

    private static TableLayoutPanel CreateActionRow()
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 2, 0, 10)
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 98));
        return row;
    }

    private static Button CreateActionButton(string text, Color backColor)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(94, 38),
            BackColor = backColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = AdminTheme.CreateHeadingFont(9.2f, FontStyle.Bold),
            Padding = new Padding(12, 6, 12, 6),
            Margin = new Padding(8, 0, 0, 0)
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(21, 38, 61);
        button.FlatAppearance.BorderSize = 1;
        return button;
    }

    private sealed class LedgerGridRow
    {
        public string LicenseId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string InstallCode { get; set; } = string.Empty;
        public string Edition { get; set; } = string.Empty;
        public string IssuedAtUtc { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
