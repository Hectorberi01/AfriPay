using AfriPay.Domain.Subscriptions;

namespace AfriPay.Application.Subscriptions.Dtos;

public sealed record SubscriptionDto(
    string         SubscriptionId,
    string         PlanId,
    string         Status,
    string         CustomerPhone,
    string?        CustomerEmail,
    string?        CustomerName,
    long           Amount,
    string         Currency,
    string         ProviderKey,
    bool           IsInTrial,
    DateTimeOffset CurrentPeriodStart,
    DateTimeOffset CurrentPeriodEnd,
    DateTimeOffset? TrialEnd,
    DateTimeOffset? CancelledAt,
    DateTimeOffset? EndsAt,
    int            RetryCount,
    string?        LastFailReason,
    DateTimeOffset CreatedAt)
{
    public static SubscriptionDto FromDomain(Subscription s) => new(
        s.Id.ToString(), s.PlanId.ToString(), s.Status.ToString().ToLower(),
        s.CustomerPhone, s.CustomerEmail, s.CustomerName,
        s.Amount, s.Currency, s.ProviderKey, s.IsInTrial,
        s.CurrentPeriodStart, s.CurrentPeriodEnd, s.TrialEnd,
        s.CancelledAt, s.EndsAt, s.RetryCount, s.LastFailReason, s.CreatedAt);
}