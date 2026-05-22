using AfriPay.Domain.Exceptions;
using AfriPay.Domain.Payments.ValueObjects;

namespace AfriPay.Domain.Payments;

/// <summary>
/// Aggregate root Payment.
///
/// Invariants garantis :
///   • Un seul (MerchantId, IdempotencyKey) par transaction.
///   • Les transitions de statut sont séquentielles et tracées.
///   • Seul un paiement Pending peut être annulé.
///   • Seul un paiement Completed peut être remboursé.
///   • Un paiement en statut final (Completed/Failed/Cancelled/Expired)
///     ne peut plus changer de statut.
/// </summary>
public sealed class Payment
{
    //Identité
    public Guid   Id              { get; private set; }
    public Guid   MerchantId     { get; private set; }
    public string IdempotencyKey  { get; private set; } = default!;
 
    //Montant
    public Money  Amount          { get; private set; } = default!;
    public Money? Fee             { get; private set; }
    public Money? Net             { get; private set; }
 
    //Provider
    public string  ProviderKey        { get; private set; } = default!;  // demandé ("auto" possible)
    public string? ProviderUsed       { get; private set; }              // réellement utilisé
    public string? ProviderReference  { get; private set; }              // ref côté provider
 
    //Statut
    public PaymentStatus Status { get; private set; }
 
    //Client
    public CustomerInfo? Customer { get; private set; }
    
    //Métadonnées
    public Dictionary<string, string> Metadata   { get; private set; } = [];
    public string?                    WebhookUrl { get; private set; }
    public string?                    UssdCode   { get; private set; }
 
    //Timestamps
    public DateTimeOffset  CreatedAt   { get; private set; }
    public DateTimeOffset  ExpiresAt   { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset  UpdatedAt   { get; private set; }
 
    //Collections (append-only)
    private readonly List<PaymentStatusTransition> _transitions = [];
    private readonly List<ProviderAttempt>         _attempts    = [];
 
    public IReadOnlyList<PaymentStatusTransition> Transitions => _transitions.AsReadOnly();
    public IReadOnlyList<ProviderAttempt>         Attempts    => _attempts.AsReadOnly();
 
    //Propriétés calculées 
    public bool IsExpired    => DateTimeOffset.UtcNow > ExpiresAt && Status == PaymentStatus.Pending;
    public bool IsFinalState => Status is PaymentStatus.Completed
        or PaymentStatus.Failed or PaymentStatus.Cancelled
        or PaymentStatus.Expired or PaymentStatus.Refunded;
 
    private Payment() { }   // EF Core
    
    
    /// <summary>
    /// Crée un nouveau paiement en statut Pending.
    /// La transition initiale est enregistrée immédiatement dans l'audit trail.
    /// </summary>
    public static Payment Create(
        Guid                        merchantId,
        string                      idempotencyKey,
        Money                       amount,
        string                      providerKey,
        CustomerInfo?               customer,
        Dictionary<string, string>? metadata,
        string?                     webhookUrl)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new DomainException("IdempotencyKey is required.");
        if (string.IsNullOrWhiteSpace(providerKey))
            throw new DomainException("ProviderKey is required.");
 
        var now     = DateTimeOffset.UtcNow;
        var payment = new Payment
        {
            Id             = Guid.NewGuid(),
            MerchantId     = merchantId,
            IdempotencyKey = idempotencyKey,
            Amount         = amount,
            ProviderKey    = providerKey,
            Status         = PaymentStatus.Pending,
            Customer       = customer,
            Metadata       = metadata ?? [],
            WebhookUrl     = webhookUrl,
            CreatedAt      = now,
            ExpiresAt      = now.AddMinutes(30),
            UpdatedAt      = now,
        };
 
        payment.AddTransition(null, PaymentStatus.Pending, "Payment initiated", providerKey);
        return payment;
    }
    
