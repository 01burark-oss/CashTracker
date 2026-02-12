using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CashTracker.Core.Models;

namespace CashTracker.App
{
    internal sealed partial class MainForm
    {
        private async Task SendDailySummaryAsync(Button senderButton)
        {
            var today = DateTime.Today;
            var summary = await _summaryService.GetSummaryAsync(today, today);
            await SendSummaryToTelegramAsync("Günlük Özet", today, today, summary, senderButton);
        }

        private async Task SendLast30SummaryAsync(Button senderButton)
        {
            var to = DateTime.Today;
            var from = to.AddDays(-29);
            var summary = await _summaryService.GetSummaryAsync(from, to);
            await SendSummaryToTelegramAsync("Son 30 Gün Özet", from, to, summary, senderButton);
        }

        private async Task SendLast365SummaryAsync(Button senderButton)
        {
            var to = DateTime.Today;
            var from = to.AddDays(-364);
            var summary = await _summaryService.GetSummaryAsync(from, to);
            await SendSummaryToTelegramAsync("Son 365 Gün Özet", from, to, summary, senderButton);
        }

        private async Task SendMonthlySummaryAsync(Button senderButton)
        {
            if (_cmbMonth.SelectedItem is not MonthItem item)
                return;

            var from = new DateTime(item.Year, item.Month, 1);
            var to = from.AddMonths(1).AddDays(-1);
            var summary = await _summaryService.GetMonthlySummaryAsync(item.Year, item.Month);
            await SendSummaryToTelegramAsync($"Aylık Özet ({item.Display})", from, to, summary, senderButton);
        }

        private async Task SendYearlySummaryAsync(Button senderButton)
        {
            if (_cmbYear.SelectedItem is not YearItem item)
                return;

            var from = new DateTime(item.Year, 1, 1);
            var to = new DateTime(item.Year, 12, 31);
            var summary = await _summaryService.GetSummaryAsync(from, to);
            await SendSummaryToTelegramAsync($"Yıllık Özet ({item.Year})", from, to, summary, senderButton);
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
                MessageBox.Show("Telegram ayarları eksik. Sol menüden \"Botu Değiştir\" adımını kullan.");
                return;
            }

            senderButton.Enabled = false;

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine(title);
                sb.AppendLine($"Aralık: {from:yyyy-MM-dd} - {to:yyyy-MM-dd}");
                sb.AppendLine("--------------------------------");
                sb.AppendLine($"Gelir: {summary.IncomeTotal:n2}");
                sb.AppendLine($"Gider: {summary.ExpenseTotal:n2}");
                sb.AppendLine($"Net: {summary.Net:n2}");
                sb.AppendLine($"İşlem: {summary.IncomeCount + summary.ExpenseCount} (Gelir {summary.IncomeCount}, Gider {summary.ExpenseCount})");

                await _backupReport.SendTextAsync(sb.ToString().Trim());
                MessageBox.Show("Özet Telegram'a gönderildi.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Telegram gönderim hatası: " + ex.Message);
            }
            finally
            {
                senderButton.Enabled = true;
            }
        }
    }
}

