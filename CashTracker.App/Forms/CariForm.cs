using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CashTracker.App.UI;
using CashTracker.Core.Entities;
using CashTracker.Core.Services;

namespace CashTracker.App.Forms
{
    internal sealed class CariForm : Form
    {
        private readonly ICariService _cariService;
        private readonly DataGridView _grid = new();
        private readonly DataGridView _movementGrid = new();
        private readonly ComboBox _cmbTip = new();
        private readonly TextBox _txtUnvan = new();
        private readonly TextBox _txtTelefon = new();
        private readonly TextBox _txtEposta = new();
        private readonly TextBox _txtVergiNo = new();
        private readonly TextBox _txtVergiDairesi = new();
        private readonly TextBox _txtAdres = new();
        private readonly CheckBox _chkAktif = new();
        private readonly Label _lblBakiye = new();
        private readonly ComboBox _cmbHareketTipi = new();
        private readonly NumericUpDown _numHareketTutar = new();
        private readonly DateTimePicker _dtHareket = new();
        private readonly TextBox _txtHareketAciklama = new();
        private int? _selectedId;

        public CariForm(ICariService cariService)
        {
            _cariService = cariService;
            Text = "Cari Hesaplar";
            Width = 1180;
            Height = 760;
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
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
            Controls.Add(root);

            _grid.Dock = DockStyle.Fill;
            _grid.ReadOnly = true;
            _grid.AllowUserToAddRows = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.MultiSelect = false;
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

            var title = new Label
            {
                Text = "Cari kart",
                Font = new Font(Font, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10)
            };
            right.Controls.Add(title, 0, 0);

            var form = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 8,
                AutoSize = true
            };
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            right.Controls.Add(form, 0, 1);

            _cmbTip.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbTip.Items.AddRange(["Musteri", "Tedarikci", "HerIkisi"]);
            _cmbTip.SelectedIndex = 0;
            _chkAktif.Text = "Aktif";
            _chkAktif.Checked = true;
            AddRow(form, 0, "Tip", _cmbTip);
            AddRow(form, 1, "Unvan", _txtUnvan);
            AddRow(form, 2, "Telefon", _txtTelefon);
            AddRow(form, 3, "E-posta", _txtEposta);
            AddRow(form, 4, "Vergi/TC No", _txtVergiNo);
            AddRow(form, 5, "Vergi Dairesi", _txtVergiDairesi);
            AddRow(form, 6, "Adres", _txtAdres);
            AddRow(form, 7, "", _chkAktif);

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

            _lblBakiye.Text = "Bakiye: 0";
            _lblBakiye.Font = new Font(Font, FontStyle.Bold);
            _lblBakiye.AutoSize = true;
            _lblBakiye.Margin = new Padding(0, 0, 0, 10);
            right.Controls.Add(_lblBakiye, 0, 3);

            var movements = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            movements.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            movements.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            movements.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            right.Controls.Add(movements, 0, 4);

            var movementInputs = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight
            };
            movements.Controls.Add(movementInputs, 0, 0);
            _cmbHareketTipi.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbHareketTipi.Items.AddRange(["Borc", "Alacak", "Tahsilat", "Odeme"]);
            _cmbHareketTipi.SelectedIndex = 0;
            _numHareketTutar.Maximum = 1_000_000_000;
            _numHareketTutar.DecimalPlaces = 2;
            _numHareketTutar.Width = 110;
            _dtHareket.Width = 130;
            _txtHareketAciklama.Width = 190;
            var btnAddMovement = CreateButton("Hareket Ekle");
            movementInputs.Controls.AddRange([
                Labeled("Tip", _cmbHareketTipi),
                Labeled("Tutar", _numHareketTutar),
                Labeled("Tarih", _dtHareket),
                Labeled("Aciklama", _txtHareketAciklama),
                btnAddMovement
            ]);
            btnAddMovement.Click += async (_, __) => await AddMovementAsync();

