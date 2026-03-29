using System.Drawing;
using CashTracker.App;
using CashTracker.App.Printing;
using Xunit;

namespace CashTracker.Tests
{
    public sealed class PrintReportLayoutMetricsTests
    {
        [Fact]
        public void MeasureRecordRowHeight_ShortValues_StaysAtMinimumHeight()
        {
            AppLocalization.SetLanguage("tr");

            using var bitmap = new Bitmap(1200, 800);
            using var graphics = Graphics.FromImage(bitmap);
            using var font = new Font("Segoe UI", 8f, FontStyle.Regular);

            var row = new PrintRecordRow
            {
                Date = new DateTime(2026, 3, 1),
                Description = "Kisa aciklama",
                CategoryDisplay = "Kahve",
                MethodDisplay = "Nakit",
                Amount = 125m,
                IsIncome = true
            };

            var height = PrintReportLayoutMetrics.MeasureRecordRowHeight(graphics, font, row, [90f, 180f, 120f, 80f, 80f, 90f], 20f, 54f);

            Assert.Equal(20f, height);
        }

        [Fact]
        public void MeasureRecordRowHeight_LongDescription_GrowsButStaysWithinLimit()
        {
            AppLocalization.SetLanguage("tr");

            using var bitmap = new Bitmap(1200, 800);
            using var graphics = Graphics.FromImage(bitmap);
            using var font = new Font("Segoe UI", 8f, FontStyle.Regular);

            var row = new PrintRecordRow
            {
                Date = new DateTime(2026, 3, 1),
                Description = "Uzun aciklama satiri musteri notlariyla birlikte birden fazla satira tasarak muhasebe raporunda kesilmeyi tetiklemelidir.",
                CategoryDisplay = "Atistirmalik ve kahvalti urunleri",
                MethodDisplay = "Kurumsal kart odemesi",
                Amount = 125m,
                IsIncome = true
            };

            var height = PrintReportLayoutMetrics.MeasureRecordRowHeight(graphics, font, row, [90f, 180f, 120f, 80f, 80f, 90f], 20f, 54f);

            Assert.True(height > 20f);
            Assert.True(height <= 54f);
        }

        [Fact]
        public void MeasureSummaryRowHeight_LongCategory_GrowsButStaysWithinLimit()
        {
            using var bitmap = new Bitmap(1200, 800);
            using var graphics = Graphics.FromImage(bitmap);
            using var font = new Font("Segoe UI", 8f, FontStyle.Regular);

            var height = PrintReportLayoutMetrics.MeasureSummaryRowHeight(
                graphics,
                font,
                "Uzun kategori adi birden fazla satira tasarak detay kategori sayfasinda daha buyuk bir satir gerektirmelidir",
                220f,
                20f,
                42f);

            Assert.True(height > 20f);
            Assert.True(height <= 42f);
        }
    }
}
