using System;
using CashTracker.App;
using Xunit;

namespace CashTracker.Tests
{
    public sealed class SummaryRangeCatalogTests
    {
        [Theory]
        [InlineData(SummaryRangeCatalog.Daily, 0)]
        [InlineData(SummaryRangeCatalog.Weekly, 6)]
        [InlineData(SummaryRangeCatalog.Last30Days, 29)]
        [InlineData(SummaryRangeCatalog.Monthly, 5)]
        [InlineData(SummaryRangeCatalog.Last3Months, 89)]
        [InlineData(SummaryRangeCatalog.Last6Months, 180)]
        [InlineData(SummaryRangeCatalog.Last1Year, 364)]
        public void GetRange_ReturnsExpectedInclusiveSpan(string code, int expectedDaysInclusive)
        {
            var today = new DateTime(2026, 3, 6);
            var (from, to) = SummaryRangeCatalog.GetRange(code, today);

            Assert.Equal(today, to);
            Assert.Equal(expectedDaysInclusive, (to - from).Days);
        }

        [Fact]
        public void NormalizeCode_UnknownValue_FallsBackToDefault()
        {
            var actual = SummaryRangeCatalog.NormalizeCode("unsupported", SummaryRangeCatalog.Last1Year);
            Assert.Equal(SummaryRangeCatalog.Last1Year, actual);
        }

        [Fact]
        public void GetDisplay_Monthly_ReturnsCurrentMonthName()
        {
            AppLocalization.SetLanguage("tr");
            var actual = SummaryRangeCatalog.GetDisplay(SummaryRangeCatalog.Monthly, new DateTime(2026, 3, 6));
            Assert.Equal("Mart Ayi", actual);
        }

        [Fact]
        public void GetDisplay_Monthly_ChangesWithCurrentMonth()
        {
            AppLocalization.SetLanguage("tr");

            var march = SummaryRangeCatalog.GetDisplay(SummaryRangeCatalog.Monthly, new DateTime(2026, 3, 6));
            var april = SummaryRangeCatalog.GetDisplay(SummaryRangeCatalog.Monthly, new DateTime(2026, 4, 6));

            Assert.Equal("Mart Ayi", march);
            Assert.Equal("Nisan Ayi", april);
        }
    }
}
