using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CashTracker.App.Services;

namespace CashTracker.App.Forms
{
    internal sealed partial class SettingsForm
    {
        private async Task LoadAllAsync()
        {
            await LoadBusinessesAsync();
            await LoadKalemlerAsync();
            await LoadLicenseStateAsync();
        }

        private void InitializeLanguageSelector()
        {
            _cmbLanguage.DataSource = null;
            _cmbLanguage.DisplayMember = nameof(LanguageOption.DisplayName);
            _cmbLanguage.ValueMember = nameof(LanguageOption.Code);
            _cmbLanguage.DataSource = AppLocalization.SupportedLanguages.ToList();
            _cmbLanguage.SelectedValue = AppLocalization.CurrentLanguage;
        }

        private void ApplyLanguageSelection()
        {
            if (_cmbLanguage.SelectedItem is not LanguageOption selected)
                return;

            if (string.Equals(selected.Code, AppLocalization.CurrentLanguage, StringComparison.OrdinalIgnoreCase))
            {
                SetBusinessHint(AppLocalization.T("settings.language.same"), HintTone.Neutral);
                return;
            }

            var state = AppStateStore.Load(_runtimeOptions.AppDataPath);
            state.LanguageCode = selected.Code;
            AppStateStore.Save(_runtimeOptions.AppDataPath, state);
            AppLocalization.SetLanguage(selected.Code);
            CultureInfo.DefaultThreadCurrentCulture = AppLocalization.CurrentCulture;
            CultureInfo.DefaultThreadCurrentUICulture = AppLocalization.CurrentCulture;

            var restartNow = MessageBox.Show(
                AppLocalization.T("settings.language.saved"),
                AppLocalization.T("settings.language.savedTitle"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (restartNow == DialogResult.Yes)
            {
                Application.Restart();
                if (Owner is Form owner)
                    owner.Close();
                Close();
                return;
            }

            SetBusinessHint(AppLocalization.T("settings.language.savedHint"), HintTone.Success);
        }

        private async Task LoadBusinessesAsync(int? preferredBusinessId = null)
        {
            _isLoadingBusinesses = true;
            var shouldSync = false;
            SetBusinessHint(AppLocalization.T("settings.hint.businessesLoading"));
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
                        AppLocalization.T("settings.hint.noBusiness"),
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
                    AppLocalization.F("settings.error.businessLoad", ex.Message),
                    AppLocalization.T("settings.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                _cmbBusinesses.Enabled = false;
                _txtRenameBusiness.Enabled = false;
                _btnSetActiveBusiness.Enabled = false;
                _btnRenameBusiness.Enabled = false;
                _btnDeleteBusiness.Enabled = false;
                SetBusinessHint(AppLocalization.T("settings.hint.businessLoadFail"), HintTone.Error);
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
                    ? AppLocalization.T("settings.hint.businessActiveInfo")
                    : AppLocalization.T("settings.hint.businessInactiveInfo"),
                item.IsAktif ? HintTone.Success : HintTone.Neutral);
        }

        private async Task SetActiveBusinessAsync()
        {
            if (_cmbBusinesses.SelectedItem is not IsletmeItem item)
                return;

            if (item.IsAktif)
            {
                SetBusinessHint(AppLocalization.T("settings.hint.businessAlreadyActive"), HintTone.Neutral);
                return;
            }

            _btnSetActiveBusiness.Enabled = false;

            try
            {
                await _isletmeService.SetActiveAsync(item.Id);
                await LoadBusinessesAsync(item.Id);
                await LoadKalemlerAsync();
                SetBusinessHint(AppLocalization.F("settings.hint.businessActivated", item.Ad), HintTone.Success);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    AppLocalization.F("settings.error.businessActivate", ex.Message),
                    AppLocalization.T("settings.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                SetBusinessHint(AppLocalization.T("settings.hint.businessActivateFail"), HintTone.Error);
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
                    AppLocalization.T("settings.error.businessNameRequired"),
                    AppLocalization.T("settings.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                SetBusinessHint(AppLocalization.T("settings.hint.businessNameRequired"), HintTone.Warning);
                return;
            }

            if (newName.Length < 2)
            {
                SetBusinessHint(AppLocalization.T("settings.hint.businessNameMin"), HintTone.Warning);
                return;
            }

            if (string.Equals(item.Ad, newName, StringComparison.OrdinalIgnoreCase))
            {
                SetBusinessHint(AppLocalization.T("settings.hint.businessNameSame"), HintTone.Neutral);
                return;
            }

            if (HasBusinessWithName(newName, exceptBusinessId: item.Id))
            {
                SetBusinessHint(AppLocalization.T("settings.hint.businessNameExists"), HintTone.Warning);
                return;
            }

            _btnRenameBusiness.Enabled = false;

            try
            {
                await _isletmeService.RenameAsync(item.Id, newName);
                await LoadBusinessesAsync(item.Id);
                SetBusinessHint(AppLocalization.T("settings.hint.businessRenamed"), HintTone.Success);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    AppLocalization.F("settings.error.businessRename", ex.Message),
                    AppLocalization.T("settings.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                SetBusinessHint(AppLocalization.T("settings.hint.businessRenameFail"), HintTone.Error);
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
                    AppLocalization.T("settings.error.newBusinessNameRequired"),
                    AppLocalization.T("settings.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                SetBusinessHint(AppLocalization.T("settings.hint.newBusinessNameRequired"), HintTone.Warning);
                return;
            }

            if (newName.Length < 2)
            {
                SetBusinessHint(AppLocalization.T("settings.hint.newBusinessNameMin"), HintTone.Warning);
                return;
            }

            if (HasBusinessWithName(newName))
            {
                SetBusinessHint(AppLocalization.T("settings.hint.newBusinessNameExists"), HintTone.Warning);
                return;
            }

            _btnAddBusiness.Enabled = false;

            try
            {
                var id = await _isletmeService.CreateAsync(newName, makeActive: true);
                _txtNewBusiness.Text = string.Empty;
                await LoadBusinessesAsync(id);
                await LoadKalemlerAsync();
                SetBusinessHint(AppLocalization.F("settings.hint.businessAdded", newName), HintTone.Success);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    AppLocalization.F("settings.error.businessAdd", ex.Message),
                    AppLocalization.T("settings.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                SetBusinessHint(AppLocalization.T("settings.hint.businessAddFail"), HintTone.Error);
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
                SetBusinessHint(AppLocalization.T("settings.hint.minOneBusiness"), HintTone.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                AppLocalization.F("settings.confirm.businessDeleteBody", item.Ad),
                AppLocalization.T("settings.confirm.businessDeleteTitle"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
                return;

            _btnDeleteBusiness.Enabled = false;
            _btnRenameBusiness.Enabled = false;
            _btnSetActiveBusiness.Enabled = false;

            var approved = await RequireTelegramApprovalAsync(
                AppLocalization.T("settings.approval.businessDeleteTitle"),
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
                SetBusinessHint(AppLocalization.F("settings.hint.businessDeleted", item.Ad), HintTone.Success);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    AppLocalization.F("settings.error.businessDelete", ex.Message),
                    AppLocalization.T("settings.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                SetBusinessHint(AppLocalization.T("settings.hint.businessDeleteFail"), HintTone.Error);
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
            var activeText = item.IsAktif
                ? AppLocalization.T("settings.business.activeText")
                : AppLocalization.T("settings.business.passiveText");
            var total = 0;

            if (_cmbBusinesses.DataSource is IEnumerable<IsletmeItem> items)
                total = items.Count();

            var businessLine = AppLocalization.F("settings.business.approvalLine", item.Ad, activeText);
            var countLine = total > 0
                ? AppLocalization.F("settings.business.totalLine", total)
                : string.Empty;

            if (string.IsNullOrWhiteSpace(countLine))
                return businessLine;

            return $"{businessLine}\n{countLine}";
        }

        private async Task<string> GetActiveBusinessNameAsync()
        {
            try
            {
                var active = await _isletmeService.GetActiveAsync();
                return string.IsNullOrWhiteSpace(active.Ad) ? AppLocalization.T("common.unknown") : active.Ad.Trim();
            }
            catch
            {
                return AppLocalization.T("common.unknown");
            }
        }

        private async Task OpenPinChangeAsync()
        {
            using var form = new PinSetupForm(_appSecurityService, isFirstRun: false);
            if (form.ShowDialog(this) == DialogResult.OK)
                SetBusinessHint("PIN guncellendi.", HintTone.Success);

            await Task.CompletedTask;
        }

        private async Task LoadLicenseStateAsync()
        {
            if (_txtInstallCode is not null)
                _txtInstallCode.Text = _licenseService.GetInstallCode();

            var access = await _licenseService.EvaluateAccessAsync();
            var current = await _licenseService.GetCurrentStatusAsync();

            if (_txtLicenseKey is not null && current.IsValid)
                _txtLicenseKey.Text = current.LicenseKey;

            if (_lblLicenseStatus is null)
                return;

            _lblLicenseStatus.Text = access.Mode switch
            {
                LicenseAccessMode.Active => AppLocalization.F("settings.license.status.active", current.Payload?.CustomerName ?? "lisansli kullanici"),
                LicenseAccessMode.LegacyExempt => AppLocalization.T("settings.license.status.legacy"),
                LicenseAccessMode.Blocked => access.Message,
                _ => AppLocalization.F("settings.license.status.trial", access.DaysRemaining)
            };

            _lblLicenseStatus.ForeColor = access.Mode switch
            {
                LicenseAccessMode.Active => ResolveHintColor(HintTone.Success),
                LicenseAccessMode.LegacyExempt => ResolveHintColor(HintTone.Success),
                LicenseAccessMode.Blocked => ResolveHintColor(HintTone.Error),
                _ => ResolveHintColor(HintTone.Warning)
            };
        }

        private async Task ActivateLicenseAsync()
        {
            if (_btnActivateLicense is null || _txtLicenseKey is null || _lblLicenseStatus is null)
                return;

            _btnActivateLicense.Enabled = false;

            try
            {
                var result = await _licenseService.ActivateAsync(_txtLicenseKey.Text);
                _lblLicenseStatus.Text = result.Message;
                _lblLicenseStatus.ForeColor = result.IsValid
                    ? ResolveHintColor(HintTone.Success)
                    : ResolveHintColor(HintTone.Error);

                if (result.IsValid)
                    await LoadLicenseStateAsync();
            }
            finally
            {
                _btnActivateLicense.Enabled = true;
            }
        }

    }
}
