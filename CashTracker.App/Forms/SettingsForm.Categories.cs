using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CashTracker.App.Forms
{
    public sealed partial class SettingsForm
    {
        private async Task LoadKalemlerAsync()
        {
            if (_isLoadingKalemler)
                return;

            _isLoadingKalemler = true;
            _btnDeleteKalem.Enabled = false;
            SetCategoryHint("Kalemler yukleniyor...");

            try
            {
                var tip = GetSelectedTip();
                var rows = await _kalemTanimiService.GetByTipAsync(tip);
                var items = rows
                    .Select(x => new KalemItem
                    {
                        Id = x.Id,
                        Ad = x.Ad
                    })
                    .ToList();

                _lstKalemler.DataSource = items;
                _lstKalemler.DisplayMember = nameof(KalemItem.Ad);
                _lstKalemler.ValueMember = nameof(KalemItem.Id);

                if (items.Count == 0)
                {
                    SetCategoryHint(
                        $"{tip} icin tanimli kalem yok. Asagidan yeni kalem ekleyebilirsin.",
                        HintTone.Warning);
                }
                else
                {
                    SetCategoryHint($"{tip} icin {items.Count} kalem var.", HintTone.Success);
                }

                UpdateDeleteKalemState();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Kalem listesi yuklenemedi: " + ex.Message,
                    "Ayarlar",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                SetCategoryHint("Kalem listesi yuklenemedi.", HintTone.Error);
            }
            finally
            {
                _isLoadingKalemler = false;
            }
        }

        private async Task AddKalemAsync()
        {
            var tip = GetSelectedTip();
            var ad = NormalizeText(_txtNewKalem.Text);
            if (string.IsNullOrWhiteSpace(ad))
            {
                MessageBox.Show(
                    "Kalem adi bos birakilamaz.",
                    "Ayarlar",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                SetCategoryHint("Kalem adi bos birakilamaz.", HintTone.Warning);
                return;
            }

            if (ad.Length < 2)
            {
                SetCategoryHint("Kalem adi en az 2 karakter olmalidir.", HintTone.Warning);
                return;
            }

            if (HasKalemWithName(ad))
            {
                SetCategoryHint("Bu kalem zaten listede var.", HintTone.Warning);
                return;
            }

            _btnAddKalem.Enabled = false;

            try
            {
                await _kalemTanimiService.CreateAsync(tip, ad);
                _txtNewKalem.Text = string.Empty;
                await LoadKalemlerAsync();
                SetCategoryHint($"{tip} kalemi eklendi: {ad}", HintTone.Success);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Kalem eklenemedi: " + ex.Message,
                    "Ayarlar",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                SetCategoryHint("Kalem eklenemedi.", HintTone.Error);
            }
            finally
            {
                _btnAddKalem.Enabled = true;
            }
        }

        private async Task DeleteSelectedKalemAsync()
        {
            if (_lstKalemler.SelectedItem is not KalemItem item)
            {
                MessageBox.Show(
                    "Lutfen silmek icin bir kalem sec.",
                    "Ayarlar",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                SetCategoryHint("Silmek icin listeden bir kalem sec.", HintTone.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"'{item.Ad}' kalemini silmek istiyor musun?",
                "Kalem Sil",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            _btnDeleteKalem.Enabled = false;

            try
            {
                await _kalemTanimiService.DeleteAsync(item.Id);
                await LoadKalemlerAsync();
                SetCategoryHint($"Kalem silindi: {item.Ad}", HintTone.Success);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Kalem silinemedi: " + ex.Message,
                    "Ayarlar",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                SetCategoryHint("Kalem silinemedi.", HintTone.Error);
            }
            finally
            {
                _btnDeleteKalem.Enabled = true;
            }
        }

        private string GetSelectedTip()
        {
            return _cmbKalemTip.SelectedItem?.ToString() == "Gelir" ? "Gelir" : "Gider";
        }

        private void UpdateDeleteKalemState()
        {
            _btnDeleteKalem.Enabled = _lstKalemler.SelectedItem is KalemItem;
        }

        private bool HasKalemWithName(string name)
        {
            if (_lstKalemler.DataSource is not IEnumerable<KalemItem> items)
                return false;

            return items.Any(x => string.Equals(x.Ad, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
