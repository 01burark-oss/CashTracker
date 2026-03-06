using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using CashTracker.Core.Models;

namespace CashTracker.App
{
    internal sealed partial class MainForm
    {
        private void LoadMonths()
        {
            var list = new List<MonthItem>();
            var now = DateTime.Now;
            var culture = AppLocalization.CurrentCulture;

            for (int i = 0; i < 24; i++)
            {
                var d = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                list.Add(new MonthItem
                {
                    Year = d.Year,
                    Month = d.Month,
                    Display = d.ToString("MMMM yyyy", culture)
                });
            }

            _cmbMonth.DataSource = list;
            _cmbMonth.DisplayMember = "Display";
            _cmbMonth.ValueMember = "Month";
            _cmbMonth.SelectedIndex = 0;
        }

        private void LoadYears()
        {
            var now = DateTime.Now;
            var list = new List<YearItem>();

            for (int year = now.Year; year >= now.Year - 10; year--)
            {
                list.Add(new YearItem
                {
                    Year = year,
                    Display = year.ToString(CultureInfo.InvariantCulture)
                });
            }

            _cmbYear.DataSource = list;
            _cmbYear.DisplayMember = "Display";
            _cmbYear.ValueMember = "Year";
            _cmbYear.SelectedIndex = 0;
        }

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
            await RefreshActiveBusinessInfoAsync();
            var today = DateTime.Today;
            _lastSummaryDate = today;
            RefreshSummaryRangeSelectorDisplays(today);

            var sDaily = await _summaryService.GetSummaryAsync(today, today);
            var dailyRecords = await _kasaService.GetAllAsync(today, today);

            ApplySummary(_cardDaily, sDaily);
            await RefreshSummaryRangeCardAsync(_cardPrimaryRange, today);
            await RefreshSummaryRangeCardAsync(_cardSecondaryRange, today);
            ApplyDailyOverview(sDaily, dailyRecords);

            await RefreshMonthlyAsync();
            await RefreshYearlyAsync();
        }

        private async Task RefreshMonthlyAsync()
        {
            if (_cmbMonth.SelectedItem is not MonthItem item) return;

            var s = await _summaryService.GetMonthlySummaryAsync(item.Year, item.Month);

            _lblMonthIncome.Text = AppLocalization.F("main.summary.income", s.IncomeTotal);
            _lblMonthExpense.Text = AppLocalization.F("main.summary.expense", s.ExpenseTotal);
            _lblMonthNet.Text = AppLocalization.F("main.summary.net", s.Net);
        }

        private async Task RefreshYearlyAsync()
        {
            if (_cmbYear.SelectedItem is not YearItem item) return;

            var from = new DateTime(item.Year, 1, 1);
            var to = new DateTime(item.Year, 12, 31);
            var s = await _summaryService.GetSummaryAsync(from, to);

            _lblYearIncome.Text = AppLocalization.F("main.summary.income", s.IncomeTotal);
            _lblYearExpense.Text = AppLocalization.F("main.summary.expense", s.ExpenseTotal);
            _lblYearNet.Text = AppLocalization.F("main.summary.net", s.Net);
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
