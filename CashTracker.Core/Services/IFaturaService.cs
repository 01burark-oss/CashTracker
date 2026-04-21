using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;

namespace CashTracker.Core.Services
{
    public interface IFaturaService
    {
        Task<List<Fatura>> GetAllAsync(CancellationToken ct = default);
        Task<FaturaDetail?> GetDetailAsync(int id, CancellationToken ct = default);
        Task<FaturaTotals> CalculateTotalsAsync(IEnumerable<FaturaSatirRequest> satirlar, CancellationToken ct = default);
        Task<int> CreateDraftAsync(FaturaCreateRequest request, CancellationToken ct = default);
        Task UpdateDraftAsync(int id, FaturaCreateRequest request, CancellationToken ct = default);
        Task MarkAsPortalDraftAsync(int id, string uuid, string belgeNo, CancellationToken ct = default);
        Task MarkAsIssuedAsync(int id, CancellationToken ct = default);
        Task CancelAsync(int id, CancellationToken ct = default);
    }
}
