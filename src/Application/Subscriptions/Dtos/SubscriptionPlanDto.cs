using AfriPay.Domain.Subscriptions;

namespace AfriPay.Application.Subscriptions.Dtos;

public sealed record SubscriptionPlanDto(
    string         PlanId,
    string         Name,
    string?        Description,
    long           Amount,
    string         Currency,
    string         Interval,
    int            IntervalCount,
    string         ProviderKey,
    int            TrialDays,
    bool           IsActive,
    DateTimeOffset CreatedAt)
{
    public static SubscriptionPlanDto FromDomain(SubscriptionPlan p) => new(
        p.Id.ToString(), p.Name, p.Description, p.Amount, p.Currency,
        p.Interval.ToString().ToLower(), p.IntervalCount, p.ProviderKey,
        p.TrialDays, p.IsActive, p.CreatedAt);
}
