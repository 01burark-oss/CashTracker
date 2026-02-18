using System;
using System.Threading;
using System.Threading.Tasks;

namespace CashTracker.Core.Services
{
    public enum TelegramApprovalStatus
    {
        Approved,
        Rejected,
        TimedOut,
        NotConfigured,
        Failed
    }

    public sealed record TelegramApprovalRequest(
        string Title,
        string Details,
        TimeSpan Timeout);

    public sealed record TelegramApprovalResult(
        TelegramApprovalStatus Status,
        string? Message = null);

    public interface ITelegramApprovalService
    {
        Task<TelegramApprovalResult> RequestApprovalAsync(
            TelegramApprovalRequest request,
            CancellationToken ct = default);

        bool TryResolve(string code, bool approved, out string? title);
    }
}
