using System;
using System.Threading.Tasks;
using CashTracker.Core.Models;

namespace CashTracker.Core.Services
{
    public interface IDailyReportService
    {
        Task<DailyReport> GetDailyReportAsync(DateTime date);
    }
}
