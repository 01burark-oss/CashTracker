using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using CashTracker.Core.Entities;

namespace CashTracker.App.Forms
{
    public sealed partial class KasaForm
    {
        private async Task SaveAsync()
        {
            var isNew = _selectedId == 0;
            var kasa = new Kasa
            {
                Id = _selectedId,
                Tarih = isNew ? DateTime.Now : _dtTarih.Value,
                Tip = MapTip(_cmbTip.SelectedItem?.ToString() ?? "Gelir"),
                Tutar = _numTutar.Value,
                GiderTuru = NormalizeText(_txtGiderTuru.Text),
                Aciklama = NormalizeText(_txtAciklama.Text)
            };

            if (kasa.Tip == "Gider" && string.IsNullOrWhiteSpace(kasa.GiderTuru))
            {
                MessageBox.Show("Gider türü zorunludur.");
                return;
            }

            if (isNew)
                await _kasaService.CreateAsync(kasa);
            else
                await _kasaService.UpdateAsync(kasa);

            await LoadAllAsync();
        }

        private async Task DeleteAsync()
        {
            if (_selectedId == 0)
                return;

            var confirm = MessageBox.Show(
                "Kaydı silmek istiyor musun?",
                "Onay",
                MessageBoxButtons.YesNo);

            if (confirm != DialogResult.Yes)
                return;

            await _kasaService.DeleteAsync(_selectedId);
            await LoadAllAsync();
        }

        private static string? NormalizeText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}

