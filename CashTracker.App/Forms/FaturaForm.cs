using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CashTracker.App.UI;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;

namespace CashTracker.App.Forms
{
    internal sealed class FaturaForm : Form
    {
        private readonly IFaturaService _faturaService;
        private readonly ICariService _cariService;
        private readonly IUrunHizmetService _urunService;
        private readonly IStokService _stokService;
        private readonly IGibPortalService _gibPortalService;
        private readonly ITahsilatOdemeService _tahsilatOdemeService;
        private readonly DataGridView _grid = new();
        private readonly ComboBox _cmbFaturaTipi = new();
        private readonly ComboBox _cmbCari = new();
        private readonly DateTimePicker _dtTarih = new();
        private readonly DateTimePicker _dtVade = new();
        private readonly CheckBox _chkVade = new();
        private readonly ComboBox _cmbOdemeYontemi = new();
        private readonly TextBox _txtAciklama = new();
        private readonly ComboBox _cmbUrun = new();
        private readonly TextBox _txtSatirAciklama = new();
        private readonly TextBox _txtBirim = new();
        private readonly NumericUpDown _numMiktar = new();
        private readonly NumericUpDown _numBirimFiyat = new();
        private readonly NumericUpDown _numKdv = new();
        private readonly NumericUpDown _numIskonto = new();
        private readonly CheckBox _chkStokEtkilesin = new();
        private readonly Label _lblDetail = new();
        private readonly NumericUpDown _numTahsilat = new();
        private readonly TextBox _txtTahsilatAciklama = new();
        private List<CariKart> _cariler = [];
        private List<UrunHizmet> _urunler = [];
        private int? _selectedFaturaId;

        public FaturaForm(
            IFaturaService faturaService,
            ICariService cariService,
            IUrunHizmetService urunService,
            IStokService stokService,
            IGibPortalService gibPortalService,
            ITahsilatOdemeService tahsilatOdemeService)
        {
            _faturaService = faturaService;
            _cariService = cariService;
            _urunService = urunService;
            _stokService = stokService;
            _gibPortalService = gibPortalService;
            _tahsilatOdemeService = tahsilatOdemeService;
            Text = "Faturalar";
            Width = 1280;
            Height = 820;
            UiMetrics.ApplyFullscreenDialogDefaults(this);
            Font = new Font("Segoe UI", 10f);
            BuildUi();
            Load += async (_, __) => await LoadDataAsync();
        }

