using System;
using System.Threading.Tasks;
using CashTracker.Core.Models;

namespace CashTracker.Core.Services
{
    public interface ISummaryService
    {
        Task<PeriodSummary> GetSummaryAsync(DateTime from, DateTime to);
        Task<PeriodSummary> GetMonthlySummaryAsync(int year, int month);
    }
}
