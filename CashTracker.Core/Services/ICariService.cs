using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Entities;

namespace CashTracker.Core.Services
{
    public interface ICariService
    {
        Task<List<CariKart>> GetAllAsync(CancellationToken ct = default);
        Task<CariKart?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<int> CreateAsync(CariKart cariKart, CancellationToken ct = default);
        Task UpdateAsync(CariKart cariKart, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);
        Task<int> CreateHareketAsync(CariHareket hareket, CancellationToken ct = default);
        Task<List<CariHareket>> GetHareketlerAsync(int cariKartId, CancellationToken ct = default);
        Task<decimal> GetBakiyeAsync(int cariKartId, CancellationToken ct = default);
    }
}
