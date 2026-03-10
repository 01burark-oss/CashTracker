using System;
using System.Threading.Tasks;
using CashTracker.Core.Models;

namespace CashTracker.Core.Services
{
    public interface IDashboardSnapshotService
    {
        Task<DashboardSnapshot> GetSnapshotAsync(
            DateTime referenceDate,
            string primaryRangeCode,
            string secondaryRangeCode,
            int month,
            int monthYear,
            int year);
    }
}
