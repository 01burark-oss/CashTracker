using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CashTracker.Core.Entities;

namespace CashTracker.Core.Services
{
    public interface IKasaService
    {
        Task<List<Kasa>> GetAllAsync(DateTime? from = null, DateTime? to = null);
        Task<Kasa?> GetByIdAsync(int id);
        Task<int> CreateAsync(Kasa kasa);
        Task UpdateAsync(Kasa kasa);
        Task DeleteAsync(int id);
    }
}
