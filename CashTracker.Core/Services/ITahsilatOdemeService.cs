using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;

namespace CashTracker.Core.Services
{
    public interface ITahsilatOdemeService
    {
        Task<int> CreateAsync(TahsilatOdemeRequest request, CancellationToken ct = default);
    }
}
