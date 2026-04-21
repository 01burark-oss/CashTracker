using System;
using System.Threading;
using System.Threading.Tasks;

namespace CashTracker.Core.Services
{
    public interface IOnMuhasebeReportService
    {
        Task<string> CreateMonthlyExportAsync(DateTime month, string outputDirectory, CancellationToken ct = default);
    }
}
