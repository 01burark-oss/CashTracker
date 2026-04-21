using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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
            UpdateTopClockDisplay();
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
            if (_btnBusinessSelector is not null)
                _btnBusinessSelector.Text = FormatBusinessSelectorText(businessName);

            var yesterday = today.AddDays(-1);
            var yesterdaySummary = await _summaryService.GetSummaryAsync(yesterday, yesterday);
            var recentRows = await _kasaService.GetAllAsync(today.AddDays(-11), today);
            var recentNetSeries = BuildRecentNetSeries(recentRows, today, 12);

            _lblSnapshotIncomeValue.Text = FormatAmount(snapshot.DailySummary.IncomeTotal);
            _lblSnapshotNetValue.Text = FormatAmount(snapshot.DailySummary.Net);
            _lblSnapshotExpenseValue.Text = FormatAmount(snapshot.DailySummary.ExpenseTotal);
            _lblSnapshotNetValue.ForeColor = snapshot.DailySummary.Net < 0
                ? Color.FromArgb(177, 40, 40)
                : Color.FromArgb(84, 89, 96);
            ApplyDeltaBadge(_lblSnapshotIncomeDelta, snapshot.DailySummary.IncomeTotal, yesterdaySummary.IncomeTotal, true);
            ApplyDeltaBadge(_lblSnapshotExpenseDelta, snapshot.DailySummary.ExpenseTotal, yesterdaySummary.ExpenseTotal, false);
            _netSparkChart.Values = recentNetSeries;
            _paymentDistributionChart.Slices = BuildPaymentSlices(snapshot.DailyPaymentMethodBreakdowns);

            ApplySummary(_cardDaily, snapshot.DailySummary);
            UpdateSummaryCardTitle(_cardPrimaryRange);
            ApplySummary(_cardPrimaryRange, snapshot.PrimaryRangeSummary);
            UpdateSummaryCardTitle(_cardSecondaryRange);
            ApplySummary(_cardSecondaryRange, snapshot.SecondaryRangeSummary);
            if (_cardDaily.Title is not null)
                _cardDaily.Title.Text = "Bugun";
            if (_cardPrimaryRange.Title is not null)
                _cardPrimaryRange.Title.Text = AppLocalization.T("main.summary.range.last30Days");
            if (_cardSecondaryRange.Title is not null)
                _cardSecondaryRange.Title.Text = AppLocalization.T("main.summary.range.last1Year");
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
                if (_btnBusinessSelector is not null)
                    _btnBusinessSelector.Text = FormatBusinessSelectorText(businessName);
            }
            catch
            {
                _lblActiveBusinessReport.Text = AppLocalization.F("main.activeBusiness", AppLocalization.T("common.unknown"));
                if (_btnBusinessSelector is not null)
                    _btnBusinessSelector.Text = FormatBusinessSelectorText(null);
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
            UpdateTopClockDisplay();

            if (!_isAuthenticated)
                return;

            var today = DateTime.Today;
            if (today == _lastSummaryDate)
                return;

            await RefreshSummariesAsync();
        }

        private void UpdateTopClockDisplay()
        {
            if (_lblTopDate is null || _lblTopTime is null)
                return;

            var now = DateTime.Now;
            _lblTopDate.Text = now.ToString("dd.MM.yyyy");
            _lblTopTime.Text = now.ToString("HH:mm");
        }

        private static IReadOnlyList<decimal> BuildRecentNetSeries(IReadOnlyCollection<CashTracker.Core.Entities.Kasa> rows, DateTime today, int dayCount)
        {
            var values = new List<decimal>(dayCount);
            for (var offset = dayCount - 1; offset >= 0; offset--)
            {
                var date = today.AddDays(-offset).Date;
                var income = rows
                    .Where(x => x.Tarih.Date == date && IsIncomeTip(x.Tip))
                    .Sum(x => x.Tutar);
                var expense = rows
                    .Where(x => x.Tarih.Date == date && IsExpenseTip(x.Tip))
                    .Sum(x => x.Tutar);
                values.Add(income - expense);
            }

            return values;
        }

        private static IReadOnlyList<Controls.DashboardDonutSlice> BuildPaymentSlices(IReadOnlyCollection<DailyPaymentMethodBreakdown> breakdowns)
        {
            decimal GetAmount(string method)
            {
                var row = breakdowns.FirstOrDefault(x => string.Equals(x.Method, method, StringComparison.OrdinalIgnoreCase));
                return row is null ? 0m : Math.Abs(row.IncomeTotal) + Math.Abs(row.ExpenseTotal);
            }

            return new[]
            {
                new Controls.DashboardDonutSlice { Label = "Nakit", Amount = GetAmount("Nakit"), Color = Color.FromArgb(27, 40, 74) },
                new Controls.DashboardDonutSlice { Label = "Kredi Karti", Amount = GetAmount("KrediKarti"), Color = Color.FromArgb(46, 95, 153) },
                new Controls.DashboardDonutSlice { Label = "Online", Amount = GetAmount("OnlineOdeme"), Color = Color.FromArgb(110, 157, 206) },
                new Controls.DashboardDonutSlice { Label = "Havale", Amount = GetAmount("Havale"), Color = Color.FromArgb(189, 204, 224) }
            };
        }

        private static void ApplyDeltaBadge(Label label, decimal current, decimal previous, bool positiveIsGood)
        {
            if (label is null)
                return;

            decimal delta;
            if (previous == 0m)
                delta = current == 0m ? 0m : 100m;
            else
                delta = Math.Round(((current - previous) / previous) * 100m, 0);

            var isPositive = delta >= 0m;
            var goodState = positiveIsGood ? isPositive : !isPositive;
            label.Text = $"{(isPositive ? "+" : string.Empty)}{delta:0}%";
            label.ForeColor = goodState ? Color.FromArgb(23, 134, 92) : Color.FromArgb(177, 40, 40);
            label.BackColor = goodState ? Color.FromArgb(233, 244, 237) : Color.FromArgb(252, 237, 237);
        }
    }
}
