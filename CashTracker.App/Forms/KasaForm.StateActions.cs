using System;
using System.ComponentModel;
using System.Threading.Tasks;
using CashTracker.Core.Entities;

namespace CashTracker.App.Forms
{
    public sealed partial class KasaForm
    {
        private void ToggleGiderTuru()
        {
            var isGider = _cmbTip.SelectedItem?.ToString() == "Gider";
            _txtGiderTuru.Enabled = isGider;
            if (!isGider)
                _txtGiderTuru.Text = string.Empty;
        }

        private async Task LoadAllAsync()
        {
            var list = await _kasaService.GetAllAsync();
            _grid.DataSource = new BindingList<Kasa>(list);
            _grid.ClearSelection();
            ClearForm();
        }

        private void GridToForm()
        {
            if (_grid.CurrentRow?.DataBoundItem is not Kasa kasa)
                return;

            _selectedId = kasa.Id;
            _dtTarih.Value = kasa.Tarih;
            _cmbTip.SelectedItem = MapTip(kasa.Tip);
            _numTutar.Value = kasa.Tutar;
            _txtGiderTuru.Text = kasa.GiderTuru ?? string.Empty;
            _txtAciklama.Text = kasa.Aciklama ?? string.Empty;
            ToggleGiderTuru();
        }

        private void ClearForm()
        {
            _selectedId = 0;
            _dtTarih.Value = DateTime.Now;
            _cmbTip.SelectedIndex = 0;
            _numTutar.Value = 0;
            _txtGiderTuru.Text = string.Empty;
            _txtAciklama.Text = string.Empty;
            ToggleGiderTuru();
        }
    }
}
