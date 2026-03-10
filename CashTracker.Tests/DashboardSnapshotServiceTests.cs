using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CashTracker.App;
using CashTracker.App.Services;
using CashTracker.Core.Entities;
using CashTracker.Tests.Support;
using Xunit;

namespace CashTracker.Tests
{
    public sealed class DashboardSnapshotServiceTests
    {
        [Fact]
        public async Task GetSnapshotAsync_AggregatesDailyMonthlyAndYearlySummaries()
        {
            var today = DateTime.Today;
            var rows = new List<Kasa>
            {
                new() { Id = 1, Tarih = today, Tip = "Gelir", Tutar = 100m, OdemeYontemi = "Nakit" },
                new() { Id = 2, Tarih = today, Tip = "Gider", Tutar = 30m, OdemeYontemi = "KrediKarti" },
                new() { Id = 3, Tarih = today.AddDays(-5), Tip = "Gelir", Tutar = 250m, OdemeYontemi = "Havale" },
                new() { Id = 4, Tarih = new DateTime(today.Year, 1, 10), Tip = "Gider", Tutar = 50m, OdemeYontemi = "OnlineOdeme" }
            };

            var service = new DashboardSnapshotService(
                new FakeKasaService(rows),
                new FakeIsletmeService());

            var snapshot = await service.GetSnapshotAsync(
                today,
                SummaryRangeCatalog.Last30Days,
                SummaryRangeCatalog.Last1Year,
                today.Month,
                today.Year,
                today.Year);

            Assert.Equal(100m, snapshot.DailySummary.IncomeTotal);
            Assert.Equal(30m, snapshot.DailySummary.ExpenseTotal);
            Assert.Equal(350m, snapshot.MonthlySummary.IncomeTotal);
            Assert.Equal(80m, snapshot.YearlySummary.ExpenseTotal);
            Assert.Equal(4, snapshot.DailyPaymentMethodBreakdowns.Count);
        }
    }
}
