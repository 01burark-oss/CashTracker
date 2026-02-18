using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CashTracker.App.Forms
{
    public sealed partial class SettingsForm
    {
        private async Task LoadKalemlerAsync(int? preferredKalemId = null)
        {
            if (_isLoadingKalemler)
                return;

            _isLoadingKalemler = true;
            _btnDeleteKalem.Enabled = false;
            _btnUpdateKalem.Enabled = false;
            _txtEditKalem.Enabled = false;
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

                if (items.Count > 0)
                {
                    var selectedIndex = preferredKalemId.HasValue
                        ? items.FindIndex(x => x.Id == preferredKalemId.Value)
                        : -1;
                    _lstKalemler.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
                }
                else
                {
                    _txtEditKalem.Text = string.Empty;
                }

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

                SyncSelectedKalemToEditor();
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
                UpdateKalemActionStates();
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
                var id = await _kalemTanimiService.CreateAsync(tip, ad);
                _txtNewKalem.Text = string.Empty;
                await LoadKalemlerAsync(id);
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

        private async Task UpdateSelectedKalemAsync()
        {
            if (_lstKalemler.SelectedItem is not KalemItem item)
            {
                SetCategoryHint("Duzenlemek icin listeden bir kalem sec.", HintTone.Warning);
                return;
            }

            var yeniAd = NormalizeText(_txtEditKalem.Text);
            if (string.IsNullOrWhiteSpace(yeniAd))
            {
                SetCategoryHint("Kalem adi bos birakilamaz.", HintTone.Warning);
                return;
            }

            if (yeniAd.Length < 2)
            {
                SetCategoryHint("Kalem adi en az 2 karakter olmalidir.", HintTone.Warning);
                return;
            }

            if (string.Equals(item.Ad, yeniAd, StringComparison.OrdinalIgnoreCase))
            {
                SetCategoryHint("Yeni ad mevcut ad ile ayni.", HintTone.Neutral);
                return;
            }

            if (HasKalemWithName(yeniAd, exceptKalemId: item.Id))
            {
                SetCategoryHint("Bu kalem zaten listede var.", HintTone.Warning);
                return;
            }

            _btnUpdateKalem.Enabled = false;

            try
            {
                await _kalemTanimiService.UpdateAsync(item.Id, yeniAd);
                await LoadKalemlerAsync(item.Id);
                SetCategoryHint($"Kalem guncellendi: {item.Ad} -> {yeniAd}", HintTone.Success);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Kalem guncellenemedi: " + ex.Message,
                    "Ayarlar",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                SetCategoryHint("Kalem guncellenemedi.", HintTone.Error);
            }
            finally
            {
                UpdateKalemActionStates();
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
            _btnUpdateKalem.Enabled = false;

            var approved = await RequireTelegramApprovalAsync(
                "Kalem silme",
                await BuildKalemApprovalDetailsAsync(item),
                SetCategoryHint);

            if (!approved)
            {
                UpdateKalemActionStates();
                return;
            }

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
                UpdateKalemActionStates();
            }
        }

        private async Task<string> BuildKalemApprovalDetailsAsync(KalemItem item)
        {
            var tip = GetSelectedTip();
            var businessName = await GetActiveBusinessNameAsync();
            return $"Isletme: {businessName}\nTip: {tip}\nKalem: {item.Ad}";
        }

        private string GetSelectedTip()
        {
            return _cmbKalemTip.SelectedItem?.ToString() == "Gelir" ? "Gelir" : "Gider";
        }

        private void SyncSelectedKalemToEditor()
        {
            if (_lstKalemler.SelectedItem is KalemItem item)
            {
                _txtEditKalem.Text = item.Ad;
                _txtEditKalem.SelectionStart = _txtEditKalem.Text.Length;
                _txtEditKalem.SelectionLength = 0;
            }
            else
            {
                _txtEditKalem.Text = string.Empty;
            }

            UpdateKalemActionStates();
        }

        private void UpdateKalemActionStates()
        {
            var selected = _lstKalemler.SelectedItem as KalemItem;
            var hasSelection = selected is not null;
            var hasName = !string.IsNullOrWhiteSpace(NormalizeText(_txtEditKalem.Text));
            var hasChanged = hasSelection && !string.Equals(
                selected!.Ad,
                NormalizeText(_txtEditKalem.Text),
                StringComparison.OrdinalIgnoreCase);

            _btnDeleteKalem.Enabled = !_isLoadingKalemler && hasSelection;
            _txtEditKalem.Enabled = !_isLoadingKalemler && hasSelection;
            _btnUpdateKalem.Enabled = !_isLoadingKalemler && hasSelection && hasName && hasChanged;
        }

        private bool HasKalemWithName(string name, int? exceptKalemId = null)
        {
            if (_lstKalemler.DataSource is not IEnumerable<KalemItem> items)
                return false;

            return items.Any(x =>
                (!exceptKalemId.HasValue || x.Id != exceptKalemId.Value) &&
                string.Equals(x.Ad, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
