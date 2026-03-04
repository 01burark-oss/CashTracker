using System;
using System.Globalization;
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
            var tip = MapTip(_cmbTip.SelectedItem?.ToString() ?? AppLocalization.T("tip.income"));
            var kalem = NormalizeText(_cmbKalem.SelectedItem?.ToString() ?? _cmbKalem.Text);
            if (!TryReadAmount(out var amount))
                return;

            var kasa = new Kasa
            {
                Id = _selectedId,
                Tarih = _dtTarih.Value,
                Tip = tip,
                Tutar = amount,
                OdemeYontemi = _selectedOdemeYontemi,
                Kalem = kalem,
                GiderTuru = tip == "Gider" ? kalem : null,
                Aciklama = NormalizeText(_txtAciklama.Text)
            };

            if (string.IsNullOrWhiteSpace(kasa.Kalem))
            {
                MessageBox.Show(AppLocalization.T("kasa.error.categoryRequired"));
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
                AppLocalization.T("kasa.delete.confirmBody"),
                AppLocalization.T("kasa.delete.confirmTitle"),
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

        private bool TryReadAmount(out decimal amount)
        {
            amount = 0m;
            var raw = (_txtTutar.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(raw))
            {
                MessageBox.Show(AppLocalization.T("kasa.error.amountRequired"));
                return false;
            }

            var normalized = raw.Replace(" ", string.Empty);
            if (!TryParseAmount(normalized, out amount))
            {
                MessageBox.Show(AppLocalization.T("kasa.error.amountInvalid"));
                return false;
            }

            if (amount <= 0)
            {
                MessageBox.Show(AppLocalization.T("kasa.error.amountPositive"));
                return false;
            }

            return true;
        }

        private static bool TryParseAmount(string value, out decimal amount)
        {
            if (decimal.TryParse(value, NumberStyles.Number, AppLocalization.CurrentCulture, out amount))
                return true;

            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.GetCultureInfo("tr-TR"), out amount))
                return true;

            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.GetCultureInfo("en-US"), out amount))
                return true;

            var dotNormalized = value.Replace(',', '.');
            if (decimal.TryParse(dotNormalized, NumberStyles.Number, CultureInfo.InvariantCulture, out amount))
                return true;

            var commaNormalized = value.Replace('.', ',');
            return decimal.TryParse(commaNormalized, NumberStyles.Number, CultureInfo.GetCultureInfo("tr-TR"), out amount);
        }
    }
}
