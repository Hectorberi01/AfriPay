using AfriPay.Domain.Exceptions;
using AfriPay.Domain.Payments.ValueObjects;

namespace AfriPay.Domain.Refunds;

/// <summary>
/// Aggregate root Refund.
///
/// Invariants :
///   • Le montant remboursé ne peut pas dépasser le montant initial du paiement.
///   • Un remboursement en statut final (Completed/Failed) ne peut plus changer.
///   • Un seul remboursement total OU plusieurs remboursements partiels,
///     tant que la somme ≤ montant original.
/// </summary>


// ═══════════════════════════════════════════════════════════════
// AGGREGATE ROOT : Refund
// ═══════════════════════════════════════════════════════════════

public sealed class Refund
{
    // ── Identité ───────────────────────────────────────────────
    public Guid           Id          { get; private set; }
    public Guid           PaymentId   { get; private set; }
    public Guid           MerchantId  { get; private set; }

    // ── Montant ────────────────────────────────────────────────
    public Money          Amount      { get; private set; } = default!;
    public bool           IsPartial   { get; private set; }   // true si montant < paiement original

    // ── Statut ─────────────────────────────────────────────────
    public RefundStatus   Status      { get; private set; }
    public RefundReason   Reason      { get; private set; }
    public string?        Notes       { get; private set; }   // détail libre

    // ── Provider ───────────────────────────────────────────────
    public string?        ProviderKey       { get; private set; }   // hérite du paiement
    public string?        ProviderReference { get; private set; }   // ref remboursement côté provider

    // ── Timestamps ─────────────────────────────────────────────
    public DateTimeOffset  CreatedAt        { get; private set; }
    public DateTimeOffset  EstimatedArrival { get; private set; }
    public DateTimeOffset? CompletedAt      { get; private set; }
    public DateTimeOffset? FailedAt         { get; private set; }
    public DateTimeOffset  UpdatedAt        { get; private set; }

    // ── Propriétés calculées ───────────────────────────────────
    public bool IsFinalState => Status is RefundStatus.Completed or RefundStatus.Failed;

    private Refund() { }   // EF Core

    // ═══════════════════════════════════════════════════════════
    // FACTORY
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Crée une demande de remboursement.
    /// </summary>
    /// <param name="paymentId">ID du paiement à rembourser.</param>
    /// <param name="merchantId">ID du marchand (pour l'accès control).</param>
    /// <param name="paymentAmount">Montant original du paiement (pour valider le partiel).</param>
    /// <param name="refundAmount">
    ///   Montant à rembourser. Si null, remboursement total (= paymentAmount).
    /// </param>
    /// <param name="reason">Raison du remboursement.</param>
    /// <param name="providerKey">Provider utilisé pour le paiement initial.</param>
    /// <param name="notes">Note libre optionnelle.</param>
    public static Refund Create(
        Guid         paymentId,
        Guid         merchantId,
        Money        paymentAmount,
        Money?       refundAmount,
        RefundReason reason,
        string       providerKey,
        string?      notes = null)
    {
        // Si montant non précisé → remboursement total
        var amount = refundAmount ?? paymentAmount;

        if (amount.Currency != paymentAmount.Currency)
            throw new DomainException(
                $"Refund currency '{amount.Currency}' must match payment currency '{paymentAmount.Currency}'.");

        if (amount.Amount <= 0)
            throw new DomainException("Refund amount must be positive.");

        if (amount.Amount > paymentAmount.Amount)
            throw new DomainException(
                $"Refund amount ({amount}) cannot exceed payment amount ({paymentAmount}).");

        var now = DateTimeOffset.UtcNow;

        return new Refund
        {
            Id               = Guid.NewGuid(),
            PaymentId        = paymentId,
            MerchantId       = merchantId,
            Amount           = amount,
            IsPartial        = amount.Amount < paymentAmount.Amount,
            Status           = RefundStatus.Pending,
            Reason           = reason,
            Notes            = notes,
            ProviderKey      = providerKey,
            CreatedAt        = now,
            UpdatedAt        = now,
            EstimatedArrival = EstimateArrival(providerKey, now),
        };
    }

    // ═══════════════════════════════════════════════════════════
    // MÉTHODES MÉTIER
    // ═══════════════════════════════════════════════════════════

    /// <summary>Confirme le remboursement après validation provider.</summary>
    public void MarkCompleted(string providerReference)
    {
        GuardFinalState("complete");

        ProviderReference = providerReference;
        Status            = RefundStatus.Completed;
        CompletedAt       = DateTimeOffset.UtcNow;
        UpdatedAt         = DateTimeOffset.UtcNow;
    }

    /// <summary>Marque le remboursement comme échoué.</summary>
    public void MarkFailed(string reason)
    {
        GuardFinalState("fail");

        Notes     = string.IsNullOrWhiteSpace(Notes)
            ? reason
            : $"{Notes} | Failure: {reason}";
        Status    = RefundStatus.Failed;
        FailedAt  = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // ── Helpers ────────────────────────────────────────────────

    /// <summary>
    /// Estimation du délai de réception selon le provider.
    /// Mobile Money : quasi-immédiat (1h).
    /// Carte : 3-5 jours ouvrés.
    /// </summary>
    private static DateTimeOffset EstimateArrival(string providerKey, DateTimeOffset now) =>
        providerKey is "stripe" or "paypal"
            ? now.AddDays(5)
            : now.AddHours(1);

    private void GuardFinalState(string action)
    {
        if (IsFinalState)
            throw new DomainException(
                $"Cannot {action} refund {Id}: already in final state '{Status}'.");
    }
}

/// <summary>
/// Exception de violation de règle métier dans le domaine Refund.
/// </summary>
public sealed class DomainException(string message) : Exception(message);