    /// <summary>
    /// Enregistre une tentative d'appel provider (succès ou échec).
    /// Appelé après chaque appel à l'adapter provider, y compris les retries.
    /// </summary>
    public void RecordProviderAttempt(
        string  providerKey,
        string? providerReference,
        bool    success,
        string? errorCode,
        string? errorMessage = null,
        int?    latencyMs    = null,
        string? webhookUrl = null)
    {
        if (success)
        {
            ProviderUsed      = providerKey;
            ProviderReference = providerReference;
            WebhookUrl     = webhookUrl;
        }
 
        _attempts.Add(ProviderAttempt.Create(
            Id, providerKey, providerReference,
            success, errorCode, errorMessage, latencyMs));
 
        Touch();
    }
    
    /// <summary>
    /// Marque le paiement comme complété après confirmation provider.
    /// Calcule automatiquement les frais et le montant net.
    /// </summary>
    public void MarkCompleted(string providerReference, string providerKey)
    {
        GuardFinalState("complete");
 
        var fee = Money.CalculateFee(Amount, providerKey);
        var net = Amount.Subtract(fee);
 
        ProviderReference = providerReference;
        ProviderUsed      = providerKey;
        Fee               = fee;
        Net               = net;
        Status            = PaymentStatus.Completed;
        CompletedAt       = DateTimeOffset.UtcNow;
 
        AddTransition(PaymentStatus.Pending, PaymentStatus.Completed,
            "Payment confirmed by provider", providerKey);
        Touch();
    }
 
    /// <summary>
    /// Marque le paiement comme échoué.
    /// Appelé quand tous les retries et fallbacks sont épuisés.
    /// </summary>
    public void MarkFailed(string reason, string actor = "system")
    {
        GuardFinalState("fail");
 
        Status = PaymentStatus.Failed;
        AddTransition(PaymentStatus.Pending, PaymentStatus.Failed, reason, actor);
        Touch();
    }
    
    /// <summary>
    /// Annule un paiement en attente.
    /// Seul un paiement Pending peut être annulé — un paiement complété
    /// doit faire l'objet d'un remboursement.
    /// </summary>
    public void MarkCancelled(string actor = "merchant")
    {
        if (Status != PaymentStatus.Pending)
            throw new DomainException(
                $"Cannot cancel a payment in status '{Status}'. Only Pending payments can be cancelled.");
 
        Status = PaymentStatus.Cancelled;
        AddTransition(PaymentStatus.Pending, PaymentStatus.Cancelled, "Cancelled by merchant", actor);
        Touch();
    }
 
    /// <summary>
    /// Expire un paiement dont le délai de 30 minutes est dépassé.
    /// Appelé par le job de nettoyage ExpiryJob.
    /// </summary>
    public void MarkExpired()
    {
        if (Status != PaymentStatus.Pending)
            return;     // déjà dans un état final, pas d'erreur
 
        Status = PaymentStatus.Expired;
        AddTransition(PaymentStatus.Pending, PaymentStatus.Expired,
            "Payment expired after 30 minutes", "system");
        Touch();
    }
    
    /// <summary>
    /// Marque le paiement comme remboursé.
    /// Appelé après confirmation du remboursement par le provider.
    /// </summary>
    public void MarkRefunded()
    {
        if (Status != PaymentStatus.Completed)
            throw new DomainException(
                $"Cannot refund a payment in status '{Status}'. Only Completed payments can be refunded.");
 
        Status = PaymentStatus.Refunded;
        AddTransition(PaymentStatus.Completed, PaymentStatus.Refunded, "Full refund issued", "system");
        Touch();
    }
 
    /// <summary>Attache un code USSD à afficher au payeur (Mobile Money).</summary>
    public void SetUssdCode(string ussdCode)
    {
        UssdCode = ussdCode;
        Touch();
    }
    
    private void AddTransition(
        PaymentStatus? from, PaymentStatus to, string reason, string actor)
    {
        _transitions.Add(PaymentStatusTransition.Create(Id, from, to, reason, actor));
    }
 
    private void GuardFinalState(string action)
    {
        if (IsFinalState)
            throw new DomainException(
                $"Cannot {action} payment {Id}: already in final state '{Status}'.");
    }
 
    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}