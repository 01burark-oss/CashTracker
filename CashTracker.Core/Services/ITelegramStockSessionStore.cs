using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;

namespace CashTracker.Core.Services
{
    public interface ITelegramStockSessionStore
    {
        Task<TelegramStockSessionState?> GetAsync(long chatId, long userId, CancellationToken ct = default);
        Task SaveAsync(TelegramStockSessionState state, CancellationToken ct = default);
        Task DeleteAsync(long chatId, long userId, CancellationToken ct = default);
    }
}
