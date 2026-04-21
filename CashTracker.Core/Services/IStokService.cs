using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;

namespace CashTracker.Core.Services
{
    public interface IStokService
    {
        Task<decimal> GetCurrentStockAsync(int urunHizmetId, CancellationToken ct = default);
        Task<StokHareketResult> CreateMovementAsync(StokHareketCreateRequest request, CancellationToken ct = default);
    }
}
