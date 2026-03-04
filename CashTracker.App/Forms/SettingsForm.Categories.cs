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
            SetCategoryHint(AppLocalization.T("settings.hint.categoriesLoading"));

            try
            {
                var tip = GetSelectedTip();
                var tipDisplay = AppLocalization.GetTipDisplay(tip);
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
                        AppLocalization.F("settings.hint.noCategoryForType", tipDisplay),
                        HintTone.Warning);
                }
                else
                {
                    SetCategoryHint(AppLocalization.F("settings.hint.categoryCountForType", tipDisplay, items.Count), HintTone.Success);
                }

                SyncSelectedKalemToEditor();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    AppLocalization.F("settings.error.categoryLoad", ex.Message),
                    AppLocalization.T("settings.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                SetCategoryHint(AppLocalization.T("settings.hint.categoryLoadFail"), HintTone.Error);
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
            var tipDisplay = AppLocalization.GetTipDisplay(tip);
            var ad = NormalizeText(_txtNewKalem.Text);
            if (string.IsNullOrWhiteSpace(ad))
            {
                MessageBox.Show(
                    AppLocalization.T("settings.error.categoryNameRequired"),
                    AppLocalization.T("settings.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                SetCategoryHint(AppLocalization.T("settings.hint.categoryNameRequired"), HintTone.Warning);
                return;
            }

            if (ad.Length < 2)
            {
                SetCategoryHint(AppLocalization.T("settings.hint.categoryNameMin"), HintTone.Warning);
                return;
            }

            if (HasKalemWithName(ad))
            {
                SetCategoryHint(AppLocalization.T("settings.hint.categoryNameExists"), HintTone.Warning);
                return;
            }

            _btnAddKalem.Enabled = false;

            try
            {
                var id = await _kalemTanimiService.CreateAsync(tip, ad);
                _txtNewKalem.Text = string.Empty;
                await LoadKalemlerAsync(id);
                SetCategoryHint(AppLocalization.F("settings.hint.categoryAdded", tipDisplay, ad), HintTone.Success);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    AppLocalization.F("settings.error.categoryAdd", ex.Message),
                    AppLocalization.T("settings.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                SetCategoryHint(AppLocalization.T("settings.hint.categoryAddFail"), HintTone.Error);
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
                SetCategoryHint(AppLocalization.T("settings.hint.selectCategoryToEdit"), HintTone.Warning);
                return;
            }

            var yeniAd = NormalizeText(_txtEditKalem.Text);
            if (string.IsNullOrWhiteSpace(yeniAd))
            {
                SetCategoryHint(AppLocalization.T("settings.hint.categoryNameRequired"), HintTone.Warning);
                return;
            }

            if (yeniAd.Length < 2)
            {
                SetCategoryHint(AppLocalization.T("settings.hint.categoryNameMin"), HintTone.Warning);
                return;
            }

            if (string.Equals(item.Ad, yeniAd, StringComparison.OrdinalIgnoreCase))
            {
                SetCategoryHint(AppLocalization.T("settings.hint.categoryNameSame"), HintTone.Neutral);
                return;
            }

            if (HasKalemWithName(yeniAd, exceptKalemId: item.Id))
            {
                SetCategoryHint(AppLocalization.T("settings.hint.categoryNameExists"), HintTone.Warning);
                return;
            }

            _btnUpdateKalem.Enabled = false;

            try
            {
                await _kalemTanimiService.UpdateAsync(item.Id, yeniAd);
                await LoadKalemlerAsync(item.Id);
                SetCategoryHint(AppLocalization.F("settings.hint.categoryUpdated", item.Ad, yeniAd), HintTone.Success);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    AppLocalization.F("settings.error.categoryUpdate", ex.Message),
                    AppLocalization.T("settings.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                SetCategoryHint(AppLocalization.T("settings.hint.categoryUpdateFail"), HintTone.Error);
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
                    AppLocalization.T("settings.error.selectCategoryToDelete"),
                    AppLocalization.T("settings.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                SetCategoryHint(AppLocalization.T("settings.hint.selectCategoryToDelete"), HintTone.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                AppLocalization.F("settings.confirm.categoryDeleteBody", item.Ad),
                AppLocalization.T("settings.confirm.categoryDeleteTitle"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            _btnDeleteKalem.Enabled = false;
            _btnUpdateKalem.Enabled = false;

            var approved = await RequireTelegramApprovalAsync(
                AppLocalization.T("settings.approval.categoryDeleteTitle"),
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
                SetCategoryHint(AppLocalization.F("settings.hint.categoryDeleted", item.Ad), HintTone.Success);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    AppLocalization.F("settings.error.categoryDelete", ex.Message),
                    AppLocalization.T("settings.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                SetCategoryHint(AppLocalization.T("settings.hint.categoryDeleteFail"), HintTone.Error);
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
            return AppLocalization.F("settings.approval.categoryDetails", businessName, AppLocalization.GetTipDisplay(tip), item.Ad);
        }

        private string GetSelectedTip()
        {
            return AppLocalization.NormalizeTip(_cmbKalemTip.SelectedItem?.ToString()) == "Gider"
                ? "Gider"
                : "Gelir";
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
