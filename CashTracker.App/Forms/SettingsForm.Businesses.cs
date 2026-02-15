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
    }
}
