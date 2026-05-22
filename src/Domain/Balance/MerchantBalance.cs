using AfriPay.Domain.Exceptions;

namespace AfriPay.Domain.Balance;

/// <summary>
/// Solde d'un marchand pour une devise donnée.
///
/// Contient le solde disponible (prêt pour payout) et
/// le solde en attente (paiements reçus mais pas encore settled).
///
/// Règle : AvailableBalance + PendingBalance = solde total.
/// </summary>
public class MerchantBalance
{
    public Guid          Id               { get; private set; }
    public Guid          MerchantId       { get; private set; }
    public string        Currency         { get; private set; } = default!;
    public BalanceStatus Status           { get; private set; }
 
    /// <summary>Solde disponible pour payout (settled).</summary>
    public long          AvailableBalance { get; private set; }
 
    /// <summary>Solde en attente de confirmation (T+1 ou T+2 selon provider).</summary>
    public long          PendingBalance   { get; private set; }
 
    /// <summary>Solde total réservé pour des remboursements en cours.</summary>
    public long          ReservedBalance  { get; private set; }
 
    public DateTimeOffset CreatedAt       { get; private set; }
    public DateTimeOffset UpdatedAt       { get; private set; }
 
    private MerchantBalance() { }
 
    public static MerchantBalance Create(Guid merchantId, string currency)
        => new()
        {
            Id               = Guid.NewGuid(),
            MerchantId       = merchantId,
            Currency         = currency.ToUpperInvariant(),
            Status           = BalanceStatus.Active,
            AvailableBalance = 0,
            PendingBalance   = 0,
            ReservedBalance  = 0,
            CreatedAt        = DateTimeOffset.UtcNow,
            UpdatedAt        = DateTimeOffset.UtcNow,
        };

    
    /// <summary>
    /// Crédite le solde en attente lors d'un paiement complété.
    /// Restera en pending jusqu'au settlement (T+1 ou T+2).
    /// </summary>
    public BalanceEntry CreditPending(long amount, string referenceId, string? description = null)
    {
        EnsureActive();
        PendingBalance += amount;
        UpdatedAt       = DateTimeOffset.UtcNow;
 
        return BalanceEntry.CreateCredit(
            MerchantId, EntrySource.Payment, amount, Currency,
            referenceId, description,
            runningBalance: AvailableBalance + PendingBalance);
    }
    
    /// <summary>
    /// Transfère du solde pending vers le solde disponible (settlement).
    /// </summary>
    public BalanceEntry Settle(long amount, string referenceId)
    {
        EnsureActive();
 
        if (amount > PendingBalance)
            throw new DomainException(
                $"Cannot settle {amount} — pending balance is only {PendingBalance}.");
 
        PendingBalance   -= amount;
        AvailableBalance += amount;
        UpdatedAt         = DateTimeOffset.UtcNow;
 
        return BalanceEntry.CreateCredit(
            MerchantId, EntrySource.Adjustment, amount, Currency,
            referenceId, "Settlement: pending → available",
            runningBalance: AvailableBalance + PendingBalance);
    }
    
    /// <summary>
    /// Débite le solde disponible pour un remboursement.
    /// Réserve le montant jusqu'à confirmation du provider.
    /// </summary>
    public BalanceEntry DebitForRefund(long amount, string refundId)
    {
        EnsureActive();
 
        if (amount > AvailableBalance)
            throw new DomainException(
                $"Insufficient balance for refund. " +
                $"Available: {AvailableBalance}, Requested: {amount}.");
 
        AvailableBalance -= amount;
        ReservedBalance  += amount;
        UpdatedAt         = DateTimeOffset.UtcNow;
 
        return BalanceEntry.CreateDebit(
            MerchantId, EntrySource.Refund, amount, Currency,
            refundId, "Refund debit",
            runningBalance: AvailableBalance + PendingBalance);
    }
    
    /// <summary>Débite les frais AfriPay sur le solde disponible.</summary>
    public BalanceEntry DebitFee(long feeAmount, string paymentId)
    {
        EnsureActive();
 
        if (feeAmount > AvailableBalance)
            throw new DomainException("Insufficient balance for fee debit.");
 
        AvailableBalance -= feeAmount;
        UpdatedAt         = DateTimeOffset.UtcNow;
 
        return BalanceEntry.CreateDebit(
            MerchantId, EntrySource.Fee, feeAmount, Currency,
            paymentId, "AfriPay commission",
            runningBalance: AvailableBalance + PendingBalance);
    }
 
    /// <summary>Débite le solde disponible pour un payout vers le marchand.</summary>
    public BalanceEntry DebitForPayout(long amount, string payoutId)
    {
        EnsureActive();
 
        if (amount > AvailableBalance)
            throw new DomainException(
                $"Insufficient available balance for payout. " +
                $"Available: {AvailableBalance}, Requested: {amount}.");
 
        AvailableBalance -= amount;
        UpdatedAt         = DateTimeOffset.UtcNow;
 
        return BalanceEntry.CreateDebit(
            MerchantId, EntrySource.Payout, amount, Currency,
            payoutId, "Payout to merchant",
            runningBalance: AvailableBalance + PendingBalance);
    }
    
    /// <summary>Libère le solde réservé après confirmation du remboursement.</summary>
    public void ReleaseReserve(long amount)
    {
        if (amount > ReservedBalance)
            throw new DomainException("Cannot release more than reserved.");
 
        ReservedBalance -= amount;
        UpdatedAt        = DateTimeOffset.UtcNow;
    }
 
    /// <summary>Gèle le solde (suspicion de fraude).</summary>
    public void Freeze()
    {
        if (Status == BalanceStatus.Frozen)
            throw new DomainException("Balance is already frozen.");
 
        Status    = BalanceStatus.Frozen;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
 
    public void Unfreeze()
    {
        if (Status != BalanceStatus.Frozen)
            throw new DomainException("Balance is not frozen.");
 
        Status    = BalanceStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
    
    // Computed 
 
    public long TotalBalance => AvailableBalance + PendingBalance;
    public bool HasSufficientFunds(long amount) => AvailableBalance >= amount;
 
    // Guard 
 
    private void EnsureActive()
    {
        if (Status == BalanceStatus.Frozen)
            throw new DomainException(
                "Balance is frozen. Contact support@afripay.io.");
 
        if (Status == BalanceStatus.Closed)
            throw new DomainException("Balance is closed.");
    }
}