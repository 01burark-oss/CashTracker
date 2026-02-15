using System.Collections.Generic;
using System.Threading.Tasks;
using CashTracker.Core.Entities;

namespace CashTracker.Core.Services
{
    public interface IIsletmeService
    {
        Task<List<Isletme>> GetAllAsync();
        Task<Isletme?> GetByIdAsync(int id);
        Task<Isletme> GetActiveAsync();
        Task<int> GetActiveIdAsync();
        Task<int> CreateAsync(string ad, bool makeActive = false);
        Task RenameAsync(int id, string ad);
        Task SetActiveAsync(int id);
    }
}
