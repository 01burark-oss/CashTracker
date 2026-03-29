using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;

namespace CashTracker.Core.Services
{
    public interface IReceiptOcrService
    {
        Task<ReceiptOcrResult> AnalyzeReceiptAsync(
            ReceiptOcrRequest request,
            CancellationToken ct = default);
    }
}
