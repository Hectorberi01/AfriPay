namespace AfriPay.Domain.Subscriptions;

public sealed class SubscriptionPlan
{
    public Guid            Id            { get; private set; }
    public Guid            MerchantId    { get; private set; }
    public string          Name          { get; private set; } = default!;
    public string?         Description   { get; private set; }
    public long            Amount        { get; private set; }
    public string          Currency      { get; private set; } = default!;
    public BillingInterval Interval      { get; private set; }
    public int             IntervalCount { get; private set; } = 1;
    public string          ProviderKey   { get; private set; } = default!;
    public int             TrialDays     { get; private set; }
    public bool            IsActive      { get; private set; }
    public DateTimeOffset  CreatedAt     { get; private set; }
 
    private SubscriptionPlan() { }
 
    public static SubscriptionPlan Create(
        Guid merchantId, string name, long amount, string currency,
        BillingInterval interval, string providerKey,
        int intervalCount = 1, int trialDays = 0, string? description = null)
    {
        if (amount <= 0)
            throw new SubscriptionDomainException("Plan amount must be positive.");
        if (intervalCount <= 0)
            throw new SubscriptionDomainException("Interval count must be at least 1.");
 
        return new SubscriptionPlan
        {
            Id = Guid.NewGuid(), MerchantId = merchantId, Name = name,
            Description = description, Amount = amount,
            Currency = currency.ToUpperInvariant(), Interval = interval,
            IntervalCount = intervalCount, ProviderKey = providerKey,
            TrialDays = trialDays, IsActive = true, CreatedAt = DateTimeOffset.UtcNow,
        };
    }
 
    public void Deactivate() => IsActive = false;
 
    public DateTimeOffset NextBillingDate(DateTimeOffset from) => Interval switch
    {
        BillingInterval.Daily     => from.AddDays(IntervalCount),
        BillingInterval.Weekly    => from.AddDays(IntervalCount * 7),
        BillingInterval.Monthly   => from.AddMonths(IntervalCount),
        BillingInterval.Quarterly => from.AddMonths(IntervalCount * 3),
        BillingInterval.Yearly    => from.AddYears(IntervalCount),
        _                         => from.AddMonths(1),
    };
}