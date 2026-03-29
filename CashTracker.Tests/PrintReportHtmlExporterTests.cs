using System;
using CashTracker.App;
using CashTracker.App.Printing;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using Xunit;

namespace CashTracker.Tests
{
    public sealed class PrintReportHtmlExporterTests
    {
        [Fact]
        public void Generate_NonPreviewLimitedAccountingReport_UsesLimitedTotals()
        {
            AppLocalization.SetLanguage("tr");

            var request = new PrintReportRequest
            {
                Template = PrintReportTemplate.AccountingReport,
                From = new DateTime(2026, 3, 1),
                To = new DateTime(2026, 3, 5),
                RangeDisplay = "Ozel",
                GeneratedAt = new DateTime(2026, 3, 5, 10, 0, 0),
                RecordLimit = 1,
                IsPreview = false
            };

            var report = PrintReportComposer.Compose(
                request,
                "Demo Isletme",
                new PeriodSummary
                {
                    From = request.From,
                    To = request.To,
                    IncomeTotal = 3333m,
                    ExpenseTotal = 4444m,
                    IncomeCount = 1,
                    ExpenseCount = 1
                },
                new[]
                {
                    new Kasa { Id = 1, Tarih = new DateTime(2026, 3, 1), Tip = "Gelir", Tutar = 1111m, OdemeYontemi = "Nakit", Kalem = "Satis", Aciklama = "Ilk Kayit" },
                    new Kasa { Id = 2, Tarih = new DateTime(2026, 3, 2), Tip = "Gider", Tutar = 4444m, OdemeYontemi = "Havale", Kalem = "Kira", Aciklama = "Ikinci Kayit" }
                });

            var html = PrintReportHtmlExporter.Generate(report);

            Assert.Contains("1.111,00", html);
            Assert.DoesNotContain(">3.333,00<", html);
            Assert.DoesNotContain(">4.444,00<", html);
            Assert.Contains("Ilk Kayit", html);
            Assert.DoesNotContain("Ikinci Kayit", html);
        }

        [Fact]
        public void Generate_PreviewReport_EncodesUserContentAndShowsPreviewHint()
        {
            AppLocalization.SetLanguage("tr");

            var request = new PrintReportRequest
            {
                Template = PrintReportTemplate.ExecutiveSummary,
                From = new DateTime(2026, 3, 1),
                To = new DateTime(2026, 3, 5),
                RangeDisplay = "Ozel",
                GeneratedAt = new DateTime(2026, 3, 5, 10, 0, 0),
                RecordLimit = 1,
                IsPreview = true,
                Note = "<script>alert(1)</script>"
            };

            var report = PrintReportComposer.Compose(
                request,
                "Demo Isletme",
                new PeriodSummary
                {
                    From = request.From,
                    To = request.To,
                    IncomeTotal = 1000m,
                    ExpenseTotal = 250m,
                    IncomeCount = 1,
                    ExpenseCount = 1
                },
                new[]
                {
                    new Kasa { Id = 1, Tarih = new DateTime(2026, 3, 1), Tip = "Gelir", Tutar = 1000m, OdemeYontemi = "Nakit", Kalem = "Satis", Aciklama = "<script>alert(1)</script>" },
                    new Kasa { Id = 2, Tarih = new DateTime(2026, 3, 2), Tip = "Gider", Tutar = 250m, OdemeYontemi = "Kredi Karti", Kalem = "Kira", Aciklama = "Normal" }
                });

            var html = PrintReportHtmlExporter.Generate(report);

            Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", html);
            Assert.DoesNotContain("<script>alert(1)</script>", html);
            Assert.Contains(AppLocalization.F("print.preview.sampleNote", report.VisibleRecordCount, report.TotalRecordCount), html);
        }

        [Fact]
        public void Generate_ExecutiveSummary_UsesPlaceholderWhenNoteIsMissing()
        {
            AppLocalization.SetLanguage("tr");

            var request = new PrintReportRequest
            {
                Template = PrintReportTemplate.ExecutiveSummary,
                From = new DateTime(2026, 3, 1),
                To = new DateTime(2026, 3, 5),
                RangeDisplay = "Ozel",
                GeneratedAt = new DateTime(2026, 3, 5, 10, 0, 0),
                IsPreview = false
            };

            var report = PrintReportComposer.Compose(
                request,
                "Demo Isletme",
                new PeriodSummary
                {
                    From = request.From,
                    To = request.To,
                    IncomeTotal = 600m,
                    ExpenseTotal = 200m,
                    IncomeCount = 1,
                    ExpenseCount = 1
                },
                new[]
                {
                    new Kasa { Id = 1, Tarih = new DateTime(2026, 3, 1), Tip = "Gelir", Tutar = 600m, OdemeYontemi = "Nakit", Kalem = "Satis" },
                    new Kasa { Id = 2, Tarih = new DateTime(2026, 3, 2), Tip = "Gider", Tutar = 200m, OdemeYontemi = "Nakit", Kalem = "Kira" }
                });

            var html = PrintReportHtmlExporter.Generate(report);

            Assert.Contains(AppLocalization.T("print.note.placeholder"), html);
            Assert.Contains("Demo Isletme", html);
            Assert.Contains("600,00", html);
            Assert.Contains("200,00", html);
        }
    }
}
