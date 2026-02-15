using System;
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
            var query = db.Kasalar.AsNoTracking()
                .Where(x => x.IsletmeId == activeIsletmeId)
                .Where(x => x.Tarih >= from && x.Tarih < to);

            // SQLite decimal SUM translation is limited; aggregate on client for exactness.
            var incomeValues = await query
                .Where(x => x.Tip == "Gelir" || x.Tip == "Giris")
                .Select(x => x.Tutar)
                .ToListAsync();

            var expenseValues = await query
                .Where(x => x.Tip == "Gider" || x.Tip == "Cikis")
                .Select(x => x.Tutar)
                .ToListAsync();

            var incomeTotal = incomeValues.Sum();
            var expenseTotal = expenseValues.Sum();

            var incomeCount = await query
                .CountAsync(x => x.Tip == "Gelir" || x.Tip == "Giris");

            var expenseCount = await query
                .CountAsync(x => x.Tip == "Gider" || x.Tip == "Cikis");

            return new DailyReport
            {
                Date = from,
                IncomeTotal = incomeTotal,
                ExpenseTotal = expenseTotal,
                IncomeCount = incomeCount,
                ExpenseCount = expenseCount
            };
        }
    }
}
