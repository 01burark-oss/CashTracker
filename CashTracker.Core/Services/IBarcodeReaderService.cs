using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;

namespace CashTracker.Core.Services
{
    public interface IBarcodeReaderService
    {
        Task<BarcodeReadResult> TryReadAsync(string imagePath, CancellationToken ct = default);
    }
}
