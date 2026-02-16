using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Services
{
    public sealed class DailyReportService : IDailyReportService
    {
        private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;
        private readonly IIsletmeService _isletmeService;

        private sealed class DailyRow
        {
            public string? Tip { get; init; }
            public string? OdemeYontemi { get; init; }
            public decimal Tutar { get; init; }
        }

        public DailyReportService(
            IDbContextFactory<CashTrackerDbContext> dbFactory,
            IIsletmeService isletmeService)
        {
            _dbFactory = dbFactory;
            _isletmeService = isletmeService;
        }

        public async Task<DailyReport> GetDailyReportAsync(DateTime date)
        {
            var from = date.Date;
            var to = date.Date.AddDays(1);
            var activeIsletmeId = await _isletmeService.GetActiveIdAsync();

            await using var db = await _dbFactory.CreateDbContextAsync();
            var rows = await db.Kasalar
                .AsNoTracking()
                .Where(x => x.IsletmeId == activeIsletmeId)
                .Where(x => x.Tarih >= from && x.Tarih < to)
                .Select(x => new DailyRow
                {
                    Tip = x.Tip,
                    OdemeYontemi = x.OdemeYontemi,
                    Tutar = x.Tutar
                })
                .ToListAsync();

            var incomeRows = rows.Where(x => IsIncomeTip(x.Tip)).ToList();
            var expenseRows = rows.Where(x => IsExpenseTip(x.Tip)).ToList();

            var paymentBreakdowns = BuildPaymentMethodBreakdowns(rows);

            return new DailyReport
            {
                Date = from,
                IncomeTotal = incomeRows.Sum(x => x.Tutar),
                ExpenseTotal = expenseRows.Sum(x => x.Tutar),
                IncomeCount = incomeRows.Count(),
                ExpenseCount = expenseRows.Count(),
                PaymentMethodBreakdowns = paymentBreakdowns
            };
        }

        private static List<DailyPaymentMethodBreakdown> BuildPaymentMethodBreakdowns(
            IReadOnlyCollection<DailyRow> rows)
        {
            var byMethod = rows
                .GroupBy(x => NormalizeOdemeYontemi(x.OdemeYontemi), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        Income = g.Where(x => IsIncomeTip(x.Tip)).Sum(x => x.Tutar),
                        Expense = g.Where(x => IsExpenseTip(x.Tip)).Sum(x => x.Tutar)
                    },
                    StringComparer.OrdinalIgnoreCase);

            var methods = new[] { "Nakit", "KrediKarti", "Havale" };
            var result = new List<DailyPaymentMethodBreakdown>(methods.Length);
            foreach (var method in methods)
            {
                var income = byMethod.TryGetValue(method, out var values) ? values.Income : 0m;
                var expense = byMethod.TryGetValue(method, out values) ? values.Expense : 0m;
                result.Add(new DailyPaymentMethodBreakdown
                {
                    Method = method,
                    IncomeTotal = income,
                    ExpenseTotal = expense
                });
            }

            return result;
        }

        private static bool IsIncomeTip(string? tip)
        {
            var normalized = NormalizeAscii(tip);
            return normalized is "gelir" or "giris" or "income";
        }

        private static bool IsExpenseTip(string? tip)
        {
            var normalized = NormalizeAscii(tip);
            return normalized is "gider" or "cikis" or "expense";
        }

        private static string NormalizeOdemeYontemi(string? value)
        {
            var normalized = NormalizeAscii(value);
            return normalized switch
            {
                "nakit" => "Nakit",
                "cash" => "Nakit",
                "kredikarti" => "KrediKarti",
                "kredi karti" => "KrediKarti",
                "kart" => "KrediKarti",
                "creditcard" => "KrediKarti",
                "credit card" => "KrediKarti",
                "havale" => "Havale",
                "transfer" => "Havale",
                "bank transfer" => "Havale",
                _ => "Nakit"
            };
        }

        private static string NormalizeAscii(string? value)
        {
            return (value ?? string.Empty)
                .Trim()
                .ToLowerInvariant()
                .Replace('\u0131', 'i')
                .Replace('\u015f', 's')
                .Replace('\u011f', 'g')
                .Replace('\u00fc', 'u')
                .Replace('\u00f6', 'o')
                .Replace('\u00e7', 'c');
        }
    }
}
