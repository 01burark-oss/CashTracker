using System;
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
    internal sealed class UrunStokForm : Form
    {
        private readonly IUrunHizmetService _urunService;
        private readonly IStokService _stokService;
        private readonly DataGridView _grid = new();
        private readonly ComboBox _cmbTip = new();
        private readonly TextBox _txtAd = new();
        private readonly TextBox _txtBarkod = new();
        private readonly TextBox _txtBirim = new();
        private readonly NumericUpDown _numKdv = new();
        private readonly NumericUpDown _numAlis = new();
        private readonly NumericUpDown _numSatis = new();
        private readonly NumericUpDown _numKritik = new();
        private readonly CheckBox _chkAktif = new();
        private readonly Label _lblStok = new();
        private readonly NumericUpDown _numStokMiktar = new();
        private readonly DateTimePicker _dtStok = new();
        private readonly TextBox _txtStokAciklama = new();
        private int? _selectedId;

        public UrunStokForm(IUrunHizmetService urunService, IStokService stokService)
        {
            _urunService = urunService;
            _stokService = stokService;
            Text = "Urun / Stok";
            Width = 1120;
            Height = 700;
            UiMetrics.ApplyFullscreenDialogDefaults(this);
            Font = new Font("Segoe UI", 10f);
            BuildUi();
            Load += async (_, __) => await RefreshGridAsync();
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
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46));
            Controls.Add(root);

            _grid.Dock = DockStyle.Fill;
            _grid.ReadOnly = true;
            _grid.AllowUserToAddRows = false;
            _grid.MultiSelect = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grid.SelectionChanged += async (_, __) => await LoadSelectedAsync();
            root.Controls.Add(_grid, 0, 0);

            var right = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(14, 0, 0, 0)
            };
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.Controls.Add(right, 1, 0);

            right.Controls.Add(new Label
            {
                Text = "Urun / hizmet karti",
                Font = new Font(Font, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10)
            }, 0, 0);

            var form = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, AutoSize = true };
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            right.Controls.Add(form, 0, 1);

            _cmbTip.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbTip.Items.AddRange(["Urun", "Hizmet"]);
            _cmbTip.SelectedIndex = 0;
            _txtBirim.Text = "Adet";
            ConfigureMoney(_numKdv, 100, 20);
            ConfigureMoney(_numAlis, 1_000_000_000, 0);
            ConfigureMoney(_numSatis, 1_000_000_000, 0);
            ConfigureMoney(_numKritik, 1_000_000_000, 0);
            _chkAktif.Text = "Aktif";
            _chkAktif.Checked = true;

            AddRow(form, 0, "Tip", _cmbTip);
            AddRow(form, 1, "Ad", _txtAd);
            AddRow(form, 2, "Barkod", _txtBarkod);
            AddRow(form, 3, "Birim", _txtBirim);
            AddRow(form, 4, "KDV %", _numKdv);
            AddRow(form, 5, "Alis", _numAlis);
            AddRow(form, 6, "Satis", _numSatis);
            AddRow(form, 7, "Kritik stok", _numKritik);
            AddRow(form, 8, "", _chkAktif);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding(0, 12, 0, 14)
            };
            right.Controls.Add(buttons, 0, 2);
            var btnNew = CreateButton("Yeni");
            var btnSave = CreateButton("Kaydet");
            var btnDelete = CreateButton("Sil");
            buttons.Controls.AddRange([btnNew, btnSave, btnDelete]);
            btnNew.Click += (_, __) => ClearForm();
            btnSave.Click += async (_, __) => await SaveAsync();
            btnDelete.Click += async (_, __) => await DeleteAsync();

            _lblStok.Text = "Mevcut stok: 0";
            _lblStok.Font = new Font(Font, FontStyle.Bold);
            _lblStok.AutoSize = true;
            _lblStok.Margin = new Padding(0, 0, 0, 10);
            right.Controls.Add(_lblStok, 0, 3);

            var stockPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight
            };
            right.Controls.Add(stockPanel, 0, 4);
            _numStokMiktar.Minimum = -1_000_000_000;
            _numStokMiktar.Maximum = 1_000_000_000;
            _numStokMiktar.DecimalPlaces = 2;
            _numStokMiktar.Width = 120;
            _dtStok.Width = 130;
            _txtStokAciklama.Width = 220;
            var btnAddStock = CreateButton("Stok Isle");
            stockPanel.Controls.AddRange([
                Labeled("Miktar (+/-)", _numStokMiktar),
                Labeled("Tarih", _dtStok),
                Labeled("Aciklama", _txtStokAciklama),
                btnAddStock
            ]);
            btnAddStock.Click += async (_, __) => await AddStockAsync();
        }

        private async Task RefreshGridAsync()
        {
            var rows = await _urunService.GetAllAsync();
            var viewRows = rows.Select(x => new
            {
                x.Id,
                x.Tip,
                x.Ad,
                x.Barkod,
                x.Birim,
                Kdv = x.KdvOrani,
                Satis = x.SatisFiyati,
                x.KritikStok,
                x.Aktif
            }).ToList();
            _grid.DataSource = viewRows;
        }

        private async Task LoadSelectedAsync()
        {
            if (_grid.CurrentRow?.Cells["Id"].Value is not int id)
                return;

            _selectedId = id;
            var row = await _urunService.GetByIdAsync(id);
            if (row == null)
                return;

            _cmbTip.SelectedItem = row.Tip;
            _txtAd.Text = row.Ad;
            _txtBarkod.Text = row.Barkod;
            _txtBirim.Text = row.Birim;
            _numKdv.Value = Clamp(row.KdvOrani, _numKdv);
            _numAlis.Value = Clamp(row.AlisFiyati, _numAlis);
            _numSatis.Value = Clamp(row.SatisFiyati, _numSatis);
            _numKritik.Value = Clamp(row.KritikStok, _numKritik);
            _chkAktif.Checked = row.Aktif;
            await RefreshStockAsync(id, row.KritikStok);
        }

        private async Task RefreshStockAsync(int id, decimal criticalStock)
        {
            var stock = await _stokService.GetCurrentStockAsync(id);
            _lblStok.Text = $"Mevcut stok: {stock:N2}";
            _lblStok.ForeColor = stock <= criticalStock ? Color.FromArgb(173, 59, 56) : Color.FromArgb(20, 112, 82);
        }

        private async Task SaveAsync()
        {
            try
            {
                if (_selectedId.HasValue)
                {
                    await _urunService.UpdateAsync(new UrunHizmet
                    {
                        Id = _selectedId.Value,
                        Tip = _cmbTip.SelectedItem?.ToString() ?? "Urun",
                        Ad = _txtAd.Text,
                        Barkod = _txtBarkod.Text,
                        Birim = _txtBirim.Text,
                        KdvOrani = _numKdv.Value,
                        AlisFiyati = _numAlis.Value,
                        SatisFiyati = _numSatis.Value,
                        KritikStok = _numKritik.Value,
                        Aktif = _chkAktif.Checked
                    });
                }
                else
                {
                    _selectedId = await _urunService.CreateAsync(new UrunHizmetCreateRequest
                    {
                        Tip = _cmbTip.SelectedItem?.ToString() ?? "Urun",
                        Ad = _txtAd.Text,
                        Barkod = _txtBarkod.Text,
                        Birim = _txtBirim.Text,
                        KdvOrani = _numKdv.Value,
                        AlisFiyati = _numAlis.Value,
                        SatisFiyati = _numSatis.Value,
                        KritikStok = _numKritik.Value
                    });
                }

                await RefreshGridAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Urun kayit hatasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async Task DeleteAsync()
        {
            if (!_selectedId.HasValue)
                return;

            if (MessageBox.Show(this, "Urun ve stok hareketleri silinsin mi?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            await _urunService.DeleteAsync(_selectedId.Value);
            ClearForm();
            await RefreshGridAsync();
        }

        private async Task AddStockAsync()
        {
            if (!_selectedId.HasValue)
            {
                MessageBox.Show(this, "Once urun secin.", "Stok", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var result = await _stokService.CreateMovementAsync(new StokHareketCreateRequest
                {
                    UrunHizmetId = _selectedId.Value,
                    Tarih = _dtStok.Value.Date,
                    Miktar = _numStokMiktar.Value,
                    Kaynak = "Manuel",
                    Aciklama = _txtStokAciklama.Text
                });
                _numStokMiktar.Value = 0;
                _txtStokAciklama.Clear();
                MessageBox.Show(
                    this,
                    result.MevcutStok < 0
                        ? $"Stok islendi. Uyari: mevcut stok negatif ({result.MevcutStok:N2})."
                        : $"Stok islendi. Mevcut stok: {result.MevcutStok:N2}",
                    "Stok",
                    MessageBoxButtons.OK,
                    result.MevcutStok < 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
                await LoadSelectedAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Stok hareket hatasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ClearForm()
        {
            _selectedId = null;
            _cmbTip.SelectedIndex = 0;
            _txtAd.Clear();
            _txtBarkod.Clear();
            _txtBirim.Text = "Adet";
            _numKdv.Value = 20;
            _numAlis.Value = 0;
            _numSatis.Value = 0;
            _numKritik.Value = 0;
            _chkAktif.Checked = true;
            _lblStok.Text = "Mevcut stok: 0";
        }

        private static void ConfigureMoney(NumericUpDown input, decimal max, decimal value)
        {
            input.Maximum = max;
            input.DecimalPlaces = 2;
            input.Value = value;
        }

        private static decimal Clamp(decimal value, NumericUpDown input)
        {
            return Math.Max(input.Minimum, Math.Min(input.Maximum, value));
        }

        private static void AddRow(TableLayoutPanel table, int row, string label, Control input)
        {
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 7, 8, 7) }, 0, row);
            input.Dock = DockStyle.Fill;
            input.Margin = new Padding(0, 3, 0, 3);
            table.Controls.Add(input, 1, row);
        }

        private static Control Labeled(string label, Control input)
        {
            var panel = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, RowCount = 2, Margin = new Padding(0, 0, 8, 8) };
            panel.Controls.Add(new Label { Text = label, AutoSize = true }, 0, 0);
            panel.Controls.Add(input, 0, 1);
            return panel;
        }

        private static Button CreateButton(string text)
        {
            return new Button { Text = text, Width = 112, Height = 34, Margin = new Padding(0, 0, 8, 0) };
        }
    }
}
