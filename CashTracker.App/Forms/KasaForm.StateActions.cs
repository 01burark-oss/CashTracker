using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CashTracker.Core.Entities;

namespace CashTracker.App.Forms
{
    public sealed partial class KasaForm
    {
        private async Task LoadAllAsync()
        {
            await RefreshActiveBusinessInfoAsync();
            var list = await _kasaService.GetAllAsync();
            _grid.DataSource = new BindingList<Kasa>(list);
            _grid.ClearSelection();
            await ClearFormAsync();
        }

        private async Task GridToFormAsync()
        {
            if (_grid.CurrentRow?.DataBoundItem is not Kasa kasa)
                return;

            _selectedId = kasa.Id;
            _dtTarih.Value = kasa.Tarih;
            _cmbTip.SelectedItem = MapTip(kasa.Tip);
            _numTutar.Value = kasa.Tutar;
            _txtAciklama.Text = kasa.Aciklama ?? string.Empty;
            await LoadKalemlerForTipAsync(kasa.Kalem ?? kasa.GiderTuru);
        }

        private async Task ClearFormAsync()
        {
            _selectedId = 0;
            _dtTarih.Value = DateTime.Now;
            _cmbTip.SelectedIndex = 0;
            _numTutar.Value = 0;
            _txtAciklama.Text = string.Empty;
            await LoadKalemlerForTipAsync();
        }

        private async Task LoadKalemlerForTipAsync(string? preferredKalem = null)
        {
            if (_isLoadingKalemler)
                return;

            _isLoadingKalemler = true;
            _cmbKalem.Enabled = false;

            try
            {
                var tip = MapTip(_cmbTip.SelectedItem?.ToString() ?? "Gelir");
                var rows = await _kalemTanimiService.GetByTipAsync(tip);
                var kalemler = rows
                    .Select(x => x.Ad)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .ToList();

                var preferred = NormalizeText(preferredKalem ?? string.Empty);
                if (!string.IsNullOrWhiteSpace(preferred) &&
                    !kalemler.Any(x => string.Equals(x, preferred, StringComparison.OrdinalIgnoreCase)))
                {
                    kalemler.Insert(0, preferred);
                }

                _cmbKalem.DataSource = null;

                if (kalemler.Count == 0)
                {
                    _cmbKalem.Items.Clear();
                    return;
                }

                _cmbKalem.DataSource = kalemler;
                if (!string.IsNullOrWhiteSpace(preferred))
                {
                    var selectedIndex = kalemler.FindIndex(x => string.Equals(x, preferred, StringComparison.OrdinalIgnoreCase));
                    _cmbKalem.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
                }
                else
                {
                    _cmbKalem.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                _cmbKalem.DataSource = null;
                _cmbKalem.Items.Clear();
                MessageBox.Show(
                    "Kalemler yuklenirken hata olustu: " + ex.Message,
                    "Gelir / Gider",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _isLoadingKalemler = false;
                _cmbKalem.Enabled = _cmbKalem.Items.Count > 0;
                UpdateKalemAvailabilityUi();
            }
        }

        private void UpdateKalemAvailabilityUi()
        {
            var hasKalem = _cmbKalem.Items.Count > 0;
            var tip = MapTip(_cmbTip.SelectedItem?.ToString() ?? "Gelir");

            _btnSave.Enabled = hasKalem;
            _lblKalemEmptyHint.Visible = !hasKalem;
            _btnKalemSettings.Visible = !hasKalem;

            if (!hasKalem)
            {
                _lblKalemEmptyHint.Text =
                    $"{tip} icin kalem tanimi bulunamadi. Ayarlar ekranindan once kalem eklemelisin.";
            }
        }

        private async Task RefreshActiveBusinessInfoAsync()
        {
            try
            {
                var active = await _isletmeService.GetActiveAsync();
                var businessName = string.IsNullOrWhiteSpace(active.Ad)
                    ? "Bilinmiyor"
                    : active.Ad.Trim();

                _lblActiveBusiness.Text = $"Aktif Isletme: {businessName}";
                Text = $"Gelir / Gider - {businessName}";
            }
            catch
            {
                _lblActiveBusiness.Text = "Aktif Isletme: Bilinmiyor";
                Text = "Gelir / Gider";
            }
        }
    }
}
