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
            var tr = CultureInfo.GetCultureInfo("tr-TR");
            var list = new List<MonthItem>();
            var now = DateTime.Now;

            for (int i = 0; i < 24; i++)
            {
                var d = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                list.Add(new MonthItem
                {
                    Year = d.Year,
                    Month = d.Month,
                    Display = d.ToString("MMMM yyyy", tr)
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

        private async Task RefreshSummariesAsync()
        {
            await RefreshActiveBusinessInfoAsync();
            var today = DateTime.Today;

            var sDaily = await _summaryService.GetSummaryAsync(today, today);
            var s30 = await _summaryService.GetSummaryAsync(today.AddDays(-29), today);
            var s365 = await _summaryService.GetSummaryAsync(today.AddDays(-364), today);
            var dailyRecords = await _kasaService.GetAllAsync(today, today);

            ApplySummary(_cardDaily, sDaily);
            ApplySummary(_card30, s30);
            ApplySummary(_card365, s365);
            ApplyDailyOverview(sDaily, dailyRecords);

            await RefreshMonthlyAsync();
            await RefreshYearlyAsync();
        }

        private async Task RefreshMonthlyAsync()
        {
            if (_cmbMonth.SelectedItem is not MonthItem item) return;

            var s = await _summaryService.GetMonthlySummaryAsync(item.Year, item.Month);

            _lblMonthIncome.Text = $"Gelir: {s.IncomeTotal:n2}";
            _lblMonthExpense.Text = $"Gider: {s.ExpenseTotal:n2}";
            _lblMonthNet.Text = $"Net: {s.Net:n2}";
        }

        private async Task RefreshYearlyAsync()
        {
            if (_cmbYear.SelectedItem is not YearItem item) return;

            var from = new DateTime(item.Year, 1, 1);
            var to = new DateTime(item.Year, 12, 31);
            var s = await _summaryService.GetSummaryAsync(from, to);

            _lblYearIncome.Text = $"Gelir: {s.IncomeTotal:n2}";
            _lblYearExpense.Text = $"Gider: {s.ExpenseTotal:n2}";
            _lblYearNet.Text = $"Net: {s.Net:n2}";
        }

        private async Task RefreshActiveBusinessInfoAsync()
        {
            try
            {
                var active = await _isletmeService.GetActiveAsync();
                var businessName = string.IsNullOrWhiteSpace(active.Ad)
                    ? "Bilinmiyor"
                    : active.Ad.Trim();

                _lblActiveBusinessReport.Text = $"Raporlar Aktif Isletme: {businessName}";
            }
            catch
            {
                _lblActiveBusinessReport.Text = "Raporlar Aktif Isletme: Bilinmiyor";
            }
        }
    }
}
