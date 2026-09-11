namespace CashTracker.Core.Services;

public sealed record SubscriptionPriceIncreaseNotice(
    int SubscriptionId,
    string UserReference,
    string RecipientEmail,
    decimal NewNetAmount,
    DateTime EffectiveAt,
    string TextVersion,
    string Text);

public sealed record SubscriptionPriceProtectionDecision(
    bool CanCharge,
    decimal AllowedNetAmount,
    string Reason,
    long? EvidenceId = null);

public interface ISubscriptionPriceProtectionService
{
    Task<long> SchedulePriceIncreaseAsync(
        SubscriptionPriceIncreaseNotice notice,
        DateTime nowUtc,
        CancellationToken ct = default);

    Task<SubscriptionPriceProtectionDecision> EvaluateRenewalAsync(
        int subscriptionId,
        decimal proposedNetAmount,
        DateTime renewalAtUtc,
        CancellationToken ct = default);
}
