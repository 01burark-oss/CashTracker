using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;

namespace CashTracker.App
{
    internal sealed partial class MainForm
    {
        private async Task SendDailySummaryAsync(Button senderButton)
        {
            var today = DateTime.Today;
            var summary = await _summaryService.GetSummaryAsync(today, today);
            await SendSummaryToTelegramAsync(AppLocalization.T("main.telegram.dailyTitle"), today, today, summary, senderButton);
        }

        private async Task SendLast30SummaryAsync(Button senderButton)
        {
            var to = DateTime.Today;
            var from = to.AddDays(-29);
            var summary = await _summaryService.GetSummaryAsync(from, to);
            await SendSummaryToTelegramAsync(AppLocalization.T("main.telegram.last30Title"), from, to, summary, senderButton);
        }

        private async Task SendLast365SummaryAsync(Button senderButton)
        {
            var to = DateTime.Today;
            var from = to.AddDays(-364);
            var summary = await _summaryService.GetSummaryAsync(from, to);
            await SendSummaryToTelegramAsync(AppLocalization.T("main.telegram.last365Title"), from, to, summary, senderButton);
        }

        private async Task SendMonthlySummaryAsync(Button senderButton)
        {
            if (_cmbMonth.SelectedItem is not MonthItem item)
                return;

            var from = new DateTime(item.Year, item.Month, 1);
            var to = from.AddMonths(1).AddDays(-1);
            var summary = await _summaryService.GetMonthlySummaryAsync(item.Year, item.Month);
            await SendSummaryToTelegramAsync(
                AppLocalization.F("main.telegram.monthlyTitle", item.Display),
                from,
                to,
                summary,
                senderButton);
        }

        private async Task SendYearlySummaryAsync(Button senderButton)
        {
            if (_cmbYear.SelectedItem is not YearItem item)
                return;

            var from = new DateTime(item.Year, 1, 1);
            var to = new DateTime(item.Year, 12, 31);
            var summary = await _summaryService.GetSummaryAsync(from, to);
            await SendSummaryToTelegramAsync(
                AppLocalization.F("main.telegram.yearlyTitle", item.Year),
                from,
                to,
                summary,
                senderButton);
        }

        private async Task SendSummaryToTelegramAsync(
            string title,
            DateTime from,
            DateTime to,
            PeriodSummary summary,
            Button senderButton)
        {
            if (!_telegramSettings.IsEnabled)
            {
                MessageBox.Show(AppLocalization.T("main.telegram.notConfigured"));
                return;
            }

            senderButton.Enabled = false;

            try
            {
                var records = await _kasaService.GetAllAsync(from, to);
                var activeBusiness = await _isletmeService.GetActiveAsync();
                var businessName = string.IsNullOrWhiteSpace(activeBusiness.Ad)
                    ? AppLocalization.T("common.unknown")
                    : activeBusiness.Ad.Trim();

                var sb = new StringBuilder();
                sb.AppendLine(title);
                sb.AppendLine(AppLocalization.F("main.telegram.range", from, to));
                sb.AppendLine(AppLocalization.F("main.telegram.business", businessName));
                sb.AppendLine("--------------------------------");
                sb.AppendLine(AppLocalization.F("main.summary.income", summary.IncomeTotal));
                sb.AppendLine(AppLocalization.F("main.summary.expense", summary.ExpenseTotal));
                sb.AppendLine(AppLocalization.F("main.summary.net", summary.Net));
                sb.AppendLine(AppLocalization.F(
                    "main.telegram.tx",
                    summary.IncomeCount + summary.ExpenseCount,
                    summary.IncomeCount,
                    summary.ExpenseCount));
                AppendOdemeYontemiBreakdown(sb, records);
                AppendKalemBreakdown(sb, records, "Gelir");
                AppendKalemBreakdown(sb, records, "Gider");

                await _backupReport.SendTextAsync(sb.ToString().Trim());
                MessageBox.Show(AppLocalization.T("main.telegram.sent"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(AppLocalization.F("main.telegram.sendError", ex.Message));
            }
            finally
            {
                senderButton.Enabled = true;
            }
        }

        private static void AppendKalemBreakdown(StringBuilder sb, IReadOnlyCollection<Kasa> records, string tip)
        {
            var kalemRows = records
                .Where(x => IsTip(x.Tip, tip))
                .GroupBy(GetKalemName, StringComparer.OrdinalIgnoreCase)
                .Select(g => new
                {
                    Kalem = g.Key,
                    Toplam = g.Sum(x => x.Tutar),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Toplam)
                .ThenBy(x => x.Kalem, StringComparer.OrdinalIgnoreCase)
                .ToList();

            sb.AppendLine();
            sb.AppendLine(AppLocalization.F("main.telegram.categoryHeader", AppLocalization.GetTipDisplay(tip)));
            if (kalemRows.Count == 0)
            {
                sb.AppendLine(AppLocalization.T("main.telegram.noRecord"));
                return;
            }

            foreach (var row in kalemRows)
                sb.AppendLine(AppLocalization.F("main.telegram.categoryRow", row.Kalem, row.Toplam, row.Count));
        }

        private static void AppendOdemeYontemiBreakdown(StringBuilder sb, IReadOnlyCollection<Kasa> records)
        {
            var byMethod = records
                .GroupBy(x => NormalizeOdemeYontemi(x.OdemeYontemi), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        Income = g.Where(x => IsTip(x.Tip, "Gelir")).Sum(x => x.Tutar),
                        Expense = g.Where(x => IsTip(x.Tip, "Gider")).Sum(x => x.Tutar)
                    },
                    StringComparer.OrdinalIgnoreCase);

            sb.AppendLine();
            sb.AppendLine(AppLocalization.T("main.telegram.methodsHeader"));
            foreach (var method in new[] { "Nakit", "KrediKarti", "OnlineOdeme", "Havale" })
            {
                var income = byMethod.TryGetValue(method, out var values) ? values.Income : 0m;
                var expense = byMethod.TryGetValue(method, out values) ? values.Expense : 0m;
                sb.AppendLine(AppLocalization.F("main.telegram.methodRow", GetOdemeYontemiLabel(method), income, expense, income - expense));
            }
        }

        private static bool IsTip(string? rawTip, string tip)
        {
            var normalized = (rawTip ?? string.Empty).Trim().ToLowerInvariant();
            if (tip == "Gelir")
                return normalized is "gelir" or "giris" or "giriş" or "income" or "einnahme";

            if (tip == "Gider")
                return normalized is "gider" or "cikis" or "çıkış" or "expense" or "ausgabe";

            return false;
        }

        private static string GetKalemName(Kasa row)
        {
            if (!string.IsNullOrWhiteSpace(row.Kalem))
                return row.Kalem.Trim();

            if (!string.IsNullOrWhiteSpace(row.GiderTuru))
                return row.GiderTuru.Trim();

            return IsTip(row.Tip, "Gider")
                ? AppLocalization.T("main.telegram.defaultExpenseCategory")
                : AppLocalization.T("main.telegram.defaultIncomeCategory");
        }

        private static string GetOdemeYontemiLabel(string method)
        {
            return method switch
            {
                "KrediKarti" => AppLocalization.T("payment.card"),
                "OnlineOdeme" => AppLocalization.T("payment.online"),
                "Havale" => AppLocalization.T("payment.transfer"),
                "Nakit" => AppLocalization.T("payment.cash"),
                _ => method
            };
        }
    }
}