        private void BuildUi()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(16)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 49));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 51));
            Controls.Add(root);

            _grid.Dock = DockStyle.Fill;
            _grid.ReadOnly = true;
            _grid.AllowUserToAddRows = false;
            _grid.MultiSelect = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grid.SelectionChanged += async (_, __) => await LoadSelectedDetailAsync();
            root.Controls.Add(_grid, 0, 0);

            var right = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                Padding = new Padding(14, 0, 0, 0)
            };
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.Controls.Add(right, 1, 0);

            right.Controls.Add(new Label
            {
                Text = "Yerel fatura taslagi",
                Font = new Font(Font.FontFamily, 13f, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10)
            }, 0, 0);

            var invoiceForm = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 4, AutoSize = true };
            invoiceForm.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95));
            invoiceForm.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            invoiceForm.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95));
            invoiceForm.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            right.Controls.Add(invoiceForm, 0, 1);

            _cmbFaturaTipi.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbFaturaTipi.Items.AddRange(["Satis", "Alis"]);
            _cmbFaturaTipi.SelectedIndex = 0;
            _cmbFaturaTipi.SelectedIndexChanged += (_, __) => ApplySelectedProduct();
            _cmbCari.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbOdemeYontemi.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbOdemeYontemi.Items.AddRange(["Nakit", "KrediKarti", "OnlineOdeme", "Havale"]);
            _cmbOdemeYontemi.SelectedIndex = 0;
            _chkVade.Text = "Vade var";
            _dtVade.Enabled = false;
            _chkVade.CheckedChanged += (_, __) => _dtVade.Enabled = _chkVade.Checked;
            _txtAciklama.Width = 220;

            AddRow(invoiceForm, 0, "Tip", _cmbFaturaTipi, "Cari", _cmbCari);
            AddRow(invoiceForm, 1, "Tarih", _dtTarih, "Odeme", _cmbOdemeYontemi);
            AddRow(invoiceForm, 2, "", _chkVade, "Vade", _dtVade);
            AddRow(invoiceForm, 3, "Aciklama", _txtAciklama, "", new Label());

            var lineGroup = new GroupBox
            {
                Text = "V1 satir girisi (tek satir)",
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(10),
                Margin = new Padding(0, 12, 0, 8)
            };
            right.Controls.Add(lineGroup, 0, 2);

            var lineForm = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 4, AutoSize = true };
            lineForm.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95));
            lineForm.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            lineForm.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95));
            lineForm.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            lineGroup.Controls.Add(lineForm);

            _cmbUrun.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbUrun.SelectedIndexChanged += (_, __) => ApplySelectedProduct();
            _txtBirim.Text = "Adet";
            ConfigureNumber(_numMiktar, 1_000_000, 1);
            ConfigureNumber(_numBirimFiyat, 1_000_000_000, 0);
            ConfigureNumber(_numKdv, 100, 20);
            ConfigureNumber(_numIskonto, 100, 0);
            _chkStokEtkilesin.Text = "Stok etkilensin";
            _chkStokEtkilesin.Checked = true;

            AddRow(lineForm, 0, "Urun", _cmbUrun, "Satir", _txtSatirAciklama);
            AddRow(lineForm, 1, "Birim", _txtBirim, "Miktar", _numMiktar);
            AddRow(lineForm, 2, "Birim fiyat", _numBirimFiyat, "KDV %", _numKdv);
            AddRow(lineForm, 3, "Iskonto %", _numIskonto, "", _chkStokEtkilesin);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding(0, 10, 0, 8)
            };
            right.Controls.Add(buttons, 0, 3);
            var btnCreate = CreateButton("Taslak Olustur", 130);
            var btnPortalDraft = CreateButton("GIB Taslak", 110);
            var btnIssue = CreateButton("Kes / Onayla", 120);
            var btnCancel = CreateButton("Iptal", 80);
            buttons.Controls.AddRange([btnCreate, btnPortalDraft, btnIssue, btnCancel]);
            btnCreate.Click += async (_, __) => await CreateDraftAsync();
            btnPortalDraft.Click += async (_, __) => await CreatePortalDraftAsync();
            btnIssue.Click += async (_, __) => await MarkIssuedAsync();
            btnCancel.Click += async (_, __) => await CancelAsync();

            _lblDetail.AutoSize = true;
            _lblDetail.MaximumSize = new Size(620, 0);
            _lblDetail.Margin = new Padding(0, 0, 0, 10);
            right.Controls.Add(_lblDetail, 0, 4);

            var paymentPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight
            };
            right.Controls.Add(paymentPanel, 0, 5);
            ConfigureNumber(_numTahsilat, 1_000_000_000, 0);
            _txtTahsilatAciklama.Width = 220;
            var btnPayment = CreateButton("Tahsilat/Odeme", 130);
            paymentPanel.Controls.AddRange([
                Labeled("Tutar", _numTahsilat),
                Labeled("Aciklama", _txtTahsilatAciklama),
                btnPayment
            ]);
            btnPayment.Click += async (_, __) => await AddPaymentAsync();
        }

        private async Task LoadDataAsync()
        {
            _cariler = await _cariService.GetAllAsync();
            _urunler = await _urunService.GetAllAsync();

            _cmbCari.DataSource = _cariler.Select(x => new Option(x.Id, x.Unvan)).ToList();
            _cmbCari.DisplayMember = nameof(Option.Text);
            _cmbCari.ValueMember = nameof(Option.Id);

            var productOptions = new List<Option> { new(0, "Manuel satir") };
            productOptions.AddRange(_urunler.Select(x => new Option(x.Id, $"{x.Ad} ({x.Tip})")));
            _cmbUrun.DataSource = productOptions;
            _cmbUrun.DisplayMember = nameof(Option.Text);
            _cmbUrun.ValueMember = nameof(Option.Id);
            await RefreshInvoicesAsync();
        }

        private async Task RefreshInvoicesAsync()
        {
            var rows = await _faturaService.GetAllAsync();
            _grid.DataSource = rows.Select(x => new
            {
                x.Id,
                Tarih = x.Tarih.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                x.FaturaTipi,
                x.Durum,
                No = string.IsNullOrWhiteSpace(x.PortalBelgeNo) ? x.YerelFaturaNo : x.PortalBelgeNo,
                Toplam = x.GenelToplam.ToString("N2"),
                Odenen = x.OdenenTutar.ToString("N2")
            }).ToList();
        }

        private async Task LoadSelectedDetailAsync()
        {
            if (_grid.CurrentRow?.Cells["Id"].Value is not int id)
                return;

            _selectedFaturaId = id;
            var detail = await _faturaService.GetDetailAsync(id);
            if (detail == null)
                return;

            var fatura = detail.Fatura;
            var remaining = Math.Max(0, fatura.GenelToplam - fatura.OdenenTutar);
            _numTahsilat.Value = Math.Min(_numTahsilat.Maximum, remaining);
            _lblDetail.Text =
                $"Secili: {fatura.YerelFaturaNo} | {fatura.Durum} | Cari: {detail.Cari?.Unvan ?? "-"} | Toplam: {fatura.GenelToplam:N2} | Kalan: {remaining:N2}\n" +
                $"Portal No: {(string.IsNullOrWhiteSpace(fatura.PortalBelgeNo) ? "-" : fatura.PortalBelgeNo)} | UUID: {(string.IsNullOrWhiteSpace(fatura.PortalUuid) ? "-" : fatura.PortalUuid)}";
        }

        private async Task CreateDraftAsync()
        {
            if (_cmbCari.SelectedValue is not int cariId || cariId <= 0)
            {
                MessageBox.Show(this, "Once cari kart secin.", "Fatura", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var productId = _cmbUrun.SelectedValue is int selectedProductId && selectedProductId > 0 ? selectedProductId : (int?)null;
                await _faturaService.CreateDraftAsync(new FaturaCreateRequest
                {
                    CariKartId = cariId,
                    FaturaTipi = _cmbFaturaTipi.SelectedItem?.ToString() ?? "Satis",
                    Tarih = _dtTarih.Value.Date,
                    VadeTarihi = _chkVade.Checked ? _dtVade.Value.Date : null,
                    OdemeYontemi = _cmbOdemeYontemi.SelectedItem?.ToString() ?? "Nakit",
                    Aciklama = _txtAciklama.Text,
                    Satirlar =
                    [
                        new FaturaSatirRequest
                        {
                            UrunHizmetId = productId,
                            Aciklama = string.IsNullOrWhiteSpace(_txtSatirAciklama.Text) ? "Urun/Hizmet" : _txtSatirAciklama.Text,
                            Birim = _txtBirim.Text,
                            Miktar = _numMiktar.Value,
                            BirimFiyat = _numBirimFiyat.Value,
                            KdvOrani = _numKdv.Value,
                            IskontoOrani = _numIskonto.Value,
                            StokEtkilesin = _chkStokEtkilesin.Checked && productId.HasValue
                        }
                    ]
                });

                await RefreshInvoicesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Fatura taslak hatasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async Task CreatePortalDraftAsync()
        {
            if (!_selectedFaturaId.HasValue)
                return;

            var result = await _gibPortalService.CreatePortalDraftAsync(_selectedFaturaId.Value);
            MessageBox.Show(this, result.Message, "GIB Portal", MessageBoxButtons.OK, result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            await RefreshInvoicesAsync();
            await LoadSelectedDetailAsync();
        }

        private async Task MarkIssuedAsync()
        {
            if (!_selectedFaturaId.HasValue)
                return;

            var detail = await _faturaService.GetDetailAsync(_selectedFaturaId.Value);
            if (detail == null)
                return;

            if (detail.Fatura.Durum == FaturaDurum.PortalTaslak && !string.IsNullOrWhiteSpace(detail.Fatura.PortalUuid))
            {
                await CompletePortalSmsApprovalAsync(detail);
                return;
            }

            var warning = await BuildNegativeStockWarningAsync(detail);
            var message = string.IsNullOrWhiteSpace(warning)
                ? "Fatura Kesildi durumuna alinsin mi? Bu islem cari ve stok hareketi olusturur."
                : warning + Environment.NewLine + Environment.NewLine + "Yine de devam edilsin mi?";

            if (MessageBox.Show(this, message, "Fatura onayi", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            try
            {
                await _faturaService.MarkAsIssuedAsync(_selectedFaturaId.Value);
                await RefreshInvoicesAsync();
                await LoadSelectedDetailAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Fatura kesme hatasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async Task CompletePortalSmsApprovalAsync(FaturaDetail detail)
        {
            var warning = await BuildNegativeStockWarningAsync(detail);
            if (!string.IsNullOrWhiteSpace(warning) &&
                MessageBox.Show(this, warning + Environment.NewLine + Environment.NewLine + "GIB SMS onayina devam edilsin mi?", "Negatif stok uyarisi", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            var start = await _gibPortalService.StartSmsApprovalAsync(detail.Fatura.Id);
            if (!start.Success)
            {
                MessageBox.Show(this, start.Message, "GIB SMS", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var prompt = new SmsCodePromptForm(start.Message + Environment.NewLine + "Gelen SMS kodunu girin.");
            if (prompt.ShowDialog(this) != DialogResult.OK)
                return;

            var complete = await _gibPortalService.CompleteSmsApprovalAsync(detail.Fatura.Id, start.OperationId, prompt.SmsCode);
            MessageBox.Show(this, complete.Message, "GIB SMS", MessageBoxButtons.OK, complete.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            await RefreshInvoicesAsync();
            await LoadSelectedDetailAsync();
        }

        private async Task<string> BuildNegativeStockWarningAsync(FaturaDetail detail)
        {
            if (detail.Fatura.FaturaTipi != "Satis")
                return string.Empty;

            var warnings = new List<string>();
            foreach (var line in detail.Satirlar.Where(x => x.StokEtkilesin && x.UrunHizmetId.HasValue))
            {
                var current = await _stokService.GetCurrentStockAsync(line.UrunHizmetId!.Value);
                var after = current - line.Miktar;
                if (after < 0)
                    warnings.Add($"{line.Aciklama}: mevcut {current:N2}, satis {line.Miktar:N2}, kalan {after:N2}");
            }

            return warnings.Count == 0
                ? string.Empty
                : "Negatif stok uyarisi:" + Environment.NewLine + string.Join(Environment.NewLine, warnings);
        }

        private async Task CancelAsync()
        {
            if (!_selectedFaturaId.HasValue)
                return;

            try
            {
                await _faturaService.CancelAsync(_selectedFaturaId.Value);
                await RefreshInvoicesAsync();
                await LoadSelectedDetailAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Fatura iptal hatasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async Task AddPaymentAsync()
        {
            if (!_selectedFaturaId.HasValue)
                return;

            try
            {
                await _tahsilatOdemeService.CreateAsync(new TahsilatOdemeRequest
                {
                    FaturaId = _selectedFaturaId.Value,
                    Tarih = DateTime.Now.Date,
                    Tutar = _numTahsilat.Value,
                    OdemeYontemi = _cmbOdemeYontemi.SelectedItem?.ToString() ?? "Nakit",
                    Aciklama = _txtTahsilatAciklama.Text
                });
                _txtTahsilatAciklama.Clear();
                await RefreshInvoicesAsync();
                await LoadSelectedDetailAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Tahsilat/odeme hatasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ApplySelectedProduct()
        {
            if (_cmbUrun.SelectedValue is not int productId || productId <= 0)
            {
                _chkStokEtkilesin.Checked = false;
                return;
            }

            var product = _urunler.FirstOrDefault(x => x.Id == productId);
            if (product == null)
                return;

            _txtSatirAciklama.Text = product.Ad;
            _txtBirim.Text = product.Birim;
            _numKdv.Value = Clamp(product.KdvOrani, _numKdv);
            var price = string.Equals(_cmbFaturaTipi.SelectedItem?.ToString(), "Alis", StringComparison.Ordinal)
                ? product.AlisFiyati
                : product.SatisFiyati;
            _numBirimFiyat.Value = Clamp(price, _numBirimFiyat);
            _chkStokEtkilesin.Checked = product.Tip == "Urun";
        }

        private static void ConfigureNumber(NumericUpDown input, decimal max, decimal value)
        {
            input.Maximum = max;
            input.DecimalPlaces = 2;
            input.Value = value;
            input.Width = 120;
        }

        private static decimal Clamp(decimal value, NumericUpDown input)
        {
            return Math.Max(input.Minimum, Math.Min(input.Maximum, value));
        }

        private static void AddRow(TableLayoutPanel table, int row, string label1, Control input1, string label2, Control input2)
        {
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(new Label { Text = label1, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 7, 8, 7) }, 0, row);
            input1.Dock = DockStyle.Fill;
            input1.Margin = new Padding(0, 3, 12, 3);
            table.Controls.Add(input1, 1, row);
            table.Controls.Add(new Label { Text = label2, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 7, 8, 7) }, 2, row);
            input2.Dock = DockStyle.Fill;
            input2.Margin = new Padding(0, 3, 0, 3);
            table.Controls.Add(input2, 3, row);
        }

        private static Control Labeled(string label, Control input)
        {
            var panel = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, RowCount = 2, Margin = new Padding(0, 0, 8, 8) };
            panel.Controls.Add(new Label { Text = label, AutoSize = true }, 0, 0);
            panel.Controls.Add(input, 0, 1);
            return panel;
        }

        private static Button CreateButton(string text, int width)
        {
            return new Button { Text = text, Width = width, Height = 34, Margin = new Padding(0, 0, 8, 0) };
        }

        private sealed record Option(int Id, string Text);
    }
}
