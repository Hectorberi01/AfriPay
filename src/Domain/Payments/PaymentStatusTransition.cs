namespace AfriPay.Domain.Payments;

/// <summary>
/// Audit trail immuable des transitions de statut.
/// Append-only — jamais de UPDATE ni DELETE en base.
/// </summary>
public sealed class PaymentStatusTransition
{
    public Guid           Id         { get; private set; }
    public Guid           PaymentId  { get; private set; }
    public PaymentStatus? FromStatus { get; private set; }  // null = première transition
    public PaymentStatus  ToStatus   { get; private set; }
    public string         Reason     { get; private set; } = default!;
    public string         Actor      { get; private set; } = default!;  // provider_key ou "system"
    public DateTimeOffset OccurredAt { get; private set; }
 
    private PaymentStatusTransition() { }   // EF Core
 
    internal static PaymentStatusTransition Create(
        Guid paymentId, PaymentStatus? from, PaymentStatus to,
        string reason, string actor) => new()
    {
        Id         = Guid.NewGuid(),
        PaymentId  = paymentId,
        FromStatus = from,
        ToStatus   = to,
        Reason     = reason,
        Actor      = actor,
        OccurredAt = DateTimeOffset.UtcNow,
    };
}