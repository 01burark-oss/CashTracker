using System;
using CashTracker.App;
using CashTracker.App.Printing;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using Xunit;

namespace CashTracker.Tests
{
    public sealed class PrintReportComposerTests
    {
        [Fact]
        public void Compose_AggregatesPaymentMethodsAndCategories()
        {
            AppLocalization.SetLanguage("tr");

            var request = new PrintReportRequest
            {
                Template = PrintReportTemplate.AccountingReport,
                From = new DateTime(2026, 3, 1),
                To = new DateTime(2026, 3, 6),
                RangeDisplay = "Mart",
                GeneratedAt = new DateTime(2026, 3, 6, 12, 0, 0),
                RecordLimit = null
            };
            var summary = new PeriodSummary
            {
                From = request.From,
                To = request.To,
                IncomeTotal = 1750m,
                ExpenseTotal = 620m,
                IncomeCount = 2,
                ExpenseCount = 2
            };
            var rows = new[]
            {
                new Kasa { Id = 1, Tarih = new DateTime(2026, 3, 1), Tip = "Gelir", Tutar = 1000m, OdemeYontemi = "Nakit", Kalem = "Satis" },
                new Kasa { Id = 2, Tarih = new DateTime(2026, 3, 2), Tip = "Gelir", Tutar = 750m, OdemeYontemi = "KrediKarti", Kalem = "Paket" },
                new Kasa { Id = 3, Tarih = new DateTime(2026, 3, 3), Tip = "Gider", Tutar = 500m, OdemeYontemi = "Nakit", Kalem = "Kira" },
                new Kasa { Id = 4, Tarih = new DateTime(2026, 3, 4), Tip = "Gider", Tutar = 120m, OdemeYontemi = "Havale", Kalem = "Temizlik" }
            };

            var report = PrintReportComposer.Compose(request, "Demo Isletme", summary, rows);

            Assert.Equal(4, report.TotalRecordCount);
            Assert.Equal(4, report.VisibleRecordCount);
            Assert.Equal(3, report.PaymentMethods.Count);
            Assert.Equal("Nakit", report.PaymentMethods[0].DisplayName);
            Assert.Equal(1000m, report.PaymentMethods[0].Income);
            Assert.Equal(500m, report.PaymentMethods[0].Expense);
            Assert.Equal("Kira", report.ExpenseCategories[0].CategoryName);
            Assert.Equal("Satis", report.IncomeCategories[0].CategoryName);
        }

        [Fact]
        public void Compose_UsesFallbackNamesAndAppliesRecordLimit()
        {
            AppLocalization.SetLanguage("tr");

            var request = new PrintReportRequest
            {
                Template = PrintReportTemplate.AccountingReport,
                From = new DateTime(2026, 3, 1),
                To = new DateTime(2026, 3, 6),
                RangeDisplay = "Ozel",
                GeneratedAt = new DateTime(2026, 3, 6, 12, 0, 0),
                RecordLimit = 1,
                IsPreview = true
            };

            var rows = new[]
            {
                new Kasa { Id = 8, Tarih = new DateTime(2026, 3, 5), Tip = "Gider", Tutar = 320m, OdemeYontemi = "OnlineOdeme", Kalem = null, GiderTuru = null, Aciklama = null },
                new Kasa { Id = 9, Tarih = new DateTime(2026, 3, 6), Tip = "Gelir", Tutar = 910m, OdemeYontemi = "Kredi Karti", Kalem = null, GiderTuru = null, Aciklama = "Acilis" }
            };

            var report = PrintReportComposer.Compose(
                request,
                "Demo",
                new PeriodSummary { From = request.From, To = request.To, IncomeTotal = 910m, ExpenseTotal = 320m, IncomeCount = 1, ExpenseCount = 1 },
                rows);

            Assert.True(report.IsPreview);
            Assert.Equal(2, report.TotalRecordCount);
            Assert.Single(report.Records);
            Assert.Equal("Genel Gider", report.ExpenseCategories[0].CategoryName);
            Assert.Equal("Genel Gelir", report.IncomeCategories[0].CategoryName);
            Assert.Equal("Genel Gider", report.Records[0].Description);
        }
    }
}
