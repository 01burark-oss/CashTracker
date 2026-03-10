using System;
using System.Threading.Tasks;
using CashTracker.Core.Models;

namespace CashTracker.App
{
    internal sealed partial class MainForm
    {
        private void LoadSummaryRangeSelectors()
        {
            _isLoadingSummaryRangeSelectors = true;

            try
            {
                BindSummaryRangeSelector(_cardPrimaryRange.RangeSelector, DateTime.Today);
                BindSummaryRangeSelector(_cardSecondaryRange.RangeSelector, DateTime.Today);

                var state = AppStateStore.Load(_runtimeOptions.AppDataPath);
                SelectSummaryRange(_cardPrimaryRange, state.SummaryPrimaryRange);
                SelectSummaryRange(_cardSecondaryRange, state.SummarySecondaryRange);

                UpdateSummaryCardTitle(_cardPrimaryRange);
                UpdateSummaryCardTitle(_cardSecondaryRange);
            }
            finally
            {
                _isLoadingSummaryRangeSelectors = false;
            }
        }

        private async Task RefreshSummariesAsync()
        {
            var today = DateTime.Today;
            _lastSummaryDate = today;
            RefreshSummaryRangeSelectorDisplays(today);

            var snapshot = await _dashboardSnapshotService.GetSnapshotAsync(
                today,
                GetSelectedSummaryRangeCode(_cardPrimaryRange),
                GetSelectedSummaryRangeCode(_cardSecondaryRange),
                today.Month,
                today.Year,
                today.Year);

            var businessName = string.IsNullOrWhiteSpace(snapshot.ActiveBusinessName)
                ? AppLocalization.T("common.unknown")
                : snapshot.ActiveBusinessName;

            _lblActiveBusinessReport.Text = AppLocalization.F("main.activeBusiness", businessName);
            ApplySummary(_cardDaily, snapshot.DailySummary);
            UpdateSummaryCardTitle(_cardPrimaryRange);
            ApplySummary(_cardPrimaryRange, snapshot.PrimaryRangeSummary);
            UpdateSummaryCardTitle(_cardSecondaryRange);
            ApplySummary(_cardSecondaryRange, snapshot.SecondaryRangeSummary);
            ApplyDailyOverview(snapshot.DailySummary, snapshot.DailyPaymentMethodBreakdowns);
        }

        private async Task RefreshActiveBusinessInfoAsync()
        {
            try
            {
                var active = await _isletmeService.GetActiveAsync();
                var businessName = string.IsNullOrWhiteSpace(active.Ad)
                    ? AppLocalization.T("common.unknown")
                    : active.Ad.Trim();

                _lblActiveBusinessReport.Text = AppLocalization.F("main.activeBusiness", businessName);
            }
            catch
            {
                _lblActiveBusinessReport.Text = AppLocalization.F("main.activeBusiness", AppLocalization.T("common.unknown"));
            }
        }

        private void BindSummaryRangeSelector(ComboBox? selector, DateTime referenceDate)
        {
            if (selector is null)
                return;

            selector.DataSource = SummaryRangeCatalog.CreateLocalizedOptions(referenceDate);
            selector.DisplayMember = "Display";
            selector.ValueMember = "Code";
        }

        private void RefreshSummaryRangeSelectorDisplays(DateTime referenceDate)
        {
            _isLoadingSummaryRangeSelectors = true;

            try
            {
                RefreshSummaryRangeSelectorDisplay(_cardPrimaryRange, referenceDate);
                RefreshSummaryRangeSelectorDisplay(_cardSecondaryRange, referenceDate);
            }
            finally
            {
                _isLoadingSummaryRangeSelectors = false;
            }
        }

        private void RefreshSummaryRangeSelectorDisplay(SummaryCard card, DateTime referenceDate)
        {
            var selector = card.RangeSelector;
            if (selector is null)
                return;

            var selectedCode = GetSelectedSummaryRangeCode(card);
            BindSummaryRangeSelector(selector, referenceDate);
            selector.SelectedValue = selectedCode;
        }

        private void SelectSummaryRange(SummaryCard card, string? code)
        {
            var selector = card.RangeSelector;
            if (selector is null)
                return;

            var normalized = SummaryRangeCatalog.NormalizeCode(code, card.DefaultRangeCode);
            selector.SelectedValue = normalized;
        }

        private string GetSelectedSummaryRangeCode(SummaryCard card)
        {
            var selected = card.RangeSelector?.SelectedValue as string;
            return SummaryRangeCatalog.NormalizeCode(selected, card.DefaultRangeCode);
        }

        private void UpdateSummaryCardTitle(SummaryCard card)
        {
            if (card.Title is not null)
                card.Title.Text = SummaryRangeCatalog.GetDisplay(GetSelectedSummaryRangeCode(card), DateTime.Today);
        }

        private async Task RefreshSummaryRangeCardAsync(SummaryCard card, DateTime today)
        {
            var rangeCode = GetSelectedSummaryRangeCode(card);
            var (from, to) = SummaryRangeCatalog.GetRange(rangeCode, today);
            var summary = await _summaryService.GetSummaryAsync(from, to);
            UpdateSummaryCardTitle(card);
            ApplySummary(card, summary);
        }

        private void SaveSummaryRangeSelections()
        {
            var state = AppStateStore.Load(_runtimeOptions.AppDataPath);
            state.SummaryPrimaryRange = GetSelectedSummaryRangeCode(_cardPrimaryRange);
            state.SummarySecondaryRange = GetSelectedSummaryRangeCode(_cardSecondaryRange);
            AppStateStore.Save(_runtimeOptions.AppDataPath, state);
        }

        private async Task HandleSummaryRangeChangedAsync(SummaryCard card)
        {
            if (_isLoadingSummaryRangeSelectors)
                return;

            SaveSummaryRangeSelections();
            UpdateSummaryCardTitle(card);
            await RefreshSummaryRangeCardAsync(card, DateTime.Today);
        }

        private async Task RefreshSummariesIfDateChangedAsync()
        {
            if (!_isAuthenticated)
                return;

            var today = DateTime.Today;
            if (today == _lastSummaryDate)
                return;

            await RefreshSummariesAsync();
        }
    }
}
