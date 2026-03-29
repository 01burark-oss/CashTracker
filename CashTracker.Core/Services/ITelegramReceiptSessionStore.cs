using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;

namespace CashTracker.Core.Services
{
    public interface ITelegramReceiptSessionStore
    {
        Task<TelegramReceiptSessionState?> GetAsync(
            long chatId,
            long userId,
            CancellationToken ct = default);

        Task SaveAsync(
            TelegramReceiptSessionState state,
            CancellationToken ct = default);

        Task DeleteAsync(
            long chatId,
            long userId,
            CancellationToken ct = default);
    }
}
