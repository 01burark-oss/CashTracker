using System.Collections.Generic;
using System.Threading.Tasks;
using CashTracker.Core.Entities;

namespace CashTracker.Core.Services
{
    public interface IKalemTanimiService
    {
        Task<List<KalemTanimi>> GetAllAsync();
        Task<List<KalemTanimi>> GetByTipAsync(string tip);
        Task<int> CreateAsync(string tip, string ad);
        Task DeleteAsync(int id);
    }
}
