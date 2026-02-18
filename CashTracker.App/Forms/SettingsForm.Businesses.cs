using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CashTracker.App.Forms
{
    public sealed partial class SettingsForm
    {
        private async Task LoadAllAsync()
        {
            await LoadBusinessesAsync();
            await LoadKalemlerAsync();
        }

        private async Task LoadBusinessesAsync(int? preferredBusinessId = null)
        {
            _isLoadingBusinesses = true;
            var shouldSync = false;
            SetBusinessHint("Isletmeler yukleniyor...");
            _cmbBusinesses.Enabled = false;
            _btnSetActiveBusiness.Enabled = false;
            _btnRenameBusiness.Enabled = false;
            _btnDeleteBusiness.Enabled = false;
            _btnAddBusiness.Enabled = false;

            try
            {
                var rows = await _isletmeService.GetAllAsync();
                var items = rows
                    .Select(x => new IsletmeItem
                    {
                        Id = x.Id,
                        Ad = x.Ad,
                        IsAktif = x.IsAktif
                    })
                    .ToList();

                _cmbBusinesses.DataSource = items;
                _cmbBusinesses.DisplayMember = nameof(IsletmeItem.Display);
                _cmbBusinesses.ValueMember = nameof(IsletmeItem.Id);

                if (items.Count == 0)
                {
                    _txtRenameBusiness.Text = string.Empty;
                    _cmbBusinesses.Enabled = false;
                    _txtRenameBusiness.Enabled = false;
                    _btnSetActiveBusiness.Enabled = false;
                    _btnRenameBusiness.Enabled = false;
                    _btnDeleteBusiness.Enabled = false;
                    _btnAddBusiness.Enabled = true;
                    SetBusinessHint(
                        "Isletme bulunamadi. Yeni isletme ekleyerek baslayabilirsin.",
                        HintTone.Warning);
                    return;
                }

                _cmbBusinesses.Enabled = true;
                _txtRenameBusiness.Enabled = true;
                _btnAddBusiness.Enabled = true;

                var selectedId = preferredBusinessId ?? items.FirstOrDefault(x => x.IsAktif)?.Id ?? items[0].Id;
                _cmbBusinesses.SelectedValue = selectedId;
                shouldSync = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Isletme listesi yuklenirken hata olustu: " + ex.Message,
                    "Ayarlar",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                _cmbBusinesses.Enabled = false;
                _txtRenameBusiness.Enabled = false;
                _btnSetActiveBusiness.Enabled = false;
                _btnRenameBusiness.Enabled = false;
                _btnDeleteBusiness.Enabled = false;
                SetBusinessHint("Isletme listesi yuklenemedi.", HintTone.Error);
            }
            finally
            {
                _isLoadingBusinesses = false;
                _btnAddBusiness.Enabled = true;
            }

            if (shouldSync)
                SyncSelectedBusinessToEditor();
        }

        private void SyncSelectedBusinessToEditor()
        {
            if (_isLoadingBusinesses)
                return;

            if (_cmbBusinesses.SelectedItem is not IsletmeItem item)
                return;

            _txtRenameBusiness.Text = item.Ad;
            _btnSetActiveBusiness.Enabled = !item.IsAktif;
            _btnRenameBusiness.Enabled = true;
            _btnDeleteBusiness.Enabled = CanDeleteBusiness();
            SetBusinessHint(
                item.IsAktif
                    ? "Bu isletme aktif. Ozetler ve kayitlar bu isletmeye gore listelenir."
                    : "Bu isletmeyi aktif yaparsan tum listeler secili isletmeye gore filtrelenir.",
                item.IsAktif ? HintTone.Success : HintTone.Neutral);
        }

        private async Task SetActiveBusinessAsync()
        {
            if (_cmbBusinesses.SelectedItem is not IsletmeItem item)
                return;

            if (item.IsAktif)
            {
                SetBusinessHint("Secili isletme zaten aktif.", HintTone.Neutral);
                return;
            }

            _btnSetActiveBusiness.Enabled = false;

            try
            {
                await _isletmeService.SetActiveAsync(item.Id);
                await LoadBusinessesAsync(item.Id);
                await LoadKalemlerAsync();
                SetBusinessHint($"Aktif isletme degistirildi: {item.Ad}", HintTone.Success);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Aktif isletme degistirilemedi: " + ex.Message,
                    "Ayarlar",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                SetBusinessHint("Aktif isletme degistirilemedi.", HintTone.Error);
            }
            finally
            {
                _btnSetActiveBusiness.Enabled = true;
            }
        }

        private async Task RenameSelectedBusinessAsync()
        {
            if (_cmbBusinesses.SelectedItem is not IsletmeItem item)
                return;

            var newName = NormalizeText(_txtRenameBusiness.Text);
            if (string.IsNullOrWhiteSpace(newName))
            {
                MessageBox.Show(
                    "Isletme adi bos birakilamaz.",
                    "Ayarlar",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                SetBusinessHint("Isletme adi bos birakilamaz.", HintTone.Warning);
                return;
            }

            if (newName.Length < 2)
            {
                SetBusinessHint("Isletme adi en az 2 karakter olmalidir.", HintTone.Warning);
                return;
            }

            if (string.Equals(item.Ad, newName, StringComparison.OrdinalIgnoreCase))
            {
                SetBusinessHint("Yeni ad mevcut ad ile ayni.", HintTone.Neutral);
                return;
            }

            if (HasBusinessWithName(newName, exceptBusinessId: item.Id))
            {
                SetBusinessHint("Bu isimde bir isletme zaten var.", HintTone.Warning);
                return;
            }

            _btnRenameBusiness.Enabled = false;

            try
            {
                await _isletmeService.RenameAsync(item.Id, newName);
                await LoadBusinessesAsync(item.Id);
                SetBusinessHint("Isletme adi guncellendi.", HintTone.Success);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Isletme adi guncellenemedi: " + ex.Message,
                    "Ayarlar",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                SetBusinessHint("Isletme adi guncellenemedi.", HintTone.Error);
            }
            finally
            {
                _btnRenameBusiness.Enabled = true;
            }
        }

        private async Task AddBusinessAsync()
        {
            var newName = NormalizeText(_txtNewBusiness.Text);
            if (string.IsNullOrWhiteSpace(newName))
            {
                MessageBox.Show(
                    "Yeni isletme adi bos birakilamaz.",
                    "Ayarlar",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                SetBusinessHint("Yeni isletme adi bos birakilamaz.", HintTone.Warning);
                return;
            }

            if (newName.Length < 2)
            {
                SetBusinessHint("Yeni isletme adi en az 2 karakter olmalidir.", HintTone.Warning);
                return;
            }

            if (HasBusinessWithName(newName))
            {
                SetBusinessHint("Bu isimde bir isletme zaten var.", HintTone.Warning);
                return;
            }

            _btnAddBusiness.Enabled = false;

            try
            {
                var id = await _isletmeService.CreateAsync(newName, makeActive: true);
                _txtNewBusiness.Text = string.Empty;
                await LoadBusinessesAsync(id);
                await LoadKalemlerAsync();
                SetBusinessHint($"Yeni isletme eklendi ve aktif yapildi: {newName}", HintTone.Success);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Yeni isletme eklenemedi: " + ex.Message,
                    "Ayarlar",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                SetBusinessHint("Yeni isletme eklenemedi.", HintTone.Error);
            }
            finally
            {
                _btnAddBusiness.Enabled = true;
            }
        }

        private bool HasBusinessWithName(string name, int? exceptBusinessId = null)
        {
            if (_cmbBusinesses.DataSource is not IEnumerable<IsletmeItem> items)
                return false;

            return items.Any(x =>
                (!exceptBusinessId.HasValue || x.Id != exceptBusinessId.Value) &&
                string.Equals(x.Ad, name, StringComparison.OrdinalIgnoreCase));
        }

        private bool CanDeleteBusiness()
        {
            if (_isLoadingBusinesses)
                return false;

            if (_cmbBusinesses.SelectedItem is not IsletmeItem)
                return false;

            if (_cmbBusinesses.DataSource is not IEnumerable<IsletmeItem> items)
                return false;

            return items.Count() > 1;
        }

        private async Task DeleteSelectedBusinessAsync()
        {
            if (_cmbBusinesses.SelectedItem is not IsletmeItem item)
                return;

            if (!CanDeleteBusiness())
            {
                SetBusinessHint("En az bir isletme kalmali. Bu isletme silinemez.", HintTone.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"'{item.Ad}' isletmesini silmek istiyor musun?",
                "Isletme Sil",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
                return;

            _btnDeleteBusiness.Enabled = false;
            _btnRenameBusiness.Enabled = false;
            _btnSetActiveBusiness.Enabled = false;

            var approved = await RequireTelegramApprovalAsync(
                "Isletme silme",
                BuildBusinessApprovalDetails(item),
                SetBusinessHint);

            if (!approved)
            {
                UpdateBusinessActionStates();
                return;
            }

            try
            {
                await _isletmeService.DeleteAsync(item.Id);
                await LoadBusinessesAsync();
                await LoadKalemlerAsync();
                SetBusinessHint($"Isletme silindi: {item.Ad}", HintTone.Success);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Isletme silinemedi: " + ex.Message,
                    "Ayarlar",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                SetBusinessHint("Isletme silinemedi.", HintTone.Error);
            }
            finally
            {
                UpdateBusinessActionStates();
            }
        }

        private void UpdateBusinessActionStates()
        {
            if (_cmbBusinesses.SelectedItem is not IsletmeItem item)
            {
                _btnSetActiveBusiness.Enabled = false;
                _btnRenameBusiness.Enabled = false;
                _btnDeleteBusiness.Enabled = false;
                return;
            }

            _btnSetActiveBusiness.Enabled = !item.IsAktif;
            _btnRenameBusiness.Enabled = true;
            _btnDeleteBusiness.Enabled = CanDeleteBusiness();
        }

        private string BuildBusinessApprovalDetails(IsletmeItem item)
        {
            var activeText = item.IsAktif ? "Aktif" : "Pasif";
            var total = 0;

            if (_cmbBusinesses.DataSource is IEnumerable<IsletmeItem> items)
                total = items.Count();

            var businessLine = $"Isletme: {item.Ad} ({activeText})";
            var countLine = total > 0 ? $"Toplam isletme: {total}" : string.Empty;

            if (string.IsNullOrWhiteSpace(countLine))
                return businessLine;

            return $"{businessLine}\n{countLine}";
        }

        private async Task<string> GetActiveBusinessNameAsync()
        {
            try
            {
                var active = await _isletmeService.GetActiveAsync();
                return string.IsNullOrWhiteSpace(active.Ad) ? "Bilinmiyor" : active.Ad.Trim();
            }
            catch
            {
                return "Bilinmiyor";
            }
        }
    }
}
