namespace CashTracker.Core.Models;

public static class SubscriptionNotificationEventTypes
{
    public const string PaidPeriodStarted = "subscription.paid-period-started";
    public const string RenewalDue = "subscription.renewal-due";
    public const string PaymentFailed = "subscription.payment-failed";
    public const string CancellationScheduled = "subscription.cancellation-scheduled";
    public const string PriceIncrease = "subscription.price-increase";
}

public abstract record SubscriptionNotificationEvent(
    string EventType,
    int SubscriptionId,
    int BusinessId,
    string AccountType,
    DateTime OccurredAt);

public sealed record PaidSubscriptionPeriodStarted(
    int SubscriptionId,
    int BusinessId,
    string AccountType,
    DateTime OccurredAt,
    DateTime PeriodStartsAt,
    DateTime PeriodEndsAt,
    decimal NetAmount,
    string Currency)
    : SubscriptionNotificationEvent(SubscriptionNotificationEventTypes.PaidPeriodStarted, SubscriptionId, BusinessId, AccountType, OccurredAt);

public sealed record SubscriptionRenewalDue(
    int SubscriptionId,
    int BusinessId,
    string AccountType,
    DateTime OccurredAt,
    DateTime RenewalAt,
    decimal ProposedNetAmount,
    string Currency)
    : SubscriptionNotificationEvent(SubscriptionNotificationEventTypes.RenewalDue, SubscriptionId, BusinessId, AccountType, OccurredAt);

public sealed record SubscriptionPaymentFailed(
    int SubscriptionId,
    int BusinessId,
    string AccountType,
    DateTime OccurredAt,
    decimal AttemptedNetAmount,
    string Currency,
    string FailureCode)
    : SubscriptionNotificationEvent(SubscriptionNotificationEventTypes.PaymentFailed, SubscriptionId, BusinessId, AccountType, OccurredAt);

public sealed record SubscriptionCancellationScheduled(
    int SubscriptionId,
    int BusinessId,
    string AccountType,
    DateTime OccurredAt,
    DateTime PeriodEndsAt)
    : SubscriptionNotificationEvent(SubscriptionNotificationEventTypes.CancellationScheduled, SubscriptionId, BusinessId, AccountType, OccurredAt);

public sealed record SubscriptionPriceIncreaseAnnounced(
    int SubscriptionId,
    int BusinessId,
    string AccountType,
    DateTime OccurredAt,
    decimal OldNetAmount,
    decimal NewNetAmount,
    string Currency,
    DateTime EffectiveAt,
    string TextVersion)
    : SubscriptionNotificationEvent(SubscriptionNotificationEventTypes.PriceIncrease, SubscriptionId, BusinessId, AccountType, OccurredAt);
