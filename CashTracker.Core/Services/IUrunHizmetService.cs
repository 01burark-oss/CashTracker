using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;

namespace CashTracker.Core.Services
{
    public interface IUrunHizmetService
    {
        Task<List<UrunHizmet>> GetAllAsync(CancellationToken ct = default);
        Task<UrunHizmet?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<UrunHizmet?> GetByBarcodeAsync(string barcode, CancellationToken ct = default);
        Task<int> CreateAsync(UrunHizmetCreateRequest request, CancellationToken ct = default);
        Task UpdateAsync(UrunHizmet urunHizmet, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);
    }
}
