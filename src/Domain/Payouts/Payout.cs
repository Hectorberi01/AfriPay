namespace AfriPay.Domain.Payouts;

public sealed class Payout
{
    public Guid              Id                { get; private set; }
    public Guid              MerchantId        { get; private set; }
    public PayoutStatus      Status            { get; private set; }
    public PayoutMethod      Method            { get; private set; }
 
    // Montant
    public long              Amount            { get; private set; }
    public string            Currency          { get; private set; } = default!;
 
    // Destination
    public PayoutDestination Destination       { get; private set; } = default!;
 
    // Provider
    public string?           ProviderReference { get; private set; }
    public string?           FailureReason     { get; private set; }
 
    // Schedule
    public PayoutSchedule    Schedule          { get; private set; }
    public string?           Notes             { get; private set; }
 
    // Timestamps
    public DateTimeOffset    CreatedAt         { get; private set; }
    public DateTimeOffset    UpdatedAt         { get; private set; }
    public DateTimeOffset?   ProcessingAt      { get; private set; }
    public DateTimeOffset?   CompletedAt       { get; private set; }
    public DateTimeOffset?   FailedAt          { get; private set; }
    
    private Payout() { }

    public static Payout Create(
        Guid merchantId,
        long amount,
        string currency,
        PayoutDestination destination,
        PayoutSchedule schedule = PayoutSchedule.Manual,
        string? notes = null)
    {
        if (amount <= 0)
            throw new PayoutDomainException("Payout amount must be positive.");

        if (amount < MinimumPayout(currency))
            throw new PayoutDomainException(
                $"Minimum payout for {currency} is {MinimumPayout(currency)}.");

        return new Payout
        {
            Id = Guid.NewGuid(),
            MerchantId = merchantId,
            Status = PayoutStatus.Pending,
            Method = destination.Method,
            Amount = amount,
            Currency = currency.ToUpperInvariant(),
            Destination = destination,
            Schedule = schedule,
            Notes = notes,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
    }
    
    public void MarkProcessing()
    {
        if (Status != PayoutStatus.Pending)
            throw new PayoutDomainException(
                $"Cannot process payout in status '{Status}'.");
 
        Status       = PayoutStatus.Processing;
        ProcessingAt = DateTimeOffset.UtcNow;
        UpdatedAt    = DateTimeOffset.UtcNow;
    }
 
    public void MarkCompleted(string providerReference)
    {
        if (Status != PayoutStatus.Processing)
            throw new PayoutDomainException(
                $"Cannot complete payout in status '{Status}'.");
 
        Status            = PayoutStatus.Completed;
        ProviderReference = providerReference;
        CompletedAt       = DateTimeOffset.UtcNow;
        UpdatedAt         = DateTimeOffset.UtcNow;
    }
    
    public void MarkFailed(string reason)
    {
        if (Status is not (PayoutStatus.Pending or PayoutStatus.Processing))
            throw new PayoutDomainException(
                $"Cannot fail payout in status '{Status}'.");
 
        Status        = PayoutStatus.Failed;
        FailureReason = reason;
        FailedAt      = DateTimeOffset.UtcNow;
        UpdatedAt     = DateTimeOffset.UtcNow;
    }
 
    public void Cancel(string? reason = null)
    {
        if (Status != PayoutStatus.Pending)
            throw new PayoutDomainException(
                "Only pending payouts can be cancelled.");
 
        Status        = PayoutStatus.Cancelled;
        FailureReason = reason;
        UpdatedAt     = DateTimeOffset.UtcNow;
    }
    
    /// <summary>Montant minimum de payout par devise.</summary>
    private static long MinimumPayout(string currency)
        => currency.ToUpperInvariant() switch
        {
            "XOF" => 1000, 
            "XAF" => 1000,
            "EUR" => 1000,
            "USD" => 1000,
            _     => 500,
        };
}
public sealed class PayoutDomainException(string message) : Exception(message);