            _movementGrid.Dock = DockStyle.Fill;
            _movementGrid.ReadOnly = true;
            _movementGrid.AllowUserToAddRows = false;
            _movementGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            movements.Controls.Add(_movementGrid, 0, 2);
        }

        private async Task RefreshGridAsync()
        {
            var rows = await _cariService.GetAllAsync();
            _grid.DataSource = rows.Select(x => new
            {
                x.Id,
                x.Tip,
                x.Unvan,
                x.Telefon,
                VergiNo = x.VergiNoTc,
                x.Aktif
            }).ToList();
        }

        private async Task LoadSelectedAsync()
        {
            if (_grid.CurrentRow?.Cells["Id"].Value is not int id)
                return;

            _selectedId = id;
            var row = await _cariService.GetByIdAsync(id);
            if (row == null)
                return;

            _cmbTip.SelectedItem = row.Tip;
            _txtUnvan.Text = row.Unvan;
            _txtTelefon.Text = row.Telefon;
            _txtEposta.Text = row.Eposta;
            _txtVergiNo.Text = row.VergiNoTc;
            _txtVergiDairesi.Text = row.VergiDairesi;
            _txtAdres.Text = row.Adres;
            _chkAktif.Checked = row.Aktif;
            await RefreshMovementsAsync(id);
        }

        private async Task RefreshMovementsAsync(int id)
        {
            var balance = await _cariService.GetBakiyeAsync(id);
            _lblBakiye.Text = $"Bakiye: {balance:N2}";
            var movements = await _cariService.GetHareketlerAsync(id);
            _movementGrid.DataSource = movements.Select(x => new
            {
                x.Id,
                Tarih = x.Tarih.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                x.HareketTipi,
                Tutar = x.Tutar.ToString("N2"),
                x.Kaynak,
                x.Aciklama
            }).ToList();
        }

        private async Task SaveAsync()
        {
            try
            {
                var row = new CariKart
                {
                    Id = _selectedId ?? 0,
                    Tip = _cmbTip.SelectedItem?.ToString() ?? "Musteri",
                    Unvan = _txtUnvan.Text,
                    Telefon = _txtTelefon.Text,
                    Eposta = _txtEposta.Text,
                    VergiNoTc = _txtVergiNo.Text,
                    VergiDairesi = _txtVergiDairesi.Text,
                    Adres = _txtAdres.Text,
                    Aktif = _chkAktif.Checked
                };

                if (_selectedId.HasValue)
                    await _cariService.UpdateAsync(row);
                else
                    _selectedId = await _cariService.CreateAsync(row);

                await RefreshGridAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Cari kayit hatasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async Task DeleteAsync()
        {
            if (!_selectedId.HasValue)
                return;

            if (MessageBox.Show(this, "Cari kart ve hareketleri silinsin mi?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            await _cariService.DeleteAsync(_selectedId.Value);
            ClearForm();
            await RefreshGridAsync();
        }

        private async Task AddMovementAsync()
        {
            if (!_selectedId.HasValue)
            {
                MessageBox.Show(this, "Once cari secin.", "Cari hareket", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                await _cariService.CreateHareketAsync(new CariHareket
                {
                    CariKartId = _selectedId.Value,
                    Tarih = _dtHareket.Value.Date,
                    HareketTipi = _cmbHareketTipi.SelectedItem?.ToString() ?? "Borc",
                    Tutar = _numHareketTutar.Value,
                    Kaynak = "Manuel",
                    Aciklama = _txtHareketAciklama.Text
                });
                _numHareketTutar.Value = 0;
                _txtHareketAciklama.Clear();
                await RefreshMovementsAsync(_selectedId.Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Cari hareket hatasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ClearForm()
        {
            _selectedId = null;
            _cmbTip.SelectedIndex = 0;
            _txtUnvan.Clear();
            _txtTelefon.Clear();
            _txtEposta.Clear();
            _txtVergiNo.Clear();
            _txtVergiDairesi.Clear();
            _txtAdres.Clear();
            _chkAktif.Checked = true;
            _lblBakiye.Text = "Bakiye: 0";
            _movementGrid.DataSource = null;
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
