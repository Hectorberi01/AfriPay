namespace AfriPay.Domain.Disputes;

/// <summary>
/// Contestation d'un paiement par le payeur.
/// Lost → balance débitée du montant + frais chargeback.
/// Won  → aucun impact financier.
/// </summary>
public sealed class Dispute
{
    public Guid           Id                { get; private set; }
    public Guid           MerchantId        { get; private set; }
    public Guid           PaymentId         { get; private set; }
    public DisputeStatus  Status            { get; private set; }
    public DisputeReason  Reason            { get; private set; }
    public long           Amount            { get; private set; }
    public string         Currency          { get; private set; } = default!;
    public long           ChargebackFee     { get; private set; }
    public string?        ProviderReference { get; private set; }
    public string?        ProviderKey       { get; private set; }
    public string?        CustomerNote      { get; private set; }
    public string?        MerchantNote      { get; private set; }
    public string?        ResolutionNote    { get; private set; }
    public DateTimeOffset RespondBy         { get; private set; }
    public DateTimeOffset CreatedAt         { get; private set; }
    public DateTimeOffset UpdatedAt         { get; private set; }
    public DateTimeOffset? ResolvedAt       { get; private set; }
 
    private readonly List<DisputeEvidence> _evidence = [];
    public IReadOnlyList<DisputeEvidence> Evidence => _evidence.AsReadOnly();
 
    private Dispute() { }
 
    public static Dispute Open(
        Guid merchantId, Guid paymentId, long amount, string currency,
        DisputeReason reason, string? providerReference = null,
        string? providerKey = null, string? customerNote = null)
        => new()
        {
            Id = Guid.NewGuid(), MerchantId = merchantId, PaymentId = paymentId,
            Status = DisputeStatus.Open, Reason = reason,
            Amount = amount, Currency = currency.ToUpperInvariant(),
            ChargebackFee = ChargebackFeeFor(currency),
            ProviderReference = providerReference, ProviderKey = providerKey,
            CustomerNote = customerNote,
            RespondBy = DateTimeOffset.UtcNow.AddDays(7),
            CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow,
        };
 
    public void SubmitEvidence(
        EvidenceType type, string fileName, string storageKey,
        string? merchantNote = null, string? description = null)
    {
        if (Status is not (DisputeStatus.Open or DisputeStatus.EvidenceSubmitted))
            throw new DisputeDomainException($"Cannot submit evidence in status '{Status}'.");
        if (DateTimeOffset.UtcNow > RespondBy)
            throw new DisputeDomainException("Deadline for evidence submission has passed.");
 
        _evidence.Add(DisputeEvidence.Create(Id, type, fileName, storageKey, description));
        if (merchantNote is not null) MerchantNote = merchantNote;
        Status = DisputeStatus.EvidenceSubmitted;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
 
    public void StartReview()
    {
        if (Status != DisputeStatus.EvidenceSubmitted)
            throw new DisputeDomainException("Only submitted disputes can be reviewed.");
        Status = DisputeStatus.UnderReview; UpdatedAt = DateTimeOffset.UtcNow;
    }
 
    public void MarkWon(string? note = null)
    {
        EnsureResolvable();
        Status = DisputeStatus.Won; ResolutionNote = note;
        ResolvedAt = DateTimeOffset.UtcNow; UpdatedAt = DateTimeOffset.UtcNow;
    }
 
    public void MarkLost(string? note = null)
    {
        EnsureResolvable();
        Status = DisputeStatus.Lost; ResolutionNote = note;
        ResolvedAt = DateTimeOffset.UtcNow; UpdatedAt = DateTimeOffset.UtcNow;
    }
 
    public void Cancel()
    {
        if (IsTerminal) throw new DisputeDomainException($"Cannot cancel a {Status} dispute.");
        Status = DisputeStatus.Cancelled;
        ResolvedAt = DateTimeOffset.UtcNow; UpdatedAt = DateTimeOffset.UtcNow;
    }
 
    public void Expire()
    {
        if (IsTerminal) return;
        Status = DisputeStatus.Expired;
        ResolvedAt = DateTimeOffset.UtcNow; UpdatedAt = DateTimeOffset.UtcNow;
    }
 
    public bool IsOverdue  => Status == DisputeStatus.Open && DateTimeOffset.UtcNow > RespondBy;
    public bool IsTerminal => Status is DisputeStatus.Won or DisputeStatus.Lost
                           or DisputeStatus.Cancelled or DisputeStatus.Expired;
    public long TotalChargebackAmount => Amount + ChargebackFee;
 
    private void EnsureResolvable()
    {
        if (IsTerminal) throw new DisputeDomainException($"Dispute is already {Status}.");
    }
 
    private static long ChargebackFeeFor(string currency) =>
        currency.ToUpperInvariant() switch
        {
            "XOF" or "XAF" => 2500,
            "EUR" or "USD"  => 1500,
            _               => 1000,
        };
}
 
public sealed class DisputeDomainException(string message) : Exception(message);