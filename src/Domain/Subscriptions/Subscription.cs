namespace AfriPay.Domain.Subscriptions;

public sealed class Subscription
{
    public Guid               Id                 { get; private set; }
    public Guid               MerchantId         { get; private set; }
    public Guid               PlanId             { get; private set; }
    public SubscriptionStatus Status             { get; private set; }
    public string             CustomerPhone      { get; private set; } = default!;
    public string?            CustomerEmail      { get; private set; }
    public string?            CustomerName       { get; private set; }
    public long               Amount             { get; private set; }
    public string             Currency           { get; private set; } = default!;
    public string             ProviderKey        { get; private set; } = default!;
    public DateTimeOffset     CurrentPeriodStart { get; private set; }
    public DateTimeOffset     CurrentPeriodEnd   { get; private set; }
    public DateTimeOffset?    TrialEnd           { get; private set; }
    public DateTimeOffset?    CancelledAt        { get; private set; }
    public DateTimeOffset?    EndsAt             { get; private set; }
    public int                RetryCount         { get; private set; }
    public DateTimeOffset?    NextRetryAt        { get; private set; }
    public string?            LastFailReason     { get; private set; }
    public DateTimeOffset     CreatedAt          { get; private set; }
    public DateTimeOffset     UpdatedAt          { get; private set; }
 
    private Subscription() { }
 
    public static Subscription Create(
        Guid merchantId, SubscriptionPlan plan,
        string customerPhone, string? customerEmail = null,
        string? customerName = null, DateTimeOffset? startsAt = null)
    {
        var now   = startsAt ?? DateTimeOffset.UtcNow;
        var start = plan.TrialDays > 0 ? now.AddDays(plan.TrialDays) : now;
        return new Subscription
        {
            Id = Guid.NewGuid(), MerchantId = merchantId, PlanId = plan.Id,
            Status = SubscriptionStatus.Active,
            CustomerPhone = customerPhone, CustomerEmail = customerEmail, CustomerName = customerName,
            Amount = plan.Amount, Currency = plan.Currency, ProviderKey = plan.ProviderKey,
            CurrentPeriodStart = now, CurrentPeriodEnd = plan.NextBillingDate(start),
            TrialEnd = plan.TrialDays > 0 ? now.AddDays(plan.TrialDays) : null,
            CreatedAt = now, UpdatedAt = now,
        };
    }
 
    public void RenewPeriod(SubscriptionPlan plan)
    {
        EnsureNotTerminal();
        CurrentPeriodStart = CurrentPeriodEnd;
        CurrentPeriodEnd   = plan.NextBillingDate(CurrentPeriodEnd);
        RetryCount = 0; NextRetryAt = null; LastFailReason = null;
        Status = SubscriptionStatus.Active; UpdatedAt = DateTimeOffset.UtcNow;
    }
 
    public void RecordRenewalFailure(string reason, DateTimeOffset nextRetry)
    {
        EnsureNotTerminal();
        RetryCount++; LastFailReason = reason; NextRetryAt = nextRetry;
        Status = SubscriptionStatus.PastDue; UpdatedAt = DateTimeOffset.UtcNow;
    }
 
    public void Cancel(bool immediately = false)
    {
        if (Status == SubscriptionStatus.Cancelled)
            throw new SubscriptionDomainException("Already cancelled.");
        CancelledAt = DateTimeOffset.UtcNow;
        Status  = immediately ? SubscriptionStatus.Cancelled : Status;
        EndsAt  = immediately ? DateTimeOffset.UtcNow : CurrentPeriodEnd;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
 
    public void Pause()
    {
        EnsureNotTerminal();
        Status = SubscriptionStatus.Paused; UpdatedAt = DateTimeOffset.UtcNow;
    }
 
    public void Resume(SubscriptionPlan plan)
    {
        if (Status != SubscriptionStatus.Paused)
            throw new SubscriptionDomainException("Only paused subscriptions can be resumed.");
        Status = SubscriptionStatus.Active;
        CurrentPeriodEnd = plan.NextBillingDate(DateTimeOffset.UtcNow);
        UpdatedAt = DateTimeOffset.UtcNow;
    }
 
    public void Expire()
    {
        Status = SubscriptionStatus.Expired; UpdatedAt = DateTimeOffset.UtcNow;
    }
 
    public bool IsInTrial       => TrialEnd.HasValue && DateTimeOffset.UtcNow < TrialEnd;
    public bool IsDueForRenewal => Status == SubscriptionStatus.Active
                                && DateTimeOffset.UtcNow >= CurrentPeriodEnd;
    public bool IsDueForRetry   => Status == SubscriptionStatus.PastDue
                                && NextRetryAt.HasValue
                                && DateTimeOffset.UtcNow >= NextRetryAt;
 
    private void EnsureNotTerminal()
    {
        if (Status is SubscriptionStatus.Cancelled or SubscriptionStatus.Expired)
            throw new SubscriptionDomainException($"Cannot modify a {Status} subscription.");
    }
}
 
public sealed class SubscriptionDomainException(string message) : Exception(message);
 